using HarmonyLib;
using Bygd.Framework;

namespace Bygd
{
    [HarmonyPatch(typeof(Piece), "SetCreator")]
    internal class LumberjackPost_OnPlaced_Patch
    {
        static void Postfix(Piece __instance)
        {
            if (__instance == null || !__instance.name.StartsWith(PrefabNames.LumberjackPost))
                return;

            if (!LumberjackPost_Runtime.IsLivePost(__instance))
                return;

            if (Player.m_localPlayer != null)
                Player.m_localPlayer.Message(MessageHud.MessageType.Center,
                    Localization.instance.Localize("$lumberjack_post_placed"));

            Log.Info($"Lumberjack post placed @ {__instance.transform.position}");
            LumberjackPost_Runtime.EnsureComponent(__instance);
        }
    }

    [HarmonyPatch(typeof(Piece), "Awake")]
    internal class LumberjackPost_RestoreComponent_Patch
    {
        static void Postfix(Piece __instance)
        {
            if (__instance == null || !__instance.name.StartsWith(PrefabNames.LumberjackPost))
                return;

            LumberjackPost_Runtime.EnsureComponent(__instance);
        }
    }
}
