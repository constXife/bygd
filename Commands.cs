using System.Collections.Generic;
using System.Text;
using Bygd.Framework;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace Bygd
{
    internal static class Commands
    {
        public static void Register()
        {
            CommandManager.Instance.AddConsoleCommand(new BygdCommand());
        }
    }

    internal class BygdCommand : ConsoleCommand
    {
        public override string Name => "bygd";
        public override string Help => "bygd plan|build|rotate|cancel|setlevel|devmode|debug|list|go|cleanup|reset|patrol|relink|levelup";

        public override List<string> CommandOptionList()
        {
            return new List<string>
            {
                "plan", "build", "rotate", "cancel",
                "setlevel", "devmode", "debug", "list",
                "go", "cleanup", "reset", "patrol", "relink", "levelup"
            };
        }

        // Jotunn Run: args[0] = first argument after "bygd"
        public override void Run(string[] args)
        {
            if (args.Length < 1)
            {
                Print("Usage: bygd plan|build|rotate|cancel|setlevel|devmode|debug|list|go|cleanup");
                return;
            }

            switch (args[0])
            {
                case "list":     HandleList(); return;
                case "reset":    HandleReset(); return;
                case "patrol":   HandlePatrol(); return;
                case "relink":   HandleRelink(); return;
                case "levelup":  HandleLevelup(); return;
                case "setlevel": HandleSetLevel(args); return;
                case "devmode":  HandleDevMode(); return;
                case "debug":    HandleDebug(); return;
                case "cleanup":  HandleCleanup(); return;
                case "go":       HandleGo(args); return;
                case "plan":     HandlePlan(args); return;
                case "build":    HandleBuild(); return;
                case "rotate":   HandleRotate(args); return;
                case "cancel":   HandleCancel(); return;
            }

            Print($"Unknown subcommand: {args[0]}");
        }

        private static void Print(string text)
        {
            Console.instance.Print(text);
        }

        private static void HandleList()
        {
            var sb = new StringBuilder();

            sb.AppendLine("--- Stations (@) ---");
            if (BygdPlugin.Stations.Count == 0)
                sb.AppendLine("  (empty)");
            foreach (var kvp in BygdPlugin.Stations)
                sb.AppendLine($"  {kvp.Key} -> {kvp.Value}");

            sb.AppendLine("--- Waypoints (#) ---");
            if (BygdPlugin.Waypoints.Count == 0)
                sb.AppendLine("  (empty)");
            foreach (var kvp in BygdPlugin.Waypoints)
                sb.AppendLine($"  {kvp.Key} -> {kvp.Value}");

            Print(sb.ToString());
        }

        private static void HandleReset()
        {
            var activePatrols = new List<CourierPatrol>(CourierPatrol.GetActivePatrols());
            int stopped = 0;
            foreach (var patrol in activePatrols)
            {
                if (patrol.CourierObject != null)
                {
                    var walker = patrol.CourierObject.GetComponent<CourierWalker>();
                    if (walker != null)
                        Object.Destroy(walker);
                }
                patrol.FinishPatrol();
                stopped++;
            }

            int boarsRemoved = 0;
            List<Character> chars = Reflect.Character_GetAllCharacters?.Invoke(null, null) as List<Character>;
            if (chars != null)
            {
                foreach (var c in chars)
                {
                    if (c == null) continue;

                    bool isCourierBoar = c.name == PrefabNames.CourierBoar;
                    if (!isCourierBoar)
                    {
                        var nv = c.GetComponent<ZNetView>();
                        if (nv != null && OutpostTransferState.ReadZdoString(nv, "bygd_courier_boar") == "1")
                            isCourierBoar = true;
                    }

                    if (isCourierBoar)
                    {
                        ZNetScene.instance.Destroy(c.gameObject);
                        boarsRemoved++;
                    }
                }
            }

            foreach (var post in Object.FindObjectsOfType<CourierPostComponent>())
                post.ForceRespawnCourier();

            Print($"Reset: stopped {stopped} patrols, removed {boarsRemoved} boars, couriers returned");
        }

        private static void HandlePatrol()
        {
            var posts = Object.FindObjectsOfType<CourierPostComponent>();
            if (posts.Length == 0)
            {
                Print("No courier posts found");
                return;
            }

            var mailPosts = MailPostComponent.GetAllMailPosts();
            Print($"Courier posts: {posts.Length}, mail posts: {mailPosts.Count}");

            foreach (var mailPost in mailPosts)
            {
                string key = mailPost.GetStationKey() ?? "(unnamed)";
                bool hasMail = mailPost.HasPendingMail();
                Print($"  MailPost @{key}: {(hasMail ? "has package" : "empty")}");
            }

            foreach (var post in posts)
            {
                var postNview = post.GetComponent<ZNetView>();
                int budget = CourierBinding.GetBudget(postNview);
                Print($"  Post: budget={budget} calories");
                // ForceStartPatrol accepts Terminal — using Console.instance
                post.ForceStartPatrol(Console.instance);
            }
        }

        private static void HandleRelink()
        {
            int cleared = 0;
            foreach (var container in Object.FindObjectsOfType<Container>())
            {
                if (container == null) continue;
                var cnv = container.GetComponent<ZNetView>();
                if (cnv == null) continue;
                string marker = OutpostTransferState.ReadZdoString(cnv, "bygd_chest_table_id");
                if (!string.IsNullOrEmpty(marker))
                {
                    OutpostTransferState.WriteZdoString(cnv, "bygd_chest_table_id", "");
                    cleared++;
                }
            }

            int linked = 0;
            foreach (var table in Object.FindObjectsOfType<OutpostTableComponent>())
            {
                var nv = table.GetComponent<ZNetView>();
                if (OutpostTransferState.IsTransferred(nv))
                {
                    var chest = OutpostChestLink.FindAndLinkChest(table, nv);
                    if (chest != null) linked++;
                }
            }

            Print($"Relink: cleared {cleared} markers, linked {linked} chests");
        }

        private static void HandleLevelup()
        {
            var tables = Object.FindObjectsOfType<OutpostTableComponent>();
            foreach (var table in tables)
            {
                var nv = table.GetComponent<ZNetView>();
                if (!OutpostTransferState.IsTransferred(nv))
                    continue;

                int level = OutpostResources.GetLevel(nv);
                int wood = OutpostResources.GetWood(nv);
                int cal = OutpostResources.GetCalories(nv);
                int comfort = OutpostComfort.GetComfortAtPoint(table.transform.position, true);
                int reqComfort = OutpostComfort.GetRequiredComfort(level + 1);

                Print($"Level={level}, comfort={comfort}/{reqComfort}, wood={wood}/6, cal={cal}/240");

                if (comfort >= reqComfort && wood >= OutpostResources.LevelUpWoodThreshold && cal >= OutpostResources.LevelUpCaloriesThreshold)
                {
                    long pid = Player.m_localPlayer != null ? Player.m_localPlayer.GetPlayerID() : 0;
                    if (level == 1 && (pid == 0 || OutpostResources.GetRelation(nv, pid) < OutpostResources.AccessThreshold))
                    {
                        Print("Player trust required (relation >= 30)");
                        continue;
                    }
                    OutpostResources.SetLevel(nv, level + 1);
                    Print($"Level up: {level} → {level + 1}");
                }
                else
                {
                    Print("Conditions not met");
                }
            }
        }

        private static void HandleDebug()
        {
            var sb = new StringBuilder();
            Vector3 playerPos = Player.m_localPlayer != null ? Player.m_localPlayer.transform.position : Vector3.zero;

            int comfort = OutpostComfort.GetComfortAtPoint(playerPos, true);
            sb.AppendLine($"--- Comfort at player position: {comfort} ---");
            sb.AppendLine($"  Roof: {RoofCheck.HasRoofAbove(playerPos)}");

            var tables = Object.FindObjectsOfType<OutpostTableComponent>();
            sb.AppendLine($"--- Outposts: {tables.Length} ---");
            foreach (var table in tables)
            {
                var nv = table.GetComponent<ZNetView>();
                bool transferred = OutpostTransferState.IsTransferred(nv);
                int level = OutpostResources.GetLevel(nv);
                int wood = OutpostResources.GetWood(nv);
                int cal = OutpostResources.GetCalories(nv);
                int resin = OutpostResources.GetResin(nv);
                int tableComfort = OutpostComfort.GetComfortAtPoint(table.transform.position, true);
                string tableId = OutpostSettlerBinding.GetObjectZdoId(table.gameObject);
                sb.AppendLine($"  pos={table.transform.position:F0} transferred={transferred} level={level} comfort={tableComfort} wood={wood} cal={cal} resin={resin}");
                sb.AppendLine($"  tableId={tableId}");

                int chestCount = 0;
                bool linkedChestFound = false;
                foreach (var c in Object.FindObjectsOfType<Container>())
                {
                    if (c == null || c.gameObject == table.gameObject) continue;
                    float dist = Vector3.Distance(c.transform.position, table.transform.position);
                    if (dist > 20f) continue;
                    chestCount++;
                    var cnv = c.GetComponent<ZNetView>();
                    string marker = cnv != null ? OutpostTransferState.ReadZdoString(cnv, "bygd_chest_table_id") : "";
                    bool isLinked = marker == tableId && !string.IsNullOrEmpty(tableId);
                    int items = c.GetInventory() != null ? c.GetInventory().NrOfItems() : 0;
                    sb.AppendLine($"    chest dist={dist:F1}m linked={isLinked} items={items} marker=\"{marker}\"");
                    if (isLinked) linkedChestFound = true;
                }
                if (chestCount == 0)
                    sb.AppendLine("    (no chests within 20m)");
                else if (!linkedChestFound)
                    sb.AppendLine($"    ! {chestCount} chests nearby, but none linked");
            }

            var settlers = Object.FindObjectsOfType<SettlerNPC>();
            var couriers = Object.FindObjectsOfType<CourierNPC>();
            sb.AppendLine($"--- NPC: settlers={settlers.Length}, couriers={couriers.Length} ---");

            List<Character> characters = Reflect.Character_GetAllCharacters?.Invoke(null, null) as List<Character>;
            int dvergerCount = 0;
            if (characters != null)
            {
                foreach (var c in characters)
                {
                    if (c != null && c.name.Contains("Dverger"))
                        dvergerCount++;
                }
            }
            sb.AppendLine($"  Dvergers in world: {dvergerCount}");

            Print(sb.ToString());
        }

        private static void HandleSetLevel(string[] args)
        {
            if (args.Length < 2 || !int.TryParse(args[1], out int targetLevel))
            {
                Print("Usage: bygd setlevel <level>");
                return;
            }

            var tables = Object.FindObjectsOfType<OutpostTableComponent>();
            if (tables.Length == 0)
            {
                Print("No outposts found");
                return;
            }

            Vector3 playerPos = Player.m_localPlayer != null
                ? Player.m_localPlayer.transform.position
                : Vector3.zero;

            OutpostTableComponent closest = null;
            float closestDist = float.MaxValue;

            foreach (var table in tables)
            {
                float dist = Vector3.Distance(table.transform.position, playerPos);
                if (dist < closestDist)
                {
                    closest = table;
                    closestDist = dist;
                }
            }

            var nv = closest.GetComponent<ZNetView>();

            if (!OutpostTransferState.IsTransferred(nv))
            {
                OutpostTransferState.SetTransferred(nv, true);
                long pid = Player.m_localPlayer != null ? Player.m_localPlayer.GetPlayerID() : 0;
                if (pid != 0)
                    OutpostResources.AddRelation(nv, pid, OutpostResources.AccessThreshold);
                Print("Outpost automatically transferred to NPC");
            }

            int oldLevel = OutpostResources.GetLevel(nv);
            OutpostResources.SetLevel(nv, targetLevel);
            Print($"Level: {oldLevel} → {targetLevel} (dist={closestDist:F0}m)");
        }

        private static void HandleDevMode()
        {
            BygdPlugin.DevMode = !BygdPlugin.DevMode;
            string state = BygdPlugin.DevMode ? "ON" : "OFF";
            Print($"DevMode: {state} (settlers don't leave, degradation disabled)");
        }

        private static void HandlePlan(string[] args)
        {
            var table = FindNearestTable();
            if (table == null)
            {
                Print("No outposts nearby");
                return;
            }

            var builder = table.GetComponent<BlueprintBuilder>();
            if (builder != null && builder.IsBuilding)
            {
                Print($"Building already in progress: {(int)(builder.Progress * 100)}%");
                return;
            }

            BlueprintData blueprint;
            if (args.Length >= 2)
            {
                blueprint = BlueprintRegistry.Get(args[1]);
                if (blueprint == null)
                {
                    var keys = BlueprintRegistry.GetAvailableKeys();
                    Print($"Blueprint '{args[1]}' not found. Available: {string.Join(", ", keys)}");
                    return;
                }
            }
            else
            {
                var nv = table.GetComponent<ZNetView>();
                int level = OutpostResources.GetLevel(nv);
                blueprint = BlueprintRegistry.GetForLevel(level);
                if (blueprint == null)
                {
                    var keys = BlueprintRegistry.GetAvailableKeys();
                    Print($"No blueprint for level {level}. Available: {string.Join(", ", keys)}");
                    return;
                }
            }

            BlueprintGhost.Show(table, blueprint);
            Print($"Plan '{blueprint.Name}' ({blueprint.Pieces.Count} pieces). bygd rotate / bygd build / bygd cancel");
        }

        private static void HandleBuild()
        {
            var ghost = BlueprintGhost.GetActive();
            if (ghost == null)
            {
                Print("No active plan. First run: bygd plan");
                return;
            }

            var table = ghost.GetComponent<OutpostTableComponent>();
            var nv = table.GetComponent<ZNetView>();
            int wood = OutpostResources.GetWood(nv);
            int needed = ghost.Blueprint.Pieces.Count;

            Print($"Starting construction: {needed} pieces, wood in outpost: {wood}");
            BlueprintBuilder.Start(table, ghost);
        }

        private static void HandleRotate(string[] args)
        {
            var ghost = BlueprintGhost.GetActive();
            if (ghost == null)
            {
                Print("No active plan");
                return;
            }

            float degrees = 90f;
            if (args.Length >= 2)
                float.TryParse(args[1], out degrees);

            ghost.Rotate(degrees);
            Print($"Plan rotated by {degrees}° (total: {ghost.Rotation}°)");
        }

        private static void HandleCancel()
        {
            var ghost = BlueprintGhost.GetActive();
            if (ghost == null)
            {
                Print("No active plan");
                return;
            }

            BlueprintGhost.Cancel();
            Print("Plan cancelled");
        }

        private static OutpostTableComponent FindNearestTable()
        {
            if (Player.m_localPlayer == null)
                return null;

            Vector3 playerPos = Player.m_localPlayer.transform.position;
            OutpostTableComponent closest = null;
            float closestDist = 50f;

            foreach (var table in Object.FindObjectsOfType<OutpostTableComponent>())
            {
                float dist = Vector3.Distance(table.transform.position, playerPos);
                if (dist < closestDist)
                {
                    closest = table;
                    closestDist = dist;
                }
            }

            return closest;
        }

        private static void HandleCleanup()
        {
            string report = OutpostTable_Runtime.CleanupLoadedSettlers();
            Print(report);
        }

        private static void HandleGo(string[] args)
        {
            if (args.Length < 3)
            {
                Print("Usage: bygd go <from> <to> [creature]");
                return;
            }

            string from = args[1];
            string to = args[2];
            string creature = args.Length >= 4 ? args[3] : PrefabNames.Lox;

            CartHorse.StartTrip(from, to, creature);
            Print($"Sending {creature}: {from} -> {to}");
        }
    }
}
