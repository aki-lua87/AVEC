using UdonSharp;
using HoshinoLabs.IwaSync3.Udon;
using VRC.SDK3.Components.Video;

namespace aki_lua87.CommonVideoPlaylist
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class IwaSyncSequenceListener : VideoCoreEventListener
    {
        public IwaSyncAdapter adapter;
        public override void OnPlayerPause() { if (adapter != null) adapter._NotifyLocalStateChanged(); }
        public override void OnPlayerPlay() { if (adapter != null) adapter._NotifyLocalStateChanged(); }
        public override void OnVideoStart() { if (adapter != null) adapter._NativeStarted(); }
        public override void OnVideoReady() { if (adapter != null) adapter._NativeStarted(); }
        public override void OnVideoEnd() { if (adapter != null) adapter._NativeEnded(); }
        public override void OnChangeURL() { if (adapter != null) adapter._NativeLoading(); }
        public override void OnPlayerStop() { if (adapter != null) adapter._CancelSequence(); }
        public override void OnVideoError(VideoError error) { if (adapter != null) adapter._NativeError(); }
    }
}
