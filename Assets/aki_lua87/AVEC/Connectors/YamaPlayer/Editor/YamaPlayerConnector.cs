using UnityEditor;
using aki_lua87.AVEC.Editor;
namespace aki_lua87.AVEC.Connectors.Editor
{
    [InitializeOnLoad]
    public static class YamaPlayerConnector
    {
        static YamaPlayerConnector() => ConnectorRegistry.Register(new ConnectorDefinition {
            Id = "YamaPlayer", DisplayName = "YamaPlayer", Order = 3,
            PlayerType = "Yamadev.YamaStream.UI.UIController", PlaylistAdapterType = "aki_lua87.CommonVideoPlaylist.YamaPlayerAdapter",
            RemoteTargetType = "aki_lua87.CommonGestureRemote.YamaPlayerRemoteTarget", AuthorityField = "_controller",
            ConfigurePlaylist = (adapter, player) => ConnectorWiring.ConfigurePlaylist(adapter, player, 3),
            ValidatePlaylist = (adapter, player) => ConnectorWiring.ValidatePlaylist(adapter, player, 3),
            ConfigureRemote = (target, player) => ConnectorWiring.ConfigureRemote(target, player, 3),
            DiscoverRemoteOutputs = (target, player) => ConnectorWiring.DetectOutputs(target, player, 3),
        });
    }
}
