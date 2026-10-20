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
        public bool enableQueue = true;
        public bool enableHistory = true;
        public bool enableUserList = true;
        public GameObject userPanel;
        public GameObject userAddPanel;
        public Image userTabImage;
        public Text userDeleteLabel;
        public Text userPageLabel;
        public Text[] userLabels = new Text[0];
        public GameObject[] userRows = new GameObject[0];
        public VRC.SDK3.Components.VRCUrlInputField userUrlInput;
        public PlaylistTitleResolver userTitleResolver;
        // Editor-wired queue affordances, kept serialized for runtime visibility control.
        [HideInInspector] public GameObject[] queueAddButtons = new GameObject[0];
        [System.NonSerialized] public PlaylistUserData localUserData;
        private bool userDeleteMode;
        public Text heading;
        public Text status;
        public Text playbackModeLabel;
        public Image playbackModeIcon;
        public Sprite orderedIcon;
        public Sprite randomIcon;
        public Sprite repeatIcon;
        public GameObject historyPanel;
        public Text historyButtonLabel;
        public Text historyPageLabel;
        public Image historyTabImage;
        public Text[] historyLabels = new Text[0];
        public GameObject[] historyRows = new GameObject[0];
        // Persisted references to row progress bars, created without rebuilding rows.
        [HideInInspector] public Image[] videoProgress = new Image[0];
        [HideInInspector] public Image[] queueProgress = new Image[0];
        [System.NonSerialized] public bool[] hiddenEntries;
        private int historyPage;
        public Slider volumeSlider;
        public Text volumeLabel;
        public Image volumeIcon;
        public Sprite speakerIcon;
        public Sprite mutedIcon;
        public Image transportIcon;
        public Sprite playIcon;
        public Sprite pauseIcon;
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
        private void Start() { _ApplySpecialLists(); SelectInitialList(); _RefreshVolume(); SendCustomEventDelayedSeconds(nameof(_PollVolume), .5f); }
        public void _ApplySpecialLists()
        {
            if (VRC.SDK3.UdonNetworkCalling.NetworkCalling.InNetworkCall) return;
            if (queueTabImage != null) queueTabImage.gameObject.SetActive(enableQueue);
            if (historyTabImage != null) historyTabImage.gameObject.SetActive(enableHistory);
            if (userTabImage != null) userTabImage.gameObject.SetActive(enableUserList);
            for (int i = 0; i < queueAddButtons.Length; i++) if (queueAddButtons[i] != null) queueAddButtons[i].SetActive(enableQueue);
            if (!enableQueue && queuePanel != null) queuePanel.SetActive(false);
            if (!enableHistory && historyPanel != null) historyPanel.SetActive(false);
            if (!enableUserList && userPanel != null) userPanel.SetActive(false);
        }
        private void SelectInitialList()
        {
            if (enableQueue && queuePanel != null) _ToggleQueue();
            else if (enableHistory && historyPanel != null) _ToggleHistory();
            else if (enableUserList && userPanel != null) _ToggleUserList();
            else _SelectTab(0);
        }
        private void HideUserList()
        {
            if (userPanel != null) userPanel.SetActive(false);
            if (userAddPanel != null) userAddPanel.SetActive(false);
            if (userTabImage != null) userTabImage.color = normalColor;
            userDeleteMode = false;
        }
        public void _ToggleUserList()
        {
            if (VRC.SDK3.UdonNetworkCalling.NetworkCalling.InNetworkCall || !enableUserList || userPanel == null) return;
            selectedTab = -3; userDeleteMode = false;
            userPanel.SetActive(true);
            if (userAddPanel != null) userAddPanel.SetActive(false);
            if (queuePanel != null) queuePanel.SetActive(false);
            if (historyPanel != null) historyPanel.SetActive(false);
            if (videoScroll != null) videoScroll.gameObject.SetActive(false);
            if (queueTabImage != null) queueTabImage.color = normalColor;
            if (historyTabImage != null) historyTabImage.color = normalColor;
            if (userTabImage != null) userTabImage.color = selectedColor;
            for (int i = 0; i < tabImages.Length; i++) if (tabImages[i] != null) tabImages[i].color = normalColor;
            if (heading != null) heading.text = "ユーザーリスト";
            _RefreshUserList();
            SetStatus(localUserData == null || !localUserData._Ready() ? "保存データを読み込んでいます" : "自分のリストです。動画を選択すると再生します");
        }
        public void _OpenUserAdd()
        {
            if (VRC.SDK3.UdonNetworkCalling.NetworkCalling.InNetworkCall || !enableUserList || userAddPanel == null) return;
            if (localUserData == null || !localUserData._Ready()) { SetStatus("保存データの読み込み完了をお待ちください"); return; }
            userDeleteMode = false; userAddPanel.SetActive(true); _RefreshUserList();
        }
        public void _CloseUserAdd()
        {
            if (VRC.SDK3.UdonNetworkCalling.NetworkCalling.InNetworkCall || userAddPanel == null) return;
            userAddPanel.SetActive(false); _RefreshUserList();
        }
        public void _SubmitUserUrl()
        {
            if (VRC.SDK3.UdonNetworkCalling.NetworkCalling.InNetworkCall || !enableUserList || userUrlInput == null) return;
            int result = localUserData == null ? 6 : localUserData._Add(userUrlInput.GetUrl(), userTitleResolver);
            SetStatus(result == 1 ? "ユーザーリストに追加しました" : result == 6 ? "保存データの読み込み完了をお待ちください" : result == 8 ? "このURLは登録済みです" : result == 5 ? "ユーザーリストは最大32件です" : result == 9 ? "保存容量が不足しています。項目を削除してください" : "2048文字以内のhttp(s)の動画URLを入力してください");
            if (result == 1) { userUrlInput.SetUrl(VRCUrl.Empty); _CloseUserAdd(); }
            _RefreshUserList();
        }
        public void _ToggleUserDelete()
        {
            if (VRC.SDK3.UdonNetworkCalling.NetworkCalling.InNetworkCall || !enableUserList) return;
            userDeleteMode = !userDeleteMode; _CloseUserAdd(); _RefreshUserList();
            SetStatus(userDeleteMode ? "削除する動画を選択してください" : "動画を選択すると再生します");
        }
        public void _ClickUserSlot(int slot)
        {
            if (VRC.SDK3.UdonNetworkCalling.NetworkCalling.InNetworkCall || !enableUserList || localUserData == null || !localUserData._Ready() || slot < 0 || slot >= userLabels.Length) return;
            int index = slot;
            if (index >= localUserData._Count()) return;
            if (userDeleteMode) { localUserData._Remove(index); _RefreshUserList(); SetStatus("ユーザーリストから削除しました"); return; }
            int result = adapter == null ? 0 : adapter._PlayUserUrl(localUserData._Url(index), localUserData._Title(index));
            SetStatus(result == 1 ? "再生を要求しました" : result == 2 ? "操作権限がありません" : result == 7 ? "読み込み間隔を空けて再試行してください" : "動画を再生できません。接続先・URLを確認してください");
        }
        public void _RefreshUserList()
        {
            int count = localUserData == null || !localUserData._Ready() ? 0 : localUserData._Count();
            if (userDeleteLabel != null) userDeleteLabel.text = userDeleteMode ? "削除を終了" : "削除モード";
            if (userLabels.Length == 0) return;
            if (userPageLabel != null) userPageLabel.text = localUserData == null || !localUserData._Ready() ? "読み込み中…" : count == 0 ? "リストは空です" : count + "件";
            for (int i = 0; i < userLabels.Length; i++)
            {
                int index = i;
                bool visible = index < count && (userAddPanel == null || !userAddPanel.activeSelf);
                if (i < userRows.Length && userRows[i] != null) userRows[i].SetActive(visible);
                if (userLabels[i] != null) userLabels[i].text = visible ? (userDeleteMode ? "× " : "") + localUserData._Title(index) : "";
            }
        }
        public void _PollVolume()
        {
            if (VRC.SDK3.UdonNetworkCalling.NetworkCalling.InNetworkCall) return;
            _RefreshVolume();
            SendCustomEventDelayedSeconds(nameof(_PollVolume), .5f);
        }
        public void _RefreshVolume()
        {
            _RefreshQueue();
            _RefreshHistory();
            _RefreshProgress();
            _RefreshPlaybackMode();
            if (connectionLabel != null) connectionLabel.text = adapter == null || !adapter._Available() ? "未接続" : connectionName;
            if (audioLinkLabel != null) audioLinkLabel.text = _AudioLinkStatus();
            if (volumeIcon != null) volumeIcon.sprite = adapter != null && adapter._GetMute() ? mutedIcon : speakerIcon;
            if (transportIcon != null) transportIcon.sprite = adapter != null && adapter._IsPlaybackRunning() ? pauseIcon : playIcon;
            float value = adapter == null ? -1f : adapter._GetVolume();
            if (volumeSlider != null) { volumeSlider.interactable = value >= 0f; if (value >= 0f) volumeSlider.SetValueWithoutNotify(value); }
            if (volumeLabel != null) volumeLabel.text = value < 0f ? "—" : Mathf.RoundToInt(value * 100f) + "%";
        }
        public void _EnqueueVideo(int index)
        {
            if (VRC.SDK3.UdonNetworkCalling.NetworkCalling.InNetworkCall || !enableQueue) return;
            int result = adapter == null ? 0 : adapter._EnqueueFrom(this, index);
            SetStatus(result == 1 ? "キューに追加しました" : result == 2 ? "操作権限がありません" : result == 5 ? "キューは最大32件です" : "キューへ追加できません。接続先・URLを確認してください。");
            _RefreshQueue();
        }
        public void _ToggleQueue()
        {
            if (VRC.SDK3.UdonNetworkCalling.NetworkCalling.InNetworkCall || !enableQueue || queuePanel == null) return;
            HideUserList();
            queuePanel.SetActive(true);
            if (historyPanel != null) historyPanel.SetActive(false);
            if (historyTabImage != null) historyTabImage.color = normalColor;
            if (queueAddPanel != null) queueAddPanel.SetActive(false);
            if (videoScroll != null) videoScroll.gameObject.SetActive(false);
            selectedTab = -1;
            if (heading != null) heading.text = "再生キュー";
            if (queueTabImage != null) queueTabImage.color = selectedColor;
            for (int i = 0; i < tabImages.Length; i++) if (tabImages[i] != null) tabImages[i].color = normalColor;
            _RefreshQueue();
            SetStatus(adapter == null || !adapter._Available() ? "接続先を確認してください" : adapter._QueueCount() == 0 ? "動画を追加してください" : "追加した順に再生します");
        }
        public void _OpenQueueAdd()
        {
            if (VRC.SDK3.UdonNetworkCalling.NetworkCalling.InNetworkCall || !enableQueue || queueAddPanel == null) return;
            queueAddPanel.SetActive(true);
            _RefreshQueue();
        }
        public void _CloseQueueAdd()
        {
            if (VRC.SDK3.UdonNetworkCalling.NetworkCalling.InNetworkCall || queueAddPanel == null) return;
            queueAddPanel.SetActive(false);
            _RefreshQueue();
        }
        public void _SubmitQueueUrl()
        {
            if (VRC.SDK3.UdonNetworkCalling.NetworkCalling.InNetworkCall || !enableQueue || queueUrlInput == null) return;
            int result = adapter == null ? 0 : adapter._EnqueueUrl(queueUrlInput.GetUrl());
            SetStatus(result == 1 ? "キューに追加しました" : result == 2 ? "操作権限がありません" : result == 5 ? "キューは最大32件です" : "http(s)の動画URLを入力してください");
            if (result == 1) { queueUrlInput.SetUrl(VRCUrl.Empty); _CloseQueueAdd(); }
            _RefreshQueue();
        }
        public void _QueueNextPage() { if (VRC.SDK3.UdonNetworkCalling.NetworkCalling.InNetworkCall) return; queuePage++; _RefreshQueue(); }
        public void _QueuePreviousPage() { if (VRC.SDK3.UdonNetworkCalling.NetworkCalling.InNetworkCall) return; queuePage--; _RefreshQueue(); }
        public void _ClearQueue()
        {
            if (VRC.SDK3.UdonNetworkCalling.NetworkCalling.InNetworkCall || !enableQueue) return;
            int result = adapter == null ? 0 : adapter._ClearQueue();
            SetStatus(result == 1 ? "待機中のキューを消去しました" : result == 2 ? "操作権限がありません" : "接続先を確認してください");
            _RefreshQueue();
        }
        public void _StartQueuedPlayback()
        {
            if (VRC.SDK3.UdonNetworkCalling.NetworkCalling.InNetworkCall || !enableQueue) return;
            int result = adapter == null ? 0 : adapter._PlayQueueNext();
            SetStatus(result == 1 ? "キューの再生を要求しました" : result == 2 ? "操作権限がありません" : result == 7 ? "読み込み間隔を空けて再試行してください" : result == 4 ? "キューは空です" : "接続先を確認してください");
        }
        public void _RemoveQueueSlot(int slot)
        {
            if (VRC.SDK3.UdonNetworkCalling.NetworkCalling.InNetworkCall || !enableQueue || queueLabels.Length == 0) return;
            int result = adapter == null ? 0 : adapter._RemoveQueued(queuePage * queueLabels.Length + slot);
            SetStatus(result == 1 ? "キューから削除しました" : result == 2 ? "操作権限がありません" : result == 4 ? "再生中の項目は終了後に削除されます" : "接続先を確認してください");
            _RefreshQueue();
        }
        public void _RefreshQueue()
        {
            int count = adapter == null ? 0 : adapter._QueueCount();
            if (queueButtonLabel != null) queueButtonLabel.text = "再生キュー";
            if (queueLabels.Length == 0) return;
            int pages = Mathf.Max(1, (count + queueLabels.Length - 1) / queueLabels.Length);
            queuePage = Mathf.Clamp(queuePage, 0, pages - 1);
            if (queuePageLabel != null) queuePageLabel.text = "";
            for (int i = 0; i < queueLabels.Length; i++)
            {
                int index = queuePage * queueLabels.Length + i;
                bool visible = index < count && (queueAddPanel == null || !queueAddPanel.activeSelf);
                if (i < queueRows.Length && queueRows[i] != null) queueRows[i].SetActive(visible);
                if (queueLabels[i] != null) queueLabels[i].text = visible ? adapter._QueueDisplayTitle(index) : "";
            }
        }
        public void _ToggleHistory()
        {
            if (VRC.SDK3.UdonNetworkCalling.NetworkCalling.InNetworkCall || !enableHistory || historyPanel == null) return;
            HideUserList();
            selectedTab = -2;
            historyPanel.SetActive(true);
            if (queuePanel != null) queuePanel.SetActive(false);
            if (videoScroll != null) videoScroll.gameObject.SetActive(false);
            if (queueAddPanel != null) queueAddPanel.SetActive(false);
            if (heading != null) heading.text = "再生履歴";
            if (queueTabImage != null) queueTabImage.color = normalColor;
            if (historyTabImage != null) historyTabImage.color = selectedColor;
            for (int i = 0; i < tabImages.Length; i++) if (tabImages[i] != null) tabImages[i].color = normalColor;
            _RefreshHistory();
            SetStatus("履歴を選択すると再生します（最新32件）");
        }
        public void _HistoryNextPage() { if (VRC.SDK3.UdonNetworkCalling.NetworkCalling.InNetworkCall) return; historyPage++; _RefreshHistory(); }
        public void _HistoryPreviousPage() { if (VRC.SDK3.UdonNetworkCalling.NetworkCalling.InNetworkCall) return; historyPage--; _RefreshHistory(); }
        public void _PlayHistorySlot(int slot)
        {
            if (VRC.SDK3.UdonNetworkCalling.NetworkCalling.InNetworkCall || !enableHistory) return;
            int result = adapter == null ? 0 : adapter._PlayHistory(historyPage * historyLabels.Length + slot);
            SetStatus(result == 1 ? "履歴の再生を要求しました" : result == 2 ? "操作権限がありません" : result == 7 ? "読み込み間隔を空けて再試行してください" : "履歴を再生できません");
        }
        public void _RefreshHistory()
        {
            int count = adapter == null ? 0 : adapter._HistoryCount();
            if (historyButtonLabel != null) historyButtonLabel.text = "再生履歴";
            if (historyLabels.Length == 0) return;
            int pages = Mathf.Max(1, (count + historyLabels.Length - 1) / historyLabels.Length);
            historyPage = Mathf.Clamp(historyPage, 0, pages - 1);
            if (historyPageLabel != null) historyPageLabel.text = "";
            for (int i = 0; i < historyLabels.Length; i++)
            {
                int index = historyPage * historyLabels.Length + i;
                if (i < historyRows.Length && historyRows[i] != null) historyRows[i].SetActive(index < count);
                if (historyLabels[i] != null) historyLabels[i].text = index < count ? adapter._HistoryDisplayTitle(index) : "";
            }
        }
        public bool _EntryVisible(int index) { return hiddenEntries == null || index >= hiddenEntries.Length || !hiddenEntries[index]; }
        public void _RefreshVisibleEntries()
        {
            for (int i = 0; i < videoImages.Length; i++) if (videoImages[i] != null)
                videoImages[i].gameObject.SetActive(i < videoTabs.Length && videoTabs[i] == selectedTab && _EntryVisible(i));
        }
        public void _RefreshProgress()
        {
            float progress = adapter == null || !adapter._Available() ? -1f : adapter._GetProgress();
            for (int i = 0; i < videoProgress.Length; i++)
            {
                var bar = videoProgress[i]; if (bar == null) continue;
                bool playing = progress >= 0f && adapter != null && adapter._IsPlayingEntry(this, i);
                bar.gameObject.SetActive(playing); if (playing) bar.fillAmount = progress;
            }
            for (int i = 0; i < queueProgress.Length; i++)
            {
                var bar = queueProgress[i]; if (bar == null) continue;
                bool playing = progress >= 0f && adapter != null && adapter._QueueIsPlaying(queuePage * queueLabels.Length + i);
                bar.gameObject.SetActive(playing); if (playing) bar.fillAmount = progress;
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
        public void _ToggleMute()
        {
            if (VRC.SDK3.UdonNetworkCalling.NetworkCalling.InNetworkCall) return;
            int result = adapter == null ? 0 : adapter._SetMute(!adapter._GetMute());
            SetStatus(result == 1 ? (adapter._GetMute() ? "ミュートしました（自分のみ）" : "ミュートを解除しました（自分のみ）") : result == 2 ? "操作権限がありません" : "接続先を確認してください");
            _RefreshVolume();
        }
        public void _TogglePlayback()
        {
            if (VRC.SDK3.UdonNetworkCalling.NetworkCalling.InNetworkCall) return;
            if (adapter != null && adapter._IsPlaybackRunning()) _StopPlayback(); else _StartPlayback();
            _RefreshVolume();
        }
        public void _StopPlayback()
        {
            if (VRC.SDK3.UdonNetworkCalling.NetworkCalling.InNetworkCall) return;
            int result = adapter == null ? 0 : adapter._ControlPlayback(true);
            SetStatus(result == 1 ? "一時停止を要求しました" : result == 2 ? "操作権限がありません" : "接続先を確認してください");
        }
        public void _StartPlayback()
        {
            if (VRC.SDK3.UdonNetworkCalling.NetworkCalling.InNetworkCall) return;
            int result = adapter == null ? 0 : adapter._ControlPlayback(false);
            SetStatus(result == 1 ? "再開を要求しました" : result == 2 ? "操作権限がありません" : result == 4 ? "再開できる動画がありません。動画を選択してください" : "接続先を確認してください");
        }

        public void _SelectTab(int index)
        {
            if (VRC.SDK3.UdonNetworkCalling.NetworkCalling.InNetworkCall) return;
            HideUserList();
            if (queuePanel != null) queuePanel.SetActive(false);
            if (historyPanel != null) historyPanel.SetActive(false);
            if (historyTabImage != null) historyTabImage.color = normalColor;
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
                bool visible = i < videoTabs.Length && videoTabs[i] == index && _EntryVisible(i);
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
                if (videoTabs[i] != tab || !_EntryVisible(i)) continue;
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
            bool repeat = adapter != null && adapter._IsRepeatMode();
            if (playbackModeIcon != null) { playbackModeIcon.sprite = repeat ? repeatIcon : random ? randomIcon : orderedIcon; playbackModeIcon.color = repeat || random ? accentColor : iconColor; }
            if (playbackModeLabel != null) playbackModeLabel.text = repeat ? "1動画リピート" : random ? "ランダム再生" : "順番再生";
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
