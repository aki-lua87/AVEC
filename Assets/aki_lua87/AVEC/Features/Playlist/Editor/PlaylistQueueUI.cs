using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;
using UdonSharpEditor;

namespace aki_lua87.CommonVideoPlaylist.Editor
{
    public static class PlaylistQueueUI
    {
        // Fit wrapped titles inside their existing authored row bounds.
        public static int RefreshQueueTitleClipping(PlaylistView view)
        {
            if (view == null) return 0;
            int count = 0;
            foreach (var label in view.queueLabels)
            {
                if (label == null) continue;
                ClipTitle(label, "Queue title viewport"); count++;
            }
            return count;
        }

        public static void RefreshLongTitleClipping(PlaylistView view)
        {
            foreach (var label in view.videoLabels) if (label != null) ClipTitle(label, "Video title viewport");
            foreach (var label in view.historyLabels) if (label != null) ClipTitle(label, "History title viewport");
            RefreshHistoryTitleBounds(view);
        }

        // History rows resize with their list; inherited fixed-width title clips must follow.
        public static void RefreshHistoryTitleBounds(PlaylistView view)
        {
            if (view == null) return;
            foreach (var label in view.historyLabels)
            {
                if (label == null) continue;
                var clip = label.rectTransform.parent as RectTransform;
                if (clip == null || clip.name != "History title viewport") continue;
                Undo.RecordObject(clip, "Fit history title to row width");
                var min = clip.anchorMin; min.x = 0; clip.anchorMin = min;
                var max = clip.anchorMax; max.x = 1; clip.anchorMax = max;
                var left = clip.offsetMin; left.x = 10; clip.offsetMin = left;
                var right = clip.offsetMax; right.x = -10; clip.offsetMax = right;
                if (clip.GetComponent<RectMask2D>() == null) Undo.AddComponent<RectMask2D>(clip.gameObject);
                Undo.RecordObject(label, "Fit history title inside row");
                label.horizontalOverflow = HorizontalWrapMode.Wrap;
                label.verticalOverflow = VerticalWrapMode.Truncate;
                label.resizeTextForBestFit = true;
                label.resizeTextMinSize = Mathf.Min(9, Mathf.Max(1, label.fontSize));
                label.resizeTextMaxSize = Mathf.Max(1, label.fontSize);
                PlaylistConnections.Save(clip); PlaylistConnections.Save(label);
            }
        }

        static void ClipTitle(Text label, string viewportName)
        {
            var rect = label.rectTransform;
            var parent = rect.parent as RectTransform;
            if (parent != null && parent.name != viewportName && parent.name != "Queue title viewport" && parent.name != "Title viewport")
            {
                var go = new GameObject(viewportName, typeof(RectTransform), typeof(RectMask2D));
                Undo.RegisterCreatedObjectUndo(go, "Clip long queue titles");
                var viewport = go.GetComponent<RectTransform>();
                viewport.SetParent(parent, false);
                viewport.SetSiblingIndex(rect.GetSiblingIndex());
                viewport.anchorMin = rect.anchorMin; viewport.anchorMax = rect.anchorMax;
                viewport.pivot = rect.pivot;
                viewport.sizeDelta = rect.sizeDelta; viewport.anchoredPosition3D = rect.anchoredPosition3D;
                viewport.localRotation = rect.localRotation; viewport.localScale = rect.localScale;
                Undo.RecordObject(rect, "Keep queue title bounds");
                Undo.SetTransformParent(rect, viewport, "Clip long queue titles");
                rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
                rect.offsetMin = rect.offsetMax = Vector2.zero;
                var position = rect.localPosition; position.z = 0f; rect.localPosition = position;
                rect.localRotation = Quaternion.identity; rect.localScale = Vector3.one;
                PlaylistConnections.Save(rect); PlaylistConnections.Save(viewport);
            }
            Undo.RecordObject(label, "Keep long queue title visible");
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            // Shrink long titles without changing row geometry.
            label.resizeTextForBestFit = true; label.resizeTextMinSize = 9; label.resizeTextMaxSize = Mathf.Max(9, label.fontSize);
            PlaylistConnections.Save(label);
        }

        // Legacy scene rows may still contain these; queue actions now live in video details.
        public static void HideLegacyRowButtons(PlaylistView view)
        {
            if (view == null) return;
            foreach (var button in view.queueAddButtons ?? new GameObject[0])
            {
                if (button == null || !button.activeSelf) continue;
                PlaylistUndo.RecordObject(button, "Hide legacy row queue action");
                button.SetActive(false); PlaylistConnections.Save(button);
            }
        }

        public static void EnsureQueueSkipButton(PlaylistView view)
        {
            if (view == null || view.queuePanel == null) return;
            var panel = view.queuePanel.transform;
            var rect = panel.Find("Skip queued playback") as RectTransform;
            if (rect == null)
            {
                float width = ((RectTransform)panel).rect.width;
                var next = Button(panel, "Skip queued playback", "次の動画へ", view, "_SkipQueuedPlayback", width - 116, 3, 104, 30, view.normalColor);
                rect = (RectTransform)next.transform;
                var caption = next.GetComponentInChildren<Text>(true); caption.fontSize = 14;
                PlaylistConnections.Save(caption);
            }
            RefreshQueueHeader(view);
        }

        // Migrate only the header controls; retain authored height, styling and events.
        public static void RefreshQueueHeader(PlaylistView view)
        {
            if (view == null || view.queuePanel == null) return;
            var panel = view.queuePanel.transform;
            var rect = panel.Find("Skip queued playback") as RectTransform;
            var add = panel.Find("Add video") as RectTransform;
            if (rect == null || add == null) return;
            var clear = panel.Find("Clear queue") as RectTransform;
            if (clear != null)
            {
                // Preserve the old right edge, including any manual margin adjustment.
                float right = clear.localPosition.x + clear.rect.xMax;
                PlaylistUndo.RecordObject(rect, "Move next video button to header edge");
                var position = rect.localPosition; position.x = right - rect.rect.xMax;
                rect.localPosition = position;
                PlaylistConnections.Save(rect);
            }
            float nextLeft = rect.localPosition.x + rect.rect.xMin;
            float addLeft = add.localPosition.x + add.rect.xMin;
            float width = nextLeft - 8f - addLeft;
            if (width > 0 && !Mathf.Approximately(width, add.rect.width))
            {
                PlaylistUndo.RecordObject(add, "Expand add video button");
                add.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
                var position = add.localPosition; position.x = addLeft - add.rect.xMin;
                add.localPosition = position;
                PlaylistConnections.Save(add);
            }
            foreach (string name in new[] { "Start queued playback", "Clear queue" })
            {
                var old = panel.Find(name);
                if (old != null) PlaylistUndo.DestroyObjectImmediate(old.gameObject);
            }
        }

        // Keep controls inside their actual row, including after ScrollRect lays it out.
        // Changes only the affected spacing.
        public static void RefreshQueueControlLayout(PlaylistView view)
        {
            if (view == null) return;
            var scroll = view.videoScroll;
            if (scroll != null && scroll.content != null && scroll.verticalScrollbar != null)
            {
                var layout = scroll.content.GetComponent<VerticalLayoutGroup>();
                int inset = Mathf.CeilToInt(((RectTransform)scroll.verticalScrollbar.transform).rect.width + 6f);
                if (layout != null && layout.padding.right < inset)
                {
                    Undo.RecordObject(layout, "Keep queue buttons clear of scrollbar");
                    var p = layout.padding;
                    layout.padding = new RectOffset(p.left, inset, p.top, p.bottom);
                    PlaylistConnections.Save(layout);
                }
            }
            foreach (var row in view.queueRows)
            {
                if (row == null) continue;
                var remove = row.transform.Find("Remove queued item") as RectTransform;
                if (remove == null) continue;
                Undo.RecordObject(remove, "Anchor queue delete button to row edge");
                float y = remove.anchoredPosition.y;
                var min = remove.anchorMin; var max = remove.anchorMax;
                min.x = max.x = 1f; remove.anchorMin = min; remove.anchorMax = max;
                var pivot = remove.pivot; pivot.x = 1f; remove.pivot = pivot;
                remove.anchoredPosition = new Vector2(-10f, y);
                PlaylistConnections.Save(remove);
            }
            foreach (var label in view.queueLabels)
            {
                if (label == null) continue;
                var area = label.rectTransform;
                if (area.parent.GetComponent<RectMask2D>() != null) area = (RectTransform)area.parent;
                Undo.RecordObject(area, "Keep queue title clear of delete button");
                var min = area.anchorMin; var max = area.anchorMax;
                min.x = 0f; max.x = 1f; area.anchorMin = min; area.anchorMax = max;
                var left = area.offsetMin; var right = area.offsetMax;
                left.x = 10f; right.x = -52f; area.offsetMin = left; area.offsetMax = right;
                PlaylistConnections.Save(area);
            }
        }

        public static void EnsureHistoryUrlPanel(PlaylistView view)
        {
            if (view == null || view.historyPanel == null) return;
            Undo.RecordObject(view, "Configure video details");
            float width = ((RectTransform)view.historyPanel.transform).rect.width;
            var panel = view.historyUrlPanel == null ? Box(view.historyPanel.transform.parent, "Video details", view.normalColor) : (RectTransform)view.historyUrlPanel.transform;
            if (panel.parent != view.historyPanel.transform.parent) Undo.SetTransformParent(panel, view.historyPanel.transform.parent, "Share video details screen");
            var source = (RectTransform)view.historyPanel.transform;
            panel.anchorMin = source.anchorMin; panel.anchorMax = source.anchorMax; panel.pivot = source.pivot;
            panel.sizeDelta = source.sizeDelta; panel.anchoredPosition3D = source.anchoredPosition3D;
            panel.SetAsLastSibling();
            foreach (string name in new[] { "Copy hint", "Select URL" }) { var old = panel.Find(name); if (old != null) old.gameObject.SetActive(false); }
            var titleNode = panel.Find("Heading");
            var title = titleNode == null ? Label(panel, "Heading", "", view, 20) : titleNode.GetComponent<Text>();
            PlaylistAppearance.Place(title.rectTransform, 12, 10, width - 24, 48);
            title.text = ""; title.supportRichText = false; FitTitle(title, 12, 20); view.detailTitle = title;
            var inputNode = panel.Find("URL input");
            var inputRect = inputNode == null ? Box(panel, "URL input", view.normalColor) : (RectTransform)inputNode;
            PlaylistAppearance.Place(inputRect, 12, 66, width - 24, 34);
            var oldInput = inputRect.GetComponent<VRC.SDK3.Components.VRCUrlInputField>();
            if (oldInput != null) Undo.DestroyObjectImmediate(oldInput);
            var textNode = inputRect.Find("Text");
            var text = textNode == null ? Label(inputRect, "Text", "", view, 16) : textNode.GetComponent<Text>();
            text.text = ""; text.supportRichText = false; text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.rectTransform.anchorMin = Vector2.zero; text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = new Vector2(8, 4); text.rectTransform.offsetMax = new Vector2(-8, -4);
            var input = inputRect.GetComponent<InputField>(); if (input == null) input = Undo.AddComponent<InputField>(inputRect.gameObject);
            input.textComponent = text; input.targetGraphic = inputRect.GetComponent<Image>();
            input.characterLimit = 0; input.readOnly = true; input.lineType = InputField.LineType.SingleLine;
            input.onEndEdit = new InputField.EndEditEvent(); input.onValueChanged = new InputField.OnChangeEvent();
            input.navigation = new Navigation { mode = Navigation.Mode.None };
            EnsureDetailButton(panel, "Play video", "再生", view, "_PlayDetails", 12, 116, (width - 32) / 2);
            view.detailQueueButton = EnsureDetailButton(panel, "Queue video", "キューへ追加", view, "_EnqueueDetails", (width + 8) / 2, 116, (width - 32) / 2).gameObject;
            view.detailQueueButton.SetActive(view.enableQueue);
            EnsureDetailButton(panel, "Close URL", "戻る", view, "_CloseHistoryUrl", 12, 166, width - 24);
            view.historyUrlPanel = panel.gameObject; view.detailUrlInput = input;
            EnsureUserDetailActions(view);
            panel.gameObject.SetActive(false);
            foreach (var component in panel.GetComponentsInChildren<Component>(true)) if (component != null) PlaylistConnections.Save(component);
            PlaylistConnections.Save(panel.gameObject); PlaylistConnections.Save(view);
            PlaylistAppearance.RefreshSupplementalTheme(view.GetComponent<PlaylistSetup>());
            PlaylistCompactLayout.Details(view);
        }
        public static void EnsureUserDetailActions(PlaylistView view)
        {
            if (view.userPanel != null)
            {
                var old = view.userPanel.transform.Find("Delete mode");
                if (old != null) Undo.DestroyObjectImmediate(old.gameObject);
            }
            if (view.historyUrlPanel == null) return;
            var panel = (RectTransform)view.historyUrlPanel.transform;
            var back = panel.Find("Close URL") as RectTransform;
            if (back == null) return;
            var button = panel.Find("Remove user video");
            if (button == null)
                button = EnsureDetailButton(panel, "Remove user video", "リストから削除", view, "_RemoveUserDetails", 12, -back.anchoredPosition.y, (panel.rect.width - 32) / 2).transform;
            Undo.RecordObject(view, "Configure user detail deletion");
            view.detailRemoveButton = button.gameObject; view.detailBackButton = back;
            view.detailRemoveButton.SetActive(false);
            PlaceDetailFooter(view);
            foreach (var c in button.GetComponentsInChildren<Component>(true)) if (c != null) PlaylistConnections.Save(c);
            PlaylistConnections.Save(button.gameObject); PlaylistConnections.Save(view);
        }
        public static void PlaceDetailFooter(PlaylistView view)
        {
            if (view == null || view.historyUrlPanel == null) return;
            foreach (string name in new[] { "Remove user video", "Close URL" })
            {
                var rect = view.historyUrlPanel.transform.Find(name) as RectTransform;
                if (rect == null) continue;
                Undo.RecordObject(rect, "Anchor detail delete and back buttons at bottom");
                float x = rect.anchoredPosition.x;
                rect.anchorMin = rect.anchorMax = Vector2.zero; rect.pivot = Vector2.zero;
                rect.anchoredPosition = new Vector2(x, 12);
                PlaylistConnections.Save(rect);
            }
        }
        static Button EnsureDetailButton(RectTransform panel, string name, string caption, PlaylistView view, string method, float x, float y, float width)
        {
            var node = panel.Find(name);
            var button = node == null ? Button(panel, name, caption, view, null, x, y, width, 38, view.normalColor) : node.GetComponent<Button>();
            PlaylistAppearance.Place((RectTransform)button.transform, x, y, width, 38);
            button.GetComponentInChildren<Text>(true).text = caption;
            button.onClick = new Button.ButtonClickedEvent();
            UnityEventTools.AddStringPersistentListener(button.onClick, UdonSharpEditorUtility.GetBackingUdonBehaviour(view).SendCustomEvent, method);
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            return button;
        }
        static void FitTitle(Text label, int min, int max)
        {
            Undo.RecordObject(label, "Wrap and fit video title");
            label.horizontalOverflow = HorizontalWrapMode.Wrap; label.verticalOverflow = VerticalWrapMode.Truncate;
            label.resizeTextForBestFit = true; label.resizeTextMinSize = min; label.resizeTextMaxSize = max;
            label.alignment = TextAnchor.MiddleLeft; PlaylistConnections.Save(label);
        }
        public static void EnsureRequestedPresentation(PlaylistView view)
        {
            EnsureHistoryUrlPanel(view);
            foreach (var labels in new[] { view.videoLabels, view.historyLabels, view.userLabels, view.queueLabels })
                foreach (var label in labels) if (label != null) FitTitle(label, 9, Mathf.Max(9, label.fontSize));
            HideLegacyRowButtons(view);
            if (view.descriptionLabel == null && view.heading != null)
            {
                var area = view.heading.rectTransform;
                if (area.parent.name == "Title viewport") area = (RectTransform)area.parent;
                var label = Label(area.parent, "Playlist description", "", view, 10);
                var rect = label.rectTransform;
                rect.anchorMin = area.anchorMin; rect.anchorMax = area.anchorMax; rect.pivot = area.pivot;
                rect.sizeDelta = area.sizeDelta; rect.anchoredPosition3D = area.anchoredPosition3D;
                float width = area.rect.width;
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width * .46f);
                rect.anchoredPosition += new Vector2(width * .54f, 0);
                Undo.RecordObject(area, "Reserve playlist description space");
                area.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width * .5f);
                label.horizontalOverflow = HorizontalWrapMode.Wrap; label.resizeTextForBestFit = true;
                label.resizeTextMinSize = 9; label.resizeTextMaxSize = label.fontSize;
                view.descriptionLabel = label; PlaylistConnections.Save(area); PlaylistConnections.Save(label);
            }
            EnsureLoadingIcon(view);
            PlaylistAppearance.UpdateHeaderPresentation(view);
            PlaylistConnections.Save(view);
        }

        // Only migrate the detail controls requested here; preserve authored panel bounds and styling.
        public static void CompactDetailsLayout(PlaylistView view)
        {
            if (view.detailTitle == null || view.detailUrlInput == null || view.historyUrlPanel == null) return;
            var title = view.detailTitle.rectTransform;
            Undo.RecordObject(title, "Compact video detail title");
            title.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 48);
            PlaylistConnections.Save(title);
            var input = view.detailUrlInput.GetComponent<RectTransform>();
            Undo.RecordObject(input, "Compact video detail URL");
            input.anchoredPosition = new Vector2(input.anchoredPosition.x, title.anchoredPosition.y - 56);
            input.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 34);
            PlaylistConnections.Save(input);
            Undo.RecordObject(view.detailUrlInput, "Single line detail URL");
            view.detailUrlInput.lineType = InputField.LineType.SingleLine;
            PlaylistConnections.Save(view.detailUrlInput);
            foreach (string name in new[] { "Play video", "Queue video", "Close URL" })
            {
                var node = view.historyUrlPanel.transform.Find(name) as RectTransform;
                if (node == null) continue;
                Undo.RecordObject(node, "Compact video detail actions");
                node.anchoredPosition = new Vector2(node.anchoredPosition.x, input.anchoredPosition.y - (name == "Close URL" ? 100 : 50));
                PlaylistConnections.Save(node);
            }
            PlaceDetailFooter(view);
        }

        public static void EnsureLoadingIcon(PlaylistView view)
        {
            if (view.videoScroll == null) return;
            if (view.loadingIcon == null)
            {
                var go = new GameObject("Playlist loading", typeof(RectTransform));
                Undo.RegisterCreatedObjectUndo(go, "Create playlist loading indicator");
                go.transform.SetParent(view.videoScroll.transform.parent, false);
                var rect = (RectTransform)go.transform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
                rect.sizeDelta = new Vector2(48, 48);
                var area = (RectTransform)view.videoScroll.transform;
                rect.position = area.TransformPoint(area.rect.center);
                Undo.RecordObject(view, "Connect playlist loading indicator");
                view.loadingIcon = go; go.SetActive(false); PlaylistConnections.Save(view);
            }
            var root = view.loadingIcon.transform;
            var legacy = root.GetComponent<Text>();
            Color color = legacy == null ? view.normalColor : legacy.color;
            if (legacy != null) { Undo.RecordObject(legacy, "Replace font loading glyph"); legacy.enabled = false; PlaylistConnections.Save(legacy); }
            // Geometric spokes stay visible independently of font glyph metrics and clipping.
            for (int i = 0; i < 8; i++)
            {
                string name = "Loading spoke " + i;
                var rect = root.Find(name) as RectTransform;
                if (rect == null)
                {
                    var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                    Undo.RegisterCreatedObjectUndo(go, "Create loading spoke");
                    go.transform.SetParent(root, false); rect = (RectTransform)go.transform;
                }
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
                float angle = i * 45f * Mathf.Deg2Rad;
                rect.anchoredPosition = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle)) * 15;
                rect.sizeDelta = new Vector2(4, 10); rect.localRotation = Quaternion.Euler(0, 0, -i * 45);
                var image = rect.GetComponent<Image>();
                image.color = new Color(color.r, color.g, color.b, color.a * (1f - i * .1f)); image.raycastTarget = false;
                PlaylistConnections.Save(rect); PlaylistConnections.Save(image);
            }
        }

        public static void Ensure(PlaylistSetup setup)
        {
            var view = setup.view;
            if (view == null) return;
            if (view.queuePanel == null)
            {
                PlaylistUndo.RecordObject(view, "Add playlist queue UI");
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
                Button(panel, "Skip queued playback", "次の動画へ", view, "_SkipQueuedPlayback", width - 116, 3, 104, 30, view.normalColor);
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
            RefreshQueueTitleClipping(view);
            EnsureTabLayout(setup);
            EnsureQueueSkipButton(view);
        }
        // Update captions without changing layout or button events.
        public static void UpdateQueueLabels(PlaylistView view)
        {
            if (view == null) return;
            if (view.queueButtonLabel != null) { PlaylistUndo.RecordObject(view.queueButtonLabel, "Rename queue tab"); view.queueButtonLabel.text = "再生キュー"; PlaylistConnections.Save(view.queueButtonLabel); }
            if (view.historyButtonLabel != null) { PlaylistUndo.RecordObject(view.historyButtonLabel, "Rename history tab"); view.historyButtonLabel.text = "再生履歴"; PlaylistConnections.Save(view.historyButtonLabel); }
            if (view.heading != null && (view.heading.text == "キュー" || view.heading.text == "再生待ちキュー")) { PlaylistUndo.RecordObject(view.heading, "Rename queue heading"); view.heading.text = "再生キュー"; PlaylistConnections.Save(view.heading); }
            if (view.queuePanel == null) return;
            var panel = view.queuePanel.transform;
            var heading = panel.Find("Queue heading");
            var headingLabel = heading == null ? null : heading.GetComponent<Text>();
            if (headingLabel != null) { PlaylistUndo.RecordObject(headingLabel, "Rename queue heading"); headingLabel.text = "再生キュー"; PlaylistConnections.Save(headingLabel); }
        }
        static void EnsureTabLayout(PlaylistSetup setup)
        {
            var view = setup.view;
            var toggle = view.queueButtonLabel == null ? null : view.queueButtonLabel.transform.parent;
            if (toggle == null) return;
            if (toggle.parent != setup.tabContent) {
                PlaylistUndo.SetTransformParent(toggle, setup.tabContent, "Move queue into playlist tabs");
                toggle.SetSiblingIndex(0);
                var rect = toggle.GetComponent<RectTransform>();
                PlaylistUndo.RecordObject(rect, "Match queue tab layout");
                rect.localScale = Vector3.one; rect.localRotation = Quaternion.identity;
                var layout = toggle.GetComponent<LayoutElement>();
                if (layout == null) layout = PlaylistUndo.AddComponent<LayoutElement>(toggle.gameObject);
                var templateLayout = setup.tabTemplate.GetComponent<LayoutElement>();
                if (templateLayout != null) EditorUtility.CopySerialized(templateLayout, layout);
                else layout.preferredHeight = setup.tabTemplate.GetComponent<RectTransform>().rect.height;
                var text = view.queueButtonLabel;
                var source = setup.tabTemplate.GetComponentInChildren<Text>(true);
                PlaylistUndo.RecordObject(text, "Match queue tab label");
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
                Button(panel, "Add video", "＋ 動画を追加", view, "_OpenQueueAdd", 8, 3, width - 132, 30, view.normalColor);
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
            PlaylistUndo.RegisterCreatedObjectUndo(go, "Add playlist queue UI"); go.GetComponent<Image>().color = color;
            return go.GetComponent<RectTransform>();
        }
        static Text Label(Transform parent, string name, string value, PlaylistView view, int size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text)); go.transform.SetParent(parent, false);
            PlaylistUndo.RegisterCreatedObjectUndo(go, "Add playlist queue label");
            var text = go.GetComponent<Text>(); text.text = value; text.fontSize = size;
            text.font = view.heading != null ? view.heading.font : AssetDatabase.LoadAssetAtPath<Font>(aki_lua87.AVEC.Editor.PackageLayout.Core + "/Fonts/ZenMaruGothic-Regular.ttf");
            text.color = view.iconColor; text.alignment = TextAnchor.MiddleLeft; text.raycastTarget = false;
            return text;
        }
    }
}
