using System.Collections.Generic;
using Bygd.Framework;
using UnityEngine;

namespace Bygd
{
    internal static class OutpostResources
    {
        private const string WoodCountKey = "bygd_res_wood";
        private const string CaloriesCountKey = "bygd_res_calories";
        private const string ResinCountKey = "bygd_res_resin";
        private const string RelationPrefix = "bygd_rel_";
        private const string LastConsumeTimeKey = "bygd_last_consume";
        private const string StarvingStartKey = "bygd_starving_start";
        private const string OutpostLevelKey = "bygd_outpost_level";

        public const int AccessThreshold = 30;
        public const float ConsumeIntervalSeconds = 1200f; // 20 minutes real time
        public const float DegradeAfterSeconds = 2400f;    // 40 minutes without resources -> level -1
        public const float AbandonAfterSeconds = 7200f;    // 2 hours without resources -> elder leaves
        public const int CaloriesPerCycle = 40;             // ~1 average food item per consumption cycle
        // Level-up threshold: supply for 2 days for 2 inhabitants
        // 2 Valheim-days x 1.5 cycles/day x 2 people = 6 cycles x 40 calories = 240
        public const int LevelUpCaloriesThreshold = 240;
        public const int LevelUpWoodThreshold = 6;

        public static int GetWood(ZNetView nview)
        {
            return ReadInt(nview, WoodCountKey);
        }

        public static void SetWood(ZNetView nview, int value)
        {
            WriteInt(nview, WoodCountKey, value);
        }

        public static int GetCalories(ZNetView nview)
        {
            return ReadInt(nview, CaloriesCountKey);
        }

        public static void SetCalories(ZNetView nview, int value)
        {
            WriteInt(nview, CaloriesCountKey, value);
        }

        public static int GetResin(ZNetView nview)
        {
            return ReadInt(nview, ResinCountKey);
        }

        public static void SetResin(ZNetView nview, int value)
        {
            WriteInt(nview, ResinCountKey, value);
        }

        public static int GetRelation(ZNetView nview, long playerID)
        {
            return ReadInt(nview, RelationPrefix + playerID);
        }

        public static void AddRelation(ZNetView nview, long playerID, int amount)
        {
            int current = GetRelation(nview, playerID);
            WriteInt(nview, RelationPrefix + playerID, current + amount);
        }

        public static int CollectFromChest(Container chest, ZNetView tableNview, long playerID)
        {
            var inventory = chest.GetInventory();
            List<ItemDrop.ItemData> items = inventory.GetAllItems();

            int woodCollected = 0;
            int caloriesCollected = 0;
            int resinCollected = 0;

            foreach (var item in items)
            {
                int stack = item.m_stack;

                if (IsWood(item))
                    woodCollected += stack;
                else if (IsResin(item))
                    resinCollected += stack;
                else if (IsFood(item))
                    caloriesCollected += (int)(item.m_shared.m_food * stack);
            }

            if (woodCollected == 0 && caloriesCollected == 0 && resinCollected == 0)
            {
                foreach (var item in items)
                    Log.Diag($"CollectFromChest: unrecognized item '{item.m_shared.m_name}' x{item.m_stack}, m_food={item.m_shared.m_food}");
                return 0;
            }

            int currentWood = GetWood(tableNview);
            int currentCalories = GetCalories(tableNview);
            int currentResin = GetResin(tableNview);
            WriteInt(tableNview, WoodCountKey, currentWood + woodCollected);
            WriteInt(tableNview, CaloriesCountKey, currentCalories + caloriesCollected);
            WriteInt(tableNview, ResinCountKey, currentResin + resinCollected);

            int relationGain = woodCollected + resinCollected + caloriesCollected / CaloriesPerCycle;
            AddRelation(tableNview, playerID, relationGain);

            inventory.RemoveAll();

            if (Player.m_localPlayer != null)
            {
                string msg = string.Format(
                    Localization.instance.Localize("$outpost_resources_collected"),
                    woodCollected, caloriesCollected, resinCollected);
                Player.m_localPlayer.Message(MessageHud.MessageType.Center, msg);
            }

            Log.Info($"Resources collected: wood={woodCollected}, calories={caloriesCollected}, resin={resinCollected}, relation +{relationGain}");
            return relationGain;
        }

        // --- Outpost level ---

        public static int GetLevel(ZNetView nview)
        {
            return ReadInt(nview, OutpostLevelKey);
        }

        public static void SetLevel(ZNetView nview, int level)
        {
            WriteInt(nview, OutpostLevelKey, level);
        }

        // --- Consumption and degradation ---

        public enum ConsumeResult
        {
            NotYet,      // not yet time to consume
            Consumed,    // resources spent
            LeveledUp,   // level increased
            Starving,    // no resources, outpost is starving
            Degraded,    // level dropped
            Abandoned    // elder left
        }

        public static ConsumeResult TryConsume(ZNetView nview, Vector3 tablePosition)
        {
            double now = ZNet.instance.GetTimeSeconds();
            double lastConsume = ReadDouble(nview, LastConsumeTimeKey);

            if (lastConsume <= 0)
            {
                WriteDouble(nview, LastConsumeTimeKey, now);
                return ConsumeResult.NotYet;
            }

            if (now - lastConsume < ConsumeIntervalSeconds)
                return ConsumeResult.NotYet;

            int wood = GetWood(nview);
            int calories = GetCalories(nview);

            if (wood > 0 && calories >= CaloriesPerCycle)
            {
                WriteInt(nview, WoodCountKey, wood - 1);
                WriteInt(nview, CaloriesCountKey, calories - CaloriesPerCycle);
                WriteDouble(nview, LastConsumeTimeKey, now);
                WriteDouble(nview, StarvingStartKey, 0);
                Log.Diag($"Outpost consumed: wood {wood}->{wood - 1}, calories {calories}->{calories - CaloriesPerCycle}");

                if (TryLevelUp(nview, tablePosition))
                    return ConsumeResult.LeveledUp;

                return ConsumeResult.Consumed;
            }

            // No resources — starvation
            double starvingStart = ReadDouble(nview, StarvingStartKey);
            if (starvingStart <= 0)
            {
                WriteDouble(nview, StarvingStartKey, now);
                Log.Info("Outpost started starving");
                return ConsumeResult.Starving;
            }

            double starvingDuration = now - starvingStart;

            if (starvingDuration >= AbandonAfterSeconds)
            {
                WriteDouble(nview, StarvingStartKey, 0);
                Log.Info("Outpost abandoned — elder leaves");
                return ConsumeResult.Abandoned;
            }

            if (starvingDuration >= DegradeAfterSeconds)
            {
                int level = GetLevel(nview);
                if (level > 0)
                {
                    SetLevel(nview, level - 1);
                    Log.Info($"Outpost level dropped: {level}->{level - 1}");
                    return ConsumeResult.Degraded;
                }
            }

            return ConsumeResult.Starving;
        }

        private static bool TryLevelUp(ZNetView nview, Vector3 tablePosition)
        {
            int level = GetLevel(nview);
            int wood = GetWood(nview);
            int calories = GetCalories(nview);
            int targetLevel = level + 1;

            // Resources — mandatory condition for any level-up
            if (wood < LevelUpWoodThreshold || calories < LevelUpCaloriesThreshold)
                return false;

            // Comfort — check at table position
            int comfort = OutpostComfort.GetComfortAtPoint(tablePosition);
            int requiredComfort = OutpostComfort.GetRequiredComfort(targetLevel);
            if (comfort < requiredComfort)
                return false;

            // Additional conditions per level
            switch (level)
            {
                case 0:
                    // 0 -> 1: supplies + comfort >= 4
                    break;

                case 1:
                    // 1 -> 2: + trust from at least one player
                    if (!HasTrustedPlayer(nview))
                        return false;
                    break;
            }

            SetLevel(nview, targetLevel);
            Log.Info($"Outpost level increased: {level}->{targetLevel} (comfort={comfort})");
            return true;
        }

        private static bool HasTrustedPlayer(ZNetView nview)
        {
            // Check local player — the only one whose ID we know on the client
            if (Player.m_localPlayer == null)
                return false;

            long playerID = Player.m_localPlayer.GetPlayerID();
            return GetRelation(nview, playerID) >= AccessThreshold;
        }

        // --- Double for time (via string, ZDO has no double) ---

        private static double ReadDouble(ZNetView nview, string key)
        {
            string val = OutpostTransferState.ReadZdoString(nview, key);
            if (string.IsNullOrEmpty(val))
                return 0;
            double.TryParse(val, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out double result);
            return result;
        }

        private static void WriteDouble(ZNetView nview, string key, double value)
        {
            OutpostTransferState.WriteZdoString(nview, key,
                value.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        private static bool IsWood(ItemDrop.ItemData item)
        {
            string name = item.m_shared.m_name;
            return name == "$item_wood"
                || name == "$item_finewood"
                || name == "$item_roundlog"
                || name == "$item_yggdrasilwood";
        }

        private static bool IsResin(ItemDrop.ItemData item)
        {
            return item.m_shared.m_name == "$item_resin";
        }

        private static bool IsFood(ItemDrop.ItemData item)
        {
            return item.m_shared.m_food > 0;
        }

        private static int ReadInt(ZNetView nview, string key)
        {
            if (nview == null || Reflect.ZNetView_GetZDO == null || Reflect.ZDO_GetInt == null)
                return 0;

            object zdo = Reflect.ZNetView_GetZDO.Invoke(nview, null);
            if (zdo == null)
                return 0;

            return (int)Reflect.ZDO_GetInt.Invoke(zdo, new object[] { key, 0 });
        }

        private static void WriteInt(ZNetView nview, string key, int value)
        {
            if (nview == null || Reflect.ZNetView_GetZDO == null || Reflect.ZDO_Set_Int == null)
                return;

            if (Reflect.ZNetView_ClaimOwnership != null)
                Reflect.ZNetView_ClaimOwnership.Invoke(nview, null);

            object zdo = Reflect.ZNetView_GetZDO.Invoke(nview, null);
            if (zdo == null)
                return;

            Reflect.ZDO_Set_Int.Invoke(zdo, new object[] { key, value });
        }
    }
}
