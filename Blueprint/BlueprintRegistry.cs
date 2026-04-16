using System.Collections.Generic;
using System.IO;
using Bygd.Framework;

namespace Bygd
{
    internal static class BlueprintRegistry
    {
        private static readonly Dictionary<string, BlueprintData> s_blueprints = new Dictionary<string, BlueprintData>();
        private static bool s_loaded;

        // Outpost level requirements
        private static readonly Dictionary<string, int> s_levelRequirements = new Dictionary<string, int>
        {
            { "PiNoKi_SmallHut", 1 },
            { "PiNoKi_Longhouse", 3 },
        };

        public static void EnsureLoaded()
        {
            if (s_loaded)
                return;

            s_loaded = true;
            s_blueprints.Clear();

            string dir = Path.Combine(Path.GetDirectoryName(BygdPlugin.Instance.Info.Location), "blueprints");
            if (!Directory.Exists(dir))
            {
                Log.Info($"Blueprints folder not found: {dir}");
                return;
            }

            foreach (string file in Directory.GetFiles(dir, "*.blueprint"))
            {
                var data = BlueprintParser.Parse(file);
                if (data == null)
                    continue;

                string key = Path.GetFileNameWithoutExtension(file);
                s_blueprints[key] = data;
            }

            Log.Info($"Blueprints loaded: {s_blueprints.Count}");
        }

        public static BlueprintData Get(string key)
        {
            EnsureLoaded();
            s_blueprints.TryGetValue(key, out BlueprintData data);
            return data;
        }

        public static Dictionary<string, BlueprintData> GetAll()
        {
            EnsureLoaded();
            return s_blueprints;
        }

        /// <summary>
        /// Returns the appropriate blueprint for the outpost level.
        /// Selects the blueprint with the highest allowed level.
        /// </summary>
        public static BlueprintData GetForLevel(int outpostLevel)
        {
            EnsureLoaded();

            string bestKey = null;
            int bestLevel = -1;

            foreach (var kvp in s_levelRequirements)
            {
                if (kvp.Value <= outpostLevel && kvp.Value > bestLevel)
                {
                    if (s_blueprints.ContainsKey(kvp.Key))
                    {
                        bestKey = kvp.Key;
                        bestLevel = kvp.Value;
                    }
                }
            }

            if (bestKey != null)
                return s_blueprints[bestKey];

            return null;
        }

        public static int GetRequiredLevel(string key)
        {
            s_levelRequirements.TryGetValue(key, out int level);
            return level;
        }

        public static List<string> GetAvailableKeys()
        {
            EnsureLoaded();
            return new List<string>(s_blueprints.Keys);
        }
    }
}
