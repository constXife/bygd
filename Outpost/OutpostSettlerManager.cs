using System.Collections.Generic;
using Bygd.Framework;
using UnityEngine;

namespace Bygd
{
    internal static class OutpostSettlerManager
    {
        private const float SettlerAdoptRadius = 4f;

        public static Vector3 GetSpawnPosition(Transform t) => NPCSpawnHelper.GetSpawnPosition(t);

        public static GameObject SpawnSettler(ZNetView tableNview, Transform tableTransform)
        {
            var settler = NPCSpawnHelper.SpawnDverger<SettlerNPC>(tableTransform, "Elder");
            if (settler == null) return null;

            OutpostSettlerBinding.BindSettler(tableNview, tableTransform, settler);

            if (Player.m_localPlayer != null)
                Player.m_localPlayer.Message(MessageHud.MessageType.Center,
                    Localization.instance.Localize("$msg_settler_arrived"));

            return settler;
        }

        public static void DespawnSettler(GameObject settler, ZNetView tableNview)
        {
            OutpostSettlerBinding.ClearBoundSettler(tableNview);
            NPCSpawnHelper.Despawn(settler);

            if (Player.m_localPlayer != null)
                Player.m_localPlayer.Message(MessageHud.MessageType.Center,
                    Localization.instance.Localize("$msg_settler_left"));
        }

        public static GameObject RefreshSettlerReference(ZNetView tableNview, Transform tableTransform)
        {
            Vector3 spawnPos = GetSpawnPosition(tableTransform);
            var characters = Reflect.Character_GetAllCharacters?.Invoke(null, null) as List<Character>;
            if (characters == null) return null;

            string tableKey = OutpostSettlerBinding.GetTableKey(tableNview, tableTransform);
            string storedSettlerId = OutpostSettlerBinding.GetBoundSettlerId(tableNview);

            var boundCandidates = new List<Character>();
            Character exactMatch = null;
            Character closest = null;
            float closestDist = float.MaxValue;

            foreach (var c in characters)
            {
                if (c == null) continue;

                string charId = OutpostSettlerBinding.GetObjectZdoId(c.gameObject);
                if (!string.IsNullOrEmpty(storedSettlerId) && charId == storedSettlerId)
                    exactMatch = c;

                string charTableKey = OutpostSettlerBinding.GetSettlerTableKey(c.gameObject);
                if (!string.IsNullOrEmpty(tableKey) && charTableKey == tableKey)
                {
                    boundCandidates.Add(c);
                    float dist = Vector3.Distance(c.transform.position, spawnPos);
                    if (dist < closestDist)
                    {
                        closest = c;
                        closestDist = dist;
                    }
                    continue;
                }

                // Legacy: Dverger nearby without binding
                if (!IsLegacyCandidate(c)) continue;
                float legacyDist = Vector3.Distance(c.transform.position, spawnPos);
                if (legacyDist > SettlerAdoptRadius || legacyDist >= closestDist) continue;
                if (exactMatch != null || boundCandidates.Count > 0) continue;

                closest = c;
                closestDist = legacyDist;
            }

            Character resolved = exactMatch ?? closest;
            if (resolved == null) return null;

            var obj = resolved.gameObject;
            if (obj.GetComponent<SettlerNPC>() == null)
                obj.AddComponent<SettlerNPC>();

            OutpostSettlerBinding.BindSettler(tableNview, tableTransform, obj);

            if (OutpostSettlerBinding.IsOwner(tableNview))
            {
                foreach (var candidate in boundCandidates)
                {
                    if (candidate != null && candidate != resolved)
                        NPCSpawnHelper.Despawn(candidate.gameObject);
                }
            }

            return obj;
        }

        private static bool IsLegacyCandidate(Character c)
        {
            string name = c.name;
            return name.StartsWith(PrefabNames.Dverger) || name.StartsWith("OutpostElder");
        }
    }
}
