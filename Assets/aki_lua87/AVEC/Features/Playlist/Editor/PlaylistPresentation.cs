using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;
using UdonSharpEditor;

namespace aki_lua87.CommonVideoPlaylist.Editor
{
    // Only the requested presentation properties are migrated. Existing rows survive.
    public static class PlaylistPresentation
    {
        public static void Apply(PlaylistSetup setup)
        {
            var view = setup.view;
            if (view == null) return;
            Undo.RecordObject(view, "Playlist presentation");
            foreach (var canvas in setup.GetComponentsInChildren<Canvas>(true)) {
                var scaler = canvas.GetComponent<CanvasScaler>();
                if (scaler == null) scaler = Undo.AddComponent<CanvasScaler>(canvas.gameObject);
                Undo.RecordObject(scaler, "Sharper playlist text");
                scaler.dynamicPixelsPerUnit = Mathf.Max(4f, scaler.dynamicPixelsPerUnit);
                PlaylistConnections.Save(scaler);
            }
            foreach (var text in setup.GetComponentsInChildren<Text>(true)) {
                if (!text.resizeTextForBestFit) continue;
                Undo.RecordObject(text, "Readable minimum text size");
                text.resizeTextMinSize = Mathf.Min(Mathf.Max(text.resizeTextMinSize, 14), Mathf.Max(1, text.fontSize));
                PlaylistConnections.Save(text);
            }
            if (view.queueButtonLabel != null) { view.queueButtonLabel.text = "再生キュー"; PlaylistConnections.Save(view.queueButtonLabel); }
            if (view.historyButtonLabel != null) { view.historyButtonLabel.text = "再生履歴"; PlaylistConnections.Save(view.historyButtonLabel); }
            ScrollList(view, false);
            ScrollList(view, true);
            var solid = SolidProgress();
            foreach (var image in setup.GetComponentsInChildren<Image>(true)) {
                if (image.name != "Playback progress") continue;
                Undo.RecordObject(image, "Solid video progress"); image.sprite = solid;
                PlaylistConnections.Save(image);
            }
            foreach (var selectable in setup.GetComponentsInChildren<Selectable>(true)) {
                Undo.RecordObject(selectable, "Ray only playlist UI");
                var navigation = selectable.navigation; navigation.mode = Navigation.Mode.None; selectable.navigation = navigation;
                PlaylistConnections.Save(selectable);
            }
            foreach (var scroll in setup.GetComponentsInChildren<ScrollRect>(true)) {
                Undo.RecordObject(scroll, "Ray only scrolling");
                // Scrollbar/ray dragging remains available; axis/wheel scroll events do not move the list.
                scroll.scrollSensitivity = 0f;
                PlaylistConnections.Save(scroll);
            }
            PlaylistConnections.Save(view);
        }

        public static Sprite SolidProgress()
        {
            string path = PlaylistFactory.Root + "/Icons/progress-solid.png";
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null) return sprite;
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            texture.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white }); texture.Apply();
            System.IO.File.WriteAllBytes(path, texture.EncodeToPNG()); Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite; importer.spritePixelsPerUnit = 100;
            importer.mipmapEnabled = false; importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed; importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static void ScrollList(PlaylistView view, bool history)
        {
            var panel = history ? view.historyPanel : view.queuePanel;
            var rows = history ? view.historyRows : view.queueRows;
            if (panel == null || rows.Length == 0 || rows[0] == null) return;
            foreach (string name in new[] { "Previous queue page", "Next queue page", "Queue page" }) {
                var old = panel.transform.Find(name);
                if (old != null) { Undo.RecordObject(old.gameObject, "Remove page controls"); old.gameObject.SetActive(false); PlaylistConnections.Save(old.gameObject); }
            }
            var pageLabel = history ? view.historyPageLabel : view.queuePageLabel;
            if (pageLabel != null) { pageLabel.gameObject.SetActive(false); PlaylistConnections.Save(pageLabel.gameObject); }
            var viewport = panel.transform.Find("List viewport") as RectTransform;
            RectTransform content;
            if (viewport == null) {
                viewport = new GameObject("List viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect)).GetComponent<RectTransform>();
                Undo.RegisterCreatedObjectUndo(viewport.gameObject, "Scrollable playlist list"); viewport.SetParent(panel.transform, false);
                var first = rows[0].GetComponent<RectTransform>();
                float top = Mathf.Max(0, -first.anchoredPosition.y);
                viewport.anchorMin = Vector2.zero; viewport.anchorMax = Vector2.one;
                viewport.offsetMin = new Vector2(8, 6); viewport.offsetMax = new Vector2(-8, -top);
                viewport.GetComponent<Image>().color = new Color(0, 0, 0, 0);
                content = new GameObject("List content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter)).GetComponent<RectTransform>();
                Undo.RegisterCreatedObjectUndo(content.gameObject, "Scrollable playlist content"); content.SetParent(viewport, false);
                content.anchorMin = new Vector2(0, 1); content.anchorMax = Vector2.one; content.pivot = new Vector2(.5f, 1);
                content.sizeDelta = Vector2.zero;
                var layout = content.GetComponent<VerticalLayoutGroup>(); layout.spacing = 4;
                layout.childControlWidth = true; layout.childControlHeight = true; layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
                content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                var scroll = viewport.GetComponent<ScrollRect>(); scroll.viewport = viewport; scroll.content = content;
                scroll.horizontal = false; scroll.vertical = true; scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 0;
                if (view.videoScroll != null && view.videoScroll.verticalScrollbar != null) {
                    var bar = Object.Instantiate(view.videoScroll.verticalScrollbar, viewport, false);
                    bar.name = "List scrollbar";
                    if (bar.targetGraphic != null) bar.targetGraphic.color = view.accentColor;
                    Undo.RegisterCreatedObjectUndo(bar.gameObject, "Ray draggable list scrollbar");
                    bar.onValueChanged = new Scrollbar.ScrollEvent();
                    var barRect = bar.GetComponent<RectTransform>();
                    barRect.anchorMin = new Vector2(1, 0); barRect.anchorMax = Vector2.one; barRect.pivot = Vector2.one;
                    barRect.offsetMin = new Vector2(-18, 0); barRect.offsetMax = Vector2.zero;
                    barRect.localScale = Vector3.one; barRect.localRotation = Quaternion.identity;
                    scroll.verticalScrollbar = bar; scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
                    content.offsetMax = new Vector2(-22, 0);
                    foreach (var c in bar.GetComponentsInChildren<Component>(true)) PlaylistConnections.Save(c);
                }
                viewport.SetAsFirstSibling(); // URL form remains above the scrolling rows.
            } else content = viewport.Find("List content") as RectTransform;
            if (content == null) return;
            var allRows = new List<GameObject>(rows);
            var allLabels = new List<Text>(history ? view.historyLabels : view.queueLabels);
            while (allRows.Count < 32) {
                var row = Object.Instantiate(rows[0], content, false);
                row.name = (history ? "History row " : "Queue row ") + allRows.Count;
                Undo.RegisterCreatedObjectUndo(row, "Add list capacity");
                allRows.Add(row); allLabels.Add(row.transform.Find("Queue title").GetComponent<Text>());
                row.SetActive(false);
            }
            for (int i = 0; i < allRows.Count; i++) {
                var row = allRows[i]; if (row == null) continue;
                float height = row.GetComponent<RectTransform>().rect.height;
                if (row.transform.parent != content) Undo.SetTransformParent(row.transform, content, "Move list row into scroll content");
                var layout = row.GetComponent<LayoutElement>(); if (layout == null) layout = Undo.AddComponent<LayoutElement>(row);
                layout.preferredHeight = height > 0 ? height : 38; layout.minHeight = layout.preferredHeight;
                var rect = row.GetComponent<RectTransform>(); rect.localScale = Vector3.one; rect.localRotation = Quaternion.identity;
                var oldButton = row.transform.Find("Remove queued item");
                var button = history ? row.GetComponent<Button>() : oldButton == null ? null : oldButton.GetComponent<Button>();
                if (history) {
                    if (oldButton != null) { oldButton.gameObject.SetActive(false); PlaylistConnections.Save(oldButton.gameObject); }
                    if (button == null) button = Undo.AddComponent<Button>(row);
                    button.targetGraphic = row.GetComponent<Image>();
                }
                if (button != null) {
                    var click = button.GetComponent<PlaylistButton>();
                    if (click == null) click = button.gameObject.AddUdonSharpComponent<PlaylistButton>();
                    Undo.RecordObject(click, "List row action"); click.view = view; click.index = i;
                    click.historySlot = history; click.removeQueueSlot = !history; click.enqueue = false; click.isTab = false;
                    PlaylistConnections.Save(click);
                    button.onClick = new Button.ButtonClickedEvent();
                    UnityEventTools.AddStringPersistentListener(button.onClick, UdonSharpEditorUtility.GetBackingUdonBehaviour(click).SendCustomEvent, "_Click");
                    PlaylistConnections.Save(button);
                }
                PlaylistConnections.Save(layout); PlaylistConnections.Save(rect);
            }
            if (history) { view.historyRows = allRows.ToArray(); view.historyLabels = allLabels.ToArray(); }
            else {
                view.queueRows = allRows.ToArray(); view.queueLabels = allLabels.ToArray();
                view.queueProgress = allRows.Select(row => row.transform.Find("Playback progress")?.GetComponent<Image>()).ToArray();
            }
            foreach (var c in viewport.GetComponents<Component>()) PlaylistConnections.Save(c);
            foreach (var c in content.GetComponents<Component>()) PlaylistConnections.Save(c);
        }
    }
}
