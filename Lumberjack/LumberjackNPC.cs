using Bygd.Framework;

namespace Bygd
{
    internal class LumberjackNPC : BaseNPC
    {
        protected override string DisplayName => Localization.instance.Localize("$lumberjack_name");
        protected override string ObjectName => "Lumberjack";
        protected override string NameLocKey => "lumberjack_name";
        protected override string TalkLocKey => "lumberjack_talk";

        protected override string GetContextualLine()
        {
            var worker = GetComponent<LumberjackWorker>();
            if (worker != null && worker.IsWorking)
                return Localization.instance.Localize("$lumberjack_working");

            return Localization.instance.Localize("$lumberjack_idle");
        }
    }
}
