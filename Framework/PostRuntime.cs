using UnityEngine;

namespace Bygd.Framework
{
    /// <summary>
    /// Common logic for all anchor buildings (OutpostTable, CourierPost, MailPost, LumberjackPost).
    /// Checks that the piece is "alive" and attaches a MonoBehaviour component.
    /// </summary>
    internal static class PostRuntime
    {
        /// <summary>
        /// Checks that the piece is active, has a ZNetView and ZDO.
        /// </summary>
        public static bool IsLivePost(Piece piece, string prefabPrefix)
        {
            if (piece == null || !piece.name.StartsWith(prefabPrefix))
                return false;

            if (!piece.gameObject.activeInHierarchy)
                return false;

            var nview = piece.GetComponent<ZNetView>();
            if (nview == null)
                return false;

            string zdoId = OutpostSettlerBinding.GetObjectZdoId(piece.gameObject);
            return !string.IsNullOrEmpty(zdoId);
        }

        /// <summary>
        /// Attaches component T to the piece if it doesn't already have one.
        /// </summary>
        public static void EnsureComponent<T>(Piece piece, string prefabPrefix) where T : MonoBehaviour
        {
            if (!IsLivePost(piece, prefabPrefix))
                return;

            if (piece.GetComponent<T>() == null)
            {
                piece.gameObject.AddComponent<T>();
                Log.Diag($"PostRuntime: attached {typeof(T).Name} to {piece.name}");
            }
        }
    }
}
