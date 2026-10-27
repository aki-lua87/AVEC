using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UdonSharp;
using UdonSharpEditor;
using VRC.SDK3.Components;

namespace aki_lua87.CommonVideoPlaylist.Editor
{
    // Additive configuration only. Existing rows, transforms, themes and player wiring survive.
    public static class PlaylistSpecialLists
    {
        public static void Apply(PlaylistSetup setup)
        {
            if (setup == null || setup.view == null) return;
            var view = setup.view;
            Undo.RecordObject(view, "Configure playlist special lists");
            view.enableQueue = setup.enableQueue; view.enableHistory = setup.enableHistory; view.enableUserList = setup.enableUserList;
            if (setup.enableUserList && view.userPanel == null) { CreateUserUI(setup); PlaylistThemeGraphics.ApplyNew(setup); }
            PlaylistPresentation.ScrollUserList(view);
            PlaceUserAddButton(view);
            view.queueAddButtons = setup.GetComponentsInChildren<PlaylistButton>(true).Where(b => b.enqueue).Select(b => b.gameObject).ToArray();
            if (setup.enableUserList && setup.gameObject.scene.IsValid() && !EditorUtility.IsPersistent(setup) &&
                !EditorSceneManager.IsPreviewScene(setup.gameObject.scene) && PrefabStageUtility.GetPrefabStage(setup.gameObject) == null)
                EnsureStorage(setup);
            aki_lua87.AVEC.Editor.FeatureExtensionRegistry.Configure("Playlist", setup);
            // Record visibility directly so checkbox changes also survive Undo and prefab overrides.
            SetVisible(view.queueTabImage == null ? null : view.queueTabImage.gameObject, setup.enableQueue);
            SetVisible(view.historyTabImage == null ? null : view.historyTabImage.gameObject, setup.enableHistory);
            SetVisible(view.userTabImage == null ? null : view.userTabImage.gameObject, setup.enableUserList);
            foreach (var button in view.queueAddButtons) SetVisible(button, setup.enableQueue);
            if (!setup.enableQueue) SetVisible(view.queuePanel, false);
            if (!setup.enableHistory) SetVisible(view.historyPanel, false);
            if (!setup.enableUserList) SetVisible(view.userPanel, false);
            // If a disabled panel was selected in the editor, leave a usable initial view.
            if (!setup.enableQueue && (view.historyPanel == null || !view.historyPanel.activeSelf) && (view.userPanel == null || !view.userPanel.activeSelf))
            {
                // Set editor visibility directly; runtime tab methods can dispatch web extensions.
                SetVisible(view.historyPanel, setup.enableHistory);
                SetVisible(view.userPanel, !setup.enableHistory && setup.enableUserList);
                bool regular = !setup.enableHistory && !setup.enableUserList;
                SetVisible(view.videoScroll == null ? null : view.videoScroll.gameObject, regular);
                if (view.heading != null) { Undo.RecordObject(view.heading, "Select enabled playlist list"); view.heading.text = setup.enableHistory ? "再生履歴" : setup.enableUserList ? "ユーザーリスト" : view.tabNames.Length > 0 ? view.tabNames[0] : "Playlist"; PlaylistConnections.Save(view.heading); }
                SetColor(view.queueTabImage, view.normalColor);
                SetColor(view.historyTabImage, setup.enableHistory ? view.selectedColor : view.normalColor);
                SetColor(view.userTabImage, !setup.enableHistory && setup.enableUserList ? view.selectedColor : view.normalColor);
                for (int i = 0; i < view.tabImages.Length; i++) SetColor(view.tabImages[i], regular && i == 0 ? view.selectedColor : view.normalColor);
                if (regular) for (int i = 0; i < view.videoImages.Length; i++) if (view.videoImages[i] != null) SetVisible(view.videoImages[i].gameObject, i < view.videoTabs.Length && view.videoTabs[i] == 0);
            }
            PlaylistTabIcons.Apply(setup);
            PlaylistConnections.Save(view); PlaylistConnections.Save(setup);
            PlaylistTabletBuilder.RefreshBindings(setup);
        }
        static void SetVisible(GameObject target, bool active)
        {
            if (target == null || target.activeSelf == active) return;
            Undo.RecordObject(target, "Change special list visibility"); target.SetActive(active); PlaylistConnections.Save(target);
        }
        static void SetColor(Image image, Color color)
        {
            if (image == null || image.color == color) return;
            Undo.RecordObject(image, "Select enabled playlist tab"); image.color = color; PlaylistConnections.Save(image);
        }
        static void EnsureStorage(PlaylistSetup setup)
        {
            var scene = setup.gameObject.scene;
            var stores = UnityEngine.Object.FindObjectsOfType<PlaylistUserData>(true).Where(s => s.gameObject.scene == scene).ToArray();
            if (stores.Length > 1) throw new InvalidOperationException("ユーザーリスト保存用PlayerObjectが複数あります。既存のNetwork IDを保持したまま参照を確認してください。");
            PlaylistUserData store;
            if (stores.Length == 0)
            {
                var go = new GameObject("CommonVideoPlaylist User Data");
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, scene);
                Undo.RegisterCreatedObjectUndo(go, "Add persistent user playlist");
                Undo.AddComponent<VRCPlayerObject>(go);
                Undo.AddComponent<VRCEnablePersistence>(go);
                store = go.AddUdonSharpComponent<PlaylistUserData>();
            }
            else store = stores[0];
            if (store.GetComponent<VRCPlayerObject>() == null || store.GetComponent<VRCEnablePersistence>() == null)
                throw new InvalidOperationException("既存ユーザーリスト保存オブジェクトのPersistenceコンポーネントを確認してください。");
            Undo.RecordObject(store, "Wire persistent playlist views");
            store.views = UnityEngine.Object.FindObjectsOfType<PlaylistSetup>(true)
                .Where(s => s.gameObject.scene == scene && s.enableUserList && s.view != null).Select(s => s.view).Distinct().ToArray();
            PlaylistConnections.Save(store);
            var backing = UdonSharpEditorUtility.GetBackingUdonBehaviour(store);
            if (backing == null || backing.programSource == null) throw new InvalidOperationException("ユーザーリストのUdonプログラムが未設定です。");
        }
        static void CreateUserUI(PlaylistSetup setup)
        {
            var view = setup.view;
            if (setup.tabContent == null || view.queueTabImage == null || view.queuePanel == null) throw new InvalidOperationException("ユーザーリスト追加に必要な既存キューUIがありません。");
            var tab = UnityEngine.Object.Instantiate(view.queueTabImage.gameObject, setup.tabContent, false);
            tab.name = "User list toggle"; Undo.RegisterCreatedObjectUndo(tab, "Add user list tab");
            tab.transform.SetSiblingIndex(view.historyTabImage == null ? 1 : view.historyTabImage.transform.GetSiblingIndex() + 1);
            var tabLabel = tab.GetComponentInChildren<Text>(true); tabLabel.text = "ユーザーリスト";
            tabLabel.resizeTextForBestFit = true; tabLabel.resizeTextMinSize = 14; tabLabel.resizeTextMaxSize = Mathf.Max(14, tabLabel.fontSize);
            tabLabel.horizontalOverflow = HorizontalWrapMode.Wrap; tabLabel.verticalOverflow = VerticalWrapMode.Truncate;
            Wire(tab.GetComponent<Button>(), view, "_ToggleUserList"); view.userTabImage = tab.GetComponent<Image>();
            view.userTabImage.color = view.normalColor;
            var source = view.queuePanel.GetComponent<RectTransform>();
            var panel = Box(setup.transform, "User list panel", view.queuePanel.GetComponent<Image>().color);
            panel.anchorMin = source.anchorMin; panel.anchorMax = source.anchorMax; panel.pivot = source.pivot;
            panel.anchoredPosition = source.anchoredPosition; panel.sizeDelta = source.sizeDelta;
            panel.localScale = source.localScale; panel.localRotation = source.localRotation;
            float width = source.rect.width; float height = source.rect.height;
            AddButton(panel, "Add user video", "＋ 動画を追加", view, "_OpenUserAdd", 8, 3, width - 148, 30);
            view.userRows = new GameObject[6]; view.userLabels = new Text[6];
            float rowHeight = Mathf.Max(24, (height - 78) / 6f - 4);
            for (int i = 0; i < 6; i++)
            {
                var row = Box(panel, "User row " + i, view.normalColor);
                PlaylistAppearance.Place(row, 8, 6 + i * (rowHeight + 4), width - 16, rowHeight);
                var label = Label(row, "Title", "", view); Stretch(label.rectTransform, 10, 4);
                label.supportRichText = false; label.resizeTextForBestFit = true; label.resizeTextMinSize = 14; label.resizeTextMaxSize = label.fontSize;
                var button = row.gameObject.AddComponent<Button>(); button.targetGraphic = row.GetComponent<Image>();
                var navigation = button.navigation; navigation.mode = Navigation.Mode.None; button.navigation = navigation;
                var click = row.gameObject.AddUdonSharpComponent<PlaylistButton>(); click.view = view; click.index = i; click.userSlot = true;
                PlaylistConnections.Save(click);
                UnityEventTools.AddStringPersistentListener(button.onClick, UdonSharpEditorUtility.GetBackingUdonBehaviour(click).SendCustomEvent, "_Click");
                view.userRows[i] = row.gameObject; view.userLabels[i] = label; row.gameObject.SetActive(false);
            }
            view.userPageLabel = Label(panel, "User page", "読み込み中…", view); view.userPageLabel.alignment = TextAnchor.MiddleCenter;
            PlaylistAppearance.Place(view.userPageLabel.rectTransform, 114, height - 34, width - 228, 28);
            var form = Box(panel, "Add user video form", panel.GetComponent<Image>().color);
            PlaylistAppearance.Place(form, 8, 38, width - 16, 132);
            var heading = Label(form, "Input heading", "動画URLを入力", view); PlaylistAppearance.Place(heading.rectTransform, 12, 8, width - 40, 28);
            var inputRect = Box(form, "Video URL", view.normalColor); PlaylistAppearance.Place(inputRect, 12, 42, width - 56, 36);
            var text = Label(inputRect, "Text", "", view); Stretch(text.rectTransform, 8, 2); text.supportRichText = false;
            var hint = Label(inputRect, "Placeholder", "https://…", view); Stretch(hint.rectTransform, 8, 2);
            var input = inputRect.gameObject.AddComponent<VRCUrlInputField>(); input.textComponent = text; input.placeholder = hint;
            input.targetGraphic = inputRect.GetComponent<Image>(); input.characterLimit = 2048; input.AllowSendingOnEndEdit = false;
            AddButton(form, "Submit user URL", "追加", view, "_SubmitUserUrl", width - 198, 90, 76, 30);
            AddButton(form, "Cancel user URL", "閉じる", view, "_CloseUserAdd", width - 110, 90, 76, 30);
            view.userUrlInput = input; view.userAddPanel = form.gameObject; view.userPanel = panel.gameObject;
            form.gameObject.SetActive(false); panel.gameObject.SetActive(false);
            PlaceUserAddButton(view);
            foreach (var c in panel.GetComponentsInChildren<Component>(true)) if (c != null) PlaylistConnections.Save(c);
            foreach (var c in tab.GetComponentsInChildren<Component>(true)) if (c != null) PlaylistConnections.Save(c);
        }
        public static void PlaceUserAddButton(PlaylistView view)
        {
            if (view == null || view.userPanel == null) return;
            var panel = (RectTransform)view.userPanel.transform;
            var button = panel.Find("Add user video") as RectTransform;
            if (button == null) return;
            Undo.RecordObject(button, "Fix user add button in footer");
            button.anchorMin = new Vector2(.75f, 0); button.anchorMax = new Vector2(1, 0);
            button.pivot = new Vector2(1, 0); button.sizeDelta = new Vector2(-16, 30);
            button.anchoredPosition = new Vector2(-8, 8);
            PlaylistConnections.Save(button);
            var viewport = panel.Find("List viewport") as RectTransform;
            if (viewport != null)
            {
                Undo.RecordObject(viewport, "Reserve fixed user list footer");
                viewport.offsetMin = new Vector2(viewport.offsetMin.x, 46);
                viewport.offsetMax = new Vector2(viewport.offsetMax.x, -6);
                PlaylistConnections.Save(viewport);
            }
            if (view.userPageLabel != null)
            {
                var count = view.userPageLabel.rectTransform;
                Undo.RecordObject(count, "Keep user count beside add button");
                count.anchorMin = Vector2.zero; count.anchorMax = new Vector2(.75f, 0);
                count.pivot = Vector2.zero; count.sizeDelta = new Vector2(-24, 28);
                count.anchoredPosition = new Vector2(8, 8); PlaylistConnections.Save(count);
            }
        }
        static RectTransform Box(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image)); go.transform.SetParent(parent, false);
            Undo.RegisterCreatedObjectUndo(go, "Add user playlist UI"); go.GetComponent<Image>().color = color; return (RectTransform)go.transform;
        }
        static Text Label(Transform parent, string name, string caption, PlaylistView view)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text)); go.transform.SetParent(parent, false);
            Undo.RegisterCreatedObjectUndo(go, "Add user playlist label"); var label = go.GetComponent<Text>();
            var source = view.queueLabels.FirstOrDefault(t => t != null) ?? view.heading;
            label.text = caption; label.font = source.font; label.fontSize = source.fontSize; label.color = source.color;
            label.alignment = TextAnchor.MiddleLeft; label.raycastTarget = false; label.supportRichText = false; return label;
        }
        static void Stretch(RectTransform rect, float x, float y)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(x, y); rect.offsetMax = new Vector2(-x, -y);
        }
        static Button AddButton(Transform parent, string name, string caption, PlaylistView view, string method, float x, float y, float width, float height)
        {
            var rect = Box(parent, name, view.normalColor); PlaylistAppearance.Place(rect, x, y, width, height);
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = rect.GetComponent<Image>();
            var nav = button.navigation; nav.mode = Navigation.Mode.None; button.navigation = nav;
            var label = Label(rect, "Label", caption, view); label.alignment = TextAnchor.MiddleCenter; Stretch(label.rectTransform, 0, 0);
            Wire(button, view, method); return button;
        }
        static void Wire(Button button, PlaylistView view, string method)
        {
            button.onClick = new Button.ButtonClickedEvent();
            UnityEventTools.AddStringPersistentListener(button.onClick, UdonSharpEditorUtility.GetBackingUdonBehaviour(view).SendCustomEvent, method);
            PlaylistConnections.Save(button);
        }
    }
}
