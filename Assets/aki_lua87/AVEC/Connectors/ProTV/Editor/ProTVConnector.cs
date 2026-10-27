using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using aki_lua87.AVEC.Editor;

namespace aki_lua87.AVEC.Connectors.Editor
{
    [InitializeOnLoad]
    public static class ProTVConnector
    {
        static ProTVConnector() => ConnectorRegistry.Register(new ConnectorDefinition {
            Id = "ProTV", DisplayName = "ProTV V3", SupportedMajorVersion = "3", Order = 4,
            PlayerType = "ArchiTech.ProTV.TVManager",
            PlaylistAdapterType = "aki_lua87.CommonVideoPlaylist.ProTVAdapter",
            RemoteTargetType = "aki_lua87.CommonGestureRemote.ProTVRemoteTarget",
            ConfigurePlaylist = ConfigurePlaylist,
            ValidatePlaylist = (adapter, player) => {
                var listener = adapter.GetType().GetField("eventsListener").GetValue(adapter) as Component;
                if (listener == null || listener.GetType().GetField("tv").GetValue(listener) as Component != player ||
                    listener.GetType().GetField("adapter").GetValue(listener) as Component != adapter)
                    return "ProTVのイベント接続が未設定です。「接続設定を修復」で更新してください。";
                return null;
            },
            DiscoverRemoteOutputs = DiscoverOutputs,
        });

        static void ConfigurePlaylist(Component adapter, Component player)
        {
            // ATEventHandler disallows multiple components on a single GameObject.
            // Keep the TVPlugin on a dedicated child, while the synced adapter shares TV ownership.
            var field = adapter.GetType().GetField("eventsListener");
            var listener = field.GetValue(adapter) as Component;
            if (listener == null) {
                var type = System.AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("aki_lua87.CommonVideoPlaylist.ProTVSequenceListener")).First(t=>t!=null);
                var go = new GameObject("AVEC ProTV Events"); Undo.RegisterCreatedObjectUndo(go,"Add ProTV listener"); go.transform.SetParent(player.transform,false);
                listener = UdonSharpEditor.UdonSharpUndo.AddComponent(go,type);
                field.SetValue(adapter,listener);
            }
            Undo.RecordObject(listener,"Wire ProTV listener");
            listener.GetType().GetField("tv").SetValue(listener,player);
            listener.GetType().GetField("adapter").SetValue(listener,adapter);
            UdonSharpEditor.UdonSharpEditorUtility.CopyProxyToUdon((UdonSharp.UdonSharpBehaviour)listener);
            EditorUtility.SetDirty(listener); PrefabUtility.RecordPrefabInstancePropertyModifications(listener);
            var backing=UdonSharpEditor.UdonSharpEditorUtility.GetBackingUdonBehaviour((UdonSharp.UdonSharpBehaviour)listener);
            if(backing==null || backing.programSource==null) throw new System.InvalidOperationException("ProTV listener program missing.");
        }

        static Component[] Managers(Component player)
        {
            var property = new SerializedObject(player).FindProperty("videoManagers");
            var result = new List<Component>();
            if (property != null)
                for (int i = 0; i < property.arraySize; i++)
                    if (property.GetArrayElementAtIndex(i).objectReferenceValue is Component manager) result.Add(manager);
            // Authoring/build-time arrays may not yet be populated.
            result.AddRange(player.GetComponentsInChildren<Component>(true).Where(c => c != null && c.GetType().FullName == "ArchiTech.ProTV.VPManager"));
            return result.Distinct().ToArray();
        }
        static IEnumerable<Object> References(Component component, string field)
        {
            var property = new SerializedObject(component).FindProperty(field);
            if (property == null || !property.isArray) yield break;
            for (int i = 0; i < property.arraySize; i++) yield return property.GetArrayElementAtIndex(i).objectReferenceValue;
        }
        static void DiscoverOutputs(Component target, Component player)
        {
            var renderers = new List<Renderer>();
            var graphics = new List<Graphic>();
            foreach (var manager in Managers(player))
                foreach (var screen in References(manager, "screens").OfType<GameObject>()) {
                    renderers.AddRange(screen.GetComponentsInChildren<Renderer>(true));
                    graphics.AddRange(screen.GetComponentsInChildren<Graphic>(true));
                }
            var materials = References(player, "customMaterials").OfType<Material>().ToArray();
            renderers.AddRange(Object.FindObjectsOfType<Renderer>(true).Where(r => r.gameObject.scene == player.gameObject.scene && r.sharedMaterials.Any(m => materials.Contains(m))));
            graphics.AddRange(Object.FindObjectsOfType<Graphic>(true).Where(g => g.gameObject.scene == player.gameObject.scene && materials.Contains(g.material)));
            target.GetType().GetField("screens").SetValue(target, renderers.Distinct().ToArray());
            target.GetType().GetField("screenGraphics").SetValue(target, graphics.Distinct().ToArray());
        }
    }
}
