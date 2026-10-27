using UdonSharp;
using ArchiTech.ProTV;

namespace aki_lua87.CommonVideoPlaylist
{
    // TVPlugin resolves the parent TV and registers through ProTV's native event API.
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class ProTVSequenceListener : TVPlugin
    {
        public ProTVAdapter adapter;
        public override void _TvPause() { if (adapter != null) adapter._NotifyLocalStateChanged(); }
        public override void _TvSeekChange() { if (adapter != null) adapter._NotifyLocalStateChanged(); }
        public override void _TvMediaReady() { if (adapter != null) adapter._NativeStarted(); }
        public override void _TvPlay() { if (adapter != null) adapter._NativeStarted(); }
        public override void _TvMediaEnd() { if (adapter != null) adapter._NativeEnded(); }
        public override void _TvLoading() { if (adapter != null) adapter._NativeLoading(); }
        public override void _TvStop() { if (adapter != null) adapter._NativeStopped(); }
        public override void _TvVideoPlayerError() { if (adapter != null) adapter._NativeError(); }
    }
}
