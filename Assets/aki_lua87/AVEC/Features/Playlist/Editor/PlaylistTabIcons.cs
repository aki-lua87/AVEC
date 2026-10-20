using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;

namespace aki_lua87.CommonVideoPlaylist.Editor
{
    public static class PlaylistTabIcons
    {
        public static void Apply(PlaylistSetup setup)
        {
            if (setup == null || setup.view == null) return;
            var view = setup.view;
            Ensure(setup, view.queueTabImage, "tab-queue");
            Ensure(setup, view.historyTabImage, "tab-history");
            Ensure(setup, view.userTabImage, "tab-user");
            for (int i = 0; i < view.tabImages.Length; i++)
            {
                bool dynamic = view.titleSourceUrls != null && i < view.titleSourceUrls.Length && !VRCUrl.IsNullOrEmpty(view.titleSourceUrls[i]);
                Ensure(setup, view.tabImages[i], dynamic ? "tab-dynamic" : "tab-static");
            }
            if (setup.tabTemplate != null) Ensure(setup, setup.tabTemplate.GetComponent<Image>(), "tab-static");
        }
        internal static void Ensure(PlaylistSetup setup, Image tab, string kind)
        {
            if (tab == null) return;
            var label = tab.GetComponentInChildren<Text>(true);
            if (label == null) return;
            var metadata = tab.GetComponent<PlaylistTabIcon>();
            if (metadata == null) metadata = PlaylistUndo.AddComponent<PlaylistTabIcon>(tab.gameObject);
            PlaylistUndo.RecordObject(metadata, "Configure playlist tab icon");
            if (metadata.labelArea == null)
                metadata.labelArea = tab.transform.Find("Title viewport") as RectTransform ?? label.rectTransform;
            float originalLeft = metadata.labelArea.offsetMin.x - metadata.appliedInset;
            // Use the existing label padding first; compact menus usually need only 6px extra.
            float iconX = Mathf.Max(12f, originalLeft - 6f);
            if (metadata.icon == null)
            {
                var go = new GameObject("Tab kind icon", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(tab.transform, false); go.layer = tab.gameObject.layer;
                PlaylistUndo.RegisterCreatedObjectUndo(go, "Add playlist tab icon");
                metadata.icon = go.GetComponent<Image>();
                var rect = (RectTransform)go.transform;
                rect.anchorMin = rect.anchorMax = new Vector2(0, .5f); rect.pivot = new Vector2(.5f, .5f);
                rect.anchoredPosition = new Vector2(iconX, 0);
                rect.sizeDelta = new Vector2(22f, 22f);
            }
            if (metadata.placementVersion < 1)
            {
                var rect = metadata.icon.rectTransform;
                PlaylistUndo.RecordObject(rect, "Place tab icon inside existing padding");
                var position = rect.anchoredPosition; position.x = iconX; rect.anchoredPosition = position;
                metadata.placementVersion = 1; PlaylistConnections.Save(rect);
            }
            float wanted = setup.showTabIcons ? Mathf.Max(0f, iconX + 12f - originalLeft) : 0f;
            if (!Mathf.Approximately(metadata.appliedInset, wanted))
            {
                PlaylistUndo.RecordObject(metadata.labelArea, "Reserve tab icon space");
                var offset = metadata.labelArea.offsetMin; offset.x += wanted - metadata.appliedInset;
                metadata.labelArea.offsetMin = offset; metadata.appliedInset = wanted;
                PlaylistConnections.Save(metadata.labelArea);
            }
            var icon = metadata.icon;
            PlaylistUndo.RecordObject(icon, "Set playlist tab kind");
            icon.sprite = PlaylistIcons.Get(kind); icon.color = label.color;
            icon.type = Image.Type.Simple; icon.preserveAspect = true; icon.raycastTarget = false;
            if (icon.gameObject.activeSelf != setup.showTabIcons)
            {
                PlaylistUndo.RecordObject(icon.gameObject, "Show playlist tab icons");
                icon.gameObject.SetActive(setup.showTabIcons); PlaylistConnections.Save(icon.gameObject);
            }
            PlaylistConnections.Save(icon); PlaylistConnections.Save(metadata);
        }
    }
}
