using UdonSharp;
using VRC.SDKBase;
using HoshinoLabs.IwaSync3.Udon;

namespace aki_lua87.CommonVideoPlaylist
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class IwaSyncAdapter : PlaylistAdapter
    {
        public VideoCore player;
        public bool streamMode;
        public override float _GetVolume() { return player == null ? -1f : UnityEngine.Mathf.InverseLerp(player.minVolume, player.maxVolume, player.volume); }
        public override int _SetVolume(float value)
        {
            if (VRC.SDK3.UdonNetworkCalling.NetworkCalling.InNetworkCall || !_Available()) return 0;
            player.volume = UnityEngine.Mathf.Lerp(player.minVolume, player.maxVolume, UnityEngine.Mathf.Clamp01(value)); return 1;
        }
        public override bool _IsPlaybackRunning() { return player != null && player.isPlaying && !player.paused; }
        public override bool _GetMute() { return player != null && player.muted; }
        public override int _SetMute(bool value)
        {
            if (VRC.SDK3.UdonNetworkCalling.NetworkCalling.InNetworkCall || !_Available()) return 0;
            player.muted = value;
            return 1;
        }
        public override int _ControlPlayback(bool stop)
        {
            if (VRC.SDK3.UdonNetworkCalling.NetworkCalling.InNetworkCall || !_Available()) return 0;
            if (!stop && !player.isPlaying) return 4;
            if (!_AcquireSequenceControl()) return 2;
            if (!player.isPlaying) return 4;
            if (stop) player.Pause(); else player.Play();
            player.RequestSerialization(); return 1;
        }
        public IwaSyncSequenceListener sequenceListener;
        public override void _SetNativeMode(int mode) { if (player == null) return; player.loop = mode == 2; player.RequestSerialization(); }
        public override bool _GetNativeRepeat() { return player != null && player.loop; }
        public override float _GetProgress() { return player == null || player.duration <= 0f || float.IsInfinity(player.duration) ? -1f : UnityEngine.Mathf.Clamp01(player.time / player.duration); }
        private void Start()
        {
            if (views.Length > 0 && player != null && sequenceListener != null) player.AddListener(sequenceListener);
        }
        public override bool _Available() { return player != null && player.gameObject.activeInHierarchy; }
        public override VRCUrl _CurrentUrl() { return player == null ? VRCUrl.Empty : player.url; }
        public override bool _IsLocalPlayback() { return false; }
        public override bool _NativeReady() { return player != null && player.isPlaying; }
        public override bool _AcquireSequenceControl()
        {
            if (VRC.SDK3.UdonNetworkCalling.NetworkCalling.InNetworkCall || !_Available() || !Utilities.IsValid(Networking.LocalPlayer)) return false;
            if (player.locked && !Networking.IsMaster && !(player.isAllowInstanceOwner && Networking.LocalPlayer.isInstanceOwner)) return false;
            player.TakeOwnership();
            return Networking.IsOwner(player.gameObject);
        }
        public override int _Request(VRCUrl url, string title)
        {
            if (!_Available() || !Utilities.IsValid(Networking.LocalPlayer)) return 0;
            if (VRCUrl.IsNullOrEmpty(url)) return 3;
            if (player.locked && !Networking.IsMaster && !(player.isAllowInstanceOwner && Networking.LocalPlayer.isInstanceOwner)) return 2;
            player.TakeOwnership();
            if (!Networking.IsOwner(player.gameObject)) return 2;
            player.PlayURL(streamMode ? 0x20u : 0x10u, url);
            player.RequestSerialization();
            return 1;
        }
    }
}
