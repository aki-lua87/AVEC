using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UdonSharp;
using UdonSharpEditor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace aki_lua87.CommonVideoPlaylist.Editor
{
    public static class YoutubeChannelImport
    {
        const string Server = "https://vrc.akakitune87.net";
        [Serializable] class Registration { public string channel_id; }
        [Serializable] class Response { public string result; public string auther; public PlaylistEntry[] videos; }

        public static string NormalizeChannelId(string input)
        {
            string id = (input ?? "").Trim();
            if (Uri.TryCreate(id, UriKind.Absolute, out var uri) &&
                (uri.Scheme == "https" || uri.Scheme == "http") &&
                (uri.Host == "www.youtube.com" || uri.Host == "youtube.com" || uri.Host == "m.youtube.com"))
            {
                var parts = uri.AbsolutePath.Trim('/').Split('/');
                if (parts.Length >= 2 && parts[0] == "channel") id = parts[1];
            }
            if (!Regex.IsMatch(id, @"^UC[A-Za-z0-9_-]{22}$"))
                throw new ArgumentException("UCで始まる24文字のチャンネルID、または /channel/ID 形式のYouTube URLを入力してください。");
            return id;
        }

        public static async Task<PlaylistTab> Fetch(string input, bool dynamic)
        {
            string id = NormalizeChannelId(input);
            using (var client = new HttpClient { Timeout = TimeSpan.FromSeconds(60) })
            {
                Response data;
                if (dynamic)
                {
                    using (var body = new StringContent(JsonUtility.ToJson(new Registration { channel_id = id }), Encoding.UTF8, "application/json"))
                    using (var response = await client.PostAsync(Server + "/video/yt/channel/regist", body))
                    {
                        response.EnsureSuccessStatusCode();
                        data = JsonUtility.FromJson<Response>(await response.Content.ReadAsStringAsync());
                    }
                    if (data == null || data.result != "OK") throw new InvalidOperationException("チャンネルの登録に失敗しました。");
                    var videos = new PlaylistEntry[20];
                    for (int i = 0; i < videos.Length; i++) videos[i] = new PlaylistEntry {
                        title = (i + 1).ToString(), url = Server + "/videos/yt/ch/" + id + "?n=" + i, dynamicTitleIndex = i
                    };
                    return new PlaylistTab { name = string.IsNullOrWhiteSpace(data.auther) ? id : data.auther,
                        titleSourceUrl = Server + "/videos/yt/chlist/" + id + "?version=v2", videos = videos };
                }
                using (var response = await client.GetAsync(Server + "/api/yt/ch/" + id + "/video/all"))
                {
                    response.EnsureSuccessStatusCode();
                    data = JsonUtility.FromJson<Response>(await response.Content.ReadAsStringAsync());
                }
                if (data == null || data.videos == null || data.videos.Length == 0) throw new InvalidOperationException("動画一覧を取得できませんでした。");
                foreach (var entry in data.videos)
                {
                    if (entry == null || !Uri.TryCreate(entry.url, UriKind.Absolute, out var url) || (url.Scheme != "https" && url.Scheme != "http"))
                        throw new InvalidOperationException("動画一覧に不正なURLが含まれています。");
                    entry.dynamicTitleIndex = -1;
                }
                return new PlaylistTab { name = string.IsNullOrWhiteSpace(data.auther) ? id : data.auther, videos = data.videos };
            }
        }
    }

    public static class YoutubePlaylistImport
    {
        const string Server = "https://vrc.akakitune87.net";
        [Serializable] class Response { public string[] titles; public string[] urls; }

        public static string NormalizePlaylistId(string input)
        {
            string id = (input ?? "").Trim();
            if (Uri.TryCreate(id, UriKind.Absolute, out var uri))
            {
                if ((uri.Scheme != "https" && uri.Scheme != "http") ||
                    (uri.Host != "youtube.com" && uri.Host != "www.youtube.com" && uri.Host != "m.youtube.com" &&
                     uri.Host != "music.youtube.com" && uri.Host != "youtu.be"))
                    throw new ArgumentException("YouTubeのプレイリストURL、またはプレイリストIDを入力してください。");
                id = null;
                foreach (string parameter in uri.Query.TrimStart('?').Split('&'))
                {
                    int separator = parameter.IndexOf('=');
                    if (separator < 0 || parameter.Substring(0, separator) != "list") continue;
                    if (id != null) throw new ArgumentException("listが複数指定されています。YouTubeのプレイリストURLを確認してください。");
                    id = Uri.UnescapeDataString(parameter.Substring(separator + 1));
                }
            }
            // Playlist IDs vary in prefix/length; a single video ID (11 chars) is not a playlist.
            if (id == null || !Regex.IsMatch(id, @"^[A-Za-z0-9_-]{12,100}$"))
                throw new ArgumentException("list=を含むYouTube URL、またはプレイリストIDを入力してください（動画URLだけでは取り込めません）。");
            return id;
        }

        public static async Task<PlaylistTab> Fetch(string input, bool dynamic)
        {
            string id = NormalizePlaylistId(input);
            string source = Server + "/yt/pl/" + id + "/titles";
            using (var client = new HttpClient { Timeout = TimeSpan.FromSeconds(60) })
            using (var response = await client.GetAsync(source))
            {
                if (!response.IsSuccessStatusCode)
                    throw new InvalidOperationException("プレイリストを取得できませんでした（HTTP " + (int)response.StatusCode + "）。公開状態・IDを確認し、時間を空けて再試行してください。");
                var data = JsonUtility.FromJson<Response>(await response.Content.ReadAsStringAsync());
                if (data?.titles == null || data.urls == null || data.urls.Length == 0 || data.titles.Length != data.urls.Length)
                    throw new InvalidOperationException("プレイリストの動画一覧を取得できませんでした。空のリストや非公開リストは取り込めません。");
                var videos = new PlaylistEntry[data.urls.Length];
                for (int i = 0; i < videos.Length; i++)
                {
                    if (!Uri.TryCreate(data.urls[i], UriKind.Absolute, out var url) || (url.Scheme != "https" && url.Scheme != "http"))
                        throw new InvalidOperationException("プレイリストの動画一覧に不正なURLが含まれています。");
                    videos[i] = new PlaylistEntry {
                        title = string.IsNullOrEmpty(data.titles[i]) ? (i + 1).ToString() : data.titles[i],
                        url = dynamic ? Server + "/yt/pl/" + id + "/video?n=" + i : data.urls[i],
                        dynamicTitleIndex = dynamic ? i : -1
                    };
                }
                return new PlaylistTab { name = id, titleSourceUrl = dynamic ? source : "", videos = videos };
            }
        }
    }

}
