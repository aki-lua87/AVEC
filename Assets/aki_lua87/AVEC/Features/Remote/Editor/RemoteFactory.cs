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
    // Shared path used by remote models and summon-area authoring.
    public static class RemoteFactory
    {
        public const string Root = aki_lua87.AVEC.Editor.PackageLayout.Remote;
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
