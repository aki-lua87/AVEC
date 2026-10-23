using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UdonSharpEditor;
using VVMW.ThirdParties.Yttl;
using aki_lua87.AVEC.Editor;
using aki_lua87.CommonVideoPlaylist;
using aki_lua87.CommonVideoPlaylist.Editor;

namespace aki_lua87.AVEC.Extensions.YttlTitles.Editor
{
    [InitializeOnLoad]
    public static class YttlTitlesExtension
    {
        static YttlTitlesExtension() => FeatureExtensionRegistry.Register(new FeatureEditorExtension
        {
            Id = "YttlTitles", Feature = "Playlist", Configure = Configure, DrawInspector = DrawInspector
        });

        public static void Configure(Component component)
        {
            var setup = component as PlaylistSetup;
            if (setup == null || setup.view == null) return;
            var view = setup.view;
            if (!setup.enableUserList || setup.titleMetadataSource == null)
            {
                // Disconnect only this extension; other title providers belong to their owners.
                if (view.userTitleResolver is VizVidPlaylistTitles)
                {
                    Undo.RecordObject(view, "Disconnect YTTL titles");
                    view.userTitleResolver = null;
                    PlaylistConnections.Save(view);
                }
                return;
            }
            Component source = setup.titleMetadataSource;
            if (source is VRC.Udon.UdonBehaviour backing) source = UdonSharpEditorUtility.GetProxyBehaviour(backing);
            var manager = source as YttlManager;
            if (manager == null) throw new InvalidOperationException("VizVid同梱のYttlManagerを指定してください。");
            if (!PlaylistConnections.CanConnect(setup)) return;
            if (manager.gameObject.scene != setup.gameObject.scene)
                throw new InvalidOperationException("YTTLは同じシーンから指定してください。");
            var bridge = setup.GetComponent<VizVidPlaylistTitles>();
            if (bridge == null) bridge = (VizVidPlaylistTitles)UdonSharpUndo.AddComponent(setup.gameObject, typeof(VizVidPlaylistTitles));
            Undo.RecordObject(bridge, "Connect YTTL titles");
            bridge.manager = manager;
            PlaylistConnections.Save(bridge);
            Undo.RecordObject(view, "Connect YTTL titles");
            view.userTitleResolver = bridge;
            PlaylistConnections.Save(view);
        }

        static void DrawInspector(Component component)
        {
            var setup = component as PlaylistSetup;
            if (setup == null || !setup.enableUserList) return;
            EditorGUILayout.LabelField("YTTLタイトル取得（拡張）", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            var source = EditorGUILayout.ObjectField("YTTL Manager", setup.titleMetadataSource, typeof(YttlManager), true) as Component;
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(setup, "Select YTTL Manager");
                setup.titleMetadataSource = source;
                PlaylistConnections.Save(setup);
                TryConfigure(setup);
            }
            if (setup.titleMetadataSource == null && PlaylistConnections.CanConnect(setup))
            {
                var managers = UnityEngine.Object.FindObjectsOfType<YttlManager>(true)
                    .Where(m => m.gameObject.scene == setup.gameObject.scene).ToArray();
                if (managers.Length == 1 && GUILayout.Button("シーン内のYTTLを接続"))
                {
                    Undo.RecordObject(setup, "Select YTTL Manager");
                    setup.titleMetadataSource = managers[0];
                    PlaylistConnections.Save(setup);
                    TryConfigure(setup);
                }
            }
            if (setup.titleMetadataSource != null && GUILayout.Button("YTTL接続を適用")) TryConfigure(setup);
            EditorGUILayout.HelpBox("ユーザーリストへURLを追加した時にタイトルを取得します。未接続・取得失敗時はURLで利用できます。", MessageType.Info);
        }

        static void TryConfigure(PlaylistSetup setup)
        {
            try { Configure(setup); }
            catch (Exception error) { Debug.LogException(error); }
        }
    }
}
