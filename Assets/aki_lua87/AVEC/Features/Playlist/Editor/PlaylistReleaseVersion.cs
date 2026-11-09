using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace aki_lua87.CommonVideoPlaylist.Editor
{
    // Serialized UI text: no runtime dependency on the release builder or EditorPrefs.
    public static class PlaylistReleaseVersion
    {
        public const string LabelPath = "Playback mode/Release version";
        const string Template = "Assets/aki_lua87/AVEC/Prefabs/AVEC-VideoPlaylist.prefab";

        public static string CurrentText
        {
            get
            {
                var root = AssetDatabase.LoadAssetAtPath<GameObject>(Template);
                var label = root == null ? null : root.transform.Find(LabelPath)?.GetComponent<Text>();
                return label == null ? "unreleased" : label.text;
            }
        }

        public static bool Ensure(PlaylistView view, string text)
        {
            var mode = view.transform.Find("Playback mode") as RectTransform;
            if (mode == null) return false;
            var label = mode.Find("Release version")?.GetComponent<Text>();
            bool created = label == null;
            if (created)
            {
                var go = new GameObject("Release version", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
                Undo.RegisterCreatedObjectUndo(go, "Add AVEC release version");
                go.layer = mode.gameObject.layer;
                go.transform.SetParent(mode, false);
                label = go.GetComponent<Text>();
                var source = view.transform.Find("Connected player")?.GetComponent<Text>();
                if (source == null) source = view.GetComponentsInChildren<Text>(true).FirstOrDefault(t => t != label && t.font != null);
                label.font = source == null ? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") : source.font;
                label.color = source == null ? Color.white : source.color;
                label.fontSize = 12;
                label.alignment = TextAnchor.MiddleRight;
                label.supportRichText = false;
                label.raycastTarget = false;
                label.resizeTextForBestFit = true;
                label.resizeTextMinSize = 8;
                label.resizeTextMaxSize = 12;
                var rect = label.rectTransform;
                rect.anchorMin = rect.anchorMax = Vector2.one;
                rect.pivot = new Vector2(1, 0);
                rect.anchoredPosition = new Vector2(0, 2);
                rect.sizeDelta = new Vector2(180, 16);
            }
            if (!created && label.text == text) return false;
            Undo.RecordObject(label, "Stamp AVEC release version");
            label.text = text;
            EditorUtility.SetDirty(label);
            if (PrefabUtility.IsPartOfPrefabInstance(label)) PrefabUtility.RecordPrefabInstancePropertyModifications(label);
            return true;
        }

        public static int UpdateLoadedDevelopmentScene(string version)
        {
            int changed = 0;
            foreach (var view in Resources.FindObjectsOfTypeAll<PlaylistView>())
            {
                var scene = view.gameObject.scene;
                if (!scene.IsValid() || !scene.isLoaded || scene.path != "Assets/aki_lua87/Demo/Scenes/CommonAssetsDemo.unity") continue;
                if (!Ensure(view, "v" + version)) continue;
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
                changed++;
            }
            return changed;
        }

        // Stamp only owned distribution templates. On export failure restore the previous labels.
        public sealed class ExportStamp : IDisposable
        {
            readonly Dictionary<string, string[]> previous = new Dictionary<string, string[]>();
            bool committed;
            public ExportStamp(IEnumerable<string> paths, string version)
            {
                try
                {
                    foreach (string path in paths.Where(p => p.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase)).Distinct())
                    {
                        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                        if (asset == null || asset.GetComponentsInChildren<PlaylistView>(true).Length == 0) continue;
                        var root = PrefabUtility.LoadPrefabContents(path);
                        try
                        {
                            var views = root.GetComponentsInChildren<PlaylistView>(true);
                            previous[path] = views.Select(v => v.transform.Find(LabelPath)?.GetComponent<Text>()?.text).ToArray();
                            bool changed = false;
                            foreach (var view in views) changed |= Ensure(view, "v" + version);
                            if (changed) PrefabUtility.SaveAsPrefabAsset(root, path);
                        }
                        finally { PrefabUtility.UnloadPrefabContents(root); }
                    }
                }
                catch { Dispose(); throw; }
            }
            public void Commit() { committed = true; }
            public void Dispose()
            {
                if (committed) return;
                foreach (var entry in previous)
                {
                    var root = PrefabUtility.LoadPrefabContents(entry.Key);
                    try
                    {
                        var views = root.GetComponentsInChildren<PlaylistView>(true);
                        for (int i = 0; i < views.Length; i++)
                        {
                            var label = views[i].transform.Find(LabelPath);
                            if (entry.Value[i] == null) { if (label != null) UnityEngine.Object.DestroyImmediate(label.gameObject); }
                            else Ensure(views[i], entry.Value[i]);
                        }
                        PrefabUtility.SaveAsPrefabAsset(root, entry.Key);
                    }
                    finally { PrefabUtility.UnloadPrefabContents(root); }
                }
            }
        }
    }
}
