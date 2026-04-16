using System.Collections;
using System.Collections.Generic;
using Bygd.Framework;
using UnityEngine;

namespace Bygd
{
    public class LumberjackPostComponent : MonoBehaviour, Hoverable, Interactable
    {
        private const float CheckInterval = 5f;
        private const float SearchRadius = 10f;

        private GameObject _lumberjack;
        private ZNetView _nview;
        private LumberjackWorker _worker;

        // --- Hoverable / Interactable ---

        public string GetHoverText()
        {
            string ghostHover = AnchorUI.GetGhostHoverText(this);
            if (ghostHover != null)
                return ghostHover;

            string postName = Localization.instance.Localize("$piece_lumberjack_post");
            bool hasWorker = _lumberjack != null;

            if (_worker != null && _worker.IsWorking)
            {
                string task = _worker.CurrentTask;
                return $"{postName} ({task})";
            }

            string state = Localization.instance.Localize(hasWorker ? "$lumberjack_present" : "$ui_npc_absent");
            string open = Localization.instance.Localize("$ui_open");
            return $"{postName} ({state})\n[<color=yellow><b>$KEY_Use</b></color>] {open}";
        }

        public string GetHoverName()
        {
            return Localization.instance.Localize("$piece_lumberjack_post");
        }

        public bool Interact(Humanoid user, bool hold, bool alt)
        {
            if (hold)
                return false;

            if (AnchorUI.HandleGhostInteract(this, user, alt))
                return true;

            ShowUI(user);
            return true;
        }

        public bool UseItem(Humanoid user, ItemDrop.ItemData item)
        {
            return false;
        }

        private void ShowUI(Humanoid user)
        {
            if (AnchorUI.IsOpen)
            {
                AnchorUI.Close();
                return;
            }

            string title = Localization.instance.Localize("$piece_lumberjack_post");
            bool hasWorker = _lumberjack != null;
            string info = Localization.instance.Localize(hasWorker ? "$lumberjack_present" : "$lumberjack_missing");

            var buttons = new List<ButtonDef>();

            buttons.Add(new ButtonDef(Localization.instance.Localize("$ui_build_house"), () =>
            {
                AnchorUI.Close();
                BlueprintSelectionUI.Show(this);
            }));

            if (hasWorker && _lumberjack.GetComponent<LumberjackNPC>() != null)
            {
                buttons.Add(new ButtonDef(Localization.instance.Localize("$ui_talk"), () =>
                {
                    AnchorUI.Close();
                    var npc = _lumberjack.GetComponent<LumberjackNPC>();
                    npc?.Interact(user, false, false);
                }));
            }

            AnchorUI.Show(title, info, buttons);
        }

        // --- Lifecycle ---

        void Awake()
        {
            _nview = GetComponent<ZNetView>();
        }

        void Start()
        {
            StartCoroutine(ConditionLoop());
        }

        private IEnumerator ConditionLoop()
        {
            yield return new WaitForSeconds(5f);

            while (true)
            {
                if (OutpostSettlerBinding.IsOwner(_nview))
                    UpdateWorkerState();

                yield return new WaitForSeconds(CheckInterval);
            }
        }

        // --- Worker ---

        private void UpdateWorkerState()
        {
            if (_lumberjack == null)
                _lumberjack = LumberjackManager.RefreshReference(_nview, transform);

            bool conditionsMet = HasNearbyBed() && HasParentOutpost();

            if (conditionsMet && _lumberjack == null)
            {
                _lumberjack = LumberjackManager.Spawn(_nview, transform);
                if (_lumberjack != null)
                    EnsureWorker();
            }
            else if (!conditionsMet && _lumberjack != null && !BygdPlugin.DevMode)
            {
                LumberjackManager.Despawn(_lumberjack, _nview);
                _lumberjack = null;
                _worker = null;
            }
            else if (_lumberjack != null)
            {
                EnsureWorker();
            }
        }

        private void EnsureWorker()
        {
            if (_worker == null && _lumberjack != null)
            {
                _worker = _lumberjack.GetComponent<LumberjackWorker>();
                if (_worker == null)
                {
                    _worker = _lumberjack.AddComponent<LumberjackWorker>();
                    _worker.Init(this, _nview);
                }
            }
        }

        private bool HasNearbyBed() =>
            ObjectFinder.HasNearby<Bed>(transform.position, SearchRadius);

        private bool HasParentOutpost() =>
            FindParentTable() != null || BygdPlugin.DevMode;

        public OutpostTableComponent FindParentTable() =>
            OutpostCache.FindNearestTransferred(transform.position, 30f);

        void OnDestroy()
        {
            if (_lumberjack != null)
            {
                LumberjackManager.Despawn(_lumberjack, _nview);
                _lumberjack = null;
            }
            _worker = null;
            Log.Info("Lumberjack post destroyed - cleanup completed");
        }
    }
}
