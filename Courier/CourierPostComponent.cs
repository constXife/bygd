using System.Collections;
using Bygd.Framework;
using UnityEngine;

namespace Bygd
{
    public class CourierPostComponent : MonoBehaviour, Hoverable, Interactable
    {
        private const float CheckInterval = 5f;
        private const float SearchRadius = 10f;
        private const float TableSearchRadius = 20f;

        private GameObject _courier;
        private ZNetView _nview;
        private CourierDeliveryRunner _delivery;

        // --- Hoverable / Interactable ---

        public string GetHoverText()
        {
            string ghostHover = AnchorUI.GetGhostHoverText(this);
            if (ghostHover != null)
                return ghostHover;

            string postName = Localization.instance.Localize("$piece_courier_post");

            if (_delivery != null && _delivery.IsActive)
            {
                var patrol = _delivery.Patrol;
                int stop = patrol.CurrentStopIndex;
                int total = patrol.Stops.Count;
                int bagCount = patrol.Bag.Count;
                float progress = patrol.GetProgress();
                string direction = patrol.IsReturningHome
                    ? Localization.instance.Localize("$courier_patrol_home")
                    : string.Format(Localization.instance.Localize("$courier_patrol_stop"), stop + 1, total);
                string bagInfo = bagCount > 0
                    ? string.Format(Localization.instance.Localize("$courier_patrol_bag_info"), bagCount)
                    : string.Empty;
                string status = string.Format(
                    Localization.instance.Localize("$courier_patrol_hover"),
                    direction,
                    (int)(progress * 100),
                    bagInfo);
                return $"{postName} ({status})";
            }

            if (_delivery != null && _delivery.IsOnCooldown())
            {
                string status = Localization.instance.Localize("$courier_post_resting");
                return $"{postName} ({status})";
            }

            bool hasCourier = _courier != null;
            string state = Localization.instance.Localize(hasCourier ? "$courier_post_occupied" : "$courier_post_empty");
            string open = Localization.instance.Localize("$ui_open");
            return $"{postName} ({state})\n[<color=yellow><b>$KEY_Use</b></color>] {open}";
        }

        public string GetHoverName()
        {
            return Localization.instance.Localize("$piece_courier_post");
        }

        public bool Interact(Humanoid user, bool hold, bool alt)
        {
            if (hold)
                return false;

            if (AnchorUI.HandleGhostInteract(this, user, alt))
                return true;

            // Courier is on patrol: show the current status.
            if (_delivery != null && _delivery.IsActive)
            {
                var patrol = _delivery.Patrol;
                string msg = string.Format(
                    Localization.instance.Localize("$courier_patrol_status"),
                    patrol.CurrentStopIndex, patrol.Stops.Count);
                user?.Message(MessageHud.MessageType.Center, msg);
                return true;
            }

            ShowUI(user);
            return true;
        }

        private void ShowUI(Humanoid user)
        {
            if (AnchorUI.IsOpen)
            {
                AnchorUI.Close();
                return;
            }

            string title = Localization.instance.Localize("$piece_courier_post");
            bool hasCourier = _courier != null;
            string info = hasCourier
                ? Localization.instance.Localize("$courier_post_occupied")
                : Localization.instance.Localize("$courier_post_empty");

            var buttons = new System.Collections.Generic.List<ButtonDef>();

            buttons.Add(new ButtonDef(Localization.instance.Localize("$ui_build_house"), () =>
            {
                AnchorUI.Close();
                BlueprintSelectionUI.Show(this);
            }));

            if (hasCourier)
            {
                buttons.Add(new ButtonDef(Localization.instance.Localize("$ui_talk"), () =>
                {
                    AnchorUI.Close();
                    var courierNpc = _courier.GetComponent<CourierNPC>();
                    if (courierNpc != null)
                        courierNpc.Interact(user, false, false);
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
            _nview = GetComponent<ZNetView>();
            _delivery = new CourierDeliveryRunner(this, _nview);
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
                {
                    CourierBinding.EnsurePostKey(_nview, transform);

                    if (!_delivery.IsActive)
                        UpdateCourierState();
                }
                yield return new WaitForSeconds(CheckInterval);
            }
        }

        // --- Courier state ---

        private void UpdateCourierState()
        {
            if (_courier == null)
                _courier = CourierManager.RefreshCourierReference(_nview, transform);

            bool conditionsMet = IsParentTableValid() && HasNearbyBed();

            if (conditionsMet && _courier == null)
                _courier = CourierManager.SpawnCourier(_nview, transform);
            else if (!conditionsMet && _courier != null)
            {
                CourierManager.DespawnCourier(_courier, _nview);
                _courier = null;
            }
        }

        // --- Delivery delegation ---

        public void ForceRespawnCourier()
        {
            _delivery.Reset();

            if (_courier != null)
            {
                ZNetScene.instance.Destroy(_courier);
                _courier = null;
            }

            _courier = CourierManager.SpawnCourier(_nview, transform);
        }

        public void ForceStartPatrol(Terminal context)
        {
            if (_courier == null)
            {
                context.AddString("  No courier found - spawning one");
                _courier = CourierManager.SpawnCourier(_nview, transform);
                if (_courier == null)
                {
                    context.AddString("  Failed to spawn courier");
                    return;
                }
            }

            _delivery.ForceStartPatrol(_courier, context);
        }

        public bool IsDelivering()
        {
            return _delivery.IsActive;
        }

        public bool RequestDelivery(MailPostComponent targetMailPost)
        {
            if (_courier == null)
                return false;

            return _delivery.RequestDelivery(_courier, targetMailPost);
        }

        public double GetCooldownRemaining()
        {
            return _delivery.GetCooldownRemaining();
        }

        // --- Conditions ---

        private bool IsParentTableValid()
        {
            var table = FindParentTable();
            if (table == null)
                return false;

            var tableNview = table.GetComponent<ZNetView>();
            if (!OutpostTransferState.IsTransferred(tableNview))
                return false;

            int level = OutpostResources.GetLevel(tableNview);
            return level >= 2;
        }

        private bool HasNearbyBed() =>
            ObjectFinder.HasNearby<Bed>(transform.position, SearchRadius);

        void OnDestroy()
        {
            if (_delivery != null)
                _delivery.Reset();

            if (_courier != null)
            {
                CourierManager.DespawnCourier(_courier, _nview);
                _courier = null;
            }

            Log.Info("Courier post destroyed - cleanup completed");
        }

        // --- Parent table ---

        public OutpostTableComponent FindParentTable()
        {
            // Resolve by the stored ZDO binding first.
            string parentTableKey = CourierBinding.GetParentTable(_nview);
            if (!string.IsNullOrEmpty(parentTableKey))
            {
                foreach (var table in OutpostCache.GetTransferredTables())
                {
                    if (table == null) continue;
                    var tableNview = table.GetComponent<ZNetView>();
                    string tableKey = OutpostSettlerBinding.GetTableKey(tableNview, table.transform);
                    if (parentTableKey == tableKey)
                        return table;
                }
            }

            // Fallback to the nearest transferred table.
            return OutpostCache.FindNearestTransferred(transform.position, TableSearchRadius);
        }

        public void DespawnCourierCascade()
        {
            if (_courier != null)
            {
                CourierManager.DespawnCourier(_courier, _nview);
                _courier = null;
            }
        }

        internal Vector3 GetCourierAnchorPosition()
        {
            return CourierManager.GetSpawnPosition(transform);
        }
    }
}
