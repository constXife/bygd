using Bygd.Framework;
using UnityEngine;

namespace Bygd
{
    internal static class CourierBinding
    {
        private const string PostKeyField = "bygd_courier_post_key";
        private const string CourierIdField = "bygd_courier_id";
        private const string ParentTableField = "bygd_courier_parent_table";
        private const string CourierPostKeyField = "bygd_courier_post_key";
        private const string BudgetField = "bygd_courier_budget";

        public static string EnsurePostKey(ZNetView nview, Transform transform)
        {
            string existing = GetPostKey(nview);
            if (!string.IsNullOrEmpty(existing))
                return existing;

            if (nview != null && !OutpostSettlerBinding.IsOwner(nview))
                return GenerateFallbackKey(transform);

            ClaimOwnership(nview);

            string key = OutpostSettlerBinding.GetObjectZdoId(nview != null ? nview.gameObject : null);
            if (string.IsNullOrEmpty(key))
                key = GenerateFallbackKey(transform);

            WriteString(GetZdo(nview), PostKeyField, key);
            return key;
        }

        public static string GetPostKey(ZNetView nview)
        {
            return ReadString(GetZdo(nview), PostKeyField);
        }

        public static string GetBoundCourierId(ZNetView nview)
        {
            return ReadString(GetZdo(nview), CourierIdField);
        }

        public static void ClearBoundCourier(ZNetView nview)
        {
            WriteString(GetZdo(nview), CourierIdField, string.Empty);
        }

        public static void BindCourier(ZNetView postView, Transform postTransform, GameObject courierObject)
        {
            if (courierObject == null)
                return;

            string postKey = EnsurePostKey(postView, postTransform);
            string courierId = OutpostSettlerBinding.GetObjectZdoId(courierObject);
            ZNetView courierView = courierObject.GetComponent<ZNetView>();

            ClaimOwnership(postView);
            ClaimOwnership(courierView);
            WriteString(GetZdo(postView), CourierIdField, courierId);
            WriteString(GetZdo(courierView), CourierPostKeyField, postKey);
        }

        public static string GetCourierPostKey(GameObject courierObject)
        {
            if (courierObject == null)
                return string.Empty;

            return ReadString(GetZdo(courierObject.GetComponent<ZNetView>()), CourierPostKeyField);
        }

        // --- Link to parent elder's table ---

        public static void SetParentTable(ZNetView postNview, string tableKey)
        {
            WriteString(GetZdo(postNview), ParentTableField, tableKey);
        }

        public static string GetParentTable(ZNetView postNview)
        {
            return ReadString(GetZdo(postNview), ParentTableField);
        }

        // --- Budget ---

        public static int GetBudget(ZNetView nview)
        {
            object zdo = GetZdo(nview);
            if (zdo == null || Reflect.ZDO_GetInt == null)
                return 0;
            return (int)Reflect.ZDO_GetInt.Invoke(zdo, new object[] { BudgetField, 0 });
        }

        public static void SetBudget(ZNetView nview, int value)
        {
            ClaimOwnership(nview);
            object zdo = GetZdo(nview);
            if (zdo == null || Reflect.ZDO_Set_Int == null)
                return;
            Reflect.ZDO_Set_Int.Invoke(zdo, new object[] { BudgetField, value });
        }

        public static void AddBudget(ZNetView nview, int calories)
        {
            int current = GetBudget(nview);
            SetBudget(nview, current + calories);
        }

        // --- Helpers ---

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
            if (zdo == null || Reflect.ZDO_GetString == null)
                return string.Empty;

            return (string)Reflect.ZDO_GetString.Invoke(zdo, new object[] { key, string.Empty });
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
                return "bygd_courier_unknown";

            Vector3 pos = transform.position;
            return $"bygd_courier_{pos.x:F1}_{pos.y:F1}_{pos.z:F1}";
        }
    }
}
