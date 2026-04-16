using System.Collections.Generic;
using BepInEx;
using HarmonyLib;
using Jotunn;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using Bygd.Framework;
using UnityEngine;

namespace Bygd
{
    [BepInPlugin("com.constxife.bygd", "Bygd", "1.0.0")]
    [BepInDependency(Main.ModGuid)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    public class BygdPlugin : BaseUnityPlugin
    {
        public static BygdPlugin Instance;

        private readonly Harmony harmony = new Harmony("com.constxife.bygd");

        public static Dictionary<string, Vector3> Stations = new Dictionary<string, Vector3>();
        public static Dictionary<string, Vector3> Waypoints = new Dictionary<string, Vector3>();

        public static bool DevMode;

        private void Awake()
        {
            Instance = this;
            Log.Init(Logger);

            Localizations.Register();
            Commands.Register();
            PrefabManager.OnVanillaPrefabsAvailable += RegisterPieces;
            harmony.PatchAll();
            Log.Info("Bygd mod loaded!");
        }

        private void RegisterPieces()
        {
            RegisterOutpostTable();
            RegisterCourierPost();
            RegisterMailPost();
            RegisterLumberjackPost();

            if (ZNetScene.instance != null)
                PrefabNames.ValidateAll(ZNetScene.instance);

            PrefabManager.OnVanillaPrefabsAvailable -= RegisterPieces;
        }

        private void RegisterOutpostTable()
        {
            var config = new PieceConfig();
            config.Name = "$piece_outpost_table";
            config.Description = "$piece_outpost_table_desc";
            config.PieceTable = PieceTables.Hammer;
            config.Category = PieceCategories.Misc;
            config.AddRequirement(new RequirementConfig(PrefabNames.Wood, 10, 0, true));

            var customPiece = new CustomPiece(PrefabNames.OutpostTable, PrefabNames.Workbench, config);
            StripWorkbenchComponents(customPiece.PiecePrefab);
            PieceManager.Instance.AddPiece(customPiece);
            Log.Info("Outpost Table registered via Jotunn");
        }

        private void RegisterCourierPost()
        {
            var config = new PieceConfig();
            config.Name = "$piece_courier_post";
            config.Description = "$piece_courier_post_desc";
            config.PieceTable = PieceTables.Hammer;
            config.Category = PieceCategories.Misc;
            config.AddRequirement(new RequirementConfig(PrefabNames.Wood, 5, 0, true));

            var customPiece = new CustomPiece(PrefabNames.CourierPost, PrefabNames.Workbench, config);
            StripWorkbenchComponents(customPiece.PiecePrefab);
            PieceManager.Instance.AddPiece(customPiece);
            Log.Info("Courier Post registered via Jotunn");
        }

        private void RegisterMailPost()
        {
            var config = new PieceConfig();
            config.Name = "$piece_mailpost";
            config.Description = "$piece_mailpost_desc";
            config.PieceTable = PieceTables.Hammer;
            config.Category = PieceCategories.Misc;
            config.AddRequirement(new RequirementConfig(PrefabNames.Wood, 3, 0, true));

            var customPiece = new CustomPiece(PrefabNames.MailPost, PrefabNames.Workbench, config);
            StripWorkbenchComponents(customPiece.PiecePrefab);
            PieceManager.Instance.AddPiece(customPiece);
            Log.Info("Mail Post registered via Jotunn");
        }

        private void RegisterLumberjackPost()
        {
            var config = new PieceConfig();
            config.Name = "$piece_lumberjack_post";
            config.Description = "$piece_lumberjack_post_desc";
            config.PieceTable = PieceTables.Hammer;
            config.Category = PieceCategories.Misc;
            config.AddRequirement(new RequirementConfig(PrefabNames.Wood, 5, 0, true));

            var customPiece = new CustomPiece(PrefabNames.LumberjackPost, PrefabNames.Workbench, config);
            StripWorkbenchComponents(customPiece.PiecePrefab);
            PieceManager.Instance.AddPiece(customPiece);
            Log.Info("Lumberjack Post registered via Jotunn");
        }

        /// <summary>
        /// Strips CraftingStation, CircleProjector and EffectArea —
        /// everything left over from the workbench clone that draws the radius circle.
        /// </summary>
        private static void StripWorkbenchComponents(GameObject go)
        {
            var craftingStation = go.GetComponent<CraftingStation>();
            if (craftingStation != null)
                Object.DestroyImmediate(craftingStation);

            var piece = go.GetComponent<Piece>();
            if (piece != null)
                piece.m_craftingStation = null;

            foreach (var circle in go.GetComponentsInChildren<CircleProjector>(true))
                Object.DestroyImmediate(circle.gameObject);

            foreach (var effect in go.GetComponentsInChildren<EffectArea>(true))
                Object.DestroyImmediate(effect.gameObject);
        }
    }
}
