using UdonSharp;
using VRC.SDK3.UdonNetworkCalling;
using aki_lua87.CommonGestureRemote;

namespace aki_lua87.AVEC.SimpleScreen
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class SimpleScreenRemoteTarget : RemoteTarget
    {
        public SimpleScreenPlayer player;
        public override bool _Ready() { return player != null && player._Available(); }
        public override float _GetVolume() { return player == null ? -1f : player._Volume(); }
        public override void _SetVolume(float value) { if (!NetworkCalling.InNetworkCall && player != null) player._SetVolume(value); }
        public override bool _GetMute() { return player != null && player._Muted(); }
        public override void _SetMute(bool value) { if (!NetworkCalling.InNetworkCall && player != null) player._SetMute(value); }
        public override int _Playback(int action)
        {
            if (NetworkCalling.InNetworkCall || !_Ready()) return 0;
            if (action == 21) action = player._Playing() ? 12 : 13;
            if (action == 8) return player._Stop();
            if (action == 12 || action == 13) return player._Pause(action == 12);
            if (action == 17) return player._Restart();
            if (action == 16 || action == 19) return player._SetRepeat(action == 19 || !player._Repeat());
            return 5;
        }
    }
}
