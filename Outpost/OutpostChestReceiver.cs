using HarmonyLib;
using Bygd.Framework;
using UnityEngine;

namespace Bygd
{
    internal static class OutpostChestLink
    {
        private const string ChestMarkerKey = "bygd_chest_table_id";
        private const float SearchRadius = 20f;

        public static Container FindAndLinkChest(OutpostTableComponent table, ZNetView tableNview)
        {
            string tableId = OutpostSettlerBinding.GetObjectZdoId(table.gameObject);
            if (string.IsNullOrEmpty(tableId))
                return null;

            Container closest = null;
            float closestDist = float.MaxValue;

            foreach (var container in Object.FindObjectsOfType<Container>())
            {
                if (container == null)
                    continue;

                float dist = Vector3.Distance(container.transform.position, table.transform.position);
                if (dist > SearchRadius || dist >= closestDist)
                    continue;

                // Don't link the table to itself
                if (container.gameObject == table.gameObject)
                    continue;

                closest = container;
                closestDist = dist;
            }

            if (closest == null)
                return null;

            // Mark the chest — write table ID to its ZDO
            var chestNview = closest.GetComponent<ZNetView>();
            OutpostTransferState.WriteZdoString(chestNview, ChestMarkerKey, tableId);

            // Make chest public (accessible without Ward)
            closest.m_privacy = Container.PrivacySetting.Public;
            closest.m_checkGuardStone = false;

            Log.Info($"Receiver chest linked to outpost, dist={closestDist:F1}m");
            return closest;
        }

        public static void UnlinkChest(OutpostTableComponent table)
        {
            string tableId = OutpostSettlerBinding.GetObjectZdoId(table.gameObject);
            if (string.IsNullOrEmpty(tableId))
                return;

            foreach (var container in Object.FindObjectsOfType<Container>())
            {
                if (container == null)
                    continue;

                var chestNview = container.GetComponent<ZNetView>();
                string linkedId = OutpostTransferState.ReadZdoString(chestNview, ChestMarkerKey);
                if (linkedId == tableId)
                {
                    OutpostTransferState.WriteZdoString(chestNview, ChestMarkerKey, "");
                    container.m_checkGuardStone = true;
                    container.m_privacy = Container.PrivacySetting.Private;
                    Log.Info("Receiver chest unlinked from outpost");
                }
            }
        }

        public static OutpostTableComponent FindLinkedTable(Container chest)
        {
            var chestNview = chest.GetComponent<ZNetView>();
            string tableId = OutpostTransferState.ReadZdoString(chestNview, ChestMarkerKey);
            if (string.IsNullOrEmpty(tableId))
                return null;

            foreach (var table in Object.FindObjectsOfType<OutpostTableComponent>())
            {
                if (table == null)
                    continue;

                string id = OutpostSettlerBinding.GetObjectZdoId(table.gameObject);
                if (id == tableId)
                    return table;
            }

            return null;
        }

        public static void EnsureLinked(OutpostTableComponent table, ZNetView tableNview)
        {
            string tableId = OutpostSettlerBinding.GetObjectZdoId(table.gameObject);
            if (string.IsNullOrEmpty(tableId))
                return;

            // Check if there is already a linked chest
            foreach (var container in Object.FindObjectsOfType<Container>())
            {
                if (container == null || container.gameObject == table.gameObject)
                    continue;

                if (!OutpostChestCollector.IsValidChest(container))
                    continue;

                var chestNview = container.GetComponent<ZNetView>();
                string linkedId = OutpostTransferState.ReadZdoString(chestNview, ChestMarkerKey);
                if (linkedId == tableId)
                    return; // already linked
            }

            // No linked chest found — link the nearest one
            FindAndLinkChest(table, tableNview);
        }

        public static OutpostTableComponent FindNearestTransferredTable(Vector3 position)
        {
            OutpostTableComponent closest = null;
            float closestDist = SearchRadius;

            foreach (var table in Object.FindObjectsOfType<OutpostTableComponent>())
            {
                if (table == null)
                    continue;

                float dist = Vector3.Distance(position, table.transform.position);
                if (dist >= closestDist)
                    continue;

                var nview = table.GetComponent<ZNetView>();
                if (!OutpostTransferState.IsTransferred(nview))
                    continue;

                closest = table;
                closestDist = dist;
            }

            return closest;
        }
    }

    internal static class OutpostChestCollector
    {
        public static bool IsValidChest(Container container)
        {
            if (container == null)
                return false;

            if (!container.gameObject.activeInHierarchy)
                return false;

            var nview = container.GetComponent<ZNetView>();
            if (nview == null || !nview.IsValid())
                return false;

            // Ghost after destruction has no WearNTear (durability)
            if (container.GetComponent<WearNTear>() == null)
                return false;

            return true;
        }

        public static Container FindLinkedChestFor(OutpostTableComponent table)
        {
            string tableId = OutpostSettlerBinding.GetObjectZdoId(table.gameObject);
            if (string.IsNullOrEmpty(tableId))
                return null;

            foreach (var container in Object.FindObjectsOfType<Container>())
            {
                if (container == null || container.gameObject == table.gameObject)
                    continue;

                if (!IsValidChest(container))
                    continue;

                var chestNview = container.GetComponent<ZNetView>();
                string marker = OutpostTransferState.ReadZdoString(chestNview, "bygd_chest_table_id");
                if (marker == tableId)
                    return container;
            }

            return null;
        }

        public static void TryCollect(OutpostTableComponent table, ZNetView tableNview)
        {
            string tableId = OutpostSettlerBinding.GetObjectZdoId(table.gameObject);
            if (string.IsNullOrEmpty(tableId))
                return;

            // Collect from all chests within 20m radius:
            // 1. Linked chests (bygd_chest_table_id)
            // 2. MailPost chests nearby (courier delivered)
            foreach (var container in Object.FindObjectsOfType<Container>())
            {
                if (container == null || container.gameObject == table.gameObject)
                    continue;

                if (!IsValidChest(container))
                    continue;

                float dist = Vector3.Distance(container.transform.position, table.transform.position);
                if (dist > 20f)
                    continue;

                // Linked offering chest?
                var chestNview = container.GetComponent<ZNetView>();
                string marker = OutpostTransferState.ReadZdoString(chestNview, "bygd_chest_table_id");
                bool isLinkedChest = marker == tableId;

                // MailPost chest nearby?
                bool isMailPostChest = false;
                foreach (var mailPost in MailPostComponent.GetAllMailPosts())
                {
                    if (mailPost == null) continue;
                    var mailChest = mailPost.FindLinkedChest();
                    if (mailChest == container)
                    {
                        isMailPostChest = true;
                        break;
                    }
                }

                if (!isLinkedChest && !isMailPostChest)
                    continue;

                var inventory = container.GetInventory();
                if (inventory == null || inventory.NrOfItems() == 0)
                    continue;

                long playerID = Player.m_localPlayer != null ? Player.m_localPlayer.GetPlayerID() : 0;
                int relationBefore = playerID != 0 ? OutpostResources.GetRelation(tableNview, playerID) : 0;

                int relationGained = OutpostResources.CollectFromChest(container, tableNview, playerID);
                // Food/wood visualization disabled

                if (playerID != 0 && relationGained > 0)
                {
                    int relationAfter = OutpostResources.GetRelation(tableNview, playerID);

                    if (relationBefore < OutpostResources.AccessThreshold
                        && relationAfter >= OutpostResources.AccessThreshold
                        && Player.m_localPlayer != null)
                    {
                        Player.m_localPlayer.Message(MessageHud.MessageType.Center,
                            Localization.instance.Localize("$outpost_access_granted"));
                    }
                }
            }
        }
    }
}
