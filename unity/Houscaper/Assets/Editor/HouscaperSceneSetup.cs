using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Houscaper.EditorTools
{
    /// <summary>
    /// The scene holds nothing but a Bootstrap object, so it is generated rather than committed.
    /// That keeps the repository free of hand-authored scene YAML and version drift.
    /// </summary>
    [InitializeOnLoad]
    public static class HouscaperSceneSetup
    {
        public const string ScenePath = "Assets/Scenes/Main.unity";

        static HouscaperSceneSetup()
        {
            EditorApplication.delayCall += EnsureMainScene;
        }

        static void EnsureMainScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (EditorApplication.isCompiling) return;
            if (File.Exists(ScenePath)) return;
            CreateScene(openIt: true);
        }

        [MenuItem("Houscaper/Regenerate Main Scene")]
        public static void Regenerate() => CreateScene(openIt: true);

        static void CreateScene(bool openIt)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Houscaper: cannot create Main scene while playing.");
                return;
            }

            Directory.CreateDirectory("Assets/Scenes");

            // Single mode: Additive fails when the only open scene is an unsaved Untitled scene.
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var go = new GameObject("Houscaper");
            SceneManager.MoveGameObjectToScene(go, scene);
            go.AddComponent<Bootstrap>();

            EditorSceneManager.SaveScene(scene, ScenePath);

            AssetDatabase.Refresh();
            RegisterInBuildSettings();

            if (openIt)
            {
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            Debug.Log("Houscaper: generated " + ScenePath);
        }

        public static void RegisterInBuildSettings()
        {
            var scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            EditorBuildSettings.scenes = scenes;
        }
    }
}
