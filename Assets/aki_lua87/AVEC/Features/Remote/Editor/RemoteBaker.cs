using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VRC.SDKBase.Editor.BuildPipeline;

namespace aki_lua87.CommonGestureRemote.Editor
{
    public static class RemoteBaker
    {
        public static string Normalize(string value) => (value ?? "").ToUpperInvariant().Replace("→", "R").Replace("←", "L").Replace("↑", "U").Replace("↓", "D").Replace(" ", "").Replace("　", "").Trim();
        public static string ValidateData(GestureData data)
        {
            if (data == null || data.bindings == null || data.bindings.Length == 0) return "ジェスチャーデータを設定してください。";
            var used = new HashSet<string>();
            for (int i = 0; i < data.bindings.Length; i++)
            {
                var b = data.bindings[i];
                string path = b == null ? "" : Normalize(b.gesture);
                if (path.Length == 0 || path.Length > 8 || path.Any(c => !"RLUD".Contains(c))) return (i + 1) + "行目: 上下左右を1～8方向で入力してください。";
                for (int j = 1; j < path.Length; j++) if (path[j] == path[j - 1]) return (i + 1) + "行目: 同方向の連続指定は1つにまとめてください。";
                if (!used.Add(path)) return (i + 1) + "行目: ジェスチャーが重複しています。";
                if (string.IsNullOrWhiteSpace(b.label) || !Enum.IsDefined(typeof(RemoteAction), b.action)) return (i + 1) + "行目: 表示名と操作を設定してください。";
                if (b.action == RemoteAction.CustomEvent && (string.IsNullOrWhiteSpace(b.customEvent) || b.customEvent.Any(char.IsWhiteSpace))) return (i + 1) + "行目: カスタムイベント名が不正です。";
            }
            return null;
        }
        public static void Bake(RemoteSetup setup)
        {
            if (setup.remote == null) throw new InvalidOperationException("GestureRemote 参照がありません。");
            string error = ValidateData(setup.data);
            if (error != null) throw new InvalidOperationException(error);
            Undo.RecordObject(setup.remote, "Bake remote gestures");
            var bindings = setup.data.bindings;
            setup.remote.gestures = bindings.Select(b => Normalize(b.gesture)).ToArray();
            setup.remote.labels = bindings.Select(b => b.label).ToArray();
            setup.remote.actions = bindings.Select(b => (int)b.action).ToArray();
            setup.remote.customEvents = bindings.Select(b => b.customEvent ?? "").ToArray();
            RemoteConnections.Save(setup.remote);
        }
        public static void BakeLoaded()
        {
            foreach (var setup in UnityEngine.Object.FindObjectsOfType<RemoteSetup>(true))
            {
                if (!RemoteConnections.InScene(setup)) continue;
                RemoteConnections.AutoConnect(setup);
                if (setup.player != null) RemoteConnections.Connect(setup, setup.player);
                else if (setup.remote != null) { setup.remote.playerTarget = null; setup.remote.playlistTarget = null; }
                Bake(setup);
            }
        }
    }
    [InitializeOnLoad]
    public static class RemotePlayBake
    {
        static RemotePlayBake() { EditorApplication.playModeStateChanged += OnPlay; }
        static void OnPlay(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingEditMode) return;
            try { RemoteBaker.BakeLoaded(); }
            catch (Exception e) { Debug.LogException(e); EditorApplication.isPlaying = false; }
        }
    }
    public class RemoteBuildBake : IVRCSDKBuildRequestedCallback
    {
        public int callbackOrder => -90; // After CommonVideoPlaylist's -100 bake.
        public bool OnBuildRequested(VRCSDKRequestedBuildType type)
        {
            if (type != VRCSDKRequestedBuildType.Scene) return true;
            try { RemoteBaker.BakeLoaded(); return true; }
            catch (Exception e) { Debug.LogException(e); return false; }
        }
    }
}
