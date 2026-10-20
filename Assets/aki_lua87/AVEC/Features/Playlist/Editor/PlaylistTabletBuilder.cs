using System;
using System.Collections.Generic;
using System.Linq;
using UdonSharpEditor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using VRC.Udon;

namespace aki_lua87.CommonVideoPlaylist.Editor
{
    public static class PlaylistTabletBuilder
    {
        public const string PrefabPath = "Assets/aki_lua87/AVEC/Prefabs/CommonVideoPlaylistTablet.prefab";

        [MenuItem("Tools/AVEC/Playlist/Create Handheld Copy of Selected Playlist")]
        public static void CreateSelected()
        {
            var source = Selection.activeGameObject == null ? null : Selection.activeGameObject.GetComponentInParent<PlaylistSetup>();
            if (source == null || !PlaylistConnections.CanConnect(source))
            {
                Debug.LogWarning("[CVP] シーン内のPlaylistSetupを選択してください。");
                return;
            }
            Selection.activeGameObject = Create(source);
        }

        // Copies the current in-memory UI, including authored layouts, data and connections.
        // Never bakes, rethemes or applies overrides to the wall-mounted source.
        public static GameObject Create(PlaylistSetup source, bool connect = true)
        {
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Create handheld playlist");
            var root = new GameObject("CommonVideoPlaylist Tablet");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, source.gameObject.scene);
            Undo.RegisterCreatedObjectUndo(root, "Create handheld playlist");
            root.transform.SetParent(source.transform.parent, true);
            root.transform.SetPositionAndRotation(source.transform.position + source.transform.right * .85f - source.transform.up * .1f, source.transform.rotation);
            root.layer = 13; // Pickup; Canvas keeps its authored UI layer.
            var body = Undo.AddComponent<Rigidbody>(root);
            body.useGravity = false; body.isKinematic = true; body.mass = .5f;
            var backCollider = Undo.AddComponent<BoxCollider>(root);
            var pickup = Undo.AddComponent<VRC.SDK3.Components.VRCPickup>(root);
            pickup.AutoHold = VRC.SDKBase.VRC_Pickup.AutoHoldMode.No;
            pickup.orientation = VRC.SDKBase.VRC_Pickup.PickupOrientation.Any;
            pickup.proximity = .6f;
            pickup.InteractionText = "タブレットを持つ";
            pickup.DisallowTheft = true;
            var clone = UnityEngine.Object.Instantiate(source.gameObject, root.transform, false);
            clone.name = "Playlist Screen";
            clone.transform.localPosition = Vector3.zero;
            clone.transform.localRotation = Quaternion.identity;
            var rect = clone.GetComponent<RectTransform>();
            float scale = .32f / rect.rect.width;
            clone.transform.localScale = Vector3.one * scale;
            var setup = clone.GetComponent<PlaylistSetup>();
            // RectTransform scale changes only; all child layout, fonts and colors survive.
            var touch = root.AddUdonSharpComponent<PlaylistTabletTouch>();
            touch.surface = rect; touch.pickup = pickup;
            float height = rect.rect.height * scale;
            backCollider.center = new Vector3(0, 0, .0035f);
            backCollider.size = new Vector3(.342f, height + .022f, .005f);
            AddFrame(root.transform, "Back", new Vector3(0, 0, .0035f), new Vector3(.342f, height + .022f, .005f));
            AddFrame(root.transform, "Left grip", new Vector3(-.168f, 0, .003f), new Vector3(.016f, height + .022f, .006f));
            AddFrame(root.transform, "Right grip", new Vector3(.168f, 0, .003f), new Vector3(.016f, height + .022f, .006f));
            AddFrame(root.transform, "Top grip", new Vector3(0, height * .5f + .007f, .003f), new Vector3(.32f, .014f, .006f));
            AddFrame(root.transform, "Bottom grip", new Vector3(0, -height * .5f - .007f, .003f), new Vector3(.32f, .014f, .006f));
            foreach (var proxy in clone.GetComponentsInChildren<UdonSharp.UdonSharpBehaviour>(true))
                UdonSharpEditorUtility.CopyProxyToUdon(proxy);
            RefreshBindings(setup);
            if (connect && setup.player != null) PlaylistConnections.Connect(setup, setup.player);
            EditorSceneManager.MarkSceneDirty(root.scene);
            Undo.CollapseUndoOperations(group);
            return root;
        }

        static void AddFrame(Transform parent, string name, Vector3 position, Vector3 size)
        {
            var frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frame.name = name; frame.layer = 13;
            frame.transform.SetParent(parent, false);
            frame.transform.localPosition = position; frame.transform.localScale = size;
            frame.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            frame.GetComponent<Renderer>().receiveShadows = false;
            frame.GetComponent<Renderer>().sharedMaterial = FrameMaterial();
            if (name == "Back") frame.GetComponent<Collider>().enabled = false;
        }

        static Material FrameMaterial()
        {
            const string folder = "Assets/aki_lua87/AVEC/Features/Playlist/Materials";
            const string path = folder + "/TabletFrame.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder(PlaylistFactory.Root, "Materials");
            material = new Material(Shader.Find("Unlit/Color"));
            material.color = new Color(.035f, .04f, .05f);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        [MenuItem("Tools/AVEC/Playlist/Create Handheld Prefab")]
        public static void CreatePrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
            {
                Debug.Log("[CVP] Existing handheld prefab preserved."); return;
            }
            var preview = EditorSceneManager.NewPreviewScene();
            try
            {
                var source = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PlaylistFactory.PrefabPath), preview);
                var tablet = Create(source.GetComponent<PlaylistSetup>(), false);
                tablet.transform.SetParent(null, false);
                tablet.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                PrefabUtility.SaveAsPrefabAsset(tablet, PrefabPath);
            }
            finally { EditorApplication.delayCall += () => EditorSceneManager.ClosePreviewScene(preview); }
        }

        [MenuItem("CONTEXT/PlaylistTabletTouch/Refresh Touch Bindings")]
        static void RefreshSelectedBindings(MenuCommand command)
        {
            var touch = (PlaylistTabletTouch)command.context;
            if (touch.surface != null) RefreshBindings(touch.surface.GetComponent<PlaylistSetup>());
        }

        public static void RefreshBindings(PlaylistSetup setup)
        {
            if (setup == null) return;
            var touch = setup.GetComponentInParent<PlaylistTabletTouch>();
            if (touch == null) return;
            var buttons = new List<Button>();
            var targets = new List<UdonBehaviour>();
            var names = new List<string>();
            foreach (var button in setup.GetComponentsInChildren<Button>(true))
            {
                var serialized = new SerializedObject(button);
                var calls = serialized.FindProperty("m_OnClick.m_PersistentCalls.m_Calls");
                for (int i = 0; i < calls.arraySize; i++)
                {
                    var call = calls.GetArrayElementAtIndex(i);
                    if (call.FindPropertyRelative("m_CallState").enumValueIndex == 0) continue;
                    var target = call.FindPropertyRelative("m_Target").objectReferenceValue as UdonBehaviour;
                    string method = call.FindPropertyRelative("m_MethodName").stringValue;
                    if (target == null || method != "SendCustomEvent" || call.FindPropertyRelative("m_Mode").enumValueIndex != 5)
                    {
                        Debug.LogWarning("[CVP] Finger touch requires SendCustomEvent(string): " + button.name, button); continue;
                    }
                    buttons.Add(button); targets.Add(target);
                    names.Add(call.FindPropertyRelative("m_Arguments.m_StringArgument").stringValue);
                }
            }
            PlaylistUndo.RecordObject(touch, "Refresh tablet touch bindings");
            touch.eventButtons = buttons.ToArray(); touch.eventTargets = targets.ToArray(); touch.eventNames = names.ToArray();
            UdonSharpEditorUtility.CopyProxyToUdon(touch);
            EditorUtility.SetDirty(touch);
        }
    }
}
