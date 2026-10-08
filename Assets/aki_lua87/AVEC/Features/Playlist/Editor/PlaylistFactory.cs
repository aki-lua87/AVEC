using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;
using UdonSharp;
using UdonSharpEditor;
using UdonSharp.Compiler;

namespace aki_lua87.CommonVideoPlaylist.Editor
{
    public static class PlaylistFactory
    {
        public const string Root = aki_lua87.AVEC.Editor.PackageLayout.Playlist;
        public const string PrefabPath = aki_lua87.AVEC.Editor.PackageLayout.Prefabs + "/CommonVideoPlaylist.prefab";
        [MenuItem("Tools/AVEC/Playlist/1 Create Program Assets")]
        public static void Programs()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:AssemblyDefinitionAsset", new[] { Root }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string udonPath = Path.ChangeExtension(path, ".asset");
                if (AssetDatabase.LoadMainAssetAtPath(udonPath) != null) continue;
                var definition = ScriptableObject.CreateInstance<UdonSharpAssemblyDefinition>();
                definition.sourceAssembly = AssetDatabase.LoadAssetAtPath<UnityEditorInternal.AssemblyDefinitionAsset>(path);
                AssetDatabase.CreateAsset(definition, udonPath);
            }
            foreach (string guid in AssetDatabase.FindAssets("t:MonoScript", new[] { Root }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                var type = script.GetClass();
                if (type == null || type.IsAbstract || !typeof(UdonSharpBehaviour).IsAssignableFrom(type)) continue;
                if (UdonSharpEditorUtility.GetUdonSharpProgramAsset(type) != null) continue;
                var asset = ScriptableObject.CreateInstance<UdonSharpProgramAsset>();
                asset.sourceCsScript = script;
                asset.ScriptVersion = UdonSharpProgramVersion.CurrentVersion;
                AssetDatabase.CreateAsset(asset, Path.ChangeExtension(path, ".asset"));
            }
            AssetDatabase.SaveAssets();
            UdonSharpCompilerV1.CompileSync();
            Debug.Log("[CVP] Program assets generated and Udon compilation requested.");
        }
        [MenuItem("Tools/AVEC/Playlist/2 Create Sample Prefab")]
        public static void CreateSample()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null) { Debug.Log("[CVP] Existing prefab preserved."); return; }
            Directory.CreateDirectory(aki_lua87.AVEC.Editor.PackageLayout.Prefabs);
            Directory.CreateDirectory(Root + "/Samples/Playlists");
            AssetDatabase.Refresh();
            var data = AssetDatabase.LoadAssetAtPath<PlaylistData>(PlaylistAssets.DefaultPath);
            if (data == null) {
            data = ScriptableObject.CreateInstance<PlaylistData>();
            data.tabs = new PlaylistTab[0];
            AssetDatabase.CreateAsset(data, PlaylistAssets.DefaultPath);
            }
            var temporary = EditorSceneManager.NewPreviewScene();
            var go = CreateUI(data, temporary);
            PrefabUtility.SaveAsPrefabAsset(go, PrefabPath);
            // UdonSharp finishes component setup in delayCall; dispose after that callback.
            EditorApplication.delayCall += () => EditorSceneManager.ClosePreviewScene(temporary);
            AssetDatabase.SaveAssets();
            Debug.Log("[CVP] Created sample data and world-space UI prefab.");
        }
        public static GameObject CreateUI(PlaylistData data, UnityEngine.SceneManagement.Scene scene = default)
        {
            var root = new GameObject("Common Video Playlist", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            if (scene.IsValid()) UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
            root.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            root.GetComponent<Canvas>().scaleFactor = 3;
            var rect = root.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(1200, 760);
            root.transform.localScale = Vector3.one * .0016f;
            root.AddComponent<VRC.SDK3.Components.VRCUiShape>();
            var collider = root.AddComponent<BoxCollider>(); collider.size = new Vector3(1200,760,1);
            var setup = root.AddComponent<PlaylistSetup>(); setup.data = data;
            setup.theme = PlaylistAppearance.DefaultTheme();
            var view = root.AddUdonSharpComponent<PlaylistView>(); setup.view = view;
            var bg = Box("Background", root.transform, new Color(.035f,.055f,.085f)); Stretch(bg);
            var accent = Box("Accent", root.transform, new Color(.25f,.86f,.76f)); Place(accent, 32,28,52,6);
            var title = Label("Library title", root.transform, "VIDEO LIBRARY", 34); Place(title.rectTransform,32,48,750,55);
            var subtitle = Label("Subtitle", root.transform, "One collection. Your player.", 22); Place(subtitle.rectTransform,34,105,900,36);
            subtitle.color = new Color(.57f,.66f,.76f);
            var divider = Box("Divider",root.transform,new Color(.15f,.2f,.28f)); Place(divider,32,159,1136,2);
            view.heading = Label("Tab title",root.transform,"Featured",28); Place(view.heading.rectTransform,344,181,810,46);
            Label("Collections label",root.transform,"COLLECTIONS",20).rectTransform.SetParent(root.transform,false);
            Place(root.transform.Find("Collections label").GetComponent<RectTransform>(),32,190,278,36);
            setup.tabContent = Scroll(root.transform,"Tabs",32,242,278,398,out _);
            setup.videoContent = Scroll(root.transform,"Videos",344,242,824,398,out var scroll);
            view.videoScroll = scroll;
            view.status = Label("Status",root.transform,"Not connected / 接続先を設定してください",22); Place(view.status.rectTransform,32,668,1136,45);
            var hint = Label("Hint",root.transform,"Selection highlight = request only  /  選択表示は再生状態ではありません",18); Place(hint.rectTransform,32,716,1136,30); hint.color = new Color(.57f,.66f,.76f);
            var templates = new GameObject("Templates",typeof(RectTransform)); templates.transform.SetParent(root.transform,false);
            setup.tabTemplate = Template(templates.transform,"Tab template",62,24);
            setup.videoTemplate = Template(templates.transform,"Video template",76,26);
            templates.SetActive(false);
            PlaylistAppearance.ApplyCompact(setup);
            PlaylistBaker.Bake(setup);
            return root;
        }
        static GameObject Template(Transform parent,string name,float height,int fontSize)
        {
            var rect = Box(name,parent,new Color(.12f,.16f,.22f)); rect.sizeDelta = new Vector2(280,height);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = rect.GetComponent<Image>();
            var colors = button.colors; colors.highlightedColor = new Color(.8f,1,1); colors.pressedColor = new Color(.55f,.85f,.85f); button.colors = colors;
            var layout = rect.gameObject.AddComponent<LayoutElement>(); layout.minHeight = height; layout.preferredHeight = height;
            var text = Label("Label",rect,"Item",fontSize); Stretch(text.rectTransform); text.rectTransform.offsetMin = new Vector2(18,4); text.rectTransform.offsetMax = new Vector2(-18,-4);
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            return rect.gameObject;
        }
        public static void EnsurePlaybackModeButton(PlaylistSetup setup)
        {
            PlaylistAppearance.EnsureControls(setup);
        }
        static RectTransform Scroll(Transform parent,string name,float x,float y,float w,float h,out ScrollRect scroll)
        {
            var frame = Box(name,parent,new Color(.055f,.08f,.12f)); Place(frame,x,y,w,h);
            scroll = frame.gameObject.AddComponent<ScrollRect>(); scroll.horizontal=false; scroll.vertical=true; scroll.scrollSensitivity=40; scroll.movementType=ScrollRect.MovementType.Clamped;
            var viewport=Box("Viewport",frame,Color.white); Stretch(viewport); viewport.offsetMax=new Vector2(-30,0); viewport.GetComponent<Image>().enabled=false; viewport.gameObject.AddComponent<RectMask2D>();
            var content=new GameObject("Content",typeof(RectTransform),typeof(VerticalLayoutGroup),typeof(ContentSizeFitter)).GetComponent<RectTransform>(); content.SetParent(viewport,false);
            content.anchorMin=new Vector2(0,1); content.anchorMax=Vector2.one; content.pivot=new Vector2(.5f,1); content.sizeDelta=Vector2.zero;
            var layout=content.GetComponent<VerticalLayoutGroup>(); layout.spacing=10; layout.padding=new RectOffset(0,0,0,8); layout.childControlHeight=true; layout.childForceExpandHeight=false; layout.childControlWidth=true;
            content.GetComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport=viewport; scroll.content=content;
            var rail=Box("Scrollbar",frame,new Color(.08f,.11f,.16f)); rail.anchorMin=new Vector2(1,0); rail.anchorMax=Vector2.one; rail.pivot=Vector2.one; rail.offsetMin=new Vector2(-24,0); rail.offsetMax=Vector2.zero;
            var handle=Box("Handle",rail,new Color(.35f,.52f,.6f)); Stretch(handle);
            var bar=rail.gameObject.AddComponent<Scrollbar>(); bar.handleRect=handle; bar.targetGraphic=handle.GetComponent<Image>(); bar.direction=Scrollbar.Direction.BottomToTop;
            scroll.verticalScrollbar=bar;
            return content;
        }
        static RectTransform Box(string name,Transform parent,Color color)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Image)); go.transform.SetParent(parent,false); go.GetComponent<Image>().color=color; return go.GetComponent<RectTransform>();
        }
        static Text Label(string name,Transform parent,string value,int size)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Text)); go.transform.SetParent(parent,false); var text=go.GetComponent<Text>();
            text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); text.fontSize=size; text.text=value; text.color=new Color(.92f,.96f,1); text.alignment=TextAnchor.MiddleLeft; text.raycastTarget=false; return text;
        }
        static void Place(RectTransform rect,float x,float y,float w,float h) { rect.anchorMin=rect.anchorMax=new Vector2(0,1); rect.pivot=new Vector2(0,1); rect.anchoredPosition=new Vector2(x,-y); rect.sizeDelta=new Vector2(w,h); }
        static void Stretch(RectTransform rect) { rect.anchorMin=Vector2.zero; rect.anchorMax=Vector2.one; rect.offsetMin=rect.offsetMax=Vector2.zero; }

        [MenuItem("Tools/AVEC/Playlist/3 Create Validation Scene")]
        public static void Scene()
        {
            string path=Root+"/Samples/Scenes/PlaylistValidation.unity";
            Directory.CreateDirectory(Root + "/Samples/Scenes");
            if (File.Exists(path)) { Debug.Log("[CVP] Existing validation scene preserved."); return; }
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null) throw new InvalidOperationException("Create the sample prefab first.");
            // Additive scene protects even unsaved changes in the user's existing scene.
            var scene=EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects,NewSceneMode.Additive);
            UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
            var camera=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Camera>()).First(); camera.transform.position=new Vector3(0,1.5f,-2); camera.transform.rotation=Quaternion.identity;
            var floor=GameObject.CreatePrimitive(PrimitiveType.Plane); floor.name="Floor"; floor.layer=11; floor.transform.localScale=Vector3.one*2;
            var world=new GameObject("VRCWorld"); world.AddComponent<VRC.SDK3.Components.VRCSceneDescriptor>();
            var spawn=new GameObject("Spawn"); spawn.transform.position=new Vector3(0,0,-1);
            world.GetComponent<VRC.SDK3.Components.VRCSceneDescriptor>().spawns=new[] {spawn.transform};
            var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath),scene); go.transform.position=new Vector3(0,1.5f,0);
            new GameObject("EventSystem",typeof(UnityEngine.EventSystems.EventSystem),typeof(UnityEngine.EventSystems.StandaloneInputModule));
            EditorSceneManager.SaveScene(scene,path);
            Debug.Log("[CVP] Created isolated validation scene.");
        }
        [MenuItem("Tools/AVEC/Playlist/4 Export Packages")]
        public static void OpenReleaseBuilder()
        {
            var type=PlaylistConnections.Resolve("aki_lua87.CommonAssets.Editor.CommonAssetsReleaseBuilder");
            if(type!=null)type.GetMethod("ShowWindow").Invoke(null,null);else Export();
        }
        public static void Export()
        {
            if (!EditorApplication.ExecuteMenuItem("Tools/Common Assets/Release Builder")) Debug.LogWarning("Release Builderを導入してください。配布出力はRelease Builderで行います。");
        }


    }
}
