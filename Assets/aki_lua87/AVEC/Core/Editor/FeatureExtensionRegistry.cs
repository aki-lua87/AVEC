using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace aki_lua87.AVEC.Editor
{
    public sealed class FeatureEditorExtension
    {
        public string Id, Feature, Label;
        public Action<UnityEngine.Object> Open;
        public Action<Component> Bake;
    }
    public static class FeatureExtensionRegistry
    {
        static readonly Dictionary<string, FeatureEditorExtension> entries = new Dictionary<string, FeatureEditorExtension>();
        public static void Register(FeatureEditorExtension extension)
        {
            if (extension == null || string.IsNullOrEmpty(extension.Id)) throw new ArgumentException("Extension ID is required.");
            if (entries.ContainsKey(extension.Id)) throw new InvalidOperationException("Duplicate extension: " + extension.Id);
            entries.Add(extension.Id, extension);
        }
        public static void DrawButtons(string feature, UnityEngine.Object context)
        {
            foreach (var extension in entries.Values.Where(e => e.Feature == feature).OrderBy(e => e.Id))
                if (extension.Open != null && GUILayout.Button(extension.Label)) extension.Open(context);
        }
        public static void Bake(string feature, Component setup)
        {
            foreach (var extension in entries.Values.Where(e => e.Feature == feature).OrderBy(e => e.Id)) extension.Bake?.Invoke(setup);
        }
    }
}
