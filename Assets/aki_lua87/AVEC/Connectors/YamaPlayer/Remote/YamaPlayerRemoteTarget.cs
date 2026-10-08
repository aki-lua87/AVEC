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
        public override bool _Ready() { return player != null && player.gameObject.activeInHierarchy && controller != null && controller.Handler != null; }
        public override float _GetVolume() { return controller == null ? 0f : controller.Volume; }
        public override void _SetVolume(float value) { if (!NetworkCalling.InNetworkCall && controller != null) controller.Volume = value; }
        public override bool _GetMute() { return controller != null && controller.Mute; }
        public override void _SetMute(bool value) { if (!NetworkCalling.InNetworkCall && controller != null) controller.Mute = value; }
        public override int _Playback(int action)
        {
            if (NetworkCalling.InNetworkCall || !_Ready() || !Utilities.IsValid(Networking.LocalPlayer)) return 0;
            if (action != 8 && action != 12 && action != 13 && action != 16) return 5;
            if (!player.InvokeBeforeEvent(action == 8 ? "BeforeUserStopVideo" : action == 12 ? "BeforeUserPauseVideo" : "BeforeUserPlayTrack")) return 2;
            controller.TakeOwnership();
            if (!controller.IsLocal && !Networking.IsOwner(controller.gameObject)) return 2;
            if (action == 16) { controller.Repeat = 0; controller.Loop = !controller.Loop; }
            else if (action == 8) controller.Stop();
            else if (action == 12) controller.Pause();
            else controller.Play();
            return 1;
        }
    }
}
