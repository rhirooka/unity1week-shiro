#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SnowEscape.Editor
{
    [InitializeOnLoad]
    internal static class PenguinPrefabBuilder
    {
        private const string SourceScenePath = "Assets/Scenes/SampleScene.unity";
        private const string ResourcesFolder = "Assets/Resources";
        private const string PrefabsFolder = ResourcesFolder + "/Prefabs";
        private const string PrefabPath = PrefabsFolder + "/PenguinPlayer.prefab";

        static PenguinPrefabBuilder()
        {
            EditorApplication.delayCall += EnsurePenguinPrefab;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        [MenuItem("Snow Escape/Rebuild Penguin Prefab")]
        private static void RebuildPenguinPrefab()
        {
            AssetDatabase.DeleteAsset(PrefabPath);
            EnsurePenguinPrefab();
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
                EditorApplication.delayCall += EnsurePenguinPrefab;
        }

        private static void EnsurePenguinPrefab()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode ||
                AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
                return;

            Scene sourceScene = SceneManager.GetSceneByPath(SourceScenePath);
            bool closeWhenFinished = !sourceScene.IsValid() || !sourceScene.isLoaded;
            if (closeWhenFinished)
                sourceScene = EditorSceneManager.OpenScene(SourceScenePath, OpenSceneMode.Additive);

            GameObject source = FindAuthoredPenguin(sourceScene);
            if (source == null)
            {
                Debug.LogError($"PenguinPlayer could not be found in {SourceScenePath}.");
                if (closeWhenFinished) EditorSceneManager.CloseScene(sourceScene, true);
                return;
            }

            EnsureFolder(ResourcesFolder, "Assets", "Resources");
            EnsureFolder(PrefabsFolder, ResourcesFolder, "Prefabs");
            PrefabUtility.SaveAsPrefabAsset(source, PrefabPath);
            AssetDatabase.SaveAssets();

            if (closeWhenFinished) EditorSceneManager.CloseScene(sourceScene, true);
            Debug.Log($"Created the authored penguin prefab at {PrefabPath}.");
        }

        private static GameObject FindAuthoredPenguin(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == "PenguinPlayer" &&
                    root.transform.Find("Belly") != null &&
                    root.transform.Find("Beak") != null &&
                    root.transform.Find("LeftWing") != null &&
                    root.transform.Find("RightWing") != null)
                    return root;
            }
            return null;
        }

        private static void EnsureFolder(string path, string parent, string name)
        {
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(parent, name);
        }
    }
}
#endif
