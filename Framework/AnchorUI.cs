using System;
using System.Collections.Generic;
using Bygd.Framework;
using Jotunn.Managers;
using UnityEngine;
using UnityEngine.UI;

namespace Bygd
{
    internal class AnchorUIUpdater : MonoBehaviour
    {
        void Update()
        {
            if (AnchorUI.IsOpen && (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Tab)))
                AnchorUI.Close();
        }
    }

    internal static class AnchorUI
    {
        private static GameObject _panel;

        public static bool IsOpen => _panel != null && _panel.activeSelf;

        public static void Close()
        {
            if (_panel != null)
                UnityEngine.Object.Destroy(_panel);
            _panel = null;
            GUIManager.BlockInput(false);
        }

        /// <summary>
        /// Shows a panel with a title, optional info line and buttons.
        /// </summary>
        public static void Show(string title, string info, List<ButtonDef> buttons)
        {
            Close();

            int buttonCount = buttons.Count;
            float panelHeight = 120f + buttonCount * 55f;
            if (!string.IsNullOrEmpty(info))
                panelHeight += 30f;

            _panel = GUIManager.Instance.CreateWoodpanel(
                parent: GUIManager.CustomGUIFront.transform,
                anchorMin: new Vector2(0.5f, 0.5f),
                anchorMax: new Vector2(0.5f, 0.5f),
                position: new Vector2(0, 0),
                width: 400,
                height: panelHeight,
                draggable: false);

            _panel.AddComponent<AnchorUIUpdater>();

            GUIManager.Instance.CreateText(
                text: title,
                parent: _panel.transform,
                anchorMin: new Vector2(0.5f, 1f),
                anchorMax: new Vector2(0.5f, 1f),
                position: new Vector2(0f, -40f),
                font: GUIManager.Instance.AveriaSerifBold,
                fontSize: 22,
                color: GUIManager.Instance.ValheimOrange,
                outline: true,
                outlineColor: Color.black,
                width: 350f,
                height: 35f,
                addContentSizeFitter: false);

            float buttonStartY = -90f;

            if (!string.IsNullOrEmpty(info))
            {
                GUIManager.Instance.CreateText(
                    text: info,
                    parent: _panel.transform,
                    anchorMin: new Vector2(0.5f, 1f),
                    anchorMax: new Vector2(0.5f, 1f),
                    position: new Vector2(0f, -70f),
                    font: GUIManager.Instance.AveriaSerif,
                    fontSize: 16,
                    color: Color.white,
                    outline: true,
                    outlineColor: Color.black,
                    width: 350f,
                    height: 25f,
                    addContentSizeFitter: false);
                buttonStartY = -110f;
            }

            for (int i = 0; i < buttons.Count; i++)
            {
                var def = buttons[i];
                float yPos = buttonStartY - i * 55f;

                var btnObj = GUIManager.Instance.CreateButton(
                    text: def.Label,
                    parent: _panel.transform,
                    anchorMin: new Vector2(0.5f, 1f),
                    anchorMax: new Vector2(0.5f, 1f),
                    position: new Vector2(0f, yPos),
                    width: 300f,
                    height: 45f);

                btnObj.GetComponent<Button>().onClick.AddListener(def.OnClick);
            }

            _panel.SetActive(true);
            GUIManager.BlockInput(true);
        }

        /// <summary>
        /// Ghost buttons for any anchor: rotate / build / cancel.
        /// </summary>
        public static List<ButtonDef> GhostButtons(MonoBehaviour anchor, BlueprintGhost ghost)
        {
            return new List<ButtonDef>
            {
                new ButtonDef($"Rotate plan ({ghost.Rotation}°)", () =>
                {
                    ghost.Rotate(90f);
                    Close();
                }),
                new ButtonDef("Start building", () =>
                {
                    BlueprintBuilder.Start(anchor, ghost);
                    Close();
                }),
                new ButtonDef("Cancel plan", () =>
                {
                    BlueprintGhost.Cancel();
                    Close();
                })
            };
        }

        /// <summary>
        /// Handles interact when ghost/builder is active. Returns true if handled.
        /// </summary>
        public static bool HandleGhostInteract(MonoBehaviour anchor, Humanoid user, bool alt)
        {
            var ghost = anchor.GetComponent<BlueprintGhost>();
            if (ghost != null)
            {
                if (alt)
                {
                    BlueprintBuilder.Start(anchor, ghost);
                    user?.Message(MessageHud.MessageType.Center, "Building started!");
                }
                else
                {
                    ghost.Rotate(90f);
                    user?.Message(MessageHud.MessageType.TopLeft, $"Plan rotated: {ghost.Rotation}°");
                }
                return true;
            }

            var builder = anchor.GetComponent<BlueprintBuilder>();
            if (builder != null && builder.IsBuilding)
            {
                user?.Message(MessageHud.MessageType.Center, $"Building: {(int)(builder.Progress * 100)}%");
                return true;
            }

            return false;
        }

        /// <summary>
        /// Hover text for ghost/builder. Returns null if there is no active plan.
        /// </summary>
        public static string GetGhostHoverText(MonoBehaviour anchor)
        {
            var ghost = anchor.GetComponent<BlueprintGhost>();
            if (ghost != null)
            {
                string planName = ghost.Blueprint?.Name ?? "plan";
                return $"<color=yellow>{planName}</color> ({ghost.Rotation}°)\n"
                    + "[<color=yellow><b>$KEY_Use</b></color>] Rotate\n"
                    + "[<color=yellow><b>Shift+$KEY_Use</b></color>] Build";
            }

            var builder = anchor.GetComponent<BlueprintBuilder>();
            if (builder != null && builder.IsBuilding)
                return $"Building: {(int)(builder.Progress * 100)}%";

            return null;
        }
    }

    internal struct ButtonDef
    {
        public string Label;
        public UnityEngine.Events.UnityAction OnClick;

        public ButtonDef(string label, Action onClick)
        {
            Label = label;
            OnClick = new UnityEngine.Events.UnityAction(onClick);
        }
    }
}
