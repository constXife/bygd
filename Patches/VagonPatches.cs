using HarmonyLib;
using Bygd.Framework;
using UnityEngine;

namespace Bygd
{
    [HarmonyPatch(typeof(Vagon), "FixedUpdate")]
    public class Vagon_FixedUpdate_Patch
    {
        static bool Prefix(Vagon __instance)
        {
            ConfigurableJoint joint = (ConfigurableJoint)Reflect.Vagon_m_attachJoin.GetValue(__instance);
            if (joint == null) return true;

            Rigidbody connected = ((Joint)joint).connectedBody;
            if (connected == null) return true;

            if (connected.GetComponent<CartHorse>() != null)
                return false;

            return true;
        }
    }

    [HarmonyPatch(typeof(Vagon), "InUse")]
    public class Vagon_InUse_Patch
    {
        static void Postfix(Vagon __instance, ref bool __result)
        {
            ConfigurableJoint joint = (ConfigurableJoint)Reflect.Vagon_m_attachJoin.GetValue(__instance);
            if (joint == null) return;

            Rigidbody connected = ((Joint)joint).connectedBody;
            if (connected == null) return;

            if (connected.GetComponent<CartHorse>() != null)
                __result = false;
        }
    }

    [HarmonyPatch(typeof(Vagon), "Interact")]
    public class Vagon_Interact_Patch
    {
        static bool Prefix(Vagon __instance, Humanoid character, bool hold, ref bool __result)
        {
            if (hold) return true;

            ConfigurableJoint joint = (ConfigurableJoint)Reflect.Vagon_m_attachJoin.GetValue(__instance);
            if (joint == null) return true;

            Rigidbody connected = ((Joint)joint).connectedBody;
            if (connected == null) return true;

            CartHorse driver = connected.GetComponent<CartHorse>();
            if (driver == null) return true;

            Character charComp = character.GetComponent<Character>();

            if (charComp.IsAttached())
            {
                charComp.AttachStop();
                character.Message(MessageHud.MessageType.TopLeft, "You dismounted the cart");
                __result = true;
                return false;
            }

            Transform seat = __instance.transform.Find("PassengerSeat");
            if (seat == null)
            {
                GameObject seatObj = new GameObject("PassengerSeat");
                seatObj.transform.SetParent(__instance.transform);
                seatObj.transform.localPosition = new Vector3(0f, 1.5f, 0f);
                seat = seatObj.transform;
            }

            Rigidbody cartBody = __instance.gameObject.GetComponent<Rigidbody>();
            charComp.AttachStart(
                seat,
                cartBody != null ? cartBody.gameObject : __instance.gameObject,
                false, false, false, "",
                new Vector3(0f, 0.5f, -1.5f),
                null
            );

            character.Message(MessageHud.MessageType.TopLeft, "You mounted the cart. Press E to dismount");
            __result = true;
            return false;
        }
    }
}
