using System.Collections.Generic;
using Bygd.Framework;
using UnityEngine;

namespace Bygd
{
    public class MailPostComponent : MonoBehaviour, Hoverable, Interactable
    {
        private const float ChestSearchRadius = 3f;
        private const float SignSearchRadius = 3f;

        private ZNetView _nview;
        private string _stationKey;

        // --- Hoverable / Interactable ---

        public string GetHoverText()
        {
            string pieceName = Localization.instance.Localize("$piece_mailpost");

            if (string.IsNullOrEmpty(_stationKey))
                return $"{pieceName}\n{Localization.instance.Localize("$mailpost_needs_sign")}";

            bool hasMail = HasPendingMail();
            string status = Localization.instance.Localize(hasMail ? "$mailpost_has_mail" : "$mailpost_empty");

            string courierInfo = GetNearestCourierInfo();
            return $"{pieceName}: @{_stationKey} ({status}){courierInfo}";
        }

        private string GetNearestCourierInfo()
        {
            // Search for active patrols
            var patrols = CourierPatrol.GetActivePatrols();
            foreach (var patrol in patrols)
            {
                if (patrol.CourierObject == null)
                    continue;

                float dist = Vector3.Distance(patrol.CourierObject.transform.position, transform.position);
                if (patrol.IsReturningHome)
                    return $"\nCourier: {dist:F0}m (-> home)";

                int stop = patrol.CurrentStopIndex;
                int total = patrol.Stops.Count;
                return $"\nCourier: {dist:F0}m (-> stop {stop + 1}/{total})";
            }

            // No active patrol — check cooldown
            foreach (var post in Object.FindObjectsOfType<CourierPostComponent>())
            {
                if (post == null)
                    continue;

                double remaining = post.GetCooldownRemaining();
                if (remaining > 0)
                {
                    int min = (int)(remaining / 60);
                    int sec = (int)(remaining % 60);
                    return $"\nCourier resting ({min}:{sec:D2})";
                }
            }

            return "";
        }

        public string GetHoverName()
        {
            return Localization.instance.Localize("$piece_mailpost");
        }

        public bool Interact(Humanoid user, bool hold, bool alt)
        {
            if (hold)
                return false;

            RefreshStationName();

            var chest = FindLinkedChest();
            var sign = FindLinkedSign();

            if (sign == null || string.IsNullOrEmpty(_stationKey))
            {
                user?.Message(MessageHud.MessageType.Center,
                    Localization.instance.Localize("$mailpost_needs_sign"));
                return true;
            }

            if (chest == null)
            {
                user?.Message(MessageHud.MessageType.Center,
                    Localization.instance.Localize("$mailpost_needs_chest"));
                return true;
            }

            string destination = GetDestination(sign);
            int itemCount = chest.GetInventory().NrOfItems();

            if (string.IsNullOrEmpty(destination))
            {
                if (itemCount == 0)
                    user?.Message(MessageHud.MessageType.Center,
                        $"@{_stationKey}: empty. Write address: @{_stationKey} -> @Destination");
                else
                    user?.Message(MessageHud.MessageType.Center,
                        $"@{_stationKey}: {itemCount} items in chest. Set destination on the sign.");
                return true;
            }

            if (itemCount == 0)
            {
                user?.Message(MessageHud.MessageType.Center,
                    string.Format(Localization.instance.Localize("$mailpost_chest_empty"), destination));
                return true;
            }

            // Call the courier
            var courierPost = FindNearestCourierPost();
            if (courierPost == null)
            {
                user?.Message(MessageHud.MessageType.Center,
                    Localization.instance.Localize("$mailpost_no_courier"));
                return true;
            }

            if (courierPost.IsDelivering())
            {
                user?.Message(MessageHud.MessageType.Center,
                    Localization.instance.Localize("$mailpost_courier_busy"));
                return true;
            }

            bool sent = courierPost.RequestDelivery(this);
            if (sent)
                user?.Message(MessageHud.MessageType.Center,
                    string.Format(Localization.instance.Localize("$mailpost_sent"), itemCount, destination));
            else
                user?.Message(MessageHud.MessageType.Center,
                    Localization.instance.Localize("$mailpost_send_failed"));

            return true;
        }

        public bool UseItem(Humanoid user, ItemDrop.ItemData item)
        {
            return false;
        }

        // --- Lifecycle ---

        void Awake()
        {
            _nview = GetComponent<ZNetView>();
        }

        void Start()
        {
            RefreshStationName();
        }

        void OnDestroy()
        {
            UnregisterStation();
        }

        /// <summary>
        /// Updates station name from a nearby sign. Called periodically.
        /// </summary>
        public void RefreshStationName()
        {
            var sign = FindLinkedSign();
            string newName = GetStationName(sign);

            if (string.IsNullOrEmpty(newName))
            {
                UnregisterStation();
                return;
            }

            string newKey = newName;
            if (_stationKey == newKey)
                return;

            UnregisterStation();
            _stationKey = newKey;
            BygdPlugin.Stations[_stationKey] = transform.position;
            Log.Diag($"MailPost registered as station: @{_stationKey}");
        }

        private void UnregisterStation()
        {
            if (_stationKey == null)
                return;

            BygdPlugin.Stations.Remove(_stationKey);
            _stationKey = null;
        }

        // --- Mail detection ---

        public bool HasPendingMail()
        {
            var chest = FindLinkedChest();
            if (chest == null)
                return false;

            if (chest.GetInventory().NrOfItems() == 0)
                return false;

            var sign = FindLinkedSign();
            if (sign == null)
                return false;

            string dest = GetDestination(sign);
            return !string.IsNullOrEmpty(dest);
        }

        public string GetStationKey()
        {
            return _stationKey;
        }

        // --- Chest / Sign linking ---

        public Container FindLinkedChest()
        {
            Container closest = null;
            float closestDist = ChestSearchRadius;

            foreach (var container in Object.FindObjectsOfType<Container>())
            {
                if (container == null || container.gameObject == gameObject)
                    continue;

                float dist = Vector3.Distance(container.transform.position, transform.position);
                if (dist < closestDist)
                {
                    closest = container;
                    closestDist = dist;
                }
            }

            return closest;
        }

        public Sign FindLinkedSign()
        {
            Sign closest = null;
            float closestDist = SignSearchRadius;

            foreach (var sign in Object.FindObjectsOfType<Sign>())
            {
                if (sign == null)
                    continue;

                float dist = Vector3.Distance(sign.transform.position, transform.position);
                if (dist < closestDist)
                {
                    closest = sign;
                    closestDist = dist;
                }
            }

            return closest;
        }

        /// <summary>
        /// Parses sign text. Format: "@Name" or "@Name -> @Address"
        /// </summary>
        public static void ParseSign(Sign sign, out string name, out string destination)
        {
            name = null;
            destination = null;

            if (sign == null)
                return;

            string text = sign.GetText();
            if (string.IsNullOrEmpty(text))
                return;

            text = text.Trim();
            if (!text.StartsWith("@"))
                return;

            int arrowIdx = text.IndexOf("->");
            if (arrowIdx < 0)
            {
                // Name only: "@Mine"
                name = text.Substring(1).Trim();
                return;
            }

            // Name + address: "@Mine -> @Outpost"
            name = text.Substring(1, arrowIdx - 1).Trim();
            string destPart = text.Substring(arrowIdx + 2).Trim();
            if (destPart.StartsWith("@"))
                destPart = destPart.Substring(1);
            destination = destPart.Trim();
        }

        public static string GetDestination(Sign sign)
        {
            ParseSign(sign, out _, out string destination);
            return destination;
        }

        public static string GetStationName(Sign sign)
        {
            ParseSign(sign, out string name, out _);
            return name;
        }

        /// <summary>
        /// Clears delivery address, keeping the station name: "@Mine -> @Outpost" -> "@Mine"
        /// </summary>
        public static void ClearSignDestination(Sign sign)
        {
            if (sign == null)
                return;

            ParseSign(sign, out string name, out _);
            if (!string.IsNullOrEmpty(name))
                sign.SetText("@" + name);
            else
                sign.SetText(string.Empty);
        }

        private static CourierPostComponent FindNearestCourierPost()
        {
            CourierPostComponent closest = null;
            float closestDist = float.MaxValue;

            foreach (var post in Object.FindObjectsOfType<CourierPostComponent>())
            {
                if (post == null)
                    continue;
                // Any courier post — for now take the nearest one
                float dist = Player.m_localPlayer != null
                    ? Vector3.Distance(Player.m_localPlayer.transform.position, post.transform.position)
                    : 0f;
                if (dist < closestDist)
                {
                    closest = post;
                    closestDist = dist;
                }
            }

            return closest;
        }

        // --- Static: find all mail posts ---

        public static List<MailPostComponent> GetAllMailPosts()
        {
            var result = new List<MailPostComponent>();
            foreach (var post in Object.FindObjectsOfType<MailPostComponent>())
            {
                if (post != null)
                    result.Add(post);
            }
            return result;
        }

        public static MailPostComponent FindByStationKey(string stationKey)
        {
            foreach (var post in Object.FindObjectsOfType<MailPostComponent>())
            {
                if (post != null && post._stationKey == stationKey)
                    return post;
            }
            return null;
        }

        public static MailPostComponent FindNearestToStation(string stationName)
        {
            if (!BygdPlugin.Stations.TryGetValue(stationName, out Vector3 stationPos))
                return null;

            MailPostComponent closest = null;
            float closestDist = 20f;

            foreach (var post in Object.FindObjectsOfType<MailPostComponent>())
            {
                if (post == null)
                    continue;

                float dist = Vector3.Distance(post.transform.position, stationPos);
                if (dist < closestDist)
                {
                    closest = post;
                    closestDist = dist;
                }
            }

            return closest;
        }
    }
}
