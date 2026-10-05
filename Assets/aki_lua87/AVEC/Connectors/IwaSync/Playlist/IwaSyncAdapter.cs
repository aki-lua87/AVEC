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
        public override float _GetVolume() { return player == null ? -1f : player.volume; }
        public override int _SetVolume(float value)
        {
            if (VRC.SDK3.UdonNetworkCalling.NetworkCalling.InNetworkCall || !_Available()) return 0;
            player.volume = UnityEngine.Mathf.Clamp01(value); return 1;
        }
        public override int _ControlPlayback(bool stop)
        {
            if (VRC.SDK3.UdonNetworkCalling.NetworkCalling.InNetworkCall || !_Available()) return 0;
            if (!stop && !player.isPlaying) return 4;
            if (!_AcquireSequenceControl()) return 2;
            if (stop) { _CancelSequence(); player.Stop(); } else player.Play();
            player.RequestSerialization(); return 1;
        }
        public IwaSyncSequenceListener sequenceListener;
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
