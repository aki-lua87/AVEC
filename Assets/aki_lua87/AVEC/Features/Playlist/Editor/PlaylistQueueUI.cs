using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;
using UdonSharpEditor;

namespace aki_lua87.CommonVideoPlaylist.Editor
{
    public static class PlaylistQueueUI
    {
        public static void Ensure(PlaylistSetup setup)
        {
            var view = setup.view;
            if (view == null) return;
            if (view.queuePanel == null)
            {
                Undo.RecordObject(view, "Add playlist queue UI");
                var toggle = Button(setup.transform, "Queue toggle", "再生キュー", view, "_ToggleQueue", 202, 18, 102, 42, view.normalColor);
                view.queueButtonLabel = toggle.GetComponentInChildren<Text>();
                var panel = Box(setup.transform, "Queue panel", setup.theme == null ? view.normalColor : setup.theme.surface);
                var videos = setup.transform.Find("Videos") as RectTransform;
                if (videos != null) {
                    panel.anchorMin = videos.anchorMin; panel.anchorMax = videos.anchorMax; panel.pivot = videos.pivot;
                    panel.anchoredPosition = videos.anchoredPosition; panel.sizeDelta = videos.sizeDelta;
                    panel.localScale = videos.localScale; panel.localRotation = videos.localRotation;
                } else PlaylistAppearance.Place(panel, 232, 106, 644, 328);
                float width = panel.rect.width;
                var header = Label(panel, "Queue heading", "再生キュー", view, 18);
                PlaylistAppearance.Place(header.rectTransform, 12, 3, width - 210, 32);
                Button(panel, "Start queued playback", "再生", view, "_StartQueuedPlayback", width - 182, 3, 76, 30, view.normalColor);
                Button(panel, "Clear queue", "クリア", view, "_ClearQueue", width - 98, 3, 86, 30, view.normalColor);
                view.queueLabels = new Text[6]; view.queueRows = new GameObject[6];
                for (int i = 0; i < 6; i++) {
                    var row = Box(panel, "Queue row " + i, view.normalColor);
                    PlaylistAppearance.Place(row, 8, 38 + i * 42, width - 16, 38);
                    view.queueRows[i] = row.gameObject;
                    var label = Label(row, "Queue title", "", view, 16);
                    label.supportRichText = false; label.resizeTextForBestFit = true; label.resizeTextMinSize = 12; label.resizeTextMaxSize = 16;
                    PlaylistAppearance.Place(label.rectTransform, 10, 0, width - 78, 38);
                    view.queueLabels[i] = label;
                    var remove = Button(row, "Remove queued item", "×", view, null, width - 58, 4, 32, 30, view.normalColor);
                    var click = remove.gameObject.AddUdonSharpComponent<PlaylistButton>();
                    click.view = view; click.index = i; click.removeQueueSlot = true;
                    PlaylistConnections.Save(click);
                    UnityEventTools.AddStringPersistentListener(remove.onClick, UdonSharpEditorUtility.GetBackingUdonBehaviour(click).SendCustomEvent, "_Click");
                    PlaylistConnections.Save(remove);
                }
                Button(panel, "Previous queue page", "前へ", view, "_QueuePreviousPage", 8, 294, 64, 28, view.normalColor);
                Button(panel, "Next queue page", "次へ", view, "_QueueNextPage", width - 72, 294, 64, 28, view.normalColor);
                view.queuePageLabel = Label(panel, "Queue page", "キューは空です", view, 16);
                view.queuePageLabel.alignment = TextAnchor.MiddleCenter;
                PlaylistAppearance.Place(view.queuePageLabel.rectTransform, 80, 294, width - 160, 28);
                view.queuePanel = panel.gameObject;
                panel.gameObject.SetActive(false);
                PlaylistConnections.Save(panel.gameObject); PlaylistConnections.Save(view);
            }
            UpdateQueueLabels(view);
            EnsureTabLayout(setup);
            for (int i = 0; i < view.videoImages.Length; i++)
                if (view.videoImages[i] != null) EnsureAddButton(setup, view.videoImages[i].gameObject, i);
        }
        // Update only the requested labels and icon, preserving layout and button events.
        public static void UpdateQueueLabels(PlaylistView view)
        {
            if (view == null) return;
            if (view.queueButtonLabel != null) { Undo.RecordObject(view.queueButtonLabel, "Rename queue tab"); view.queueButtonLabel.text = "再生キュー"; PlaylistConnections.Save(view.queueButtonLabel); }
            if (view.historyButtonLabel != null) { Undo.RecordObject(view.historyButtonLabel, "Rename history tab"); view.historyButtonLabel.text = "再生履歴"; PlaylistConnections.Save(view.historyButtonLabel); }
            if (view.heading != null && (view.heading.text == "キュー" || view.heading.text == "再生待ちキュー")) { Undo.RecordObject(view.heading, "Rename queue heading"); view.heading.text = "再生キュー"; PlaylistConnections.Save(view.heading); }
            if (view.queuePanel == null) return;
            var panel = view.queuePanel.transform;
            var clear = panel.Find("Clear queue");
            var clearLabel = clear == null ? null : clear.GetComponentInChildren<Text>(true);
            if (clearLabel != null) { Undo.RecordObject(clearLabel, "Rename clear button"); clearLabel.text = "クリア"; PlaylistConnections.Save(clearLabel); }
            var heading = panel.Find("Queue heading");
            var headingLabel = heading == null ? null : heading.GetComponent<Text>();
            if (headingLabel != null) { Undo.RecordObject(headingLabel, "Rename queue heading"); headingLabel.text = "再生キュー"; PlaylistConnections.Save(headingLabel); }
            var play = panel.Find("Start queued playback");
            if (play == null) return;
            var label = play.GetComponentInChildren<Text>(true);
            if (label != null) { Undo.RecordObject(label, "Replace queue play text with icon"); label.enabled = false; PlaylistConnections.Save(label); }
            var existing = play.Find("Icon");
            Image icon;
            if (existing == null)
            {
                var go = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                Undo.RegisterCreatedObjectUndo(go, "Add queue play icon"); go.transform.SetParent(play, false);
                icon = go.GetComponent<Image>();
                icon.rectTransform.anchorMin = icon.rectTransform.anchorMax = icon.rectTransform.pivot = new Vector2(.5f, .5f);
                icon.rectTransform.anchoredPosition = Vector2.zero; icon.rectTransform.sizeDelta = new Vector2(24f, 24f);
                icon.color = label == null ? view.iconColor : label.color;
            }
            else { icon = existing.GetComponent<Image>(); if (icon == null) return; Undo.RecordObject(icon, "Update queue play icon"); }
            icon.sprite = PlaylistIcons.Get("play"); icon.preserveAspect = true; icon.raycastTarget = false;
            PlaylistConnections.Save(icon);
        }
        static void EnsureTabLayout(PlaylistSetup setup)
        {
            var view = setup.view;
            var toggle = view.queueButtonLabel == null ? null : view.queueButtonLabel.transform.parent;
            if (toggle == null) return;
            if (toggle.parent != setup.tabContent) {
                Undo.SetTransformParent(toggle, setup.tabContent, "Move queue into playlist tabs");
                toggle.SetSiblingIndex(0);
                var rect = toggle.GetComponent<RectTransform>();
                Undo.RecordObject(rect, "Match queue tab layout");
                rect.localScale = Vector3.one; rect.localRotation = Quaternion.identity;
                var layout = toggle.GetComponent<LayoutElement>();
                if (layout == null) layout = Undo.AddComponent<LayoutElement>(toggle.gameObject);
                var templateLayout = setup.tabTemplate.GetComponent<LayoutElement>();
                if (templateLayout != null) EditorUtility.CopySerialized(templateLayout, layout);
                else layout.preferredHeight = setup.tabTemplate.GetComponent<RectTransform>().rect.height;
                var text = view.queueButtonLabel;
                var source = setup.tabTemplate.GetComponentInChildren<Text>(true);
                Undo.RecordObject(text, "Match queue tab label");
                text.font = source.font; text.fontSize = source.fontSize; text.alignment = source.alignment;
                text.rectTransform.anchorMin = source.rectTransform.anchorMin; text.rectTransform.anchorMax = source.rectTransform.anchorMax;
                text.rectTransform.offsetMin = source.rectTransform.offsetMin; text.rectTransform.offsetMax = source.rectTransform.offsetMax;
                PlaylistConnections.Save(rect); PlaylistConnections.Save(layout); PlaylistConnections.Save(text);
            }
            toggle.SetSiblingIndex(0);
            view.queueTabImage = toggle.GetComponent<Image>();
            var panel = view.queuePanel.transform;
            float width = ((RectTransform)panel).rect.width;
            var oldHeading = panel.Find("Queue heading");
            if (oldHeading != null) { oldHeading.gameObject.SetActive(false); PlaylistConnections.Save(oldHeading.gameObject); }
            if (panel.Find("Add video") == null)
                Button(panel, "Add video", "＋ 動画を追加", view, "_OpenQueueAdd", 8, 3, width - 198, 30, view.normalColor);
            if (view.queueAddPanel == null) {
                var form = Box(panel, "Add video form", setup.theme == null ? view.normalColor : setup.theme.surface);
                PlaylistAppearance.Place(form, 8, 38, width - 16, 126);
                var heading = Label(form, "Input heading", "動画URLを入力", view, 18);
                PlaylistAppearance.Place(heading.rectTransform, 12, 8, width - 40, 28);
                var inputRect = Box(form, "Video URL", view.normalColor);
                PlaylistAppearance.Place(inputRect, 12, 42, width - 56, 36);
                var value = Label(inputRect, "Text", "", view, 16);
                value.supportRichText = false;
                value.rectTransform.anchorMin = Vector2.zero; value.rectTransform.anchorMax = Vector2.one;
                value.rectTransform.offsetMin = new Vector2(8, 2); value.rectTransform.offsetMax = new Vector2(-8, -2);
                var hint = Label(inputRect, "Placeholder", "https://…", view, 16);
                hint.rectTransform.anchorMin = Vector2.zero; hint.rectTransform.anchorMax = Vector2.one;
                hint.rectTransform.offsetMin = new Vector2(8, 2); hint.rectTransform.offsetMax = new Vector2(-8, -2);
                var input = inputRect.gameObject.AddComponent<VRC.SDK3.Components.VRCUrlInputField>();
                input.textComponent = value; input.placeholder = hint; input.targetGraphic = inputRect.GetComponent<Image>();
                input.characterLimit = 2048; input.AllowSendingOnEndEdit = false;
                Button(form, "Submit URL", "追加", view, "_SubmitQueueUrl", width - 198, 86, 76, 30, view.normalColor);
                Button(form, "Cancel URL", "閉じる", view, "_CloseQueueAdd", width - 110, 86, 76, 30, view.normalColor);
                view.queueUrlInput = input; view.queueAddPanel = form.gameObject;
                form.gameObject.SetActive(false);
                PlaylistConnections.Save(input); PlaylistConnections.Save(form.gameObject);
            }
            PlaylistConnections.Save(view);
            Canvas.ForceUpdateCanvases();
        }
        public static void EnsureAddButton(PlaylistSetup setup, GameObject row, int index)
        {
            if (row.transform.Find("Add to queue") != null) return;
            var label = row.GetComponentInChildren<Text>(true);
            if (label != null) {
                Undo.RecordObject(label.rectTransform, "Reserve queue button space");
                var offset = label.rectTransform.offsetMax; offset.x -= 44; label.rectTransform.offsetMax = offset;
                PlaylistConnections.Save(label.rectTransform);
            }
            var button = Button(row.transform, "Add to queue", "+", setup.view, null, 0, 0, 36, 36, setup.view.normalColor);
            var rect = button.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1, .5f); rect.anchoredPosition = new Vector2(-6, 0);
            var click = button.gameObject.AddUdonSharpComponent<PlaylistButton>();
            click.view = setup.view; click.index = index; click.enqueue = true;
            PlaylistConnections.Save(click);
            UnityEventTools.AddStringPersistentListener(button.onClick, UdonSharpEditorUtility.GetBackingUdonBehaviour(click).SendCustomEvent, "_Click");
            PlaylistConnections.Save(button); PlaylistConnections.Save(rect);
        }
        static Button Button(Transform parent, string name, string caption, PlaylistView view, string method, float x, float y, float width, float height, Color color)
        {
            var rect = Box(parent, name, color);
            PlaylistAppearance.Place(rect, x, y, width, height);
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = rect.GetComponent<Image>();
            var label = Label(rect, "Label", caption, view, 16); label.alignment = TextAnchor.MiddleCenter;
            label.rectTransform.anchorMin = Vector2.zero; label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
            if (method != null) UnityEventTools.AddStringPersistentListener(button.onClick, UdonSharpEditorUtility.GetBackingUdonBehaviour(view).SendCustomEvent, method);
            PlaylistConnections.Save(button); return button;
        }
        static RectTransform Box(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image)); go.transform.SetParent(parent, false);
            Undo.RegisterCreatedObjectUndo(go, "Add playlist queue UI"); go.GetComponent<Image>().color = color;
            return go.GetComponent<RectTransform>();
        }
        static Text Label(Transform parent, string name, string value, PlaylistView view, int size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text)); go.transform.SetParent(parent, false);
            Undo.RegisterCreatedObjectUndo(go, "Add playlist queue label");
            var text = go.GetComponent<Text>(); text.text = value; text.fontSize = size;
            text.font = view.heading != null ? view.heading.font : AssetDatabase.LoadAssetAtPath<Font>(aki_lua87.AVEC.Editor.PackageLayout.Core + "/Fonts/ZenMaruGothic-Regular.ttf");
            text.color = view.iconColor; text.alignment = TextAnchor.MiddleLeft; text.raycastTarget = false;
            return text;
        }
    }
}
