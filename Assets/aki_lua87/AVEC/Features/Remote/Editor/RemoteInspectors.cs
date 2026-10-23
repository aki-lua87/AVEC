using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace aki_lua87.CommonGestureRemote.Editor
{
    [CustomPropertyDrawer(typeof(GestureBinding))]
    public class GestureBindingDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) => EditorGUIUtility.singleLineHeight * 4 + 12;
        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(rect, label, property);
            float h = EditorGUIUtility.singleLineHeight;
            var line = new Rect(rect.x, rect.y, rect.width, h);
            var gesture = property.FindPropertyRelative("gesture");
            string path = RemoteBaker.Normalize(gesture.stringValue);
            string[] names = { "なし", "→", "←", "↑", "↓" };
            string[] directions = { "", "R", "L", "U", "D" };
            EditorGUI.PrefixLabel(line, new GUIContent("方向（最大3）"));
            float start = rect.x + EditorGUIUtility.labelWidth;
            float width = (rect.width - EditorGUIUtility.labelWidth) / 3;
            string updated = "";
            EditorGUI.BeginChangeCheck();
            for (int i = 0; i < 3; i++) {
                string value = i < path.Length ? path.Substring(i, 1) : "";
                int selected = System.Array.IndexOf(directions, value);
                int next = EditorGUI.Popup(new Rect(start + i * width, line.y, width - 3, h), Mathf.Max(0, selected), names);
                updated += directions[next];
            }
            if (EditorGUI.EndChangeCheck()) gesture.stringValue = updated;
            line.y += h + 3; EditorGUI.PropertyField(line, property.FindPropertyRelative("label"), new GUIContent("表示名"));
            line.y += h + 3; EditorGUI.PropertyField(line, property.FindPropertyRelative("action"), new GUIContent("機能"));
            line.y += h + 3;
            using (new EditorGUI.DisabledScope(property.FindPropertyRelative("action").intValue != 15))
                EditorGUI.PropertyField(line, property.FindPropertyRelative("customEvent"), new GUIContent("イベント名"));
            EditorGUI.EndProperty();
        }
    }
    [CustomEditor(typeof(GestureData))]
    public class GestureDataEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox("上下左右を最大3ステップで登録します。同方向の連続は1ステップになります。トリガーを離すと実行します。\n操作の同期範囲は接続先プレイヤーの設定に従います。", MessageType.Info);
            EditorGUI.BeginChangeCheck();
            DrawDefaultInspector();
            if (EditorGUI.EndChangeCheck()) {
                foreach (var setup in UnityEngine.Object.FindObjectsOfType<RemoteSetup>(true))
                    if (setup.data == target && RemoteBaker.ValidateData(setup.data) == null && !EditorApplication.isPlayingOrWillChangePlaymode) RemoteBaker.Bake(setup);
                if (!EditorApplication.isPlayingOrWillChangePlaymode) GestureGuideBuilder.RefreshLoaded((GestureData)target);
            }
            string error = RemoteBaker.ValidateData((GestureData)target);
            if (error != null) EditorGUILayout.HelpBox(error, MessageType.Error);
        }
    }
    [CustomEditor(typeof(RemoteSetup))]
    public class RemoteSetupEditor : UnityEditor.Editor
    {
        string error;
        bool details;
        bool editorActive;
        bool readOnlyCacheDirty = true;
        Component cachePlayer;
        List<Component> playerCandidates = new List<Component>();
        List<Component> playlistCandidates = new List<Component>();
        string[] playerLabels = new string[0], playlistLabels = new string[0];
        SerializedObject remoteObject, targetObject;

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
                try { var setup = (RemoteSetup)target; RemoteConnections.AutoConnect(setup); RemoteConnections.AssignDefaultSummonObject(setup); }
                catch (Exception e) { error = e.Message; }
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
            remoteObject?.Dispose(); remoteObject = null;
            targetObject?.Dispose(); targetObject = null;
        }
        void InvalidateReadOnlyCache() { readOnlyCacheDirty = true; Repaint(); }
        UndoPropertyModification[] OnPropertiesModified(UndoPropertyModification[] changes)
        {
            foreach (var change in changes)
            {
                var value = change.currentValue;
                if (value == null) continue;
                var component = value.target as Component;
                if (component == null) continue;
                string field = value.propertyPath;
                // Moving objects or adjusting sensitivity/volume cannot change connection candidates.
                if ((component is RemoteSetup && (field == "player" || field == "playlist" || field == "remote")) ||
                    field == "adapter" || field == "player" ||
                    (component.GetType().FullName == "aki_lua87.CommonVideoPlaylist.PlaylistSetup" && field == "view"))
                { InvalidateReadOnlyCache(); break; }
            }
            return changes;
        }
        void EnsureReadOnlyCache(RemoteSetup setup)
        {
            if (cachePlayer != setup.player) readOnlyCacheDirty = true;
            if (!readOnlyCacheDirty || (Event.current != null && Event.current.type != EventType.Layout)) return;
            // Scene-wide discovery belongs to selection/structural changes, never each repaint.
            playerCandidates = RemoteConnections.Find(setup);
            playlistCandidates = RemoteConnections.FindPlaylists(setup);
            playerLabels = playerCandidates.Select(RemoteConnections.Label).ToArray();
            playlistLabels = playlistCandidates.Select(RemoteConnections.Label).ToArray();
            cachePlayer = setup.player;
            readOnlyCacheDirty = false;
        }
        static SerializedObject ReuseSerializedObject(ref SerializedObject cached, UnityEngine.Object value)
        {
            if (cached == null || cached.targetObject != value)
            { cached?.Dispose(); cached = new SerializedObject(value); }
            cached.UpdateIfRequiredOrScript();
            return cached;
        }
        public override void OnInspectorGUI()
        {
            var setup = (RemoteSetup)target;
            EnsureReadOnlyCache(setup);
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            {
                serializedObject.UpdateIfRequiredOrScript();
                EditorGUILayout.LabelField("AVEC Remote", EditorStyles.boldLabel);
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(serializedObject.FindProperty("data"), new GUIContent("ジェスチャー設定"));
                bool dataChanged = EditorGUI.EndChangeCheck();
                serializedObject.ApplyModifiedProperties();
                if (dataChanged) Attempt(() => RemoteBaker.Bake(setup));
                if (setup.data != null && GUILayout.Button("ジェスチャー設定を編集")) Selection.activeObject = setup.data;
                if (!RemoteConnections.InScene(setup))
                    EditorGUILayout.HelpBox("Prefabをシーンへ配置すると、接続先を選択できます。", MessageType.Info);
                else
                {
                    serializedObject.UpdateIfRequiredOrScript();
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("player"), new GUIContent("接続先プレイヤー"));
                    bool changed = EditorGUI.EndChangeCheck();
                    serializedObject.ApplyModifiedProperties();
                    if (changed) Attempt(() => RemoteConnections.Connect(setup, setup.player));
                    var candidates = playerCandidates;
                    if (candidates.Count == 0) EditorGUILayout.HelpBox("対応プレイヤーが見つかりません。使用するプレイヤーとConnectorを導入してください。", MessageType.Info);
                    for (int i = 0; i < candidates.Count; i++)
                    {
                        var candidate = candidates[i];
                        if (candidate != null && GUILayout.Button((candidate == setup.player ? "● " : "○ ") + playerLabels[i]))
                            Attempt(() => RemoteConnections.Connect(setup, candidate));
                    }
                    serializedObject.UpdateIfRequiredOrScript();
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("playlist"), new GUIContent("連携する PlaylistView"));
                    bool playlistChanged = EditorGUI.EndChangeCheck();
                    serializedObject.ApplyModifiedProperties();
                    if (playlistChanged) Attempt(() => RemoteConnections.ConnectPlaylist(setup));
                    for (int i = 0; i < playlistCandidates.Count; i++)
                    {
                        var view = playlistCandidates[i];
                        if (view != null && GUILayout.Button((view == setup.playlist ? "● " : "○ ") + playlistLabels[i]))
                            Attempt(() => { Undo.RecordObject(setup, "Select remote playlist"); setup.playlist = view; RemoteConnections.ConnectPlaylist(setup); });
                    }
                    serializedObject.UpdateIfRequiredOrScript(); EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("nativePlaylist"), new GUIContent("ネイティブのプレイリスト"));
                    bool nativeChanged = EditorGUI.EndChangeCheck(); serializedObject.ApplyModifiedProperties();
                    if (nativeChanged) Attempt(() => RemoteConnections.ConfigureNativePlaylist(setup));
                    if (setup.playlist == null) EditorGUILayout.HelpBox("次／前の動画はネイティブのプレイリストを使用します。操作先がないジェスチャーは無効になり、ガイドにも表示されません。", MessageType.Info);
                }
                serializedObject.UpdateIfRequiredOrScript();
                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(serializedObject.FindProperty("modelStyle"), new GUIContent("リモコンのモデル"));
                if (serializedObject.FindProperty("modelStyle").enumValueIndex == (int)RemoteModelStyle.Custom)
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("customModelPrefab"), new GUIContent("自作モデル Prefab"));
                serializedObject.ApplyModifiedProperties();
                if (GUILayout.Button("選択したモデルを反映")) Attempt(() => RemoteModels.Apply(setup));
                EditorGUILayout.HelpBox("Standard / Slim / 自作Prefabを選択できます。モデルの位置・回転・大きさは Model 内で調整します。Tip と本体の BoxCollider は独立しています。", MessageType.Info);
                if (setup.remote != null)
                {
                    var remote = ReuseSerializedObject(ref remoteObject, setup.remote);
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(remote.FindProperty("tabletToSummon"), new GUIContent("呼び出すオブジェクト", "接続先のPlaylistタブレットを自動設定します。別のオブジェクトのTransformにも変更できます。"));
                    EditorGUILayout.PropertyField(remote.FindProperty("customEventTarget"), new GUIContent("カスタムイベントの接続先"));
                    EditorGUILayout.PropertyField(remote.FindProperty("summonEnabled"), new GUIContent("呼び出しを初期状態で有効にする", "このリモコンを右スティック下の長押しで呼び出します。ジェスチャーで自分のON/OFFを切り替えられます。"));
                    EditorGUILayout.PropertyField(remote.FindProperty("summonArea"), new GUIContent("呼び出し可能範囲", "未設定ならどこからでも呼び出せます。BoxColliderのCenter／Sizeで範囲を指定します。"));
                    details = EditorGUILayout.Foldout(details, "詳細設定（感度・表示・接続）", true);
                    if (details)
                        foreach (string field in new[] { "minimumStroke", "directionBias", "timeout", "volumeStep", "scaleWithAvatar", "tip", "trail", "label", "drawingColor", "matchedColor" })
                            EditorGUILayout.PropertyField(remote.FindProperty(field));
                    if (details)
                        foreach (string field in new[] { "summonHoldSeconds", "helpIdleSeconds", "helpOffset", "helpUIScale", "helpPanel", "helpCommands", "helpHint", "helpStick", "helpHintGroup" })
                            EditorGUILayout.PropertyField(remote.FindProperty(field));
                    bool remoteChanged = EditorGUI.EndChangeCheck();
                    remote.ApplyModifiedProperties();
                    if (remoteChanged) RemoteConnections.Save(setup.remote);
                    EditorGUILayout.HelpBox("範囲なし：どこからでも呼び出せます。範囲あり：その中にいる時だけ呼び出せます。重なった範囲では複数のリモコンが対象になります。", MessageType.Info);
                    string areaError = RemoteSummonAreas.Validate(setup.remote);
                    if (areaError != null) EditorGUILayout.HelpBox(areaError, MessageType.Warning);
                    if (RemoteConnections.InScene(setup) && setup.remote.summonArea == null && GUILayout.Button("呼び出し範囲を作成・接続"))
                        Attempt(() => RemoteSummonAreas.CreateAndAssign(setup));
                    if (setup.remote.summonArea != null && GUILayout.Button("呼び出し範囲を選択")) Selection.activeGameObject = setup.remote.summonArea.gameObject;
                    if (details && RemoteConnections.InScene(setup))
                    {
                        EditorGUILayout.HelpBox("通常の設定変更は自動反映されます。接続参照が壊れた場合だけ、下の修復を使用してください。", MessageType.Info);
                        if (GUILayout.Button(new GUIContent("接続設定を修復", "接続先への中継参照を修復し、ジェスチャー設定とガイドを再反映します。")))
                            Attempt(() => { RemoteConnections.Connect(setup, setup.player); RemoteBaker.Bake(setup); });
                    }
                    var playerTarget = setup.remote.playerTarget;
                    if (playerTarget != null)
                    {
                        if (details)
                        {
                            var t = ReuseSerializedObject(ref targetObject, playerTarget);
                            EditorGUI.BeginChangeCheck();
                            foreach (string field in new[] { "audioSources", "volumeSliders", "muteModules" })
                                if (t.FindProperty(field) != null) EditorGUILayout.PropertyField(t.FindProperty(field), true);
                            bool targetChanged = EditorGUI.EndChangeCheck(); t.ApplyModifiedProperties();
                            if (targetChanged) RemoteConnections.Save(playerTarget);
                            if (GUILayout.Button("接続先から画面・音声を再取得")) Attempt(() =>
                            {
                                Undo.RecordObject(playerTarget, "Detect remote outputs");
                                RemoteConnections.DetectOutputs(playerTarget, setup.player, RemoteConnections.Kind(setup.player));
                                RemoteConnections.Save(playerTarget);
                            });
                        }
                    }
                }
                if (!string.IsNullOrEmpty(error)) EditorGUILayout.HelpBox(error, MessageType.Error);
                EditorGUILayout.HelpBox("持つ → トリガーを引いて描く → 候補名を確認して離す。\nVR: グリップを離すと手放します。デスクトップ: 持ったまま左クリック＋視点移動。", MessageType.Info);
            }
        }
        void Attempt(Action action) { try { action(); error = null; } catch (Exception e) { error = e.Message; } finally { InvalidateReadOnlyCache(); } }
    }
}
