using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using aki_lua87.AVEC;
using VRC.SDK3.Persistence;


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
        public Text userPageLabel;
        public Text[] userLabels = new Text[0];
        public GameObject[] userRows = new GameObject[0];
        public VRC.SDK3.Components.VRCUrlInputField userUrlInput;
        public PlaylistTitleResolver userTitleResolver;
        // Editor-wired queue affordances, kept serialized for runtime visibility control.
        [HideInInspector] public GameObject[] queueAddButtons = new GameObject[0];
        [System.NonSerialized] public PlaylistUserData localUserData;
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
        public GameObject historyUrlPanel;
        public InputField detailUrlInput;
        public Text detailTitle;
        public GameObject detailQueueButton;
        public GameObject detailRemoveButton;
        public RectTransform detailBackButton;
        private bool detailFromUserList;
        public Text descriptionLabel;
        // Serialized editor wiring for the optional compact list selector; state stays local.
        [HideInInspector] public GameObject listPickerPanel;
        [HideInInspector] public Text listPickerLabel;
        public GameObject loadingIcon;
        // Editor-baked descriptions; response URLs stay local and are never converted to VRCUrl.
        [HideInInspector] public string[] tabDescriptions = new string[0];
        [System.NonSerialized] public string[] resolvedUrls;
        [System.NonSerialized] public bool[] loadingTabs;
        private int detailVideo = -1;
        private bool loadingAnimationScheduled;
        private VRCUrl detailPlaybackUrl = new VRCUrl("");
        private string detailName = "";
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
        // Local visibility only. Editor wiring keeps the optional Remote package independent.
        [HideInInspector] public GameObject[] remoteObjects = new GameObject[0];
        [HideInInspector] public GameObject remoteToggleButton;
        [HideInInspector] public GameObject remoteOffSlash;
        // Persisted editor wiring for local ray-pointer hints; the panel never blocks rays.
        [HideInInspector] public RectTransform controlTip;
        [HideInInspector] public Text controlTipLabel;
        [HideInInspector] public RectTransform[] controlTipTargets = new RectTransform[0];
        private RectTransform activeTipTarget;
        // Editor-baked destination name; persisted because PlaylistSetup is EditorOnly.
        [HideInInspector] public string connectionName = "";
        public GameObject queuePanel;
        public Text queueButtonLabel;
        public Text queuePageLabel;
        public Image queueTabImage;
        public GameObject queueAddPanel;
        public VRC.SDK3.Components.VRCUrlInputField queueUrlInput;
        public Text[] queueLabels = new Text[0];
        public GameObject[] queueRows = new GameObject[0];
        private int queuePage;
        public Color accentColor = new Color(.25f, .86f, .76f);
        public Color iconColor = new Color(.92f, .96f, 1f);
        public ScrollRect videoScroll;
        // Editor-wired, inactive spacers preserve the full scroll extent while rows are windowed.
        [HideInInspector] public LayoutElement videoTopSpacer;
        [HideInInspector] public LayoutElement videoBottomSpacer;
        // RectOffset getters are not exposed to Udon; preserve the authored padding at edit time.
        [HideInInspector] public float videoWindowPaddingTop;
        private int[] windowEntries;
        private float[] windowOffsets;
        private float[] windowHeights;
        private int windowCount;
        private int windowTab = -1;
        private int windowFirst = -1;
        private int windowLast = -1;
        private float windowSpacing;
        private float windowRowsEnd;
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
        private void Start() { _ApplySpecialLists(); SelectInitialList(); _RefreshLocalState(); }
        private void OnEnable() { _RefreshLocalState(); }
        public void _RefreshLocalState()
        {
            if (VRC.SDK3.UdonNetworkCalling.NetworkCalling.InNetworkCall) return;
            if (adapter != null) adapter._NotifyLocalStateChanged();
            _RefreshVolume();
            _RefreshProgress();
        }
        public void _ApplySpecialLists()
        {
            if (VRC.SDK3.UdonNetworkCalling.NetworkCalling.InNetworkCall) return;
            if (status != null) status.gameObject.SetActive(false);
            if (heading != null) heading.gameObject.SetActive(false);
            if (queueTabImage != null) queueTabImage.gameObject.SetActive(enableQueue);
            if (historyTabImage != null) historyTabImage.gameObject.SetActive(enableHistory);
            if (userTabImage != null) userTabImage.gameObject.SetActive(enableUserList);
            for (int i = 0; i < queueAddButtons.Length; i++) if (queueAddButtons[i] != null) queueAddButtons[i].SetActive(false);
            if (detailQueueButton != null) detailQueueButton.SetActive(enableQueue);
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
            _CloseHistoryUrl();
            if (userPanel != null) userPanel.SetActive(false);
            if (userAddPanel != null) userAddPanel.SetActive(false);
            if (userTabImage != null) userTabImage.color = normalColor;
        }
        public void _ToggleUserList()
        {
            if (VRC.SDK3.UdonNetworkCalling.NetworkCalling.InNetworkCall || !enableUserList || userPanel == null) return;
            _CloseListPicker();
            _CloseHistoryUrl();
            selectedTab = -3; _RefreshTabInfo();
            userPanel.SetActive(true);
            if (userAddPanel != null) userAddPanel.SetActive(false);
            if (queuePanel != null) queuePanel.SetActive(false);
            if (historyPanel != null) historyPanel.SetActive(false);
            if (videoScroll != null) videoScroll.gameObject.SetActive(false);
            if (queueTabImage != null) queueTabImage.color = normalColor;
            if (historyTabImage != null) historyTabImage.color = normalColor;
            if (userTabImage != null) userTabImage.color = selectedColor;
            for (int i = 0; i < tabImages.Length; i++) if (tabImages[i] != null) tabImages[i].color = normalColor;
            if (heading != null) heading.text = "保存リスト";
            _RefreshUserList();
            SetStatus(localUserData == null || !localUserData._Ready() ? "保存データを読み込んでいます" : "自分のリストです");
        }
        public void _OpenUserAdd()
        {
            if (VRC.SDK3.UdonNetworkCalling.NetworkCalling.InNetworkCall || !enableUserList || userAddPanel == null) return;
            if (localUserData == null || !localUserData._Ready()) { SetStatus("保存データの読み込み完了をお待ちください"); return; }
            userAddPanel.SetActive(true); _RefreshUserList();
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
            SetStatus(result == 1 ? "保存リストに追加しました" : result == 6 ? "保存データの読み込み完了をお待ちください" : result == 8 ? "このURLは登録済みです" : result == 5 ? "保存リストは最大32件です" : result == 9 ? "保存容量が不足しています。項目を削除してください" : "2048文字以内のhttp(s)の動画URLを入力してください");
            if (result == 1) { userUrlInput.SetUrl(VRCUrl.Empty); _CloseUserAdd(); }
            _RefreshUserList();
        }
        public void _ClickUserSlot(int slot)
        {
            if (VRC.SDK3.UdonNetworkCalling.NetworkCalling.InNetworkCall || !enableUserList || localUserData == null || !localUserData._Ready() || slot < 0 || slot >= userLabels.Length) return;
            int index = slot;
            if (index >= localUserData._Count()) return;
            OpenDetails(-1, localUserData._Url(index), localUserData._Title(index), "", true);
        }
        public void _RefreshUserList()
        {
            int count = localUserData == null || !localUserData._Ready() ? 0 : localUserData._Count();
            if (userLabels.Length == 0) return;
            if (userPageLabel != null) userPageLabel.text = localUserData == null || !localUserData._Ready() ? "読み込み中…" : count == 0 ? "リストは空です" : count + "件";
            for (int i = 0; i < userLabels.Length; i++)
            {
                int index = i;
                bool visible = index < count && (userAddPanel == null || !userAddPanel.activeSelf);
                if (i < userRows.Length && userRows[i] != null) userRows[i].SetActive(visible);
                if (userLabels[i] != null) userLabels[i].text = visible ? localUserData._Title(index) : "";
            }
        }
        public void _PollVolume()
        {
            // Compatibility entry point; periodic polling now belongs to the shared adapter.
            _RefreshLocalState();
        }
        private bool remotePreferencesRestored;
        private bool remotePreferenceChanged;
        public override void OnPlayerRestored(VRCPlayerApi player)
        {
            if (!Utilities.IsValid(player) || !player.isLocal || remotePreferencesRestored) return;
            remotePreferencesRestored = true;
            for (int i = 0; i < remoteObjects.Length; i++)
            {
                var remote = remoteObjects[i];
                if (remote == null) continue;
                var behaviour = (VRC.Udon.UdonBehaviour)remote.GetComponent(typeof(VRC.Udon.UdonBehaviour));
                if (behaviour != null) behaviour.SendCustomEvent("_RestoreSummonPreference");
                string key = RemotePreferenceKey(remote.transform);
                if (remotePreferenceChanged) PlayerData.SetBool(key, remote.activeSelf);
                else
                {
                    bool saved;
                    if (PlayerData.TryGetBool(player, key, out saved)) SetRemoteActive(remote, saved);
                }
            }
            _RefreshRemoteToggle();
            if (adapter != null) adapter._NotifyLocalStateChanged();
        }
        private string RemotePreferenceKey(Transform node)
        {
            string path = "";
            while (node != null)
            {
                path = "/" + node.GetSiblingIndex() + ":" + node.name.Length + ":" + node.name + path;
                node = node.parent;
            }
            return "AVEC.Remote" + path + "/Enabled";
        }
        private void SetRemoteActive(GameObject remote, bool value)
        {
            // Drop our held remote before disabling its entire object and input loop.
            var pickup = (VRC.SDK3.Components.VRCPickup)remote.GetComponent(typeof(VRC.SDK3.Components.VRCPickup));
            if (!value && pickup != null && pickup.IsHeld && Utilities.IsValid(pickup.currentPlayer) && pickup.currentPlayer.isLocal) pickup.Drop();
            if (remote.activeSelf != value) remote.SetActive(value);
        }
        public void _ToggleRemote()
        {
            if (VRC.SDK3.UdonNetworkCalling.NetworkCalling.InNetworkCall) return;
            bool enabled = false;
            for (int i = 0; i < remoteObjects.Length; i++)
                if (remoteObjects[i] != null && remoteObjects[i].activeSelf) enabled = true;
            for (int i = 0; i < remoteObjects.Length; i++)
            {
                var remote = remoteObjects[i];
                if (remote == null) continue;
                SetRemoteActive(remote, !enabled);
                if (remotePreferencesRestored) PlayerData.SetBool(RemotePreferenceKey(remote.transform), !enabled);
            }
            // Preserve input made while persistence is still loading; never write early.
            remotePreferenceChanged = true;
            _RefreshRemoteToggle();
            if (adapter != null) adapter._NotifyLocalStateChanged();
        }
        public void _ShowPlaybackTip() { ShowControlTip(0, "再生/一時停止"); }
        public void _ShowPlaybackModeTip() { ShowControlTip(1, "順送り/シャッフル/リピート"); }
        public void _ShowRemoteTip() { ShowControlTip(2, "リモコンON/OFF"); }
        public void _ShowMuteTip() { ShowControlTip(3, "ミュートON/OFF"); }
        private void ShowControlTip(int index, string caption)
        {
            if (VRC.SDK3.UdonNetworkCalling.NetworkCalling.InNetworkCall || controlTip == null || controlTipLabel == null ||
                index < 0 || index >= controlTipTargets.Length) return;
            var target = controlTipTargets[index];
            if (target == null || !target.gameObject.activeInHierarchy) return;
            var root = (RectTransform)transform;
            var top = root.InverseTransformPoint(target.TransformPoint(new Vector3(target.rect.center.x, target.rect.yMax, 0)));
            var bottom = root.InverseTransformPoint(target.TransformPoint(new Vector3(target.rect.center.x, target.rect.yMin, 0)));
            float halfWidth = controlTip.rect.width * .5f;
            float halfHeight = controlTip.rect.height * .5f;
            float x = Mathf.Clamp(top.x, root.rect.xMin + halfWidth + 8, root.rect.xMax - halfWidth - 8);
            float y = top.y + halfHeight + 8;
            if (y + halfHeight > root.rect.yMax - 8) y = bottom.y - halfHeight - 8;
            controlTip.position = root.TransformPoint(new Vector3(x, y, 0));
            controlTipLabel.text = caption;
            activeTipTarget = target;
            controlTip.SetAsLastSibling();
            controlTip.gameObject.SetActive(true);
        }
        public void _HideControlTip()
        {
            if (VRC.SDK3.UdonNetworkCalling.NetworkCalling.InNetworkCall) return;
            activeTipTarget = null;
            if (controlTip != null) controlTip.gameObject.SetActive(false);
        }
        private void OnDisable() { _HideControlTip(); }
        public void _RefreshRemoteToggle()
        {
            if (VRC.SDK3.UdonNetworkCalling.NetworkCalling.InNetworkCall) return;
            bool available = false;
            bool enabled = false;
            for (int i = 0; i < remoteObjects.Length; i++)
            {
                if (remoteObjects[i] == null) continue;
                available = true;
                if (remoteObjects[i].activeSelf) enabled = true;
            }
            if (remoteToggleButton != null && remoteToggleButton.activeSelf != available) remoteToggleButton.SetActive(available);
            if (remoteOffSlash != null && remoteOffSlash.activeSelf != !enabled) remoteOffSlash.SetActive(!enabled);
        }
        public void _RefreshVolume()
        {
            _RefreshRemoteToggle();
            if (activeTipTarget != null && !activeTipTarget.gameObject.activeInHierarchy) _HideControlTip();
            _RefreshPlaybackMode();
            if (connectionLabel != null) connectionLabel.text = "接続先: " + (adapter == null || !adapter.localAvailable ? "未接続" : connectionName);
            if (volumeIcon != null) volumeIcon.sprite = adapter != null && adapter.localMuted ? mutedIcon : speakerIcon;
            if (transportIcon != null) transportIcon.sprite = adapter != null && adapter.localPlaying ? pauseIcon : playIcon;
            float value = adapter == null ? -1f : adapter.localVolume;
            if (volumeSlider != null) { volumeSlider.interactable = value >= 0f; if (value >= 0f) volumeSlider.SetValueWithoutNotify(value); }
            if (volumeLabel != null) volumeLabel.text = value < 0f ? "—" : Mathf.RoundToInt(value * 100f) + "%";
        }
        public void _EnqueueVideo(int index)
        {
            if (VRC.SDK3.UdonNetworkCalling.NetworkCalling.InNetworkCall || !enableQueue) return;
            int result = adapter == null ? 0 : adapter._EnqueueFrom(this, index);
            SetStatus(result == 1 ? "キューに追加しました" : result == 2 ? "操作権限がありません" : result == 5 ? "キューは最大32件です" : "キューへ追加できません。接続先・URLを確認してください。");
            if (result == 1) _CloseHistoryUrl();
            _RefreshQueue();
        }
        public void _ToggleQueue()
        {
            if (VRC.SDK3.UdonNetworkCalling.NetworkCalling.InNetworkCall || !enableQueue || queuePanel == null) return;
            _CloseListPicker();
            HideUserList();
            queuePanel.SetActive(true);
            if (historyPanel != null) historyPanel.SetActive(false);
            if (historyTabImage != null) historyTabImage.color = normalColor;
            if (queueAddPanel != null) queueAddPanel.SetActive(false);
            if (videoScroll != null) videoScroll.gameObject.SetActive(false);
            selectedTab = -1; _RefreshTabInfo();
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
            SetStatus(result == 1 ? (adapter._QueueAdvancePending() ? "削除しました。読み込み制限の解除後に次の動画へ進みます" : "キューから削除しました") : result == 2 ? "操作権限がありません" : "接続先を確認してください");
            _RefreshQueue();
        }
        public void _SkipQueuedPlayback()
        {
            if (VRC.SDK3.UdonNetworkCalling.NetworkCalling.InNetworkCall || !enableQueue) return;
            int result = adapter == null ? 0 : adapter._SkipToNextQueued();
            SetStatus(result == 1 ? (adapter._QueueAdvancePending() ? "現在の動画を終了しました。読み込み制限の解除後に次へ進みます" : adapter._QueueCount() == 0 ? "最後のキューの再生を終了しました" : "次のキューの再生を要求しました") : result == 2 ? "操作権限がありません" : result == 4 ? "次に再生するキューがありません" : result == 7 ? "動画を読み込み中です。読み込み後に再試行してください" : "接続先を確認してください");
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
            _RefreshProgress();
        }
        public void _ToggleHistory()
        {
            if (VRC.SDK3.UdonNetworkCalling.NetworkCalling.InNetworkCall || !enableHistory || historyPanel == null) return;
            _CloseListPicker();
            HideUserList();
            selectedTab = -2; _RefreshTabInfo();
            historyPanel.SetActive(true);
            if (queuePanel != null) queuePanel.SetActive(false);
            if (videoScroll != null) videoScroll.gameObject.SetActive(false);
            if (queueAddPanel != null) queueAddPanel.SetActive(false);
            if (heading != null) heading.text = "再生履歴";
            if (queueTabImage != null) queueTabImage.color = normalColor;
            if (historyTabImage != null) historyTabImage.color = selectedColor;
            for (int i = 0; i < tabImages.Length; i++) if (tabImages[i] != null) tabImages[i].color = normalColor;
            _RefreshHistory();
            SetStatus(adapter == null || !adapter._Available() ? "接続先を確認してください" : adapter._HistoryCount() == 0 ? "再生履歴はありません" : "履歴を選択してください（最新32件）");
        }
        public void _HistoryNextPage() { if (VRC.SDK3.UdonNetworkCalling.NetworkCalling.InNetworkCall) return; historyPage++; _RefreshHistory(); }
        public void _HistoryPreviousPage() { if (VRC.SDK3.UdonNetworkCalling.NetworkCalling.InNetworkCall) return; historyPage--; _RefreshHistory(); }
        public void _PlayHistorySlot(int slot)
        {
            if (VRC.SDK3.UdonNetworkCalling.NetworkCalling.InNetworkCall || !enableHistory || adapter == null || slot < 0 || slot >= historyLabels.Length) return;
            int index = historyPage * historyLabels.Length + slot;
            if (index >= adapter._HistoryCount()) { SetStatus("この動画は再生履歴にありません"); return; }
            VRCUrl url = adapter._HistoryUrl(index);
            if (VRCUrl.IsNullOrEmpty(url)) { SetStatus("再生履歴のURLを取得できませんでした"); return; }
            string title = adapter._HistoryTitle(index);
            string copyUrl = adapter._HistoryCopyUrl(index);
            OpenDetails(-1, url, title, copyUrl, false);
        }
        private void OpenDetails(int index, VRCUrl url, string title, string displayUrl, bool fromUserList)
        {
            if (historyUrlPanel == null || detailUrlInput == null) { SetStatus("動画詳細画面の接続先を確認してください"); return; }
            if (VRCUrl.IsNullOrEmpty(url)) { SetStatus("動画のURLを取得できませんでした"); return; }
            detailVideo = index; detailPlaybackUrl = url; detailName = title;
            detailFromUserList = fromUserList; RefreshDetailUserAction();
            if (detailTitle != null) detailTitle.text = title;
            detailUrlInput.text = string.IsNullOrEmpty(displayUrl) ? url.Get() : displayUrl;
            if (detailQueueButton != null) detailQueueButton.SetActive(enableQueue);
            historyUrlPanel.transform.SetAsLastSibling();
            historyUrlPanel.SetActive(true);
        }
        public void _PlayDetails()
        {
            if (VRC.SDK3.UdonNetworkCalling.NetworkCalling.InNetworkCall || adapter == null) return;
            if (detailVideo >= 0) { PlayVideo(detailVideo); return; }
            RefreshSavedDetailTitle();
            int result = adapter._PlayUserUrl(detailPlaybackUrl, detailName);
            SetStatus(result == 1 ? "再生を要求しました" : result == 2 ? "操作権限がありません" : result == 7 ? "読み込み間隔を空けて再試行してください" : "接続先・URLを確認してください");
            if (result == 1) _CloseHistoryUrl();
        }
        public void _EnqueueDetails()
        {
            if (VRC.SDK3.UdonNetworkCalling.NetworkCalling.InNetworkCall || !enableQueue || adapter == null) return;
            if (detailVideo >= 0) { _EnqueueVideo(detailVideo); return; }
            RefreshSavedDetailTitle();
            int result = adapter._EnqueueTitledUrl(detailPlaybackUrl, detailName);
            SetStatus(result == 1 ? "キューに追加しました" : result == 2 ? "操作権限がありません" : result == 5 ? "キューは最大32件です" : "キューへ追加できませんでした");
            if (result == 1) _CloseHistoryUrl();
        }
        // Metadata can arrive while the details screen is already open.
        private void RefreshSavedDetailTitle()
        {
            if (!detailFromUserList || localUserData == null || VRCUrl.IsNullOrEmpty(detailPlaybackUrl)) return;
            for (int i = 0; i < localUserData._Count(); i++)
                if (!VRCUrl.IsNullOrEmpty(localUserData._Url(i)) && localUserData._Url(i).Get() == detailPlaybackUrl.Get())
                {
                    detailName = localUserData._Title(i);
                    if (detailTitle != null) detailTitle.text = detailName;
                    return;
                }
        }
        private void RefreshDetailUserAction()
        {
            bool show = detailFromUserList && enableUserList;
            if (detailRemoveButton != null) detailRemoveButton.SetActive(show);
            if (detailBackButton == null) return;
            float width = ((RectTransform)detailBackButton.parent).rect.width;
            detailBackButton.sizeDelta = new Vector2(show ? (width - 32) / 2 : width - 24, detailBackButton.sizeDelta.y);
            detailBackButton.anchoredPosition = new Vector2(show ? (width + 8) / 2 : 12, detailBackButton.anchoredPosition.y);
        }
        public void _RemoveUserDetails()
        {
            if (VRC.SDK3.UdonNetworkCalling.NetworkCalling.InNetworkCall || !enableUserList || !detailFromUserList || historyUrlPanel == null || !historyUrlPanel.activeSelf) return;
            if (localUserData == null || !localUserData._Ready()) { SetStatus("保存データの読み込み完了をお待ちください"); return; }
            // Resolve by URL again so list changes cannot delete a different row.
            int index = -1;
            for (int i = 0; i < localUserData._Count(); i++)
                if (localUserData._Url(i).Get() == detailPlaybackUrl.Get()) { index = i; break; }
            int result = index < 0 ? 3 : localUserData._Remove(index);
            if (result == 1) { _CloseHistoryUrl(); _RefreshUserList(); }
            SetStatus(result == 1 ? "保存リストから削除しました" : result == 6 ? "保存データの読み込み完了をお待ちください" : "この動画はリストにありません");
        }
        public void _FocusHistoryUrl()
        {
            if (VRC.SDK3.UdonNetworkCalling.NetworkCalling.InNetworkCall || detailUrlInput == null) return;
            detailUrlInput.Select(); detailUrlInput.ActivateInputField();
        }
        public void _CloseHistoryUrl()
        {
            if (VRC.SDK3.UdonNetworkCalling.NetworkCalling.InNetworkCall) return;
            detailFromUserList = false; RefreshDetailUserAction();
            if (detailUrlInput != null) detailUrlInput.DeactivateInputField();
            if (historyUrlPanel != null && historyUrlPanel.activeSelf) historyUrlPanel.SetActive(false);
        }
        public void _RefreshTabInfo()
        {
            if (VRC.SDK3.UdonNetworkCalling.NetworkCalling.InNetworkCall) return;
            if (listPickerLabel != null)
            {
                string name = "リストを選択";
                if (selectedTab >= 0 && tabNames != null && selectedTab < tabNames.Length) name = tabNames[selectedTab];
                else if (selectedTab == -1) name = "再生キュー";
                else if (selectedTab == -2) name = "再生履歴";
                else if (selectedTab == -3) name = "保存リスト";
                listPickerLabel.text = name + "  ▾";
            }
            if (descriptionLabel != null)
            {
                string description = "";
                if (selectedTab >= 0 && selectedTab < tabDescriptions.Length) description = tabDescriptions[selectedTab];
                else if (selectedTab == -2) description = "最近再生した動画（最新32件）";
                else if (selectedTab == -3) description = "ワールドに保存されるリスト";
                descriptionLabel.text = string.IsNullOrEmpty(description) ? "" : description.Replace("\r", " ").Replace("\n", " ");
            }
            if (loadingIcon != null)
            {
                bool show = selectedTab >= 0 && loadingTabs != null && selectedTab < loadingTabs.Length && loadingTabs[selectedTab];
                loadingIcon.SetActive(show);
                if (show && !loadingAnimationScheduled) { loadingAnimationScheduled = true; SendCustomEventDelayedSeconds(nameof(_AnimateLoading), .1f); }
            }
        }
        public void _ToggleListPicker()
        {
            if (VRC.SDK3.UdonNetworkCalling.NetworkCalling.InNetworkCall || listPickerPanel == null) return;
            bool show = !listPickerPanel.activeSelf;
            if (show) listPickerPanel.transform.SetAsLastSibling();
            listPickerPanel.SetActive(show);
        }
        public void _CloseListPicker()
        {
            if (VRC.SDK3.UdonNetworkCalling.NetworkCalling.InNetworkCall || listPickerPanel == null) return;
            listPickerPanel.SetActive(false);
        }
        public void _AnimateLoading()
        {
            if (VRC.SDK3.UdonNetworkCalling.NetworkCalling.InNetworkCall) return;
            if (loadingIcon == null || !loadingIcon.activeInHierarchy) { loadingAnimationScheduled = false; return; }
            loadingIcon.transform.Rotate(0f, 0f, -30f);
            SendCustomEventDelayedSeconds(nameof(_AnimateLoading), .1f);
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
            if (VRC.SDK3.UdonNetworkCalling.NetworkCalling.InNetworkCall) return;
            _RefreshProgress();
            if (videoTopSpacer != null && videoBottomSpacer != null && videoScroll != null && videoScroll.content != null)
            {
                var layout = videoScroll.content.GetComponent<VerticalLayoutGroup>();
                if (layout != null && layout.enabled)
                {
                    if (windowEntries == null || windowEntries.Length != videoImages.Length)
                    {
                        windowEntries = new int[videoImages.Length];
                        windowOffsets = new float[videoImages.Length];
                        windowHeights = new float[videoImages.Length];
                    }
                    windowCount = 0; windowFirst = -1; windowLast = -1; windowTab = selectedTab;
                    windowSpacing = layout.spacing;
                    float offset = videoWindowPaddingTop;
                    // Build the selected tab once. Scroll callbacks only search these cached offsets.
                    for (int i = 0; i < videoImages.Length; i++)
                    {
                        if (videoImages[i] == null) continue;
                        videoImages[i].gameObject.SetActive(false);
                        if (selectedTab < 0 || i >= videoTabs.Length || videoTabs[i] != selectedTab || !_EntryVisible(i)) continue;
                        var rect = videoImages[i].rectTransform;
                        // LayoutUtility ignores inactive behaviours, but offscreen rows are deliberately inactive.
                        var element = videoImages[i].GetComponent<LayoutElement>();
                        float height = layout.childControlHeight && element != null
                            ? Mathf.Max(element.minHeight, element.preferredHeight) : rect.rect.height;
                        if (height < 0f) height = rect.rect.height;
                        height = Mathf.Max(1f, height);
                        windowEntries[windowCount] = i;
                        windowOffsets[windowCount] = offset;
                        windowHeights[windowCount] = height;
                        offset += height + windowSpacing;
                        windowCount++;
                    }
                    windowRowsEnd = windowCount > 0 ? offset - windowSpacing : offset;
                    _RefreshVideoWindow();
                    return;
                }
            }
            // Older, unmigrated instances remain usable.
            for (int i = 0; i < videoImages.Length; i++) if (videoImages[i] != null)
                videoImages[i].gameObject.SetActive(selectedTab >= 0 && i < videoTabs.Length && videoTabs[i] == selectedTab && _EntryVisible(i));
        }

        // Wired to ScrollRect.onValueChanged as a local SendCustomEvent, preserving other listeners.
        public void _RefreshVideoWindow()
        {
            if (VRC.SDK3.UdonNetworkCalling.NetworkCalling.InNetworkCall || windowEntries == null || windowTab != selectedTab || videoTopSpacer == null || videoBottomSpacer == null) return;
            if (windowCount == 0)
            {
                videoTopSpacer.gameObject.SetActive(false); videoBottomSpacer.gameObject.SetActive(false);
                return;
            }
            float top = Mathf.Max(0f, videoScroll.content.anchoredPosition.y);
            float height = videoScroll.viewport != null ? videoScroll.viewport.rect.height : 0f;
            if (height <= 0f) height = ((RectTransform)videoScroll.transform).rect.height;
            float bottom = top + Mathf.Max(1f, height);
            int lo = 0, hi = windowCount;
            while (lo < hi)
            {
                int mid = (lo + hi) / 2;
                if (windowOffsets[mid] + windowHeights[mid] < top) lo = mid + 1; else hi = mid;
            }
            int first = Mathf.Max(0, Mathf.Min(windowCount - 1, lo) - 5);
            lo = 0; hi = windowCount;
            while (lo < hi)
            {
                int mid = (lo + hi) / 2;
                if (windowOffsets[mid] <= bottom) lo = mid + 1; else hi = mid;
            }
            int last = Mathf.Min(windowCount - 1, Mathf.Max(0, lo - 1) + 5);
            if (first == windowFirst && last == windowLast) return;
            for (int i = windowFirst; i >= 0 && i <= windowLast; i++)
                if (i < first || i > last) videoImages[windowEntries[i]].gameObject.SetActive(false);
            for (int i = first; i <= last; i++)
                if (i < windowFirst || i > windowLast) videoImages[windowEntries[i]].gameObject.SetActive(true);
            videoTopSpacer.preferredHeight = first > 0 ? Mathf.Max(0f, windowOffsets[first] - windowOffsets[0] - windowSpacing) : 0f;
            videoBottomSpacer.preferredHeight = last < windowCount - 1 ? Mathf.Max(0f, windowRowsEnd - windowOffsets[last] - windowHeights[last] - windowSpacing) : 0f;
            videoTopSpacer.gameObject.SetActive(first > 0);
            videoBottomSpacer.gameObject.SetActive(last < windowCount - 1);
            windowFirst = first; windowLast = last;
        }
        private string progressUrl = "";
        private int progressSelection = -2;
        private int progressVideo = -1;
        private int shownVideoProgress = -1;
        private int shownQueueProgress = -1;
        private bool progressInitialized;
        public void _RefreshProgress()
        {
            if (!progressInitialized)
            {
                progressInitialized = true;
                for (int i = 0; i < videoProgress.Length; i++) if (videoProgress[i] != null) videoProgress[i].gameObject.SetActive(false);
                for (int i = 0; i < queueProgress.Length; i++) if (queueProgress[i] != null) queueProgress[i].gameObject.SetActive(false);
            }
            float progress = adapter == null ? -1f : adapter.localProgress;
            VRCUrl current = progress >= 0f ? adapter.localUrl : VRCUrl.Empty;
            string raw = VRCUrl.IsNullOrEmpty(current) ? "" : current.Get();
            if (raw != progressUrl || progressSelection != selectedVideo)
            {
                progressUrl = raw; progressSelection = selectedVideo; progressVideo = -1;
                if (raw.Length > 0)
                {
                    if (selectedVideo >= 0 && selectedVideo < urls.Length && !VRCUrl.IsNullOrEmpty(urls[selectedVideo]) && urls[selectedVideo].Get() == raw)
                        progressVideo = selectedVideo;
                    else for (int i = 0; i < urls.Length; i++)
                        if (!VRCUrl.IsNullOrEmpty(urls[i]) && urls[i].Get() == raw) { progressVideo = i; break; }
                }
            }
            bool queuePlaying = adapter != null && adapter._QueueIsPlaying(0);
            int video = !queuePlaying && progress >= 0f && progressVideo >= 0 && progressVideo < videoProgress.Length && _EntryVisible(progressVideo) ? progressVideo : -1;
            int queued = progress >= 0f && queuePage == 0 && queuePlaying && queueProgress.Length > 0 ? 0 : -1;
            if (shownVideoProgress != video)
            {
                if (shownVideoProgress >= 0 && videoProgress[shownVideoProgress] != null) videoProgress[shownVideoProgress].gameObject.SetActive(false);
                shownVideoProgress = video;
                if (video >= 0 && videoProgress[video] != null) videoProgress[video].gameObject.SetActive(true);
            }
            if (shownQueueProgress != queued)
            {
                if (shownQueueProgress >= 0 && queueProgress[shownQueueProgress] != null) queueProgress[shownQueueProgress].gameObject.SetActive(false);
                shownQueueProgress = queued;
                if (queued >= 0 && queueProgress[queued] != null) queueProgress[queued].gameObject.SetActive(true);
            }
            if (video >= 0 && videoProgress[video] != null) videoProgress[video].fillAmount = progress;
            if (queued >= 0 && queueProgress[queued] != null) queueProgress[queued].fillAmount = progress;
        }
        public void _ChangeVolume()
        {
            if (VRC.SDK3.UdonNetworkCalling.NetworkCalling.InNetworkCall || volumeSlider == null) return;
            int result = adapter == null ? 0 : adapter._ChangeLocalVolume(Mathf.Clamp01(volumeSlider.value));
            if (result != 1) SetStatus("音量を変更できません。接続先を確認してください");
            _RefreshVolume();
        }
        public void _ToggleMute()
        {
            if (VRC.SDK3.UdonNetworkCalling.NetworkCalling.InNetworkCall) return;
            int result = adapter == null ? 0 : adapter._ChangeLocalMute(!adapter._GetMute());
            SetStatus(result == 1 ? (adapter._GetMute() ? "ミュートしました" : "ミュートを解除しました") : result == 2 ? "操作権限がありません" : "接続先を確認してください");
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
            int result = adapter == null ? 0 : adapter._ChangeLocalPlayback(true);
            SetStatus(result == 1 ? "一時停止を要求しました" : result == 2 ? "操作権限がありません" : "接続先を確認してください");
        }
        public void _StartPlayback()
        {
            if (VRC.SDK3.UdonNetworkCalling.NetworkCalling.InNetworkCall) return;
            int result = adapter == null ? 0 : adapter._ChangeLocalPlayback(false);
            SetStatus(result == 1 ? "再開を要求しました" : result == 2 ? "操作権限がありません" : result == 4 ? "再開できる動画がありません。動画を選択してください" : "接続先を確認してください");
        }

        public void _SelectTab(int index)
        {
            if (VRC.SDK3.UdonNetworkCalling.NetworkCalling.InNetworkCall) return;
            _CloseListPicker();
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
                if (visible) count++;
            }
            if (videoScroll != null) { videoScroll.StopMovement(); videoScroll.verticalNormalizedPosition = 1; }
            _RefreshVisibleEntries();
            // Scroll bounds may not exist until the first Canvas layout completes.
            SendCustomEventDelayedFrames(nameof(_ResetVideoScroll), 1);
            if (count == 0) SetStatus("No videos in this tab / このタブは空です");
            else if (adapter == null || !adapter._Available()) SetStatus("Not connected / 接続先を設定してください");
            else SetStatus("Select a video / 動画を選択してください");
            for (int i = 0; i < extensions.Length; i++) if (extensions[i] != null) extensions[i]._OnSelectionChanged(index);
            _RefreshTabInfo();
        }

        public void _ResetVideoScroll()
        {
            if (VRC.SDK3.UdonNetworkCalling.NetworkCalling.InNetworkCall || videoScroll == null) return;
            videoScroll.StopMovement();
            if (videoScroll.content != null) videoScroll.content.anchoredPosition = new Vector2(videoScroll.content.anchoredPosition.x, 0f);
            videoScroll.verticalNormalizedPosition = 1f;
            _RefreshVideoWindow();
        }

        public void _SelectVideo(int index)
        {
            if (VRC.SDK3.UdonNetworkCalling.NetworkCalling.InNetworkCall || index < 0 || index >= urls.Length || index >= titles.Length || !_EntryVisible(index)) return;
            if (selectedTab >= 0 && loadingTabs != null && selectedTab < loadingTabs.Length && loadingTabs[selectedTab]) return;
            string display = resolvedUrls != null && index < resolvedUrls.Length ? resolvedUrls[index] : "";
            // Do not expose the redirect URL as the actual URL before a dynamic response arrives.
            if (index < dynamicTitleIndices.Length && dynamicTitleIndices[index] >= 0 && string.IsNullOrEmpty(display)) return;
            OpenDetails(index, urls[index], titles[index], display, false);
        }
        private void PlayVideo(int index)
        {
            if (VRC.SDK3.UdonNetworkCalling.NetworkCalling.InNetworkCall) return;
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
            _CloseHistoryUrl();
            _ShowSequenceSelection(index);
            SetStatus("Requested / 再生を要求しました（終了後は同じタブの次の動画へ進みます）");
        }
        public void _ShowSequenceSelection(int index)
        {
            selectedVideo = index;
            for (int i = 0; i < videoImages.Length; i++)
                if (videoImages[i] != null) videoImages[i].color = i == selectedVideo ? selectedColor : normalColor;
            _RefreshProgress();
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
