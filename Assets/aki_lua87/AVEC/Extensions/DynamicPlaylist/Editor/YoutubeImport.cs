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
        [Serializable] internal class Response { public string result; public string auther; public string channel_id; public PlaylistEntry[] videos; }

        internal static string NormalizeChannelInput(string input)
        {
            string value = (input ?? "").Trim();
            if (Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
                (uri.Scheme == "https" || uri.Scheme == "http") &&
                (uri.Host == "www.youtube.com" || uri.Host == "youtube.com" || uri.Host == "m.youtube.com"))
            {
                var parts = uri.AbsolutePath.Trim('/').Split('/');
                if (parts.Length > 0 && parts[0].StartsWith("@")) value = Uri.UnescapeDataString(parts[0]);
            }
            // Handles are sent in the JSON body only. The server resolves their
            // canonical ID before any playback or title URL is constructed.
            if (value.Length <= 256 && !value.Any(char.IsControl) && Regex.IsMatch(value, @"\A@[^\s/\\?#]+\z")) return value;
            return NormalizeChannelId(value);
        }

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
            if (!Regex.IsMatch(id, @"\AUC[A-Za-z0-9_-]{22}\z"))
                throw new ArgumentException("UCで始まる24文字のチャンネルID、@ハンドル、またはYouTubeのチャンネルURLを入力してください。");
            return id;
        }

        internal static Response ParseRegistration(string json)
        {
            var data = JsonUtility.FromJson<Response>(json);
            if (data == null || data.result != "OK") throw new InvalidOperationException("チャンネルの登録に失敗しました。");
            if (string.IsNullOrEmpty(data.channel_id) || !Regex.IsMatch(data.channel_id, @"\AUC[A-Za-z0-9_-]{22}\z"))
                throw new InvalidOperationException("登録APIから有効なチャンネルIDを取得できませんでした。channel_idを返すAPIか確認してください。");
            return data;
        }

        internal static PlaylistTab CreateDynamicTab(string id, string author)
        {
            if (string.IsNullOrEmpty(id) || !Regex.IsMatch(id, @"\AUC[A-Za-z0-9_-]{22}\z"))
                throw new ArgumentException("動的URLにはUC形式のチャンネルIDが必要です。");
            var videos = new PlaylistEntry[20];
            for (int i = 0; i < videos.Length; i++) videos[i] = new PlaylistEntry {
                title = (i + 1).ToString(), url = Server + "/videos/yt/ch/" + id + "?n=" + i, dynamicTitleIndex = i
            };
            return new PlaylistTab { name = string.IsNullOrWhiteSpace(author) ? id : author,
                titleSourceUrl = Server + "/videos/yt/chlist/" + id + "?version=v2", videos = videos };
        }

        public static async Task<PlaylistTab> Fetch(string input, bool dynamic)
        {
            string channelInput = NormalizeChannelInput(input);
            using (var client = new HttpClient { Timeout = TimeSpan.FromSeconds(60) })
            {
                Response registration;
                using (var body = new StringContent(JsonUtility.ToJson(new Registration { channel_id = channelInput }), Encoding.UTF8, "application/json"))
                using (var response = await client.PostAsync(Server + "/video/yt/channel/regist", body))
                {
                    response.EnsureSuccessStatusCode();
                    registration = ParseRegistration(await response.Content.ReadAsStringAsync());
                }
                string id = registration.channel_id;
                if (dynamic) return CreateDynamicTab(id, registration.auther);
                Response data;
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
                return new PlaylistTab { name = string.IsNullOrWhiteSpace(data.auther)
                    ? (string.IsNullOrWhiteSpace(registration.auther) ? id : registration.auther) : data.auther, videos = data.videos };
            }
        }
    }

    public static class YoutubeSearchImport
    {
        const string Server = "https://vrc.akakitune87.net";
        const int Slots = 20;
        [Serializable] class Response { public string[] titles; public string[] urls; }

        internal static string NormalizeQuery(string input)
        {
            string query = (input ?? "").Trim();
            if (query.Length == 0 || query.Length > 256 || query.Any(char.IsControl))
                throw new ArgumentException("検索キーワードを1～256文字で入力してください（改行や制御文字は使用できません）。");
            return query;
        }

        internal static string TitlesUrl(string input)
        {
            return Server + "/yt/search/titles?q=" + Uri.EscapeDataString(NormalizeQuery(input)) + "&version=v2";
        }

        public static async Task<PlaylistTab> Fetch(string input)
        {
            string query = NormalizeQuery(input);
            using (var client = new HttpClient { Timeout = TimeSpan.FromSeconds(60) })
            using (var response = await client.GetAsync(TitlesUrl(query)))
            {
                if (!response.IsSuccessStatusCode)
                    throw new InvalidOperationException("検索結果を取得できませんでした（HTTP " + (int)response.StatusCode + "）。時間を空けて再試行してください。");
                return ParseResponse(query, await response.Content.ReadAsStringAsync());
            }
        }

        // Kept separate from HTTP so the generated URLs can be verified offline.
        internal static PlaylistTab ParseResponse(string input, string json)
        {
            string query = NormalizeQuery(input);
            var data = JsonUtility.FromJson<Response>(json);
            if (data?.titles == null || data.urls == null || data.titles.Length != data.urls.Length)
                throw new InvalidOperationException("検索結果の形式が不正です。JSON対応の検索API（version=v2）を確認してください。");
            var videos = new PlaylistEntry[Slots];
            string videoUrl = Server + "/yt/search/video?q=" + Uri.EscapeDataString(query) + "&n=";
            for (int i = 0; i < videos.Length; i++)
            {
                if (i < data.urls.Length && (!Uri.TryCreate(data.urls[i], UriKind.Absolute, out var url) ||
                    (url.Scheme != "https" && url.Scheme != "http")))
                    throw new InvalidOperationException("検索結果に不正な動画URLが含まれています。");
                videos[i] = new PlaylistEntry {
                    title = i < data.titles.Length && !string.IsNullOrEmpty(data.titles[i]) ? data.titles[i] : (i + 1).ToString(),
                    url = videoUrl + i, dynamicTitleIndex = i
                };
            }
            // Reserve all slots even for an empty result: future results can appear
            // without rebuilding the world. Runtime title loading hides empty slots.
            return new PlaylistTab { name = query, titleSourceUrl = TitlesUrl(query), videos = videos };
        }
    }

    public static class YoutubePlaylistImport
    {
        const string Server = "https://vrc.akakitune87.net";
        [Serializable] class Response { public string playlist_title; public string[] titles; public string[] urls; }

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
                var videos = new PlaylistEntry[dynamic ? 20 : data.urls.Length];
                for (int i = 0; i < videos.Length; i++)
                {
                    if (i < data.urls.Length && (!Uri.TryCreate(data.urls[i], UriKind.Absolute, out var url) || (url.Scheme != "https" && url.Scheme != "http")))
                        throw new InvalidOperationException("プレイリストの動画一覧に不正なURLが含まれています。");
                    videos[i] = new PlaylistEntry {
                        title = i >= data.titles.Length || string.IsNullOrEmpty(data.titles[i]) ? (i + 1).ToString() : data.titles[i],
                        url = dynamic ? Server + "/yt/pl/" + id + "/video?n=" + i : data.urls[i],
                        dynamicTitleIndex = dynamic ? i : -1
                    };
                }
                // Older servers omit playlist_title; keep imports usable without
                // a second request to YouTube or an API key in the Unity project.
                string title = string.IsNullOrWhiteSpace(data.playlist_title) ? id : data.playlist_title.Trim();
                return new PlaylistTab { name = title, titleSourceUrl = dynamic ? source : "", videos = videos };
            }
        }

    }

}
