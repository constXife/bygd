using System.Collections.Generic;
using Bygd.Framework;
using UnityEngine;

namespace Bygd
{
    /// <summary>
    /// Shows a ghost preview of the blueprint around the table.
    /// Attach to the table's GameObject. Destroy to cancel.
    /// </summary>
    internal class BlueprintGhost : MonoBehaviour
    {
        private BlueprintData _blueprint;
        private float _rotation;
        private readonly List<GameObject> _ghosts = new List<GameObject>();

        private static Material s_ghostMaterial;

        public BlueprintData Blueprint => _blueprint;
        public float Rotation => _rotation;

        public void Init(BlueprintData blueprint, float initialRotation = 0f)
        {
            _blueprint = blueprint;
            _rotation = initialRotation;
            Log.Info($"BlueprintGhost.Init: '{blueprint.Name}', {blueprint.Pieces.Count} pieces");
            RebuildGhosts();
        }

        public void Rotate(float degrees)
        {
            _rotation = (_rotation + degrees) % 360f;
            RebuildGhosts();
            Log.Info($"Plan rotated: {_rotation}deg");
        }

        void OnDestroy()
        {
            ClearGhosts();
        }

        private void RebuildGhosts()
        {
            ClearGhosts();

            if (_blueprint == null)
                return;

            Log.Info($"RebuildGhosts: anchor={transform.position}, rotation={_rotation}, pieces={_blueprint.Pieces.Count}");

            var transformed = _blueprint.Transform(transform.position, _rotation);
            int skipped = 0;

            foreach (var tp in transformed)
            {
                var prefab = ZNetScene.instance?.GetPrefab(tp.Piece.PrefabName);
                if (prefab == null)
                {
                    skipped++;
                    continue;
                }

                try
                {
                    var ghost = CreateGhostPiece(prefab, tp.WorldPos, tp.WorldRot, tp.Piece.Scale);
                    if (ghost != null)
                        _ghosts.Add(ghost);
                }
                catch (System.Exception ex)
                {
                    Log.Error($"Ghost piece '{tp.Piece.PrefabName}' failed: {ex.Message}");
                    skipped++;
                }
            }

            Log.Info($"Ghost preview: {_ghosts.Count} created, {skipped} skipped (no prefab)");
        }

        private void ClearGhosts()
        {
            foreach (var ghost in _ghosts)
            {
                if (ghost != null)
                    Destroy(ghost);
            }
            _ghosts.Clear();
        }

        private static GameObject CreateGhostPiece(GameObject prefab, Vector3 pos, Quaternion rot, Vector3 scale)
        {
            // Disable prefab before cloning so ZNetView.Awake() doesn't fire
            bool wasActive = prefab.activeSelf;
            prefab.SetActive(false);

            var ghost = Instantiate(prefab, pos, rot);
            ghost.name = "bygd_ghost";

            prefab.SetActive(wasActive);

            if (scale != Vector3.one)
                ghost.transform.localScale = scale;

            StripToVisual(ghost);
            ApplyGhostMaterial(ghost);

            ghost.SetActive(true);
            return ghost;
        }

        /// <summary>
        /// Removes everything except visuals.
        /// Keeps MeshFilter, MeshRenderer, LODGroup, Transform.
        /// </summary>
        private static void StripToVisual(GameObject obj)
        {
            var toDestroy = new List<Component>();

            foreach (var comp in obj.GetComponentsInChildren<Component>(true))
            {
                if (comp == null) continue;
                if (comp is Transform) continue;
                if (comp is MeshFilter) continue;
                if (comp is MeshRenderer) continue;
                if (comp is LODGroup) continue;

                toDestroy.Add(comp);
            }

            // Destroy in reverse order — dependent components first
            for (int i = toDestroy.Count - 1; i >= 0; i--)
            {
                if (toDestroy[i] != null)
                    DestroyImmediate(toDestroy[i]);
            }
        }

        private static void ApplyGhostMaterial(GameObject obj)
        {
            EnsureGhostMaterial();

            foreach (var renderer in obj.GetComponentsInChildren<MeshRenderer>(true))
            {
                var materials = renderer.sharedMaterials;
                var ghostMats = new Material[materials.Length];
                for (int i = 0; i < materials.Length; i++)
                    ghostMats[i] = s_ghostMaterial;
                renderer.sharedMaterials = ghostMats;
            }
        }

        /// <summary>
        /// Search for ghost material in this order:
        /// 1. PlanBuild mod material (blueprint wireframe style)
        /// 2. Valheim placement ghost material
        /// 3. Custom fallback
        /// </summary>
        private static void EnsureGhostMaterial()
        {
            if (s_ghostMaterial != null)
                return;

            // PlanBuild shader: "Lux Lit Particles/ Bumped"
            foreach (var shader in Resources.FindObjectsOfTypeAll<Shader>())
            {
                if (shader != null && shader.name == "Lux Lit Particles/ Bumped")
                {
                    s_ghostMaterial = new Material(shader);
                    s_ghostMaterial.color = new Color(1f, 1f, 1f, 0.5f);
                    Log.Info("Ghost material: PlanBuild shader found");
                    return;
                }
            }

            // Or a material with this shader
            foreach (var mat in Resources.FindObjectsOfTypeAll<Material>())
            {
                if (mat != null && mat.shader != null && mat.shader.name == "Lux Lit Particles/ Bumped")
                {
                    s_ghostMaterial = new Material(mat);
                    Log.Info($"Ghost material: copy of '{mat.name}'");
                    return;
                }
            }

            Log.Error("PlanBuild not installed — ghost preview unavailable");
        }

        // --- Static control methods ---

        public static BlueprintGhost GetActive()
        {
            foreach (var table in Object.FindObjectsOfType<OutpostTableComponent>())
            {
                var ghost = table.GetComponent<BlueprintGhost>();
                if (ghost != null)
                    return ghost;
            }
            return null;
        }

        public static BlueprintGhost Show(MonoBehaviour anchor, BlueprintData blueprint)
        {
            Log.Info($"BlueprintGhost.Show: anchor={anchor.name}, blueprint={blueprint.Name}");

            var existing = anchor.GetComponent<BlueprintGhost>();
            if (existing != null)
                Destroy(existing);

            try
            {
                var ghost = anchor.gameObject.AddComponent<BlueprintGhost>();
                ghost.Init(blueprint);
                return ghost;
            }
            catch (System.Exception ex)
            {
                Log.Error($"BlueprintGhost.Show failed: {ex}");
                return null;
            }
        }

        public static void Cancel()
        {
            var active = GetActive();
            if (active != null)
                Destroy(active);
        }
    }
}
