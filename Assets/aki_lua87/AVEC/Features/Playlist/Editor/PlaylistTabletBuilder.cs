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


        // Narrow migration: retain the screen, its layout and every data/connection override.
        public static void RefreshShape(PlaylistTabletTouch tablet)
        {
            if (tablet == null) return;
            if (tablet.surface != null)
                foreach (var collider in tablet.surface.GetComponents<Collider>())
                {
                    Undo.RecordObject(collider.gameObject, "Nonblocking tablet screen");
                    collider.gameObject.layer = 13;
                    PlaylistConnections.Save(collider.gameObject);
                }
            foreach (string name in new[] { "Left grip", "Right grip", "Top grip", "Bottom grip" })
            {
                var grip = tablet.transform.Find(name);
                if (grip == null) continue;
                var renderer = grip.GetComponent<Renderer>();
                if (renderer != null) { Undo.RecordObject(renderer, "Rectangular tablet frame"); renderer.enabled = false; PlaylistConnections.Save(renderer); }
                var collider = grip.GetComponent<Collider>();
                if (collider != null) { Undo.RecordObject(collider, "Nonblocking tablet frame"); collider.enabled = false; PlaylistConnections.Save(collider); }
            }
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
