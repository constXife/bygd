using System.Collections.Generic;
using Bygd.Framework;
using UnityEngine;

namespace Bygd
{
    internal static class OutpostVisuals
    {
        private const float FireRefuelThreshold = 2f;
        private const float FireRefuelAmount = 4f;
        private const float SearchRadius = 10f;
        private const string DecorTag = "bygd_decor";

        private static readonly string[] FoodDecorPrefabs = {
            PrefabNames.TankardDvergr, PrefabNames.Tankard, PrefabNames.CookedMeat
        };

        private static readonly string[] WoodStackPrefabs = {
            PrefabNames.WoodCoreStack, PrefabNames.BlackwoodStack
        };

        // --- Auto-refuel fires ---

        public static void RefuelNearbyFires(Transform tableTransform, ZNetView tableNview)
        {
            foreach (var fireplace in Object.FindObjectsOfType<Fireplace>())
            {
                if (fireplace == null)
                    continue;

                float dist = Vector3.Distance(fireplace.transform.position, tableTransform.position);
                if (dist > SearchRadius)
                    continue;

                bool isTorch = fireplace.gameObject.name.Contains("torch");
                int fuel = isTorch ? OutpostResources.GetResin(tableNview) : OutpostResources.GetWood(tableNview);
                if (fuel <= 0)
                    continue;

                float currentFuel = GetFireplaceFuel(fireplace);
                if (currentFuel >= FireRefuelThreshold)
                    continue;

                int addFuel = (int)Mathf.Min(FireRefuelAmount, fuel);
                SetFireplaceFuel(fireplace, currentFuel + addFuel);

                if (isTorch)
                {
                    OutpostResources.SetResin(tableNview, fuel - addFuel);
                    Log.Diag($"Torch refueled: +{addFuel} resin");
                }
                else
                {
                    OutpostResources.SetWood(tableNview, fuel - addFuel);
                    Log.Diag($"Fireplace refueled: +{addFuel} wood");
                }
            }
        }

        private static float GetFireplaceFuel(Fireplace fireplace)
        {
            var nview = fireplace.GetComponent<ZNetView>();
            if (nview == null)
                return 0f;

            object zdo = Reflect.ZNetView_GetZDO.Invoke(nview, null);
            if (zdo == null)
                return 0f;

            return (float)Reflect.ZDO_GetFloat.Invoke(zdo, new object[] { "fuel", 0f });
        }

        private static void SetFireplaceFuel(Fireplace fireplace, float fuel)
        {
            var nview = fireplace.GetComponent<ZNetView>();
            if (nview == null)
                return;

            object zdo = Reflect.ZNetView_GetZDO.Invoke(nview, null);
            if (zdo == null)
                return;

            Reflect.ZDO_Set_Float.Invoke(zdo, new object[] { "fuel", fuel });
        }

        // --- Food display on dining table ---

        public static void UpdateFoodDisplay(Transform outpostTableTransform, ZNetView tableNview)
        {
            int calories = OutpostResources.GetCalories(tableNview);
            int displayCount = CaloriesDisplayCount(calories);

            Transform diningTable = FindDiningTable(outpostTableTransform);

            CleanupDecor();

            if (displayCount <= 0 || diningTable == null)
                return;

            float tableTopY = FindTableTopY(diningTable);

            for (int i = 0; i < displayCount; i++)
            {
                string prefabName = FoodDecorPrefabs[i % FoodDecorPrefabs.Length];
                var prefab = ZNetScene.instance?.GetPrefab(prefabName);
                if (prefab == null)
                    continue;

                Vector3 localOffset = GetTableDecorOffset(i, displayCount);
                Vector3 worldPos = diningTable.position
                    + diningTable.right * localOffset.x
                    + diningTable.forward * localOffset.z;
                worldPos.y = tableTopY;

                var decor = CreateVisualClone(prefab, worldPos, diningTable);
                if (decor != null)
                    decor.name = DecorTag;
            }
        }

        // Creates a purely visual clone — mesh only, no components
        private static GameObject CreateVisualClone(GameObject prefab, Vector3 position, Transform parent)
        {
            var clone = Object.Instantiate(prefab, position, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));

            // Remove ALL components except Transform, MeshFilter, MeshRenderer, LODGroup
            StripComponents(clone);

            // Make it a child of the dining table — moves with it
            clone.transform.SetParent(parent, true);

            return clone;
        }

        private static void StripComponents(GameObject obj)
        {
            // First collect all components to destroy
            var toDestroy = new List<Component>();

            foreach (var comp in obj.GetComponentsInChildren<Component>(true))
            {
                if (comp == null)
                    continue;

                if (comp is Transform)
                    continue;
                if (comp is MeshFilter)
                    continue;
                if (comp is MeshRenderer)
                    continue;
                if (comp is LODGroup)
                    continue;

                toDestroy.Add(comp);
            }

            foreach (var comp in toDestroy)
                Object.DestroyImmediate(comp);
        }

        private static int CaloriesDisplayCount(int calories)
        {
            if (calories <= 0) return 0;
            if (calories <= 120) return 1;   // ~3 average food items
            if (calories <= 400) return 2;   // ~10 average food items
            return 3;
        }

        // Evenly spaced in a circle on the table, with a small radius from center
        private static Vector3 GetTableDecorOffset(int index, int total)
        {
            float radius = 0.25f;
            if (total == 1)
                return Vector3.zero; // centered

            float angle = (360f / total) * index;
            float rad = angle * Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(rad) * radius, 0f, Mathf.Cos(rad) * radius);
        }

        // Raycast top-down to find the table surface
        private static float FindTableTopY(Transform table)
        {
            Vector3 rayStart = table.position + Vector3.up * 2f;
            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 3f))
                return hit.point.y + 0.05f; // slightly above the surface

            // Fallback: standard vanilla table height
            return table.position.y + 0.85f;
        }

        // --- Wood display near house wall (outside) ---

        private static GameObject _woodDecorInstance;

        public static void UpdateWoodDisplay(Transform outpostTableTransform, ZNetView tableNview)
        {
            int wood = OutpostResources.GetWood(tableNview);

            // Wood ran out — remove the stack
            if (wood <= 0)
            {
                if (_woodDecorInstance != null)
                {
                    Object.Destroy(_woodDecorInstance);
                    _woodDecorInstance = null;
                }
                return;
            }

            // Stack already exists — don't recreate
            if (_woodDecorInstance != null)
                return;

            GameObject prefab = null;
            foreach (string name in WoodStackPrefabs)
            {
                prefab = ZNetScene.instance?.GetPrefab(name);
                if (prefab != null)
                    break;
            }

            if (prefab == null)
                return;

            Vector3 spawnPos;
            Quaternion rotation;
            if (!FindOutsideWallSpot(outpostTableTransform, out spawnPos, out rotation))
                return;

            _woodDecorInstance = Object.Instantiate(prefab, spawnPos, rotation);
            StripComponents(_woodDecorInstance);
            _woodDecorInstance.name = WoodDecorTag;
        }

        private static bool FindOutsideWallSpot(Transform tableTransform, out Vector3 position, out Quaternion rotation)
        {
            position = Vector3.zero;
            rotation = Quaternion.identity;

            Vector3 origin = tableTransform.position + Vector3.up * 0.5f;

            // Find a wall: raycast in 12 directions
            Vector3 bestWallHit = Vector3.zero;
            Vector3 bestWallNormal = Vector3.forward;
            float bestDist = float.MaxValue;

            for (int i = 0; i < 12; i++)
            {
                float angle = i * 30f * Mathf.Deg2Rad;
                Vector3 dir = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle));

                if (!Physics.Raycast(origin, dir, out RaycastHit hit, SearchRadius))
                    continue;

                if (hit.collider.GetComponentInParent<WearNTear>() == null)
                    continue;

                // Horizontal normal = wall (not floor/roof)
                if (Mathf.Abs(hit.normal.y) > 0.3f)
                    continue;

                if (hit.distance < bestDist)
                {
                    bestDist = hit.distance;
                    bestWallHit = hit.point;
                    bestWallNormal = hit.normal;
                }
            }

            if (bestDist >= float.MaxValue)
                return false;

            // Second raycast: from wall point outward, to ensure we place OUTSIDE
            // Check: from the point outward (along normal) there should be no roof
            Vector3 outsidePos = bestWallHit + bestWallNormal * 1.0f;

            bool hasRoof = Physics.Raycast(outsidePos + Vector3.up * 0.3f, Vector3.up, out RaycastHit roofHit, 10f)
                && roofHit.collider.GetComponentInParent<WearNTear>() != null;

            if (hasRoof)
            {
                // This side is under the roof — try the opposite
                outsidePos = bestWallHit - bestWallNormal * 1.0f;
                bestWallNormal = -bestWallNormal;

                hasRoof = Physics.Raycast(outsidePos + Vector3.up * 0.3f, Vector3.up, out roofHit, 10f)
                    && roofHit.collider.GetComponentInParent<WearNTear>() != null;

                if (hasRoof)
                    return false; // both sides are under the roof
            }

            position = outsidePos;

            // Height from terrain
            if (ZoneSystem.instance != null)
                position.y = ZoneSystem.instance.GetGroundHeight(position);

            // Parallel to the wall
            Vector3 wallForward = Vector3.Cross(bestWallNormal, Vector3.up).normalized;
            rotation = Quaternion.LookRotation(wallForward, Vector3.up);

            return true;
        }

        private const string WoodDecorTag = "bygd_wood_decor";

        private static void CleanupByTag(string tag)
        {
            var toDestroy = new List<GameObject>();
            foreach (var go in Object.FindObjectsOfType<Transform>())
            {
                if (go != null && go.gameObject.name == tag)
                    toDestroy.Add(go.gameObject);
            }
            foreach (var obj in toDestroy)
                Object.Destroy(obj);
        }

        private static Transform FindDiningTable(Transform center)
        {
            Piece closest = null;
            float closestDist = SearchRadius;

            foreach (var piece in Object.FindObjectsOfType<Piece>())
            {
                if (piece == null)
                    continue;

                string pieceName = piece.gameObject.name;
                if (!pieceName.StartsWith("piece_table") || pieceName.StartsWith(PrefabNames.OutpostTable))
                    continue;

                float dist = Vector3.Distance(piece.transform.position, center.position);
                if (dist < closestDist)
                {
                    closest = piece;
                    closestDist = dist;
                }
            }

            return closest?.transform;
        }

        private static void CleanupDecor()
        {
            CleanupByTag(DecorTag);
        }

        // --- Utility ---

        private static T FindNearest<T>(Transform center) where T : Component
        {
            T closest = null;
            float closestDist = SearchRadius;

            foreach (var comp in Object.FindObjectsOfType<T>())
            {
                if (comp == null)
                    continue;

                float dist = Vector3.Distance(comp.transform.position, center.position);
                if (dist < closestDist)
                {
                    closest = comp;
                    closestDist = dist;
                }
            }

            return closest;
        }
    }
}
