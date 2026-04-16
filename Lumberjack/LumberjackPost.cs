using Bygd.Framework;

namespace Bygd
{
    internal static class LumberjackPost_Runtime
    {
        public static bool IsLivePost(Piece piece) =>
            PostRuntime.IsLivePost(piece, PrefabNames.LumberjackPost);

        public static void EnsureComponent(Piece piece) =>
            PostRuntime.EnsureComponent<LumberjackPostComponent>(piece, PrefabNames.LumberjackPost);
    }
}
