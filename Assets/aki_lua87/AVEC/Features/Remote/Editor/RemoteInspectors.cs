using System;
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
            EditorGUILayout.HelpBox("上下左右を最大3ステップで登録します。同方向の連続は1ステップになります。トリガーを離すと実行します。\nミュート・音量は自分のみ、再生・リピートは接続先の同期範囲に反映します。", MessageType.Info);
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
        void OnEnable()
        {
            EditorApplication.delayCall += () =>
            {
                if (target == null || EditorApplication.isPlayingOrWillChangePlaymode) return;
                try { RemoteConnections.AutoConnect((RemoteSetup)target); }
                catch (Exception e) { error = e.Message; }
            };
        }
        public override void OnInspectorGUI()
        {
            var setup = (RemoteSetup)target;
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            {
                serializedObject.Update();
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
                    serializedObject.Update();
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("player"), new GUIContent("接続先プレイヤー"));
                    bool changed = EditorGUI.EndChangeCheck();
                    serializedObject.ApplyModifiedProperties();
                    if (changed) Attempt(() => RemoteConnections.Connect(setup, setup.player));
                    var candidates = RemoteConnections.Find(setup);
                    if (candidates.Count == 0) EditorGUILayout.HelpBox("対応プレイヤーが見つかりません。使用するプレイヤーとConnectorを導入してください。", MessageType.Info);
                    foreach (var candidate in candidates)
                        if (GUILayout.Button((candidate == setup.player ? "● " : "○ ") + RemoteConnections.Label(candidate)))
                            Attempt(() => RemoteConnections.Connect(setup, candidate));
                    serializedObject.Update();
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("playlist"), new GUIContent("連携する PlaylistView"));
                    bool playlistChanged = EditorGUI.EndChangeCheck();
                    serializedObject.ApplyModifiedProperties();
                    if (playlistChanged) Attempt(() => RemoteConnections.ConnectPlaylist(setup));
                    foreach (var view in RemoteConnections.FindPlaylists(setup))
                        if (GUILayout.Button((view == setup.playlist ? "● " : "○ ") + RemoteConnections.Label(view)))
                            Attempt(() => { Undo.RecordObject(setup, "Select remote playlist"); setup.playlist = view; RemoteConnections.ConnectPlaylist(setup); });
                    if (setup.playlist == null) EditorGUILayout.HelpBox("次／前の動画・タブ・再生順操作には、同じプレイヤーに接続したAVEC PlaylistとPlaylistRemote Extensionが必要です。", MessageType.Info);
                    if (GUILayout.Button("接続を修復・設定を反映")) Attempt(() => { RemoteConnections.Connect(setup, setup.player); RemoteBaker.Bake(setup); });
                }
                if (setup.remote != null)
                {
                    var remote = new SerializedObject(setup.remote);
                    remote.Update();
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(remote.FindProperty("customEventTarget"), new GUIContent("カスタムイベントの接続先"));
                    EditorGUILayout.PropertyField(remote.FindProperty("summonEnabled"), new GUIContent("召喚を有効にする", "このリモコンを右スティック下の長押しで召喚します。複数配置している場合、使うものだけONにしてください。"));
                    details = EditorGUILayout.Foldout(details, "感度・表示・画面設定", true);
                    if (details)
                        foreach (string field in new[] { "minimumStroke", "directionBias", "timeout", "volumeStep", "scaleWithAvatar", "tip", "trail", "label", "drawingColor", "matchedColor" })
                            EditorGUILayout.PropertyField(remote.FindProperty(field));
                    if (details)
                        foreach (string field in new[] { "helpHoldSeconds", "summonHoldSeconds", "helpIdleSeconds", "helpOffset", "helpUIScale", "helpPanel", "helpCommands", "helpHint", "helpStick", "helpHintGroup" })
                            EditorGUILayout.PropertyField(remote.FindProperty(field));
                    bool remoteChanged = EditorGUI.EndChangeCheck();
                    remote.ApplyModifiedProperties();
                    if (remoteChanged) RemoteConnections.Save(setup.remote);
                    var playerTarget = setup.remote.playerTarget;
                    if (playerTarget != null)
                    {
                        if (details)
                        {
                            var t = new SerializedObject(playerTarget);
                            t.Update(); EditorGUI.BeginChangeCheck();
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
        void Attempt(Action action) { try { action(); error = null; } catch (Exception e) { error = e.Message; } }
    }
}
