using HarmonyLib;
using Bygd.Framework;
using UnityEngine;

namespace Bygd
{
    [HarmonyPatch(typeof(Piece), "SetCreator")]
    internal class CourierPost_OnPlaced_Patch
    {
        static void Postfix(Piece __instance)
        {
            if (__instance == null || !__instance.name.StartsWith(PrefabNames.CourierPost))
                return;

            if (!CourierPost_Runtime.IsLiveCourierPost(__instance))
                return;

            // Bind to the nearest transferred outpost (if any)
            var table = FindNearestTransferredTable(__instance.transform.position);
            if (table != null)
            {
                var postNview = __instance.GetComponent<ZNetView>();
                var tableNview = table.GetComponent<ZNetView>();
                string tableKey = OutpostSettlerBinding.GetTableKey(tableNview, table.transform);
                CourierBinding.SetParentTable(postNview, tableKey);
            }

            if (Player.m_localPlayer != null)
                Player.m_localPlayer.Message(MessageHud.MessageType.Center,
                    Localization.instance.Localize("$courier_post_placed"));

            Log.Info($"Courier post placed @ {__instance.transform.position}");
            CourierPost_Runtime.EnsureComponent(__instance);
        }

        private static OutpostTableComponent FindNearestTransferredTable(Vector3 pos)
        {
            OutpostTableComponent closest = null;
            float closestDist = 20f;

            foreach (var table in OutpostCache.GetTransferredTables())
            {
                if (table == null)
                    continue;

                float dist = Vector3.Distance(pos, table.transform.position);
                if (dist < closestDist)
                {
                    closest = table;
                    closestDist = dist;
                }
            }

            return closest;
        }
    }

    [HarmonyPatch(typeof(Piece), "Awake")]
    internal class CourierPost_RestoreComponent_Patch
    {
        static void Postfix(Piece __instance)
        {
            if (__instance == null || !__instance.name.StartsWith(PrefabNames.CourierPost))
                return;

            CourierPost_Runtime.EnsureComponent(__instance);
        }
    }
}
