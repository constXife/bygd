using System.Collections;
using System.Collections.Generic;
using Bygd.Framework;
using UnityEngine;

namespace Bygd
{
    internal class BlueprintBuilder : MonoBehaviour
    {
        private const float PiecesPerSecond = 3f;
        private const int WoodPerPiece = 1;

        private BlueprintData _blueprint;
        private float _rotation;
        private int _builtCount;
        private int _totalCount;
        private bool _building;

        public bool IsBuilding => _building;
        public float Progress => _totalCount > 0 ? (float)_builtCount / _totalCount : 0f;

        public static BlueprintBuilder Start(MonoBehaviour anchor, BlueprintGhost ghost)
        {
            var blueprint = ghost.Blueprint;
            float rotation = ghost.Rotation;

            Destroy(ghost);

            var builder = anchor.gameObject.AddComponent<BlueprintBuilder>();
            builder._blueprint = blueprint;
            builder._rotation = rotation;
            builder._totalCount = blueprint.Pieces.Count;
            builder.StartCoroutine(builder.BuildCoroutine());
            return builder;
        }

        private IEnumerator BuildCoroutine()
        {
            _building = true;
            var nview = GetComponent<ZNetView>();
            var sorted = _blueprint.GetBuildOrder();
            var transformed = _blueprint.Transform(transform.position, _rotation);

            // Level terrain under the house
            LevelTerrain(transform.position, _blueprint, _rotation);
            yield return new WaitForSeconds(0.5f);

            var buildList = BuildTransformedList(sorted, transformed);

            float interval = 1f / PiecesPerSecond;

            foreach (var tp in buildList)
            {
                if (!BygdPlugin.DevMode)
                {
                    int wood = OutpostResources.GetWood(nview);
                    if (wood < WoodPerPiece)
                    {
                        if (Player.m_localPlayer != null)
                            Player.m_localPlayer.Message(MessageHud.MessageType.Center,
                                "Not enough wood to continue building!");
                        Log.Info($"Construction paused: no wood ({_builtCount}/{_totalCount})");
                        while (OutpostResources.GetWood(nview) < WoodPerPiece)
                            yield return new WaitForSeconds(5f);
                    }
                }

                bool spawned = SpawnPiece(tp.Piece.PrefabName, tp.WorldPos, tp.WorldRot, tp.Piece.Scale);
                if (spawned)
                {
                    if (!BygdPlugin.DevMode)
                        OutpostResources.SetWood(nview, OutpostResources.GetWood(nview) - WoodPerPiece);
                    _builtCount++;
                }

                yield return new WaitForSeconds(interval);
            }

            _building = false;
            Log.Info($"Construction completed: {_builtCount} pieces");

            if (Player.m_localPlayer != null)
                Player.m_localPlayer.Message(MessageHud.MessageType.Center,
                    $"House built! ({_builtCount} pieces)");

            Destroy(this);
        }

        private static bool SpawnPiece(string prefabName, Vector3 pos, Quaternion rot, Vector3 scale)
        {
            var prefab = ZNetScene.instance?.GetPrefab(prefabName);
            if (prefab == null)
            {
                Log.Diag($"Build: prefab '{prefabName}' not found, skipping");
                return false;
            }

            var obj = Instantiate(prefab, pos, rot);

            if (scale != Vector3.one)
                obj.transform.localScale = scale;

            var piece = obj.GetComponent<Piece>();
            if (piece != null && Player.m_localPlayer != null)
                piece.SetCreator(Player.m_localPlayer.GetPlayerID());

            return true;
        }

        private static void LevelTerrain(Vector3 center, BlueprintData blueprint, float rotation)
        {
            // Find house radius
            float maxDist = 0f;
            var anchorRot = Quaternion.Euler(0f, rotation, 0f);
            var fpCenter = new Vector3(
                (blueprint.Pieces[0].Position.x + blueprint.Pieces[blueprint.Pieces.Count - 1].Position.x) / 2f,
                0f,
                (blueprint.Pieces[0].Position.z + blueprint.Pieces[blueprint.Pieces.Count - 1].Position.z) / 2f);

            foreach (var piece in blueprint.Pieces)
            {
                if (piece.Position.y > 0.5f) continue; // floor only
                Vector3 offset = anchorRot * (piece.Position - fpCenter);
                float dist = new Vector2(offset.x, offset.z).magnitude;
                if (dist > maxDist) maxDist = dist;
            }

            float radius = maxDist + 2f;

            // Use TerrainOp for leveling (like a hoe in the game)
            var prefab = ZNetScene.instance?.GetPrefab("raise");
            if (prefab == null)
            {
                Log.Info("TerrainOp prefab 'raise' not found, skipping terrain leveling");
                return;
            }

            // Create TerrainOp directly.
            var terrainComp = TerrainComp.FindTerrainCompiler(center);
            if (terrainComp == null)
            {
                Log.Info("TerrainComp not found, skipping terrain leveling");
                return;
            }

            // Level ground through TerrainOp.
            var go = new GameObject("bygd_terrain_op");
            go.transform.position = center;
            var op = go.AddComponent<TerrainOp>();
            op.m_settings.m_level = true;
            op.m_settings.m_levelRadius = radius;
            op.m_settings.m_levelOffset = 0f;
            op.m_settings.m_square = true;

            // TerrainOp.OnPlaced is triggered automatically during Awake/Start.
            Log.Info($"Terrain leveling triggered: radius={radius:F1}m");
        }

        private static List<TransformedPiece> BuildTransformedList(
            List<BlueprintPiece> sorted,
            List<TransformedPiece> transformed)
        {
            var result = new List<TransformedPiece>();
            var used = new bool[transformed.Count];

            foreach (var sortedPiece in sorted)
            {
                for (int i = 0; i < transformed.Count; i++)
                {
                    if (used[i]) continue;
                    if (transformed[i].Piece.PrefabName == sortedPiece.PrefabName
                        && transformed[i].Piece.Position == sortedPiece.Position)
                    {
                        result.Add(transformed[i]);
                        used[i] = true;
                        break;
                    }
                }
            }

            return result;
        }
    }
}
