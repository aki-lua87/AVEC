using System.Linq;
using UnityEditor;
using UnityEngine;

namespace aki_lua87.CommonVideoPlaylist.Editor
{
    public static class PlaylistAssets
    {
        public const string DefaultPath = PlaylistFactory.Root + "/Samples/Playlists/DefaultPlaylist.asset";
        public static bool IsDefault(PlaylistData data) => data != null && AssetDatabase.GetAssetPath(data) == DefaultPath;
        public static int LoadedUsers(PlaylistData data) => data == null ? 0 : Resources.FindObjectsOfTypeAll<PlaylistSetup>().Count(s => !EditorUtility.IsPersistent(s) && s.gameObject.scene.IsValid() && s.gameObject.scene.isLoaded && s.data == data);

        public static PlaylistData CreateFor(PlaylistSetup setup, string path)
        {
            if (setup == null || string.IsNullOrEmpty(path)) return null;
            if (EditorApplication.isPlayingOrWillChangePlaymode) return null;
            var data = ScriptableObject.CreateInstance<PlaylistData>();
            data.name = System.IO.Path.GetFileNameWithoutExtension(path);
            AssetDatabase.CreateAsset(data, path);
            Undo.RegisterCreatedObjectUndo(data, "Create playlist data");
            Undo.RecordObject(setup, "Assign dedicated playlist");
            setup.data = data;
            PlaylistConnections.Save(setup);
            return data;
        }
    }
}
