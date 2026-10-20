using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UdonSharpEditor;
using aki_lua87.AVEC.Editor;
using aki_lua87.CommonVideoPlaylist;
using aki_lua87.CommonVideoPlaylist.Editor;

namespace aki_lua87.AVEC.Extensions.DynamicPlaylist.Editor
{
    [InitializeOnLoad]
    public static class DynamicPlaylistExtension
    {
        static DynamicPlaylistExtension() => FeatureExtensionRegistry.Register(new FeatureEditorExtension
        {
            Id = "DynamicPlaylist",
            Feature = "Playlist",
            Label = "aki_lua87's Playlist Generator を開く",
            Open = DynamicPlaylistWindow.Open,
            Bake = Apply
        });
        public static void Apply(Component component)
        {
            var setup = component as PlaylistSetup;
            if (setup == null || setup.view == null) return;
            var view = setup.view;
            bool needed = view.titleSourceUrls != null && view.titleSourceUrls.Any(u => !VRC.SDKBase.VRCUrl.IsNullOrEmpty(u));
            var provider = view.GetComponent<DynamicPlaylistTitles>();
            bool added = needed && provider == null;
            if (added) provider = PlaylistUndo.AddUdonSharpComponent<DynamicPlaylistTitles>(view.gameObject);
            var providers = new List<FeatureExtensionBehaviour>(view.extensions ?? new FeatureExtensionBehaviour[0]);
            providers.RemoveAll(p => p == null || p is DynamicPlaylistTitles);
            PlaylistUndo.RecordObject(view, "Wire dynamic playlist extension");
            if (provider != null)
            {
                PlaylistUndo.RecordObject(provider, "Wire dynamic playlist titles");
                provider.view = view;
                provider.enabled = needed;
                if (needed) providers.Add(provider);
                PlaylistConnections.Save(provider);
            }
            view.extensions = providers.ToArray();
            PlaylistConnections.Save(view);
            // Adding another UdonBehaviour changes the SDK's component signature for this object.
            // Preserve its existing ID while updating only the affected object's metadata.
            if (added && view.gameObject.scene.IsValid())
                foreach (var descriptor in UnityEngine.Object.FindObjectsOfType<VRC.SDK3.Components.VRCSceneDescriptor>(true))
                {
                    if (descriptor.gameObject.scene != view.gameObject.scene) continue;
                    var pair = descriptor.NetworkIDCollection.FirstOrDefault(p => p.gameObject == view.gameObject);
                    if (pair == null) continue;
                    PlaylistUndo.RecordObject(descriptor, "Update dynamic playlist network ID metadata");
                    pair.SerializedTypeNames = VRC.SDKBase.Network.NetworkIDAssignment.GetSerializedTypes(view.gameObject);
                    PlaylistConnections.Save(descriptor);
                }
        }
    }

    public class DynamicPlaylistWindow : EditorWindow
    {
        [SerializeField] PlaylistData target;
        [SerializeField] string input = "";
        [SerializeField] int sourceKind;
        [SerializeField] bool dynamicUrls = true;
        [SerializeField] PlaylistTab preview;
        string message;
        MessageType messageType;
        bool loading;
        Vector2 scroll;
        int revision;
        void OnEnable() { titleContent = new GUIContent("aki_lua87's Playlist Generator"); }
        [MenuItem("Tools/AVEC/Extensions/aki_lua87's Playlist Generator")]
        public static void ShowWindow() => Open(Selection.activeObject);
        public static void Open(UnityEngine.Object context)
        {
            var window = GetWindow<DynamicPlaylistWindow>("aki_lua87's Playlist Generator");
            if (context is PlaylistData data) window.target = data;
            if (context is PlaylistSetup setup) window.target = setup.data;
            window.minSize = new Vector2(560, 400); window.Show();
        }
        void OnGUI()
        {
            using (new EditorGUI.DisabledScope(loading || EditorApplication.isPlayingOrWillChangePlaymode))
            {
                target = (PlaylistData)EditorGUILayout.ObjectField("挿入先プレイリスト", target, typeof(PlaylistData), false);
                if (target != null)
                {
                    EditorGUILayout.LabelField(AssetDatabase.GetAssetPath(target), EditorStyles.wordWrappedMiniLabel);
                }
                EditorGUI.BeginChangeCheck();
                sourceKind = EditorGUILayout.Popup("取り込み元", sourceKind, new[] { "YouTubeチャンネル", "YouTubeプレイリスト", "YouTube検索" });
                input = EditorGUILayout.TextField(sourceKind == 2 ? "検索キーワード" : sourceKind == 0 ? "チャンネルID / @ハンドル / URL" : "ID / URL", input);
                if (sourceKind == 2)
                {
                    using (new EditorGUI.DisabledScope(true)) EditorGUILayout.Toggle("動的URLを使用", true);
                }
                else dynamicUrls = EditorGUILayout.Toggle("動的URLを使用", dynamicUrls);
                if (EditorGUI.EndChangeCheck()) { preview = null; revision++; }
                EditorGUILayout.HelpBox(sourceKind == 2
                    ? "固定キーワードの検索結果を新着順で表示する動的タブを作成します（最大20枠）。結果はサーバーの時刻単位キャッシュに従って更新されます。タブ名はプレビューで変更できます。"
                    : "動的URLは外部サービスの最新一覧に追従します。OFFでは取得時の固定URLを挿入します。", MessageType.Info);
                if (sourceKind == 2 || dynamicUrls) EditorGUILayout.LabelField("動的URLにはAllow Untrusted URLsが必要です。", EditorStyles.wordWrappedMiniLabel);
                if (GUILayout.Button(loading ? "取得中…" : "取得してプレビュー")) FetchPreview();
                if (!string.IsNullOrEmpty(message)) EditorGUILayout.HelpBox(message, messageType);
                if (preview != null)
                {
                    preview.name = EditorGUILayout.TextField("挿入するタブ名", preview.name);
                    EditorGUILayout.LabelField("動画数", preview.videos.Length.ToString());
                    scroll = EditorGUILayout.BeginScrollView(scroll);
                    foreach (var video in preview.videos) EditorGUILayout.LabelField(video.title, video.url, EditorStyles.wordWrappedMiniLabel);
                    EditorGUILayout.EndScrollView();
                    using (new EditorGUI.DisabledScope(target == null))
                        if (GUILayout.Button("このタブを挿入して保存", GUILayout.Height(30))) InsertPreview();
                }
            }
        }
        async void FetchPreview()
        {
            int request = ++revision;
            loading = true; preview = null; message = "取得中…"; messageType = MessageType.Info;
            try
            {
                var tab = sourceKind == 2 ? await YoutubeSearchImport.Fetch(input)
                    : sourceKind == 1 ? await YoutubePlaylistImport.Fetch(input, dynamicUrls)
                    : await YoutubeChannelImport.Fetch(input, dynamicUrls);
                if (this == null || request != revision) return;
                preview = tab; message = "取得しました。内容を確認して挿入してください。";
            }
            catch (Exception e) { if (this != null) { message = e.Message; messageType = MessageType.Error; } }
            finally { if (this != null) { loading = false; Repaint(); } }
        }
        public static int Insert(PlaylistData target, PlaylistTab tab)
        {
            if (target == null || tab == null) throw new ArgumentNullException();
            if (Resources.FindObjectsOfTypeAll<PlaylistEditWindow>().Any(w => w.Source == target && w.hasUnsavedChanges))
                throw new InvalidOperationException("本体の編集ウィンドウに未保存の変更があります。保存または破棄してから挿入してください。");
            var edited = Instantiate(target);
            try
            {
                var list = new List<PlaylistTab>(edited.tabs ?? new PlaylistTab[0]);
                // Copy preview: edits in this window must not mutate the inserted asset.
                list.Add(JsonUtility.FromJson<PlaylistTab>(JsonUtility.ToJson(tab)));
                edited.tabs = list.ToArray();
                int count = PlaylistEditWindow.SaveAndApply(target, edited);
                foreach (var window in Resources.FindObjectsOfTypeAll<PlaylistEditWindow>()) if (window.Source == target) window.RefreshIfClean();
                return count;
            }
            finally { DestroyImmediate(edited); }
        }
        void InsertPreview()
        {
            try { int count = Insert(target, preview); preview = null; messageType = MessageType.Info; message = "挿入して保存しました。" + count + " 個のUIへ反映しました。シーンは通常の操作で保存してください。"; }
            catch (Exception e) { message = e.Message; messageType = MessageType.Error; }
        }
    }
}
