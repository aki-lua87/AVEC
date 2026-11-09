using System;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;
using UdonSharpEditor;

namespace aki_lua87.CommonVideoPlaylist.Editor
{
    // Presentation-only edits. Never rebake data or reset a connected scene instance.
    public static class PlaylistCompactLayout
    {
        public const string PrefabPath = "Assets/aki_lua87/AVEC/Prefabs/AVEC-VideoPlaylistCompact.prefab";
        const string DescriptionClip = "Playlist description clip";

        // Targeted repair for existing compact instances, without reapplying the layout.
        public static void RefreshDescriptionAndScrollbars(PlaylistView view)
        {
            if (view == null || view.listPickerPanel == null) return;
            var picker = view.transform.Find("List picker") as RectTransform;
            var clip = view.descriptionLabel == null ? null : view.descriptionLabel.transform.parent as RectTransform;
            if (picker != null && clip != null && clip.name == DescriptionClip)
            {
                Undo.RecordObject(clip, "Place compact description below list title");
                // Match the selector's horizontal bounds, retaining the description height.
                clip.anchorMin = picker.anchorMin; clip.anchorMax = picker.anchorMax; clip.pivot = picker.pivot;
                clip.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, picker.rect.width);
                clip.anchoredPosition = picker.anchoredPosition + new Vector2(0, -picker.rect.height - 6);
                PlaylistConnections.Save(clip);
            }
            foreach (var bar in view.GetComponentsInChildren<Scrollbar>(true))
            {
                if (bar.direction != Scrollbar.Direction.BottomToTop && bar.direction != Scrollbar.Direction.TopToBottom) continue;
                var rect = (RectTransform)bar.transform;
                Undo.RecordObject(rect, "Slim compact scrollbar");
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 10);
                PlaylistConnections.Save(rect);
            }
        }

        public static void SingleLineDescription(PlaylistView view, bool reclaimInheritedOverrides = false)
        {
            if (view == null || view.descriptionLabel == null) return;
            var label = view.descriptionLabel;
            var root = (RectTransform)view.transform;
            var rect = label.rectTransform;
            bool migrate = rect.parent.name != DescriptionClip && rect.rect.height > 20f;
            if ((migrate || reclaimInheritedOverrides) && view.listPickerPanel == null)
            {
                // Keep each panel's bottom, width and manual offsets; reclaim only 16px at its top.
                foreach (string name in new[] { "Tabs", "Videos", "Queue panel", "History panel", "History URL", "Video details", "User list panel" })
                {
                    var panel = root.Find(name) as RectTransform;
                    if (panel == null) continue;
                    float top = root.rect.yMax - root.InverseTransformPoint(panel.TransformPoint(new Vector3(0, panel.rect.yMax, 0))).y;
                    float reclaim = Mathf.Min(16f, Mathf.Max(0, top - 90f));
                    if (reclaim < .01f) continue;
                    Undo.RecordObject(panel, "Reclaim description space");
                    panel.offsetMax += new Vector2(0, reclaim);
                    PlaylistConnections.Save(panel);
                }
            }
            RectTransform clip;
            if (rect.parent.name == DescriptionClip) clip = (RectTransform)rect.parent;
            else
            {
                clip = new GameObject(DescriptionClip, typeof(RectTransform), typeof(RectMask2D)).GetComponent<RectTransform>();
                Undo.RegisterCreatedObjectUndo(clip.gameObject, "Clip single-line description");
                clip.gameObject.layer = label.gameObject.layer;
                clip.SetParent(rect.parent, false);
                clip.anchorMin = rect.anchorMin; clip.anchorMax = rect.anchorMax; clip.pivot = rect.pivot;
                clip.sizeDelta = rect.sizeDelta; clip.anchoredPosition3D = rect.anchoredPosition3D;
                clip.localRotation = rect.localRotation; clip.localScale = rect.localScale;
                Undo.SetTransformParent(rect, clip, "Keep description in one line");
            }
            Undo.RecordObject(clip, "Single-line description height");
            clip.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 18f);
            Undo.RecordObject(rect, "Stretch clipped description");
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            Undo.RecordObject(label, "Single-line description text");
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.alignment = TextAnchor.LowerLeft; label.lineSpacing = 1;
            label.resizeTextForBestFit = false; label.supportRichText = false;
            label.text = (label.text ?? "").Replace("\r", " ").Replace("\n", " ");
            PlaylistConnections.Save(clip); PlaylistConnections.Save(rect); PlaylistConnections.Save(label);
        }

        // Creates a separate, unconnected distribution template from the existing owned prefab.
        public static void CreatePrefab()
        {
            if (AssetDatabase.LoadMainAssetAtPath(PrefabPath) != null)
                throw new InvalidOperationException("Compact prefab already exists; edit it without regenerating it.");
            var root = PrefabUtility.LoadPrefabContents("Assets/aki_lua87/AVEC/Prefabs/AVEC-VideoPlaylist.prefab");
            try
            {
                root.name = "AVEC-VideoPlaylistCompact";
                var setup = root.GetComponent<PlaylistSetup>();
                var view = setup.view;
                var picker = new GameObject("List picker", typeof(RectTransform), typeof(Image), typeof(Button));
                picker.layer = root.layer; picker.transform.SetParent(root.transform, false);
                picker.GetComponent<Image>().color = view.selectedColor;
                var label = UnityEngine.Object.Instantiate(root.transform.Find("Library title").GetComponent<Text>(), picker.transform, false);
                label.name = "Label"; label.text = "再生キュー  ▾"; label.fontSize = 20;
                label.resizeTextForBestFit = true; label.resizeTextMinSize = 14; label.resizeTextMaxSize = 20;
                label.alignment = TextAnchor.MiddleLeft; label.supportRichText = false; label.raycastTarget = false;
                label.rectTransform.anchorMin = Vector2.zero; label.rectTransform.anchorMax = Vector2.one;
                label.rectTransform.offsetMin = new Vector2(12, 4); label.rectTransform.offsetMax = new Vector2(-12, -4);
                view.listPickerPanel = root.transform.Find("Tabs").gameObject;
                view.listPickerLabel = label;
                EnsurePickerDismiss(view);
                var button = picker.GetComponent<Button>(); button.targetGraphic = picker.GetComponent<Image>();
                button.colors = root.transform.Find("Play").GetComponent<Button>().colors;
                button.navigation = new Navigation { mode = Navigation.Mode.None };
                UnityEventTools.AddStringPersistentListener(button.onClick, UdonSharpEditorUtility.GetBackingUdonBehaviour(view).SendCustomEvent, nameof(PlaylistView._ToggleListPicker));
                ((RectTransform)root.transform).sizeDelta = new Vector2(440, 560);
                var collider = root.GetComponent<BoxCollider>(); if (collider != null) collider.size = new Vector3(440, 560, collider.size.z);
                // Only the new clone gains the missing saved-list capacity; existing scene UI is untouched.
                PlaylistPresentation.ScrollUserList(view);
                Refresh(setup);
                // Initialize only the new prefab's palette from its selected theme.
                PlaylistAppearance.ApplyTheme(setup);
                view.listPickerPanel.SetActive(false);
                PlaylistConnections.Save(view); PlaylistConnections.Save(setup);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        public static void Refresh(PlaylistSetup setup)
        {
            if (setup == null || setup.view == null || setup.view.listPickerPanel == null) return;
            var view = setup.view;
            var root = setup.transform;
            Position(root, "Accent", 20, 12, 36, 4);
            Position(root, "Library title", 20, 19, 300, 32);
            var title = root.Find("Library title").GetComponent<Text>();
            title.fontSize = 24; title.alignment = TextAnchor.MiddleLeft;
            title.verticalOverflow = VerticalWrapMode.Overflow; PlaylistConnections.Save(title);
            Position(root, "Connected player", 20, 51, 300, 20);
            if (view.connectionLabel != null) { view.connectionLabel.fontSize = 13; view.connectionLabel.alignment = TextAnchor.MiddleLeft; PlaylistConnections.Save(view.connectionLabel); }
            Position(root, "Playback mode", 376, 24, 44, 44);
            Position(root, "Divider", 20, 78, 400, 1);
            Position(root, "List picker", 20, 88, 400, 44);
            foreach (string name in new[] { "Videos", "Queue panel", "History panel", "User list panel", "History URL", "Video details" })
                Position(root, name, 20, 160, 400, 318);
            Position(root, "Tabs", 20, 138, 400, 340);
            SingleLineDescription(view);
            Position(root, DescriptionClip, 20, 138, 400, 18);
            Position(root, "Play", 20, 496, 44, 44);
            Position(root, "Stop", 20, 496, 44, 44);
            Position(root, "Volume caption", 76, 496, 36, 44);
            Position(root, "Volume rail", 124, 505, 168, 28);
            Position(root, "Volume value", 304, 496, 48, 44);
            Position(root, "Remote toggle", 376, 496, 44, 44);
            if (view.volumeLabel != null) { view.volumeLabel.fontSize = 18; view.volumeLabel.resizeTextMaxSize = 18; PlaylistConnections.Save(view.volumeLabel); }
            if (view.volumeSlider != null && view.volumeSlider.handleRect != null)
            {
                view.volumeSlider.handleRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 36);
                PlaylistConnections.Save(view.volumeSlider.handleRect);
            }
            SetRow(setup.tabTemplate, 44, 20); SetRow(setup.videoTemplate, 58, 22);
            foreach (var image in new[] { view.queueTabImage, view.historyTabImage, view.userTabImage }) if (image != null) SetRow(image.gameObject, 44, 20);
            foreach (var image in view.tabImages) if (image != null) SetRow(image.gameObject, 44, 20);
            foreach (var image in view.videoImages) if (image != null) SetRow(image.gameObject, 58, 22);
            foreach (var rows in new[] { view.queueRows, view.historyRows, view.userRows }) foreach (var row in rows) SetRow(row, 58, 22);
            RefreshDescriptionAndScrollbars(view);
            PlaylistQueueUI.RefreshHistoryTitleBounds(view);
            if (view.queuePanel != null)
            {
                Position(view.queuePanel.transform, "Add video", 8, 4, 264, 44);
                Position(view.queuePanel.transform, "Skip queued playback", 280, 4, 112, 44);
                var viewport = view.queuePanel.transform.Find("List viewport") as RectTransform;
                if (viewport != null) { viewport.offsetMax = new Vector2(viewport.offsetMax.x, -52); PlaylistConnections.Save(viewport); }
            }
            UserFooter(view);
            Details(view);
            foreach (var panel in new[] { view.queueAddPanel, view.userAddPanel }) if (panel != null) FitForm(panel.transform);
            if (view.loadingIcon != null)
            {
                var rect = (RectTransform)view.loadingIcon.transform;
                PlaylistAppearance.Place(rect, 196, 295, 48, 48);
            }
            // New geometry only; all colors, fonts, data, connection and root transforms survive.
            Canvas.ForceUpdateCanvases();
            foreach (var layout in setup.GetComponentsInChildren<VerticalLayoutGroup>(true)) LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)layout.transform);
        }

        public static void Details(PlaylistView view)
        {
            if (view.listPickerPanel != null && view.historyUrlPanel != null)
            {
                var panel = view.historyUrlPanel.transform;
                Position(panel, "Heading", 12, 10, 376, 62);
                Position(panel, "URL input", 12, 84, 376, 34);
                Position(panel, "Play video", 12, 134, 184, 44);
                Position(panel, "Queue video", 204, 134, 184, 44);
                foreach (string name in new[] { "Remove user video", "Close URL" })
                {
                    var rect = panel.Find(name) as RectTransform;
                    if (rect == null) continue;
                    rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.zero;
                    rect.anchoredPosition = new Vector2(12, 12);
                    rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, name == "Remove user video" ? 184 : 376);
                    rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 44); PlaylistConnections.Save(rect);
                }
            }
        }

        public static void EnsurePickerDismiss(PlaylistView view)
        {
            if (view.listPickerPanel == null || view.listPickerPanel.transform.Find("List picker dismiss viewport") != null) return;
            // "viewport" also keeps theme passes from recoloring this transparent click target.
            var go = new GameObject("List picker dismiss viewport", typeof(RectTransform), typeof(Image), typeof(Button));
            go.layer = view.gameObject.layer; go.transform.SetParent(view.listPickerPanel.transform, false);
            go.transform.SetAsFirstSibling();
            PlaylistAppearance.Place((RectTransform)go.transform, -20, -138, 440, 560);
            go.GetComponent<Image>().color = Color.clear;
            var button = go.GetComponent<Button>(); button.targetGraphic = go.GetComponent<Image>();
            button.transition = Selectable.Transition.None;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            UnityEventTools.AddStringPersistentListener(button.onClick, UdonSharpEditorUtility.GetBackingUdonBehaviour(view).SendCustomEvent, nameof(PlaylistView._CloseListPicker));
        }

        public static void UserFooter(PlaylistView view)
        {
            if (view.userPanel == null) return;
            var panel = (RectTransform)view.userPanel.transform;
            var button = panel.Find("Add user video") as RectTransform;
            if (button != null)
            {
                Undo.RecordObject(button, "Compact saved-list footer");
                button.anchorMin = button.anchorMax = new Vector2(1, 0); button.pivot = new Vector2(1, 0);
                button.anchoredPosition = new Vector2(-8, 8); button.sizeDelta = new Vector2(154, 38);
                PlaylistConnections.Save(button);
            }
            var viewport = panel.Find("List viewport") as RectTransform;
            if (viewport != null) { viewport.offsetMin = new Vector2(viewport.offsetMin.x, 54); PlaylistConnections.Save(viewport); }
        }

        static void FitForm(Transform panel)
        {
            PlaylistAppearance.Place((RectTransform)panel, 8, 0, 384, 318);
            Position(panel, "Input heading", 12, 20, 360, 28);
            Position(panel, "Video URL", 12, 60, 360, 36);
            foreach (string name in new[] { "Submit URL", "Submit user URL" }) Position(panel, name, 204, 114, 76, 44);
            foreach (string name in new[] { "Cancel URL", "Cancel user URL" }) Position(panel, name, 288, 114, 84, 44);
        }
        static void SetRow(GameObject row, float height, int fontSize)
        {
            if (row == null) return;
            var layout = row.GetComponent<LayoutElement>();
            if (layout != null) { layout.minHeight = layout.preferredHeight = height; PlaylistConnections.Save(layout); }
            var rect = row.GetComponent<RectTransform>();
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height); PlaylistConnections.Save(rect);
            var label = row.GetComponentInChildren<Text>(true);
            if (label != null) { label.fontSize = fontSize; label.resizeTextMaxSize = fontSize; PlaylistConnections.Save(label); }
        }
        static void Position(Transform root, string name, float x, float y, float width, float height)
        {
            var rect = root.Find(name) as RectTransform;
            if (rect != null) PlaylistAppearance.Place(rect, x, y, width, height);
        }
    }
}
