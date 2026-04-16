using HarmonyLib;
using Bygd.Framework;

namespace Bygd
{
    /// <summary>
    /// Anchor structures (the elder table and courier post) cannot be destroyed
    /// while they remain active (transferred to an NPC or bound to an outpost).
    /// </summary>
    [HarmonyPatch(typeof(WearNTear), "Damage")]
    internal class Anchor_DamageProtection_Patch
    {
        static bool Prefix(WearNTear __instance)
        {
            return !AnchorProtection.IsProtected(__instance);
        }
    }

    [HarmonyPatch(typeof(WearNTear), "Remove")]
    internal class Anchor_RemoveProtection_Patch
    {
        static bool Prefix(WearNTear __instance)
        {
            if (!AnchorProtection.IsProtected(__instance))
                return true;

            Player.m_localPlayer?.Message(MessageHud.MessageType.Center,
                Localization.instance.Localize("$anchor_return_first"));
            return false;
        }
    }

    internal static class AnchorProtection
    {
        public static bool IsProtected(WearNTear wnt)
        {
            if (wnt == null)
                return false;

            string name = wnt.name;

            // Elder's Table is protected while transferred to an NPC.
            if (name.StartsWith(PrefabNames.OutpostTable))
            {
                var nview = wnt.GetComponent<ZNetView>();
                return nview != null && OutpostTransferState.IsTransferred(nview);
            }

            // Courier Post is protected while it is bound to an outpost.
            if (name.StartsWith(PrefabNames.CourierPost))
            {
                var nview = wnt.GetComponent<ZNetView>();
                if (nview == null)
                    return false;
                string parentTable = CourierBinding.GetParentTable(nview);
                return !string.IsNullOrEmpty(parentTable);
            }

            return false;
        }
    }
}
