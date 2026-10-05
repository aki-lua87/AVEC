using aki_lua87.AVEC.Editor;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UdonSharp;
using UdonSharpEditor;

namespace aki_lua87.CommonGestureRemote.Editor
{
    public static class RemoteConnections
    {
        public static string[] Kinds => ConnectorRegistry.All.Select(d => d.DisplayName).ToArray();
        public static string[] PlayerTypes => ConnectorRegistry.All.Select(d => d.PlayerType).ToArray();
        public static string[] TargetTypes => ConnectorRegistry.All.Select(d => d.RemoteTargetType).ToArray();
        public const string ViewType = "aki_lua87.CommonVideoPlaylist.PlaylistView";
        public static Type Resolve(string name) => AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name)).FirstOrDefault(t => t != null);
        public static int Kind(Component player) => player == null ? -1 : Array.IndexOf(PlayerTypes, player.GetType().FullName);
        public static bool InScene(RemoteSetup setup) => setup != null && !EditorUtility.IsPersistent(setup) && setup.gameObject.scene.IsValid() && PrefabStageUtility.GetPrefabStage(setup.gameObject) == null;
        public static List<Component> Find(RemoteSetup setup)
        {
            var result = new List<Component>();
            if (!InScene(setup)) return result;
            foreach (string name in PlayerTypes)
            {
                var type = Resolve(name);
                if (type != null) result.AddRange(UnityEngine.Object.FindObjectsOfType(type, true).OfType<Component>().Where(c => c.gameObject.scene == setup.gameObject.scene));
            }
            return result;
        }
        public static string Label(Component c)
        {
            if (c == null) return "未接続";
            string path = c.name;
            for (Transform t = c.transform.parent; t != null; t = t.parent) path = t.name + "/" + path;
            int kind = Kind(c);
            return (kind < 0 ? "Playlist" : Kinds[kind]) + " — " + path;
        }
        public static UnityEngine.Object Ref(Component c, string field)
        {
            if (c == null) return null;
            return new SerializedObject(c).FindProperty(field)?.objectReferenceValue;
        }
        public static List<Component> FindPlaylists(RemoteSetup setup)
        {
            var type = Resolve(ViewType);
            if (type == null || !InScene(setup) || setup.player == null) return new List<Component>();
            return UnityEngine.Object.FindObjectsOfType(type, true).OfType<Component>()
                .Where(v => v.gameObject.scene == setup.gameObject.scene && Ref(Ref(v, "adapter") as Component, "player") == setup.player).ToList();
        }
        public static bool AutoConnect(RemoteSetup setup)
        {
            if (!InScene(setup) || setup.remote == null || setup.player != null) return false;
            var found = Find(setup);
            if (found.Count != 1) return false;
            Connect(setup, found[0]);
            return true;
        }
        public static void Connect(RemoteSetup setup, Component player)
        {
            if (!InScene(setup) || setup.remote == null) throw new InvalidOperationException("シーン上のリモコンを選択してください。");
            int kind = Kind(player);
            if (player != null && (kind < 0 || player.gameObject.scene != setup.gameObject.scene)) throw new InvalidOperationException("同じシーンの対応プレイヤーを選択してください。");
            Type type = player == null ? null : Resolve(TargetTypes[kind]);
            if (player != null && type == null) throw new InvalidOperationException(Kinds[kind] + " Connector が必要です。");
            Undo.RecordObjects(new UnityEngine.Object[] { setup, setup.remote }, "Connect gesture remote");
            setup.player = player;
            setup.remote.playerTarget = null;
            if (player != null)
            {
                var target = UnityEngine.Object.FindObjectsOfType(type, true).OfType<RemoteTarget>()
                    .FirstOrDefault(t => t.gameObject.scene == setup.gameObject.scene && Ref(t, "player") == player);
                bool created = target == null;
                if (created)
                {
                    var go = new GameObject("Gesture Remote Target");
                    Undo.RegisterCreatedObjectUndo(go, "Create remote target");
                    go.transform.SetParent(player.transform, false);
                    target = (RemoteTarget)UdonSharpUndo.AddComponent(go, type);
                }
                Undo.RecordObject(target, "Wire remote target");
                type.GetField("player").SetValue(target, player);

                if (created) ConnectorRegistry.All[kind].DiscoverRemoteOutputs?.Invoke(target, player);
                ConnectorRegistry.All[kind].ConfigureRemote?.Invoke(target, player);
                Save(target);
                CheckProgram(target);
                setup.remote.playerTarget = target;
            }
            // Clear a playlist from the old player, never navigate a different player accidentally.
            if (setup.playlist != null && Ref(Ref(setup.playlist, "adapter") as Component, "player") != player) setup.playlist = null;
            var playlists = FindPlaylists(setup);
            if (setup.playlist == null && playlists.Count == 1 && Resolve("aki_lua87.CommonGestureRemote.PlaylistRemoteTarget") != null) setup.playlist = playlists[0];
            ConnectPlaylist(setup);
            Save(setup.remote); Save(setup);
        }
        public static void ConnectPlaylist(RemoteSetup setup)
        {
            if (setup.remote == null || !InScene(setup)) return;
            Undo.RecordObject(setup.remote, "Connect remote playlist");
            setup.remote.playlistTarget = null;
            if (setup.playlist != null)
            {
                if (setup.playlist.GetType().FullName != ViewType || setup.playlist.gameObject.scene != setup.gameObject.scene ||
                    Ref(Ref(setup.playlist, "adapter") as Component, "player") != setup.player || setup.player == null)
                    throw new InvalidOperationException("同じ接続先プレイヤーの PlaylistView を選択してください。");
                var type = Resolve("aki_lua87.CommonGestureRemote.PlaylistRemoteTarget");
                if (type == null) throw new InvalidOperationException("PlaylistRemote Extension が必要です。");
                var target = setup.GetComponentsInChildren<RemoteTarget>(true).FirstOrDefault(t => t.GetType() == type);
                if (target == null)
                {
                    var go = new GameObject("Playlist Bridge");
                    Undo.RegisterCreatedObjectUndo(go, "Create playlist bridge");
                    go.transform.SetParent(setup.transform, false);
                    target = (RemoteTarget)UdonSharpUndo.AddComponent(go, type);
                }
                Undo.RecordObject(target, "Wire playlist bridge");
                type.GetField("view").SetValue(target, setup.playlist);
                Save(target); CheckProgram(target);
                setup.remote.playlistTarget = target;
            }
            Save(setup.remote); Save(setup);
        }
        public static void DetectOutputs(RemoteTarget target, Component player, int kind)
        {
            if (kind >= 0 && kind < ConnectorRegistry.All.Length) ConnectorRegistry.All[kind].DiscoverRemoteOutputs?.Invoke(target, player);
        }
        public static void CheckProgram(UdonSharpBehaviour script)
        {
            var backing = UdonSharpEditorUtility.GetBackingUdonBehaviour(script);
            var program = backing == null ? null : backing.programSource as UdonSharpProgramAsset;
            if (program == null || program.sourceCsScript.GetClass() != script.GetType())
                throw new InvalidOperationException("Udon Program Source が未設定です。Tools > AVEC > Remote > Generate Assets を実行してください。");
        }
        public static void Save(UnityEngine.Object obj)
        {
            if (obj is UdonSharpBehaviour udon) UdonSharpEditorUtility.CopyProxyToUdon(udon);
            EditorUtility.SetDirty(obj);
            PrefabUtility.RecordPrefabInstancePropertyModifications(obj);
            if (obj is Component c && c.gameObject.scene.IsValid()) EditorSceneManager.MarkSceneDirty(c.gameObject.scene);
        }
    }
}
