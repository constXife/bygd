using Bygd.Framework;
using UnityEngine;

namespace Bygd
{
    internal static class LumberjackManager
    {
        public static Vector3 GetSpawnPosition(Transform t) => NPCSpawnHelper.GetSpawnPosition(t);

        public static GameObject Spawn(ZNetView nview, Transform anchor) =>
            NPCSpawnHelper.SpawnDverger<LumberjackNPC>(
                anchor,
                Localization.instance?.Localize("$lumberjack_name") ?? "Lumberjack");

        public static void Despawn(GameObject npc, ZNetView nview) =>
            NPCSpawnHelper.Despawn(npc);

        public static GameObject RefreshReference(ZNetView nview, Transform anchor) =>
            NPCSpawnHelper.FindNearestNPC<LumberjackNPC>(anchor, 30f);
    }
}
