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
        private int modeOverride = -1; // -1: authored default; 0: ordered; 1: random; 2: repeat one
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
        // FIFO keeps source indices for playback, and captures titles at submission for all clients.
        [UdonSynced] private int[] syncedQueueViews = new int[0];
        [UdonSynced] private int[] syncedQueueVideos = new int[0];
        [UdonSynced] private bool syncedQueueOnly;
        private int[] queueViews = new int[0];
        private int[] queueVideos = new int[0];
        private bool queueOnly;
        [UdonSynced] private string[] syncedQueueTitles = new string[0];
        private string[] queueTitles = new string[0];
        [UdonSynced] private VRCUrl[] syncedQueueUrls = new VRCUrl[0];
        [UdonSynced] private VRCUrl syncedQueuePlayingUrl = new VRCUrl("");
        private VRCUrl[] queueUrls = new VRCUrl[0];
        [UdonSynced] private string[] syncedQueueSubmitters = new string[0];
        private string[] queueSubmitters = new string[0];
        // Capture at submission, rather than attributing automatic playback to the current owner.
        [UdonSynced] private string syncedPlayingSubmitter = "";
        private string playingSubmitter = "";
        private VRCUrl queuePlayingUrl = new VRCUrl("");
        [UdonSynced] private string syncedQueuePlayingTitle = "";
        private string queuePlayingTitle = "";
        private VRCUrl endedUrl;
        private float endedAt;
        [UdonSynced] private bool syncedQueueActive;
        private bool queueActive;
        [UdonSynced] private bool syncedQueueAdvanceRequested;
        private bool queueAdvanceRequested;
        private bool queueAdvanceScheduled;
        public bool _QueueAdvancePending() { return queueAdvanceRequested; }
        // Bounded shared history, newest first; playback position stays native/local.
        [UdonSynced] private VRCUrl[] syncedHistoryUrls = new VRCUrl[0];
        [UdonSynced] private string[] syncedHistoryTitles = new string[0];
        private VRCUrl[] historyUrls = new VRCUrl[0];
        private string[] historyTitles = new string[0];
        [UdonSynced] private string[] syncedHistoryCopyUrls = new string[0];
        private string[] historyCopyUrls = new string[0];
        [UdonSynced] private string[] syncedHistorySubmitters = new string[0];
        private string[] historySubmitters = new string[0];
        private VRCUrl lastHistoryUrl = new VRCUrl("");
        private int lastHistoryRevision = -1;
        [UdonSynced] private int previousMode;

        public virtual float _GetProgress() { return -1f; }
        public virtual void _SetNativeMode(int mode) { }
        public virtual bool _GetNativeRepeat() { return false; }
        public bool _IsRepeatMode() { return _Available() && _GetNativeRepeat(); }
        public bool _QueueIsPlaying(int index) { return index == 0 && queueActive && MatchesCurrent(); }
        public bool _IsPlayingEntry(PlaylistView view, int index)
        {
            if (!_Available() || !_NativeReady()) return false;
            if (ActiveView() == view && cursorVideo == index && MatchesCurrent()) return true;
            VRCUrl current = _CurrentUrl();
            return ValidEntry(view, index) && !VRCUrl.IsNullOrEmpty(current) && current.Get() == view.urls[index].Get();
        }
        public int _HistoryCount() { return historyUrls.Length; }
        public VRCUrl _HistoryUrl(int index) { return index >= 0 && index < historyUrls.Length ? historyUrls[index] : VRCUrl.Empty; }
        public string _HistoryCopyUrl(int index)
        {
            if (index >= 0 && index < historyCopyUrls.Length && !string.IsNullOrEmpty(historyCopyUrls[index])) return historyCopyUrls[index];
            VRCUrl url = _HistoryUrl(index); return VRCUrl.IsNullOrEmpty(url) ? "" : url.Get();
        }
        private string CopyUrl(VRCUrl url)
        {
            if (VRCUrl.IsNullOrEmpty(url)) return "";
            for (int v = 0; v < views.Length; v++)
            {
                PlaylistView view = views[v]; if (view == null || view.resolvedUrls == null) continue;
                for (int i = 0; i < view.urls.Length && i < view.resolvedUrls.Length; i++)
                    if (!VRCUrl.IsNullOrEmpty(view.urls[i]) && view.urls[i].Get() == url.Get() && !string.IsNullOrEmpty(view.resolvedUrls[i])) return view.resolvedUrls[i];
            }
            return url.Get();
        }
        public string _HistoryTitle(int index) { return index >= 0 && index < historyTitles.Length ? historyTitles[index] : ""; }
        public string _HistoryDisplayTitle(int index)
        {
            return FormatSubmission(index >= 0 && index < historySubmitters.Length ? historySubmitters[index] : "", _HistoryTitle(index));
        }
        private string LocalSubmitter()
        {
            return Utilities.IsValid(Networking.LocalPlayer) ? Networking.LocalPlayer.displayName : "";
        }
        private string FormatSubmission(string submitter, string title)
        {
            // Names are plain text even on a rich-text-enabled Text component.
            string name = string.IsNullOrEmpty(submitter) ? "不明" : submitter.Replace("<", "＜").Replace(">", "＞");
            return "(" + name + ") " + title;
        }
        public int _PlayHistory(int index)
        {
            if (NetworkCalling.InNetworkCall || requesting || !_Available()) return 0;
            if (index < 0 || index >= historyUrls.Length) return 4;
            return _PlayUserUrl(historyUrls[index], historyTitles[index]);
        }
        public int _PlayUserUrl(VRCUrl url, string title)
        {
            if (NetworkCalling.InNetworkCall || requesting || !_Available()) return 0;
            if (VRCUrl.IsNullOrEmpty(url)) return 3;
            string raw = url.Get();
            if ((!raw.StartsWith("https://") && !raw.StartsWith("http://")) || raw.Length > 2048) return 3;
            if (Time.realtimeSinceStartup - lastLoadTime < 5.1f) return 7;
            if (!_AcquireSequenceControl() || !CanWrite()) return 2;
            requesting = true;
            _SetNativeMode(modeOverride == 2 ? 2 : 0);
            if (string.IsNullOrEmpty(title)) title = raw;
            int result = _Request(url, title);
            requesting = false;
            if (result != 1) return result;
            queueAdvanceRequested = false;
            FinishQueued();
            cursorView = -1; cursorVideo = -1; queueOnly = true;
            queuePlayingUrl = url; revision++; armed = false; endPending = false;
            queuePlayingTitle = title;
            playingSubmitter = LocalSubmitter();
            CommitCursor(); return 1;
        }
        private void RecordHistory()
        {
            if (!CanWrite() || !_NativeReady()) return;
            bool enabled = false;
            for (int i = 0; i < views.Length; i++) if (views[i] != null && views[i].enableHistory) enabled = true;
            if (!enabled) return;
            VRCUrl url = _CurrentUrl();
            if (VRCUrl.IsNullOrEmpty(url)) return;
            if (lastHistoryRevision == revision && !VRCUrl.IsNullOrEmpty(lastHistoryUrl) && lastHistoryUrl.Get() == url.Get()) return;
            lastHistoryRevision = revision; lastHistoryUrl = url;
            int length = Mathf.Min(32, historyUrls.Length + 1);
            var newUrls = new VRCUrl[length]; var newTitles = new string[length]; var newSubmitters = new string[length]; var newCopies = new string[length];
            for (int i = 1; i < length; i++) { newUrls[i] = historyUrls[i - 1]; newTitles[i] = historyTitles[i - 1]; newSubmitters[i] = historySubmitters[i - 1]; newCopies[i] = i - 1 < historyCopyUrls.Length ? historyCopyUrls[i - 1] : ""; }
            newUrls[0] = url; newCopies[0] = CopyUrl(url);
            newTitles[0] = MatchesCurrent() && !string.IsNullOrEmpty(queuePlayingTitle) ? queuePlayingTitle : url.Get();
            if (string.IsNullOrEmpty(newTitles[0])) newTitles[0] = url.Get();
            newSubmitters[0] = MatchesCurrent() ? playingSubmitter : "";
            historyUrls = newUrls; historyTitles = newTitles; historySubmitters = newSubmitters; historyCopyUrls = newCopies;
            CommitCursor();
        }
        private void FinishQueued()
        {
            if (queueActive && queueViews.Length > 0) RemoveQueued(0);
            queueActive = false;
        }

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
        public virtual bool _IsPlaybackRunning() { return false; }
        public virtual bool _GetMute() { return false; }
        public virtual int _SetMute(bool value) { return 0; }
        public int _QueueCount() { return queueViews.Length; }
        public string _QueueDisplayTitle(int index)
        {
            return FormatSubmission(index >= 0 && index < queueSubmitters.Length ? queueSubmitters[index] : "", _QueueTitle(index));
        }
        private void AppendQueueTitle(string title)
        {
            var values = new string[queueTitles.Length + 1];
            System.Array.Copy(queueTitles, values, queueTitles.Length);
            values[values.Length - 1] = title;
            queueTitles = values;
        }
        private void AppendQueueSubmitter()
        {
            var names = new string[queueSubmitters.Length + 1];
            System.Array.Copy(queueSubmitters, names, queueSubmitters.Length);
            names[names.Length - 1] = LocalSubmitter();
            queueSubmitters = names;
        }
        public string _QueueTitle(int index)
        {
            if (index < 0 || index >= queueViews.Length || index >= queueVideos.Length) return "";
            if (index < queueTitles.Length && !string.IsNullOrEmpty(queueTitles[index])) return queueTitles[index];
            return index < queueUrls.Length && !VRCUrl.IsNullOrEmpty(queueUrls[index]) ? queueUrls[index].Get() : "無効な動画";
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
            AppendQueueSubmitter();
            AppendQueueTitle(string.IsNullOrEmpty(view.titles[index]) ? view.urls[index].Get() : view.titles[index]);
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
            AppendQueueSubmitter();
            AppendQueueTitle(raw);
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
            if (index == 0 && queueActive)
            {
                if (queueViews.Length > 1 && Time.realtimeSinceStartup - lastLoadTime < 5.1f)
                {
                    int paused = _ControlPlayback(true);
                    if (paused != 1) return paused;
                    queueActive = false; RemoveQueued(0);
                    armed = false; endPending = false; cursorView = -1; cursorVideo = -1;
                    queuePlayingUrl = VRCUrl.Empty; queuePlayingTitle = ""; queueOnly = true; revision++;
                    queueAdvanceRequested = true;
                    CommitCursor(); ScheduleRemovedQueue(); return 1;
                }
                int[] oldViews = queueViews; int[] oldVideos = queueVideos;
                VRCUrl[] oldUrls = queueUrls; string[] oldTitles = queueTitles; string[] oldNames = queueSubmitters;
                queueActive = false; RemoveQueued(0);
                int result = queueViews.Length > 0 ? PlayQueued() : _ControlPlayback(true);
                if (result != 1)
                {
                    queueViews = oldViews; queueVideos = oldVideos; queueUrls = oldUrls;
                    queueTitles = oldTitles; queueSubmitters = oldNames; queueActive = true;
                    return result;
                }
                if (queueViews.Length == 0)
                {
                    armed = false; endPending = false; cursorView = -1; cursorVideo = -1;
                    queuePlayingUrl = VRCUrl.Empty; queuePlayingTitle = ""; queueOnly = true; revision++;
                }
                CommitCursor(); return 1;
            }
            RemoveQueued(index); CommitCursor(); return 1;
        }
        public int _ClearQueue()
        {
            if (NetworkCalling.InNetworkCall || requesting || !_Available()) return 0;
            if (!_AcquireSequenceControl() || !CanWrite()) return 2;
            while (queueViews.Length > (queueActive ? 1 : 0)) RemoveQueued(queueViews.Length - 1);
            if (queueViews.Length == 0) queueAdvanceRequested = false;
            CommitCursor(); return 1;
        }
        private void ScheduleRemovedQueue()
        {
            if (!queueAdvanceRequested || queueAdvanceScheduled || !CanWrite()) return;
            queueAdvanceScheduled = true;
            SendCustomEventDelayedSeconds(nameof(_ContinueRemovedQueue), Mathf.Max(.1f, 5.1f - (Time.realtimeSinceStartup - lastLoadTime)));
        }
        public void _ContinueRemovedQueue()
        {
            if (NetworkCalling.InNetworkCall) return;
            queueAdvanceScheduled = false;
            if (!queueAdvanceRequested || requesting || !_Available() || !CanWrite()) return;
            if (Time.realtimeSinceStartup - lastLoadTime < 5.1f) { ScheduleRemovedQueue(); return; }
            int result = queueViews.Length > 0 ? PlayQueued() : 4;
            queueAdvanceRequested = false; CommitCursor();
            if (result != 1 && result != 4)
                for (int i = 0; i < views.Length; i++) if (views[i] != null) views[i]._SequenceStatus("次のキューを再生できません。接続先・権限を確認してください。");
        }
        private void RemoveQueued(int index)
        {
            int length = queueViews.Length - 1;
            var sources = new int[length]; var videos = new int[length];
            var inputs = new VRCUrl[length];
            var names = new string[length]; var titles = new string[length];
            for (int from = 0, to = 0; from < queueViews.Length; from++)
            {
                if (from == index) continue;
                sources[to] = queueViews[from]; videos[to] = queueVideos[from]; inputs[to] = queueUrls[from]; names[to] = queueSubmitters[from]; titles[to] = queueTitles[from]; to++;
            }
            queueViews = sources; queueVideos = videos; queueUrls = inputs; queueSubmitters = names; queueTitles = titles;
        }
        private int PlayQueued()
        {
            if (queueActive) return _ControlPlayback(false);
            if (queueViews.Length == 0 || queueVideos.Length != queueViews.Length) return 4;
            int source = queueViews[0];
            if (source == -1)
            {
                VRCUrl url = queueUrls[0];
                requesting = true;
                _SetNativeMode(modeOverride == 2 ? 2 : 0);
                int submitted = _Request(url, url.Get());
                requesting = false;
                if (submitted != 1) return submitted;
                queueAdvanceRequested = false;
                queuePlayingUrl = url; queueOnly = true;
                queuePlayingTitle = url.Get();
                playingSubmitter = queueSubmitters[0];
                cursorView = -1; cursorVideo = -1; revision++; endPending = false; armed = false;
                queueActive = true; CommitCursor(); return 1;
            }
            if (source < 0 || source >= views.Length) return 3;
            int result = PlayEntry(views[source], queueVideos[0], false, queueSubmitters[0], _QueueTitle(0));
            if (result == 1) { queueActive = true; queueOnly = true; CommitCursor(); }
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

        public int _SkipToNextQueued()
        {
            if (NetworkCalling.InNetworkCall || requesting || !_Available()) return 0;
            if (queueAdvanceRequested) return 1;
            if (queueActive) return _RemoveQueued(0);
            if (queueViews.Length == 0) return 4;
            if (!_AcquireSequenceControl() || !CanWrite()) return 2;
            if (Time.realtimeSinceStartup - lastLoadTime < 5.1f)
            {
                if (!_NativeReady()) return 7;
                int paused = _ControlPlayback(true);
                if (paused != 1) return paused;
                armed = false; endPending = false; cursorView = -1; cursorVideo = -1;
                queuePlayingUrl = VRCUrl.Empty; queuePlayingTitle = ""; queueOnly = true; revision++;
                queueAdvanceRequested = true;
                CommitCursor(); ScheduleRemovedQueue(); return 1;
            }
            return PlayQueued();
        }

        public bool _IsRandomMode(PlaylistView view, int tab)
        {
            if (modeOverride >= 0) return modeOverride == 1;
            if (ActiveView() != null) return randomSeed != 0;
            return view != null && view.randomTabs != null && tab >= 0 && tab < view.randomTabs.Length && view.randomTabs[tab];
        }
        // Explicit, idempotent remote mode selection; never reload the current video.
        public int _SetPlaybackMode(PlaylistView view, int nextMode)
        {
            if (NetworkCalling.InNetworkCall || requesting || !_Available()) return 0;
            if (System.Array.IndexOf(views, view) < 0) return 0;
            if (nextMode < 0 || nextMode > 2) return 3;
            if (!_AcquireSequenceControl() || !CanWrite()) return 2;
            if (modeOverride == nextMode && _GetNativeRepeat() == (nextMode == 2)) { _SetNativeMode(nextMode); return 1; }
            modeOverride = nextMode;
            if (nextMode != 2) previousMode = nextMode;
            _SetNativeMode(nextMode);
            if (ValidEntry(ActiveView(), cursorVideo))
            {
                sequenceStart = cursorVideo;
                randomSeed = nextMode == 1 ? Random.Range(1, int.MaxValue) : 0;
            }
            // Preserve the current load and a pending natural End; only its successor changes.
            CommitCursor();
            return 1;
        }
        public int _TogglePlaybackMode(PlaylistView view, int tab)
        {
            return _SetPlaybackMode(view, _IsRepeatMode() ? 0 : _IsRandomMode(view, tab) ? 2 : 1);
        }
        public int _ToggleRepeat()
        {
            if (NetworkCalling.InNetworkCall || requesting || !_Available()) return 0;
            if (!_AcquireSequenceControl() || !CanWrite()) return 2;
            if (_GetNativeRepeat()) modeOverride = modeOverride == 2 ? previousMode : (_IsRandomMode(ActiveView(), 0) ? 1 : 0);
            else { previousMode = _IsRandomMode(ActiveView(), 0) ? 1 : 0; modeOverride = 2; }
            _SetNativeMode(modeOverride);
            sequenceStart = cursorVideo;
            randomSeed = modeOverride == 1 ? Random.Range(1, int.MaxValue) : 0;
            CommitCursor(); return 1;
        }

        public int _PlayFrom(PlaylistView view, int index)
        {
            if (NetworkCalling.InNetworkCall) return 0;
            return PlayEntry(view, index, true, LocalSubmitter(), null);
        }

        // Manual remote navigation shares the native-player cursor; no second cursor is synced.
        // Random navigation follows the same stable seeded order in either direction.
        public int _StepVideo(PlaylistView fallback, int direction)
        {
            if (NetworkCalling.InNetworkCall || requesting || !_Available()) return 0;
            if (fallback == null || System.Array.IndexOf(views, fallback) < 0) return 3;
            if (Time.realtimeSinceStartup - lastLoadTime < 5.1f) return 7;
            if (direction > 0 && queueViews.Length > (queueActive ? 1 : 0))
            {
                if (!_AcquireSequenceControl() || !CanWrite()) return 2;
                // Keep the current queue entry if the successor request is rejected.
                int[] oldViews = queueViews; int[] oldVideos = queueVideos;
                VRCUrl[] oldUrls = queueUrls; string[] oldTitles = queueTitles; string[] oldNames = queueSubmitters;
                bool oldActive = queueActive;
                if (queueActive) FinishQueued();
                int queuedResult = PlayQueued();
                if (queuedResult != 1) { queueViews = oldViews; queueVideos = oldVideos; queueUrls = oldUrls; queueTitles = oldTitles; queueSubmitters = oldNames; queueActive = oldActive; }
                return queuedResult;
            }
            if (direction > 0 && queueActive) return 4;
            var view = ActiveView();
            int current = cursorVideo;
            if (!ValidEntry(view, current) || !MatchesCurrent())
            {
                view = fallback;
                int selectedTab = fallback._SelectedTab();
                for (int i = 0; i < view.urls.Length; i++)
                    if (ValidEntry(view, i) && view.videoTabs[i] == selectedTab) return PlayEntry(view, i, true, LocalSubmitter(), null);
                return 4;
            }
            int next = NavigationIndex(view, current, direction);
            if (next < 0) return 4;
            // Preserve the current random seed but make a manual selection the new pass origin.
            int result = PlayEntry(view, next, false, LocalSubmitter(), null);
            if (result == 1) { FinishQueued(); queueOnly = false; sequenceStart = next; CommitCursor(); }
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

        private int PlayEntry(PlaylistView view, int index, bool beginSequence, string submitter, string capturedTitle)
        {
            if (NetworkCalling.InNetworkCall || view == null || !_Available()) return 0;
            int source = System.Array.IndexOf(views, view);
            if (source < 0 || !ValidEntry(view, index)) return 3;
            if (!_AcquireSequenceControl() || !CanWrite()) return 2;
            requesting = true;
            // Starting an AVEC sequence applies its authored mode, overriding native defaults.
            _SetNativeMode(modeOverride == 2 ? 2 : _IsRandomMode(view, view.videoTabs[index]) ? 1 : 0);
            string title = string.IsNullOrEmpty(capturedTitle) ? view.titles[index] : capturedTitle;
            if (string.IsNullOrEmpty(title)) title = view.urls[index].Get();
            int result = _Request(view.urls[index], title);
            requesting = false;
            if (result != 1) return result;
            // Queue APIs can transfer a frontend without transferring its playback core.
            queueAdvanceRequested = false;
            if (!_IsLocalPlayback() && !Networking.IsOwner(gameObject))
                Networking.SetOwner(Networking.LocalPlayer, gameObject);
            if (!CanWrite()) return 2;
            if (beginSequence)
            {
                FinishQueued();
                queueOnly = false;
                sequenceStart = index;
                int tab = view.videoTabs[index];
                bool random = modeOverride >= 0 ? modeOverride == 1 : view.randomTabs != null && tab >= 0 && tab < view.randomTabs.Length && view.randomTabs[tab];
                randomSeed = random
                    ? Random.Range(1, int.MaxValue) : 0;
            }
            cursorView = source; cursorVideo = index; revision++;
            playingSubmitter = submitter;
            queuePlayingUrl = VRCUrl.Empty;
            queuePlayingTitle = title;
            endPending = false; armed = false;
            CommitCursor();
            // Wait for the actual native Ready/Start callback, even if a queued URL
            // happens to equal the URL which is playing right now.
            return 1;
        }

        private bool CanWrite() { return _IsLocalPlayback() || Networking.IsOwner(gameObject); }
        private bool ValidEntry(PlaylistView view, int index)
        {
            if (view == null || index < 0 || index >= view.urls.Length || index >= view.titles.Length || index >= view.videoTabs.Length || !view._EntryVisible(index)) return false;
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
                syncedQueueTitles = (string[])queueTitles.Clone();
                syncedQueueUrls = (VRCUrl[])queueUrls.Clone(); syncedQueuePlayingUrl = queuePlayingUrl;
                syncedQueueSubmitters = (string[])queueSubmitters.Clone(); syncedPlayingSubmitter = playingSubmitter;
                syncedQueuePlayingTitle = queuePlayingTitle;
                syncedQueueActive = queueActive;
                syncedQueueAdvanceRequested = queueAdvanceRequested;
                syncedHistoryUrls = (VRCUrl[])historyUrls.Clone(); syncedHistoryTitles = (string[])historyTitles.Clone();
                syncedHistorySubmitters = (string[])historySubmitters.Clone();
                syncedHistoryCopyUrls = (string[])historyCopyUrls.Clone();
                RequestSerialization();
            }
            ShowCursor();
        }
        private void ShowCursor()
        {
            var view = ActiveView();
            for (int i = 0; i < views.Length; i++) if (views[i] != null) views[i]._ShowSequenceSelection(views[i] == view ? cursorVideo : -1);
            for (int i = 0; i < views.Length; i++) if (views[i] != null) { views[i]._RefreshPlaybackMode(); views[i]._RefreshQueue(); views[i]._RefreshHistory(); }
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
            queueTitles = syncedQueueTitles == null ? new string[0] : (string[])syncedQueueTitles.Clone();
            if (queueTitles.Length != queueViews.Length) queueTitles = new string[queueViews.Length];
            queueUrls = syncedQueueUrls == null ? new VRCUrl[0] : (VRCUrl[])syncedQueueUrls.Clone();
            queueSubmitters = syncedQueueSubmitters == null ? new string[0] : (string[])syncedQueueSubmitters.Clone();
            playingSubmitter = syncedPlayingSubmitter;
            queuePlayingUrl = syncedQueuePlayingUrl;
            queuePlayingTitle = syncedQueuePlayingTitle;
            queueActive = syncedQueueActive;
            queueAdvanceRequested = syncedQueueAdvanceRequested;
            historyCopyUrls = syncedHistoryCopyUrls == null ? new string[0] : (string[])syncedHistoryCopyUrls.Clone();
            historyUrls = syncedHistoryUrls == null ? new VRCUrl[0] : (VRCUrl[])syncedHistoryUrls.Clone();
            historyTitles = syncedHistoryTitles == null ? new string[0] : (string[])syncedHistoryTitles.Clone();
            historySubmitters = syncedHistorySubmitters == null ? new string[0] : (string[])syncedHistorySubmitters.Clone();
            if (historyUrls.Length != historyTitles.Length) { historyUrls = new VRCUrl[0]; historyTitles = new string[0]; }
            // Missing attribution must not discard otherwise valid queue/history entries.
            if (historyCopyUrls.Length != historyUrls.Length) historyCopyUrls = new string[historyUrls.Length];
            if (historySubmitters.Length != historyUrls.Length) historySubmitters = new string[historyUrls.Length];
            // Baseline received history so taking ownership doesn't add the current track twice.
            if (historyUrls.Length > 0) { lastHistoryUrl = historyUrls[0]; lastHistoryRevision = revision; }
            if (queueViews.Length != queueVideos.Length || queueViews.Length != queueUrls.Length) {
                queueViews = new int[0]; queueVideos = new int[0]; queueUrls = new VRCUrl[0];
            }
            if (queueSubmitters.Length != queueViews.Length) queueSubmitters = new string[queueViews.Length];
            if (changed) { endPending = false; armed = false; }
            // Cursor and native URL can arrive in either order, including for late joiners.
            if (_Available() && _NativeReady() && MatchesCurrent()) armed = true;
            ShowCursor();
            ScheduleRemovedQueue();
        }

        public override void OnOwnershipTransferred(VRCPlayerApi player)
        {
            if (!Utilities.IsValid(player) || !player.isLocal || !_Available()) return;
            if (queueAdvanceRequested) { ScheduleRemovedQueue(); return; }
            if (endPending) SendCustomEventDelayedFrames(nameof(_AdvanceSequence), 1);
            else if (_NativeReady() && MatchesCurrent()) armed = true;
        }

        public void _NativeLoading()
        {
            if (NetworkCalling.InNetworkCall) return;
            lastLoadTime = Time.realtimeSinceStartup;
            if (queueAdvanceRequested && !requesting && CanWrite()) { queueAdvanceRequested = false; CommitCursor(); }
        }
        public void _NativeStarted()
        {
            if (NetworkCalling.InNetworkCall || requesting || !_Available() || endPending) return;
            if (queueAdvanceRequested && CanWrite()) { queueAdvanceRequested = false; CommitCursor(); }
            if (MatchesCurrent())
            {
                if (_NativeReady())
                {
                    armed = true;
                    // ProTV parses URL loop / implicit replay after the initial request.
                    if (CanWrite()) _SetNativeMode(modeOverride == 2 ? 2 : queueOnly ? 0 : _IsRandomMode(ActiveView(), 0) ? 1 : 0);
                }
            }
            else if (armed) _CancelSequence();
            RecordHistory();
        }
        public void _NativeEnded()
        {
            if (NetworkCalling.InNetworkCall || requesting || endPending || (!armed && queueViews.Length == 0)) return;
            if (_IsRepeatMode() && MatchesCurrent()) return;
            if (CanWrite()) { FinishQueued(); CommitCursor(); }
            endedUrl = _CurrentUrl(); endedAt = Time.realtimeSinceStartup;
            armed = false; endPending = true; pendingRevision = revision;
            // Let the player's own end/queue handlers finish before requesting another URL.
            SendCustomEventDelayedSeconds(nameof(_AdvanceSequence), Mathf.Max(.2f, 5.1f - (Time.realtimeSinceStartup - lastLoadTime)));
        }
        public void _AdvanceSequence()
        {
            if (NetworkCalling.InNetworkCall || !endPending || pendingRevision != revision) return;
            if (!_Available()) { endPending = false; return; }
            if (!CanWrite()) return;
            float remaining = 5.1f - (Time.realtimeSinceStartup - lastLoadTime);
            if (remaining > 0) { SendCustomEventDelayedSeconds(nameof(_AdvanceSequence), remaining); return; }
            endPending = false;
            // A native queue may start another track before our delayed callback.
            // Let it finish instead of submitting a competing request.
            var current = _CurrentUrl();
            if (lastLoadTime > endedAt || (!VRCUrl.IsNullOrEmpty(current) && !VRCUrl.IsNullOrEmpty(endedUrl) && current.Get() != endedUrl.Get())) return;
            // An End received before gaining ownership could not consume the playing head.
            // Consume it now instead of resuming that same queue entry.
            if (queueActive) { FinishQueued(); CommitCursor(); }
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
            int result = PlayEntry(view, next, false, playingSubmitter, null);
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
            queueAdvanceRequested = false;
            FinishQueued();
            cursorView = -1; cursorVideo = -1; revision++;
            queueOnly = false;
            queuePlayingUrl = VRCUrl.Empty;
            queuePlayingTitle = "";
            playingSubmitter = "";
            randomSeed = 0; sequenceStart = -1;
            CommitCursor();
        }
    }
}
