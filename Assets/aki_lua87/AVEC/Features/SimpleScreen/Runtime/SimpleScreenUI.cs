using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.Components;
using VRC.SDK3.UdonNetworkCalling;

namespace aki_lua87.AVEC.SimpleScreen
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class SimpleScreenUI : UdonSharpBehaviour
    {
        public SimpleScreenPlayer player;
        public CanvasGroup panel;
        public Slider seek;
        public Slider volume;
        public VRCUrlInputField urlInput;
        public Text timeLabel;
        public Text volumeLabel;
        public Text statusLabel;
        public Image muteIcon;
        public Sprite soundSprite;
        public Sprite mutedSprite;
        [Min(.1f)] public float hideDelay = 2f;
        private bool hovered;
        private bool dragging;
        private bool seeking;
        private bool editing;
        private bool refreshing;
        private float lastActive;
        private float lastSubmit = -100f;
        private string feedback = "";
        private float feedbackUntil;
        private void Start()
        {
            lastActive = -100f; _Refresh(); SetVisible(false);
            SendCustomEventDelayedSeconds(nameof(_Poll), .2f);
        }
        public void _PointerEnter() { if (NetworkCalling.InNetworkCall) return; hovered = true; lastActive = Time.realtimeSinceStartup; SetVisible(true); }
        public void _PointerExit() { if (NetworkCalling.InNetworkCall) return; hovered = false; lastActive = Time.realtimeSinceStartup; }
        public void _BeginDrag() { if (NetworkCalling.InNetworkCall) return; dragging = true; }
        public void _EndDrag() { if (NetworkCalling.InNetworkCall) return; dragging = false; lastActive = Time.realtimeSinceStartup; }
        public void _BeginSeek() { if (NetworkCalling.InNetworkCall) return; seeking = true; _BeginDrag(); }
        public void _EndSeek()
        {
            if (NetworkCalling.InNetworkCall) return;
            if (seeking && player != null && seek != null) Feedback(player._Seek(seek.value));
            seeking = false; _EndDrag(); _Refresh();
        }
        public void _SeekChanged()
        {
            if (NetworkCalling.InNetworkCall || refreshing || seeking || player == null || seek == null) return;
            Feedback(player._Seek(seek.value)); lastActive = Time.realtimeSinceStartup;
        }
        public void _VolumeChanged()
        {
            if (NetworkCalling.InNetworkCall || refreshing || player == null || volume == null) return;
            player._SetVolume(volume.value); lastActive = Time.realtimeSinceStartup;
        }
        public void _ToggleMute()
        {
            if (NetworkCalling.InNetworkCall || player == null) return;
            player._SetMute(!player._Muted()); lastActive = Time.realtimeSinceStartup;
        }
        public void _BeginInput() { if (!NetworkCalling.InNetworkCall) { editing = true; lastActive = Time.realtimeSinceStartup; } }
        public void _SubmitUrl()
        {
            if (NetworkCalling.InNetworkCall || player == null || urlInput == null) return;
            editing = false; lastActive = Time.realtimeSinceStartup;
            if (urlInput.wasCanceled || Time.realtimeSinceStartup - lastSubmit < .5f) return;
            var url = urlInput.GetUrl();
            if (VRC.SDKBase.VRCUrl.IsNullOrEmpty(url)) return;
            lastSubmit = Time.realtimeSinceStartup;
            // Use the adapter entrypoint to cancel the previous playlist sequence and record history.
            Feedback(player.adapter != null ? player.adapter._PlayUserUrl(url, "URL") : player._Load(url));
        }
        private void Feedback(int result)
        {
            feedback = result == 1 ? "" : result == 2 ? "Playback is locked" : result == 7 ? "Please wait before loading another URL" : result == 3 ? "Check URL" : result == 4 ? "Not available for this video" : "Player unavailable";
            feedbackUntil = Time.realtimeSinceStartup + 4f; _Refresh();
        }
        private string Clock(float seconds)
        {
            int total = Mathf.Max(0, Mathf.FloorToInt(seconds));
            return total >= 3600 ? (total / 3600) + ":" + ((total / 60) % 60).ToString("D2") + ":" + (total % 60).ToString("D2") : (total / 60) + ":" + (total % 60).ToString("D2");
        }
        public void _Refresh()
        {
            if (NetworkCalling.InNetworkCall || player == null) return;
            refreshing = true;
            if (volume != null) volume.SetValueWithoutNotify(player._Volume());
            if (volumeLabel != null) volumeLabel.text = player._Muted() ? "Muted" : Mathf.RoundToInt(player._Volume() * 100f) + "%";
            if (muteIcon != null) muteIcon.sprite = player._Muted() ? mutedSprite : soundSprite;
            bool canSeek = player._Seekable();
            if (seek != null) { seek.interactable = canSeek && player._CanControl(); if (!seeking) seek.SetValueWithoutNotify(canSeek ? player._Progress() : 0f); }
            if (timeLabel != null) timeLabel.text = canSeek ? Clock(seeking && seek != null ? seek.value * player._Duration() : player._Position()) + " / " + Clock(player._Duration()) : player._Ready() ? "LIVE" : "--:-- / --:--";
            if (statusLabel != null) statusLabel.text = Time.realtimeSinceStartup < feedbackUntil && feedback != "" ? feedback : player._Status();
            refreshing = false;
        }
        private void SetVisible(bool value)
        {
            if (panel == null) return;
            panel.alpha = value ? 1f : 0f; panel.interactable = value; panel.blocksRaycasts = value;
        }
        public void _Poll()
        {
            if (NetworkCalling.InNetworkCall) return;
            bool active = hovered || dragging || editing || (urlInput != null && urlInput.isFocused);
            if (active) lastActive = Time.realtimeSinceStartup;
            bool visible = active || Time.realtimeSinceStartup - lastActive < hideDelay;
            SetVisible(visible); if (visible) _Refresh();
            SendCustomEventDelayedSeconds(nameof(_Poll), .2f);
        }
    }
}
