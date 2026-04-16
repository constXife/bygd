using HarmonyLib;
using Bygd.Framework;

namespace Bygd
{
    [HarmonyPatch(typeof(Piece), "SetCreator")]
    internal class MailPost_OnPlaced_Patch
    {
        static void Postfix(Piece __instance)
        {
            if (__instance == null || !__instance.name.StartsWith(PrefabNames.MailPost))
                return;

            if (!MailPost_Runtime.IsLiveMailPost(__instance))
                return;

            if (Player.m_localPlayer != null)
                Player.m_localPlayer.Message(MessageHud.MessageType.Center,
                    Localization.instance.Localize("$mailpost_placed"));

            Log.Info($"Mail post placed @ {__instance.transform.position}");
            MailPost_Runtime.EnsureComponent(__instance);
        }
    }

    [HarmonyPatch(typeof(Piece), "Awake")]
    internal class MailPost_RestoreComponent_Patch
    {
        static void Postfix(Piece __instance)
        {
            if (__instance == null || !__instance.name.StartsWith(PrefabNames.MailPost))
                return;

            MailPost_Runtime.EnsureComponent(__instance);
        }
    }
}
