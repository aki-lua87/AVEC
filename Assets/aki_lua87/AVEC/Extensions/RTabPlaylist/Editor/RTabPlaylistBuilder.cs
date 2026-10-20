using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UdonSharp;
using UdonSharpEditor;
using Rasta.Tablet;
using aki_lua87.CommonVideoPlaylist;
using aki_lua87.CommonVideoPlaylist.Editor;

namespace aki_lua87.AVEC.Extensions.RTabPlaylist.Editor
{
    public static class RTabPlaylistBuilder
    {
        public const string Root = "Assets/aki_lua87/AVEC/Extensions/RTabPlaylist";
        public const string PrefabPath = Root + "/Prefabs/RTabPlaylist.prefab";

        // Explicit creation only: never rebuild an existing app or its source UI.
        public static GameObject Create(PlaylistSetup source, TabletManager tablet)
        {
            if (source == null || tablet == null) throw new ArgumentNullException();
            var apps = (Transform)new SerializedObject(tablet).FindProperty("installApp").objectReferenceValue;
            if (apps == null) throw new InvalidOperationException("RTAB Apps is missing.");
            if (apps.GetComponentInChildren<RTabPlaylistApp>(true) != null)
                throw new InvalidOperationException("Playlist app already exists; edit it in place.");
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
                throw new InvalidOperationException("Prefab already exists; use Install instead.");
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Create RTAB playlist app");
            var root = new GameObject("AVEC Playlist", typeof(RectTransform), typeof(CanvasGroup));
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, tablet.gameObject.scene);
            Undo.RegisterCreatedObjectUndo(root, "Create RTAB playlist app");
            root.transform.SetParent(apps, false);
            var rect = (RectTransform)root.transform;
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var app = root.AddUdonSharpComponent<RTabPlaylistApp>();
            app.appName = "Playlist";
            app.credit = "AVEC Playlist / aki_lua87\nRTab: https://rasta.booth.pm/items/7311462";
            app.appIcon = GetAppIcon();
            var screen = UnityEngine.Object.Instantiate(source.gameObject, root.transform, false);
            screen.name = "Playlist Screen";
            // A self-contained copy preserves the current authored data, style and UI bindings.
            if (PrefabUtility.IsPartOfPrefabInstance(screen))
                PrefabUtility.UnpackPrefabInstance(screen, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            foreach (var c in screen.GetComponents<Collider>()) UnityEngine.Object.DestroyImmediate(c);
            foreach (var c in screen.GetComponents<VRC.SDK3.Components.VRCUiShape>()) UnityEngine.Object.DestroyImmediate(c);
            foreach (var c in screen.GetComponents<GraphicRaycaster>()) UnityEngine.Object.DestroyImmediate(c);
            foreach (var c in screen.GetComponents<CanvasScaler>()) UnityEngine.Object.DestroyImmediate(c);
            foreach (var c in screen.GetComponents<Canvas>()) UnityEngine.Object.DestroyImmediate(c);
            var setup = screen.GetComponent<PlaylistSetup>();
            // A title resolver is an optional player integration, not an RTAB dependency.
            foreach (var resolver in screen.GetComponentsInChildren<PlaylistTitleResolver>(true))
            {
                var backing = UdonSharpEditorUtility.GetBackingUdonBehaviour(resolver);
                UnityEngine.Object.DestroyImmediate(resolver);
                if (backing != null) UnityEngine.Object.DestroyImmediate(backing);
            }
            setup.yttlManager = null;
            setup.player = null; setup.playerKind = "";
            setup.view.adapter = null; setup.view.userTitleResolver = null;
            setup.view.audioLinks = new VRC.Udon.UdonBehaviour[0];
            setup.view.connectedAudioSources = new AudioSource[0];
            setup.view.audioLinkInstalled = false;
            setup.view.connectionName = "";
            if (setup.view.connectionLabel != null) setup.view.connectionLabel.text = "YamaPlayer: 未接続";
            if (setup.view.audioLinkLabel != null) setup.view.audioLinkLabel.text = "AudioLink: 未接続";
            FitScreen(screen.GetComponent<RectTransform>());
            app.view = setup.view;
            foreach (var proxy in root.GetComponentsInChildren<UdonSharpBehaviour>(true))
                UdonSharpEditorUtility.CopyProxyToUdon(proxy);
            System.IO.Directory.CreateDirectory(Root + "/Prefabs");
            AssetDatabase.Refresh();
            PrefabUtility.SaveAsPrefabAssetAndConnect(root, PrefabPath, InteractionMode.AutomatedAction);
            ConnectToTabletPlayer(root.GetComponent<RTabPlaylistApp>(), tablet);
            EditorSceneManager.MarkSceneDirty(root.scene);
            Undo.CollapseUndoOperations(group);
            return root;
        }

        // Only the app's outer layout changes. Existing widgets, events, data and theme survive.
        // RTAB's app canvas is 2400 x 1800, matching the bundled apps.
        public static void FitScreen(RectTransform screen)
        {
            float width = screen.rect.width;
            float oldHeight = screen.rect.height;
            float scale = 2400f / width;
            float height = 1800f / scale;
            var status = screen.Find("Status") as RectTransform;
            float bottomGap = status == null ? 20f : oldHeight + status.offsetMin.y;
            // Keep the home navigation clear while the background fills the entire display.
            float extension = height - oldHeight - Mathf.Max(0f, 96f / scale - bottomGap);
            foreach (var name in new[] { "Status", "Play", "Stop", "Volume caption", "Volume rail", "Volume value" })
            {
                var control = screen.Find(name) as RectTransform;
                if (control == null) continue;
                Undo.RecordObject(control, "Fit RTAB playlist");
                control.anchoredPosition -= new Vector2(0, extension);
                EditorUtility.SetDirty(control);
            }
            foreach (var name in new[] { "Tabs", "Videos", "Queue panel", "History panel", "User list panel" })
            {
                var panel = screen.Find(name) as RectTransform;
                if (panel == null) continue;
                Undo.RecordObject(panel, "Fit RTAB playlist");
                panel.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, panel.rect.height + extension);
                EditorUtility.SetDirty(panel);
            }
            Undo.RecordObject(screen, "Fit RTAB playlist");
            screen.anchorMin = screen.anchorMax = screen.pivot = new Vector2(.5f, .5f);
            screen.sizeDelta = new Vector2(width, height);
            screen.localRotation = Quaternion.identity;
            screen.localScale = Vector3.one * scale;
            screen.anchoredPosition3D = Vector3.zero;
            EditorUtility.SetDirty(screen);
        }

        // A dedicated launcher tile; shared in-app transparent tab icons stay unchanged.
        public static Texture2D GetAppIcon()
        {
            const string path = Root + "/playlist-app.png";
            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (existing != null) return existing;
            var glyph = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            ImageConversion.LoadImage(glyph, System.IO.File.ReadAllBytes("Assets/aki_lua87/AVEC/Features/Playlist/Icons/tab-static.png"));
            const int size = 256;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            var background = new Color(.94f, .94f, .95f);
            var ink = new Color(.22f, .25f, .30f);
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Max(Mathf.Abs(x + .5f - 128f) - 84f, 0f);
                float dy = Mathf.Max(Mathf.Abs(y + .5f - 128f) - 84f, 0f);
                float alpha = Mathf.Clamp01(40f - Mathf.Sqrt(dx * dx + dy * dy));
                float u = (x + .5f - 32f) / 192f, v = (y + .5f - 32f) / 192f;
                float mask = u >= 0 && u <= 1 && v >= 0 && v <= 1 ? glyph.GetPixelBilinear(u, v).a : 0;
                var color = Color.Lerp(background, ink, mask);
                color.a = alpha; pixels[y * size + x] = color;
            }
            texture.SetPixels(pixels); texture.Apply();
            System.IO.File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(glyph); UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.mipmapEnabled = false; importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        public static void ConnectToTabletPlayer(RTabPlaylistApp app, TabletManager tablet)
        {
            var setup = app.GetComponentInChildren<PlaylistSetup>(true);
            var type = PlaylistConnections.Resolve("Yamadev.YamaStream.UI.UIController");
            if (type == null) throw new InvalidOperationException("YamaPlayer is not installed.");
            var players = tablet.transform.root.GetComponentsInChildren(type, true).OfType<Component>().ToArray();
            if (players.Length != 1) throw new InvalidOperationException("Select a YamaPlayer explicitly in PlaylistSetup.");
            if (!PlaylistConnections.Connect(setup, players[0]))
                throw new InvalidOperationException("YamaPlayer connection failed.");
            // Register this UI with the existing persistent PlayerObject without replacing it.
            PlaylistSpecialLists.Apply(setup);
        }

        [MenuItem("Tools/AVEC/Extensions/RTAB/Install Playlist on Selected RTAB")]
        public static void Install()
        {
            var selected = Selection.activeGameObject;
            var tablet = selected == null ? null : selected.transform.root.GetComponentInChildren<TabletManager>(true);
            if (tablet == null) throw new InvalidOperationException("Select an RTAB in the scene.");
            var apps = (Transform)new SerializedObject(tablet).FindProperty("installApp").objectReferenceValue;
            if (apps.GetComponentInChildren<RTabPlaylistApp>(true) != null)
                throw new InvalidOperationException("Playlist app already installed.");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null) throw new InvalidOperationException("RTabPlaylist.prefab is missing.");
            var root = (GameObject)PrefabUtility.InstantiatePrefab(prefab, apps);
            Undo.RegisterCreatedObjectUndo(root, "Install RTAB playlist");
            ConnectToTabletPlayer(root.GetComponent<RTabPlaylistApp>(), tablet);
            Selection.activeGameObject = root;
            EditorSceneManager.MarkSceneDirty(root.scene);
        }
    }
}
