using HarmonyLib;

namespace Bygd.Framework
{
    internal interface IAISuppressed { }

    /// <summary>
    /// Temporary IAISuppressed marker for animals during CourierWalker.
    /// Removed automatically when the walker is destroyed.
    /// </summary>
    internal class AISuppressionMarker : UnityEngine.MonoBehaviour, IAISuppressed { }

    [HarmonyPatch(typeof(MonsterAI), "UpdateAI")]
    internal class MonsterAI_UpdateAI_Patch
    {
        static bool Prefix(MonsterAI __instance)
        {
            return __instance.GetComponent<IAISuppressed>() == null;
        }
    }
}
