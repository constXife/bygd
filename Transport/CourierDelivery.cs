using System.Collections.Generic;
using UnityEngine;

namespace Bygd
{
    /// <summary>
    /// Utilities for determining player proximity to a route.
    /// Used by CourierPatrol for switching between visual/simulated mode.
    /// </summary>
    internal static class CourierDelivery
    {
        private const float PlayerProximityRadius = 64f;

        public static bool IsPlayerNearRoute(List<Vector3> route)
        {
            if (Player.m_localPlayer == null)
                return false;

            Vector3 playerPos = Player.m_localPlayer.transform.position;
            foreach (Vector3 point in route)
            {
                if (Vector3.Distance(playerPos, point) < PlayerProximityRadius)
                    return true;
            }
            return false;
        }

        public static bool IsPlayerNearPoint(Vector3 point)
        {
            if (Player.m_localPlayer == null)
                return false;

            return Vector3.Distance(Player.m_localPlayer.transform.position, point) < PlayerProximityRadius;
        }
    }
}
