using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace Bygd.Framework
{
    internal static class Reflect
    {
        // --- Tameable (CartHorse, SettlerNPC) ---
        public static readonly MethodInfo Tameable_Tame =
            AccessTools.Method(typeof(Tameable), "Tame");

        // --- BaseAI (CartHorse) ---
        public static readonly MethodInfo BaseAI_FindPath =
            AccessTools.Method(typeof(BaseAI), "FindPath", new[] { typeof(Vector3) });

        public static readonly MethodInfo BaseAI_MoveTo =
            AccessTools.Method(typeof(BaseAI), "MoveTo",
                new[] { typeof(float), typeof(Vector3), typeof(float), typeof(bool) });

        // --- BaseAI (SettlerNPC) ---
        public static readonly MethodInfo BaseAI_StopMoving =
            AccessTools.Method(typeof(BaseAI), "StopMoving");

        // --- Vagon (CartHorse, VagonPatches) ---
        public static readonly FieldInfo Vagon_m_attachJoin =
            AccessTools.Field(typeof(Vagon), "m_attachJoin");

        // --- Character (OutpostTable, OutpostTableComponent) ---
        public static readonly FieldInfo Character_m_name =
            AccessTools.Field(typeof(Character), "m_name");

        public static readonly MethodInfo Character_GetAllCharacters =
            AccessTools.Method(typeof(Character), "GetAllCharacters", Type.EmptyTypes);

        // --- Player (OutpostTable — placement ghost for roof check) ---
        public static readonly FieldInfo Player_m_placementGhost =
            AccessTools.Field(typeof(Player), "m_placementGhost");

        // --- ZNetView (OutpostSettlerBinding) ---
        public static readonly MethodInfo ZNetView_GetZDO =
            AccessTools.Method(typeof(ZNetView), "GetZDO");

        public static readonly MethodInfo ZNetView_IsOwner =
            AccessTools.Method(typeof(ZNetView), "IsOwner");

        public static readonly MethodInfo ZNetView_ClaimOwnership =
            AccessTools.Method(typeof(ZNetView), "ClaimOwnership");

        // --- ZDO (OutpostSettlerBinding) ---
        public static readonly MethodInfo ZDO_GetString =
            AccessTools.Method(typeof(ZDO), "GetString", new[] { typeof(string), typeof(string) });

        public static readonly MethodInfo ZDO_Set_String =
            AccessTools.Method(typeof(ZDO), "Set", new[] { typeof(string), typeof(string) });

        public static readonly FieldInfo ZDO_m_uid =
            AccessTools.Field(typeof(ZDO), "m_uid");

        // --- ZDO int ---
        public static readonly MethodInfo ZDO_GetInt =
            AccessTools.Method(typeof(ZDO), "GetInt", new[] { typeof(string), typeof(int) });

        public static readonly MethodInfo ZDO_Set_Int =
            AccessTools.Method(typeof(ZDO), "Set", new[] { typeof(string), typeof(int) });

        // --- PrivateArea (Ward) ---
        public static readonly FieldInfo PrivateArea_m_radius =
            AccessTools.Field(typeof(PrivateArea), "m_radius");

        public static readonly FieldInfo PrivateArea_m_ownerFaction =
            AccessTools.Field(typeof(PrivateArea), "m_ownerFaction");

        public static readonly FieldInfo PrivateArea_m_piece =
            AccessTools.Field(typeof(PrivateArea), "m_piece");

        public static readonly FieldInfo PrivateArea_m_nview =
            AccessTools.Field(typeof(PrivateArea), "m_nview");

        // --- PrivateArea methods (non-public) ---
        public static readonly MethodInfo PrivateArea_SetEnabled =
            AccessTools.Method(typeof(PrivateArea), "SetEnabled");

        public static readonly MethodInfo PrivateArea_IsPermitted =
            AccessTools.Method(typeof(PrivateArea), "IsPermitted");

        public static readonly MethodInfo PrivateArea_AddPermitted =
            AccessTools.Method(typeof(PrivateArea), "AddPermitted");

        // --- ZDO float ---
        public static readonly MethodInfo ZDO_GetFloat =
            AccessTools.Method(typeof(ZDO), "GetFloat", new[] { typeof(string), typeof(float) });

        public static readonly MethodInfo ZDO_Set_Float =
            AccessTools.Method(typeof(ZDO), "Set", new[] { typeof(string), typeof(float) });

        // --- Piece (Comfort) ---
        public static readonly FieldInfo Piece_s_allComfortPieces =
            AccessTools.Field(typeof(Piece), "s_allComfortPieces");

        // --- TreeBase (Lumberjack) ---
        public static readonly MethodInfo TreeBase_RPC_Damage =
            AccessTools.Method(typeof(TreeBase), "RPC_Damage");

        public static readonly FieldInfo TreeBase_m_health =
            AccessTools.Field(typeof(TreeBase), "m_health");

        // --- Types resolved by name (SettlerNPC — may be null) ---
        public static readonly Type TraderType =
            AccessTools.TypeByName("Trader");

        public static readonly Type NpcTalkType =
            AccessTools.TypeByName("NpcTalk");

        public static readonly Type TalkerType =
            AccessTools.TypeByName("Talker");

        public static readonly FieldInfo Talker_m_nameOverride =
            TalkerType != null ? AccessTools.Field(TalkerType, "m_nameOverride") : null;
    }
}
