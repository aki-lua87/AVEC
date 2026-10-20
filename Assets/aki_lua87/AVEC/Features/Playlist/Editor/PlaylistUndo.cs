using System;
using UnityEditor;
using UnityEngine;
using UdonSharp;
using UdonSharpEditor;
using Object = UnityEngine.Object;

namespace aki_lua87.CommonVideoPlaylist.Editor
{
    // Only derived playlist updates opt out; explicit UI authoring keeps normal Undo.
    public static class PlaylistUndo
    {
        static int suppressionDepth;
        public static bool Recording => suppressionDepth == 0;
        public sealed class Scope : IDisposable
        {
            readonly bool suppressed;
            bool disposed;
            public Scope(bool recordUndo)
            {
                suppressed = !recordUndo;
                if (suppressed) suppressionDepth++;
            }
            public void Dispose()
            {
                if (disposed) return;
                disposed = true;
                if (suppressed) suppressionDepth--;
            }
        }
        public static void RecordObject(Object target, string name)
        { if (Recording) Undo.RecordObject(target, name); }
        public static void RegisterCreatedObjectUndo(Object target, string name)
        { if (Recording) Undo.RegisterCreatedObjectUndo(target, name); }
        public static void RegisterFullObjectHierarchyUndo(Object target, string name)
        { if (Recording) Undo.RegisterFullObjectHierarchyUndo(target, name); }
        public static void DestroyObjectImmediate(Object target)
        {
            if (Recording) Undo.DestroyObjectImmediate(target);
            else Object.DestroyImmediate(target);
        }
        public static T AddComponent<T>(GameObject target) where T : Component
        { return Recording ? Undo.AddComponent<T>(target) : target.AddComponent<T>(); }
        public static T AddUdonSharpComponent<T>(GameObject target) where T : UdonSharpBehaviour
        { return Recording ? UdonSharpUndo.AddComponent<T>(target) : target.AddUdonSharpComponent<T>(); }
        public static void SetTransformParent(Transform target, Transform parent, string name)
        {
            if (Recording) Undo.SetTransformParent(target, parent, name);
            else target.SetParent(parent, true);
        }
    }
}
