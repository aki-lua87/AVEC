using UnityEditor;
using UnityEngine;
using UdonSharpEditor;
using aki_lua87.AVEC.Editor;

namespace aki_lua87.AVEC.SimpleScreen.Editor
{
    [InitializeOnLoad]
    public static class SimpleScreenConnector
    {
        static SimpleScreenConnector() => ConnectorRegistry.Register(new ConnectorDefinition {
            Id = "SimpleScreen", DisplayName = "AVEC-SimpleScreen", Order = 6,
            PlayerType = "aki_lua87.AVEC.SimpleScreen.SimpleScreenPlayer",
            PlaylistAdapterType = "aki_lua87.AVEC.SimpleScreen.SimpleScreenAdapter",
            RemoteTargetType = "aki_lua87.AVEC.SimpleScreen.SimpleScreenRemoteTarget",
            ConfigurePlaylist = (adapter, player) => {
                var screen = (SimpleScreenPlayer)player;
                Undo.RecordObject(screen, "Connect SimpleScreen playlist");
                screen.adapter = (SimpleScreenAdapter)adapter;
                UdonSharpEditorUtility.CopyProxyToUdon(screen);
                EditorUtility.SetDirty(screen); PrefabUtility.RecordPrefabInstancePropertyModifications(screen);
            },
            ValidatePlaylist = (adapter, player) => ((SimpleScreenPlayer)player).adapter != adapter ? "SimpleScreenのアダプター接続を修復してください。" : null,
            DiscoverRemoteOutputs = (target, player) => {
                var output = (SimpleScreenRemoteTarget)target;
                var screen = (SimpleScreenPlayer)player;
                output.screens = screen.screen == null ? new Renderer[0] : new[]{ screen.screen };
                output.screenGraphics = new UnityEngine.UI.Graphic[0];
            }
        });
    }
}
