using SunSwitch.Core;
using SunSwitch.GameFlow;
using SunSwitch.Player;
using SunSwitch.UI;
using StarterAssets;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace SunSwitch.Art.EditorTools
{
    /// <summary>
    /// Sun Switch > Art > Make Sandbox Playable
    ///
    /// Turns Platforms_Sandbox into a small playable test level: the Lightkeeper becomes the
    /// player (LightkeeperController + Starter Assets input, PlayerDeath, PhaseSwitchInput,
    /// PhaseOverlapChecker, PlatformRider), the Main Camera follows it through a Cinemachine
    /// third-person camera, and a short "Test Course" strings the platforms and obstacles
    /// together up to a SummitGoal.
    ///
    /// Game flow (Part B): the managers get GameManager, LivesSystem, RunTimer and ShardTracker
    /// (RespawnManager's own respawn is switched off, GameManager decides), the storybook
    /// MenuRoot is driven by GameMenuFlow, and the HUD prefab shows lives, timer, phase and shards.
    ///
    /// Sandbox only: the real player prefab, camera and managers for Main.unity belong to the
    /// player and phase-core owners. Safe to run again; it rebuilds the course and reuses the rest.
    /// </summary>
    public static class PlayableSandboxSetup
    {
        const string SandboxPath = "Assets/Scenes/Sandbox/Platforms_Sandbox.unity";
        const string LightkeeperPath = "Assets/Prefabs/Characters/Lightkeeper.prefab";
        const string InputActionsPath = "Assets/Starter Assets/Runtime/InputSystem/StarterAssets.inputactions";
        const string FollowCameraPath = "Assets/Starter Assets/Runtime/ThirdPersonController/Prefabs/PlayerFollowCamera.prefab";
        const int PlayerLayer = 2;   // Ignore Raycast: keeps the ground check from hitting the player's own capsule

        const float CourseZ = -16f;
        static readonly Vector3 StartPosition = new Vector3(0, 0, CourseZ);

        [MenuItem("Sun Switch/Art/Make Sandbox Playable")]
        public static void Run()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != SandboxPath)
            {
                Debug.LogError($"[Sun Switch] Open {SandboxPath} first.");
                return;
            }

            BuildCourse(scene);
            var player = SetUpPlayer(scene);
            if (player == null) return;
            SetUpCamera(scene, player);
            var managers = SetUpManagers(scene);
            SetUpGameFlow(scene, managers);

            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("[Sun Switch] Sandbox is playable: Play on the Start screen, then WASD / mouse to move, Space to jump, Right Mouse to switch, Esc to pause, T to force a switch.");
        }

        // ---------------------------------------------------------------- course

        static void BuildCourse(UnityEngine.SceneManagement.Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
                if (root.name == "Test Course") Undo.DestroyObjectImmediate(root);
            var course = new GameObject("Test Course");
            Undo.RegisterCreatedObjectUndo(course, "Build test course");
            SceneManagerMove(course, scene);

            // Ground level: start, a Both hop, then Day and Night stepping stones (switch mid-run).
            var start = Place(course, "Platforms/Base/Checkpoint", StartPosition);
            SetStart(start);
            Place(course, "Platforms/Platform_Medium_Both", P(6, 0));
            Place(course, "Platforms/Platform_Small_Day", P(10.5f, 0));
            Place(course, "Platforms/Platform_Small_Night", P(14, 0));
            // Kill bar on a wide platform: jump the bar as it comes round.
            Place(course, "Platforms/Platform_Wide_Both", P(19.5f, 0));
            Place(course, "Obstacles/KillBar_Both", P(19.5f, 0));
            // Jump pad up to a Night-only ledge 5 m higher: switch to Night in the air.
            Place(course, "Platforms/Platform_Medium_Both", P(26, 0));
            Place(course, "Obstacles/JumpPad_Both", P(26, 0));
            Place(course, "Platforms/Platform_Wide_Night", P(32, 5));
            Place(course, "Obstacles/Shard_Night", P(32, 6.2f));
            // Upper level: checkpoint, moving platform, Day landing.
            Place(course, "Platforms/Base/Checkpoint", P(38.5f, 5));
            Place(course, "Platforms/Platform_Moving_Both", P(43, 5));
            Place(course, "Platforms/Platform_Medium_Day", P(53, 5));
            // Night-only spikes with a Day shard above them: switch to Day to grab it.
            Place(course, "Platforms/Platform_Wide_Both", P(60, 5));
            Place(course, "Obstacles/KillSurface_M_Night", P(60, 5));
            Place(course, "Obstacles/Shard_Day", P(60, 6.2f));
            // Night wall across the last platform: pass it in Day.
            Place(course, "Platforms/Platform_Wide_Both", P(67, 5));
            Place(course, "Platforms/Wall_Night", P(66, 5), Quaternion.Euler(0, 90, 0));
            // Summit: one more hop to the goal, which wins the run.
            Place(course, "Platforms/Platform_Medium_Both", P(74, 5));
            Place(course, "Obstacles/SummitGoal", P(74, 5));
            // Catch falls before the void line far below.
            var plane = Place(course, "Obstacles/KillPlane", new Vector3(34, -4, CourseZ));
            if (plane != null)
            {
                plane.transform.position = new Vector3(36, -4, CourseZ);
                plane.transform.localScale = new Vector3(2.1f, 1f, 0.5f);   // x -6..78: start to summit
            }
        }

        static Vector3 P(float x, float y) => new Vector3(x, y, CourseZ);

        static void SetStart(GameObject checkpoint)
        {
            var cp = checkpoint != null ? checkpoint.GetComponentInChildren<Checkpoint>(true) : null;
            if (cp == null) return;
            var so = new SerializedObject(cp);
            so.FindProperty("isStart").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------------------------------------------------------------- player

        static GameObject SetUpPlayer(UnityEngine.SceneManagement.Scene scene)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(LightkeeperPath);
            GameObject player = null;
            foreach (var root in scene.GetRootGameObjects())
                if (PrefabUtility.GetCorrespondingObjectFromSource(root) == prefab) { player = root; break; }
            if (player == null)
            {
                player = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                Undo.RegisterCreatedObjectUndo(player, "Add Lightkeeper");
            }
            player.transform.SetPositionAndRotation(StartPosition, Quaternion.Euler(0, 90, 0));
            player.tag = "Player";
            player.layer = PlayerLayer;

            var cc = GetOrAdd<CharacterController>(player);
            cc.height = 1.8f;
            cc.radius = 0.28f;
            cc.center = new Vector3(0, 0.93f, 0);
            cc.slopeLimit = 45f;
            cc.stepOffset = 0.25f;
            cc.skinWidth = 0.02f;
            cc.minMoveDistance = 0f;

            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            if (actions == null) { Debug.LogError($"[Sun Switch] {InputActionsPath} missing (Starter Assets)."); return null; }
            var input = GetOrAdd<PlayerInput>(player);
            input.actions = actions;
            input.defaultActionMap = "Player";
            input.notificationBehavior = PlayerNotifications.SendMessages;   // StarterAssetsInputs listens to messages
            GetOrAdd<StarterAssetsInputs>(player);

            var cameraRoot = player.transform.Find("PlayerCameraRoot");
            if (cameraRoot == null)
            {
                cameraRoot = new GameObject("PlayerCameraRoot").transform;
                cameraRoot.SetParent(player.transform, false);
            }
            cameraRoot.localPosition = new Vector3(0, 1.375f, 0);
            cameraRoot.localRotation = Quaternion.identity;

            var controller = GetOrAdd<LightkeeperController>(player);
            controller.GroundLayers = 1;   // Default
            controller.CinemachineCameraTarget = cameraRoot.gameObject;

            GetOrAdd<PlayerDeath>(player);
            GetOrAdd<PhaseSwitchInput>(player);
            GetOrAdd<PhaseOverlapChecker>(player);
            GetOrAdd<PlatformRider>(player);
            EditorUtility.SetDirty(player);
            return player;
        }

        // ---------------------------------------------------------------- camera

        static void SetUpCamera(UnityEngine.SceneManagement.Scene scene, GameObject player)
        {
            var main = Camera.main;
            if (main == null)
                foreach (var root in scene.GetRootGameObjects())
                    if (root.TryGetComponent<Camera>(out var c)) { main = c; break; }
            if (main == null) { Debug.LogError("[Sun Switch] The sandbox has no camera."); return; }
            GetOrAdd<CinemachineBrain>(main.gameObject);

            CinemachineCamera follow = null;
            foreach (var root in scene.GetRootGameObjects())
                if (root.name == "PlayerFollowCamera" && root.TryGetComponent(out follow)) break;
            if (follow == null)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(FollowCameraPath);
                if (prefab == null) { Debug.LogError($"[Sun Switch] {FollowCameraPath} missing (Starter Assets)."); return; }
                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                Undo.RegisterCreatedObjectUndo(go, "Add follow camera");
                follow = go.GetComponent<CinemachineCamera>();
            }
            follow.Target.TrackingTarget = player.transform.Find("PlayerCameraRoot");
            follow.transform.SetPositionAndRotation(player.transform.position + new Vector3(-4, 2, 0), Quaternion.Euler(10, 90, 0));
            EditorUtility.SetDirty(follow);
        }

        // ---------------------------------------------------------------- managers

        static GameObject SetUpManagers(UnityEngine.SceneManagement.Scene scene)
        {
            if (Object.FindFirstObjectByType<WorldPhase>(FindObjectsInactive.Include) == null)
                DayNightLookSetup.AddToOpenScene();   // adds DayNightLook + WorldPhase in sandboxes

            var respawn = Object.FindFirstObjectByType<RespawnManager>(FindObjectsInactive.Include);
            if (respawn == null)
            {
                var go = new GameObject("Sandbox Managers");
                Undo.RegisterCreatedObjectUndo(go, "Add sandbox managers");
                SceneManagerMove(go, scene);
                respawn = go.AddComponent<RespawnManager>();
            }
            // GameManager decides between respawn and Game Over; respawning here too would do it twice.
            var so = new SerializedObject(respawn);
            so.FindProperty("respawnOnDeath").boolValue = false;
            so.ApplyModifiedPropertiesWithoutUndo();
            return respawn.gameObject;
        }

        // ---------------------------------------------------------------- game flow

        const string MenuRootPath = "Assets/UI/Prefabs/MenuRoot.prefab";

        /// Part B: GameManager + lives, timer and shards on the managers, the storybook menu
        /// driven by GameMenuFlow, the HUD, and an EventSystem for the menu buttons.
        static void SetUpGameFlow(UnityEngine.SceneManagement.Scene scene, GameObject managers)
        {
            var lives = GetOrAdd<LivesSystem>(managers);
            var timer = GetOrAdd<RunTimer>(managers);
            var shards = GetOrAdd<ShardTracker>(managers);
            var game = GetOrAdd<GameManager>(managers);
            var gso = new SerializedObject(game);
            gso.FindProperty("lives").objectReferenceValue = lives;
            gso.FindProperty("timer").objectReferenceValue = timer;
            gso.FindProperty("shards").objectReferenceValue = shards;
            gso.FindProperty("startInMenu").boolValue = true;
            gso.ApplyModifiedPropertiesWithoutUndo();

            // The sandbox has fewer than the tower's 12 shards: count the ones in the scene.
            int shardCount = Object.FindObjectsByType<Shard>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
            var sso = new SerializedObject(shards);
            sso.FindProperty("total").intValue = Mathf.Max(1, shardCount);
            sso.ApplyModifiedPropertiesWithoutUndo();

            if (Object.FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include) == null)
            {
                var events = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                Undo.RegisterCreatedObjectUndo(events, "Add EventSystem");
                SceneManagerMove(events, scene);
            }

            var menu = FindInstance(scene, MenuRootPath) ?? Instantiate(scene, MenuRootPath, "Add menu");
            if (menu != null) GetOrAdd<GameMenuFlow>(menu);

            if (AssetDatabase.LoadAssetAtPath<GameObject>(HudPrefabBuilder.PrefabPath) == null) HudPrefabBuilder.Build();
            if (FindInstance(scene, HudPrefabBuilder.PrefabPath) == null) Instantiate(scene, HudPrefabBuilder.PrefabPath, "Add HUD");

            EditorUtility.SetDirty(managers);
        }

        static GameObject FindInstance(UnityEngine.SceneManagement.Scene scene, string prefabPath)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            foreach (var root in scene.GetRootGameObjects())
                if (prefab != null && PrefabUtility.GetCorrespondingObjectFromSource(root) == prefab) return root;
            return null;
        }

        static GameObject Instantiate(UnityEngine.SceneManagement.Scene scene, string prefabPath, string undo)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) { Debug.LogError($"[Sun Switch] {prefabPath} missing."); return null; }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            Undo.RegisterCreatedObjectUndo(go, undo);
            return go;
        }

        // ---------------------------------------------------------------- helpers

        static GameObject Place(GameObject parent, string prefab, Vector3 position, Quaternion? rotation = null)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Prefabs/{prefab}.prefab");
            if (asset == null)
            {
                Debug.LogError($"[Sun Switch] Assets/Prefabs/{prefab}.prefab missing.");
                return null;
            }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent.scene);
            go.transform.SetParent(parent.transform, false);
            go.transform.SetPositionAndRotation(position, rotation ?? Quaternion.identity);
            return go;
        }

        static void SceneManagerMove(GameObject go, UnityEngine.SceneManagement.Scene scene)
        {
            if (go.scene != scene) UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, scene);
        }

        static T GetOrAdd<T>(GameObject go) where T : Component =>
            go.TryGetComponent<T>(out var c) ? c : Undo.AddComponent<T>(go);
    }
}
