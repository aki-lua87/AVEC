using UnityEditor;
using aki_lua87.AVEC.Editor;
namespace aki_lua87.AVEC.Connectors.Editor
{
    [InitializeOnLoad]
    public static class IwaSyncConnector
    {
        static IwaSyncConnector() => ConnectorRegistry.Register(new ConnectorDefinition {
            Id = "IwaSync", DisplayName = "iwaSync3", Order = 0,
            PlayerType = "HoshinoLabs.IwaSync3.Udon.VideoCore", PlaylistAdapterType = "aki_lua87.CommonVideoPlaylist.IwaSyncAdapter",
            RemoteTargetType = "aki_lua87.CommonGestureRemote.IwaSyncRemoteTarget", AuthorityField = "",
            ConfigurePlaylist = (adapter, player) => ConnectorWiring.ConfigurePlaylist(adapter, player, 0),
            ValidatePlaylist = (adapter, player) => ConnectorWiring.ValidatePlaylist(adapter, player, 0),
            ConfigureRemote = (target, player) => ConnectorWiring.ConfigureRemote(target, player, 0),
            DiscoverRemoteOutputs = (target, player) => ConnectorWiring.DetectOutputs(target, player, 0),
        });
    }
}
