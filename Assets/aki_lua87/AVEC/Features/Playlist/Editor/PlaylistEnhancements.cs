using System.Linq;
using System.Collections.Generic;
using VRC.SDKBase;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;
using UdonSharpEditor;

namespace aki_lua87.CommonVideoPlaylist.Editor
{
    // Additive migration: never rebuild existing rows, themes, connections or data.
    public static class PlaylistEnhancements
    {
        public static void ExpandDynamicSlots(PlaylistSetup setup)
        {
            var view = setup.view;
            var urls = new List<VRCUrl>(view.urls); var titles = new List<string>(view.titles);
            var tabs = new List<int>(view.videoTabs); var indices = new List<int>(view.dynamicTitleIndices);
            var images = new List<Image>(view.videoImages); var labels = new List<Text>(view.videoLabels);
            PlaylistUndo.RecordObject(view, "Expand dynamic playlist to twenty slots");
            for (int tab = 0; tab < view.titleSourceUrls.Length; tab++) {
                string source = view.titleSourceUrls[tab].Get();
                if (!source.StartsWith("https://vrc.akakitune87.net/yt/pl/") || !source.EndsWith("/titles")) continue;
                int template = -1;
                for (int i = 0; i < view.videoTabs.Length; i++) if (view.videoTabs[i] == tab && view.videoImages[i] != null) { template = i; break; }
                if (template < 0) continue;
                for (int slot = 0; slot < 20; slot++) {
                    bool exists = false;
                    for (int i = 0; i < tabs.Count; i++) if (tabs[i] == tab && indices[i] == slot) { exists = true; break; }
                    if (exists) continue;
                    int index = urls.Count;
                    string title = (slot + 1).ToString();
                    var row = Object.Instantiate(view.videoImages[template].gameObject, setup.videoContent, false);
                    row.name = "Video " + index; PlaylistUndo.RegisterCreatedObjectUndo(row, "Add dynamic playlist slot");
                    var label = row.GetComponentInChildren<Text>(true); label.text = title;
                    foreach (var click in row.GetComponentsInChildren<PlaylistButton>(true)) {
                        click.view = view; click.index = index; PlaylistConnections.Save(click);
                        var button = click.GetComponent<Button>(); button.onClick = new Button.ButtonClickedEvent();
                        UnityEventTools.AddStringPersistentListener(button.onClick, UdonSharpEditorUtility.GetBackingUdonBehaviour(click).SendCustomEvent, "_Click");
                        PlaylistConnections.Save(button);
                    }
                    row.SetActive(view.videoImages[template].gameObject.activeSelf);
                    urls.Add(new VRCUrl(source.Substring(0, source.Length - 7) + "/video?n=" + slot));
                    titles.Add(title); tabs.Add(tab); indices.Add(slot); images.Add(row.GetComponent<Image>()); labels.Add(label);
                }
            }
            view.urls = urls.ToArray(); view.titles = titles.ToArray(); view.videoTabs = tabs.ToArray();
            view.dynamicTitleIndices = indices.ToArray(); view.videoImages = images.ToArray(); view.videoLabels = labels.ToArray();
            PlaylistConnections.Save(view);
        }
        public static void Apply(PlaylistSetup setup)
        {
            var view = setup.view;
            if (view == null) throw new System.InvalidOperationException("PlaylistView reference is missing.");
            PlaylistUndo.RecordObject(view, "Add playlist history and progress");
            view.repeatIcon = PlaylistIcons.Get("repeat");
            var stop = view.transform.Find("Stop/Icon");
            if (stop != null) { PlaylistUndo.RecordObject(stop.GetComponent<Image>(), "Pause icon"); stop.GetComponent<Image>().sprite = PlaylistIcons.Get("pause"); PlaylistConnections.Save(stop.GetComponent<Image>()); }
            foreach (var image in view.tabImages) if (image != null) PlaylistAppearance.FitTabLabel(image.gameObject);
            if (setup.tabTemplate != null) PlaylistAppearance.FitTabLabel(setup.tabTemplate);
            EnsureHistory(setup);
            view.videoProgress = view.videoImages.Select(row => row == null ? null : Progress(row.transform, view)).ToArray();
            view.queueProgress = view.queueRows.Select(row => row == null ? null : Progress(row.transform, view)).ToArray();
            PlaylistConnections.Save(view);
        }
        static void Wire(Button button, PlaylistView view, string method)
        {
            button.onClick = new Button.ButtonClickedEvent();
            UnityEventTools.AddStringPersistentListener(button.onClick, UdonSharpEditorUtility.GetBackingUdonBehaviour(view).SendCustomEvent, method);
            PlaylistConnections.Save(button);
        }
        static void EnsureHistory(PlaylistSetup setup)
        {
            var view = setup.view;
            if (view.historyPanel != null || view.queuePanel == null || view.queueTabImage == null) return;
            var existingTab = setup.tabContent.Find("History toggle");
            var tab = existingTab == null ? Object.Instantiate(view.queueTabImage.gameObject, view.queueTabImage.transform.parent, false) : existingTab.gameObject;
            tab.name = "History toggle"; if (existingTab == null) PlaylistUndo.RegisterCreatedObjectUndo(tab, "Add history tab");
            tab.transform.SetSiblingIndex(view.queueTabImage.transform.GetSiblingIndex() + 1);
            view.historyTabImage = tab.GetComponent<Image>(); view.historyTabImage.color = view.normalColor;
            view.historyButtonLabel = tab.GetComponentInChildren<Text>(true); view.historyButtonLabel.text = "再生履歴";
            Wire(tab.GetComponent<Button>(), view, "_ToggleHistory");
            var existingPanel = view.transform.Find("History panel");
            var panel = existingPanel == null ? Object.Instantiate(view.queuePanel, view.queuePanel.transform.parent, false) : existingPanel.gameObject;
            panel.name = "History panel"; if (existingPanel == null) PlaylistUndo.RegisterCreatedObjectUndo(panel, "Add history panel");
            var panelRect = panel.GetComponent<RectTransform>(); var sourceRect = view.queuePanel.GetComponent<RectTransform>();
            panelRect.anchorMin = sourceRect.anchorMin; panelRect.anchorMax = sourceRect.anchorMax;
            panelRect.pivot = sourceRect.pivot; panelRect.anchoredPosition = sourceRect.anchoredPosition; panelRect.sizeDelta = sourceRect.sizeDelta;
            panel.GetComponent<Image>().color = view.queuePanel.GetComponent<Image>().color;
            view.historyPanel = panel;
            foreach (string name in new[] { "Add video", "Add video form", "Start queued playback", "Skip queued playback", "Clear queue", "Queue heading" }) {
                var child = panel.transform.Find(name); if (child != null) Object.DestroyImmediate(child.gameObject);
            }
            view.historyLabels = new Text[view.queueLabels.Length]; view.historyRows = new GameObject[view.queueRows.Length];
            for (int i = 0; i < view.historyRows.Length; i++) {
                var row = panel.transform.Find("History row " + i); if (row == null) row = panel.transform.Find("Queue row " + i); if (row == null) continue;
                row.name = "History row " + i; view.historyRows[i] = row.gameObject;
                row.GetComponent<Image>().color = view.queueRows[i].GetComponent<Image>().color;
                view.historyLabels[i] = row.GetComponentsInChildren<Text>(true).First(t => t.name == "Queue title");
                view.historyLabels[i].font = view.queueLabels[i].font;
                view.historyLabels[i].fontSize = view.queueLabels[i].fontSize;
                view.historyLabels[i].color = view.queueLabels[i].color;
                var button = row.Find("Remove queued item").GetComponent<Button>();
                button.GetComponentInChildren<Text>(true).text = "▶";
                var click = button.GetComponent<PlaylistButton>();
                if (click == null) click = UdonSharpEditorUtility.GetProxyBehaviour(button.GetComponent<VRC.Udon.UdonBehaviour>()) as PlaylistButton;
                click.view = view; click.index = i; click.removeQueueSlot = false; click.historySlot = true;
                PlaylistConnections.Save(click);
                button.onClick = new Button.ButtonClickedEvent();
                UnityEventTools.AddStringPersistentListener(button.onClick, UdonSharpEditorUtility.GetBackingUdonBehaviour(click).SendCustomEvent, "_Click");
                PlaylistConnections.Save(button);
            }
            Wire(panel.transform.Find("Previous queue page").GetComponent<Button>(), view, "_HistoryPreviousPage");
            Wire(panel.transform.Find("Next queue page").GetComponent<Button>(), view, "_HistoryNextPage");
            view.historyPageLabel = panel.transform.Find("Queue page").GetComponent<Text>();
            view.historyPageLabel.text = "履歴は空です";
            panel.SetActive(false);
            foreach (var component in panel.GetComponentsInChildren<Component>(true)) if (component != null) PlaylistConnections.Save(component);
            foreach (var component in tab.GetComponentsInChildren<Component>(true)) if (component != null) PlaylistConnections.Save(component);
        }
        internal static Image Progress(Transform row, PlaylistView view)
        {
            var existing = row.Find("Playback progress"); if (existing != null) return existing.GetComponent<Image>();
            var go = new GameObject("Playback progress", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(row, false); PlaylistUndo.RegisterCreatedObjectUndo(go, "Add video progress");
            var rect = go.GetComponent<RectTransform>(); rect.anchorMin = Vector2.zero; rect.anchorMax = new Vector2(1, 0); rect.pivot = new Vector2(.5f, 0);
            rect.offsetMin = new Vector2(4, 1); rect.offsetMax = new Vector2(-4, 5);
            var image = go.GetComponent<Image>(); image.color = view.accentColor; image.raycastTarget = false;
            image.sprite = PlaylistPresentation.SolidProgress();
            image.type = Image.Type.Filled; image.fillMethod = Image.FillMethod.Horizontal; image.fillOrigin = 0; image.fillAmount = 0;
            go.SetActive(false); return image;
        }
    }
}
