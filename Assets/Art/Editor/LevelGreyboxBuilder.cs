using SunSwitch.Core;
using SunSwitch.Obstacles;
using SunSwitch.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SunSwitch.Art.EditorTools
{
    /// <summary>
    /// Sun Switch > Art > Build Level Grey-box
    ///
    /// Places the whole Sun Switch Level Plan (four sections around the tower, 12 shards,
    /// 6 checkpoints, kill planes, the Summit auto-cycle zone, the SummitGoal and the instruction
    /// hints) into
    /// Assets/Scenes/Sandbox/Level_Greybox.unity, with the playable rig from Make Sandbox Playable
    /// (player at CP1, follow camera, managers, menu, HUD).
    ///
    /// One root per section ("Section 1 - Mossy Base", …) so each owner can copy theirs into
    /// SectionN.unity. Safe to run again: the section roots are rebuilt, the rig is reused.
    /// The positions below are the plan's tables; change them here and in the plan together.
    /// </summary>
    public static class LevelGreyboxBuilder
    {
        public const string ScenePath = "Assets/Scenes/Sandbox/Level_Greybox.unity";

        [MenuItem("Sun Switch/Art/Build Level Grey-box")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            UnityEngine.SceneManagement.Scene scene;
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            else
            {
                // Saved straight away: the Day/Night setup treats scenes under /Sandbox/ as sandboxes.
                scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                EditorSceneManager.SaveScene(scene, ScenePath);
            }

            foreach (var root in scene.GetRootGameObjects())
                if (root.name.StartsWith("Section ")) Object.DestroyImmediate(root);

            for (int i = 0; i < SectionNames.Length; i++) BuildSection(i, Root(SectionNames[i]));

            if (!PlayableSandboxSetup.SetUpPlayableRig(scene, Vector3.zero, Quaternion.identity)) return;
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[Sun Switch] Level grey-box built in {ScenePath}.");
        }

        /// <summary>Section root names, in order. GameScenesBuilder uses them for Section1-4.unity.</summary>
        internal static readonly string[] SectionNames =
            { "Section 1 - Mossy Base", "Section 2 - Aqueducts", "Section 3 - Clockwork", "Section 4 - Summit" };

        /// <summary>Shards in the whole level (3 per section).</summary>
        internal const int ShardTotal = 12;

        /// <summary>Fills root with section index (0-3): platforms, shards, checkpoints, hazards, hints.</summary>
        internal static void BuildSection(int index, GameObject root)
        {
            switch (index)
            {
                case 0: BuildSection1(root); break;
                case 1: BuildSection2(root); break;
                case 2: BuildSection3(root); break;
                case 3: BuildSection4(root); break;
            }
        }

        // ---------------------------------------------------------------- Section 1 (P3)
        // Along +Z at x 0, y 0 -> 2. Switching on the ground, ghosts, the blocked switch, a fork.

        static void BuildSection1(GameObject s)
        {
            var cp1 = Checkpoint(s, 0, 0, 0, 0);
            SetStart(cp1);
            Hint(s, "Controls", new Vector3(0, 1.5f, 1), new Vector3(6, 4, 6),
                "[WASD] move     [Space] jump     [Shift] sprint     [Mouse] look around");
            Hint(s, "Switch", new Vector3(0, 1.5f, 17), new Vector3(5, 4, 4),           // end of the Day platform + the station
                "[Right Mouse] switch Day / Night. Some platforms only exist at Night.");
            Hint(s, "Ghost", new Vector3(0, 1.5f, 32.25f), new Vector3(6, 4, 8.5f),     // up to the wall
                "A Day wall is only a ghost at Night. Switch, then walk through it.");
            P(s, "Platforms/Platform_Bridge_Both", 0, 0, 7);
            P(s, "Platforms/Platform_Medium_Day", 0, 0, 14);
            P(s, "Obstacles/Shard_Day", 0, 1.2f, 14);                      // #1
            P(s, "Platforms/Platform_Small_Both", 0, 0, 18);                // switch station
            P(s, "Platforms/Platform_Medium_Night", 0, 0, 22);
            P(s, "Platforms/Platform_Small_Both", 0, 0, 26);
            P(s, "Platforms/Platform_Medium_Day", 0, 0, 30);
            P(s, "Platforms/Platform_Bridge_Both", 0, 0, 37);
            P(s, "Platforms/Wall_Day", 0, 0, 37);                           // spans the 2 m bridge
            P(s, "Obstacles/Shard_Night", 0, 1.2f, 39.5f);                  // #2
            P(s, "Platforms/Platform_Wide_Both", 0, 0, 45);                 // fork
            P(s, "Platforms/Platform_Small_Day", -3, 0.75f, 51);            // Day path
            P(s, "Platforms/Platform_Small_Day", -3, 1.5f, 55);
            P(s, "Obstacles/Shard_Day", -3, 2.7f, 55);                      // #3
            P(s, "Platforms/Platform_Small_Day", -3, 2f, 59);
            P(s, "Platforms/Platform_Medium_Night", 3, 0.75f, 52);          // Night path
            P(s, "Platforms/Platform_Medium_Night", 3, 1.5f, 57.5f);
            P(s, "Platforms/Platform_Wide_Both", 0, 2, 64);
            KillPlane(s, new Vector3(0, -4, 32), new Vector3(0.6f, 1, 2.1f));
        }

        // ---------------------------------------------------------------- Section 2 (P2)
        // Along +X at z 71, y 2 -> 12. Mid-air switching, jump pads, a risky side path.

        static void BuildSection2(GameObject s)
        {
            Checkpoint(s, 0, 2, 71, 90);                                    // CP2
            Hint(s, "Mid-air Switch", new Vector3(3, 3.5f, 71), new Vector3(10, 4, 6),
                "Switch in mid-air: jump first, then [Right Mouse] before you land.");
            P(s, "Platforms/Platform_Medium_Both", 6, 2, 71);
            P(s, "Platforms/Platform_Small_Night", 10.5f, 2, 71);
            P(s, "Platforms/Platform_Small_Day", 14, 2, 71);
            P(s, "Obstacles/Shard_Night", 15.75f, 3.4f, 71);                // #4 switch early
            P(s, "Platforms/Platform_Small_Night", 17.5f, 2, 71);
            P(s, "Platforms/Platform_Wide_Both", 23, 2, 71);
            P(s, "Platforms/Platform_Medium_Both", 30, 2, 71);
            P(s, "Obstacles/JumpPad_Day", 30, 2, 71);
            P(s, "Platforms/Platform_Medium_Night", 35, 7, 71);
            P(s, "Platforms/Platform_Small_Day", 35, 7, 76.5f);             // side path
            P(s, "Obstacles/Shard_Day", 35, 8.2f, 76.5f);                   // #5
            Checkpoint(s, 40.5f, 7, 71, 90);                                // CP2b
            P(s, "Platforms/Platform_Wide_Both", 48, 7, 71);
            KillBar(P(s, "Obstacles/KillBar_Both", 48, 7, 71), 0, 90);
            P(s, "Platforms/Platform_Medium_Both", 55, 7, 71);
            P(s, "Obstacles/JumpPad_Night", 55, 7, 71);
            P(s, "Obstacles/Shard_Night", 55.2f, 10.4f, 71);                // #6 on the way up (pads fire at the trigger edge)
            P(s, "Platforms/Platform_Medium_Day", 60, 12, 71);
            var mover = P(s, "Platforms/Platform_Moving_Both", 64.5f, 12, 71);
            var pointB = mover != null ? mover.transform.Find("PointB") : null;
            if (pointB != null) pointB.localPosition = new Vector3(10, 0, 0);
            P(s, "Platforms/Wall_Day", 69.5f, 12, 71, 90);                  // across the track
            KillPlane(s, new Vector3(40, -1, 71), new Vector3(2.2f, 1, 0.4f));
        }

        // ---------------------------------------------------------------- Section 3 (P1)
        // Along -Z at x 78, y 12 -> 17. Phased hazards.

        static void BuildSection3(GameObject s)
        {
            Checkpoint(s, 78, 12, 71, 180);                                 // CP3
            P(s, "Platforms/Platform_Wide_Day", 78, 12, 64);
            P(s, "Obstacles/KillSurface_S_Day", 76.5f, 12, 64);
            P(s, "Obstacles/KillSurface_S_Day", 79.5f, 12, 64);
            P(s, "Obstacles/Shard_Night", 78, 14, 64);                      // #7 double switch
            P(s, "Platforms/Platform_Pillar_Night", 76.5f, 12, 58.5f);
            P(s, "Platforms/Platform_Pillar_Day", 79.5f, 12, 55);
            P(s, "Platforms/Platform_Pillar_Night", 76.5f, 12, 51.5f);
            P(s, "Obstacles/Shard_Night", 76.5f, 13.2f, 51.5f);             // #8
            Checkpoint(s, 78, 12, 46.5f, 180);                              // CP3b
            Hint(s, "Phased Hazards", new Vector3(78, 13.5f, 46.5f), new Vector3(6, 4, 4.5f),
                "Hazards follow the phase too: a Day bar is only a ghost at Night.");
            P(s, "Platforms/Platform_Wide_Both", 78, 12, 40);
            KillBar(P(s, "Obstacles/KillBar_Day", 78, 12, 40), 0, 90);
            KillBar(P(s, "Obstacles/KillBar_Night", 78, 12, 40), 90, 90);
            P(s, "Platforms/Platform_Bridge_Day", 78, 12, 31);
            P(s, "Platforms/Wall_Day", 78, 12, 28);
            P(s, "Platforms/Platform_Bridge_Night", 78, 12, 22);
            P(s, "Obstacles/JumpPad_Night", 78, 12, 19.5f);
            P(s, "Obstacles/Shard_Day", 78, 18.5f, 16.5f);                  // #9 on the way down
            P(s, "Platforms/Platform_Wide_Day", 78, 17, 13);
            KillPlane(s, new Vector3(78, 8, 41), new Vector3(0.4f, 1, 1.85f));
        }

        // ---------------------------------------------------------------- Section 4 (P4)
        // Along -X at z 13, then +Z to the centre, y 17 -> 25. Auto-cycle rhythm finale.

        static void BuildSection4(GameObject s)
        {
            Checkpoint(s, 72, 17, 13, 270);                                 // CP4, inside the zone
            Hint(s, "Auto-cycle", new Vector3(71.5f, 18.5f, 13), new Vector3(6, 4, 6),
                "Here the world flips by itself. Wait on safe ground, and jump when the platforms blink.");
            var zone = new GameObject("AutoCycleZone");
            zone.transform.SetParent(s.transform, false);
            zone.transform.position = new Vector3(55.5f, 22.5f, 22.25f);
            var box = zone.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(37, 15, 26.5f);                          // x 37-74, z 9-35.5
            zone.AddComponent<AutoCycleZone>();
            zone.AddComponent<PhaseFlipCue>();                              // platforms blink before each flip

            // The flip only lands you safely if you take off just before it, so every phased hop
            // starts or ends on a Both platform where you can stand and catch the beat. Only two
            // hops go phased-to-phased (Night 18.5 > Day 23, Night 30 > Day 33.5), each after a rest.
            P(s, "Platforms/Platform_Wide_Both", 65.5f, 17, 13);
            P(s, "Platforms/Platform_Medium_Day", 59, 17, 13);
            P(s, "Obstacles/Shard_Night", 56, 18.6f, 13);                   // #10 leave the Day platform late, flip in the air
            P(s, "Platforms/Platform_Medium_Both", 53, 17, 13);             // rest
            P(s, "Platforms/Platform_Medium_Night", 47.5f, 17, 13);
            P(s, "Platforms/Platform_Wide_Both", 41.5f, 17, 13);            // rest
            P(s, "Obstacles/JumpPad_Day", 41.5f, 17, 13);
            P(s, "Obstacles/Shard_Night", 41.5f, 23.5f, 16);                // #11
            P(s, "Platforms/Platform_Medium_Night", 41.5f, 22, 18.5f);
            P(s, "Platforms/Platform_Small_Day", 41.5f, 22.75f, 23);        // the stair
            P(s, "Platforms/Platform_Small_Both", 41.5f, 23.5f, 26.5f);     // rest
            P(s, "Platforms/Platform_Small_Night", 41.5f, 24.25f, 30);
            P(s, "Platforms/Platform_Small_Day", 41.5f, 25, 33.5f);
            P(s, "Platforms/Platform_Small_Both", 45, 24.25f, 30);          // side step, also a rest
            P(s, "Obstacles/Shard_Day", 45, 25.45f, 30);                    // #12
            P(s, "Platforms/Platform_Wide_Both", 41.5f, 25, 39);
            P(s, "Obstacles/SummitGoal", 41.5f, 25, 40);
            KillPlane(s, new Vector3(55.5f, 13, 26), new Vector3(0.9f, 1, 0.8f));
        }

        // ---------------------------------------------------------------- helpers

        static GameObject Root(string name) => new GameObject(name);

        static GameObject P(GameObject parent, string prefab, float x, float y, float z, float rotY = 0)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Prefabs/{prefab}.prefab");
            if (asset == null)
            {
                Debug.LogError($"[Sun Switch] Assets/Prefabs/{prefab}.prefab missing.");
                return null;
            }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent.scene);
            go.transform.SetParent(parent.transform, false);
            go.transform.SetPositionAndRotation(new Vector3(x, y, z), Quaternion.Euler(0, rotY, 0));
            return go;
        }

        static GameObject Checkpoint(GameObject parent, float x, float y, float z, float rotY) =>
            P(parent, "Platforms/Base/Checkpoint", x, y, z, rotY);

        static void SetStart(GameObject checkpoint)
        {
            var cp = checkpoint != null ? checkpoint.GetComponentInChildren<Checkpoint>(true) : null;
            if (cp == null) return;
            var so = new SerializedObject(cp);
            so.FindProperty("isStart").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // Instruction shown on the HUD while the player is in the box (HintZone, HintPanel).
        static void Hint(GameObject parent, string name, Vector3 center, Vector3 size, string message)
        {
            var go = new GameObject("Hint - " + name);
            go.transform.SetParent(parent.transform, false);
            go.transform.position = center;
            var so = new SerializedObject(go.AddComponent<HintZone>());
            so.FindProperty("message").stringValue = message;
            so.FindProperty("size").vector3Value = size;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void KillBar(GameObject bar, float startAngle, float degreesPerSecond)
        {
            var spin = bar != null ? bar.GetComponentInChildren<SpinningBar>(true) : null;
            if (spin == null) return;
            var so = new SerializedObject(spin);
            so.FindProperty("startAngle").floatValue = startAngle;
            so.FindProperty("degreesPerSecond").floatValue = degreesPerSecond;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void KillPlane(GameObject parent, Vector3 position, Vector3 scale)
        {
            var plane = P(parent, "Obstacles/KillPlane", position.x, position.y, position.z);
            if (plane != null) plane.transform.localScale = scale;
        }
    }
}
