using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.SDK3.Components.Video;
using VRC.SDK3.Video.Components.AVPro;
using VRC.SDK3.UdonNetworkCalling;

namespace aki_lua87.AVEC.SimpleScreen
{
    // One owner writes the transport; every client derives position from the same server clock.
    // The playlist adapter must share this GameObject so both authorities transfer together.
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class SimpleScreenPlayer : UdonSharpBehaviour
    {
        public VRCAVProVideoPlayer videoPlayer;
        public AudioSource[] speakers = new AudioSource[0];
        public Renderer screen;
        public SimpleScreenAdapter adapter;
        public SimpleScreenUI controls;
        [Range(0f, 1f)] public float initialVolume = .5f;
        [Min(.2f)] public float correctionThreshold = 1.25f;
        [Min(10f)] public float loadTimeout = 45f;
        [Range(0, 5)] public int retryLimit = 3;
        [Tooltip("When enabled, only the instance master or instance owner may take control.")]
        public bool masterOnly;

        // 0 stopped, 1 loading, 2 playing, 3 paused, 4 ended.
        [UdonSynced] private byte transport;
        [UdonSynced] private VRCUrl currentUrl = new VRCUrl("");
        [UdonSynced] private int mediaRevision;
        [UdonSynced] private double anchorTime;
        [UdonSynced] private float anchorPosition;
        [UdonSynced] private bool repeat;
        private int appliedRevision = -1;
        private byte appliedTransport = 255;
        private bool initialized;
        private bool ready;
        private bool loading;
        private bool failed;
        private bool startedNotified;
        private int retries;
        private float retryAt = -1f;
        private float loadStarted;
        private float lastLoad = -100f;
        private float lastRequest = -100f;
        private float nextCorrection;
        private float nextBroadcast;
        private float serializeAt = -1f;
        private float volume;
        private bool muted;
        private string message = "";

        private void Start()
        {
            initialized = true;
            volume = Mathf.Clamp01(initialVolume);
            _ApplyAudio();
            ApplyTransport();
            SendCustomEventDelayedSeconds(nameof(_Tick), .25f);
        }
        public bool _Available() { return videoPlayer != null && gameObject.activeInHierarchy; }
        public bool _Ready() { return ready && !loading && !failed && (transport == 2 || transport == 3); }
        public bool _Playing() { return _Ready() && transport == 2; }
        public bool _Loading() { return loading || retryAt >= 0f; }
        public bool _Failed() { return failed; }
        public bool _Repeat() { return repeat; }
        public VRCUrl _Url() { return currentUrl; }
        public float _Volume() { return initialized ? volume : initialVolume; }
        public bool _Muted() { return muted; }
        public string _Status() { return message; }
        public float _Duration() { return ready && videoPlayer != null ? videoPlayer.GetDuration() : 0f; }
        public bool _Seekable()
        {
            float d = _Duration();
            return _Ready() && d > 0f && !float.IsInfinity(d) && !float.IsNaN(d);
        }
        public float _Position() { return ready && videoPlayer != null ? videoPlayer.GetTime() : 0f; }
        public float _Progress() { return _Seekable() ? Mathf.Clamp01(_Position() / _Duration()) : -1f; }
        public bool _CanControl()
        {
            var local = Networking.LocalPlayer;
            return Utilities.IsValid(local) && (!masterOnly || local.isMaster || local.isInstanceOwner);
        }
        public bool _TakeControl()
        {
            if (NetworkCalling.InNetworkCall || !_Available() || !_CanControl()) return false;
            if (!Networking.IsOwner(gameObject)) Networking.SetOwner(Networking.LocalPlayer, gameObject);
            return Networking.IsOwner(gameObject);
        }
        public override bool OnOwnershipRequest(VRCPlayerApi requestingPlayer, VRCPlayerApi requestedOwner)
        {
            return Utilities.IsValid(requestedOwner) && (!masterOnly || requestedOwner.isMaster || requestedOwner.isInstanceOwner);
        }
        public int _Load(VRCUrl url)
        {
            if (NetworkCalling.InNetworkCall || !_Available()) return 0;
            if (VRCUrl.IsNullOrEmpty(url)) return 3;
            string raw = url.Get();
            if (raw.Length > 2048 || (!raw.StartsWith("https://") && !raw.StartsWith("http://"))) return 3;
            // VRChat rate limits URL loads. Do not take ownership on a rejected request.
            if (Time.realtimeSinceStartup - lastRequest < 5.1f || Time.realtimeSinceStartup - lastLoad < 5.1f) return 7;
            if (!_TakeControl()) return 2;
            lastRequest = Time.realtimeSinceStartup;
            currentUrl = url; mediaRevision++; transport = 1;
            anchorPosition = 0f; anchorTime = Networking.GetServerTimeInSeconds();
            Commit(); ApplyTransport();
            return 1;
        }
        public int _Pause(bool pause)
        {
            if (NetworkCalling.InNetworkCall || !_Ready()) return 4;
            if (!_TakeControl()) return 2;
            anchorPosition = TargetPosition(); anchorTime = Networking.GetServerTimeInSeconds();
            transport = (byte)(pause ? 3 : 2);
            Commit(); ApplyTransport(); return 1;
        }
        public int _Stop()
        {
            if (NetworkCalling.InNetworkCall || !_Available()) return 0;
            if (!_TakeControl()) return 2;
            transport = 0; currentUrl = VRCUrl.Empty; mediaRevision++;
            anchorPosition = 0f; Commit(); ApplyTransport(); return 1;
        }
        public int _Seek(float fraction)
        {
            if (NetworkCalling.InNetworkCall || !_Seekable()) return 4;
            if (!_TakeControl()) return 2;
            anchorPosition = Mathf.Clamp01(fraction) * Mathf.Max(0f, _Duration() - .1f);
            anchorTime = Networking.GetServerTimeInSeconds();
            Commit(); Align(true); NotifyState(); return 1;
        }
        public int _Restart()
        {
            if (NetworkCalling.InNetworkCall || !_Seekable()) return 4;
            if (!_TakeControl()) return 2;
            anchorPosition = 0f; anchorTime = Networking.GetServerTimeInSeconds(); transport = 2;
            Commit(); ApplyTransport(); Align(true); return 1;
        }
        public int _SetRepeat(bool value)
        {
            if (value && ready && !_Seekable()) return 4;
            if (NetworkCalling.InNetworkCall || !_TakeControl()) return 2;
            repeat = value; Commit(); NotifyState(); return 1;
        }
        public void _SetVolume(float value)
        {
            if (NetworkCalling.InNetworkCall) return;
            volume = Mathf.Clamp01(value); _ApplyAudio(); NotifyState();
        }
        public void _SetMute(bool value)
        {
            if (NetworkCalling.InNetworkCall) return;
            muted = value; _ApplyAudio(); NotifyState();
        }
        public void _ApplyAudio()
        {
            if (NetworkCalling.InNetworkCall) return;
            for (int i = 0; i < speakers.Length; i++) if (speakers[i] != null)
            { speakers[i].volume = volume; speakers[i].mute = muted; }
        }
        private void Commit()
        {
            if (!Networking.IsOwner(gameObject)) return;
            RequestSerialization(); nextBroadcast = Time.realtimeSinceStartup + 10f;
        }
        public override void OnPostSerialization(VRC.Udon.Common.SerializationResult result)
        {
            if (!result.success && Networking.IsOwner(gameObject)) serializeAt = Time.realtimeSinceStartup + 1f;
        }
        public override void OnDeserialization() { if (initialized) ApplyTransport(); }
        public override void OnOwnershipTransferred(VRCPlayerApi player)
        {
            if (!initialized || !Utilities.IsValid(player) || !player.isLocal) return;
            // Keep the received clock anchor; inheriting ownership never restarts the video.
            Commit();
            if (transport == 1 && ready) BeginPlayback();
            else ApplyTransport();
        }
        private void ApplyTransport()
        {
            if (!_Available()) return;
            bool changedMedia = appliedRevision != mediaRevision;
            bool changedState = appliedTransport != transport;
            appliedTransport = transport;
            if (changedMedia)
            {
                appliedRevision = mediaRevision;
                ready = false; loading = false; failed = false; startedNotified = false;
                retries = 0; retryAt = -1f; message = "";
                videoPlayer.Stop();
                if (transport != 0 && transport != 4 && !VRCUrl.IsNullOrEmpty(currentUrl))
                {
                    retryAt = Mathf.Max(Time.realtimeSinceStartup, lastLoad + 5.1f);
                    message = "Loading…";
                    TryLoad();
                }
            }
            if (transport == 0 || transport == 4)
            {
                loading = false; ready = false; retryAt = -1f; failed = false;
                videoPlayer.Stop(); message = transport == 4 ? "Finished" : "";
                if (changedState && adapter != null)
                {
                    if (transport == 4) adapter._NativeEnded(); else adapter._NativeStopped();
                }
            }
            else if (ready)
            {
                if (transport == 1 && Networking.IsOwner(gameObject)) BeginPlayback();
                else Align(true);
            }
            NotifyState();
        }
        private void TryLoad()
        {
            if (retryAt < 0f || Time.realtimeSinceStartup < retryAt || transport == 0 || transport == 4 || !_Available()) return;
            retryAt = -1f; loading = true; ready = false; failed = false;
            loadStarted = Time.realtimeSinceStartup; lastLoad = loadStarted;
            videoPlayer.Loop = false;
            if (adapter != null) adapter._NativeLoading();
            videoPlayer.LoadURL(currentUrl);
        }
        public override void OnVideoReady()
        {
            if (!loading || appliedRevision != mediaRevision || transport == 0 || transport == 4) return;
            loading = false; ready = true; failed = false; retryAt = -1f;
            message = "";
            // The owner anchors playback only after its media is ready, not while URL resolution runs.
            if (transport == 1 && Networking.IsOwner(gameObject)) BeginPlayback();
            else Align(true);
            _ApplyAudio(); NotifyState();
        }
        private void BeginPlayback()
        {
            if (!Networking.IsOwner(gameObject) || !ready) return;
            anchorPosition = 0f; anchorTime = Networking.GetServerTimeInSeconds(); transport = 2;
            if (!_Seekable()) repeat = false;
            appliedTransport = transport; Commit(); Align(true);
        }
        private float TargetPosition()
        {
            double elapsed = transport == 2 ? Networking.GetServerTimeInSeconds() - anchorTime : 0d;
            float position = Mathf.Max(0f, anchorPosition + (float)elapsed);
            float duration = _Duration();
            if (duration > 0f && !float.IsInfinity(duration) && !float.IsNaN(duration)) position = Mathf.Min(position, duration);
            return position;
        }
        private void Align(bool force)
        {
            if (!ready || failed || videoPlayer == null) return;
            if (transport == 1) { videoPlayer.Pause(); message = "Waiting for owner…"; return; }
            if (transport != 2 && transport != 3) return;
            message = transport == 3 ? "Paused" : "";
            if (_Seekable())
            {
                float target = TargetPosition();
                if (force || Mathf.Abs(videoPlayer.GetTime() - target) > correctionThreshold)
                    videoPlayer.SetTime(Mathf.Min(target, Mathf.Max(0f, _Duration() - .05f)));
            }
            if (transport == 3) videoPlayer.Pause();
            else if (!videoPlayer.IsPlaying) videoPlayer.Play();
            if (!startedNotified)
            {
                startedNotified = true;
                if (adapter != null) adapter._NativeStarted();
            }
        }
        public override void OnVideoStart() { if (ready) { _ApplyAudio(); NotifyState(); } }
        public override void OnVideoEnd()
        {
            if (!ready || loading || transport != 2) return;
            if (_Seekable() && TargetPosition() < _Duration() - 2f) return;
            if (!Networking.IsOwner(gameObject)) return;
            if (repeat && _Seekable()) { anchorPosition = 0f; anchorTime = Networking.GetServerTimeInSeconds(); Commit(); Align(true); }
            else { repeat = false; transport = 4; Commit(); ApplyTransport(); }
        }
        public override void OnVideoError(VideoError error)
        {
            if (transport == 0 || transport == 4 || (!loading && !ready)) return;
            bool transient = error != VideoError.AccessDenied && error != VideoError.InvalidURL;
            Fail(error == VideoError.AccessDenied ? "URL access denied / Check Allow Untrusted URLs" : error == VideoError.InvalidURL ? "Invalid or unsupported URL" : "Video load failed", transient);
        }
        private void Fail(string reason, bool canRetry)
        {
            loading = false; ready = false; failed = true; startedNotified = false;
            if (adapter != null) adapter._NativeError();
            retryAt = -1f;
            if (canRetry && retries < retryLimit)
            {
                retries++;
                retryAt = Mathf.Max(lastLoad + 5.5f, Time.realtimeSinceStartup + Mathf.Min(30f, 5.5f * retries));
                message = reason + " · retry " + retries + "/" + retryLimit;
            }
            else message = reason + " · Submit URL again to retry";
            NotifyState();
        }
        public void _Tick()
        {
            if (NetworkCalling.InNetworkCall || !initialized) return;
            float now = Time.realtimeSinceStartup;
            if (_Available())
            {
                TryLoad();
                if (loading && now - loadStarted > loadTimeout) Fail("Load timed out", true);
                if (now >= nextCorrection) { nextCorrection = now + 2f; Align(false); }
                if (Networking.IsOwner(gameObject) && ((serializeAt >= 0f && now >= serializeAt) || (transport != 0 && now >= nextBroadcast)))
                { serializeAt = -1f; Commit(); }
                // End callbacks can be missed during a late join or client suspension.
                if (ready && transport == 2 && _Seekable() && TargetPosition() >= _Duration()) OnVideoEnd();
            }
            SendCustomEventDelayedSeconds(nameof(_Tick), .25f);
        }
        private void NotifyState()
        {
            if (controls != null) controls._Refresh();
            if (adapter != null) adapter._NotifyLocalStateChanged();
        }
    }
}
