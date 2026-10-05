using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

namespace aki_lua87.CommonVideoPlaylist
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class PlaylistTitleMarquee : UdonSharpBehaviour
    {
        public Text label;
        public RectTransform viewport;
        public float speed = 28f;
        public float pause = 1.5f;
        float elapsed;
        float nextMeasure;
        float distance;
        float lastWidth = -1;
        string lastText;

        void OnEnable()
        {
            elapsed = 0;
            nextMeasure = 0;
            lastWidth = -1;
            lastText = null;
            if (label != null) label.rectTransform.anchoredPosition = Vector2.zero;
        }

        void Update()
        {
            if (label == null || viewport == null) return;
            if (Time.time >= nextMeasure)
            {
                nextMeasure = Time.time + .5f;
                float width = viewport.rect.width;
                if (lastText != label.text || Mathf.Abs(width - lastWidth) > .5f)
                {
                    lastText = label.text;
                    lastWidth = width;
                    float textWidth = label.preferredWidth + 2;
                    distance = Mathf.Max(0, textWidth - width);
                    label.rectTransform.sizeDelta = new Vector2(Mathf.Max(width, textWidth), 0);
                    elapsed = 0;
                    label.rectTransform.anchoredPosition = Vector2.zero;
                }
            }
            if (distance <= .5f || speed <= 0) return;
            float travel = distance / speed;
            float hold = Mathf.Max(0, pause);
            elapsed += Time.deltaTime;
            float phase = elapsed % (travel * 2 + hold * 2);
            float offset = 0;
            if (phase > hold && phase < hold + travel) offset = (phase - hold) * speed;
            else if (phase >= hold + travel && phase < hold * 2 + travel) offset = distance;
            else if (phase >= hold * 2 + travel) offset = distance - (phase - hold * 2 - travel) * speed;
            label.rectTransform.anchoredPosition = new Vector2(-offset, 0);
        }
    }
}
