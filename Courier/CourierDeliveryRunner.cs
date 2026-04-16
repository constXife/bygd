using System.Collections.Generic;
using Bygd.Framework;
using UnityEngine;

namespace Bygd
{
    /// <summary>
    /// Manages delivery: patrol, mount (boar), courier movement.
    /// Created and owned by CourierPostComponent.
    /// </summary>
    internal class CourierDeliveryRunner
    {
        private readonly CourierPostComponent _post;
        private readonly ZNetView _nview;

        private GameObject _boar;
        private CourierPatrol _patrol;

        public bool IsActive => _patrol != null && _patrol.IsActive;
        public CourierPatrol Patrol => _patrol;

        public CourierDeliveryRunner(CourierPostComponent post, ZNetView nview)
        {
            _post = post;
            _nview = nview;
        }

        // --- Patrol ---

        public void TryStartPatrol(GameObject courier)
        {
            if (courier == null)
                return;

            var mailPosts = MailPostComponent.GetAllMailPosts();
            if (mailPosts.Count == 0)
                return;

            var mount = GetMountForLevel();
            _patrol = CourierPatrol.TryStart(_post, mount);
            if (_patrol == null)
                return;

            if (_patrol.Stops.Count > 0)
            {
                Vector3 target = _patrol.Stops[0].transform.position;
                StartMovingToTarget(courier, target, () => OnCourierArrivedAtStop(courier));

                if (_boar != null)
                    _patrol.SetCourierObject(_boar);
                else
                    _patrol.SetCourierObject(courier);
            }
        }

        public void ForceStartPatrol(GameObject courier, Terminal context)
        {
            if (_patrol != null && _patrol.IsActive)
            {
                context.AddString("  Courier already on patrol");
                return;
            }

            if (courier == null)
            {
                context.AddString("  No courier");
                return;
            }

            var mailPosts = MailPostComponent.GetAllMailPosts();
            context.AddString($"  MailPosts available: {mailPosts.Count}");

            var mount = GetMountForLevel();
            context.AddString($"  Transport: {(MountConfigs.IsOnFoot(mount) ? "on foot" : mount.PrefabName)}, slots: {mount.CargoSlots}");
            _patrol = CourierPatrol.TryStartForced(_post, mount);
            if (_patrol != null)
            {
                if (_patrol.Stops.Count > 0)
                {
                    Vector3 target = _patrol.Stops[0].transform.position;
                    StartMovingToTarget(courier, target, () => OnCourierArrivedAtStop(courier));

                    if (_boar != null)
                        _patrol.SetCourierObject(_boar);
                    else
                        _patrol.SetCourierObject(courier);
                }
                string transport = MountConfigs.IsOnFoot(mount) ? "on foot" : $"on {mount.PrefabName}";
                context.AddString($"  Patrol started: {_patrol.Stops.Count} stops, {transport}");
            }
            else
            {
                context.AddString("  Patrol failed to start (no MailPost / no budget?)");
            }
        }

        public bool RequestDelivery(GameObject courier, MailPostComponent targetMailPost)
        {
            if (courier == null || IsActive)
                return false;

            var mount = GetMountForLevel();
            var stops = new List<MailPostComponent> { targetMailPost };
            _patrol = CourierPatrol.TryStartDirect(_post, mount, stops);
            if (_patrol == null)
                return false;

            Vector3 target = targetMailPost.transform.position;
            StartMovingToTarget(courier, target, () => OnCourierArrivedAtStop(courier));

            if (_boar != null)
                _patrol.SetCourierObject(_boar);
            else
                _patrol.SetCourierObject(courier);

            Log.Info($"Delivery on request: courier heading to @{targetMailPost.GetStationKey()}");
            return true;
        }

        // --- Movement ---

        private void StartMovingToTarget(GameObject courier, Vector3 target, System.Action onArrived)
        {
            if (_patrol == null)
                return;

            if (MountConfigs.IsOnFoot(_patrol.Mount))
            {
                CourierWalker.StartWalking(courier, target, onArrived);
            }
            else
            {
                StartBoarTrip(courier, target, onArrived);
            }
        }

        private void OnCourierArrivedAtStop(GameObject courier)
        {
            if (_patrol == null || !_patrol.IsActive || courier == null)
                return;

            _patrol.ProcessCurrentStop();
            _patrol.AdvanceToNextStop();

            if (_patrol.IsReturningHome && _patrol.IsActive)
            {
                var table = _post.FindParentTable();
                Container outpostChest = table != null
                    ? OutpostChestCollector.FindLinkedChestFor(table)
                    : null;

                if (outpostChest != null && !_patrol.Bag.IsEmpty)
                {
                    StartMovingToTarget(courier, outpostChest.transform.position, () =>
                    {
                        _patrol.FinishPatrol();
                        Log.Info("Courier unloaded parcels into chest");
                        GoHome(courier);
                    });
                }
                else
                {
                    _patrol.FinishPatrol();
                    GoHome(courier);
                }
                return;
            }

            if (!_patrol.IsActive)
            {
                FinishMovement(courier);
                return;
            }

            Vector3 nextTarget = _patrol.CurrentStopIndex < _patrol.Stops.Count
                ? _patrol.Stops[_patrol.CurrentStopIndex].transform.position
                : _post.transform.position;

            StartMovingToTarget(courier, nextTarget, () => OnCourierArrivedAtStop(courier));
        }

        private void GoHome(GameObject courier)
        {
            StartMovingToTarget(courier, _post.transform.position, () => FinishMovement(courier));
        }

        private void FinishMovement(GameObject courier)
        {
            if (courier != null)
            {
                var walker = courier.GetComponent<CourierWalker>();
                if (walker != null)
                    Object.Destroy(walker);
            }

            CleanupBoar();
        }

        // --- Boar mount ---

        private void SpawnBoar(GameObject courier)
        {
            if (_boar != null)
                return;

            var prefab = ZNetScene.instance?.GetPrefab(_patrol.Mount.PrefabName);
            if (prefab == null)
            {
                Log.Error($"Prefab not found: {_patrol.Mount.PrefabName}");
                return;
            }

            Vector3 spawnPos = courier.transform.position + courier.transform.forward * 2f;
            _boar = Object.Instantiate(prefab, spawnPos, Quaternion.identity);

            var tameable = _boar.GetComponent<Tameable>();
            if (tameable != null)
                Reflect.Tameable_Tame.Invoke(tameable, null);

            var procreation = _boar.GetComponent<Procreation>();
            if (procreation != null)
                Object.Destroy(procreation);

            _boar.name = PrefabNames.CourierBoar;
            var boarNview = _boar.GetComponent<ZNetView>();
            if (boarNview != null)
                OutpostTransferState.WriteZdoString(boarNview, "bygd_courier_boar", "1");

            Log.Info("Courier boar spawned");
        }

        private void StartBoarTrip(GameObject courier, Vector3 target, System.Action onArrived)
        {
            SpawnBoar(courier);

            if (_boar == null)
            {
                CourierWalker.StartWalking(courier, target, onArrived);
                return;
            }

            var walker = CourierWalker.StartWalking(_boar, target, onArrived);
            walker.ShouldRun = _patrol != null && _patrol.Bag.IsEmpty;
        }

        public void CleanupBoar()
        {
            if (_boar != null)
            {
                var walker = _boar.GetComponent<CourierWalker>();
                if (walker != null)
                    Object.Destroy(walker);

                ZNetScene.instance.Destroy(_boar);
                _boar = null;
            }
        }

        // --- Transport selection ---

        private MountConfig GetMountForLevel()
        {
            var table = _post.FindParentTable();
            if (table == null)
                return MountConfigs.OnFoot;

            int level = OutpostResources.GetLevel(table.GetComponent<ZNetView>());

            if (level >= 3)
                return MountConfigs.Boar;

            return MountConfigs.OnFoot;
        }

        // --- Status ---

        public bool IsOnCooldown()
        {
            return _patrol != null && !_patrol.IsActive && _patrol.IsOnCooldown();
        }

        public double GetCooldownRemaining()
        {
            if (_patrol == null || _patrol.IsActive)
                return 0;

            double now = ZNet.instance.GetTimeSeconds();
            double remaining = _patrol.NextPatrolTime - now;
            return remaining > 0 ? remaining : 0;
        }

        public void Reset()
        {
            _patrol = null;
            CleanupBoar();
        }
    }
}
