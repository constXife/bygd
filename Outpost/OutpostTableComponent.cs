using System.Collections;
using Bygd.Framework;
using UnityEngine;

namespace Bygd
{
    public class OutpostTableComponent : MonoBehaviour, Hoverable, Interactable
    {
        private const float CheckInterval = 5f;
        private const float SearchRadius = 10f;

        private GameObject _settler;
        private Collider[] _overlapBuffer;
        private ZNetView _nview;

        // --- Hoverable / Interactable ---

        public string GetHoverText()
        {
            string ghostHover = AnchorUI.GetGhostHoverText(this);
            if (ghostHover != null)
                return ghostHover;

            bool transferred = OutpostTransferState.IsTransferred(_nview);
            string status = Localization.instance.Localize(transferred ? "$outpost_status_npc" : "$outpost_status_player");
            string tableName = Localization.instance.Localize("$piece_outpost_table");

            string info = $"{tableName} ({status})\n[<color=yellow><b>$KEY_Use</b></color>] Open";

            if (transferred)
            {
                int level = OutpostResources.GetLevel(_nview);
                int wood = OutpostResources.GetWood(_nview);
                int calories = OutpostResources.GetCalories(_nview);
                int resin = OutpostResources.GetResin(_nview);
                int comfort = OutpostComfort.GetComfortAtPoint(transform.position);
                int nextComfort = OutpostComfort.GetRequiredComfort(level + 1);

                info += $"\nLevel: {level} | Comfort: {comfort}/{nextComfort}";
                info += $"\nWood: {wood} | Calories: {calories} | Resin: {resin}";

                // Problems
                var problems = new System.Collections.Generic.List<string>();

                if (!RoofCheck.HasRoofAbove(transform.position))
                    problems.Add("no roof");

                if (!HasNearbyComponent<Fireplace>())
                    problems.Add("no fire");

                if (!HasNearbyComponent<Bed>())
                    problems.Add("no bed");

                {
                    string tableId = OutpostSettlerBinding.GetObjectZdoId(gameObject);
                    bool hasLinkedChest = false;
                    int nearbyChests = 0;
                    float nearestDist = float.MaxValue;

                    foreach (var c in Object.FindObjectsOfType<Container>())
                    {
                        if (c == null || c.gameObject == gameObject) continue;
                        float d = Vector3.Distance(c.transform.position, transform.position);
                        if (d < 20f)
                        {
                            nearbyChests++;
                            if (d < nearestDist) nearestDist = d;
                        }
                        if (!string.IsNullOrEmpty(tableId))
                        {
                            var cnv = c.GetComponent<ZNetView>();
                            if (cnv != null && OutpostTransferState.ReadZdoString(cnv, "bygd_chest_table_id") == tableId)
                                hasLinkedChest = true;
                        }
                    }

                    if (!hasLinkedChest)
                    {
                        if (nearbyChests == 0)
                            problems.Add("no chest nearby");
                        else
                            problems.Add($"chest not linked ({nearbyChests} nearby, closest {nearestDist:F0}m)");
                    }
                }

                if (wood <= 0) problems.Add("no firewood");
                if (calories <= 0) problems.Add("no food");

                if (problems.Count > 0)
                    info += $"\n<color=red>! {string.Join(", ", problems)}</color>";
            }

            return info;
        }

        public string GetHoverName()
        {
            return Localization.instance.Localize("$piece_outpost_table");
        }

        public bool Interact(Humanoid user, bool hold, bool alt)
        {
            if (hold)
                return false;

            if (AnchorUI.HandleGhostInteract(this, user, alt))
                return true;

            ShowUI();
            return true;
        }

        private void ShowUI()
        {
            if (AnchorUI.IsOpen)
            {
                AnchorUI.Close();
                return;
            }

            var nview = _nview;
            bool transferred = OutpostTransferState.IsTransferred(nview);
            int level = OutpostResources.GetLevel(nview);
            int wood = OutpostResources.GetWood(nview);
            int calories = OutpostResources.GetCalories(nview);

            string title = transferred
                ? $"Elder's Table — Lv. {level}"
                : "Elder's Table";
            string info = transferred
                ? $"Wood: {wood} | Calories: {calories}"
                : null;

            var buttons = new System.Collections.Generic.List<ButtonDef>();

            buttons.Add(new ButtonDef(Localization.instance.Localize("$ui_build_house"), () =>
            {
                AnchorUI.Close();
                BlueprintSelectionUI.Show(this);
            }));

            if (!transferred)
            {
                buttons.Add(new ButtonDef("Transfer to NPC", () =>
                {
                    var chest = OutpostChestLink.FindAndLinkChest(this, nview);
                    if (chest == null)
                    {
                        Player.m_localPlayer?.Message(MessageHud.MessageType.Center,
                            Localization.instance.Localize("$outpost_needs_chest"));
                        AnchorUI.Close();
                        return;
                    }
                    OutpostTransferState.SetTransferred(nview, true);
                    OutpostWard.ActivateWard(this, nview);
                    long pid = Player.m_localPlayer != null ? Player.m_localPlayer.GetPlayerID() : 0;
                    if (pid != 0)
                        OutpostResources.AddRelation(nview, pid, OutpostResources.AccessThreshold);
                    Player.m_localPlayer?.Message(MessageHud.MessageType.Center,
                        Localization.instance.Localize("$outpost_transferred"));
                    AnchorUI.Close();
                }));
            }
            else
            {
                buttons.Add(new ButtonDef("Return to player", () =>
                {
                    OutpostTransferState.SetTransferred(nview, false);
                    OutpostWard.DeactivateWard(this);
                    OutpostChestLink.UnlinkChest(this);
                    Player.m_localPlayer?.Message(MessageHud.MessageType.Center,
                        Localization.instance.Localize("$outpost_returned"));
                    AnchorUI.Close();
                }));
            }

            AnchorUI.Show(title, info, buttons);
        }

        public bool UseItem(Humanoid user, ItemDrop.ItemData item)
        {
            return false;
        }

        // --- Lifecycle ---

        void Awake()
        {
            _overlapBuffer = new Collider[256];
            _nview = GetComponent<ZNetView>();
            Log.Diag($"OutpostTableComponent.Awake: object={name}, zdoId={OutpostSettlerBinding.GetObjectZdoId(gameObject)}");
        }

        void Start()
        {
            Log.Diag($"OutpostTableComponent.Start: object={name}, isOwner={OutpostSettlerBinding.IsOwner(_nview)}");
            StartCoroutine(ConditionLoop());
        }

        private IEnumerator ConditionLoop()
        {
            // Let the world load — ZDO, ZNetScene, containers
            yield return new WaitForSeconds(5f);

            while (true)
            {
                if (OutpostSettlerBinding.IsOwner(_nview))
                {
                    OutpostSettlerBinding.EnsureTableKey(_nview, transform);
                    UpdateSettlerState();

                    if (OutpostTransferState.IsTransferred(_nview))
                    {
                        OutpostChestLink.EnsureLinked(this, _nview);
                        OutpostChestCollector.TryCollect(this, _nview);
                        CheckLevelUp();
                        ProcessConsumption();
                        OutpostVisuals.RefuelNearbyFires(transform, _nview);
                    }
                    else
                    {
                    }
                }
                yield return new WaitForSeconds(CheckInterval);
            }
        }

        // --- Settler ---

        private void UpdateSettlerState()
        {
            if (_settler == null)
                _settler = OutpostSettlerManager.RefreshSettlerReference(_nview, transform);

            bool hasFire = HasNearbyComponent<Fireplace>();
            bool hasBed = HasNearbyComponent<Bed>();
            bool conditionsMet = hasFire && hasBed;

            if (conditionsMet && _settler == null)
                _settler = OutpostSettlerManager.SpawnSettler(_nview, transform);
            else if (!conditionsMet && _settler != null && !BygdPlugin.DevMode)
            {
                Log.Info($"Settler leaving: hasFire={hasFire}, hasBed={hasBed}, overlapHits={_lastOverlapHits}");
                OutpostSettlerManager.DespawnSettler(_settler, _nview);
                _settler = null;
            }
        }

        // --- Consumption ---

        private void ProcessConsumption()
        {
            if (BygdPlugin.DevMode)
                return;

            var result = OutpostResources.TryConsume(_nview, transform.position);

            if (result == OutpostResources.ConsumeResult.Consumed
                || result == OutpostResources.ConsumeResult.LeveledUp)
            {
                // Food/wood visualization disabled
            }

            switch (result)
            {
                case OutpostResources.ConsumeResult.LeveledUp:
                    int newLevel = OutpostResources.GetLevel(_nview);
                    if (Player.m_localPlayer != null)
                        Player.m_localPlayer.Message(MessageHud.MessageType.Center,
                            string.Format(Localization.instance.Localize("$outpost_level_up"), newLevel));
                    break;

                case OutpostResources.ConsumeResult.Abandoned:
                    OutpostTransferState.SetTransferred(_nview, false);
                    OutpostWard.DeactivateWard(this);
                    OutpostChestLink.UnlinkChest(this);
                    if (_settler != null)
                    {
                        OutpostSettlerManager.DespawnSettler(_settler, _nview);
                        _settler = null;
                    }
                    DespawnLinkedCouriers();

                    if (Player.m_localPlayer != null)
                        Player.m_localPlayer.Message(MessageHud.MessageType.Center,
                            Localization.instance.Localize("$outpost_abandoned"));
                    break;

                case OutpostResources.ConsumeResult.Degraded:
                    if (Player.m_localPlayer != null)
                        Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft,
                            Localization.instance.Localize("$outpost_degraded"));
                    break;
            }
        }

        // --- Level-up ---

        private bool _levelCheckedToday;

        private void CheckLevelUp()
        {
            // Check only in the morning (once per day)
            if (EnvMan.instance == null)
                return;

            bool isMorning = EnvMan.IsDay() && EnvMan.instance.GetDayFraction() < 0.15f;
            if (!isMorning)
            {
                _levelCheckedToday = false;
                return;
            }

            if (_levelCheckedToday)
                return;

            _levelCheckedToday = true;
            int level = OutpostResources.GetLevel(_nview);
            int wood = OutpostResources.GetWood(_nview);
            int calories = OutpostResources.GetCalories(_nview);
            int comfort = OutpostComfort.GetComfortAtPoint(transform.position);
            int targetLevel = level + 1;
            int requiredComfort = OutpostComfort.GetRequiredComfort(targetLevel);

            if (wood < OutpostResources.LevelUpWoodThreshold)
                return;
            if (calories < OutpostResources.LevelUpCaloriesThreshold)
                return;
            if (comfort < requiredComfort)
                return;

            // Trust for level 1->2
            if (level == 1)
            {
                if (Player.m_localPlayer == null)
                    return;
                long playerID = Player.m_localPlayer.GetPlayerID();
                if (OutpostResources.GetRelation(_nview, playerID) < OutpostResources.AccessThreshold)
                    return;
            }

            OutpostResources.SetLevel(_nview, targetLevel);
            Log.Info($"Outpost level increased: {level}->{targetLevel} (comfort={comfort})");

            if (Player.m_localPlayer != null)
                Player.m_localPlayer.Message(MessageHud.MessageType.Center,
                    string.Format(Localization.instance.Localize("$outpost_level_up"), targetLevel));
        }

        void OnDestroy()
        {
            // Ghost preview
            var ghost = GetComponent<BlueprintGhost>();
            if (ghost != null)
                Destroy(ghost);

            // Builder
            var builder = GetComponent<BlueprintBuilder>();
            if (builder != null)
                Destroy(builder);

            // Settler
            if (_settler != null)
            {
                OutpostSettlerManager.DespawnSettler(_settler, _nview);
                _settler = null;
            }

            // Couriers linked to this outpost
            DespawnLinkedCouriers();

            // Ward
            if (OutpostTransferState.IsTransferred(_nview))
                OutpostWard.DeactivateWard(this);

            // Remove from cache
            OutpostCache.Invalidate();

            Log.Info("Elder's Table destroyed — cleanup done");
        }

        // --- Cascade ---

        private void DespawnLinkedCouriers()
        {
            string tableKey = OutpostSettlerBinding.GetTableKey(_nview, transform);
            if (string.IsNullOrEmpty(tableKey))
                return;

            foreach (var post in Object.FindObjectsOfType<CourierPostComponent>())
            {
                if (post == null)
                    continue;

                var postNview = post.GetComponent<ZNetView>();
                string parentKey = CourierBinding.GetParentTable(postNview);
                if (parentKey == tableKey)
                    post.DespawnCourierCascade();
            }
        }

        // --- Utility ---

        private int _lastOverlapHits;

        private bool HasNearbyComponent<T>() where T : Component
        {
            int hits = Physics.OverlapSphereNonAlloc(transform.position, SearchRadius, _overlapBuffer);
            _lastOverlapHits = hits;
            for (int i = 0; i < hits; i++)
            {
                Collider col = _overlapBuffer[i];
                if (col != null && col.GetComponentInParent<T>() != null)
                    return true;
            }

            return false;
        }

        internal Vector3 GetSettlerAnchorPosition()
        {
            return OutpostSettlerManager.GetSpawnPosition(transform);
        }
    }
}
