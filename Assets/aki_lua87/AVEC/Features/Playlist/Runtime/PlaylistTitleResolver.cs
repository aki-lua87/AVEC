using UdonSharp;
using VRC.SDKBase;

namespace aki_lua87.CommonVideoPlaylist
{
    // Optional title extensions implement this; the playlist has no metadata-provider dependency.
    public abstract class PlaylistTitleResolver : UdonSharpBehaviour
    {
        public abstract void _Resolve(VRCUrl source, PlaylistUserData destination, int entryId);
    }
}
