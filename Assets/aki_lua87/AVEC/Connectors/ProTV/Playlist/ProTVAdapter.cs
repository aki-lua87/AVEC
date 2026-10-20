using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.SDK3.UdonNetworkCalling;
using ArchiTech.ProTV;

namespace aki_lua87.CommonVideoPlaylist
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class ProTVAdapter : PlaylistAdapter
    {
        public TVManager player;
        public ProTVSequenceListener eventsListener;
        public override bool _Available() { return player != null && player.gameObject.activeInHierarchy && player.isReady; }
        public override VRCUrl _CurrentUrl() { return player == null ? VRCUrl.Empty : player.urlMain; }
        public override bool _IsLocalPlayback() { return player != null && !player.syncToOwner; }
        public override bool _NativeReady() { return _Available() && !player.IsLoadingMedia && !player.IsStopped; }
        public override bool _IsPlaybackRunning() { return _Available() && player.IsPlaying; }
        public override float _GetVolume() { return _Available() ? player.volume : -1f; }
        public override bool _GetMute() { return _Available() && player.mute; }
        public override int _SetVolume(float value)
        {
            if (NetworkCalling.InNetworkCall || !_Available()) return 0;
            // Do not turn a common UI's local volume control into a synced world change.
            if (player.syncVolumeControl) return 2;
            player.ChangeVolume(Mathf.Clamp01(value)); return 1;
        }
        public override int _SetMute(bool value)
        {
            if (NetworkCalling.InNetworkCall || !_Available()) return 0;
            player.ChangeMute(value); return 1;
        }
        public override bool _AcquireSequenceControl()
        {
            if (NetworkCalling.InNetworkCall || !_Available() || !Utilities.IsValid(Networking.LocalPlayer) || !player.CanPlayMedia) return false;
            if (_IsLocalPlayback()) return true;
            Networking.SetOwner(Networking.LocalPlayer, player.gameObject);
            return Networking.IsOwner(player.gameObject);
        }
        public override int _Request(VRCUrl url, string title)
        {
            if (NetworkCalling.InNetworkCall || !_Available() || !Utilities.IsValid(Networking.LocalPlayer)) return 0;
            if (VRCUrl.IsNullOrEmpty(url)) return 3;
            if (!_AcquireSequenceControl()) return 2;
            player.ChangeMedia(url, url, title, Networking.LocalPlayer.displayName, this);
            return 1;
        }
        public override int _ControlPlayback(bool stop)
        {
            if (NetworkCalling.InNetworkCall || !_Available()) return 0;
            if (!_NativeReady()) return 4;
            if (!_AcquireSequenceControl()) return 2;
            if (stop) player._Pause(); else player._Play();
            return 1;
        }
        public override void _SetNativeMode(int mode)
        {
            if (NetworkCalling.InNetworkCall || !_Available()) return;
            if (_IsLocalPlayback() || Networking.IsOwner(player.gameObject)) player.ChangeLoop(mode == 2);
        }
        public override bool _GetNativeRepeat() { return _Available() && player.loop > 0; }
        public override float _GetProgress()
        {
            if (!_NativeReady() || player.isLive || player.endTime <= player.startTime || float.IsInfinity(player.endTime) || float.IsNaN(player.endTime)) return -1f;
            return Mathf.Clamp01((player.currentTime - player.startTime) / (player.endTime - player.startTime));
        }
    }
}
