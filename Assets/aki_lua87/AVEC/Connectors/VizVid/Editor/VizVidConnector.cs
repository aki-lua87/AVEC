using UnityEditor;
using aki_lua87.AVEC.Editor;
namespace aki_lua87.AVEC.Connectors.Editor
{
    [InitializeOnLoad]
    public static class VizVidConnector
    {
        static VizVidConnector() => ConnectorRegistry.Register(new ConnectorDefinition {
            Id = "VizVid", DisplayName = "VizVid", Order = 2,
            PlayerType = "JLChnToZ.VRC.VVMW.FrontendHandler", PlaylistAdapterType = "aki_lua87.CommonVideoPlaylist.VizVidAdapter",
            RemoteTargetType = "aki_lua87.CommonGestureRemote.VizVidRemoteTarget", AuthorityField = "core",
            ConfigurePlaylist = (adapter, player) => ConnectorWiring.ConfigurePlaylist(adapter, player, 2),
            ValidatePlaylist = (adapter, player) => ConnectorWiring.ValidatePlaylist(adapter, player, 2),
            ConfigureRemote = (target, player) => ConnectorWiring.ConfigureRemote(target, player, 2),
            DiscoverRemoteOutputs = (target, player) => ConnectorWiring.DetectOutputs(target, player, 2),
            AudioSources = player => ConnectorWiring.AudioSources(player, 2)
        });
    }
}
