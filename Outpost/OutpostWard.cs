using System.Collections.Generic;
using Bygd.Framework;
using UnityEngine;

namespace Bygd
{
    // Cache of transferred outposts — refreshed every 10 seconds,
    // instead of FindObjectsOfType on every CheckAccess/GetBaseValue
    internal static class OutpostCache
    {
        private static readonly List<OutpostTableComponent> s_transferredTables = new List<OutpostTableComponent>();
        private static float s_lastRefreshTime;
        private const float RefreshInterval = 10f;

        public static List<OutpostTableComponent> GetTransferredTables()
        {
            if (Time.time - s_lastRefreshTime > RefreshInterval)
                Refresh();
            return s_transferredTables;
        }

        public static void Refresh()
        {
            s_transferredTables.Clear();
            foreach (var table in Object.FindObjectsOfType<OutpostTableComponent>())
            {
                if (table == null)
                    continue;

                var nview = table.GetComponent<ZNetView>();
                if (OutpostTransferState.IsTransferred(nview))
                    s_transferredTables.Add(table);
            }
            s_lastRefreshTime = Time.time;
        }

        public static void Invalidate()
        {
            s_lastRefreshTime = 0f;
        }

        public static OutpostTableComponent FindNearestTransferred(Vector3 position, float radius)
        {
            OutpostTableComponent closest = null;
            float closestDist = radius;

            foreach (var table in GetTransferredTables())
            {
                if (table == null) continue;
                float dist = Vector3.Distance(position, table.transform.position);
                if (dist < closestDist)
                {
                    closest = table;
                    closestDist = dist;
                }
            }

            return closest;
        }
    }

    internal static class OutpostWard
    {
        public static void ActivateWard(OutpostTableComponent table, ZNetView nview)
        {
            OutpostCache.Invalidate();
            Log.Info("Ward activated (via CheckAccess patch)");
        }

        public static void DeactivateWard(OutpostTableComponent table)
        {
            OutpostCache.Invalidate();
            Log.Info("Ward deactivated");
        }

        public static void GrantAccess(OutpostTableComponent table, long playerID, string playerName)
        {
            Log.Info($"Access granted to player {playerName} (relation >= {OutpostResources.AccessThreshold})");
        }
    }
}
