using BepInEx.Logging;

namespace Bygd.Framework
{
    internal static class Log
    {
        private static ManualLogSource s_logger;

        public static bool DiagEnabled { get; set; } = false;

        public static void Init(ManualLogSource logger)
        {
            s_logger = logger;
        }

        public static void Info(string message)
        {
            s_logger.LogInfo(message);
        }

        public static void Error(string message)
        {
            s_logger.LogError(message);
        }

        public static void Diag(string message)
        {
            if (DiagEnabled)
                s_logger.LogDebug(message);
        }
    }
}
