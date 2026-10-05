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
        public override int _ControlPlayback(bool stop)
        {
            if (VRC.SDK3.UdonNetworkCalling.NetworkCalling.InNetworkCall || !_Available() || !Utilities.IsValid(Networking.LocalPlayer)) return 0;
            if (!stop && controller.Stopped) return 4;
            if (!player.InvokeBeforeEvent(stop ? "BeforeUserStopVideo" : "BeforeUserPlayTrack")) return 2;
            controller.TakeOwnership();
            if (!controller.IsLocal && !Networking.IsOwner(controller.gameObject)) return 2;
            if (stop) { _CancelSequence(); controller.Stop(); } else controller.Play();
            return 1;
        }
        public YamaSequenceListener sequenceListener;
        private void Start()
        {
            if (views.Length > 0 && controller != null && sequenceListener != null) controller.AddListener(sequenceListener);
        }
        public override bool _Available() { return player != null && player.gameObject.activeInHierarchy && controller != null && controller.Handler != null; }
        public override VRCUrl _CurrentUrl() { return controller == null ? VRCUrl.Empty : TrackUtils.GetUrl(controller.Track); }
        public override bool _IsLocalPlayback() { return controller != null && controller.IsLocal; }
        public override bool _NativeReady() { return controller != null && controller.IsPlaying; }
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
