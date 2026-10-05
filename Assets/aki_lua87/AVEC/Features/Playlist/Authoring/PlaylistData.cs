using System;
using UnityEngine;

namespace aki_lua87.CommonVideoPlaylist
{
    [CreateAssetMenu(menuName = "AVEC/Playlist/Data")]
    public class PlaylistData : ScriptableObject
    {
        public PlaylistTab[] tabs = new PlaylistTab[0];
    }
    [Serializable] public class PlaylistTab
    {
        public string name = "New tab";
        public bool randomPlayback;
        [Tooltip("タイトル取得用の目次URL。空欄なら取得しません。")]
        public string titleSourceUrl = "";
        public PlaylistEntry[] videos = new PlaylistEntry[0];
    }
    [Serializable] public class PlaylistEntry
    {
        public string title = "New video";
        public string url = "https://";
        [Tooltip("目次のtitles配列の位置（0始まり）。-1は固定タイトル。")]
        public int dynamicTitleIndex = -1;
    }
}
