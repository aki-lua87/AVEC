using UdonSharp;
using VRC.SDKBase;

namespace aki_lua87.CommonVideoPlaylist
{
    // Optional connectors implement this without making the playlist depend on a player package.
    public abstract class PlaylistTitleResolver : UdonSharpBehaviour
    {
        public abstract void _Resolve(VRCUrl source, PlaylistUserData destination, int entryId);
    }
}
