using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

namespace aki_lua87.CommonVideoPlaylist
{
    // Keep the component identity so existing prefab/scene references survive.
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class PlaylistTitleMarquee : UdonSharpBehaviour
    {
        public Text label;
        public RectTransform viewport;
        public float speed = 28f;
        public float pause = 1.5f;
        private string lastText;
        private float lastWidth = -1f;
        void OnEnable() { lastText = null; lastWidth = -1f; }
        void Update()
        {
            if (label == null || viewport == null) return;
            if (lastText == label.text && Mathf.Abs(lastWidth - viewport.rect.width) < .5f) return;
            lastText = label.text; lastWidth = viewport.rect.width;
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 1;
            label.resizeTextMaxSize = Mathf.Max(1, label.fontSize);
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
        }
    }
}