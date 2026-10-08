using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.Components;
using VRC.SDKBase;

namespace aki_lua87.CommonGestureRemote.Editor
{
    public static class GestureGuideBuilder
    {
        public const string PrefabPath = aki_lua87.AVEC.Editor.PackageLayout.Prefabs + "/CommonGestureGuide.prefab";
        public static string Description(RemoteAction action)
        {
            switch (action) {
                case RemoteAction.NextVideo: return "次の動画へ（Playlist連携）";
                case RemoteAction.PreviousVideo: return "前の動画へ（Playlist連携）";
                case RemoteAction.VolumeUp: return "自分の音量を上げる";
                case RemoteAction.VolumeDown: return "自分の音量を下げる";
                case RemoteAction.MuteToggle: return "自分の音声をミュート／解除";
                case RemoteAction.Pause: return "現在の動画を一時停止";
                case RemoteAction.Resume: return "一時停止した動画を再生";
                case RemoteAction.PlaybackToggle: return "再生中は一時停止、一時停止中は再開";
                case RemoteAction.Restart: return "現在の動画を0秒から再生（配信を除く）";
                case RemoteAction.StopPlayback: return "動画の再生を停止";
                case RemoteAction.OrderedPlayback: return "順送りに設定（Playlist連携）";
                case RemoteAction.RepeatOne: return "現在の1動画を繰り返す";
                case RemoteAction.ShufflePlayback: return "シャッフルに設定（Playlist連携）";
                case RemoteAction.NextTab: return "次のPlaylistタブへ";
                case RemoteAction.PreviousTab: return "前のPlaylistタブへ";
                case RemoteAction.TogglePlaybackMode: return "再生モードを切り替える（従来互換）";
                case RemoteAction.RepeatToggle: return "1動画リピートを切り替える（従来互換）";
                case RemoteAction.CustomEvent: return "設定したカスタムイベントを実行";
                default: return "未設定";
            }
        }
        public static string BuildText(GestureData data)
        {
            string error = RemoteBaker.ValidateData(data);
            var text = new StringBuilder();
            if (error != null) text.Append(error);
            else foreach (var binding in data.bindings) {
                string arrows = RemoteBaker.Normalize(binding.gesture).Replace("R", "→").Replace("L", "←").Replace("U", "↑").Replace("D", "↓");
                if (text.Length > 0) text.Append("\n");
                text.Append(arrows).Append("   ").Append(binding.label);
            }
            return text.ToString();
        }
        public static void Refresh(GestureGuideSetup setup)
        {
            if (setup == null || setup.contents == null || setup.data == null) return;
            EnsureScrollbar(setup);
            string value = BuildText(setup.data);
            if (setup.contents.text != value) {
                Undo.RecordObject(setup.contents, "Update gesture guide"); setup.contents.text = value;
                RemoteConnections.Save(setup.contents);
            }
            // Expand only the guide's own text/scroll content to retain every configured binding.
            var content = setup.contents.rectTransform;
            float height = Mathf.Max(320, setup.contents.preferredHeight + 32);
            if (Mathf.Abs(content.rect.height - height) > .1f) {
                Undo.RecordObject(content, "Fit gesture guide contents");
                content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
                RemoteConnections.Save(content);
            }
        }
        static void EnsureScrollbar(GestureGuideSetup setup)
        {
            var scroll = setup.contents.GetComponentInParent<ScrollRect>();
            if (scroll == null || scroll.verticalScrollbar != null) return;
            var bar = new GameObject("Guide scrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
            Undo.RegisterCreatedObjectUndo(bar, "Gesture guide scrollbar");
            bar.transform.SetParent(scroll.transform, false);
            var rect = bar.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1, 0); rect.anchorMax = Vector2.one; rect.pivot = Vector2.one;
            rect.offsetMin = new Vector2(-24, 0); rect.offsetMax = Vector2.zero;
            bar.GetComponent<Image>().color = new Color(.09f, .14f, .2f);
            var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image)); handle.transform.SetParent(bar.transform, false);
            var handleRect = handle.GetComponent<RectTransform>(); handleRect.anchorMin = Vector2.zero; handleRect.anchorMax = Vector2.one;
            handleRect.offsetMin = handleRect.offsetMax = Vector2.zero;
            handle.GetComponent<Image>().color = new Color(.3f, 1f, .65f);
            var scrollbar = bar.GetComponent<Scrollbar>(); scrollbar.handleRect = handleRect; scrollbar.targetGraphic = handle.GetComponent<Image>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop; scrollbar.navigation = new Navigation { mode = Navigation.Mode.None };
            Undo.RecordObject(scroll, "Gesture guide scrollbar"); scroll.verticalScrollbar = scrollbar; scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            Undo.RecordObject(setup.contents.rectTransform, "Reserve guide scrollbar space"); setup.contents.rectTransform.offsetMax = new Vector2(-32, setup.contents.rectTransform.offsetMax.y);
            RemoteConnections.Save(scroll); RemoteConnections.Save(setup.contents.rectTransform);
        }
        public static void RefreshLoaded(GestureData data = null)
        {
            foreach (var setup in Object.FindObjectsOfType<GestureGuideSetup>(true))
                if (data == null || setup.data == data) Refresh(setup);
        }
        [MenuItem("Tools/AVEC/Remote/Create Gesture Guide Prefab")]
        public static void CreatePrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null) return;
            if (!AssetDatabase.IsValidFolder(aki_lua87.AVEC.Editor.PackageLayout.Prefabs))
                AssetDatabase.CreateFolder(aki_lua87.AVEC.Editor.PackageLayout.Root, "Prefabs");
            var scene = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
            try {
                var go = new GameObject("Common Gesture Guide", typeof(BoxCollider), typeof(Rigidbody), typeof(VRCPickup));
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, scene);
                go.GetComponent<BoxCollider>().size = new Vector3(.68f, .86f, .025f);
                var rb = go.GetComponent<Rigidbody>(); rb.useGravity = false; rb.mass = .3f; rb.drag = 8; rb.angularDrag = 8;
                var pickup = go.GetComponent<VRCPickup>(); pickup.DisallowTheft = true; pickup.AutoHold = VRC_Pickup.AutoHoldMode.Yes;
                pickup.InteractionText = "Gesture Guide / 操作一覧を持つ";
                var setup = go.AddComponent<GestureGuideSetup>(); setup.data = AssetDatabase.LoadAssetAtPath<GestureData>(RemoteFactory.DataPath);
                var canvas = new GameObject("Guide canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(VRCUiShape), typeof(Image));
                canvas.transform.SetParent(go.transform, false); canvas.transform.localPosition = new Vector3(0, 0, -.016f);
                canvas.transform.localScale = Vector3.one * .0008f;
                canvas.GetComponent<RectTransform>().sizeDelta = new Vector2(850, 1075);
                canvas.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
                canvas.GetComponent<CanvasScaler>().dynamicPixelsPerUnit = 4;
                canvas.GetComponent<Image>().color = new Color(.035f, .055f, .08f, 1);
                var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect)).GetComponent<RectTransform>();
                viewport.SetParent(canvas.transform, false); viewport.anchorMin = Vector2.zero; viewport.anchorMax = Vector2.one;
                viewport.offsetMin = new Vector2(26, 26); viewport.offsetMax = new Vector2(-26, -26);
                viewport.GetComponent<Image>().color = new Color(0, 0, 0, 0);
                var contents = new GameObject("Configured gestures", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
                contents.transform.SetParent(viewport, false); var rect = contents.rectTransform;
                rect.anchorMin = new Vector2(0, 1); rect.anchorMax = Vector2.one; rect.pivot = new Vector2(.5f, 1); rect.sizeDelta = new Vector2(0, 1040);
                contents.font = AssetDatabase.LoadAssetAtPath<Font>(aki_lua87.AVEC.Editor.PackageLayout.Core + "/Fonts/ZenMaruGothic-Regular.ttf");
                contents.fontSize = 24; contents.color = new Color(.93f, .96f, 1); contents.supportRichText = false;
                contents.alignment = TextAnchor.UpperLeft; contents.verticalOverflow = VerticalWrapMode.Overflow; contents.raycastTarget = false;
                var scroll = viewport.GetComponent<ScrollRect>(); scroll.viewport = viewport; scroll.content = rect;
                scroll.horizontal = false; scroll.vertical = true; scroll.scrollSensitivity = 0; scroll.movementType = ScrollRect.MovementType.Clamped;
                setup.contents = contents; Refresh(setup);
                UnityEditor.PrefabUtility.SaveAsPrefabAsset(go, PrefabPath);
            } finally { UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene); }
        }
    }
    [CustomEditor(typeof(GestureGuideSetup))]
    public class GestureGuideSetupEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox("リモコンと同じGesture Dataを設定してください。登録した項目を表示し、ビルド時にも更新します。パネル位置はローカルです。", MessageType.Info);
            EditorGUI.BeginChangeCheck(); DrawDefaultInspector();
            if (EditorGUI.EndChangeCheck() || GUILayout.Button("設定一覧を更新")) GestureGuideBuilder.Refresh((GestureGuideSetup)target);
        }
    }
}
