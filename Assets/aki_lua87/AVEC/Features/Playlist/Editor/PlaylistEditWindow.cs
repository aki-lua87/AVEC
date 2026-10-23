using aki_lua87.AVEC.Editor;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace aki_lua87.CommonVideoPlaylist.Editor
{
    public class PlaylistEditWindow : EditorWindow
    {
        [SerializeField] PlaylistData source;
        public PlaylistData Source => source;
        [SerializeField] PlaylistData draft;
        [SerializeField] string baseline;
        [SerializeField] int selectedTab;
        SerializedObject serializedDraft;
        ReorderableList tabs, videos;
        Vector2 tabScroll, videoScroll;
        bool loading;
        string message;
        MessageType messageType;

        public static void Open(PlaylistData data)
        {
            if (data == null) return;
            foreach (var existing in Resources.FindObjectsOfTypeAll<PlaylistEditWindow>())
                if (existing.source == data) { existing.Show(); existing.Focus(); return; }
            var window = CreateInstance<PlaylistEditWindow>();
            window.source = data;
            window.ReloadDraft();
            window.Show();
        }

        void OnEnable()
        {
            titleContent = new GUIContent("プレイリスト編集");
            minSize = new Vector2(760, 480);
            saveChangesMessage = "プレイリストの変更を保存しますか？ 保存すると使用中のUIへ反映します。";
            Undo.undoRedoPerformed += OnUndo;
            if (draft != null)
            {
                // Also repair drafts restored from the previous window implementation.
                draft.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSave;
                BuildLists();
            }
        }
        void OnDisable() { Undo.undoRedoPerformed -= OnUndo; }
        void OnDestroy() { if (draft != null) { Undo.ClearUndo(draft); DestroyImmediate(draft); } }
        void OnUndo() { if (draft != null) { BuildLists(); UpdateDirty(); Repaint(); } }

        public void RefreshIfClean() { if (!hasUnsavedChanges && source != null) { ReloadDraft(); Repaint(); } }
        void ReloadDraft()
        {
            if (draft != null) { Undo.ClearUndo(draft); DestroyImmediate(draft); }
            draft = Instantiate(source);
            // HideAndDontSave includes NotEditable, which disables PropertyField inputs.
            draft.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSave;
            baseline = JsonUtility.ToJson(source);
            hasUnsavedChanges = false;
            BuildLists();
        }
        void UpdateDirty() { hasUnsavedChanges = draft != null && JsonUtility.ToJson(draft) != baseline; }

        void BuildLists()
        {
            serializedDraft = new SerializedObject(draft);
            tabs = new ReorderableList(serializedDraft, serializedDraft.FindProperty("tabs"), true, true, true, true);
            tabs.drawHeaderCallback = r => EditorGUI.LabelField(r, "タブ（ドラッグで並べ替え）");
            tabs.drawElementCallback = (r, i, active, focused) =>
            {
                r.y += 2; r.height = EditorGUIUtility.singleLineHeight;
                var tab = tabs.serializedProperty.GetArrayElementAtIndex(i);
                EditorGUI.PropertyField(r, tab.FindPropertyRelative("name"), GUIContent.none);
            };
            tabs.onSelectCallback = l => { selectedTab = l.index; BuildVideos(); };
            tabs.onReorderCallback = l => { selectedTab = l.index; BuildVideos(); };
            tabs.onAddCallback = l =>
            {
                int i = l.serializedProperty.arraySize++;
                var tab = l.serializedProperty.GetArrayElementAtIndex(i);
                tab.FindPropertyRelative("name").stringValue = "新しいタブ";
                tab.FindPropertyRelative("titleSourceUrl").stringValue = "";
                tab.FindPropertyRelative("description").stringValue = "";
                tab.FindPropertyRelative("randomPlayback").boolValue = false;
                tab.FindPropertyRelative("videos").arraySize = 0;
                selectedTab = l.index = i; BuildVideos();
            };
            tabs.onRemoveCallback = l =>
            {
                ReorderableList.defaultBehaviours.DoRemoveButton(l);
                selectedTab = l.index; BuildVideos();
            };
            selectedTab = Mathf.Clamp(selectedTab, 0, tabs.count - 1);
            tabs.index = selectedTab;
            BuildVideos();
        }

        void BuildVideos()
        {
            videos = null;
            if (selectedTab < 0 || selectedTab >= tabs.count) return;
            var tab = tabs.serializedProperty.GetArrayElementAtIndex(selectedTab);
            var titleSource = tab.FindPropertyRelative("titleSourceUrl");
            titleSource.isExpanded = !string.IsNullOrWhiteSpace(titleSource.stringValue);
            videos = new ReorderableList(serializedDraft, tab.FindPropertyRelative("videos"), true, true, true, true);
            videos.drawHeaderCallback = r => EditorGUI.LabelField(r, "動画（ドラッグで並べ替え）");
            videos.elementHeightCallback = i => (EditorGUIUtility.singleLineHeight + 4) * 2 + 6;
            videos.drawElementCallback = (r, i, active, focused) =>
            {
                var entry = videos.serializedProperty.GetArrayElementAtIndex(i);
                r.y += 3; r.height = EditorGUIUtility.singleLineHeight;
                EditorGUI.PropertyField(r, entry.FindPropertyRelative("title"), new GUIContent((i + 1) + ". タイトル"));
                r.y += r.height + 4;
                EditorGUI.PropertyField(r, entry.FindPropertyRelative("url"), new GUIContent("動画URL"));

            };
            videos.onAddCallback = l =>
            {
                int i = l.serializedProperty.arraySize++;
                var entry = l.serializedProperty.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("title").stringValue = "新しい動画";
                entry.FindPropertyRelative("url").stringValue = "https://";
                entry.FindPropertyRelative("dynamicTitleIndex").intValue = -1;
                l.index = i;
            };
        }

        void OnGUI()
        {
            DrawSourceSelector();
            if (source == null) { EditorGUILayout.HelpBox("編集先にPlaylistDataアセットを指定してください。", MessageType.Info); return; }
            if (draft == null) ReloadDraft();
            if (serializedDraft == null) BuildLists();
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label(source.name, EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                GUILayout.Label(hasUnsavedChanges ? "未保存" : "保存済み");
                using (new EditorGUI.DisabledScope(loading || EditorApplication.isPlayingOrWillChangePlaymode))
                    if (GUILayout.Button("保存", EditorStyles.toolbarButton, GUILayout.Width(70))) { GUI.FocusControl(null); SaveChanges(); }
            }
            if (!string.IsNullOrEmpty(message)) EditorGUILayout.HelpBox(message, messageType);
            if (EditorApplication.isPlayingOrWillChangePlaymode) EditorGUILayout.HelpBox("Playモードを終了してから編集してください。", MessageType.Info);
            using (new EditorGUI.DisabledScope(loading || EditorApplication.isPlayingOrWillChangePlaymode))
            {
                serializedDraft.Update();
                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUILayout.VerticalScope(GUILayout.Width(240)))
                    {
                        tabScroll = EditorGUILayout.BeginScrollView(tabScroll);
                        tabs.DoLayoutList();
                        EditorGUILayout.EndScrollView();
                    }
                    using (new EditorGUILayout.VerticalScope())
                    {
                        videoScroll = EditorGUILayout.BeginScrollView(videoScroll);
                        if (videos != null)
                        {
                            var tab = tabs.serializedProperty.GetArrayElementAtIndex(selectedTab);
                            EditorGUILayout.PropertyField(tab.FindPropertyRelative("name"), new GUIContent("タブ名"));
                            EditorGUILayout.PropertyField(tab.FindPropertyRelative("description"), new GUIContent("説明"));
                            EditorGUILayout.PropertyField(tab.FindPropertyRelative("randomPlayback"), new GUIContent("ランダム再生", "ワールド内の「再生順」ボタンで切り替えられます。ランダム再生はタブ内を一巡して停止します。"));
                            videos.DoLayoutList();
                        }
                        else EditorGUILayout.HelpBox("左下の＋でタブを追加してください。", MessageType.Info);
                        EditorGUILayout.EndScrollView();
                        if (videos != null) DrawTitleSource(tabs.serializedProperty.GetArrayElementAtIndex(selectedTab));
                    }
                }
                if (serializedDraft.ApplyModifiedProperties()) UpdateDirty();
                EditorGUILayout.Space();
                FeatureExtensionRegistry.DrawButtons("Playlist", source);
                EditorGUILayout.LabelField("編集後に「保存」を押すと、UIへ反映します。", EditorStyles.wordWrappedMiniLabel);
            }
        }

        static void DrawTitleSource(SerializedProperty tab)
        {
            var url = tab.FindPropertyRelative("titleSourceUrl");
            url.isExpanded = EditorGUILayout.Foldout(url.isExpanded, "タイトル取得API URL (特殊な設定の場合に利用します。空欄でも問題はないです。)", true);
            if (url.isExpanded)
                using (new EditorGUI.IndentLevelScope()) EditorGUILayout.PropertyField(url, GUIContent.none);
        }

        void DrawSourceSelector()
        {
            using (new EditorGUI.DisabledScope(loading || EditorApplication.isPlayingOrWillChangePlaymode))
            {
                EditorGUI.BeginChangeCheck();
                var next = (PlaylistData)EditorGUILayout.ObjectField("編集先", source, typeof(PlaylistData), false);
                if (EditorGUI.EndChangeCheck() && next != source)
                {
                    GUI.FocusControl(null);
                    if (TryChangeSource(next)) GUIUtility.ExitGUI();
                }
            }
        }

        bool TryChangeSource(PlaylistData next)
        {
            if (next == source) return false;
            serializedDraft?.ApplyModifiedProperties();
            UpdateDirty();
            if (hasUnsavedChanges)
            {
                int choice = EditorUtility.DisplayDialogComplex("編集先を変更", "現在の編集内容を保存してから切り替えますか？",
                    "保存して切り替え", "キャンセル", "破棄して切り替え");
                if (choice == 1) return false;
                if (choice == 0)
                {
                    SaveChanges();
                    if (hasUnsavedChanges) return false;
                }
            }
            if (draft != null) { Undo.ClearUndo(draft); DestroyImmediate(draft); }
            draft = null; serializedDraft = null; tabs = null; videos = null;
            source = next; selectedTab = 0; tabScroll = videoScroll = Vector2.zero;
            baseline = null; message = null; hasUnsavedChanges = false;
            if (source != null) ReloadDraft();
            Repaint();
            return true;
        }

        public override void SaveChanges()
        {
            if (loading || source == null || draft == null || EditorApplication.isPlayingOrWillChangePlaymode) return;
            serializedDraft?.ApplyModifiedProperties();
            if (JsonUtility.ToJson(source) != baseline && !EditorUtility.DisplayDialog("保存先が変更されています", "別の操作で元のプレイリストが変更されています。現在の編集内容で上書きしますか？", "上書き保存", "キャンセル")) return;
            try
            {
                int count = SaveAndApply(source, draft);
                baseline = JsonUtility.ToJson(source);
                base.SaveChanges();
                messageType = MessageType.Info;
                message = "保存しました。" + count + " 個のUIへ反映しました。シーンの変更は通常のシーン保存で保存してください。";
            }
            catch (Exception e) { message = "保存・反映できませんでした: " + e.Message; messageType = MessageType.Error; }
        }

        public override void DiscardChanges() { if (source != null) ReloadDraft(); base.DiscardChanges(); }

        public static int SaveAndApply(PlaylistData target, PlaylistData edited)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Playモードでは保存できません。");
            if (target == null || edited == null) throw new ArgumentNullException();
            var setups = Resources.FindObjectsOfTypeAll<PlaylistSetup>().Where(s => !EditorUtility.IsPersistent(s) && s.gameObject.scene.IsValid() && s.gameObject.scene.isLoaded && s.data == target).ToArray();
            // Validate before changing data or destroying generated rows.
            foreach (var setup in setups)
                if (setup.view == null || setup.tabContent == null || setup.videoContent == null || setup.tabTemplate == null || setup.videoTemplate == null ||
                    setup.tabTemplate.GetComponentInChildren<UnityEngine.UI.Text>(true) == null || setup.videoTemplate.GetComponentInChildren<UnityEngine.UI.Text>(true) == null ||
                    setup.tabTemplate.GetComponent<UnityEngine.UI.Button>() == null || setup.videoTemplate.GetComponent<UnityEngine.UI.Button>() == null ||
                    setup.tabTemplate.GetComponent<UnityEngine.UI.Image>() == null || setup.videoTemplate.GetComponent<UnityEngine.UI.Image>() == null)
                    throw new InvalidOperationException(setup.name + " のUI参照またはテンプレートを確認してください。");
            foreach (var setup in setups) PlaylistBaker.ValidateDataRows(setup);
            // Saving commits the asset. Scene rows are a derived cache, not an Undo transaction.
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(edited), target);
            EditorUtility.SetDirty(target);
            AssetDatabase.SaveAssetIfDirty(target);
            var failures = new List<Exception>();
            foreach (var setup in setups)
                try { PlaylistBaker.ApplyData(setup, false); }
                catch (Exception e) { failures.Add(new InvalidOperationException(setup.name + ": " + e.Message, e)); }
            if (failures.Count > 0)
                throw new AggregateException("アセットは保存済みですが、一部のUIへの反映に失敗しました。参照を確認して再保存してください。", failures);
            return setups.Length;
        }
    }
}
