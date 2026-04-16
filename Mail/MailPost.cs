using Bygd.Framework;

namespace Bygd
{
    internal static class MailPost_Runtime
    {
        public static bool IsLiveMailPost(Piece piece) =>
            PostRuntime.IsLivePost(piece, PrefabNames.MailPost);

        public static void EnsureComponent(Piece piece) =>
            PostRuntime.EnsureComponent<MailPostComponent>(piece, PrefabNames.MailPost);
    }
}
