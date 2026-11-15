using System.Collections.Generic;
using System.Linq;
using SunSwitch.GameFlow;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SunSwitch.Art.EditorTools
{
    /// <summary>
    /// Sun Switch > Build Game Scenes
    ///
    /// Makes the real game scenes from the level grey-box layout (GDD scene plan):
    ///
    ///   Assets/Scenes/Main.unity                 persistent scene: Day/Night look, managers
    ///                                            (WorldPhase, RespawnManager, GameManager, lives,
    ///                                            timer, shards, SectionLoader), the Lightkeeper on
    ///                                            CP1, the follow camera, the menu, the HUD
    ///   Assets/Scenes/Sections/Section1-4.unity  one section each, loaded into Main additively by
    ///                                            SectionLoader: platforms, shards, checkpoints,
    ///                                            hazards, hints (Section4 also has the SummitGoal)
    ///
    /// and puts Main first in Build Settings with the four sections after it.
    ///
    /// Safe to run again. Main gets missing pieces added and keeps the rest. A section scene that
    /// already exists is never touched, because it belongs to its section owner once made; delete
    /// it first to make it again from LevelGreyboxBuilder.
    /// </summary>
    public static class GameScenesBuilder
    {
        public const string MainPath = "Assets/Scenes/Main.unity";
        const string SectionDir = "Assets/Scenes/Sections";

        static string SectionPath(int index) => $"{SectionDir}/{SectionScene(index)}.unity";
        static string SectionScene(int index) => $"Section{index + 1}";   // the names SectionLoader loads

        [MenuItem("Sun Switch/Build Game Scenes")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            PlatformPrefabBuilder.EnsureFolder(SectionDir);

            var made = new List<string>();
            for (int i = 0; i < LevelGreyboxBuilder.SectionNames.Length; i++)
            {
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(SectionPath(i)) != null) continue;
                var section = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                LevelGreyboxBuilder.BuildSection(i, new GameObject(LevelGreyboxBuilder.SectionNames[i]));
                EditorSceneManager.SaveScene(section, SectionPath(i));
                made.Add(SectionScene(i));
            }

            if (!BuildMain()) return;
            SetBuildScenes();
            Debug.Log("[Sun Switch] Game scenes ready: Main + Section1-4 in Build Settings" +
                      (made.Count > 0 ? $" (made {string.Join(", ", made)})." : " (sections kept as they were)."));
        }

        static bool BuildMain()
        {
            UnityEngine.SceneManagement.Scene main;
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(MainPath) != null)
                main = EditorSceneManager.OpenScene(MainPath, OpenSceneMode.Single);
            else
            {
                main = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                EditorSceneManager.SaveScene(main, MainPath);
            }

            if (!PlayableSandboxSetup.SetUpPlayableRig(main, Vector3.zero, Quaternion.identity,
                    LevelGreyboxBuilder.ShardTotal, "Managers"))
                return false;

            var game = Object.FindFirstObjectByType<GameManager>(FindObjectsInactive.Include);
            var loader = game.GetComponent<SectionLoader>();
            if (loader == null) loader = game.gameObject.AddComponent<SectionLoader>();
            var lso = new SerializedObject(loader);
            var names = lso.FindProperty("sceneNames");
            names.arraySize = LevelGreyboxBuilder.SectionNames.Length;
            for (int i = 0; i < names.arraySize; i++) names.GetArrayElementAtIndex(i).stringValue = SectionScene(i);
            lso.ApplyModifiedPropertiesWithoutUndo();
            var gso = new SerializedObject(game);
            gso.FindProperty("sections").objectReferenceValue = loader;
            gso.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(main);
            EditorSceneManager.SaveScene(main);
            return true;
        }

        /// Main and the sections first and enabled; any other scenes already listed stay, switched off.
        static void SetBuildScenes()
        {
            var game = new List<string> { MainPath };
            for (int i = 0; i < LevelGreyboxBuilder.SectionNames.Length; i++) game.Add(SectionPath(i));
            var list = game.Select(p => new EditorBuildSettingsScene(p, true)).ToList();
            foreach (var other in EditorBuildSettings.scenes)
                if (!game.Contains(other.path)) list.Add(new EditorBuildSettingsScene(other.path, false));
            EditorBuildSettings.scenes = list.ToArray();
        }
    }
}
