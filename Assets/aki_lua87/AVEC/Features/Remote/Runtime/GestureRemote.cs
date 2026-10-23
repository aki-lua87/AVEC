using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;
using VRC.SDK3.UdonNetworkCalling;

namespace aki_lua87.CommonGestureRemote
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class GestureRemote : UdonSharpBehaviour
    {
        public Transform tip;
        public LineRenderer trail;
        public Text label;
        public RemoteTarget playerTarget;
        public RemoteTarget playlistTarget;
        [Tooltip("呼び出すオブジェクトのTransform。未設定なら連携中のPlaylistタブレットを呼び出します。") ]
        public Transform tabletToSummon;
        public UdonBehaviour customEventTarget;
        [Min(.01f)] public float minimumStroke = .055f;
        [Range(1.05f, 3f)] public float directionBias = 1.35f;
        [Min(1f)] public float timeout = 8f;
        [Range(.01f, 1f)] public float volumeStep = .1f;
        public bool scaleWithAvatar = true;
        public Color drawingColor = new Color(.3f, .75f, 1f);
        public Color matchedColor = new Color(.3f, 1f, .65f);
        public Transform helpPanel;
        public Text helpCommands;
        public Transform helpHint;
        public RectTransform helpStick;
        public CanvasGroup helpHintGroup;
        public Image holdProgress;
        public Text holdCaption;
        [Min(.5f)] public float helpHoldSeconds = 4f / 3f;
        [Min(1f)] public float helpIdleSeconds = 5f;
        [Tooltip("右スティックの下長押しで、このリモコンを手元へ呼び出せるようにします。ジェスチャーで自分のON/OFFを切り替えられます。ガイド表示には影響しません。")]
        public bool summonEnabled = true;
        [Tooltip("呼び出しを許可するBoxCollider。未設定ならどこからでも呼び出せます。リモコンの子ではなく、部屋などの固定オブジェクトに配置してください。")]
        public BoxCollider summonArea;
        [Min(1f)] public float summonHoldSeconds = 1.5f;
        public Vector3 helpOffset = new Vector3(.18f, .16f, .08f);
        [Min(.0001f)] public float helpUIScale = .00065f;
        // Editor-baked, persisted arrays; shared authoring data never enters Udon.
        [HideInInspector] public string[] gestures = new string[0];
        [HideInInspector] public string[] labels = new string[0];
        [HideInInspector] public int[] actions = new int[0];
        [HideInInspector] public string[] customEvents = new string[0];
        private bool held;
        private bool drawing;
        private bool rejected;
        private bool desktop;
        private Quaternion basis;
        private Vector3 headOrigin;
        private Vector3 startTip;
        private Vector2 anchor;
        private Vector3 strokeTip;
        private string path = "";
        private string lastDirection = "";
        private int match = -1;
        private float threshold;
        private float startedAt;
        private float hideAt;
        private int pointCount;
        private Vector3 lastTrailPoint;
        private const int MaxTrailPoints = 256;
        private const int MaxDirections = 3;
        private float helpAxis;
        private float rightStickAxis;
        private float helpDownSince = -1f;
        private float lastActivity;
        private bool helpShown;
        private bool helpSeen;
        private bool helpConsumed;
        private float summonDownSince = -1f;
        private bool summonConsumed;
        private Transform[] helpMenuLines = new Transform[3];
        private bool[] helpMenuLineVisible = new bool[3];
        private Transform helpCallIcon;

        private void Start()
        {
            if (helpHint != null) helpCallIcon = helpHint.Find("Remote call icon");
            // Keep the guide pictogram, but omit it from the remote call feedback.
            if (helpHint != null) for (int i = 0; i < helpMenuLines.Length; i++)
            {
                helpMenuLines[i] = helpHint.Find("Command line " + i);
                if (helpMenuLines[i] != null) helpMenuLineVisible[i] = helpMenuLines[i].gameObject.activeSelf;
            }
            ClearVisuals();
            HideHelp();
            var pickup = (VRC.SDK3.Components.VRCPickup)GetComponent(typeof(VRC.SDK3.Components.VRCPickup));
            if (pickup != null && Utilities.IsValid(Networking.LocalPlayer))
                pickup.AutoHold = Networking.LocalPlayer.IsUserInVR() ? VRC_Pickup.AutoHoldMode.No : VRC_Pickup.AutoHoldMode.Yes;
        }
        public override void OnPickup()
        {
            if (NetworkCalling.InNetworkCall) return;
            held = true; helpSeen = false; helpAxis = 0f;
            lastActivity = Time.time;
            HideHelp();
            helpConsumed = true; // A stick held during pickup must return to neutral first.
        }
        public override void OnDrop() { held = false; Cancel(); HideHelp(); }
        public override void InputLookVertical(float value, VRC.Udon.Common.UdonInputEventArgs args)
        {
            if (NetworkCalling.InNetworkCall || !Utilities.IsValid(Networking.LocalPlayer) || !Networking.LocalPlayer.IsUserInVR()) return;
            if (args.handType != VRC.Udon.Common.HandType.RIGHT) return;
            helpAxis = value;
        }
        public override void OnPickupUseDown()
        {
            if (NetworkCalling.InNetworkCall || !held || drawing || tip == null || !Utilities.IsValid(Networking.LocalPlayer)) return;
            lastActivity = Time.time; helpDownSince = -1f;
            if (helpHint != null) helpHint.gameObject.SetActive(false);
            var head = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
            basis = head.rotation;
            headOrigin = head.position;
            startTip = tip.position;
            desktop = !Networking.LocalPlayer.IsUserInVR();
            strokeTip = startTip;
            float scale = scaleWithAvatar && !desktop ? Mathf.Clamp(Networking.LocalPlayer.GetAvatarEyeHeightAsMeters() / 1.6f, .25f, 3f) : 1f;
            threshold = Mathf.Max(.01f, minimumStroke) * scale;
            BeginStroke();
            startedAt = Time.time;
            drawing = true;
            pointCount = 0;
            if (trail != null) { trail.positionCount = 0; trail.enabled = true; }
            AddTrail(startTip);
            Show("ジェスチャーを入力", drawingColor);
        }
        public override void OnPickupUseUp()
        {
            if (NetworkCalling.InNetworkCall || !drawing) return;
            lastActivity = Time.time;
            if (!held || !Utilities.IsValid(Networking.LocalPlayer) || Time.time - startedAt > timeout) { Cancel(); return; }
            SampleInput();
            drawing = false;
            if (trail != null) trail.enabled = false;
            if (rejected || match < 0 || match >= labels.Length) { Show("キャンセル / 一致なし", drawingColor); hideAt = Time.time + 1f; return; }
            int selected = match;
            match = -1; // Consume BEFORE dispatch: repeated releases cannot repeat the action.
            int result = Execute(selected);
            string message = result == 1 ? labels[selected] + " / 操作しました" :
                result == 2 ? "操作権限がありません" : result == 3 ? "設定・URLを確認してください" :
                result == 4 ? "対象の動画がありません" : result == 5 ? "接続・画面設定を確認してください" :
                result == 6 ? "自分のプレイヤーがOFFです" : result == 7 ? "読み込み待機中です" : "接続先が無効です";
            if (result == 1 && actions[selected] == 22) message = summonEnabled ? "呼び出し：ON" : "呼び出し：OFF";
            if (actions[selected] == 23 && result == 4) message = "タブレットが設置されていません";
            if (actions[selected] == 23 && result == 2) message = "タブレットは使用中です";
            Show(message, result == 1 ? matchedColor : new Color(1f, .55f, .3f));
            hideAt = Time.time + 1.8f;
        }
        public override void PostLateUpdate()
        {
            rightStickAxis = helpAxis;
            if (Utilities.IsValid(Networking.LocalPlayer) && Networking.LocalPlayer.IsUserInVR())
            {
                // Some controller bindings do not deliver the vertical look event.
                // Keep remapped Udon input, with the SDK's physical right-stick axis as fallback.
                float physicalAxis = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryThumbstickVertical");
                if (Mathf.Abs(physicalAxis) > Mathf.Abs(rightStickAxis)) rightStickAxis = physicalAxis;
            }
            UpdateSummon();
            UpdateHelp();
            UpdateHoldHint();
            if (drawing)
            {
                if (!held || !Utilities.IsValid(Networking.LocalPlayer) || Time.time - startedAt > timeout) { Cancel(); return; }
                SampleInput();
            }
            if (label == null || !label.gameObject.activeSelf) return;
            if (!drawing && Time.time >= hideAt) { ClearVisuals(); return; }
            if (tip != null) label.transform.position = (drawing || desktop ? strokeTip : tip.position) + (basis * Vector3.up) * .07f;
            if (Utilities.IsValid(Networking.LocalPlayer))
                label.transform.rotation = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation;
        }
        private void UpdateSummon()
        {
            if (!summonEnabled) { summonDownSince = -1f; summonConsumed = false; return; }
            if (!Utilities.IsValid(Networking.LocalPlayer) || !Networking.LocalPlayer.IsUserInVR()) { summonDownSince = -1f; summonConsumed = false; return; }
            if (held || rightStickAxis > -.65f) { summonDownSince = -1f; if (rightStickAxis > -.65f) summonConsumed = false; return; }
            // Check the current player position on every held-input frame. Leaving the area
            // cancels progress, including after a teleport or respawn; no trigger state can go stale.
            if (!WithinSummonArea()) { summonDownSince = -1f; return; }
            if (summonConsumed) return;
            var pickup = (VRC.SDK3.Components.VRCPickup)GetComponent(typeof(VRC.SDK3.Components.VRCPickup));
            if (pickup == null || pickup.IsHeld) { summonDownSince = -1f; return; }
            if (summonDownSince < 0f) summonDownSince = Time.time;
            if (Time.time - summonDownSince < summonHoldSeconds) return;
            summonConsumed = true; summonDownSince = -1f;
            var sync = GetComponent<VRC.SDK3.Components.VRCObjectSync>();
            if (sync != null)
            {
                Networking.SetOwner(Networking.LocalPlayer, gameObject);
                if (!Networking.IsOwner(gameObject)) return;
                sync.FlagDiscontinuity();
            }
            var hand = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand);
            // Bring the grip near the hand without forcing a pickup or changing its scale.
            var body = GetComponent<Rigidbody>();
            if (body != null) { body.velocity = Vector3.zero; body.angularVelocity = Vector3.zero; }
            transform.SetPositionAndRotation(hand.position + hand.rotation * new Vector3(0f, 0f, .08f), hand.rotation);
        }
        private bool WithinSummonArea()
        {
            if (summonArea == null) return true;
            if (!summonArea.enabled || !summonArea.gameObject.activeInHierarchy) return false;
            // A range attached to a movable remote would follow the summoned remote.
            if (summonArea.transform == transform || summonArea.transform.IsChildOf(transform)) return false;
            // Feet lie on the floor/box boundary and can sink slightly below it.
            // Test the body instead, including small avatars and seated players.
            Vector3 bodyPosition = Networking.LocalPlayer.GetPosition() + Vector3.up *
                Mathf.Clamp(Networking.LocalPlayer.GetAvatarEyeHeightAsMeters() * .5f, .1f, .75f);
            Vector3 point = summonArea.transform.InverseTransformPoint(bodyPosition) - summonArea.center;
            Vector3 half = summonArea.size * .5f;
            return Mathf.Abs(point.x) <= half.x && Mathf.Abs(point.y) <= half.y && Mathf.Abs(point.z) <= half.z;
        }
        private void UpdateHelp()
        {
            if (!held || !Utilities.IsValid(Networking.LocalPlayer)) return;
            bool vr = Networking.LocalPlayer.IsUserInVR();
            bool down = vr ? rightStickAxis <= -.65f : Input.GetKey(KeyCode.H);
            if (vr ? rightStickAxis > -.35f : !down) helpConsumed = false;
            if (drawing || !down) helpDownSince = -1f;
            else if (!helpConsumed)
            {
                if (helpDownSince < 0f) helpDownSince = Time.time;
                lastActivity = Time.time;
                if (helpPanel != null)
                {
                    helpConsumed = true; helpDownSince = -1f;
                    _RefreshCommandGuide();
                    helpShown = !helpShown; helpSeen = true;
                    helpPanel.gameObject.SetActive(helpShown);
                }
            }
            if (!helpShown) return;
            var head = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
            float scale = scaleWithAvatar && vr ? Mathf.Clamp(Networking.LocalPlayer.GetAvatarEyeHeightAsMeters() / 1.6f, .25f, 3f) : 1f;
            Vector3 origin = vr && tip != null ? tip.position : head.position + head.rotation * new Vector3(0, -.18f, .65f);
            if (helpShown && helpPanel != null)
            {
                helpPanel.position = origin + head.rotation * (helpOffset * scale);
                helpPanel.rotation = head.rotation;
                helpPanel.localScale = Vector3.one * (helpUIScale * scale);
            }
        }
        private void UpdateHoldHint()
        {
            if (helpHint == null) return;
            bool valid = Utilities.IsValid(Networking.LocalPlayer);
            bool vr = valid && Networking.LocalPlayer.IsUserInVR();
            bool helpHolding = held && !drawing && helpDownSince >= 0f && !helpConsumed;
            bool summonHolding = !held && summonEnabled && summonDownSince >= 0f && !summonConsumed;
            bool idleHint = vr && held && !drawing && !helpSeen && !helpShown && rightStickAxis > -.65f && Time.time - lastActivity >= helpIdleSeconds;
            bool visible = valid && (helpHolding || summonHolding || idleHint);
            if (helpHint.gameObject.activeSelf != visible) helpHint.gameObject.SetActive(visible);
            float progress = helpHolding ? (Time.time - helpDownSince) / Mathf.Max(.01f, helpHoldSeconds) :
                summonHolding ? (Time.time - summonDownSince) / Mathf.Max(.01f, summonHoldSeconds) : 0f;
            if (holdProgress != null) holdProgress.fillAmount = Mathf.Clamp01(progress);
            if (!visible) return;
            if (holdCaption != null) holdCaption.text = summonHolding ? "呼出" : helpShown ? "閉じる" : "ガイド";
            if (helpCallIcon != null) helpCallIcon.gameObject.SetActive(summonHolding);
            for (int i = 0; i < helpMenuLines.Length; i++)
                if (helpMenuLines[i] != null) helpMenuLines[i].gameObject.SetActive(!summonHolding && helpMenuLineVisible[i]);
            var head = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
            float scale = scaleWithAvatar && vr ? Mathf.Clamp(Networking.LocalPlayer.GetAvatarEyeHeightAsMeters() / 1.6f, .25f, 3f) : 1f;
            // Summon feedback must stay in view even while the remote is far away.
            Vector3 origin = held && vr && tip != null ? tip.position : head.position + head.rotation * new Vector3(0f, -.18f * scale, .65f * scale);
            {
                helpHint.position = origin + head.rotation * new Vector3(.1f * scale, .09f * scale, .04f * scale);
                helpHint.rotation = head.rotation;
                helpHint.localScale = Vector3.one * (helpUIScale * scale);
                float cycle = Mathf.Repeat(Time.time - lastActivity - helpIdleSeconds, 2.4f);
                float tilt = helpHolding || summonHolding ? 1f : Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(cycle / .65f));
                if (idleHint && cycle > 1.9f) tilt = 1f - Mathf.SmoothStep(0f, 1f, (cycle - 1.9f) / .5f);
                if (helpStick != null) helpStick.anchoredPosition = new Vector2(-65f, 12f - 22f * tilt);
                if (helpHintGroup != null) helpHintGroup.alpha = .7f + .3f * tilt;
            }
        }
        private void HideHelp()
        {
            helpShown = false; helpDownSince = -1f; helpConsumed = false;
            if (holdProgress != null) holdProgress.fillAmount = 0f;
            if (helpPanel != null) helpPanel.gameObject.SetActive(false);
            if (helpHint != null) helpHint.gameObject.SetActive(false);
        }
        private void SampleInput()
        {
            if (tip == null || !Utilities.IsValid(Networking.LocalPlayer)) return;
            var head = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
            Vector3 relative;
            if (desktop)
            {
                // Mouse-look sweeps a virtual point; translations do not become gestures.
                Vector3 direction = Quaternion.Inverse(basis) * (head.rotation * Vector3.forward);
                relative = new Vector3(Mathf.Atan2(direction.x, direction.z), Mathf.Asin(Mathf.Clamp(direction.y, -1f, 1f)), 0);
            }
            else relative = Quaternion.Inverse(basis) * (tip.position - startTip - (head.position - headOrigin));
            SamplePoint(new Vector2(relative.x, relative.y));
            strokeTip = desktop ? startTip + basis * new Vector3(relative.x, relative.y, 0) : tip.position;
            AddTrail(strokeTip);
        }
        // Pure geometric recognizer, also exercised by the Editor regression suite.
        private void BeginStroke()
        {
            anchor = Vector2.zero; path = ""; lastDirection = ""; match = -1; rejected = false;
        }
        private void SamplePoint(Vector2 point)
        {
            if (rejected) return;
            Vector2 delta = point - anchor;
            float x = Mathf.Abs(delta.x), y = Mathf.Abs(delta.y);
            string direction = "";
            if (x >= threshold && x >= y * directionBias) direction = delta.x > 0 ? "R" : "L";
            else if (y >= threshold && y >= x * directionBias) direction = delta.y > 0 ? "U" : "D";
            // Ambiguous diagonals do not choose an arbitrary axis or execute an earlier match.
            else if (Mathf.Max(x, y) >= threshold) { match = -1; Show("方向をはっきり描いてください", drawingColor); return; }
            if (direction.Length == 0) return;
            anchor = point;
            if (direction == lastDirection) { UpdateMatch(); return; }
            if (path.Length >= MaxDirections) { rejected = true; match = -1; Show("長すぎます / キャンセル", drawingColor); return; }
            path += direction;
            lastDirection = direction;
            UpdateMatch();
        }
        private void UpdateMatch()
        {
            match = -1;
            for (int i = 0; i < gestures.Length; i++) if (gestures[i] == path && i < actions.Length && _ActionAvailable(actions[i])) { match = i; break; }
            string arrows = path.Replace("R", "→").Replace("L", "←").Replace("U", "↑").Replace("D", "↓");
            Show(match >= 0 && match < labels.Length ? arrows + "  " + labels[match] : arrows + "  …", match >= 0 ? matchedColor : drawingColor);
        }
        private int Execute(int index)
        {
            if (index < 0 || index >= actions.Length || index >= labels.Length) return 3;
            int action = actions[index];
            if (!_ActionAvailable(action)) return 0;
            if (action == 23 && tabletToSummon != null) return SummonTablet();
            if ((action == 1 || action == 2) && (playlistTarget == null || !playlistTarget._CanStep())) return playerTarget == null ? 0 : playerTarget._StepNative(action == 1 ? 1 : -1);
            // This is a local preference for this remote; no player connection or ownership is needed.
            if (action == 22)
            {
                summonEnabled = !summonEnabled;
                summonDownSince = -1f;
                summonConsumed = false;
                return 1;
            }
            if (action == 15)
            {
                if (customEventTarget == null || index >= customEvents.Length || string.IsNullOrEmpty(customEvents[index])) return 0;
                customEventTarget.SendCustomEvent(customEvents[index]);
                return 1;
            }
            bool sequence = action == 1 || action == 2 || action == 9 || action == 10 || action == 11 || action == 18 || action == 20 || action == 23;
            bool repeat = action == 16 || action == 19;
            RemoteTarget target = sequence || (repeat && playlistTarget != null && playlistTarget._Ready()) ? playlistTarget : playerTarget;
            return target == null ? 0 : target._Execute(action, volumeStep);
        }
        public bool _ActionAvailable(int action)
        {
            if (action == 1 || action == 2) return (playlistTarget != null && playlistTarget._CanStep()) || (playerTarget != null && playerTarget._CanStep());
            if (action == 9 || action == 10 || action == 11 || action == 18 || action == 20) return playlistTarget != null && playlistTarget._Ready();
            return true;
        }
        public void _RefreshCommandGuide()
        {
            if (NetworkCalling.InNetworkCall || helpCommands == null) return;
            string text = "";
            for (int i = 0; i < gestures.Length; i++)
            {
                if (i >= actions.Length || i >= labels.Length || !_ActionAvailable(actions[i])) continue;
                if (text.Length > 0) text += "\n";
                text += gestures[i].Replace("R", "→").Replace("L", "←").Replace("U", "↑").Replace("D", "↓") + "   " + labels[i];
            }
            helpCommands.text = text;
        }
        private int SummonTablet()
        {
            var player = Networking.LocalPlayer;
            if (!Utilities.IsValid(player) || !tabletToSummon.gameObject.activeInHierarchy) return 4;
            var pickup = (VRC.SDK3.Components.VRCPickup)tabletToSummon.GetComponent(typeof(VRC.SDK3.Components.VRCPickup));
            if (pickup != null && pickup.IsHeld) return 2;
            var sync = tabletToSummon.GetComponent<VRC.SDK3.Components.VRCObjectSync>();
            if (sync != null)
            {
                Networking.SetOwner(player, tabletToSummon.gameObject);
                if (!Networking.IsOwner(tabletToSummon.gameObject)) return 2;
                sync.FlagDiscontinuity();
            }
            var body = tabletToSummon.GetComponent<Rigidbody>();
            if (body != null) { body.velocity = Vector3.zero; body.angularVelocity = Vector3.zero; }
            var head = player.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
            float scale = Mathf.Clamp(player.GetAvatarEyeHeightAsMeters() / 1.6f, .25f, 3f);
            tabletToSummon.SetPositionAndRotation(head.position + head.rotation * new Vector3(0, -.15f * scale, .65f * scale), head.rotation);
            return 1;
        }
        private void AddTrail(Vector3 point)
        {
            if (trail == null || (pointCount > 0 && Vector3.Distance(point, lastTrailPoint) < .006f)) return;
            if (pointCount >= MaxTrailPoints) { trail.SetPosition(MaxTrailPoints - 1, point); return; }
            lastTrailPoint = point;
            trail.positionCount = ++pointCount;
            trail.SetPosition(pointCount - 1, point);
        }
        private void Show(string text, Color color)
        {
            if (label != null) { label.gameObject.SetActive(true); label.text = text; label.color = color; }
            if (trail != null) { trail.startColor = color; trail.endColor = color; }
        }
        private void Cancel() { drawing = false; match = -1; ClearVisuals(); }
        private void ClearVisuals()
        {
            if (trail != null) { trail.enabled = false; trail.positionCount = 0; }
            if (label != null) label.gameObject.SetActive(false);
        }
        private void OnDisable() { held = false; helpAxis = 0f; summonDownSince = -1f; summonConsumed = false; Cancel(); HideHelp(); }
    }
}
