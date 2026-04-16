using Bygd.Framework;
using UnityEngine;

namespace Bygd
{
    internal class MountConfig
    {
        public readonly string PrefabName;
        public readonly int CargoSlots;
        public readonly int PassengerSlots;
        public readonly float SpeedMultiplier;
        public readonly float DeliveryFee; // 0.0 = free, 0.1 = 10% of cargo
        public readonly float LoadedSlowdown; // slowdown at full load (0.5 = -50% speed)

        public MountConfig(string prefabName, int cargoSlots, int passengerSlots, float speedMultiplier, float deliveryFee, float loadedSlowdown)
        {
            PrefabName = prefabName;
            CargoSlots = cargoSlots;
            PassengerSlots = passengerSlots;
            SpeedMultiplier = speedMultiplier;
            DeliveryFee = deliveryFee;
            LoadedSlowdown = loadedSlowdown;
        }

        public float GetSpeedWithLoad(int usedSlots)
        {
            if (CargoSlots <= 0 || usedSlots <= 0)
                return SpeedMultiplier;

            float loadFraction = Mathf.Clamp01((float)usedSlots / CargoSlots);
            return SpeedMultiplier * (1f - LoadedSlowdown * loadFraction);
        }
    }

    internal static class MountConfigs
    {
        //                                                    prefab  cargo pass speed  fee  slowdown
        public static readonly MountConfig OnFoot = new MountConfig(null,   1,  0, 0.6f, 0f, 0.4f);
        public static readonly MountConfig Boar   = new MountConfig(PrefabNames.Boar, 4, 0, 0.7f, 0f, 0.2f);
        // Future:
        // public static readonly MountConfig Lox  = new MountConfig(PrefabNames.Lox, 16, 4, 0.7f, 0f, 0.05f);
        // public static readonly MountConfig Raven = new MountConfig("Raven", 0, 1, 2.0f, 0f, 0f);

        public static bool IsOnFoot(MountConfig config) => config.PrefabName == null;
    }
}
