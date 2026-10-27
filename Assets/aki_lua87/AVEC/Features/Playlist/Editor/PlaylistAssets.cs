using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace aki_lua87.CommonVideoPlaylist.Editor
{
    [InitializeOnLoad]
    public static class PlaylistAssets
    {
        // Legacy data stays local on upgrade; never overwrite or delete it.
        public const string DefaultPath = PlaylistFactory.Root + "/Samples/Playlists/DefaultPlaylist.asset";
        public const string UserDefaultPath = "Assets/AVECUserData/DefaultPlaylist.asset";
        static bool pending;
        static PlaylistAssets()
        {
            EditorApplication.hierarchyChanged += ScheduleAssignment;
            EditorSceneManager.sceneOpened += (scene, mode) => ScheduleAssignment();
            ScheduleAssignment();
        }
        static void ScheduleAssignment()
        {
            if (pending || EditorApplication.isPlayingOrWillChangePlaymode) return;
            pending = true;
            EditorApplication.delayCall += AssignLoaded;
        }
        static void AssignLoaded()
        {
            pending = false;
            if (EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) { ScheduleAssignment(); return; }
            foreach (var setup in Resources.FindObjectsOfTypeAll<PlaylistSetup>()) EnsureAssigned(setup);
        }
        public static bool EnsureAssigned(PlaylistSetup setup)
        {
            if (setup == null || setup.data != null || EditorApplication.isPlayingOrWillChangePlaymode ||
                BuildPipeline.isBuildingPlayer || EditorUtility.IsPersistent(setup) ||
                !setup.gameObject.scene.IsValid() || !setup.gameObject.scene.isLoaded ||
                EditorSceneManager.IsPreviewScene(setup.gameObject.scene) ||
                PrefabStageUtility.GetPrefabStage(setup.gameObject) != null) return false;
            var data = GetOrCreateDefault(UserDefaultPath, DefaultPath);
            Undo.RecordObject(setup, "Assign local playlist data");
            setup.data = data;
            PlaylistConnections.Save(setup);
            return true;
        }
        internal static PlaylistData GetOrCreateDefault(string userPath, string legacyPath)
        {
            var data = AssetDatabase.LoadAssetAtPath<PlaylistData>(userPath);
            if (data == null) data = AssetDatabase.LoadAssetAtPath<PlaylistData>(legacyPath);
            if (data == null)
            {
                // Refuse to replace an unrelated asset occupying the user-data path.
                if (System.IO.File.Exists(userPath) || System.IO.File.Exists(userPath + ".meta"))
                    throw new System.InvalidOperationException("Playlist data path is occupied: " + userPath);
                var folder = System.IO.Path.GetDirectoryName(userPath).Replace('\\', '/');
                if (!AssetDatabase.IsValidFolder(folder))
                    AssetDatabase.CreateFolder(System.IO.Path.GetDirectoryName(folder).Replace('\\', '/'), System.IO.Path.GetFileName(folder));
                data = ScriptableObject.CreateInstance<PlaylistData>();
                AssetDatabase.CreateAsset(data, userPath);
            }
            return data;
        }
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
