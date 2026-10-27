using UdonSharp;
using Yamadev.YamaStream;
using VRC.SDK3.Components.Video;

namespace aki_lua87.CommonVideoPlaylist
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class YamaSequenceListener : YamaPlayerListener
    {
        public YamaPlayerAdapter adapter;
        public override void AfterVideoPaused() { if (adapter != null) adapter._NotifyLocalStateChanged(); }
        public override void AfterTimeChanged(float time) { if (adapter != null && !adapter.localPlaying) adapter._NotifyLocalStateChanged(); }
        public override void AfterVideoStarted() { if (adapter != null) adapter._NativeStarted(); }
        public override void AfterVideoReady() { if (adapter != null) adapter._NativeStarted(); }
        public override void AfterVideoPlayed() { if (adapter != null) adapter._NativeStarted(); }
        public override void AfterVideoEnded() { if (adapter != null) adapter._NativeEnded(); }
        public override void AfterTrackLoaded() { if (adapter != null) adapter._NativeLoading(); }
        public override void AfterVideoStopped() { if (adapter != null) adapter._NativeStopped(); }
        public override void AfterVideoErrorOccurred(VideoError error) { if (adapter != null) adapter._NativeError(); }
    }
}
