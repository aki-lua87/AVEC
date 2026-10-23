using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UdonSharp;
using UdonSharp.Compiler;
using UdonSharpEditor;
using VRC.SDK3.Components;
using VRC.SDKBase;

namespace aki_lua87.CommonGestureRemote.Editor
{
    public static class RemoteFactory
    {
        public const string Root = aki_lua87.AVEC.Editor.PackageLayout.Remote;
        public const string PrefabPath = aki_lua87.AVEC.Editor.PackageLayout.Prefabs + "/CommonGestureRemote.prefab";
        public const string DataPath = Root + "/Samples/DefaultGestures.asset";

        [MenuItem("Tools/AVEC/Remote/Generate Assets")]
        public static void GenerateAssets()
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
                if (type == null || type.IsAbstract || !typeof(UdonSharpBehaviour).IsAssignableFrom(type) || UdonSharpEditorUtility.GetUdonSharpProgramAsset(type) != null) continue;
                var asset = ScriptableObject.CreateInstance<UdonSharpProgramAsset>();
                asset.sourceCsScript = script;
                asset.ScriptVersion = UdonSharpProgramVersion.CurrentVersion;
                AssetDatabase.CreateAsset(asset, Path.ChangeExtension(path, ".asset"));
            }
            foreach (string guid in AssetDatabase.FindAssets("t:UdonSharpProgramAsset", new[] { Root }))
                AssetDatabase.LoadAssetAtPath<UdonSharpProgramAsset>(AssetDatabase.GUIDToAssetPath(guid)).ScriptVersion = UdonSharpProgramVersion.CurrentVersion;
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            UdonSharpCompilerV1.CompileSync();
            Debug.Log("[CGR] Program assets and U# assembly definitions generated.");
        }

        [MenuItem("Tools/AVEC/Remote/Create Sample Prefab")]
        public static void CreatePrefab()
        {
            Directory.CreateDirectory(aki_lua87.AVEC.Editor.PackageLayout.Prefabs);
            Directory.CreateDirectory(Root + "/Samples");
            Directory.CreateDirectory(Root + "/Materials");
            AssetDatabase.Refresh();
            var data = AssetDatabase.LoadAssetAtPath<GestureData>(DataPath);
            if (data == null) { data = ScriptableObject.CreateInstance<GestureData>(); AssetDatabase.CreateAsset(data, DataPath); }
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null) { Debug.Log("[CGR] Existing prefab preserved."); return; }
            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var go = CreateRemote(data, scene);
                PrefabUtility.SaveAsPrefabAsset(go, PrefabPath);
            }
            finally { EditorApplication.delayCall += () => EditorSceneManager.ClosePreviewScene(scene); }
            AssetDatabase.SaveAssets();
            Debug.Log("[CGR] Pickup remote prefab and default gestures created.");
        }

        public static GameObject CreateRemote(GestureData data, UnityEngine.SceneManagement.Scene scene)
        {
            var root = new GameObject("Common Gesture Remote");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
            var rigidbody = root.AddComponent<Rigidbody>();
            rigidbody.mass = .15f;
            rigidbody.useGravity = true;
            rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            var collider = root.AddComponent<BoxCollider>();
            collider.size = new Vector3(.06f, .028f, .18f);
            var pickup = root.AddComponent<VRCPickup>();
            pickup.AutoHold = VRC_Pickup.AutoHoldMode.No;
            pickup.orientation = VRC_Pickup.PickupOrientation.Any;
            pickup.DisallowTheft = true;
            pickup.InteractionText = "Gesture Remote / 持つ";
            pickup.UseText = "トリガーを引いて描く → 離して実行";
            // Each client carries its own remote; playback is synchronized by the player.
            var grip = new GameObject("Grip").transform;
            grip.SetParent(root.transform, false);
            pickup.ExactGun = null;
            var remote = root.AddUdonSharpComponent<GestureRemote>();
            var setup = root.AddComponent<RemoteSetup>();
            setup.remote = remote; setup.data = data;
            var body = Material("Body", new Color(.04f, .06f, .09f), "Standard");
            var face = Material("Face", new Color(.1f, .16f, .23f), "Standard");
            var accent = Material("Accent", new Color(.12f, .85f, .67f), "Unlit/Color");
            Shape("Body", root.transform, Vector3.zero, new Vector3(.06f, .026f, .18f), body);
            Shape("Face", root.transform, new Vector3(0, .014f, .005f), new Vector3(.051f, .003f, .15f), face);
            Shape("Status light", root.transform, new Vector3(0, .016f, .065f), new Vector3(.032f, .001f, .006f), accent);
            for (int i = 0; i < 3; i++) Shape("Gesture mark " + i, root.transform, new Vector3(0, .017f, .015f - .022f * i), new Vector3(.02f, .003f, .008f), body);
            var tip = new GameObject("Tip").transform;
            tip.SetParent(root.transform, false); tip.localPosition = new Vector3(0, 0, .095f);
            remote.tip = tip;
            var line = new GameObject("Gesture Trail").AddComponent<LineRenderer>();
            line.transform.SetParent(root.transform, false);
            line.useWorldSpace = true; line.startWidth = .005f; line.endWidth = .003f;
            line.numCornerVertices = 3; line.numCapVertices = 3;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.sharedMaterial = Material("Trail", Color.white, "Sprites/Default");
            line.positionCount = 0; line.enabled = false; remote.trail = line;
            var canvas = new GameObject("Gesture Label", typeof(RectTransform), typeof(Canvas));
            canvas.transform.SetParent(root.transform, false);
            canvas.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            canvas.transform.localScale = Vector3.one * .0004f;
            var rect = canvas.GetComponent<RectTransform>(); rect.sizeDelta = new Vector2(1400, 240);
            var textGo = new GameObject("Function", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(canvas.transform, false);
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one; textRect.offsetMin = textRect.offsetMax = Vector2.zero;
            var text = textGo.GetComponent<Text>();
            text.font = AssetDatabase.LoadAssetAtPath<Font>(aki_lua87.AVEC.Editor.PackageLayout.Core + "/Fonts/ZenMaruGothic-Regular.ttf") ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 60; text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Overflow;
            text.supportRichText = false; text.raycastTarget = false; text.color = remote.drawingColor;
            text.text = "→↑  音量 +10%";
            remote.label = text;
            RemoteModels.Apply(setup);
            RemoteHelpBuilder.Ensure(setup);
            // Hide the whole label canvas at rest, while runtime toggles its text.
            textGo.SetActive(false);
            RemoteBaker.Bake(setup);
            RemoteConnections.CheckProgram(remote);
            return root;
        }
        private static Material Material(string name, Color color, string shader)
        {
            string path = Root + "/Materials/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            Directory.CreateDirectory(Root + "/Materials");
            material = new Material(Shader.Find(shader)); material.color = color;
            AssetDatabase.CreateAsset(material, path);
            return material;
        }
        private static void Shape(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name; go.transform.SetParent(parent, false);
            go.transform.localPosition = position; go.transform.localScale = scale;
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            go.GetComponent<Renderer>().sharedMaterial = material;
        }

        [MenuItem("Tools/AVEC/Remote/Export Packages")]
        public static void OpenReleaseBuilder()
        {
            var type=RemoteConnections.Resolve("aki_lua87.CommonAssets.Editor.CommonAssetsReleaseBuilder");
            if(type!=null)type.GetMethod("ShowWindow").Invoke(null,null);else Export();
        }
        public static void Export()
        {
            if (!EditorApplication.ExecuteMenuItem("Tools/Common Assets/Release Builder")) Debug.LogWarning("Release Builderを導入してください。配布出力はRelease Builderで行います。");
        }

    }
}

namespace aki_lua87.CommonGestureRemote.Editor
{
    // Authoring-only variants. Switching never touches runtime wiring or the pickup collider.
    public static class RemoteModels
    {
        public static void Apply(RemoteSetup setup)
        {
            if (setup.modelStyle == RemoteModelStyle.Custom)
            {
                if (setup.customModelPrefab == null || !UnityEditor.AssetDatabase.Contains(setup.customModelPrefab))
                    throw new System.InvalidOperationException("自作モデルのPrefabアセットを指定してください。");
                if (setup.customModelPrefab.GetComponentsInChildren<UnityEngine.Renderer>(true).Length == 0)
                    throw new System.InvalidOperationException("Rendererのあるモデルを指定してください。");
                if (setup.customModelPrefab.GetComponentsInChildren<UdonSharp.UdonSharpBehaviour>(true).Length > 0 ||
                    setup.customModelPrefab.GetComponentsInChildren<VRC.Udon.UdonBehaviour>(true).Length > 0)
                    throw new System.InvalidOperationException("自作モデルにはUdonスクリプトを含めず、見た目のPrefabを指定してください。");
            }
            UnityEditor.Undo.IncrementCurrentGroup();
            int group = UnityEditor.Undo.GetCurrentGroup();
            UnityEditor.Undo.SetCurrentGroupName("Select remote model");
            UnityEditor.Undo.RecordObject(setup, "Remote model references");
            if (setup.modelRoot == null)
            {
                setup.modelRoot = Child(setup.transform, "Model");
                var standard = Child(setup.modelRoot, "Standard");
                foreach (string name in new[] { "Body", "Face", "Status light", "Gesture mark 0", "Gesture mark 1", "Gesture mark 2" })
                {
                    var visual = setup.transform.Find(name);
                    if (visual != null && visual.GetComponent<UnityEngine.Renderer>() != null)
                        UnityEditor.Undo.SetTransformParent(visual, standard, "Keep original remote model");
                }
            }
            if (setup.modelStyle == RemoteModelStyle.Slim && setup.modelRoot.Find("Slim") == null)
            {
                var slim = Child(setup.modelRoot, "Slim");
                var source = setup.modelRoot.GetComponentInChildren<UnityEngine.Renderer>(true);
                var material = source == null ? null : source.sharedMaterial;
                Part(slim, "Slim body", UnityEngine.Vector3.zero, new UnityEngine.Vector3(.035f, .022f, .18f), material);
                Part(slim, "Slim face", new UnityEngine.Vector3(0, .012f, .025f), new UnityEngine.Vector3(.027f, .003f, .11f),
                    UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(RemoteFactory.Root + "/Materials/Face.mat") ?? material);
            }
            if (setup.modelStyle == RemoteModelStyle.Custom)
            {
                var current = setup.modelRoot.Find("Custom");
                if (current == null || UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(current.gameObject) != setup.customModelPrefab)
                {
                    if (current != null) UnityEditor.Undo.DestroyObjectImmediate(current.gameObject);
                    var instance = (UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(setup.customModelPrefab, setup.modelRoot);
                    UnityEditor.Undo.RegisterCreatedObjectUndo(instance, "Add custom remote model");
                    instance.name = "Custom";
                    instance.transform.localPosition = UnityEngine.Vector3.zero;
                    instance.transform.localRotation = UnityEngine.Quaternion.identity;
                    // Physics belongs to the remote root. Strip only the instantiated visual copy.
                    foreach (var pickup in instance.GetComponentsInChildren<VRC.SDK3.Components.VRCPickup>(true)) UnityEditor.Undo.DestroyObjectImmediate(pickup);
                    foreach (var sync in instance.GetComponentsInChildren<VRC.SDK3.Components.VRCObjectSync>(true)) UnityEditor.Undo.DestroyObjectImmediate(sync);
                    foreach (var collider in instance.GetComponentsInChildren<UnityEngine.Collider>(true)) UnityEditor.Undo.DestroyObjectImmediate(collider);
                    foreach (var body in instance.GetComponentsInChildren<UnityEngine.Rigidbody>(true)) UnityEditor.Undo.DestroyObjectImmediate(body);
                }
            }
            string selected = setup.modelStyle.ToString();
            foreach (UnityEngine.Transform child in setup.modelRoot)
            {
                UnityEditor.Undo.RecordObject(child.gameObject, "Show selected remote model");
                child.gameObject.SetActive(child.name == selected);
                UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(child.gameObject);
            }
            RemoteConnections.Save(setup);
            UnityEditor.Undo.CollapseUndoOperations(group);
        }
        static UnityEngine.Transform Child(UnityEngine.Transform parent, string name)
        {
            var go = new UnityEngine.GameObject(name);
            UnityEditor.Undo.RegisterCreatedObjectUndo(go, "Remote model group");
            go.transform.SetParent(parent, false);
            return go.transform;
        }
        static void Part(UnityEngine.Transform parent, string name, UnityEngine.Vector3 position, UnityEngine.Vector3 size, UnityEngine.Material material)
        {
            var go = UnityEngine.GameObject.CreatePrimitive(UnityEngine.PrimitiveType.Cube);
            UnityEditor.Undo.RegisterCreatedObjectUndo(go, "Remote model geometry");
            go.name = name; go.transform.SetParent(parent, false);
            go.transform.localPosition = position; go.transform.localScale = size;
            UnityEngine.Object.DestroyImmediate(go.GetComponent<UnityEngine.Collider>());
            go.GetComponent<UnityEngine.Renderer>().sharedMaterial = material;
        }
    }
}
