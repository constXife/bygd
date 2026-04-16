namespace Bygd.Framework
{
    internal static class PrefabNames
    {
        // Vanilla
        public const string Workbench = "piece_workbench";
        public const string Hammer = "Hammer";
        public const string Cart = "Cart";
        public const string Wood = "Wood";
        public const string Lox = "Lox";
        public const string Dverger = "Dverger";
        public const string Boar = "Boar";

        // Vanilla — decor
        public const string WoodCoreStack = "wood_core_stack";
        public const string BlackwoodStack = "blackwood_stack";
        public const string TankardDvergr = "Tankard_dvergr";
        public const string Tankard = "Tankard";
        public const string CookedMeat = "CookedMeat";

        // Custom
        public const string OutpostTable = "piece_outpost_table";
        public const string CourierPost = "piece_courier_post";
        public const string MailPost = "piece_mailpost";
        public const string CourierBoar = "BygdCourierBoar";
        public const string LumberjackPost = "piece_lumberjack_post";

        // Saplings
        public const string SaplingBeech = "Beech_Sapling";
        public const string SaplingPine = "PineTree_Sapling";
        public const string SaplingOak = "Oak_Sapling";

        public static void ValidateAll(ZNetScene scene)
        {
            ValidatePrefab(scene, Workbench);
            ValidatePrefab(scene, Hammer);
            ValidatePrefab(scene, Cart);
            ValidatePrefab(scene, Wood);
            ValidatePrefab(scene, Lox);
            ValidatePrefab(scene, Dverger);
        }

        private static void ValidatePrefab(ZNetScene scene, string name)
        {
            if (scene.GetPrefab(name) == null)
                Log.Error($"Vanilla prefab '{name}' not found in ZNetScene — mod may not work correctly");
        }
    }
}
