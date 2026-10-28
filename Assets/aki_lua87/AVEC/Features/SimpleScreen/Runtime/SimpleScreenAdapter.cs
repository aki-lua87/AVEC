using UdonSharp;
using VRC.SDKBase;
using VRC.SDK3.UdonNetworkCalling;
using aki_lua87.CommonVideoPlaylist;

namespace aki_lua87.AVEC.SimpleScreen
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class SimpleScreenAdapter : PlaylistAdapter
    {
        public SimpleScreenPlayer player;
        public override bool _Available() { return player != null && player._Available(); }
        public override VRCUrl _CurrentUrl() { return player == null ? VRCUrl.Empty : player._Url(); }
        public override bool _IsLocalPlayback() { return false; }
        public override bool _NativeReady() { return player != null && player._Ready(); }
        public override bool _IsPlaybackRunning() { return player != null && player._Playing(); }
        public override bool _AcquireSequenceControl() { return !NetworkCalling.InNetworkCall && player != null && player._TakeControl(); }
        public override int _Request(VRCUrl url, string title) { return NetworkCalling.InNetworkCall || player == null ? 0 : player._Load(url); }
        public override float _GetVolume() { return player == null ? -1f : player._Volume(); }
        public override int _SetVolume(float value) { if (NetworkCalling.InNetworkCall || player == null) return 0; player._SetVolume(value); return 1; }
        public override bool _GetMute() { return player != null && player._Muted(); }
        public override int _SetMute(bool value) { if (NetworkCalling.InNetworkCall || player == null) return 0; player._SetMute(value); return 1; }
        public override int _ControlPlayback(bool pause) { return NetworkCalling.InNetworkCall || player == null ? 0 : player._Pause(pause); }
        public override float _GetProgress() { return player == null ? -1f : player._Progress(); }
        public override bool _GetNativeRepeat() { return player != null && player._Repeat(); }
        public override void _SetNativeMode(int mode) { if (!NetworkCalling.InNetworkCall && player != null) player._SetRepeat(mode == 2); }
    }
}
