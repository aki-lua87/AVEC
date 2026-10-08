using UdonSharp;
using VRC.SDK3.UdonNetworkCalling;
using aki_lua87.CommonVideoPlaylist;

namespace aki_lua87.CommonGestureRemote
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class PlaylistRemoteTarget : RemoteTarget
    {
        public PlaylistView view;
        public override bool _Ready() { return view != null && view.adapter != null && view.adapter._Available(); }
        public override int _Execute(int action, float amount)
        {
            if (NetworkCalling.InNetworkCall || !_Ready()) return 0;
            if (action == 1 || action == 2) return view.adapter._StepVideo(view, action == 1 ? 1 : -1);
            if (action == 9) return view.adapter._TogglePlaybackMode(view, view._SelectedTab());
            if (action == 16) return view.adapter._ToggleRepeat();
            if (action == 18 || action == 19 || action == 20)
                return view.adapter._SetPlaybackMode(view, action == 18 ? 0 : action == 20 ? 1 : 2);
            if (action == 10 || action == 11) return view._StepTab(action == 10 ? 1 : -1);
            return 5;
        }
    }
}
