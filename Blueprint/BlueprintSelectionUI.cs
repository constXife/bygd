using System.Collections.Generic;
using Bygd.Framework;
using Jotunn.Managers;
using UnityEngine;
using UnityEngine.UI;

namespace Bygd
{
    internal class BlueprintSelectionUIUpdater : MonoBehaviour
    {
        void Update()
        {
            if (BlueprintSelectionUI.IsOpen && (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Tab)))
                BlueprintSelectionUI.Close();
        }
    }

    internal static class BlueprintSelectionUI
    {
        private static GameObject _panel;

        public static bool IsOpen => _panel != null && _panel.activeSelf;

        public static void Show(MonoBehaviour anchor)
        {
            if (_panel != null)
                Object.Destroy(_panel);

            _panel = GUIManager.Instance.CreateWoodpanel(
                parent: GUIManager.CustomGUIFront.transform,
                anchorMin: new Vector2(0.5f, 0.5f),
                anchorMax: new Vector2(0.5f, 0.5f),
                position: new Vector2(0, 0),
                width: 450,
                height: 350,
                draggable: false);

            _panel.AddComponent<BlueprintSelectionUIUpdater>();

            GUIManager.Instance.CreateText(
                text: Localization.instance.Localize("$blueprint_select_title"),
                parent: _panel.transform,
                anchorMin: new Vector2(0.5f, 1f),
                anchorMax: new Vector2(0.5f, 1f),
                position: new Vector2(0f, -40f),
                font: GUIManager.Instance.AveriaSerifBold,
                fontSize: 22,
                color: GUIManager.Instance.ValheimOrange,
                outline: true,
                outlineColor: Color.black,
                width: 400f,
                height: 35f,
                addContentSizeFitter: false);

            int btnIndex = 0;
            var blueprints = BlueprintRegistry.GetAll();

            foreach (var kvp in blueprints)
            {
                string key = kvp.Key;
                BlueprintData data = kvp.Value;
                int reqLevel = BlueprintRegistry.GetRequiredLevel(key);
                string label = string.Format(
                    Localization.instance.Localize("$blueprint_entry_label"),
                    data.Name,
                    data.Pieces.Count,
                    reqLevel);

                float yPos = -100f - btnIndex * 55f;
                var btnObj = GUIManager.Instance.CreateButton(
                    text: label,
                    parent: _panel.transform,
                    anchorMin: new Vector2(0.5f, 1f),
                    anchorMax: new Vector2(0.5f, 1f),
                    position: new Vector2(0f, yPos),
                    width: 350f,
                    height: 45f);

                btnObj.GetComponent<Button>().onClick.AddListener(() =>
                {
                    BlueprintGhost.Show(anchor, data);
                    Player.m_localPlayer?.Message(MessageHud.MessageType.Center,
                        string.Format(
                            Localization.instance.Localize("$blueprint_selected_hint"),
                            data.Name));
                    Close();
                });
                btnIndex++;
            }

            // Cancel button.
            float cancelY = -100f - btnIndex * 55f;
            var cancelBtn = GUIManager.Instance.CreateButton(
                text: Localization.instance.Localize("$ui_cancel"),
                parent: _panel.transform,
                anchorMin: new Vector2(0.5f, 1f),
                anchorMax: new Vector2(0.5f, 1f),
                position: new Vector2(0f, cancelY),
                width: 350f,
                height: 45f);
            cancelBtn.GetComponent<Button>().onClick.AddListener(Close);

            _panel.SetActive(true);
            GUIManager.BlockInput(true);
        }

        public static void Close()
        {
            if (_panel != null)
                _panel.SetActive(false);
            GUIManager.BlockInput(false);
        }
    }
}
