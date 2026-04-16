using System;
using System.Collections.Generic;
using Bygd.Framework;
using UnityEngine;

namespace Bygd
{
    public class CartHorse : MonoBehaviour, IAISuppressed
    {
        private List<Vector3> m_waypoints;
        private int m_currentWaypoint;
        private MonsterAI m_ai;
        private Vagon m_cart;
        private bool m_arrived;
        private float m_stuckTimer;
        private Vector3 m_lastPosition;
        private float m_repathTimer;
        private Action m_onArrived;

        public static void StartTrip(string fromStation, string toStation, string creatureName = null)
        {
            creatureName = creatureName ?? PrefabNames.Lox;

            if (!BygdPlugin.Stations.TryGetValue(fromStation, out Vector3 fromPos))
            {
                Player.m_localPlayer?.Message(MessageHud.MessageType.Center,
                    $"Station '{fromStation}' not found");
                return;
            }

            if (!BygdPlugin.Stations.TryGetValue(toStation, out Vector3 toPos))
            {
                Player.m_localPlayer?.Message(MessageHud.MessageType.Center,
                    $"Station '{toStation}' not found");
                return;
            }

            List<Vector3> route = RouteGraph.FindRoute(fromPos, toPos);
            if (route == null)
            {
                route = new List<Vector3> { toPos };
                Log.Info("Route via waypoints not found, going directly");
            }
            else
            {
                Log.Info($"Route found: {route.Count} points");
            }

            SpawnAndGo(fromPos, route, creatureName, null);
        }

        public static CartHorse StartTripDirect(Vector3 from, List<Vector3> waypoints, string creatureName, Action onArrived)
        {
            return SpawnAndGo(from, waypoints, creatureName, onArrived);
        }

        public Vagon GetCart() => m_cart;

        private static CartHorse SpawnAndGo(Vector3 from, List<Vector3> waypoints, string creatureName, Action onArrived)
        {
            GameObject creaturePrefab = ZNetScene.instance.GetPrefab(creatureName);
            if (creaturePrefab == null)
            {
                Log.Error($"Prefab '{creatureName}' not found");
                Player.m_localPlayer?.Message(MessageHud.MessageType.Center,
                    $"Creature '{creatureName}' not found");
                return null;
            }

            GameObject creatureObj = Instantiate(creaturePrefab, from + Vector3.up, Quaternion.identity);

            Tameable tameable = creatureObj.GetComponent<Tameable>();
            if (tameable != null)
                Reflect.Tameable_Tame.Invoke(tameable, null);

            GameObject cartPrefab = ZNetScene.instance.GetPrefab(PrefabNames.Cart);
            if (cartPrefab == null)
            {
                Log.Error($"Prefab '{PrefabNames.Cart}' not found");
                Destroy(creatureObj);
                return null;
            }

            Vector3 cartPos = from + creatureObj.transform.forward * -3f + Vector3.up;
            GameObject cartObj = Instantiate(cartPrefab, cartPos, Quaternion.identity);

            Vagon cart = cartObj.GetComponent<Vagon>();
            Character creature = creatureObj.GetComponent<Character>();

            if (cart == null || creature == null)
            {
                Log.Error("Failed to get Vagon/Character components");
                Destroy(creatureObj);
                Destroy(cartObj);
                return null;
            }

            AttachCartToCreature(creature, cart);

            CartHorse driver = creatureObj.AddComponent<CartHorse>();
            driver.m_waypoints = waypoints;
            driver.m_currentWaypoint = 0;
            driver.m_ai = creatureObj.GetComponent<MonsterAI>();
            driver.m_cart = cart;
            driver.m_lastPosition = from;
            driver.m_onArrived = onArrived;

            Player.m_localPlayer?.Message(MessageHud.MessageType.Center,
                $"{creatureName} dispatched! ({waypoints.Count} route points)");

            Log.Info($"{creatureName} spawned at {from}, route: {waypoints.Count} points");
            return driver;
        }

        private static void AttachCartToCreature(Character creature, Vagon cart)
        {
            Rigidbody creatureBody = creature.gameObject.GetComponent<Rigidbody>();
            if (creatureBody == null)
            {
                Log.Error("Creature has no Rigidbody");
                return;
            }

            float radius = creature.GetRadius();
            Vector3 attachOffset = new Vector3(0f, 0.8f, -(radius + 0.5f));

            ConfigurableJoint joint = cart.gameObject.AddComponent<ConfigurableJoint>();
            joint.autoConfigureConnectedAnchor = false;
            joint.anchor = cart.m_attachPoint.localPosition;
            joint.connectedAnchor = attachOffset;
            joint.connectedBody = creatureBody;
            joint.breakForce = cart.m_breakForce;

            joint.xMotion = ConfigurableJointMotion.Limited;
            joint.yMotion = ConfigurableJointMotion.Limited;
            joint.zMotion = ConfigurableJointMotion.Locked;

            SoftJointLimit linearLimit = new SoftJointLimit();
            linearLimit.limit = 0.001f;
            joint.linearLimit = linearLimit;

            SoftJointLimitSpring spring = new SoftJointLimitSpring();
            spring.spring = cart.m_spring;
            spring.damper = cart.m_springDamping;
            joint.linearLimitSpring = spring;

            Reflect.Vagon_m_attachJoin.SetValue(cart, joint);
            cart.m_attachOffset = attachOffset;

            Transform seat = cart.transform.Find("PassengerSeat");
            if (seat == null)
            {
                GameObject seatObj = new GameObject("PassengerSeat");
                seatObj.transform.SetParent(cart.transform);
                seatObj.transform.localPosition = new Vector3(0f, 1.5f, 0f);
            }
        }

        private Vector3 CurrentTarget => m_waypoints[m_currentWaypoint];

        private void Update()
        {
            if (m_arrived || m_ai == null) return;

            float dist = Vector3.Distance(transform.position, CurrentTarget);

            if (dist < 3f)
            {
                m_currentWaypoint++;
                if (m_currentWaypoint >= m_waypoints.Count)
                {
                    m_ai.StopMoving();
                    m_arrived = true;
                    Player.m_localPlayer?.Message(MessageHud.MessageType.Center,
                        "Caravan arrived at the station!");
                    Log.Info("Caravan arrived at destination");
                    m_onArrived?.Invoke();
                    return;
                }

                Log.Info($"Waypoint {m_currentWaypoint}/{m_waypoints.Count}, next: {CurrentTarget}");
            }

            m_stuckTimer += Time.deltaTime;
            if (m_stuckTimer > 5f)
            {
                float moved = Vector3.Distance(transform.position, m_lastPosition);
                if (moved < 0.5f)
                    Log.Info("Caravan stuck, recalculating path...");
                m_lastPosition = transform.position;
                m_stuckTimer = 0f;
            }

            m_repathTimer += Time.deltaTime;
            if (m_repathTimer > 1f)
            {
                Reflect.BaseAI_FindPath.Invoke(m_ai, new object[] { CurrentTarget });
                m_repathTimer = 0f;
            }

            Reflect.BaseAI_MoveTo.Invoke(m_ai, new object[] { Time.deltaTime, CurrentTarget, 0f, false });
        }
    }
}
