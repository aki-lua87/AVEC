using UdonSharp;
using UnityEngine;

namespace aki_lua87.CommonVideoPlaylist
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class PlaylistButton : UdonSharpBehaviour
    {
        public PlaylistView view;
        public int index;
        public bool isTab;
        public bool enqueue;
        public bool removeQueueSlot;
        public bool historySlot;
        public void _Click()
        {
            if (view == null) return;
            if (historySlot) view._PlayHistorySlot(index);
            else if (enqueue) view._EnqueueVideo(index);
            else if (removeQueueSlot) view._RemoveQueueSlot(index);
            else if (isTab) view._SelectTab(index);
            else view._SelectVideo(index);
        }
    }
}
