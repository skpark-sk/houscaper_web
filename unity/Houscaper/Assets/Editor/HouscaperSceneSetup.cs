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
            EditorApplication.delayCall += () =>
            {
                if (File.Exists(ScenePath)) return;
                CreateScene(openIt: false);
            };
        }

        [MenuItem("Houscaper/Regenerate Main Scene")]
        public static void Regenerate() => CreateScene(openIt: true);

        static void CreateScene(bool openIt)
        {
            Directory.CreateDirectory("Assets/Scenes");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);

            var go = new GameObject("Houscaper");
            go.AddComponent<Bootstrap>();
            SceneManager.MoveGameObjectToScene(go, scene);

            EditorSceneManager.SaveScene(scene, ScenePath);
            if (!openIt) EditorSceneManager.CloseScene(scene, true);

            AssetDatabase.Refresh();
            RegisterInBuildSettings();

            Debug.Log("Houscaper: generated " + ScenePath);
        }

        public static void RegisterInBuildSettings()
        {
            var scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            EditorBuildSettings.scenes = scenes;
        }
    }
}
