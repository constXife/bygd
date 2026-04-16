using System.Collections;
using System.Collections.Generic;
using Bygd.Framework;
using UnityEngine;

namespace Bygd
{
    /// <summary>
    /// Lumberjack work loop: find a tree, walk to it, chop it, collect drops, plant a sapling, return home.
    /// </summary>
    internal class LumberjackWorker : MonoBehaviour
    {
        private const float MinTreeDistance = 20f;   // Do not chop inside the village radius.
        private const float MaxTreeDistance = 80f;   // Do not roam too far away.
        private const float ChopTime = 5f;           // Seconds spent chopping a tree.
        private const float WorkCycleInterval = 30f; // Pause between work cycles.
        private const float ChopDamage = 50f;        // Chop damage per hit.

        private LumberjackPostComponent _post;
        private ZNetView _tableNview;
        private bool _working;
        private string _currentTask = "Idle";

        public bool IsWorking => _working;
        public string CurrentTask => _currentTask;

        public void Init(LumberjackPostComponent post, ZNetView tableNview)
        {
            _post = post;
            _tableNview = tableNview;
            _currentTask = Localize("$lumberjack_task_idle", "Idle");
            StartCoroutine(WorkLoop());
        }

        private static string Localize(string key, string fallback)
        {
            return Localization.instance != null ? Localization.instance.Localize(key) : fallback;
        }

        private IEnumerator WorkLoop()
        {
            yield return new WaitForSeconds(10f); // Allow the world to finish loading.

            while (true)
            {
                yield return new WaitForSeconds(WorkCycleInterval);

                if (_post == null)
                    yield break;

                // Find a tree.
                var tree = FindNearestTree();
                if (tree == null)
                {
                    _currentTask = Localize("$lumberjack_task_no_trees", "No trees nearby");
                    continue;
                }

                _working = true;

                // Walk to the tree.
                _currentTask = Localize("$lumberjack_task_walking", "Walking to tree");
                yield return WalkTo(tree.transform.position);

                // Chop the tree.
                if (tree != null)
                {
                    _currentTask = Localize("$lumberjack_task_chopping", "Chopping tree");
                    yield return ChopTree(tree);
                }

                // Collect nearby drops.
                _currentTask = Localize("$lumberjack_task_collecting", "Collecting wood");
                yield return new WaitForSeconds(1f);
                var collected = CollectNearbyDrops();

                // Plant a replacement sapling.
                if (collected.seeds > 0 && tree != null)
                {
                    _currentTask = Localize("$lumberjack_task_planting", "Planting sapling");
                    PlantSapling(tree.transform.position);
                    yield return new WaitForSeconds(1f);
                }

                // Return wood to the outpost.
                if (collected.wood > 0)
                {
                    var table = _post.FindParentTable();
                    if (table != null)
                    {
                        var nv = table.GetComponent<ZNetView>();
                        int current = OutpostResources.GetWood(nv);
                        OutpostResources.SetWood(nv, current + collected.wood);
                        Log.Info($"Lumberjack delivered {collected.wood} wood (total: {current + collected.wood})");
                    }
                }

                // Return home.
                _currentTask = Localize("$lumberjack_task_returning", "Returning");
                yield return WalkTo(_post.transform.position);

                _currentTask = Localize("$lumberjack_task_idle", "Idle");
                _working = false;
            }
        }

        // --- Tree search ---

        private TreeBase FindNearestTree()
        {
            Vector3 postPos = _post.transform.position;
            TreeBase closest = null;
            float closestDist = MaxTreeDistance;

            foreach (var tree in Object.FindObjectsOfType<TreeBase>())
            {
                if (tree == null) continue;

                float dist = Vector3.Distance(tree.transform.position, postPos);
                if (dist < MinTreeDistance || dist > MaxTreeDistance)
                    continue;

                if (dist < closestDist)
                {
                    closest = tree;
                    closestDist = dist;
                }
            }

            return closest;
        }

        // --- Movement ---

        private IEnumerator WalkTo(Vector3 target)
        {
            var walker = CourierWalker.StartWalking(gameObject, target, null);
            if (walker == null)
                yield break;

            // Wait until the walker arrives or times out.
            float timeout = 60f;
            float elapsed = 0f;
            while (walker != null && elapsed < timeout)
            {
                elapsed += 1f;
                yield return new WaitForSeconds(1f);
            }
        }

        // --- Chopping ---

        private IEnumerator ChopTree(TreeBase tree)
        {
            float elapsed = 0f;
            while (tree != null && elapsed < ChopTime)
            {
                // Apply chop damage to the tree.
                var hit = new HitData();
                hit.m_damage.m_chop = ChopDamage;
                hit.m_point = tree.transform.position;
                hit.m_dir = (tree.transform.position - transform.position).normalized;
                hit.m_toolTier = 0; // Stone axe.

                var nview = tree.GetComponent<ZNetView>();
                if (nview != null)
                    tree.Damage(hit);

                elapsed += 1.5f;
                yield return new WaitForSeconds(1.5f);
            }
        }

        // --- Drop collection ---

        private struct CollectedLoot
        {
            public int wood;
            public int seeds;
        }

        private CollectedLoot CollectNearbyDrops()
        {
            var loot = new CollectedLoot();
            float pickupRadius = 10f;

            var items = new List<ItemDrop>();
            foreach (var item in Object.FindObjectsOfType<ItemDrop>())
            {
                if (item == null) continue;
                float dist = Vector3.Distance(item.transform.position, transform.position);
                if (dist > pickupRadius) continue;
                items.Add(item);
            }

            foreach (var item in items)
            {
                if (item == null || item.m_itemData == null) continue;

                string name = item.m_itemData.m_shared.m_name;

                if (name == "$item_wood" || name == "$item_finewood" || name == "$item_roundlog")
                {
                    loot.wood += item.m_itemData.m_stack;
                    ZNetScene.instance.Destroy(item.gameObject);
                }
                else if (name == "$item_beechseeds" || name == "$item_pinecone" || name == "$item_acorn")
                {
                    loot.seeds += item.m_itemData.m_stack;
                    ZNetScene.instance.Destroy(item.gameObject);
                }
            }

            return loot;
        }

        // --- Planting ---

        private void PlantSapling(Vector3 treePosition)
        {
            // Pick a sapling by tree type. Keep a simple fallback for now.
            string saplingName = PrefabNames.SaplingBeech;

            var prefab = ZNetScene.instance?.GetPrefab(saplingName);
            if (prefab == null)
            {
                Log.Diag($"Sapling '{saplingName}' not found");
                return;
            }

            // Plant slightly away from the stump.
            Vector3 plantPos = treePosition + Random.insideUnitSphere * 2f;
            plantPos.y = treePosition.y;

            Object.Instantiate(prefab, plantPos, Quaternion.identity);
            Log.Info($"Lumberjack planted a sapling @ {plantPos}");
        }

        void OnDestroy()
        {
            var walker = GetComponent<CourierWalker>();
            if (walker != null)
                Destroy(walker);
        }
    }
}
