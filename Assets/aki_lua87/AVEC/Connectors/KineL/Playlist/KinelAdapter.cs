using UdonSharp;
using VRC.SDKBase;
using Kinel.VideoPlayer.Udon;
using VRC.SDK3.UdonNetworkCalling;

namespace aki_lua87.CommonVideoPlaylist
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class KinelAdapter : PlaylistAdapter
    {
        public KinelVideoPlayer player;
        // Editor-detected native volume UI targets, persisted for Udon.
        public UnityEngine.AudioSource[] audioSources = new UnityEngine.AudioSource[0];
        public UnityEngine.UI.Slider[] volumeSliders = new UnityEngine.UI.Slider[0];
        public Kinel.VideoPlayer.Udon.Module.AudioModule[] muteModules = new Kinel.VideoPlayer.Udon.Module.AudioModule[0];
        public override float _GetVolume() { return audioSources.Length == 0 || audioSources[0] == null ? -1f : audioSources[0].volume; }
        public override int _SetVolume(float value)
        {
            if (NetworkCalling.InNetworkCall || !_Available() || audioSources.Length == 0) return 0;
            value = UnityEngine.Mathf.Clamp01(value);
            for (int i = 0; i < audioSources.Length; i++) if (audioSources[i] != null) audioSources[i].volume = value;
            for (int i = 0; i < volumeSliders.Length; i++) if (volumeSliders[i] != null) volumeSliders[i].SetValueWithoutNotify(value);
            return 1;
        }
        public override bool _IsPlaybackRunning() { return player != null && player.IsPlaying && !player.IsPause; }
        public override bool _GetMute() { return audioSources.Length > 0 && audioSources[0] != null && audioSources[0].mute; }
        public override int _SetMute(bool value)
        {
            if (VRC.SDK3.UdonNetworkCalling.NetworkCalling.InNetworkCall || !_Available()) return 0;
            if (audioSources.Length == 0) return 0;
            for (int i = 0; i < muteModules.Length; i++)
            {
                var module = muteModules[i]; if (module == null) continue;
                object current = module.GetProgramVariable("mute");
                if (current != null && (bool)current != value) module.ToggleMute();
            }
            for (int i = 0; i < audioSources.Length; i++) if (audioSources[i] != null) audioSources[i].mute = value;
            return 1;
        }
        public override int _ControlPlayback(bool stop)
        {
            if (NetworkCalling.InNetworkCall || !_Available()) return 0;
            if (!stop && !player.IsPlaying && !player.IsPause) return 4;
            if (!_AcquireSequenceControl()) return 2;
            if (!player.IsPlaying && !player.IsPause) return 4;
            if (stop) player.Pause(); else player.Play();
            return 1;
        }
        private void Start() { if (views.Length > 0 && player != null) player.RegisterListener(this); }
        public override void _SetNativeMode(int mode) { if (player == null) return; player.Loop = mode == 2; player.RequestSerialization(); }
        public override bool _GetNativeRepeat() { return player != null && player.Loop; }
        public override float _GetProgress()
        {
            if (!_Available()) return -1f;
            var video = player.GetVideoPlayerController().GetCurrentVideoPlayer();
            if (video == null) return -1f;
            float duration = video.GetDuration();
            return duration <= 0f || float.IsInfinity(duration) || float.IsNaN(duration) ? -1f : UnityEngine.Mathf.Clamp01(video.GetTime() / duration);
        }
        public void OnKinelVideoStart() { if (!NetworkCalling.InNetworkCall) _NativeStarted(); }
        public void OnKinelVideoReady() { if (!NetworkCalling.InNetworkCall) _NativeStarted(); }
        public void OnKinelVideoEnd() { if (!NetworkCalling.InNetworkCall) _NativeEnded(); }
        public void OnKinelUrlUpdate() { if (!NetworkCalling.InNetworkCall) _NativeLoading(); }
        public void OnKinelVideoReset() { if (!NetworkCalling.InNetworkCall) _NativeStopped(); }
        public void OnKinelVideoError() { if (!NetworkCalling.InNetworkCall) _NativeError(); }
        public override bool _Available() { return player != null && player.gameObject.activeInHierarchy && player.GetVideoPlayerController() != null; }
        public override VRCUrl _CurrentUrl() { return player == null ? VRCUrl.Empty : player.GetUrl(); }
        public override bool _IsLocalPlayback() { return false; }
        public override bool _NativeReady() { return player != null && (player.IsPlaying || player.IsPause); }
        public override bool _AcquireSequenceControl()
        {
            if (NetworkCalling.InNetworkCall || !_Available() || !Utilities.IsValid(Networking.LocalPlayer)) return false;
            if (player.IsLock && !Networking.IsMaster) return false;
            player.TakeOwnership();
            return Networking.IsOwner(player.gameObject);
        }
        public override int _Request(VRCUrl url, string title)
        {
            if (!_Available() || !Utilities.IsValid(Networking.LocalPlayer)) return 0;
            if (VRCUrl.IsNullOrEmpty(url)) return 3;
            if (player.IsLock && !Networking.IsMaster) return 2;
            player.TakeOwnership();
            if (!Networking.IsOwner(player.gameObject)) return 2;
            player.PlayByURL(url);
            return 1;
        }
    }
}
