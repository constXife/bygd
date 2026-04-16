using UnityEngine;

namespace Bygd
{
    public class CourierNPC : BaseNPC
    {
        protected override string DisplayName => "Courier";
        protected override string ObjectName => "OutpostCourier";
        protected override string NameLocKey => "$courier_name";
        protected override string TalkLocKey => "$courier_talk";

        protected override string GetContextualLine()
        {
            var post = FindNearestPost();
            if (post == null)
                return Localization.instance.Localize("$courier_idle");

            var table = post.FindParentTable();
            if (table == null)
                return Localization.instance.Localize("$courier_idle");

            var tableNview = table.GetComponent<ZNetView>();
            if (!OutpostTransferState.IsTransferred(tableNview))
                return Localization.instance.Localize("$courier_idle");

            int wood = OutpostResources.GetWood(tableNview);
            int calories = OutpostResources.GetCalories(tableNview);

            if (wood <= 0 && calories <= 0)
                return Localization.instance.Localize("$courier_no_supplies");
            if (wood <= 3 || calories <= OutpostResources.CaloriesPerCycle)
                return Localization.instance.Localize("$courier_low_supplies");

            return Localization.instance.Localize("$courier_ready");
        }

        private CourierPostComponent FindNearestPost()
        {
            CourierPostComponent closest = null;
            float closestDist = 20f;
            foreach (var post in FindObjectsOfType<CourierPostComponent>())
            {
                float dist = Vector3.Distance(transform.position, post.transform.position);
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
