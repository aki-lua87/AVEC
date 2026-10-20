using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

namespace aki_lua87.CommonVideoPlaylist
{
    // Local input only. Playback and queue ownership remain with the existing adapter.
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class PlaylistTabletTouch : UdonSharpBehaviour
    {
        public RectTransform surface;
        public VRC.SDK3.Components.VRCPickup pickup;
        public bool enableTouch = true;
        [Tooltip("Distance in metres in front of the screen needed to rearm a tap.")]
        public float releaseDistance = .012f;
        public float maximumPenetration = .03f;
        public float dragDistance = .006f;
        public float fingertipExtension = .8f;
        // Editor-baked persistent UI events; refreshed when playlist rows are baked.
        [HideInInspector] public Button[] eventButtons = new Button[0];
        [HideInInspector] public UdonBehaviour[] eventTargets = new UdonBehaviour[0];
        [HideInInspector] public string[] eventNames = new string[0];
        private Graphic[] graphics;
        private bool[] armed = new bool[2];
        private bool[] pressed = new bool[2];
        private bool[] dragging = new bool[2];
        private Vector3[] starts = new Vector3[2];
        private Vector3[] previous = new Vector3[2];
        private Selectable[] controls = new Selectable[2];
        private ScrollRect[] scrolls = new ScrollRect[2];
        private VRCPlayerApi localPlayer;

        void Start()
        {
            localPlayer = Networking.LocalPlayer;
            if (surface != null) graphics = surface.GetComponentsInChildren<Graphic>(true);
        }

        void OnDisable()
        {
            for (int i = 0; i < 2; i++) Clear(i);
        }

        public override void PostLateUpdate()
        {
            if (!enableTouch || surface == null || !Utilities.IsValid(localPlayer) || !localPlayer.IsUserInVR())
            {
                for (int i = 0; i < 2; i++) Clear(i);
                return;
            }
            // Do not treat the fingers gripping the tablet as touches.
            for (int i = 0; i < 2; i++)
            {
                if (pickup != null && pickup.IsHeld && pickup.currentPlayer == localPlayer &&
                    ((i == 0 && pickup.currentHand == VRC_Pickup.PickupHand.Left) ||
                     (i == 1 && pickup.currentHand == VRC_Pickup.PickupHand.Right))) { Clear(i); continue; }
                Process(i);
            }
        }

        private void Clear(int hand)
        {
            pressed[hand] = false; armed[hand] = false; dragging[hand] = false;
            controls[hand] = null; scrolls[hand] = null;
        }

        private void Process(int hand)
        {
            Vector3 middle = localPlayer.GetBonePosition(hand == 0 ? HumanBodyBones.LeftIndexIntermediate : HumanBodyBones.RightIndexIntermediate);
            Vector3 distal = localPlayer.GetBonePosition(hand == 0 ? HumanBodyBones.LeftIndexDistal : HumanBodyBones.RightIndexDistal);
            if (middle == Vector3.zero || distal == Vector3.zero || (distal - middle).sqrMagnitude < .00000001f) { Clear(hand); return; }
            Vector3 tip = distal + (distal - middle) * fingertipExtension;
            Vector3 point = surface.InverseTransformPoint(tip);
            float scale = surface.lossyScale.z;
            float depth = point.z * Mathf.Abs(scale);
            bool inside = surface.rect.Contains(new Vector2(point.x, point.y));
            if (!inside || depth > maximumPenetration || depth < -.08f) { Clear(hand); return; }
            if (!pressed[hand])
            {
                if (depth < -releaseDistance) armed[hand] = true;
                if (!armed[hand] || depth < 0) return;
                armed[hand] = false;
                pressed[hand] = true;
                starts[hand] = point; previous[hand] = point;
                Graphic hit = Hit(tip);
                controls[hand] = hit == null ? null : hit.GetComponentInParent<Selectable>();
                scrolls[hand] = hit == null ? null : hit.GetComponentInParent<ScrollRect>();
                if (controls[hand] != null && !controls[hand].IsInteractable()) controls[hand] = null;
                if (scrolls[hand] != null) scrolls[hand].StopMovement();
                if (controls[hand] != null || scrolls[hand] != null)
                    localPlayer.PlayHapticEventInHand(hand == 0 ? VRC_Pickup.PickupHand.Left : VRC_Pickup.PickupHand.Right, .025f, .25f, .5f);
            }
            if (depth < -releaseDistance)
            {
                if (!dragging[hand] && controls[hand] != null && controls[hand].IsInteractable())
                {
                    Graphic hit = Hit(tip);
                    if (hit != null && hit.GetComponentInParent<Selectable>() == controls[hand]) Click(controls[hand].GetComponent<Button>());
                }
                Clear(hand); armed[hand] = true;
                return;
            }
            Vector3 movement = point - starts[hand]; movement.z = 0;
            if (surface.TransformVector(movement).magnitude > dragDistance) dragging[hand] = true;
            Slider slider = controls[hand] == null ? null : controls[hand].GetComponent<Slider>();
            Scrollbar bar = controls[hand] == null ? null : controls[hand].GetComponent<Scrollbar>();
            if (slider != null) SetSlider(slider, tip);
            else if (bar != null) SetScrollbar(bar, tip);
            else if (dragging[hand] && scrolls[hand] != null && scrolls[hand].isActiveAndEnabled)
            {
                ScrollRect scroll = scrolls[hand];
                RectTransform viewport = scroll.viewport == null ? (RectTransform)scroll.transform : scroll.viewport;
                float overflow = scroll.content.rect.height - viewport.rect.height;
                if (overflow > 0)
                {
                    Vector3 delta = viewport.InverseTransformVector(surface.TransformVector(point - previous[hand]));
                    scroll.verticalNormalizedPosition = Mathf.Clamp01(scroll.verticalNormalizedPosition - delta.y / overflow);
                }
            }
            previous[hand] = point;
        }

        private Graphic Hit(Vector3 world)
        {
            Graphic best = null; int depth = -1;
            for (int i = 0; i < graphics.Length; i++)
            {
                Graphic graphic = graphics[i];
                if (graphic == null || !graphic.isActiveAndEnabled || !graphic.raycastTarget || graphic.depth < depth) continue;
                Vector3 p = graphic.rectTransform.InverseTransformPoint(world);
                if (!graphic.rectTransform.rect.Contains(new Vector2(p.x, p.y))) continue;
                bool visible = true;
                Transform parent = graphic.transform;
                while (parent != null && parent != surface.parent)
                {
                    RectMask2D mask = parent.GetComponent<RectMask2D>();
                    Mask stencil = parent.GetComponent<Mask>();
                    if ((mask != null && mask.isActiveAndEnabled) || (stencil != null && stencil.isActiveAndEnabled))
                    {
                        RectTransform rect = (RectTransform)parent;
                        Vector3 q = rect.InverseTransformPoint(world);
                        if (!rect.rect.Contains(new Vector2(q.x, q.y))) { visible = false; break; }
                    }
                    CanvasGroup group = parent.GetComponent<CanvasGroup>();
                    if (group != null && (!group.blocksRaycasts || group.alpha <= 0)) { visible = false; break; }
                    parent = parent.parent;
                }
                if (visible) { best = graphic; depth = graphic.depth; }
            }
            return best;
        }

        private void Click(Button button)
        {
            if (button == null) return;
            for (int i = 0; i < eventButtons.Length; i++)
                if (eventButtons[i] == button && eventTargets[i] != null) eventTargets[i].SendCustomEvent(eventNames[i]);
        }

        private float Fraction(RectTransform rect, Vector3 world, bool vertical, bool reverse)
        {
            Vector3 p = rect.InverseTransformPoint(world);
            float f = vertical ? Mathf.InverseLerp(rect.rect.yMin, rect.rect.yMax, p.y) : Mathf.InverseLerp(rect.rect.xMin, rect.rect.xMax, p.x);
            return reverse ? 1 - f : f;
        }

        private void SetSlider(Slider slider, Vector3 world)
        {
            RectTransform track = slider.handleRect != null ? (RectTransform)slider.handleRect.parent : (RectTransform)slider.transform;
            float f = Fraction(track, world, (int)slider.direction >= 2, slider.direction == Slider.Direction.RightToLeft || slider.direction == Slider.Direction.TopToBottom);
            slider.value = Mathf.Lerp(slider.minValue, slider.maxValue, f);
        }

        private void SetScrollbar(Scrollbar bar, Vector3 world)
        {
            RectTransform track = bar.handleRect != null ? (RectTransform)bar.handleRect.parent : (RectTransform)bar.transform;
            bar.value = Fraction(track, world, (int)bar.direction >= 2, bar.direction == Scrollbar.Direction.RightToLeft || bar.direction == Scrollbar.Direction.TopToBottom);
        }
    }
}
