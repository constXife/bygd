using System.Collections.Generic;
using Bygd.Framework;

namespace Bygd
{
    internal class MailItem
    {
        public string Destination;
        public List<ItemDrop.ItemData> Items = new List<ItemDrop.ItemData>();
    }

    internal class MailBag
    {
        private readonly List<MailItem> _items = new List<MailItem>();
        private readonly int _maxSlots;
        private int _usedSlots;

        public MailBag(int maxSlots)
        {
            _maxSlots = maxSlots;
        }

        public int Count => _items.Count;
        public int UsedSlots => _usedSlots;
        public bool IsEmpty => _items.Count == 0;
        public bool IsFull => _usedSlots >= _maxSlots;

        /// <summary>
        /// Picks up a parcel from a chest. Searches for food as payment — the first stack with m_food > 0,
        /// takes 1 unit, converts to calories. The rest is the parcel.
        /// Returns payment calories (0 = payment not found, parcel not picked up).
        /// </summary>
        public int PickupFrom(Container chest, string destination)
        {
            if (IsFull)
            {
                Log.Diag($"Mail to @{destination}: bag full ({_usedSlots}/{_maxSlots})");
                return 0;
            }

            var inventory = chest.GetInventory();
            if (inventory.NrOfItems() == 0)
                return 0;

            // Search for payment — first food stack
            int paymentCalories = 0;
            ItemDrop.ItemData paymentItem = null;

            foreach (var item in inventory.GetAllItems())
            {
                if (item.m_shared.m_food > 0)
                {
                    paymentItem = item;
                    paymentCalories = (int)item.m_shared.m_food;
                    break;
                }
            }

            if (paymentItem == null)
            {
                Log.Info($"Mail to @{destination}: no payment in chest");
                return 0;
            }

            // Take 1 unit of payment
            if (paymentItem.m_stack > 1)
                paymentItem.m_stack--;
            else
                inventory.RemoveItem(paymentItem);

            // Take as many stacks as will fit
            int slotsAvailable = _maxSlots - _usedSlots;
            var mailItem = new MailItem { Destination = destination };
            int taken = 0;

            foreach (var item in inventory.GetAllItems())
            {
                if (taken >= slotsAvailable)
                    break;
                mailItem.Items.Add(item.Clone());
                taken++;
            }

            // Remove picked up items
            for (int i = 0; i < taken; i++)
            {
                var items = inventory.GetAllItems();
                if (items.Count > 0)
                    inventory.RemoveItem(items[0]);
            }

            _items.Add(mailItem);
            _usedSlots += taken;

            Log.Info($"Mail picked up: {taken} stacks -> @{destination} (bag: {_usedSlots}/{_maxSlots}), payment={paymentCalories} calories");
            return paymentCalories;
        }

        public int DeliverTo(Container chest, string stationName)
        {
            var delivered = new List<MailItem>();
            int totalItems = 0;

            foreach (var mail in _items)
            {
                if (mail.Destination != stationName)
                    continue;

                var inventory = chest.GetInventory();
                foreach (var item in mail.Items)
                {
                    if (inventory.AddItem(item))
                        totalItems++;
                }

                delivered.Add(mail);
            }

            foreach (var mail in delivered)
                _items.Remove(mail);

            if (totalItems > 0)
                Log.Info($"Mail delivered to @{stationName}: {totalItems} items");

            return totalItems;
        }

        public bool HasMailFor(string stationName)
        {
            foreach (var mail in _items)
            {
                if (mail.Destination == stationName)
                    return true;
            }
            return false;
        }

        public int DeliverAll(Container chest)
        {
            int totalItems = 0;
            var inventory = chest.GetInventory();

            foreach (var mail in _items)
            {
                foreach (var item in mail.Items)
                {
                    if (inventory.AddItem(item))
                        totalItems++;
                }
            }

            _items.Clear();

            if (totalItems > 0)
                Log.Info($"Mail unloaded (all): {totalItems} items");

            return totalItems;
        }

        public void Clear()
        {
            _items.Clear();
        }
    }
}
