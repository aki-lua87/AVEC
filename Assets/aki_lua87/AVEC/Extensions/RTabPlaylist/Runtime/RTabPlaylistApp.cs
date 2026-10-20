using UdonSharp;
using UnityEngine;
using Rasta.Tablet;
using aki_lua87.CommonVideoPlaylist;

namespace aki_lua87.AVEC.Extensions.RTabPlaylist
{
    // RTAB owns app visibility and touch dispatch; the playlist owns playback.
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class RTabPlaylistApp : TabletAppBehaviour
    {
        public PlaylistView view;

        public override void OnAppOpen()
        {
            if (view == null) return;
            view._RefreshVisibleEntries();
            view._RefreshVolume();
        }
    }
}
