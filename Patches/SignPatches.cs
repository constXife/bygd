using HarmonyLib;
using Bygd.Framework;
using UnityEngine;

namespace Bygd
{
    [HarmonyPatch(typeof(Sign), "SetText")]
    internal class Sign_SetText_Patch
    {
        static void Postfix(Sign __instance, string text)
        {
            if (string.IsNullOrEmpty(text)) return;

            Vector3 position = __instance.transform.position;

            if (text.StartsWith("@"))
            {
                string stationName = text.Substring(1).Trim();
                BygdPlugin.Stations[stationName] = position;

                if (Player.m_localPlayer != null)
                    Player.m_localPlayer.Message(MessageHud.MessageType.Center, $"STATION CONNECTED:\n{stationName}");

                Log.Info($"Station '{stationName}' registered. Coordinates: {position}");
            }

            if (text.StartsWith("#"))
            {
                string waypointName = text.Substring(1).Trim();
                BygdPlugin.Waypoints[waypointName] = position;

                if (Player.m_localPlayer != null)
                    Player.m_localPlayer.Message(MessageHud.MessageType.Center, $"WAYPOINT:\n{waypointName}");

                Log.Info($"Waypoint '{waypointName}' registered. Coordinates: {position}");
            }
        }
    }
}
