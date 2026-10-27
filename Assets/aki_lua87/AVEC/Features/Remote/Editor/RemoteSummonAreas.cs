using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace aki_lua87.CommonGestureRemote.Editor
{
    public static class RemoteSummonAreas
    {
        // Included automatically in AVEC-Core through its Features/Remote folder.
        public const string PrefabPath = RemoteFactory.Root + "/Samples/RemoteSummonArea.prefab";

        public static string Validate(GestureRemote remote)
        {
            var area = remote.summonArea;
            if (area == null) return null;
            if (area.transform == remote.transform || area.transform.IsChildOf(remote.transform))
                return "呼び出し範囲をリモコンの外へ移動してください。リモコン自身やその子にある範囲では呼び出せません。";
            if (area.gameObject.scene != remote.gameObject.scene)
                return "呼び出し範囲は同じシーン上のBoxColliderを指定してください。";
            if (!area.enabled || !area.gameObject.activeInHierarchy)
                return "呼び出し範囲が無効です。この状態では呼び出せません。";
            if (!area.isTrigger)
                return "通行を妨げないよう、範囲のBoxColliderはIs TriggerをONにしてください。";
            return null;
        }

        public static GameObject EnsurePrefab()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab != null) return prefab;
            var scene = EditorSceneManager.NewPreviewScene();
            try {
                var go = new GameObject("Remote Summon Area");
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, scene);
                go.layer = 2; // Ignore Raycast; the trigger never blocks player movement.
                var box = go.AddComponent<BoxCollider>();
                box.isTrigger = true; box.center = new Vector3(0, 1.5f, 0); box.size = new Vector3(8, 3, 8);
                return PrefabUtility.SaveAsPrefabAsset(go, PrefabPath);
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }

        public static BoxCollider CreateAndAssign(RemoteSetup setup)
        {
            if (!RemoteConnections.InScene(setup) || setup.remote == null) throw new InvalidOperationException("シーン上のリモコンを選択してください。");
            if (setup.remote.summonArea != null) throw new InvalidOperationException("呼び出し範囲は既に設定されています。");
            Undo.IncrementCurrentGroup(); Undo.SetCurrentGroupName("Create remote summon area");
            var go = (GameObject)PrefabUtility.InstantiatePrefab(EnsurePrefab(), setup.gameObject.scene);
            Undo.RegisterCreatedObjectUndo(go, "Create remote summon area");
            // Independent scene root: picking up or summoning the remote cannot move its range.
            var position = setup.transform.position; position.y = 0; go.transform.position = position;
            PrefabUtility.RecordPrefabInstancePropertyModifications(go.transform);
            var box = go.GetComponent<BoxCollider>();
            Undo.RecordObject(setup.remote, "Assign remote summon area"); setup.remote.summonArea = box;
            RemoteConnections.Save(setup.remote);
            Selection.activeGameObject = go;
            return box;
        }
    }
}
