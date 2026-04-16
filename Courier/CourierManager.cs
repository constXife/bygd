using System.Collections.Generic;
using Bygd.Framework;
using UnityEngine;

namespace Bygd
{
    internal static class CourierManager
    {
        private const int InitialBudget = 200;

        public static Vector3 GetSpawnPosition(Transform t) => NPCSpawnHelper.GetSpawnPosition(t);

        public static GameObject SpawnCourier(ZNetView postNview, Transform postTransform)
        {
            var courier = NPCSpawnHelper.SpawnDverger<CourierNPC>(
                postTransform,
                Localization.instance?.Localize("$courier_name") ?? "Courier");
            if (courier == null) return null;

            CourierBinding.BindCourier(postNview, postTransform, courier);

            if (CourierBinding.GetBudget(postNview) <= 0)
                CourierBinding.SetBudget(postNview, InitialBudget);

            if (Player.m_localPlayer != null)
                Player.m_localPlayer.Message(MessageHud.MessageType.Center,
                    Localization.instance.Localize("$msg_courier_arrived"));

            return courier;
        }

        public static void DespawnCourier(GameObject courier, ZNetView postNview)
        {
            CourierBinding.ClearBoundCourier(postNview);
            NPCSpawnHelper.Despawn(courier);

            if (Player.m_localPlayer != null)
                Player.m_localPlayer.Message(MessageHud.MessageType.Center,
                    Localization.instance.Localize("$msg_courier_left"));
        }

        public static GameObject RefreshCourierReference(ZNetView postNview, Transform postTransform)
        {
            Vector3 spawnPos = GetSpawnPosition(postTransform);
            var characters = Reflect.Character_GetAllCharacters?.Invoke(null, null) as List<Character>;
            if (characters == null) return null;

            string postKey = CourierBinding.GetPostKey(postNview);
            string storedCourierId = CourierBinding.GetBoundCourierId(postNview);

            var boundCandidates = new List<Character>();
            Character exactMatch = null;
            Character closest = null;
            float closestDist = float.MaxValue;

            foreach (var c in characters)
            {
                if (c == null) continue;

                string charId = OutpostSettlerBinding.GetObjectZdoId(c.gameObject);
                if (!string.IsNullOrEmpty(storedCourierId) && charId == storedCourierId)
                    exactMatch = c;

                string charPostKey = CourierBinding.GetCourierPostKey(c.gameObject);
                if (!string.IsNullOrEmpty(postKey) && charPostKey == postKey)
                {
                    boundCandidates.Add(c);
                    float dist = Vector3.Distance(c.transform.position, spawnPos);
                    if (dist < closestDist)
                    {
                        closest = c;
                        closestDist = dist;
                    }
                }
            }

            Character resolved = exactMatch ?? closest;
            if (resolved == null) return null;

            var obj = resolved.gameObject;
            if (obj.GetComponent<CourierNPC>() == null)
                obj.AddComponent<CourierNPC>();

            CourierBinding.BindCourier(postNview, postTransform, obj);

            if (OutpostSettlerBinding.IsOwner(postNview))
            {
                foreach (var candidate in boundCandidates)
                {
                    if (candidate != null && candidate != resolved)
                        NPCSpawnHelper.Despawn(candidate.gameObject);
                }
            }

            return obj;
        }
    }
}
