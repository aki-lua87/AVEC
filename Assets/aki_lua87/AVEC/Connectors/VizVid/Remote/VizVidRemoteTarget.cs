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
        public override bool _CanStep() { return player != null && player.PlayListUrls != null && player.PlayListUrls.Length > 0; }
        public override int _StepNative(int direction)
        {
            if (NetworkCalling.InNetworkCall || !_Ready() || !_CanStep()) return 4;
            if (player.Locked) return 2;
            int list = player.PlayListIndex; if (list <= 0) list = 1;
            int[] offsets = player.PlayListUrlOffsets;
            if (offsets == null || list > offsets.Length) return 4;
            int start = offsets[list - 1];
            int end = list < offsets.Length ? offsets[list] : player.PlayListUrls.Length;
            int count = end - start; if (count <= 0) return 4;
            int current = player.CurrentPlayingIndex;
            int next = current < 0 ? (direction < 0 ? count - 1 : 0) : (current + (direction < 0 ? -1 : 1) + count) % count;
            player.PlayAt(list, next, false); return 1;
        }
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
