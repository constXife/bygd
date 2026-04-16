using Bygd.Framework;
using UnityEngine;

namespace Bygd
{
    public class SettlerNPC : BaseNPC
    {
        protected override string DisplayName => "Elder";
        protected override string ObjectName => "OutpostElder";
        protected override string NameLocKey => "$settler_name";
        protected override string TalkLocKey => "$settler_talk";

        protected override string GetContextualLine()
        {
            var table = FindNearestTable();
            if (table == null)
                return Localization.instance.Localize("$settler_idle");

            var nview = table.GetComponent<ZNetView>();
            if (!OutpostTransferState.IsTransferred(nview))
                return Localization.instance.Localize("$settler_idle");

            int wood = OutpostResources.GetWood(nview);
            int calories = OutpostResources.GetCalories(nview);

            if (wood <= 0 && calories <= 0)
                return Localization.instance.Localize("$outpost_starving");
            if (wood <= 0)
                return Localization.instance.Localize("$outpost_needs_wood");
            if (calories <= 0)
                return Localization.instance.Localize("$outpost_needs_food");
            if (wood <= 3 || calories <= OutpostResources.CaloriesPerCycle)
                return Localization.instance.Localize("$outpost_low_supplies");

            if (!HasDiningTableNearby(table.transform))
                return Localization.instance.Localize("$outpost_needs_table");

            return Localization.instance.Localize("$outpost_all_good");
        }

        private bool HasDiningTableNearby(Transform center)
        {
            foreach (var piece in FindObjectsOfType<Piece>())
            {
                if (piece == null)
                    continue;

                string pieceName = piece.gameObject.name;
                if (!pieceName.StartsWith("piece_table") || pieceName.StartsWith(PrefabNames.OutpostTable))
                    continue;

                float dist = Vector3.Distance(piece.transform.position, center.position);
                if (dist < 10f)
                    return true;
            }

            return false;
        }

        private OutpostTableComponent FindNearestTable()
        {
            OutpostTableComponent closest = null;
            float closestDist = 20f;
            foreach (var table in FindObjectsOfType<OutpostTableComponent>())
            {
                float dist = Vector3.Distance(transform.position, table.transform.position);
                if (dist < closestDist)
                {
                    closest = table;
                    closestDist = dist;
                }
            }
            return closest;
        }
    }
}
