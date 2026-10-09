using UdonSharp;
using VRC.SDKBase;
using JLChnToZ.VRC.VVMW;

namespace aki_lua87.CommonVideoPlaylist
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class VizVidAdapter : PlaylistAdapter
    {
        public FrontendHandler player;
        public byte playerIndex = 1;
        public override float _GetVolume() { return !_Available() ? -1f : player.core.Volume; }
        public override int _SetVolume(float value)
        {
            if (VRC.SDK3.UdonNetworkCalling.NetworkCalling.InNetworkCall || !_Available()) return 0;
            player.core.Volume = UnityEngine.Mathf.Clamp01(value); return 1;
        }
        public override bool _IsPlaybackRunning() { return _Available() && player.core.IsPlaying; }
        public override bool _GetMute() { return _Available() && player.core.Muted; }
        public override int _SetMute(bool value)
        {
            if (VRC.SDK3.UdonNetworkCalling.NetworkCalling.InNetworkCall || !_Available()) return 0;
            player.core.Muted = value;
            return 1;
        }
        public override int _ControlPlayback(bool stop)
        {
            if (VRC.SDK3.UdonNetworkCalling.NetworkCalling.InNetworkCall || !_Available()) return 0;
            if (!stop && !player.core.IsReady) return 4;
            if (!_AcquireSequenceControl()) return 2;
            if (!player.core.IsReady) return 4;
            if (stop) player._Pause(); else player._Play();
            return 1;
        }
        private void Start()
        {
            if (views.Length == 0 || player == null || player.core == null) return;
            player.core._AddListener(this, "_onVideoStart");
            player.core._AddListener(this, "_onVideoReady");
            player.core._AddListener(this, "_onVideoEnd");
            player.core._AddListener(this, "_OnVideoBeginLoad");
            player.core._AddListener(this, "_OnVideoError");
            player._AddListener(this, "_OnStop");
        }
        public override void _SetNativeMode(int mode) { if (!_Available()) return; player.RepeatAll = false; player.RepeatOne = mode == 2; player.Shuffle = mode == 1; player.core.Loop = mode == 2; }
        public override bool _GetNativeRepeat() { return _Available() && (player.RepeatOne || player.core.Loop); }
        public override float _GetProgress() { return !_Available() || player.core.Duration <= 0f || float.IsInfinity(player.core.Duration) || float.IsNaN(player.core.Duration) ? -1f : UnityEngine.Mathf.Clamp01(player.core.Time / player.core.Duration); }
        public void _onVideoStart() { _NativeStarted(); }
        public void _onVideoReady() { _NativeStarted(); }
        public void _onVideoEnd() { _NativeEnded(); }
        public void _OnVideoBeginLoad() { _NativeLoading(); }
        public void _OnVideoError() { _NativeError(); }
        public void _OnStop() { _CancelSequence(); }
        public override bool _Available() { return player != null && player.gameObject.activeInHierarchy && player.core != null; }
        public override VRCUrl _CurrentUrl() { return player == null || player.core == null ? VRCUrl.Empty : player.core.Url; }
        public override bool _IsLocalPlayback() { return player != null && player.core != null && !player.core.IsSynced; }
        public override bool _NativeReady() { return player != null && player.core != null && player.core.IsReady; }
        public override bool _AcquireSequenceControl()
        {
            if (VRC.SDK3.UdonNetworkCalling.NetworkCalling.InNetworkCall || !_Available() || !Utilities.IsValid(Networking.LocalPlayer) || player.Locked) return false;
            if (_IsLocalPlayback()) return true;
            Networking.SetOwner(Networking.LocalPlayer, player.core.gameObject);
            return Networking.IsOwner(player.core.gameObject);
        }
        public override int _Request(VRCUrl url, string title)
        {
            if (!_Available() || !Utilities.IsValid(Networking.LocalPlayer)) return 0;
            if (VRCUrl.IsNullOrEmpty(url) || playerIndex == 0 || playerIndex > player.core.PlayerNames.Length) return 3;
            if (player.Locked) return 2;
            player.PlayUrl(url, url, title, playerIndex);
            return 1;
        }
    }
}
