using Bygd.Framework;
using UnityEngine;

namespace Bygd
{
    internal static class OutpostSettlerBinding
    {
        private const string TableKeyField = "bygd_outpost_key";
        private const string SettlerKeyField = "bygd_settler_table_key";
        private const string SettlerIdField = "bygd_settler_id";

        public static bool IsOwner(ZNetView nview)
        {
            if (nview == null || Reflect.ZNetView_IsOwner == null)
                return true;

            return (bool)Reflect.ZNetView_IsOwner.Invoke(nview, null);
        }

        public static string EnsureTableKey(ZNetView nview, Transform transform)
        {
            string existing = GetTableKey(nview, transform);
            if (!string.IsNullOrEmpty(existing))
                return existing;

            if (nview != null && !IsOwner(nview))
                return GenerateFallbackKey(transform);

            ClaimOwnership(nview);

            string key = GetObjectZdoId(nview != null ? nview.gameObject : null);
            if (string.IsNullOrEmpty(key))
                key = GenerateFallbackKey(transform);

            WriteString(GetZdo(nview), TableKeyField, key);
            return key;
        }

        public static string GetTableKey(ZNetView nview, Transform transform)
        {
            string key = ReadString(GetZdo(nview), TableKeyField);
            if (!string.IsNullOrEmpty(key))
                return key;

            return nview == null ? GenerateFallbackKey(transform) : string.Empty;
        }

        public static string GetBoundSettlerId(ZNetView nview)
        {
            return ReadString(GetZdo(nview), SettlerIdField);
        }

        public static void ClearBoundSettler(ZNetView nview)
        {
            WriteString(GetZdo(nview), SettlerIdField, string.Empty);
        }

        public static void BindSettler(ZNetView tableView, Transform tableTransform, GameObject settlerObject)
        {
            if (settlerObject == null)
                return;

            string tableKey = EnsureTableKey(tableView, tableTransform);
            string settlerId = GetObjectZdoId(settlerObject);
            ZNetView settlerView = settlerObject.GetComponent<ZNetView>();

            ClaimOwnership(tableView);
            ClaimOwnership(settlerView);
            WriteString(GetZdo(tableView), SettlerIdField, settlerId);
            WriteString(GetZdo(settlerView), SettlerKeyField, tableKey);
        }

        public static string GetSettlerTableKey(GameObject settlerObject)
        {
            if (settlerObject == null)
                return string.Empty;

            return ReadString(GetZdo(settlerObject.GetComponent<ZNetView>()), SettlerKeyField);
        }

        public static string GetObjectZdoId(GameObject gameObject)
        {
            if (gameObject == null)
                return string.Empty;

            ZNetView nview = gameObject.GetComponent<ZNetView>();
            object zdo = GetZdo(nview);
            if (zdo == null || Reflect.ZDO_m_uid == null)
                return string.Empty;

            object zdoId = Reflect.ZDO_m_uid.GetValue(zdo);
            return zdoId?.ToString() ?? string.Empty;
        }

        private static void ClaimOwnership(ZNetView nview)
        {
            if (nview != null && Reflect.ZNetView_ClaimOwnership != null)
                Reflect.ZNetView_ClaimOwnership.Invoke(nview, null);
        }

        private static object GetZdo(ZNetView nview)
        {
            if (nview == null || Reflect.ZNetView_GetZDO == null)
                return null;

            return Reflect.ZNetView_GetZDO.Invoke(nview, null);
        }

        private static string ReadString(object zdo, string key)
        {
            if (zdo == null)
                return string.Empty;

            if (Reflect.ZDO_GetString != null)
                return (string)Reflect.ZDO_GetString.Invoke(zdo, new object[] { key, string.Empty });

            return string.Empty;
        }

        private static void WriteString(object zdo, string key, string value)
        {
            if (zdo == null || Reflect.ZDO_Set_String == null)
                return;

            Reflect.ZDO_Set_String.Invoke(zdo, new object[] { key, value ?? string.Empty });
        }

        private static string GenerateFallbackKey(Transform transform)
        {
            if (transform == null)
                return "bygd_outpost_unknown";

            Vector3 pos = transform.position;
            return $"bygd_outpost_{pos.x:F1}_{pos.y:F1}_{pos.z:F1}";
        }
    }
}
