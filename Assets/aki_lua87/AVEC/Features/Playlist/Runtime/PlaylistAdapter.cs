using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.SDK3.UdonNetworkCalling;

namespace aki_lua87.CommonVideoPlaylist
{
    // One adapter per native player, on the SAME GameObject as its playback authority.
    // Only the sequence cursor and queue are synced here; playback remains owned by the native player.
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public abstract class PlaylistAdapter : UdonSharpBehaviour
    {
        // Persisted editor wiring; all UIs for this player share one cursor.
        [HideInInspector] public PlaylistView[] views = new PlaylistView[0];
        [UdonSynced] private int syncedView = -1;
        [UdonSynced] private int syncedVideo = -1;
        [UdonSynced] private int syncedRevision;
        [UdonSynced] private int syncedRandomSeed;
        [UdonSynced] private int syncedSequenceStart = -1;
        [UdonSynced] private int syncedModeOverride = -1;
        private int modeOverride = -1; // -1: authored default; 0: ordered; 1: random
        private int randomSeed;
        private int sequenceStart = -1;
        private int cursorView = -1;
        private int cursorVideo = -1;
        private int revision;
        private bool armed;
        private bool requesting;
        private bool endPending;
        private int pendingRevision;
        private int stopRevision;
        private bool stopDuringEnd;
        private float lastLoadTime = -10f;
        // FIFO entries refer to the editor-baked views, avoiding URL/title network payloads.
        [UdonSynced] private int[] syncedQueueViews = new int[0];
        [UdonSynced] private int[] syncedQueueVideos = new int[0];
        [UdonSynced] private bool syncedQueueOnly;
        private int[] queueViews = new int[0];
        private int[] queueVideos = new int[0];
        private bool queueOnly;
        [UdonSynced] private VRCUrl[] syncedQueueUrls = new VRCUrl[0];
        [UdonSynced] private VRCUrl syncedQueuePlayingUrl = VRCUrl.Empty;
        private VRCUrl[] queueUrls = new VRCUrl[0];
        private VRCUrl queuePlayingUrl = VRCUrl.Empty;
        private VRCUrl endedUrl;
        private float endedAt;

        // 0 unavailable, 1 submitted (possibly queued), 2 permission denied, 3 invalid URL.
        public abstract int _Request(VRCUrl url, string title);
        public abstract bool _Available();
        public abstract VRCUrl _CurrentUrl();
        public abstract bool _IsLocalPlayback();
        public abstract bool _NativeReady();
        public abstract bool _AcquireSequenceControl();
        // Optional native controls. Volume is local; playback uses native permissions.
        public virtual float _GetVolume() { return -1f; }
        public virtual int _SetVolume(float value) { return 0; }
        public virtual int _ControlPlayback(bool stop) { return 0; }
        public int _QueueCount() { return queueViews.Length; }
        public string _QueueTitle(int index)
        {
            if (index < 0 || index >= queueViews.Length || index >= queueVideos.Length) return "";
            int source = queueViews[index];
            if (source == -1 && index < queueUrls.Length) return queueUrls[index].Get();
            if (source < 0 || source >= views.Length || !ValidEntry(views[source], queueVideos[index])) return "無効な動画";
            return views[source].titles[queueVideos[index]];
        }
        public int _EnqueueFrom(PlaylistView view, int index)
        {
            if (NetworkCalling.InNetworkCall || requesting || !_Available()) return 0;
            int source = System.Array.IndexOf(views, view);
            if (source < 0 || !ValidEntry(view, index)) return 3;
            if (queueViews.Length >= 32) return 5;
            if (!_AcquireSequenceControl() || !CanWrite()) return 2;
            int length = queueViews.Length;
            var sources = new int[length + 1]; var videos = new int[length + 1];
            System.Array.Copy(queueViews, sources, length); System.Array.Copy(queueVideos, videos, length);
            sources[length] = source; videos[length] = index;
            queueViews = sources; queueVideos = videos;
            var inputs = new VRCUrl[length + 1];
            System.Array.Copy(queueUrls, inputs, length); inputs[length] = VRCUrl.Empty; queueUrls = inputs;
            CommitCursor();
            if (VRCUrl.IsNullOrEmpty(_CurrentUrl()) && !_NativeReady())
                SendCustomEventDelayedSeconds(nameof(_StartQueue), Mathf.Max(.1f, 5.1f - (Time.realtimeSinceStartup - lastLoadTime)));
            return 1;
        }
        public int _EnqueueUrl(VRCUrl url)
        {
            if (NetworkCalling.InNetworkCall || requesting || !_Available()) return 0;
            if (VRCUrl.IsNullOrEmpty(url)) return 3;
            string raw = url.Get();
            if ((!raw.StartsWith("https://") && !raw.StartsWith("http://")) || raw.Length > 2048) return 3;
            if (queueViews.Length >= 32) return 5;
            if (!_AcquireSequenceControl() || !CanWrite()) return 2;
            int length = queueViews.Length;
            var sources = new int[length + 1]; var videos = new int[length + 1]; var inputs = new VRCUrl[length + 1];
            System.Array.Copy(queueViews, sources, length); System.Array.Copy(queueVideos, videos, length); System.Array.Copy(queueUrls, inputs, length);
            sources[length] = -1; videos[length] = -1; inputs[length] = url;
            queueViews = sources; queueVideos = videos; queueUrls = inputs;
            CommitCursor();
            if (VRCUrl.IsNullOrEmpty(_CurrentUrl()) && !_NativeReady())
                SendCustomEventDelayedSeconds(nameof(_StartQueue), Mathf.Max(.1f, 5.1f - (Time.realtimeSinceStartup - lastLoadTime)));
            return 1;
        }
        public int _RemoveQueued(int index)
        {
            if (NetworkCalling.InNetworkCall || requesting || !_Available()) return 0;
            if (index < 0 || index >= queueViews.Length) return 3;
            if (!_AcquireSequenceControl() || !CanWrite()) return 2;
            RemoveQueued(index); CommitCursor(); return 1;
        }
        public int _ClearQueue()
        {
            if (NetworkCalling.InNetworkCall || requesting || !_Available()) return 0;
            if (!_AcquireSequenceControl() || !CanWrite()) return 2;
            queueViews = new int[0]; queueVideos = new int[0]; queueUrls = new VRCUrl[0]; CommitCursor(); return 1;
        }
        private void RemoveQueued(int index)
        {
            int length = queueViews.Length - 1;
            var sources = new int[length]; var videos = new int[length];
            var inputs = new VRCUrl[length];
            for (int from = 0, to = 0; from < queueViews.Length; from++)
            {
                if (from == index) continue;
                sources[to] = queueViews[from]; videos[to] = queueVideos[from]; inputs[to] = queueUrls[from]; to++;
            }
            queueViews = sources; queueVideos = videos; queueUrls = inputs;
        }
        private int PlayQueued()
        {
            if (queueViews.Length == 0 || queueVideos.Length != queueViews.Length) return 4;
            int source = queueViews[0];
            if (source == -1)
            {
                VRCUrl url = queueUrls[0];
                requesting = true;
                int submitted = _Request(url, url.Get());
                requesting = false;
                if (submitted != 1) return submitted;
                queuePlayingUrl = url; queueOnly = true;
                cursorView = -1; cursorVideo = -1; revision++; endPending = false; armed = false;
                RemoveQueued(0); CommitCursor(); return 1;
            }
            if (source < 0 || source >= views.Length) return 3;
            int result = PlayEntry(views[source], queueVideos[0], false);
            if (result == 1) { RemoveQueued(0); queueOnly = true; CommitCursor(); }
            return result;
        }
        public void _StartQueue()
        {
            if (NetworkCalling.InNetworkCall || requesting || !_Available() || !CanWrite() || queueViews.Length == 0) return;
            // Do not interrupt a native track which began after the queue was added.
            if (!VRCUrl.IsNullOrEmpty(_CurrentUrl()) || _NativeReady()) return;
            PlayQueued();
        }
        public int _PlayQueueNext()
        {
            if (NetworkCalling.InNetworkCall || requesting || !_Available()) return 0;
            if (Time.realtimeSinceStartup - lastLoadTime < 5.1f) return 7;
            if (!_AcquireSequenceControl() || !CanWrite()) return 2;
            return PlayQueued();
        }

        public bool _IsRandomMode(PlaylistView view, int tab)
        {
            if (modeOverride >= 0) return modeOverride == 1;
            if (ActiveView() != null) return randomSeed != 0;
            return view != null && view.randomTabs != null && tab >= 0 && tab < view.randomTabs.Length && view.randomTabs[tab];
        }
        public int _TogglePlaybackMode(PlaylistView view, int tab)
        {
            if (NetworkCalling.InNetworkCall || requesting || !_Available()) return 0;
            if (System.Array.IndexOf(views, view) < 0) return 0;
            bool random = !_IsRandomMode(view, tab);
            if (!_AcquireSequenceControl() || !CanWrite()) return 2;
            modeOverride = random ? 1 : 0;
            if (ValidEntry(ActiveView(), cursorVideo))
            {
                sequenceStart = cursorVideo;
                randomSeed = random ? Random.Range(1, int.MaxValue) : 0;
            }
            // Preserve the current load and a pending natural End; only its successor changes.
            CommitCursor();
            return 1;
        }

        public int _PlayFrom(PlaylistView view, int index)
        {
            if (NetworkCalling.InNetworkCall) return 0;
            return PlayEntry(view, index, true);
        }

        // Manual remote navigation shares the native-player cursor; no second cursor is synced.
        // Random navigation follows the same stable seeded order in either direction.
        public int _StepVideo(PlaylistView fallback, int direction)
        {
            if (NetworkCalling.InNetworkCall || requesting || !_Available()) return 0;
            if (fallback == null || System.Array.IndexOf(views, fallback) < 0) return 3;
            if (Time.realtimeSinceStartup - lastLoadTime < 5.1f) return 7;
            var view = ActiveView();
            int current = cursorVideo;
            if (!ValidEntry(view, current) || !MatchesCurrent())
            {
                view = fallback;
                int selectedTab = fallback._SelectedTab();
                for (int i = 0; i < view.urls.Length; i++)
                    if (ValidEntry(view, i) && view.videoTabs[i] == selectedTab) return PlayEntry(view, i, true);
                return 4;
            }
            int next = NavigationIndex(view, current, direction);
            if (next < 0) return 4;
            // Preserve the current random seed but make a manual selection the new pass origin.
            int result = PlayEntry(view, next, false);
            if (result == 1) { sequenceStart = next; CommitCursor(); }
            return result;
        }
        private int NavigationIndex(PlaylistView view, int current, int direction)
        {
            int tab = view.videoTabs[current];
            int next = -1;
            int rank = randomSeed == 0 ? current : RandomRank(current);
            int nextRank = direction < 0 ? int.MinValue : int.MaxValue;
            for (int i = 0; i < view.urls.Length; i++)
            {
                if (i == current || !ValidEntry(view, i) || view.videoTabs[i] != tab) continue;
                int candidate = randomSeed == 0 ? i : RandomRank(i);
                bool after = candidate > rank || (candidate == rank && i > current);
                if (direction < 0 ? after : !after) continue;
                if (next < 0 || (direction < 0 ? candidate > nextRank || (candidate == nextRank && i > next) : candidate < nextRank || (candidate == nextRank && i < next)))
                { next = i; nextRank = candidate; }
            }
            return next;
        }

        private int PlayEntry(PlaylistView view, int index, bool beginSequence)
        {
            if (NetworkCalling.InNetworkCall || view == null || !_Available()) return 0;
            int source = System.Array.IndexOf(views, view);
            if (source < 0 || !ValidEntry(view, index)) return 3;
            requesting = true;
            int result = _Request(view.urls[index], view.titles[index]);
            requesting = false;
            if (result != 1) return result;
            // Queue APIs can transfer a frontend without transferring its playback core.
            if (!_IsLocalPlayback() && !Networking.IsOwner(gameObject))
                Networking.SetOwner(Networking.LocalPlayer, gameObject);
            if (!CanWrite()) return 2;
            if (beginSequence)
            {
                queueOnly = false;
                sequenceStart = index;
                int tab = view.videoTabs[index];
                bool random = modeOverride >= 0 ? modeOverride == 1 : view.randomTabs != null && tab >= 0 && tab < view.randomTabs.Length && view.randomTabs[tab];
                randomSeed = random
                    ? Random.Range(1, int.MaxValue) : 0;
            }
            cursorView = source; cursorVideo = index; revision++;
            queuePlayingUrl = VRCUrl.Empty;
            endPending = false; armed = false;
            CommitCursor();
            // Wait for the actual native Ready/Start callback, even if a queued URL
            // happens to equal the URL which is playing right now.
            return 1;
        }

        private bool CanWrite() { return _IsLocalPlayback() || Networking.IsOwner(gameObject); }
        private bool ValidEntry(PlaylistView view, int index)
        {
            if (view == null || index < 0 || index >= view.urls.Length || index >= view.titles.Length || index >= view.videoTabs.Length) return false;
            if (VRCUrl.IsNullOrEmpty(view.urls[index])) return false;
            string raw = view.urls[index].Get();
            return raw.StartsWith("https://") || raw.StartsWith("http://");
        }
        private PlaylistView ActiveView()
        {
            return cursorView >= 0 && cursorView < views.Length ? views[cursorView] : null;
        }
        private bool MatchesCurrent()
        {
            var view = ActiveView();
            VRCUrl current = _CurrentUrl();
            if (queueOnly && !VRCUrl.IsNullOrEmpty(queuePlayingUrl)) return !VRCUrl.IsNullOrEmpty(current) && current.Get() == queuePlayingUrl.Get();
            return ValidEntry(view, cursorVideo) && !VRCUrl.IsNullOrEmpty(current) && current.Get() == view.urls[cursorVideo].Get();
        }
        private void CommitCursor()
        {
            if (!CanWrite()) return;
            if (!_IsLocalPlayback())
            {
                syncedView = cursorView; syncedVideo = cursorVideo; syncedRevision = revision;
                syncedRandomSeed = randomSeed; syncedSequenceStart = sequenceStart;
                syncedModeOverride = modeOverride;
                syncedQueueViews = (int[])queueViews.Clone(); syncedQueueVideos = (int[])queueVideos.Clone();
                syncedQueueOnly = queueOnly;
                syncedQueueUrls = (VRCUrl[])queueUrls.Clone(); syncedQueuePlayingUrl = queuePlayingUrl;
                RequestSerialization();
            }
            ShowCursor();
        }
        private void ShowCursor()
        {
            var view = ActiveView();
            if (view != null) view._ShowSequenceSelection(cursorVideo);
            for (int i = 0; i < views.Length; i++) if (views[i] != null) { views[i]._RefreshPlaybackMode(); views[i]._RefreshQueue(); }
        }
        public override void OnDeserialization()
        {
            if (_IsLocalPlayback()) return;
            bool changed = revision != syncedRevision || cursorView != syncedView || cursorVideo != syncedVideo;
            cursorView = syncedView; cursorVideo = syncedVideo; revision = syncedRevision;
            randomSeed = syncedRandomSeed; sequenceStart = syncedSequenceStart;
            modeOverride = syncedModeOverride;
            // Synced arrays must be applied even when the playback cursor did not change.
            queueViews = syncedQueueViews == null ? new int[0] : (int[])syncedQueueViews.Clone();
            queueVideos = syncedQueueVideos == null ? new int[0] : (int[])syncedQueueVideos.Clone();
            queueOnly = syncedQueueOnly;
            queueUrls = syncedQueueUrls == null ? new VRCUrl[0] : (VRCUrl[])syncedQueueUrls.Clone();
            queuePlayingUrl = syncedQueuePlayingUrl;
            if (queueViews.Length != queueVideos.Length || queueViews.Length != queueUrls.Length) {
                queueViews = new int[0]; queueVideos = new int[0]; queueUrls = new VRCUrl[0];
            }
            if (changed) { endPending = false; armed = false; }
            // Cursor and native URL can arrive in either order, including for late joiners.
            if (_Available() && _NativeReady() && MatchesCurrent()) armed = true;
            ShowCursor();
        }

        public void _NativeLoading()
        {
            if (NetworkCalling.InNetworkCall) return;
            lastLoadTime = Time.realtimeSinceStartup;
        }
        public void _NativeStarted()
        {
            if (NetworkCalling.InNetworkCall || requesting || !_Available() || endPending) return;
            if (MatchesCurrent()) { if (_NativeReady()) armed = true; }
            else if (armed) _CancelSequence();
        }
        public void _NativeEnded()
        {
            if (NetworkCalling.InNetworkCall || requesting || (!armed && queueViews.Length == 0)) return;
            bool alreadyPending = endPending;
            endedUrl = _CurrentUrl(); endedAt = Time.realtimeSinceStartup;
            armed = false; endPending = true; pendingRevision = revision;
            // Let the player's own end/queue handlers finish before requesting another URL.
            if (!alreadyPending) SendCustomEventDelayedSeconds(nameof(_AdvanceSequence), Mathf.Max(.2f, 5.1f - (Time.realtimeSinceStartup - lastLoadTime)));
        }
        public void _AdvanceSequence()
        {
            if (NetworkCalling.InNetworkCall || !endPending || pendingRevision != revision) return;
            if (!_Available() || !CanWrite()) { endPending = false; return; }
            float remaining = 5.1f - (Time.realtimeSinceStartup - lastLoadTime);
            if (remaining > 0) { SendCustomEventDelayedSeconds(nameof(_AdvanceSequence), remaining); return; }
            endPending = false;
            // A native queue may start another track before our delayed callback.
            // Let it finish instead of submitting a competing request.
            var current = _CurrentUrl();
            if (lastLoadTime > endedAt || (!VRCUrl.IsNullOrEmpty(current) && !VRCUrl.IsNullOrEmpty(endedUrl) && current.Get() != endedUrl.Get())) return;
            if (queueViews.Length > 0)
            {
                int queuedResult = PlayQueued();
                if (queuedResult != 1) for (int i = 0; i < views.Length; i++) if (views[i] != null) views[i]._SequenceStatus("キューを再生できません。接続先・権限を確認してください。");
                return;
            }
            if (queueOnly) { _CancelSequence(); return; }
            var view = ActiveView();
            if (!ValidEntry(view, cursorVideo)) { _CancelSequence(); return; }
            int next = randomSeed == 0 ? view._NextVideoInTab(cursorVideo) : NextRandomVideo(view);
            if (next < 0)
            {
                view._SequenceStatus("Playlist finished / このタブの再生が終了しました");
                _CancelSequence();
                return;
            }
            int result = PlayEntry(view, next, false);
            if (result != 1)
            {
                view._SequenceStatus("次の動画を再生できませんでした。接続先・権限・URLを確認してください。");
                _CancelSequence();
            }
        }
        // Seeded ranks define a stable circular permutation without syncing a large array.
        // Index breaks rank ties. Returning to the manually selected start ends the pass.
        private int RandomRank(int index)
        {
            int value = index ^ randomSeed;
            value = ((value >> 16) ^ value) * 0x45d9f3b;
            value = ((value >> 16) ^ value) * 0x45d9f3b;
            return ((value >> 16) ^ value) & int.MaxValue;
        }
        private int NextRandomVideo(PlaylistView view)
        {
            int currentRank = RandomRank(cursorVideo);
            int first = -1, firstRank = int.MaxValue;
            int next = -1, nextRank = int.MaxValue;
            int tab = view.videoTabs[cursorVideo];
            for (int i = 0; i < view.urls.Length; i++)
            {
                if (!ValidEntry(view, i) || view.videoTabs[i] != tab) continue;
                int rank = RandomRank(i);
                if (first < 0 || rank < firstRank || (rank == firstRank && i < first)) { first = i; firstRank = rank; }
                if ((rank > currentRank || (rank == currentRank && i > cursorVideo)) &&
                    (next < 0 || rank < nextRank || (rank == nextRank && i < next))) { next = i; nextRank = rank; }
            }
            if (next < 0) next = first;
            return next == sequenceStart ? -1 : next;
        }
        public void _NativeStopped()
        {
            if (NetworkCalling.InNetworkCall || requesting) return;
            stopRevision = revision; stopDuringEnd = endPending;
            SendCustomEventDelayedFrames(nameof(_CheckSequenceStop), 1);
        }
        public void _CheckSequenceStop()
        {
            if (NetworkCalling.InNetworkCall || stopRevision != revision || requesting || !_Available()) return;
            // YamaPlayer emits Stop immediately BEFORE its natural End event.
            if ((!endPending || stopDuringEnd) && VRCUrl.IsNullOrEmpty(_CurrentUrl())) _CancelSequence();
        }
        public void _NativeError()
        {
            if (NetworkCalling.InNetworkCall) return;
            armed = false; endPending = false; // Native retry may arm this entry again.
        }
        public void _CancelSequence()
        {
            if (NetworkCalling.InNetworkCall || requesting) return;
            armed = false; endPending = false;
            if (!CanWrite()) return;
            cursorView = -1; cursorVideo = -1; revision++;
            queueOnly = false;
            queuePlayingUrl = VRCUrl.Empty;
            randomSeed = 0; sequenceStart = -1;
            CommitCursor();
        }
    }
}
