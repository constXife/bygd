using Bygd.Framework;

namespace Bygd
{
    internal static class OutpostTransferState
    {
        private const string TransferredField = "bygd_outpost_transferred";

        public static bool IsTransferred(ZNetView nview)
        {
            return ReadZdoString(nview, TransferredField) == "1";
        }

        public static void SetTransferred(ZNetView nview, bool transferred)
        {
            WriteZdoString(nview, TransferredField, transferred ? "1" : "");
        }

        public static string ReadZdoString(ZNetView nview, string key)
        {
            if (nview == null || Reflect.ZNetView_GetZDO == null || Reflect.ZDO_GetString == null)
                return "";

            object zdo = Reflect.ZNetView_GetZDO.Invoke(nview, null);
            if (zdo == null)
                return "";

            return (string)Reflect.ZDO_GetString.Invoke(zdo, new object[] { key, "" });
        }

        public static void WriteZdoString(ZNetView nview, string key, string value)
        {
            if (nview == null || Reflect.ZNetView_GetZDO == null || Reflect.ZDO_Set_String == null)
                return;

            if (Reflect.ZNetView_ClaimOwnership != null)
                Reflect.ZNetView_ClaimOwnership.Invoke(nview, null);

            object zdo = Reflect.ZNetView_GetZDO.Invoke(nview, null);
            if (zdo == null)
                return;

            Reflect.ZDO_Set_String.Invoke(zdo, new object[] { key, value ?? "" });
        }
    }
}
