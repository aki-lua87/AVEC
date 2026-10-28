using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UdonSharp;
using UdonSharpEditor;
using VRC.SDK3.Components;
using VRC.SDK3.Video.Components.AVPro;

namespace aki_lua87.AVEC.SimpleScreen.Editor
{
    // Creates NEW objects only. Never bakes or rebuilds an existing player/UI/scene.
    public static class SimpleScreenBuilder
    {
        public const string Root = "Assets/aki_lua87/AVEC/Features/SimpleScreen";
        public const string PrefabPath = "Assets/aki_lua87/AVEC/Prefabs/AVEC-SimpleScreen.prefab";
        static readonly Color Ink = new Color(.88f,.93f,1f,1f);
        static readonly Color Accent = new Color(.25f,.72f,.95f,1f);

        // Explicit initial authoring operation; refuses to overwrite an authored prefab.
        public static string CreateDistributionPrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
                throw new InvalidOperationException("Existing SimpleScreen prefab is preserved. Edit it directly instead of regenerating it.");
            Directory.CreateDirectory(Root + "/Materials");
            Directory.CreateDirectory(Root + "/Icons");
            AssetDatabase.Refresh();
            var assemblyPath = Root + "/Runtime/AVEC.SimpleScreen.Runtime.asset";
            var assembly = AssetDatabase.LoadAssetAtPath<UdonSharpAssemblyDefinition>(assemblyPath);
            if (assembly == null)
            {
                assembly = ScriptableObject.CreateInstance<UdonSharpAssemblyDefinition>();
                assembly.sourceAssembly = AssetDatabase.LoadAssetAtPath<UnityEditorInternal.AssemblyDefinitionAsset>(Root + "/Runtime/AVEC.SimpleScreen.Runtime.asmdef");
                AssetDatabase.CreateAsset(assembly, assemblyPath);
            }
            foreach (var name in new[]{"SimpleScreenPlayer", "SimpleScreenAdapter", "SimpleScreenRemoteTarget", "SimpleScreenUI"})
            {
                string path = Root + "/Runtime/" + name + ".asset";
                if (AssetDatabase.LoadAssetAtPath<UdonSharpProgramAsset>(path) != null) continue;
                var program = ScriptableObject.CreateInstance<UdonSharpProgramAsset>();
                program.sourceCsScript = AssetDatabase.LoadAssetAtPath<MonoScript>(Root + "/Runtime/" + name + ".cs");
                AssetDatabase.CreateAsset(program, path);
            }
            UdonSharpProgramAsset.CompileAllCsPrograms(true);
            if (UdonSharpProgramAsset.AnyUdonSharpScriptHasError()) throw new InvalidOperationException("UdonSharp compilation failed.");
            // Keep only this feature's compiled programs inside its distribution folder.
            string programFolder = Root + "/Runtime/Programs";
            if (!AssetDatabase.IsValidFolder(programFolder)) AssetDatabase.CreateFolder(Root + "/Runtime", "Programs");
            foreach (string guid in AssetDatabase.FindAssets("t:UdonSharpProgramAsset", new[]{Root + "/Runtime"}))
            {
                var program = AssetDatabase.LoadAssetAtPath<UdonSharpProgramAsset>(AssetDatabase.GUIDToAssetPath(guid));
                string source = AssetDatabase.GetAssetPath(program.SerializedProgramAsset);
                string destination = programFolder + "/" + Path.GetFileName(source);
                if (source == destination) continue;
                string error = AssetDatabase.MoveAsset(source, destination);
                if (!string.IsNullOrEmpty(error)) throw new IOException(error);
            }
            var sound = SpeakerSprite(false); var muted = SpeakerSprite(true);
            var material = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/Screen.mat");
            if (material == null)
            {
                var shader = Shader.Find("Video/RealtimeEmissiveGamma");
                if (shader == null) throw new InvalidOperationException("VRChat SDK video shader is missing.");
                material = new Material(shader) { name = "AVEC SimpleScreen" };
                material.SetTexture("_MainTex", BlackTexture());
                material.SetFloat("_Emission",1f);
                AssetDatabase.CreateAsset(material, Root + "/Materials/Screen.mat");
            }
            var scene = EditorSceneManager.NewPreviewScene();
            GameObject go = null;
            try
            {
                go = new GameObject("AVEC-SimpleScreen");
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, scene);
                var video = go.AddComponent<VRCAVProVideoPlayer>();
                var videoData = new SerializedObject(video);
                videoData.FindProperty("autoPlay").boolValue = false;
                videoData.FindProperty("maximumResolution").intValue = 1080;
                videoData.ApplyModifiedPropertiesWithoutUndo();
                video.Loop = false;
                video.EnableAutomaticResync = false;
                var player = go.AddUdonSharpComponent<SimpleScreenPlayer>();
                var adapter = go.AddUdonSharpComponent<SimpleScreenAdapter>();
                player.videoPlayer = video; player.adapter = adapter; adapter.player = player;

                var screen = GameObject.CreatePrimitive(PrimitiveType.Quad);
                screen.name = "Monitor"; screen.transform.SetParent(go.transform,false);
                screen.transform.localScale = new Vector3(3.2f,1.8f,1f);
                UnityEngine.Object.DestroyImmediate(screen.GetComponent<Collider>());
                var renderer = screen.GetComponent<MeshRenderer>(); renderer.sharedMaterial = material;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; renderer.receiveShadows = false;
                player.screen = renderer;
                var output = screen.AddComponent<VRCAVProVideoScreen>();
                var outputData = new SerializedObject(output);
                outputData.FindProperty("videoPlayer").objectReferenceValue = video;
                outputData.FindProperty("textureProperty").stringValue = "_MainTex";
                outputData.FindProperty("useSharedMaterial").boolValue = false;
                outputData.ApplyModifiedPropertiesWithoutUndo();

                var speaker = new GameObject("Speaker"); speaker.transform.SetParent(go.transform,false);
                var audio = speaker.AddComponent<AudioSource>(); audio.playOnAwake = false;
                audio.volume = player.initialVolume; audio.spatialBlend = 1f;
                audio.rolloffMode = AudioRolloffMode.Linear; audio.minDistance = 1f; audio.maxDistance = 25f;
                var spatial = speaker.AddComponent<VRCSpatialAudioSource>();
                spatial.Gain = 0f; spatial.Near = 0f; spatial.Far = 25f;
                spatial.EnableSpatialization = true; spatial.UseAudioSourceVolumeCurve = true;
                var avSpeaker = speaker.AddComponent<VRCAVProVideoSpeaker>();
                var speakerData = new SerializedObject(avSpeaker);
                speakerData.FindProperty("videoPlayer").objectReferenceValue = video;
                speakerData.ApplyModifiedPropertiesWithoutUndo();
                player.speakers = new[]{audio};

                var canvas = Rect("Screen UI", go.transform, 0,0,1280,720);
                canvas.localScale = Vector3.one * .0025f;
                canvas.localPosition = new Vector3(0,0,-.015f);
                canvas.gameObject.AddComponent<Canvas>().renderMode = RenderMode.WorldSpace;
                canvas.gameObject.AddComponent<GraphicRaycaster>();
                canvas.gameObject.AddComponent<VRC.SDK3.Components.VRCUiShape>();
                // The transparent monitor-sized hit surface remains active while the panel is hidden.
                var hit = canvas.gameObject.AddComponent<Image>(); hit.color = Color.clear;
                var ui = canvas.gameObject.AddUdonSharpComponent<SimpleScreenUI>();
                ui.player = player; player.controls = ui; ui.soundSprite = sound; ui.mutedSprite = muted;
                Events(canvas.gameObject,ui,EventTriggerType.PointerEnter,nameof(SimpleScreenUI._PointerEnter));
                Events(canvas.gameObject,ui,EventTriggerType.PointerExit,nameof(SimpleScreenUI._PointerExit));
                var panel = Rect("Controls",canvas,0,-244,1248,200);
                panel.gameObject.AddComponent<Image>().color = new Color(.025f,.04f,.065f,.94f);
                ui.panel = panel.gameObject.AddComponent<CanvasGroup>();
                ui.timeLabel = Label("Time",panel,405,65,390,28,"--:-- / --:--",19,TextAnchor.MiddleRight);
                ui.statusLabel = Label("Status",panel,-200,65,740,28,"",18,TextAnchor.MiddleLeft);
                ui.seek = Slider("Progress",panel,0,25,1192,30,0f);
                Event(ui.seek.onValueChanged,ui,nameof(SimpleScreenUI._SeekChanged));
                Events(ui.seek.gameObject,ui,EventTriggerType.PointerDown,nameof(SimpleScreenUI._BeginSeek));
                Events(ui.seek.gameObject,ui,EventTriggerType.PointerUp,nameof(SimpleScreenUI._EndSeek));
                Events(ui.seek.gameObject,ui,EventTriggerType.EndDrag,nameof(SimpleScreenUI._EndSeek));
                var mute = Button("Mute",panel,-570,-45,48,44,"",ui,nameof(SimpleScreenUI._ToggleMute));
                ui.muteIcon = Rect("Speaker icon",mute.transform,0,0,28,28).gameObject.AddComponent<Image>();
                ui.muteIcon.sprite = sound; ui.muteIcon.color = Ink; ui.muteIcon.raycastTarget = false;
                ui.volume = Slider("Volume",panel,-410,-45,230,32,player.initialVolume);
                Event(ui.volume.onValueChanged,ui,nameof(SimpleScreenUI._VolumeChanged));
                Events(ui.volume.gameObject,ui,EventTriggerType.PointerDown,nameof(SimpleScreenUI._BeginDrag));
                Events(ui.volume.gameObject,ui,EventTriggerType.PointerUp,nameof(SimpleScreenUI._EndDrag));
                Events(ui.volume.gameObject,ui,EventTriggerType.EndDrag,nameof(SimpleScreenUI._EndDrag));
                ui.volumeLabel = Label("Volume value",panel,-245,-45,85,40,"50%",19,TextAnchor.MiddleCenter);

                var field = Rect("URL input",panel,155,-45,680,44);
                field.gameObject.AddComponent<Image>().color = new Color(.09f,.13f,.19f,1f);
                ui.urlInput = field.gameObject.AddComponent<VRCUrlInputField>();
                ui.urlInput.textComponent = Label("Text",field,0,0,650,40,"",18,TextAnchor.MiddleLeft);
                var placeholder = Label("Placeholder",field,0,0,650,40,"Video URL…",18,TextAnchor.MiddleLeft);
                placeholder.color = new Color(.5f,.6f,.7f,1f); ui.urlInput.placeholder = placeholder;
                ui.urlInput.characterLimit = 2048; ui.urlInput.lineType = VRCUrlInputField.LineType.SingleLine;
                ui.urlInput.navigation = new Navigation {mode = Navigation.Mode.None};
                ui.urlInput.AllowSendingOnEndEdit = true;
                Event(ui.urlInput.onEndEdit,ui,nameof(SimpleScreenUI._SubmitUrl));
                Events(field.gameObject,ui,EventTriggerType.Select,nameof(SimpleScreenUI._BeginInput));
                Button("Load URL",panel,553,-45,86,44,"LOAD",ui,nameof(SimpleScreenUI._SubmitUrl));

                foreach (var behaviour in go.GetComponentsInChildren<UdonSharpBehaviour>(true))
                { UdonSharpEditorUtility.CopyProxyToUdon(behaviour); EditorUtility.SetDirty(behaviour); }
                PrefabUtility.SaveAsPrefabAsset(go,PrefabPath);
                AssetDatabase.SaveAssets();
                return PrefabPath;
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }
        static RectTransform Rect(string name,Transform parent,float x,float y,float w,float h)
        {
            var rect = new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent,false); rect.anchorMin = rect.anchorMax = new Vector2(.5f,.5f);
            rect.pivot = new Vector2(.5f,.5f); rect.anchoredPosition = new Vector2(x,y); rect.sizeDelta = new Vector2(w,h);
            return rect;
        }
        static Text Label(string name,Transform parent,float x,float y,float w,float h,string value,int size,TextAnchor alignment)
        {
            var text = Rect(name,parent,x,y,w,h).gameObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); text.fontSize = size;
            text.color = Ink; text.text = value; text.alignment = alignment; text.raycastTarget = false;
            text.supportRichText = false; text.horizontalOverflow = HorizontalWrapMode.Overflow;
            return text;
        }
        static Button Button(string name,Transform parent,float x,float y,float w,float h,string caption,SimpleScreenUI ui,string method)
        {
            var rect = Rect(name,parent,x,y,w,h); var image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(.10f,.23f,.32f,1f);
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            button.navigation = new Navigation {mode = Navigation.Mode.None};
            Label("Caption",rect,0,0,w,h,caption,18,TextAnchor.MiddleCenter);
            UnityEventTools.AddStringPersistentListener(button.onClick,UdonSharpEditorUtility.GetBackingUdonBehaviour(ui).SendCustomEvent,method);
            return button;
        }
        static Slider Slider(string name,Transform parent,float x,float y,float w,float h,float value)
        {
            var rect = Rect(name,parent,x,y,w,h);
            // Full-height hit area around a slim visible track.
            rect.gameObject.AddComponent<Image>().color = Color.clear;
            var slider = rect.gameObject.AddComponent<Slider>(); slider.navigation = new Navigation {mode=Navigation.Mode.None};
            var background = Rect("Track",rect,0,0,w-16,6); background.gameObject.AddComponent<Image>().color = new Color(.19f,.26f,.33f,1f);
            var fillArea = Rect("Fill area",rect,0,0,w-16,6);
            var fill = Rect("Fill",fillArea,0,0,0,0); fill.anchorMin = Vector2.zero; fill.anchorMax = Vector2.one;
            fill.gameObject.AddComponent<Image>().color = Accent;
            var handleArea = Rect("Handle area",rect,0,0,w-16,18);
            var handle = Rect("Handle",handleArea,0,0,14,0); var image=handle.gameObject.AddComponent<Image>(); image.color=Ink;
            slider.fillRect=fill; slider.handleRect=handle; slider.targetGraphic=image; slider.minValue=0; slider.maxValue=1;
            slider.SetValueWithoutNotify(value);
            return slider;
        }
        static void Event<T>(UnityEngine.Events.UnityEvent<T> evt,SimpleScreenUI ui,string method)
        { UnityEventTools.AddStringPersistentListener(evt,UdonSharpEditorUtility.GetBackingUdonBehaviour(ui).SendCustomEvent,method); }
        static void Events(GameObject go,SimpleScreenUI ui,EventTriggerType type,string method)
        {
            var trigger=go.GetComponent<EventTrigger>() ?? go.AddComponent<EventTrigger>();
            var entry=new EventTrigger.Entry {eventID=type};
            UnityEventTools.AddStringPersistentListener(entry.callback,UdonSharpEditorUtility.GetBackingUdonBehaviour(ui).SendCustomEvent,method);
            trigger.triggers.Add(entry);
        }
        static Sprite SpeakerSprite(bool muted)
        {
            string path=Root+"/Icons/"+(muted?"SpeakerMuted":"Speaker")+".png";
            if (!File.Exists(path))
            {
                var texture=new Texture2D(32,32,TextureFormat.RGBA32,false);
                for(int y=0;y<32;y++) for(int x=0;x<32;x++)
                {
                    bool body=x>=4&&x<=10&&y>=11&&y<=20;
                    bool cone=x>=10&&x<=18&&Mathf.Abs(y-15.5f)<=4+(x-10)*.65f;
                    float r=Mathf.Sqrt((x-16)*(x-16)+(y-15.5f)*(y-15.5f));
                    bool waves=x>20&&Mathf.Abs(y-15.5f)<10&&((r>9&&r<11)||(r>14&&r<16));
                    bool cross=x>=21&&x<=29&&(Mathf.Abs(y-x+8)<2||Mathf.Abs(y+x-39)<2);
                    texture.SetPixel(x,y,body||cone||(muted?cross:waves)?Color.white:Color.clear);
                }
                texture.Apply(); File.WriteAllBytes(path,texture.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(path);
                var importer=(TextureImporter)AssetImporter.GetAtPath(path); importer.textureType=TextureImporterType.Sprite;
                importer.spritePixelsPerUnit=32; importer.alphaIsTransparency=true; importer.mipmapEnabled=false; importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        public static Texture2D BlackTexture()
        {
            string path=Root+"/Materials/IdleBlack.asset";
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if(texture!=null)return texture;
            texture=new Texture2D(2,2,TextureFormat.RGB24,false) {name="SimpleScreen idle black"};
            texture.SetPixels(new[]{Color.black,Color.black,Color.black,Color.black});texture.Apply();
            AssetDatabase.CreateAsset(texture,path);return texture;
        }
    }
}
