using aki_lua87.AVEC.Editor;
using System;
using System.Collections.Generic;
using System.Linq;
using UdonSharp;
using UdonSharpEditor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace aki_lua87.CommonVideoPlaylist.Editor
{
    [CustomEditor(typeof(PlaylistData))]
    public class PlaylistDataEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var data = (PlaylistData)target;
            EditorGUILayout.LabelField("タブ数", (data.tabs == null ? 0 : data.tabs.Length).ToString());
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
                if (GUILayout.Button("プレイリストを編集する")) PlaylistEditWindow.Open(data);
            FeatureExtensionRegistry.DrawButtons("Playlist", data);
            EditorGUILayout.HelpBox("専用ウィンドウでタブと動画を編集し、「保存」でUIへ反映します。", MessageType.Info);
        }
    }

    public static class PlaylistConnections
    {
        // Editor-only type lookup. Core has no compiled references to optional players.
        public static string[] Kinds => ConnectorRegistry.All.Select(d => d.DisplayName).ToArray();
        public static string[] PlayerTypes => ConnectorRegistry.All.Select(d => d.PlayerType).ToArray();
        public static string[] AdapterTypes => ConnectorRegistry.All.Select(d => d.PlaylistAdapterType).ToArray();
        public static Type Resolve(string name) => AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name)).FirstOrDefault(t => t != null);
        public static bool CanConnect(PlaylistSetup setup) => setup != null && !EditorUtility.IsPersistent(setup) && setup.gameObject.scene.IsValid() && PrefabStageUtility.GetPrefabStage(setup.gameObject) == null;
        public static List<Component> Find(PlaylistSetup setup)
        {
            var result = new List<Component>();
            if (!CanConnect(setup)) return result;
            foreach (string name in PlayerTypes)
            {
                var type = Resolve(name);
                if (type == null) continue;
                foreach (var component in UnityEngine.Object.FindObjectsOfType(type, true).OfType<Component>())
                    if (component.gameObject.scene == setup.gameObject.scene) result.Add(component);
            }
            return result;
        }
        public static int Kind(Component player) => player == null ? -1 : Array.IndexOf(PlayerTypes, player.GetType().FullName);
        public static string Label(Component player)
        {
            if (player == null) return "Missing";
            string path = player.name;
            for (var p = player.transform.parent; p != null; p = p.parent) path = p.name + "/" + path;
            int kind = Kind(player);
            return (kind < 0 ? "Unsupported" : Kinds[kind]) + " | " + path;
        }
        public static string Validate(PlaylistSetup setup)
        {
            if (setup.view == null) return "PlaylistViewを設定してください。";
            if (setup.player == null) return "未接続／参照切れです。プレイヤーを配置し、接続先を選択してください。";
            if (setup.player.gameObject.scene != setup.gameObject.scene) return "別シーンの参照です。同じシーンのプレイヤーを選択してください。";
            int kind = Kind(setup.player);
            if (kind < 0) return "対応していないコンポーネントです。候補から選択してください。";
            if (setup.playerKind != Kinds[kind]) return "種類が一致しません。「接続設定を修復」で修正してください。";
            var type = Resolve(AdapterTypes[kind]);
            if (type == null) return Kinds[kind] + " Connectorが不足しています。対応UnityPackageを導入してください。";
            if (setup.view.adapter == null || setup.view.adapter.GetType() != type) return "接続アダプターが不正です。「接続設定を修復」で修正してください。";
            if (Authority(setup.player, kind) == null || setup.view.adapter.gameObject != Authority(setup.player, kind).gameObject) return "連続再生用の接続設定を更新してください。「接続設定を修復」で更新できます。";
            if (Array.IndexOf(setup.view.adapter.views, setup.view) < 0) return "プレイリストの連続再生参照が未設定です。「接続設定を修復」で更新してください。";
            var customError = ConnectorRegistry.All[kind].ValidatePlaylist?.Invoke(setup.view.adapter, setup.player);
            if (customError != null) return customError;
            return null;
        }
        public static bool Connect(PlaylistSetup setup, Component player)
        {
            if (!CanConnect(setup) || setup.view == null || player == null || player.gameObject.scene != setup.gameObject.scene) return false;
            int kind = Kind(player);
            if (kind < 0) return false;
            Type type = Resolve(AdapterTypes[kind]);
            if (type == null) return false;
            Undo.RecordObjects(new UnityEngine.Object[] { setup, setup.view }, "Connect playlist");
            var authority = Authority(player, kind);
            if (authority == null) return false;
            var adapter = authority.GetComponents<PlaylistAdapter>().FirstOrDefault(a => a.GetType() == type);
            if (adapter == null)
            {
                adapter = (PlaylistAdapter)UdonSharpUndo.AddComponent(authority.gameObject, type);
                // Preserve options from the old per-UI adapter when upgrading an existing scene.
                var previous = setup.GetComponents<PlaylistAdapter>().FirstOrDefault(a => a.GetType() == type);
                if (previous != null)
                    foreach (string option in new[] { "streamMode", "playerIndex" })
                    {
                        var field = type.GetField(option);
                        if (field != null) field.SetValue(adapter, field.GetValue(previous));
                    }
            }
            Undo.RecordObject(adapter, "Connect playlist adapter");
            type.GetField("player").SetValue(adapter, player);
            ConnectorRegistry.All[kind].ConfigurePlaylist?.Invoke(adapter, player);
            setup.player = player;
            setup.playerKind = Kinds[kind];
            setup.view.adapter = adapter;
            var listenerField = type.GetField("sequenceListener");
            if (listenerField != null)
            {
                var listenerType = listenerField.FieldType;
                var listener = authority.GetComponents<UdonSharpBehaviour>().FirstOrDefault(b => b.GetType() == listenerType);
                if (listener == null) listener = (UdonSharpBehaviour)UdonSharpUndo.AddComponent(authority.gameObject, listenerType);
                Undo.RecordObject(listener, "Connect sequence listener");
                listenerType.GetField("adapter").SetValue(listener, adapter);
                listenerField.SetValue(adapter, listener);
                Save(listener);
                if (UdonSharpEditorUtility.GetBackingUdonBehaviour(listener)?.programSource == null) throw new InvalidOperationException("Sequence listener program is missing.");
            }
            var linkedViews = new List<PlaylistView>();
            foreach (var other in UnityEngine.Object.FindObjectsOfType<PlaylistSetup>(true))
            {
                if (!CanConnect(other) || other.view == null || other.gameObject.scene != setup.gameObject.scene || Kind(other.player) != kind || Authority(other.player, kind) != authority) continue;
                Undo.RecordObject(other.view, "Share sequence controller");
                other.view.adapter = adapter;
                linkedViews.Add(other.view);
                Save(other.view);
            }
            adapter.views = linkedViews.ToArray();
            if (UdonSharpEditorUtility.GetBackingUdonBehaviour(adapter)?.programSource == null) throw new InvalidOperationException("Sequence adapter program is missing.");
            Save(adapter); Save(setup.view); Save(setup);
            PlaylistAppearance.EnsureConnectionLabel(setup);
            return true;
        }
        static Component Authority(Component player, int kind)
        {
            if (player == null) return null;
            if (kind >= 0 && kind < ConnectorRegistry.All.Length) return ConnectorRegistry.All[kind].Authority(player);
            return player;
        }
        public static bool AutoConnect(PlaylistSetup setup)
        {
            // A surviving explicit reference is never replaced automatically.
            if (!CanConnect(setup) || setup.player != null) return false;
            var candidates = Find(setup);
            return candidates.Count == 1 && Connect(setup, candidates[0]);
        }
        public static void RefreshAudioLinkStatus(PlaylistSetup setup)
        {
            if (setup == null || setup.view == null) return;
            var view = setup.view;
            var type = Resolve("AudioLink.AudioLink");
            Undo.RecordObject(view, "Refresh AudioLink status references");
            view.audioLinkInstalled = type != null;
            view.audioLinks = type == null ? new VRC.Udon.UdonBehaviour[0] : Resources.FindObjectsOfTypeAll(type).OfType<UdonSharpBehaviour>()
                .Where(b => !EditorUtility.IsPersistent(b) && b.gameObject.scene == setup.gameObject.scene)
                .Select(UdonSharpEditorUtility.GetBackingUdonBehaviour).Where(b => b != null).ToArray();
            int kind = Kind(setup.player);
            var sources = kind < 0 ? new AudioSource[0] : ConnectorRegistry.All[kind].AudioSources?.Invoke(setup.player) ?? new AudioSource[0];
            view.connectedAudioSources = sources.Where(s => s != null).Distinct().ToArray();
            Save(view);
            if (view.audioLinkLabel != null) {
                Undo.RecordObject(view.audioLinkLabel, "Refresh AudioLink status label");
                view.audioLinkLabel.text = AudioLinkStatus(setup); Save(view.audioLinkLabel);
            }
        }
        public static string AudioLinkStatus(PlaylistSetup setup)
        {
            var view = setup.view;
            if (view == null) return "UI未設定";
            var type = Resolve("AudioLink.AudioLink");
            if (type == null) return "AudioLink: 未導入";
            var links = Resources.FindObjectsOfTypeAll(type).OfType<Component>().Where(c => !EditorUtility.IsPersistent(c) && c.gameObject.scene == setup.gameObject.scene).ToArray();
            if (links.Length == 0) return "AudioLink: 未配置";
            if (setup.player == null || view.adapter == null) return "AudioLink: プレイヤー未接続";
            if (view.connectedAudioSources.Length == 0) return "AudioLink: 音源未設定";
            foreach (var link in links) {
                if (!link.gameObject.activeInHierarchy || (link is Behaviour behaviour && !behaviour.enabled)) continue;
                var left = type.GetField("audioSource")?.GetValue(link) as AudioSource;
                var right = type.GetField("optionalRightAudioSource")?.GetValue(link) as AudioSource;
                if (view.connectedAudioSources.Any(s => s != null && (s == left || s == right))) return "AudioLink: 接続中";
            }
            return "AudioLink: 未接続";
        }
        public static void Save(UnityEngine.Object obj)
        {
            if (obj is UdonSharpBehaviour udon) UdonSharpEditorUtility.CopyProxyToUdon(udon);
            EditorUtility.SetDirty(obj);
            PrefabUtility.RecordPrefabInstancePropertyModifications(obj);
            if (obj is Component c && c.gameObject.scene.IsValid()) EditorSceneManager.MarkSceneDirty(c.gameObject.scene);
        }
    }

    [CustomEditor(typeof(PlaylistSetup))]
    public class PlaylistSetupEditor : UnityEditor.Editor
    {
        bool advanced;
        string editError;
        bool editorActive;
        bool readOnlyCacheDirty = true;
        PlaylistData cacheData;
        Component cachePlayer, cacheTitleSource;
        PlaylistView cacheView;
        bool cacheUserList;
        int loadedUsers;
        List<Component> playerCandidates = new List<Component>();
        string[] playerLabels = new string[0], connectorWarnings = new string[0];
        bool[] connectorAvailable = new bool[0];
        string connectionProblem, audioLinkStatus, connectedPlayerLabel;
        void OnEnable()
        {
            editorActive = true;
            EditorApplication.hierarchyChanged += InvalidateReadOnlyCache;
            EditorApplication.projectChanged += InvalidateReadOnlyCache;
            Undo.undoRedoPerformed += InvalidateReadOnlyCache;
            Undo.postprocessModifications += OnPropertiesModified;
            EditorApplication.delayCall += () =>
            {
                if (!editorActive || target == null || EditorApplication.isPlayingOrWillChangePlaymode) return;
                PlaylistConnections.AutoConnect((PlaylistSetup)target);
                InvalidateReadOnlyCache();
            };
        }
        void OnDisable()
        {
            editorActive = false;
            EditorApplication.hierarchyChanged -= InvalidateReadOnlyCache;
            EditorApplication.projectChanged -= InvalidateReadOnlyCache;
            Undo.undoRedoPerformed -= InvalidateReadOnlyCache;
            Undo.postprocessModifications -= OnPropertiesModified;
        }
        void InvalidateReadOnlyCache() { readOnlyCacheDirty = true; Repaint(); }
        UndoPropertyModification[] OnPropertiesModified(UndoPropertyModification[] changes)
        {
            foreach (var change in changes)
            {
                var value = change.currentValue;
                if (value == null) continue;
                string field = value.propertyPath.Split('.')[0];
                var obj = value.target;
                bool setupChanged = obj is PlaylistSetup && (field == "data" || field == "player" || field == "view" || field == "playerKind" || field == "enableUserList" || field == "titleMetadataSource");
                bool viewChanged = obj is PlaylistView && (field == "adapter" || field == "audioLinks" || field == "audioLinkInstalled" || field == "connectedAudioSources");
                bool authorityChanged = obj == cachePlayer && (field == "core" || field == "_controller");
                bool audioChanged = field == "audioSource" || field == "optionalRightAudioSource" || field == "m_Enabled" || field == "m_IsActive";
                if (setupChanged || viewChanged || obj is PlaylistAdapter || authorityChanged || audioChanged)
                { InvalidateReadOnlyCache(); break; }
            }
            return changes;
        }
        void EnsureReadOnlyCache(PlaylistSetup setup)
        {
            if (cacheData != setup.data || cachePlayer != setup.player || cacheView != setup.view || cacheTitleSource != setup.titleMetadataSource || cacheUserList != setup.enableUserList)
                readOnlyCacheDirty = true;
            if (!readOnlyCacheDirty || (Event.current != null && Event.current.type != EventType.Layout)) return;
            loadedUsers = PlaylistAssets.LoadedUsers(setup.data);
            playerCandidates = PlaylistConnections.Find(setup);
            playerLabels = playerCandidates.Select(PlaylistConnections.Label).ToArray();
            connectorAvailable = new bool[playerCandidates.Count]; connectorWarnings = new string[playerCandidates.Count];
            for (int i = 0; i < playerCandidates.Count; i++)
            {
                int kind = PlaylistConnections.Kind(playerCandidates[i]);
                connectorAvailable[i] = kind >= 0 && PlaylistConnections.Resolve(PlaylistConnections.AdapterTypes[kind]) != null;
                connectorWarnings[i] = kind < 0 ? "対応するConnectorを導入してください。" : PlaylistConnections.Kinds[kind] + " Connectorを導入してください。";
            }
            connectionProblem = PlaylistConnections.Validate(setup);
            audioLinkStatus = PlaylistConnections.AudioLinkStatus(setup);
            connectedPlayerLabel = PlaylistConnections.Label(setup.player);
            cacheData = setup.data; cachePlayer = setup.player; cacheView = setup.view; cacheTitleSource = setup.titleMetadataSource; cacheUserList = setup.enableUserList;
            readOnlyCacheDirty = false;
        }
        public override void OnInspectorGUI()
        {
            var setup = (PlaylistSetup)target;
            EnsureReadOnlyCache(setup);
            EditorGUI.BeginChangeCheck();
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            {
                serializedObject.UpdateIfRequiredOrScript();
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(serializedObject.FindProperty("data"), new GUIContent("プレイリスト"));
                bool dataChanged = EditorGUI.EndChangeCheck();
                serializedObject.ApplyModifiedProperties();
                if (dataChanged && setup.view != null) TryBake(setup);
                if (GUILayout.Button("新規プレイリストを作成")) CreatePlaylist(setup);
                int users = loadedUsers;
                if (PlaylistAssets.IsDefault(setup.data))
                    EditorGUILayout.HelpBox("DefaultPlaylistは空の配布用テンプレートです。「新規プレイリストを作成」で別データを用意できます。", MessageType.Info);
                else if (users > 1)
                    EditorGUILayout.HelpBox(users + " 個のUIがこのデータを共有しています。編集はすべてに反映されます。別々に使う場合は「新規プレイリストを作成」で別データを割り当ててください。", MessageType.Warning);
                using (new EditorGUI.DisabledScope(setup.data == null))
                    if (GUILayout.Button("プレイリストを編集する", GUILayout.Height(30)))
                        PlaylistEditWindow.Open(setup.data);
                EditorGUILayout.HelpBox("編集ウィンドウの「保存」でUIへ自動反映します。", MessageType.Info);
                FeatureExtensionRegistry.DrawButtons("Playlist", setup.data);
                if (!string.IsNullOrEmpty(editError)) EditorGUILayout.HelpBox(editError, MessageType.Error);

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("見た目", EditorStyles.boldLabel);
                if (setup.GetComponentInParent<PlaylistTabletTouch>() == null && PlaylistConnections.CanConnect(setup)
                    && GUILayout.Button("現在のUIから手持ちタブレット版を作成"))
                    Selection.activeGameObject = PlaylistTabletBuilder.Create(setup);
                serializedObject.UpdateIfRequiredOrScript();
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(serializedObject.FindProperty("theme"),new GUIContent("テーマ"));
                bool themeChanged=EditorGUI.EndChangeCheck();
                serializedObject.ApplyModifiedProperties();
                if(themeChanged)PlaylistAppearance.ApplyTheme(setup);
                if(setup.theme!=null&&GUILayout.Button("テーマを編集"))Selection.activeObject=setup.theme;
                serializedObject.UpdateIfRequiredOrScript();
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(serializedObject.FindProperty("showTabIcons"), new GUIContent("タブのアイコンを表示"));
                bool iconsChanged = EditorGUI.EndChangeCheck();
                serializedObject.ApplyModifiedProperties();
                if (iconsChanged) { PlaylistTabIcons.Apply(setup); PlaylistConnections.Save(setup); }
                EditorGUILayout.HelpBox("Theme Assetで色と表示方式を選べます。Wireframeは背景透過・枠線のみのテーマです。サイズはルートのScaleで調整してください。",MessageType.Info);
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("動画プレイヤーへの接続", EditorStyles.boldLabel);
                serializedObject.UpdateIfRequiredOrScript();
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(serializedObject.FindProperty("player"), new GUIContent("接続先"));
                bool playerChanged = EditorGUI.EndChangeCheck();
                serializedObject.ApplyModifiedProperties();
                if (playerChanged)
                {
                    // Clear the previous destination even when the new selection is invalid.
                    if (setup.view != null)
                    {
                        Undo.RecordObject(setup.view, "Change playlist connection");
                        setup.view.adapter = null;
                        PlaylistConnections.Save(setup.view);
                    }
                    PlaylistConnections.Connect(setup, setup.player);
                    PlaylistAppearance.EnsureConnectionLabel(setup);
                }
                if (!PlaylistConnections.CanConnect(setup))
                    EditorGUILayout.HelpBox("Prefabをシーンに配置して接続してください。", MessageType.Info);
                else
                {
                    var candidates = playerCandidates;
                    if (setup.player == null && candidates.Count == 1)
                        if (GUILayout.Button("見つかったプレイヤーに自動接続")) { PlaylistConnections.AutoConnect(setup); InvalidateReadOnlyCache(); }
                    EditorGUILayout.LabelField("シーン内の接続候補", candidates.Count + " 件");
                    for (int i = 0; i < candidates.Count; i++)
                    {
                        var candidate = candidates[i]; if (candidate == null) continue;
                        using (new EditorGUI.DisabledScope(!connectorAvailable[i]))
                            if (GUILayout.Button(playerLabels[i])) { PlaylistConnections.Connect(setup, candidate); InvalidateReadOnlyCache(); }
                        if (!connectorAvailable[i]) EditorGUILayout.HelpBox(connectorWarnings[i], MessageType.Warning);
                    }
                    EditorGUILayout.LabelField("接続先を選択すると、自動で接続設定を更新します。", EditorStyles.wordWrappedMiniLabel);
                }
                string problem = connectionProblem;
                if (problem != null)
                {
                    EditorGUILayout.HelpBox(problem, MessageType.Warning);
                    if (setup.player != null && PlaylistConnections.CanConnect(setup))
                        if (GUILayout.Button(new GUIContent("接続設定を修復", "選択中のプレイヤーとの接続設定を作り直します。")))
                        { PlaylistConnections.Connect(setup, setup.player); InvalidateReadOnlyCache(); }
                }
                else EditorGUILayout.HelpBox("接続済み: " + connectedPlayerLabel, MessageType.Info);
                EditorGUILayout.LabelField("AudioLink", audioLinkStatus);
                if (GUILayout.Button("AudioLinkの接続状況を更新")) {
                    PlaylistAppearance.EnsureConnectionLabel(setup);
                    PlaylistConnections.RefreshAudioLinkStatus(setup); InvalidateReadOnlyCache();
                }
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("特殊リスト", EditorStyles.boldLabel);
                serializedObject.UpdateIfRequiredOrScript();
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(serializedObject.FindProperty("enableQueue"), new GUIContent("再生キューを使用"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("enableHistory"), new GUIContent("再生履歴を使用"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("enableUserList"), new GUIContent("ユーザーリストを使用"));
                bool specialChanged = EditorGUI.EndChangeCheck();
                serializedObject.ApplyModifiedProperties();
                if (specialChanged) TrySpecialLists(setup);
                FeatureExtensionRegistry.DrawInspector("Playlist", setup);
                EditorGUILayout.LabelField("チェックを変更すると、その場で反映します。", EditorStyles.wordWrappedMiniLabel);
                EditorGUILayout.HelpBox("ユーザーリストは自分用・同じワールド内で共通です（最大32件）。タイトル取得拡張なしでもURLで利用できます。保存用PlayerObjectは公開後に削除・再作成しないでください。", MessageType.Info);
                EditorGUILayout.Space();
                advanced = EditorGUILayout.Foldout(advanced, "詳細設定（UIデザイン・参照）", true);
                if (advanced)
                {
                    serializedObject.UpdateIfRequiredOrScript();
                    foreach (var name in new[] { "view", "tabContent", "videoContent", "tabTemplate", "videoTemplate" })
                        EditorGUILayout.PropertyField(serializedObject.FindProperty(name));
                    serializedObject.ApplyModifiedProperties();
                    if (GUILayout.Button("テンプレートからUIを再生成")) TryBake(setup);
                    EditorGUILayout.HelpBox("行デザインはTemplates内で調整し、再生成してください。Play/Build時にも自動反映します。", MessageType.Info);
                }
            }
            if (EditorGUI.EndChangeCheck()) InvalidateReadOnlyCache();
        }
        void CreatePlaylist(PlaylistSetup setup)
        {
            string path = EditorUtility.SaveFilePanelInProject("プレイリストを作成", "Playlist", "asset", "このUI専用のPlaylistDataの保存先を選択してください。");
            if (string.IsNullOrEmpty(path)) return;
            var data = PlaylistAssets.CreateFor(setup, path);
            if (data == null) return;
            TryBake(setup);
            PlaylistEditWindow.Open(data);
        }
        void TryBake(PlaylistSetup setup)
        {
            try { PlaylistBaker.Bake(setup); editError = null; }
            catch (Exception e) { editError = e.Message; }
            finally { InvalidateReadOnlyCache(); }
        }
        void TrySpecialLists(PlaylistSetup setup)
        {
            try { PlaylistSpecialLists.Apply(setup); editError = null; }
            catch (Exception e) { editError = e.Message; Debug.LogException(e, setup); }
            finally { InvalidateReadOnlyCache(); }
        }
    }
}
