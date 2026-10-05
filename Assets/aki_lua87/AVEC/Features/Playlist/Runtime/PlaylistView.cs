using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using aki_lua87.AVEC;


namespace aki_lua87.CommonVideoPlaylist
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class PlaylistView : UdonSharpBehaviour
    {
        public PlaylistAdapter adapter;
        public Text heading;
        public Text status;
        public Text playbackModeLabel;
        public Image playbackModeIcon;
        public Sprite orderedIcon;
        public Sprite randomIcon;
        public Slider volumeSlider;
        public Text volumeLabel;
        public Text connectionLabel;
        // Editor-baked destination name; persisted because PlaylistSetup is EditorOnly.
        [HideInInspector] public string connectionName = "";
        public Text audioLinkLabel;
        public GameObject queuePanel;
        public Text queueButtonLabel;
        public Text queuePageLabel;
        public Image queueTabImage;
        public GameObject queueAddPanel;
        public VRC.SDK3.Components.VRCUrlInputField queueUrlInput;
        public Text[] queueLabels = new Text[0];
        public GameObject[] queueRows = new GameObject[0];
        private int queuePage;
        // Editor-discovered optional integration references, persisted for runtime status only.
        [HideInInspector] public VRC.Udon.UdonBehaviour[] audioLinks = new VRC.Udon.UdonBehaviour[0];
        [HideInInspector] public AudioSource[] connectedAudioSources = new AudioSource[0];
        [HideInInspector] public bool audioLinkInstalled;
        public Color accentColor = new Color(.25f, .86f, .76f);
        public Color iconColor = new Color(.92f, .96f, 1f);
        public ScrollRect videoScroll;
        public Color normalColor = new Color(.12f, .16f, .22f);
        public Color selectedColor = new Color(.12f, .43f, .48f);
        // Persisted editor-baked data: no ScriptableObject access in Udon.
        [HideInInspector] public string[] tabNames = new string[0];
        [HideInInspector] public bool[] randomTabs = new bool[0];
        [HideInInspector] public int[] videoTabs = new int[0];
        [HideInInspector] public string[] titles = new string[0];
        [HideInInspector] public VRCUrl[] urls = new VRCUrl[0];
        [HideInInspector] public Image[] tabImages = new Image[0];
        [HideInInspector] public Image[] videoImages = new Image[0];
        // Persisted by PlaylistBaker, including URLs that cannot be constructed in Udon.
        [HideInInspector] public VRCUrl[] titleSourceUrls = new VRCUrl[0];
        [HideInInspector] public int[] dynamicTitleIndices = new int[0];
        [HideInInspector] public Text[] videoLabels = new Text[0];
        [HideInInspector] public FeatureExtensionBehaviour[] extensions = new FeatureExtensionBehaviour[0];
        private int selectedTab = -1;
        private int selectedVideo = -1;
        private void Start() { _ToggleQueue(); _RefreshVolume(); SendCustomEventDelayedSeconds(nameof(_PollVolume), .5f); }
        public void _PollVolume()
        {
            if (VRC.SDK3.UdonNetworkCalling.NetworkCalling.InNetworkCall) return;
            _RefreshVolume();
            SendCustomEventDelayedSeconds(nameof(_PollVolume), .5f);
        }
        public void _RefreshVolume()
        {
            _RefreshQueue();
            if (connectionLabel != null) connectionLabel.text = adapter == null || !adapter._Available() ? "未接続" : connectionName;
            if (audioLinkLabel != null) audioLinkLabel.text = _AudioLinkStatus();
            float value = adapter == null ? -1f : adapter._GetVolume();
            if (volumeSlider != null) { volumeSlider.interactable = value >= 0f; if (value >= 0f) volumeSlider.SetValueWithoutNotify(value); }
            if (volumeLabel != null) volumeLabel.text = value < 0f ? "—" : Mathf.RoundToInt(value * 100f) + "%";
        }
        public void _EnqueueVideo(int index)
        {
            if (VRC.SDK3.UdonNetworkCalling.NetworkCalling.InNetworkCall) return;
            int result = adapter == null ? 0 : adapter._EnqueueFrom(this, index);
            SetStatus(result == 1 ? "キューに追加しました" : result == 2 ? "操作権限がありません" : result == 5 ? "キューは最大32件です" : "キューへ追加できません。接続先・URLを確認してください。");
            _RefreshQueue();
        }
        public void _ToggleQueue()
        {
            if (VRC.SDK3.UdonNetworkCalling.NetworkCalling.InNetworkCall || queuePanel == null) return;
            queuePanel.SetActive(true);
            if (queueAddPanel != null) queueAddPanel.SetActive(false);
            if (videoScroll != null) videoScroll.gameObject.SetActive(false);
            selectedTab = -1;
            if (heading != null) heading.text = "キュー";
            if (queueTabImage != null) queueTabImage.color = selectedColor;
            for (int i = 0; i < tabImages.Length; i++) if (tabImages[i] != null) tabImages[i].color = normalColor;
            _RefreshQueue();
            SetStatus(adapter == null || !adapter._Available() ? "接続先を確認してください" : adapter._QueueCount() == 0 ? "動画を追加してください" : "追加した順に再生します");
        }
        public void _OpenQueueAdd()
        {
            if (VRC.SDK3.UdonNetworkCalling.NetworkCalling.InNetworkCall || queueAddPanel == null) return;
            queueAddPanel.SetActive(true);
        }
        public void _CloseQueueAdd()
        {
            if (VRC.SDK3.UdonNetworkCalling.NetworkCalling.InNetworkCall || queueAddPanel == null) return;
            queueAddPanel.SetActive(false);
        }
        public void _SubmitQueueUrl()
        {
            if (VRC.SDK3.UdonNetworkCalling.NetworkCalling.InNetworkCall || queueUrlInput == null) return;
            int result = adapter == null ? 0 : adapter._EnqueueUrl(queueUrlInput.GetUrl());
            SetStatus(result == 1 ? "キューに追加しました" : result == 2 ? "操作権限がありません" : result == 5 ? "キューは最大32件です" : "http(s)の動画URLを入力してください");
            if (result == 1) { queueUrlInput.SetUrl(VRCUrl.Empty); _CloseQueueAdd(); }
            _RefreshQueue();
        }
        public void _QueueNextPage() { if (VRC.SDK3.UdonNetworkCalling.NetworkCalling.InNetworkCall) return; queuePage++; _RefreshQueue(); }
        public void _QueuePreviousPage() { if (VRC.SDK3.UdonNetworkCalling.NetworkCalling.InNetworkCall) return; queuePage--; _RefreshQueue(); }
        public void _ClearQueue()
        {
            if (VRC.SDK3.UdonNetworkCalling.NetworkCalling.InNetworkCall) return;
            int result = adapter == null ? 0 : adapter._ClearQueue();
            SetStatus(result == 1 ? "待機中のキューを消去しました" : result == 2 ? "操作権限がありません" : "接続先を確認してください");
            _RefreshQueue();
        }
        public void _StartQueuedPlayback()
        {
            if (VRC.SDK3.UdonNetworkCalling.NetworkCalling.InNetworkCall) return;
            int result = adapter == null ? 0 : adapter._PlayQueueNext();
            SetStatus(result == 1 ? "キューの再生を要求しました" : result == 2 ? "操作権限がありません" : result == 7 ? "読み込み間隔を空けて再試行してください" : result == 4 ? "キューは空です" : "接続先を確認してください");
        }
        public void _RemoveQueueSlot(int slot)
        {
            if (VRC.SDK3.UdonNetworkCalling.NetworkCalling.InNetworkCall || queueLabels.Length == 0) return;
            int result = adapter == null ? 0 : adapter._RemoveQueued(queuePage * queueLabels.Length + slot);
            SetStatus(result == 1 ? "キューから削除しました" : result == 2 ? "操作権限がありません" : "接続先を確認してください");
            _RefreshQueue();
        }
        public void _RefreshQueue()
        {
            int count = adapter == null ? 0 : adapter._QueueCount();
            if (queueButtonLabel != null) queueButtonLabel.text = "キュー (" + count + ")";
            if (queueLabels.Length == 0) return;
            int pages = Mathf.Max(1, (count + queueLabels.Length - 1) / queueLabels.Length);
            queuePage = Mathf.Clamp(queuePage, 0, pages - 1);
            if (queuePageLabel != null) queuePageLabel.text = count == 0 ? "キューは空です" : (queuePage + 1) + " / " + pages;
            for (int i = 0; i < queueLabels.Length; i++)
            {
                int index = queuePage * queueLabels.Length + i;
                bool visible = index < count;
                if (i < queueRows.Length && queueRows[i] != null) queueRows[i].SetActive(visible);
                if (queueLabels[i] != null) queueLabels[i].text = visible ? (index + 1) + ". " + adapter._QueueTitle(index) : "";
            }
        }
        public string _AudioLinkStatus()
        {
            if (!audioLinkInstalled) return "AudioLink: 未導入";
            if (audioLinks.Length == 0) return "AudioLink: 未配置";
            if (adapter == null || !adapter._Available()) return "AudioLink: プレイヤー未接続";
            if (connectedAudioSources.Length == 0) return "AudioLink: 音源未設定";
            for (int i = 0; i < audioLinks.Length; i++)
            {
                var link = audioLinks[i];
                if (link == null || !link.enabled || !link.gameObject.activeInHierarchy) continue;
                var source = (AudioSource)link.GetProgramVariable("audioSource");
                var right = (AudioSource)link.GetProgramVariable("optionalRightAudioSource");
                for (int j = 0; j < connectedAudioSources.Length; j++)
                    if (connectedAudioSources[j] != null && (connectedAudioSources[j] == source || connectedAudioSources[j] == right))
                        return "AudioLink: 接続中";
            }
            return "AudioLink: 未接続";
        }
        public void _ChangeVolume()
        {
            if (VRC.SDK3.UdonNetworkCalling.NetworkCalling.InNetworkCall || volumeSlider == null) return;
            int result = adapter == null ? 0 : adapter._SetVolume(Mathf.Clamp01(volumeSlider.value));
            if (result != 1) SetStatus("音量を変更できません。接続先を確認してください");
            _RefreshVolume();
        }
        public void _StopPlayback()
        {
            if (VRC.SDK3.UdonNetworkCalling.NetworkCalling.InNetworkCall) return;
            int result = adapter == null ? 0 : adapter._ControlPlayback(true);
            SetStatus(result == 1 ? "停止を要求しました" : result == 2 ? "操作権限がありません" : "接続先を確認してください");
        }
        public void _StartPlayback()
        {
            if (VRC.SDK3.UdonNetworkCalling.NetworkCalling.InNetworkCall || adapter == null || !adapter._Available()) { SetStatus("接続先を確認してください"); return; }
            if (adapter._QueueCount() > 0 && VRCUrl.IsNullOrEmpty(adapter._CurrentUrl())) {
                int queuedResult = adapter._PlayQueueNext();
                SetStatus(queuedResult == 1 ? "キューの再生を要求しました" : queuedResult == 2 ? "操作権限がありません" : queuedResult == 7 ? "読み込み間隔を空けて再試行してください" : "キューを再生できません");
                return;
            }
            if (!VRCUrl.IsNullOrEmpty(adapter._CurrentUrl()))
            {
                int result = adapter._ControlPlayback(false);
                if (result != 4)
                {
                    SetStatus(result == 1 ? "再生を要求しました" : result == 2 ? "操作権限がありません" : "接続先を確認してください");
                    return;
                }
            }
            if (selectedVideo >= 0 && selectedVideo < videoTabs.Length && videoTabs[selectedVideo] == _SelectedTab()) { _SelectVideo(selectedVideo); return; }
            for (int i = 0; i < videoTabs.Length; i++)
                if (videoTabs[i] == _SelectedTab() && i < urls.Length && !VRCUrl.IsNullOrEmpty(urls[i])) { _SelectVideo(i); return; }
            SetStatus("このタブに再生できる動画がありません");
        }

        public void _SelectTab(int index)
        {
            if (VRC.SDK3.UdonNetworkCalling.NetworkCalling.InNetworkCall) return;
            if (queuePanel != null) queuePanel.SetActive(false);
            if (queueAddPanel != null) queueAddPanel.SetActive(false);
            if (videoScroll != null) videoScroll.gameObject.SetActive(true);
            if (queueTabImage != null) queueTabImage.color = normalColor;
            if (tabNames == null || tabNames.Length == 0)
            {
                if (heading != null) heading.text = "Playlist";
                SetStatus("Empty playlist / プレイリストが空です");
                return;
            }
            if (index < 0 || index >= tabNames.Length) return;
            selectedTab = index;
            _RefreshPlaybackMode();
            if (heading != null) heading.text = tabNames[index];
            for (int i = 0; i < tabImages.Length; i++)
                if (tabImages[i] != null) tabImages[i].color = i == index ? selectedColor : normalColor;
            int count = 0;
            for (int i = 0; i < videoImages.Length; i++)
            {
                if (videoImages[i] == null) continue;
                bool visible = i < videoTabs.Length && videoTabs[i] == index;
                videoImages[i].gameObject.SetActive(visible);
                if (visible) count++;
            }
            if (videoScroll != null) { videoScroll.StopMovement(); videoScroll.verticalNormalizedPosition = 1; }
            // Scroll bounds may not exist until the first Canvas layout completes.
            SendCustomEventDelayedFrames(nameof(_ResetVideoScroll), 1);
            if (count == 0) SetStatus("No videos in this tab / このタブは空です");
            else if (adapter == null || !adapter._Available()) SetStatus("Not connected / 接続先を設定してください");
            else SetStatus("Select a video / 動画を選択してください");
            for (int i = 0; i < extensions.Length; i++) if (extensions[i] != null) extensions[i]._OnSelectionChanged(index);
        }

        public void _ResetVideoScroll()
        {
            if (VRC.SDK3.UdonNetworkCalling.NetworkCalling.InNetworkCall || videoScroll == null) return;
            videoScroll.StopMovement();
            if (videoScroll.content != null) videoScroll.content.anchoredPosition = new Vector2(videoScroll.content.anchoredPosition.x, 0f);
            videoScroll.verticalNormalizedPosition = 1f;
        }

        public void _SelectVideo(int index)
        {
            if (urls == null || titles == null || index < 0 || index >= urls.Length || index >= titles.Length) { SetStatus("Invalid playlist data / データが不正です"); return; }
            if (VRCUrl.IsNullOrEmpty(urls[index])) { SetStatus("Empty URL / URLが空です"); return; }
            string raw = urls[index].Get();
            if (!raw.StartsWith("https://") && !raw.StartsWith("http://")) { SetStatus("Use an HTTP(S) URL / URLを確認してください"); return; }
            if (adapter == null || !adapter._Available()) { SetStatus("Not connected / 接続先が無効です"); return; }
            int result = adapter._PlayFrom(this, index);
            if (result != 1)
            {
                SetStatus(result == 2 ? "Permission denied / 操作権限がありません" : result == 3 ? "URL rejected / URLが非対応です" : "Player unavailable / 接続先が無効です");
                return;
            }
            _ShowSequenceSelection(index);
            SetStatus("Requested / 再生を要求しました（終了後は同じタブの次の動画へ進みます）");
        }
        public void _ShowSequenceSelection(int index)
        {
            selectedVideo = index;
            for (int i = 0; i < videoImages.Length; i++)
                if (videoImages[i] != null) videoImages[i].color = i == selectedVideo ? selectedColor : normalColor;
        }
        public void _SequenceStatus(string message) { SetStatus(message); }
        public int _NextVideoInTab(int current)
        {
            if (current < 0 || current >= videoTabs.Length) return -1;
            int tab = videoTabs[current];
            for (int i = current + 1; i < videoTabs.Length; i++)
            {
                if (videoTabs[i] != tab) continue;
                if (i >= urls.Length || i >= titles.Length || VRCUrl.IsNullOrEmpty(urls[i])) continue;
                string raw = urls[i].Get();
                if (raw.StartsWith("http://") || raw.StartsWith("https://")) return i;
            }
            return -1;
        }
        private void SetStatus(string message) { if (status != null) status.text = message; }
        public int _SelectedTab() { return selectedTab < 0 ? 0 : selectedTab; }
        public int _StepTab(int direction)
        {
            if (VRC.SDK3.UdonNetworkCalling.NetworkCalling.InNetworkCall || tabNames == null || tabNames.Length == 0) return 4;
            int next = (_SelectedTab() + (direction < 0 ? -1 : 1) + tabNames.Length) % tabNames.Length;
            _SelectTab(next);
            return 1;
        }
        public void _RefreshPlaybackMode()
        {
            bool random = adapter != null ? adapter._IsRandomMode(this, selectedTab)
                : randomTabs != null && selectedTab >= 0 && selectedTab < randomTabs.Length && randomTabs[selectedTab];
            if (playbackModeIcon != null) { playbackModeIcon.sprite = random ? randomIcon : orderedIcon; playbackModeIcon.color = random ? accentColor : iconColor; }
            if (playbackModeLabel != null) playbackModeLabel.text = random ? "ランダム再生" : "順番再生";
        }
        public void _TogglePlaybackMode()
        {
            if (VRC.SDK3.UdonNetworkCalling.NetworkCalling.InNetworkCall) return;
            int result = adapter == null ? 0 : adapter._TogglePlaybackMode(this, selectedTab);
            if (result != 1) SetStatus(result == 2 ? "操作権限がありません" : "接続先を確認してください");
            else SetStatus("再生順を変更しました。再生中の動画はそのまま、次の動画から反映します。");
            _RefreshPlaybackMode();
        }
    }
}
