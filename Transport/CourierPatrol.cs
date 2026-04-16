using System.Collections.Generic;
using Bygd.Framework;
using UnityEngine;

namespace Bygd
{
    internal class CourierPatrol
    {
        private const float CooldownSeconds = 300f; // 5 min rest between patrols
        private const float BaseSpeed = 3f;
        private const float CaloriesPerMeter = 0.1f; // 10 calories per 100m

        private static readonly List<CourierPatrol> s_activePatrols = new List<CourierPatrol>();

        public static List<CourierPatrol> GetActivePatrols() => s_activePatrols;

        public CourierPostComponent Home;
        public MountConfig Mount;
        public MailBag Bag;

        // Route
        public List<MailPostComponent> Stops = new List<MailPostComponent>();
        public int CurrentStopIndex;
        public bool IsActive;
        public bool IsReturningHome;

        // Simulated mode
        public double LegStartTime;
        public double LegETA;
        public Vector3 LegFrom;
        public Vector3 LegTo;

        // Visual mode
        public GameObject CourierObject;

        // Cooldown
        public double NextPatrolTime;

        // Calorie cost for this patrol
        private int _patrolCost;

        public static CourierPatrol TryStartForced(CourierPostComponent home, MountConfig mount)
        {
            return DoTryStart(home, mount);
        }

        public static CourierPatrol TryStartDirect(CourierPostComponent home, MountConfig mount, List<MailPostComponent> stops)
        {
            if (stops == null || stops.Count == 0)
                return null;

            float totalDist = CalculatePatrolDistance(home.transform.position, stops);
            int requiredCalories = (int)(totalDist * CaloriesPerMeter);

            var postNview = home.GetComponent<ZNetView>();
            int budget = CourierBinding.GetBudget(postNview);
            if (budget < requiredCalories)
            {
                var table = home.FindParentTable();
                if (table != null)
                {
                    var tableNview = table.GetComponent<ZNetView>();
                    int outpostCalories = OutpostResources.GetCalories(tableNview);
                    if (outpostCalories >= requiredCalories)
                    {
                        OutpostResources.SetCalories(tableNview, outpostCalories - requiredCalories);
                        CourierBinding.SetBudget(postNview, requiredCalories);
                        budget = requiredCalories;
                    }
                }
            }

            if (budget < requiredCalories)
            {
                Log.Info($"Courier lacks calories: {budget}/{requiredCalories}");
                return null;
            }

            double now = ZNet.instance.GetTimeSeconds();
            var patrol = new CourierPatrol
            {
                Home = home,
                Mount = mount,
                Bag = new MailBag(mount.CargoSlots),
                Stops = stops,
                CurrentStopIndex = 0,
                IsActive = true,
                IsReturningHome = false,
                NextPatrolTime = 0,
                _patrolCost = requiredCalories,
            };

            patrol.StartLeg(now, home.transform.position, stops[0].transform.position);
            s_activePatrols.Add(patrol);
            Log.Info($"Delivery started: {stops.Count} stops, cost={requiredCalories} calories");
            return patrol;
        }

        public static CourierPatrol TryStart(CourierPostComponent home, MountConfig mount)
        {
            if (EnvMan.instance != null && !EnvMan.IsDay())
                return null;

            return DoTryStart(home, mount);
        }

        private static CourierPatrol DoTryStart(CourierPostComponent home, MountConfig mount)
        {

            double now = ZNet.instance.GetTimeSeconds();

            var allMailPosts = MailPostComponent.GetAllMailPosts();
            Log.Info($"DoTryStart: total MailPosts={allMailPosts.Count}");

            var stops = CollectReachableMailPosts(home.transform.position);
            if (stops.Count == 0)
            {
                Log.Info("DoTryStart: no available MailPosts");
                return null;
            }

            // Nearest-neighbor route
            SortByNearestNeighbor(stops, home.transform.position);

            // Calculate route distance (round trip)
            float totalDist = CalculatePatrolDistance(home.transform.position, stops);
            int requiredCalories = (int)(totalDist * CaloriesPerMeter);

            // Check budget — if empty, try to take from outpost
            var postNview = home.GetComponent<ZNetView>();
            int budget = CourierBinding.GetBudget(postNview);
            if (budget < requiredCalories)
            {
                var table = home.FindParentTable();
                if (table != null)
                {
                    var tableNview = table.GetComponent<ZNetView>();
                    int outpostCalories = OutpostResources.GetCalories(tableNview);
                    if (outpostCalories >= requiredCalories)
                    {
                        OutpostResources.SetCalories(tableNview, outpostCalories - requiredCalories);
                        CourierBinding.SetBudget(postNview, requiredCalories);
                        budget = requiredCalories;
                        Log.Info($"Outpost allocated {requiredCalories} calories for patrol");
                    }
                }
            }

            if (budget < requiredCalories)
            {
                Log.Info($"Courier lacks calories: {budget}/{requiredCalories}");
                return null;
            }

            var patrol = new CourierPatrol
            {
                Home = home,
                Mount = mount,
                Bag = new MailBag(mount.CargoSlots),
                Stops = stops,
                CurrentStopIndex = 0,
                IsActive = true,
                IsReturningHome = false,
                NextPatrolTime = 0,
                _patrolCost = requiredCalories,
            };

            patrol.StartLeg(now, home.transform.position, stops[0].transform.position);
            s_activePatrols.Add(patrol);
            Log.Info($"Patrol started: {stops.Count} stops, distance={totalDist:F0}m, cost={requiredCalories} calories");
            return patrol;
        }

        private static float CalculatePatrolDistance(Vector3 home, List<MailPostComponent> stops)
        {
            float total = 0f;
            Vector3 current = home;

            foreach (var stop in stops)
            {
                total += Vector3.Distance(current, stop.transform.position);
                current = stop.transform.position;
            }

            // Return path
            total += Vector3.Distance(current, home);
            return total;
        }

        public void CheckProgress()
        {
            if (!IsActive)
                return;

            double now = ZNet.instance.GetTimeSeconds();

            if (now < LegETA)
                return;

            // Arrived at point
            if (IsReturningHome)
            {
                OnReturnedHome();
                return;
            }

            // Process current stop
            if (CurrentStopIndex < Stops.Count)
            {
                var stop = Stops[CurrentStopIndex];
                ProcessStop(stop);
            }

            // Next stop or home
            CurrentStopIndex++;
            if (CurrentStopIndex >= Stops.Count)
            {
                // All stops visited — heading home
                IsReturningHome = true;
                Vector3 from = Stops[Stops.Count - 1].transform.position;
                Vector3 to = Home.transform.position;
                StartLeg(now, from, to);
                Log.Info("Patrol: all stops visited, returning home");
            }
            else
            {
                Vector3 from = Stops[CurrentStopIndex - 1].transform.position;
                Vector3 to = Stops[CurrentStopIndex].transform.position;
                StartLeg(now, from, to);
            }
        }

        private void ProcessStop(MailPostComponent stop)
        {
            if (stop == null)
                return;

            // Delivery: search all stations within 5m of this MailPost
            var chest = stop.FindLinkedChest();
            if (chest != null)
            {
                // By station key of this MailPost
                string stationKey = stop.GetStationKey();
                if (stationKey != null)
                    Bag.DeliverTo(chest, stationKey);

                // By @-station names nearby (outpost, player base, etc)
                foreach (var kvp in BygdPlugin.Stations)
                {
                    float dist = Vector3.Distance(kvp.Value, stop.transform.position);
                    if (dist < 5f)
                        Bag.DeliverTo(chest, kvp.Key);
                }
            }

            // Pickup: if there's a parcel (chest + sign with @address)
            if (stop.HasPendingMail())
            {
                var pickupChest = stop.FindLinkedChest();
                var sign = stop.FindLinkedSign();

                if (pickupChest != null && sign != null)
                {
                    string dest = MailPostComponent.GetDestination(sign);
                    if (!string.IsNullOrEmpty(dest))
                    {
                        int payment = Bag.PickupFrom(pickupChest, dest);
                        if (payment > 0)
                        {
                            MailPostComponent.ClearSignDestination(sign);

                            // Payment -> to courier budget
                            var postNview = Home.GetComponent<ZNetView>();
                            CourierBinding.AddBudget(postNview, payment);
                            Log.Info($"Payment received: +{payment} calories to courier budget");
                        }
                    }
                }
            }
        }

        private void OnReturnedHome()
        {
            IsActive = false;
            IsReturningHome = false;
            s_activePatrols.Remove(this);

            // Unload mail into outpost home chest
            if (!Bag.IsEmpty)
            {
                var table = Home.FindParentTable();
                if (table != null)
                {
                    var outpostChest = FindOutpostLinkedChest(table);
                    if (outpostChest != null)
                    {
                        // First by exact addresses
                        var tableNview = table.GetComponent<ZNetView>();
                        string tableKey = OutpostSettlerBinding.GetTableKey(tableNview, table.transform);
                        Bag.DeliverTo(outpostChest, "outpost_" + tableKey);

                        foreach (var kvp in BygdPlugin.Stations)
                        {
                            float dist = Vector3.Distance(kvp.Value, table.transform.position);
                            if (dist < 5f)
                                Bag.DeliverTo(outpostChest, kvp.Key);
                        }

                        // Everything remaining — goes here too (courier brought home)
                        if (!Bag.IsEmpty)
                        {
                            int dumped = Bag.DeliverAll(outpostChest);
                            Log.Info($"Courier unloaded {dumped} unrecognized parcels into home chest");
                        }
                    }
                }
            }

            // Calculation: route cost, remainder -> to village
            var postNview = Home.GetComponent<ZNetView>();
            int budget = CourierBinding.GetBudget(postNview);
            int profit = budget - _patrolCost;

            // Fuel tank resets to zero
            CourierBinding.SetBudget(postNview, 0);

            // Profit -> to outpost calories
            if (profit > 0)
            {
                var table = Home.FindParentTable();
                if (table != null)
                {
                    var tableNview = table.GetComponent<ZNetView>();
                    int currentCalories = OutpostResources.GetCalories(tableNview);
                    OutpostResources.SetCalories(tableNview, currentCalories + profit);
                    Log.Info($"Courier brought to village: +{profit} calories");
                }
            }

            NextPatrolTime = ZNet.instance.GetTimeSeconds() + CooldownSeconds;
            Log.Info($"Patrol completed, cost={_patrolCost}, profit={profit}, in bag: {Bag.Count}");

            if (Player.m_localPlayer != null)
                Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft,
                    Localization.instance.Localize("$courier_patrol_done"));
        }

        private static Container FindOutpostLinkedChest(OutpostTableComponent table)
        {
            string tableId = OutpostSettlerBinding.GetObjectZdoId(table.gameObject);
            if (string.IsNullOrEmpty(tableId))
                return null;

            foreach (var container in Object.FindObjectsOfType<Container>())
            {
                if (container == null || container.gameObject == table.gameObject)
                    continue;

                var chestNview = container.GetComponent<ZNetView>();
                string linkedId = OutpostTransferState.ReadZdoString(chestNview, "bygd_chest_table_id");
                if (linkedId == tableId)
                    return container;
            }

            return null;
        }

        public void FinishPatrol()
        {
            OnReturnedHome();
        }

        public void SetCourierObject(GameObject courier)
        {
            CourierObject = courier;
        }

        public void ProcessCurrentStop()
        {
            if (CurrentStopIndex < Stops.Count)
                ProcessStop(Stops[CurrentStopIndex]);
        }

        public void AdvanceToNextStop()
        {
            CurrentStopIndex++;
            if (CurrentStopIndex >= Stops.Count)
            {
                IsReturningHome = true;
            }
        }

        public Vector3 GetEstimatedPosition()
        {
            float progress = GetProgress();
            return Vector3.Lerp(LegFrom, LegTo, progress);
        }

        public bool IsOnCooldown()
        {
            return !IsActive && NextPatrolTime > ZNet.instance.GetTimeSeconds();
        }

        public float GetProgress()
        {
            if (!IsActive)
                return 0f;

            double now = ZNet.instance.GetTimeSeconds();
            if (LegETA <= LegStartTime)
                return 1f;

            return Mathf.Clamp01((float)((now - LegStartTime) / (LegETA - LegStartTime)));
        }

        private void StartLeg(double now, Vector3 from, Vector3 to)
        {
            float dist = Vector3.Distance(from, to);
            float speed = BaseSpeed * Mount.GetSpeedWithLoad(Bag.UsedSlots);
            float duration = dist / speed;

            LegFrom = from;
            LegTo = to;
            LegStartTime = now;
            LegETA = now + duration;
        }

        // --- Route building ---

        private static List<MailPostComponent> CollectReachableMailPosts(Vector3 origin)
        {
            var result = new List<MailPostComponent>();

            foreach (var post in MailPostComponent.GetAllMailPosts())
            {
                if (post == null)
                    continue;

                // All MailPosts are reachable — courier goes directly if no route via RouteGraph
                result.Add(post);
            }

            return result;
        }

        private static void SortByNearestNeighbor(List<MailPostComponent> stops, Vector3 start)
        {
            var sorted = new List<MailPostComponent>();
            var remaining = new List<MailPostComponent>(stops);
            Vector3 current = start;

            while (remaining.Count > 0)
            {
                MailPostComponent nearest = null;
                float nearestDist = float.MaxValue;

                foreach (var stop in remaining)
                {
                    float dist = Vector3.Distance(current, stop.transform.position);
                    if (dist < nearestDist)
                    {
                        nearest = stop;
                        nearestDist = dist;
                    }
                }

                sorted.Add(nearest);
                remaining.Remove(nearest);
                current = nearest.transform.position;
            }

            stops.Clear();
            stops.AddRange(sorted);
        }
    }
}
