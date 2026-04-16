using System.Collections.Generic;
using System.Text;
using Bygd.Framework;
using UnityEngine;

namespace Bygd
{
    public static class OutpostRegistry
    {
        public static Dictionary<string, Vector3> Outposts = new Dictionary<string, Vector3>();
    }

    public static class RoofCheck
    {
        public static bool HasRoofAbove(Vector3 pos)
        {
            if (!Physics.Raycast(pos + Vector3.up * 0.3f, Vector3.up, out RaycastHit hit, 30f))
                return false;
            return hit.collider.GetComponentInParent<WearNTear>() != null;
        }
    }

    internal static class OutpostTable_Runtime
    {
        public static void EnsureComponent(Piece piece) =>
            PostRuntime.EnsureComponent<OutpostTableComponent>(piece, PrefabNames.OutpostTable);

        public static bool IsLiveOutpostPiece(Piece piece) =>
            PostRuntime.IsLivePost(piece, PrefabNames.OutpostTable);

        public static string CleanupLoadedSettlers()
        {
            var tables = Object.FindObjectsOfType<OutpostTableComponent>();
            var tableByKey = new Dictionary<string, OutpostTableComponent>();

            foreach (var table in tables)
            {
                var nview = table.GetComponent<ZNetView>();
                string key = OutpostSettlerBinding.GetTableKey(nview, table.transform);
                if (string.IsNullOrEmpty(key) && OutpostSettlerBinding.IsOwner(nview))
                    key = OutpostSettlerBinding.EnsureTableKey(nview, table.transform);

                if (!string.IsNullOrEmpty(key))
                    tableByKey[key] = table;
            }

            List<Character> characters = Reflect.Character_GetAllCharacters?.Invoke(null, null) as List<Character>;
            if (characters == null)
                return "Cleanup not performed: Character.GetAllCharacters() unavailable";

            var settlersByKey = new Dictionary<string, List<Character>>();
            foreach (Character character in characters)
            {
                if (character == null)
                    continue;

                string key = OutpostSettlerBinding.GetSettlerTableKey(character.gameObject);
                if (string.IsNullOrEmpty(key))
                    continue;

                if (!settlersByKey.TryGetValue(key, out List<Character> list))
                {
                    list = new List<Character>();
                    settlersByKey[key] = list;
                }

                list.Add(character);
            }

            int removedOrphans = 0;
            int removedDuplicates = 0;
            int kept = 0;

            foreach (var kvp in settlersByKey)
            {
                if (!tableByKey.TryGetValue(kvp.Key, out OutpostTableComponent table))
                {
                    foreach (Character orphan in kvp.Value)
                    {
                        if (orphan == null)
                            continue;

                        ZNetScene.instance.Destroy(orphan.gameObject);
                        removedOrphans++;
                    }

                    continue;
                }

                Character keep = null;
                float closestDist = float.MaxValue;
                Vector3 anchor = table.GetSettlerAnchorPosition();

                foreach (Character candidate in kvp.Value)
                {
                    if (candidate == null)
                        continue;

                    float dist = Vector3.Distance(candidate.transform.position, anchor);
                    if (dist < closestDist)
                    {
                        keep = candidate;
                        closestDist = dist;
                    }
                }

                if (keep == null)
                    continue;

                kept++;
                OutpostSettlerBinding.BindSettler(table.GetComponent<ZNetView>(), table.transform, keep.gameObject);

                foreach (Character candidate in kvp.Value)
                {
                    if (candidate == null || candidate == keep)
                        continue;

                    ZNetScene.instance.Destroy(candidate.gameObject);
                    removedDuplicates++;
                }
            }

            // Remove stray Dvergers near tables (without ZDO binding)
            int removedStray = 0;
            var keptIds = new HashSet<string>();
            foreach (var kvp in settlersByKey)
            {
                foreach (var c in kvp.Value)
                {
                    if (c != null)
                        keptIds.Add(OutpostSettlerBinding.GetObjectZdoId(c.gameObject));
                }
            }

            foreach (Character character in characters)
            {
                if (character == null)
                    continue;

                if (!character.name.Contains("Dverger") && !character.name.Contains("OutpostElder") && !character.name.Contains("OutpostCourier"))
                    continue;

                string charId = OutpostSettlerBinding.GetObjectZdoId(character.gameObject);
                if (keptIds.Contains(charId))
                    continue;

                bool nearTable = false;
                foreach (var table in tables)
                {
                    if (Vector3.Distance(character.transform.position, table.transform.position) < 30f)
                    {
                        nearTable = true;
                        break;
                    }
                }

                if (nearTable)
                {
                    ZNetScene.instance.Destroy(character.gameObject);
                    removedStray++;
                }
            }

            // Clean up ghost chests
            int removedGhostChests = 0;
            foreach (var container in Object.FindObjectsOfType<Container>())
            {
                if (container == null)
                    continue;

                var chestNview = container.GetComponent<ZNetView>();
                string marker = chestNview != null
                    ? OutpostTransferState.ReadZdoString(chestNview, "bygd_chest_table_id")
                    : "";

                if (string.IsNullOrEmpty(marker))
                    continue;

                if (!OutpostChestCollector.IsValidChest(container))
                {
                    ZNetScene.instance.Destroy(container.gameObject);
                    removedGhostChests++;
                }
            }

            // Remove visual decor
            int removedDecor = 0;
            foreach (var t in Object.FindObjectsOfType<Transform>())
            {
                if (t != null && (t.gameObject.name == "bygd_decor" || t.gameObject.name == "bygd_wood_decor"))
                {
                    Object.Destroy(t.gameObject);
                    removedDecor++;
                }
            }

            var sb = new StringBuilder();
            sb.Append("Cleanup: ");
            sb.Append($"settlers kept {kept}, dupes {removedDuplicates}, orphans {removedOrphans}, stray {removedStray}, ghost chests {removedGhostChests}, decor {removedDecor}");
            return sb.ToString();
        }
    }
}
