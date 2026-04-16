using HarmonyLib;
using Bygd.Framework;
using UnityEngine;

namespace Bygd
{
    [HarmonyPatch(typeof(Piece), "SetCreator")]
    internal class OutpostTable_OnPlaced_Patch
    {
        static void Postfix(Piece __instance)
        {
            if (__instance == null || !__instance.name.StartsWith(PrefabNames.OutpostTable))
                return;

            if (!OutpostTable_Runtime.IsLiveOutpostPiece(__instance))
                return;

            Vector3 pos = __instance.transform.position;
            string outpostName = $"Outpost_{pos.x:F0}_{pos.z:F0}";
            OutpostRegistry.Outposts[outpostName] = pos;

            if (Player.m_localPlayer != null)
                Player.m_localPlayer.Message(MessageHud.MessageType.Center,
                    $"Outpost founded!\n{outpostName}");

            Log.Info($"Registered: '{outpostName}' @ {pos}");

            OutpostTable_Runtime.EnsureComponent(__instance);
        }
    }

    [HarmonyPatch(typeof(Piece), "Awake")]
    internal class OutpostTable_RestoreComponent_Patch
    {
        static void Postfix(Piece __instance)
        {
            if (__instance == null || !__instance.name.StartsWith(PrefabNames.OutpostTable))
                return;

            OutpostTable_Runtime.EnsureComponent(__instance);
        }
    }
}
