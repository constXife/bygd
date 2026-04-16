using HarmonyLib;
using Bygd.Framework;

namespace Bygd
{
    [HarmonyPatch(typeof(Console), "Awake")]
    internal class Console_Awake_Patch
    {
        static void Postfix(ref bool ___m_consoleEnabled)
        {
            ___m_consoleEnabled = true;
            Log.Info("Developer console forcibly unlocked");
        }
    }

    [HarmonyPatch(typeof(Player), "OnSpawned")]
    internal class Player_OnSpawned_Patch
    {
        static void Postfix(Player __instance, ref bool ___m_noPlacementCost)
        {
            if (__instance == Player.m_localPlayer)
            {
                Player.m_debugMode = true;
                ___m_noPlacementCost = true;
                BygdPlugin.DevMode = true;

                if (Console.instance != null)
                    Console.instance.TryRunCommand("devcommands");

                __instance.Message(MessageHud.MessageType.TopLeft, "Developer mode enabled (DevMode on)");
                Log.Info("Admin powers, free building, and DevMode enabled automatically");
            }
        }
    }
}
