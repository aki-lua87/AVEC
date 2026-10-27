using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using aki_lua87.AVEC.Editor;

namespace aki_lua87.AVEC.Connectors.Editor
{
    [InitializeOnLoad]
    public static class KineLV3Connector
    {
        const string Native = "Kinel.VideoPlayer.V3.Udon.System.";
        static KineLV3Connector() => ConnectorRegistry.Register(new ConnectorDefinition {
            Id = "KineLV3", DisplayName = "KineL V3", SupportedMajorVersion = "3", Order = 5,
            PlayerType = Native + "KinelPlayerController",
            PlaylistAdapterType = "aki_lua87.CommonVideoPlaylist.KinelV3Adapter",
            RemoteTargetType = "aki_lua87.CommonGestureRemote.KinelV3RemoteTarget",
            ResolveAuthority = player => Module(player, Native + "Sync.KinelVariableSyncer"),
            ConfigurePlaylist = ConfigurePlaylist,
            ValidatePlaylist = (adapter, player) => {
                var listener = Field(adapter, "eventsListener");
                return Field(adapter,"syncer") != Module(player, Native + "Sync.KinelVariableSyncer") ||
                    Field(adapter,"audioManager") == null || listener == null || Field(listener,"player") != player || Field(listener,"adapter") != adapter
                    ? "KineL V3のSync Module・AudioModule・イベント接続を確認してください。" : null;
            },
            ConfigureRemote = Configure,
            DiscoverRemoteOutputs = DiscoverOutputs,
        });
        static Type Resolve(string name) => AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(name)).FirstOrDefault(t=>t!=null);
        static Component Field(Component c, string name) => c == null ? null : new SerializedObject(c).FindProperty(name)?.objectReferenceValue as Component;
        static Component Module(Component player, string typeName)
        {
            var type = Resolve(typeName);
            if (player == null || type == null) return null;
            var matches = UnityEngine.Object.FindObjectsOfType(type, true).OfType<Component>()
                .Where(c=>c.gameObject.scene == player.gameObject.scene && Field(c,"controller") == player).ToArray();
            return matches.Length == 1 ? matches[0] : null;
        }
        static void Configure(Component target, Component player)
        {
            target.GetType().GetField("syncer").SetValue(target, Module(player, Native + "Sync.KinelVariableSyncer"));
            target.GetType().GetField("audioManager").SetValue(target, Module(player, Native + "Component.KinelAudioManager"));
        }
        static void ConfigurePlaylist(Component adapter, Component player)
        {
            Configure(adapter, player);
            var field = adapter.GetType().GetField("eventsListener");
            var listener = field.GetValue(adapter) as UdonSharp.UdonSharpBehaviour;
            if (listener == null) {
                var go = new GameObject("AVEC KineL V3 Events");
                Undo.RegisterCreatedObjectUndo(go,"Add KineL V3 listener"); go.transform.SetParent(adapter.transform,false);
                listener = UdonSharpEditor.UdonSharpUndo.AddComponent(go,Resolve("aki_lua87.CommonVideoPlaylist.KinelV3SequenceListener"));
                field.SetValue(adapter,listener);
            }
            Undo.RecordObject(listener,"Wire KineL V3 listener");
            listener.GetType().GetField("player").SetValue(listener,player);
            listener.GetType().GetField("adapter").SetValue(listener,adapter);
            UdonSharpEditor.UdonSharpEditorUtility.CopyProxyToUdon(listener);
            EditorUtility.SetDirty(listener); PrefabUtility.RecordPrefabInstancePropertyModifications(listener);
        }
        static void DiscoverOutputs(Component target, Component player)
        {
            var type = Resolve("Kinel.VideoPlayer.V3.Udon.Interface.KinelMeshScreen");
            var screens = type == null ? new Renderer[0] : UnityEngine.Object.FindObjectsOfType(type,true).OfType<Component>()
                .Where(c=>c.gameObject.scene==player.gameObject.scene && Field(c,"handler")==player)
                .Select(c=>Field(c,"targetRenderer") as Renderer).Where(r=>r!=null).ToArray();
            target.GetType().GetField("screens").SetValue(target,screens);
            target.GetType().GetField("screenGraphics").SetValue(target,new UnityEngine.UI.Graphic[0]);
        }
    }
}
