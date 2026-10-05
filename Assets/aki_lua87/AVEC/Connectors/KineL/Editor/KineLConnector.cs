using UnityEditor;
using aki_lua87.AVEC.Editor;
namespace aki_lua87.AVEC.Connectors.Editor
{
    [InitializeOnLoad]
    public static class KineLConnector
    {
        static KineLConnector() => ConnectorRegistry.Register(new ConnectorDefinition {
            Id = "KineL", DisplayName = "KineL", Order = 1,
            PlayerType = "Kinel.VideoPlayer.Udon.KinelVideoPlayer", PlaylistAdapterType = "aki_lua87.CommonVideoPlaylist.KinelAdapter",
            RemoteTargetType = "aki_lua87.CommonGestureRemote.KinelRemoteTarget", AuthorityField = "",
            ConfigurePlaylist = (adapter, player) => ConnectorWiring.ConfigurePlaylist(adapter, player, 1),
            ValidatePlaylist = (adapter, player) => ConnectorWiring.ValidatePlaylist(adapter, player, 1),
            ConfigureRemote = (target, player) => ConnectorWiring.ConfigureRemote(target, player, 1),
            DiscoverRemoteOutputs = (target, player) => ConnectorWiring.DetectOutputs(target, player, 1),
            AudioSources = player => ConnectorWiring.AudioSources(player, 1)
        });
    }
}
