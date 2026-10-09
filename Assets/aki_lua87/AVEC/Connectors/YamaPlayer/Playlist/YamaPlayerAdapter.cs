using UdonSharp;
using VRC.SDKBase;
using Yamadev.YamaStream;
using Yamadev.YamaStream.UI;

namespace aki_lua87.CommonVideoPlaylist
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class YamaPlayerAdapter : PlaylistAdapter
    {
        public UIController player;
        public Controller controller;
        public override float _GetVolume() { return controller == null ? -1f : controller.Volume; }
        public override int _SetVolume(float value)
        {
            if (VRC.SDK3.UdonNetworkCalling.NetworkCalling.InNetworkCall || !_Available()) return 0;
            controller.Volume = UnityEngine.Mathf.Clamp01(value); return 1;
        }
        public override bool _IsPlaybackRunning() { return controller != null && controller.Handler != null && controller.IsPlaying; }
        public override bool _GetMute() { return controller != null && controller.Mute; }
        public override int _SetMute(bool value)
        {
            if (VRC.SDK3.UdonNetworkCalling.NetworkCalling.InNetworkCall || !_Available()) return 0;
            if (!player.InvokeBeforeEvent("BeforeUserChangeMute")) return 2; controller.Mute = value;
            return 1;
        }
        public override int _ControlPlayback(bool stop)
        {
            if (VRC.SDK3.UdonNetworkCalling.NetworkCalling.InNetworkCall || !_Available() || !Utilities.IsValid(Networking.LocalPlayer)) return 0;
            if (!stop && controller.Stopped) return 4;
            if (controller.Stopped || controller.IsLoading) return 4;
            if (!player.InvokeBeforeEvent(stop ? "BeforeUserPauseVideo" : "BeforeUserPlayTrack")) return 2;
            controller.TakeOwnership();
            if (!controller.IsLocal && !Networking.IsOwner(controller.gameObject)) return 2;
            if (stop) controller.Pause(); else controller.Play();
            return 1;
        }
        public YamaSequenceListener sequenceListener;
        public override void _SetNativeMode(int mode) { if (controller == null) return; controller.Repeat = 0; controller.Loop = mode == 2; controller.ShufflePlay = mode == 1; }
        public override bool _GetNativeRepeat() { return controller != null && controller.Loop; }
        public override float _GetProgress() { return !_Available() || controller.Duration <= 0f || float.IsInfinity(controller.Duration) || float.IsNaN(controller.Duration) ? -1f : UnityEngine.Mathf.Clamp01(controller.VideoTime / controller.Duration); }
        private void Start()
        {
            if (views.Length > 0 && controller != null && sequenceListener != null) controller.AddListener(sequenceListener);
        }
        public override bool _Available() { return player != null && player.gameObject.activeInHierarchy && controller != null && controller.Handler != null; }
        public override VRCUrl _CurrentUrl() { return controller == null ? VRCUrl.Empty : TrackUtils.GetUrl(controller.Track); }
        public override bool _IsLocalPlayback() { return controller != null && controller.IsLocal; }
        public override bool _NativeReady() { return controller != null && (controller.IsPlaying || controller.Paused); }
        public override bool _AcquireSequenceControl()
        {
            if (VRC.SDK3.UdonNetworkCalling.NetworkCalling.InNetworkCall || !_Available() || !Utilities.IsValid(Networking.LocalPlayer)) return false;
            if (!player.InvokeBeforeEvent("BeforeUserPlayTrack")) return false;
            controller.TakeOwnership();
            return controller.IsLocal || Networking.IsOwner(controller.gameObject);
        }
        public override int _Request(VRCUrl url, string title)
        {
            if (!_Available() || !Utilities.IsValid(Networking.LocalPlayer)) return 0;
            if (VRCUrl.IsNullOrEmpty(url)) return 3;
            if (!player.InvokeBeforeEvent("BeforeUserPlayTrack")) return 2;
            controller.TakeOwnership();
            if (!controller.IsLocal && !Networking.IsOwner(controller.gameObject)) return 2;
            controller.PlayTrack(TrackUtils.NewTrack(controller.Handler.Type, title, url));
            return 1;
        }
    }
}
