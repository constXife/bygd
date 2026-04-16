using Bygd.Framework;

namespace Bygd
{
    internal static class CourierPost_Runtime
    {
        public static bool IsLiveCourierPost(Piece piece) =>
            PostRuntime.IsLivePost(piece, PrefabNames.CourierPost);

        public static void EnsureComponent(Piece piece) =>
            PostRuntime.EnsureComponent<CourierPostComponent>(piece, PrefabNames.CourierPost);
    }
}
