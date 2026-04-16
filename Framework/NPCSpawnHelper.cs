using System.Collections.Generic;
using UnityEngine;

namespace Bygd.Framework
{
    /// <summary>
    /// Common spawn/despawn/search logic for Dverger NPCs across all modules.
    /// </summary>
    internal static class NPCSpawnHelper
    {
        private const float SpawnOffset = 2f;

        public static Vector3 GetSpawnPosition(Transform anchor)
        {
            return anchor.position + anchor.forward * SpawnOffset + Vector3.up * 0.5f;
        }

        /// <summary>
        /// Spawns a Dverger NPC with the specified component and name.
        /// </summary>
        public static GameObject SpawnDverger<TNpc>(Transform anchor, string displayName) where TNpc : BaseNPC
        {
            var prefab = ZNetScene.instance?.GetPrefab(PrefabNames.Dverger);
            if (prefab == null)
            {
                Log.Error($"Dverger prefab not found for {typeof(TNpc).Name}");
                return null;
            }

            Vector3 pos = GetSpawnPosition(anchor);
            var obj = Object.Instantiate(prefab, pos, Quaternion.identity);

            var tameable = obj.GetComponent<Tameable>();
            if (tameable != null)
                Reflect.Tameable_Tame?.Invoke(tameable, null);

            if (Reflect.Character_m_name != null)
                Reflect.Character_m_name.SetValue(obj.GetComponent<Character>(), displayName);

            if (obj.GetComponent<TNpc>() == null)
                obj.AddComponent<TNpc>();

            Log.Info($"{displayName} spawned @ {pos}");
            return obj;
        }

        /// <summary>
        /// Despawns the NPC via ZNetScene (removes both the GameObject and ZDO).
        /// </summary>
        public static void Despawn(GameObject npc)
        {
            if (npc == null) return;
            ZNetScene.instance.Destroy(npc);
        }

        /// <summary>
        /// Finds the nearest NPC with component T near the anchor.
        /// </summary>
        public static GameObject FindNearestNPC<TNpc>(Transform anchor, float radius) where TNpc : Component
        {
            var characters = Reflect.Character_GetAllCharacters?.Invoke(null, null) as List<Character>;
            if (characters == null) return null;

            Vector3 pos = GetSpawnPosition(anchor);
            GameObject closest = null;
            float closestDist = radius;

            foreach (var c in characters)
            {
                if (c == null) continue;
                if (c.GetComponent<TNpc>() == null) continue;

                float dist = Vector3.Distance(c.transform.position, pos);
                if (dist < closestDist)
                {
                    closest = c.gameObject;
                    closestDist = dist;
                }
            }

            return closest;
        }
    }
}
