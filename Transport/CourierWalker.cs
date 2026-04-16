using System;
using System.Collections.Generic;
using Bygd.Framework;
using UnityEngine;

namespace Bygd
{
    public class CourierWalker : MonoBehaviour
    {
        private const float StuckTimeout = 15f; // if stuck for 15 sec — teleport

        private List<Vector3> _waypoints;
        private int _currentWaypoint;
        private MonsterAI _ai;
        private bool _arrived;
        private float _repathTimer;
        private float _stuckTimer;
        private float _totalStuckTime;
        private Vector3 _lastPosition;
        private Action _onArrived;
        public bool ShouldRun;

        public static CourierWalker StartWalking(GameObject courier, Vector3 target, Action onArrived)
        {
            return StartWalkingMulti(courier, new List<Vector3> { target }, onArrived);
        }

        public static CourierWalker StartWalkingMulti(GameObject courier, List<Vector3> waypoints, Action onArrived)
        {
            // Remove AI suppression for walking duration
            var walker = courier.GetComponent<CourierWalker>();
            if (walker != null)
                Destroy(walker);

            // Suppress standard AI for walking duration
            if (courier.GetComponent<IAISuppressed>() == null)
                courier.AddComponent<AISuppressionMarker>();

            walker = courier.AddComponent<CourierWalker>();
            walker._waypoints = waypoints;
            walker._currentWaypoint = 0;
            walker._ai = courier.GetComponent<MonsterAI>();
            walker._lastPosition = courier.transform.position;
            walker._onArrived = onArrived;

            // Allow movement — remove IAISuppressed effect
            if (walker._ai != null)
                Reflect.BaseAI_StopMoving.Invoke(walker._ai, null);

            Log.Info($"CourierWalker: start, {waypoints.Count} points");
            return walker;
        }

        public bool HasArrived => _arrived;

        private Vector3 CurrentTarget => _waypoints[_currentWaypoint];

        void Update()
        {
            if (_arrived || _ai == null)
                return;

            float dist = Vector3.Distance(transform.position, CurrentTarget);

            if (dist < 3f)
            {
                _currentWaypoint++;
                if (_currentWaypoint >= _waypoints.Count)
                {
                    Reflect.BaseAI_StopMoving.Invoke(_ai, null);
                    _arrived = true;
                    Log.Info("CourierWalker: arrived");
                    _onArrived?.Invoke();
                    return;
                }
            }

            // Stuck detection
            _stuckTimer += Time.deltaTime;
            if (_stuckTimer > 5f)
            {
                float moved = Vector3.Distance(transform.position, _lastPosition);
                if (moved < 0.3f)
                {
                    _totalStuckTime += _stuckTimer;
                    Log.Info($"CourierWalker: stuck ({_totalStuckTime:F0}sec)");

                    if (_totalStuckTime >= StuckTimeout)
                    {
                        Log.Info("CourierWalker: timeout — teleporting to target");
                        transform.position = CurrentTarget;
                        _totalStuckTime = 0f;
                    }
                }
                else
                {
                    _totalStuckTime = 0f;
                }
                _lastPosition = transform.position;
                _stuckTimer = 0f;
            }

            // Pathfinding
            _repathTimer += Time.deltaTime;
            if (_repathTimer > 1f)
            {
                Reflect.BaseAI_FindPath.Invoke(_ai, new object[] { CurrentTarget });
                _repathTimer = 0f;
            }

            // Movement
            Reflect.BaseAI_MoveTo.Invoke(_ai, new object[] { Time.deltaTime, CurrentTarget, 0f, ShouldRun });
        }

        void OnDestroy()
        {
            if (_ai != null)
                Reflect.BaseAI_StopMoving.Invoke(_ai, null);

            // Remove suppression marker if we added it
            var marker = gameObject.GetComponent<AISuppressionMarker>();
            if (marker != null)
                Destroy(marker);
        }
    }
}
