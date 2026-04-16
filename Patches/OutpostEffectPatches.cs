using HarmonyLib;
using UnityEngine;

namespace Bygd
{
    // If the player is in a transferred outpost zone,
    // GetBaseValue returns 0 -> raids won't trigger.
    [HarmonyPatch(typeof(EffectArea), "GetBaseValue")]
    internal class OutpostTransfer_SuppressRaid_Patch
    {
        private const float OutpostRadius = 40f;

        static void Postfix(Vector3 p, ref int __result)
        {
            if (__result < 3)
                return;

            if (IsNearTransferredOutpost(p))
                __result = 0;
        }

        private static bool IsNearTransferredOutpost(Vector3 point)
        {
            foreach (var table in OutpostCache.GetTransferredTables())
            {
                if (table == null)
                    continue;

                float dist = Vector3.Distance(point, table.transform.position);
                if (dist <= OutpostRadius)
                    return true;
            }

            return false;
        }
    }

    // Ward: blocks access in a transferred outpost zone
    // if the player's relation is below the threshold.
    [HarmonyPatch(typeof(PrivateArea), "CheckAccess")]
    internal class OutpostWard_CheckAccess_Patch
    {
        private const float WardRadius = 20f;

        static void Postfix(Vector3 point, ref bool __result)
        {
            if (!__result)
                return;

            foreach (var table in OutpostCache.GetTransferredTables())
            {
                if (table == null)
                    continue;

                float dist = Vector3.Distance(point, table.transform.position);
                if (dist > WardRadius)
                    continue;

                var tableNview = table.GetComponent<ZNetView>();
                long playerID = Player.m_localPlayer != null ? Player.m_localPlayer.GetPlayerID() : 0;

                if (playerID != 0 && OutpostResources.GetRelation(tableNview, playerID) >= OutpostResources.AccessThreshold)
                    return;

                __result = false;
                return;
            }
        }
    }
}
