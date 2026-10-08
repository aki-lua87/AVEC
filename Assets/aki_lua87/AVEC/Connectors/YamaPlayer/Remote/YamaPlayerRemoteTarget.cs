using UdonSharp;
using VRC.SDKBase;
using VRC.SDK3.UdonNetworkCalling;
using Yamadev.YamaStream;
using Yamadev.YamaStream.UI;

namespace aki_lua87.CommonGestureRemote
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class YamaPlayerRemoteTarget : RemoteTarget
    {
        public UIController player;
        public Controller controller;
        // Local audio does not depend on an open native UI or a loaded video handler.
        public override bool _Ready() { return controller != null && controller.gameObject.activeInHierarchy; }
        public override float _GetVolume() { return controller == null ? 0f : controller.Volume; }
        public override void _SetVolume(float value) { if (!NetworkCalling.InNetworkCall && controller != null) controller.Volume = value; }
        public override bool _GetMute() { return controller != null && controller.Mute; }
        public override void _SetMute(bool value) { if (!NetworkCalling.InNetworkCall && controller != null) controller.Mute = value; }
        private bool pendingVolume;
        private bool pendingMute;
        private float desiredVolume;
        private bool desiredMute;
        private bool audioRetryScheduled;
        public override int _Execute(int action, float amount)
        {
            if (NetworkCalling.InNetworkCall || !_Ready()) return 0;
            if (action != 3 && action != 4 && action != 14) return _Playback(action);
            if (player != null && !player.InvokeBeforeEvent(action == 14 ? "BeforeUserChangeMute" : "BeforeUserChangeVolume")) return 2;
            // Capture the requested absolute value once. Reapplying after native Start
            // cannot toggle mute twice or accumulate an extra volume step.
            if (action == 14) { desiredMute = pendingMute ? !desiredMute : !controller.Mute; pendingMute = true; controller.Mute = desiredMute; }
            else { desiredVolume = UnityEngine.Mathf.Clamp01((pendingVolume ? desiredVolume : controller.Volume) + (action == 3 ? amount : -amount)); pendingVolume = true; controller.Volume = desiredVolume; }
            if (!audioRetryScheduled) { audioRetryScheduled = true; SendCustomEventDelayedFrames(nameof(_FinishLocalAudio), 1); }
            return 1;
        }
        public void _FinishLocalAudio()
        {
            if (NetworkCalling.InNetworkCall) return;
            audioRetryScheduled = false;
            if (_Ready()) { if (pendingVolume && controller.Volume != desiredVolume) controller.Volume = desiredVolume; if (pendingMute && controller.Mute != desiredMute) controller.Mute = desiredMute; }
            pendingVolume = false; pendingMute = false;
        }
        public override int _Playback(int action)
        {
            if (NetworkCalling.InNetworkCall || !_Ready() || player == null || controller.Handler == null || !Utilities.IsValid(Networking.LocalPlayer)) return 0;
            if (action == 21) action = controller.IsPlaying ? 12 : 13;
            if (action != 8 && action != 12 && action != 13 && action != 16 && action != 17 && action != 19) return 5;
            if ((action == 12 || action == 13 || action == 17) && (controller.Stopped || controller.IsLoading)) return 4;
            if (action == 17 && (controller.IsLive || controller.Duration <= 0)) return 4;
            if (!player.InvokeBeforeEvent(action == 8 ? "BeforeUserStopVideo" : action == 12 ? "BeforeUserPauseVideo" : action == 17 ? "BeforeUserSetTime" : "BeforeUserPlayTrack")) return 2;
            if (action == 17 && !player.InvokeBeforeEvent("BeforeUserPlayTrack")) return 2;
            controller.TakeOwnership();
            if (!controller.IsLocal && !Networking.IsOwner(controller.gameObject)) return 2;
            if (action == 16 || action == 19) { controller.Repeat = 0; controller.Loop = action == 19 || !controller.Loop; }
            else if (action == 17) { controller.SetTime(0f); controller.Play(); }
            else if (action == 8) controller.Stop();
            else if (action == 12) controller.Pause();
            else controller.Play();
            return 1;
        }
    }
}
