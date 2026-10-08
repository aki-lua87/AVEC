using UdonSharp;
using VRC.SDKBase;
using VRC.SDK3.UdonNetworkCalling;
using JLChnToZ.VRC.VVMW;

namespace aki_lua87.CommonGestureRemote
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class VizVidRemoteTarget : RemoteTarget
    {
        public FrontendHandler player;
        public override bool _Ready() { return player != null && player.gameObject.activeInHierarchy && player.core != null; }
        public override float _GetVolume() { return player == null || player.core == null ? 0f : player.core.Volume; }
        public override void _SetVolume(float value) { if (!NetworkCalling.InNetworkCall && player != null && player.core != null) player.core.Volume = value; }
        public override bool _GetMute() { return player != null && player.core != null && player.core.Muted; }
        public override void _SetMute(bool value) { if (!NetworkCalling.InNetworkCall && player != null && player.core != null) player.core.Muted = value; }
        public override int _Playback(int action)
        {
            if (NetworkCalling.InNetworkCall || !_Ready() || !Utilities.IsValid(Networking.LocalPlayer)) return 0;
            if (action == 21) action = player.core.IsPlaying ? 12 : 13;
            if (action != 8 && action != 12 && action != 13 && action != 16 && action != 17 && action != 19) return 5;
            if ((action == 12 || action == 13 || action == 17) && !player.core.IsReady) return 4;
            if (action == 17 && (player.core.Duration <= 0f || float.IsInfinity(player.core.Duration) || float.IsNaN(player.core.Duration))) return 4;
            if (player.Locked) return 2;
            if (action == 16 || action == 19) {
                bool repeat = action == 19 || !player.RepeatOne;
                player.RepeatAll = false; player.Shuffle = false; player.RepeatOne = repeat; player.core.Loop = repeat;
            }
            else if (action == 17) { player.core.Progress = 0f; player._Play(); }
            else if (action == 8) player._Stop();
            else if (action == 12) player._Pause();
            else player._Play();
            return 1;
        }
    }
}
