using System.Collections.Generic;
using SunSwitch.Art;
using SunSwitch.Obstacles;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SunSwitch.Art.EditorTools
{
    /// <summary>
    /// Builds the obstacle prefabs (GDD §5) from the meshes made by ArtSource/obstacles.py.
    ///
    /// Obstacles sit ON a platform: the pivot is the bottom centre, so place them at the
    /// platform's top y. Same layout rules as the platforms: colliders on the root (or a named
    /// child for triggers), art in Visual. Behaviour comes from the gameplay scripts in
    /// Assets/Scripts (SunSwitch.Obstacles, SunSwitch.Core). Every root has a PhaseObject;
    /// variants only swap the phase material and set the PhaseObject tag + ghost material.
    ///
    ///   KillBar_X            root: post collider, PhaseObject
    ///   ├─ Visual            post
    ///   └─ Arm               SpinningBar + kinematic Rigidbody + trigger + KillSurface, 0.75 m up
    ///        └─ Visual       striped bar (5.5 m, sweeps a 2.75 m radius)
    ///   JumpPad_X            root: stone collider, PhaseObject; Trigger child: JumpPad
    ///   KillSurface_S/M_X    root: base collider, PhaseObject; Kill child: trigger + KillSurface
    ///   Shard_Day / _Night   root: trigger + Shard + PhaseObject; Visual bobs (GDD §4)
    ///   KillPlane            invisible 40 x 40 m trigger + KillSurface; put one under each section
    ///
    /// Menu commands (both safe to run again):
    ///   Build Obstacle Prefabs      – creates or updates the prefabs in place (same GUIDs)
    ///   Add Obstacle Row To Sandbox – puts one of each on platforms in Platforms_Sandbox
    /// </summary>
    public static class ObstaclePrefabBuilder
    {
        const string MeshDir = "Assets/Art/Meshes/Obstacles";
        const string PhaseMaterialDir = "Assets/Art/Materials/Phase";
        const string MaterialDir = "Assets/Art/Materials/Obstacles";
        const string BaseDir = "Assets/Prefabs/Obstacles/Base";
        const string VariantDir = "Assets/Prefabs/Obstacles";
        const string SandboxPath = "Assets/Scenes/Sandbox/Platforms_Sandbox.unity";
        const string LitShader = "SunSwitch/Storybook";

        const float BarHeight = 0.75f;   // bar centre above the post's bottom (ArtSource/obstacles.py)
        const float BarLength = 5.5f;
        const float BarThickness = 0.34f;

        static readonly string[] Phases = { "Day", "Night", "Both" };

        [MenuItem("Sun Switch/Art/Build Obstacle Prefabs")]
        public static void Build()
        {
            PlatformPrefabBuilder.EnsureFolder(BaseDir);
            PlatformPrefabBuilder.EnsureFolder(MaterialDir);
            var phaseMats = new Dictionary<string, Material>();
            foreach (var p in Phases)
            {
                phaseMats[p] = AssetDatabase.LoadAssetAtPath<Material>($"{PhaseMaterialDir}/M_Phase_{p}.mat");
                if (phaseMats[p] == null)
                {
                    Debug.LogError($"[Sun Switch] {PhaseMaterialDir}/M_Phase_{p}.mat missing. Run Build Grey-box Platforms first.");
                    return;
                }
            }
            var shardMats = new Dictionary<string, Material>
            {
                ["Day"] = ShardMaterial("M_Shard_Day", Hex("E9B64F"), new Color(1.6f, 1.05f, 0.3f)),
                ["Night"] = ShardMaterial("M_Shard_Night", Hex("8C9CE0"), new Color(0.55f, 0.75f, 1.8f)),
            };

            int count = 0;
            count += BuildPhased("KillBar", BuildKillBar, phaseMats);
            count += BuildPhased("JumpPad", BuildJumpPad, phaseMats);
            count += BuildPhased("KillSurface_S", root => BuildKillSurface(root, "Obstacle_KillSurface_S", 2f), phaseMats);
            count += BuildPhased("KillSurface_M", root => BuildKillSurface(root, "Obstacle_KillSurface_M", 4f), phaseMats);
            count += BuildPhased("Shard", BuildShard, shardMats);
            count += BuildSingle("KillPlane", BuildKillPlane);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Sun Switch] Built {count} obstacle prefabs in {VariantDir}.");
        }

        [MenuItem("Sun Switch/Art/Add Obstacle Row To Sandbox")]
        public static void AddSandboxRow()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != SandboxPath)
            {
                Debug.LogError($"[Sun Switch] Open {SandboxPath} first.");
                return;
            }
            foreach (var root in scene.GetRootGameObjects())
                if (root.name == "Obstacle Row") Undo.DestroyObjectImmediate(root);

            var row = new GameObject("Obstacle Row");
            Undo.RegisterCreatedObjectUndo(row, "Add obstacle row");
            const float z = 36f;
            Place(row, "Platforms/Platform_Wide_Both", new Vector3(3, 0, z));
            Place(row, "Obstacles/KillBar_Both", new Vector3(3, 0, z));
            Place(row, "Platforms/Platform_Medium_Day", new Vector3(11, 0, z));
            Place(row, "Obstacles/JumpPad_Day", new Vector3(11, 0, z));
            Place(row, "Platforms/Platform_Medium_Night", new Vector3(17, 0, z));
            Place(row, "Obstacles/KillSurface_M_Night", new Vector3(17, 0, z));
            Place(row, "Platforms/Platform_Small_Both", new Vector3(22, 0, z));
            Place(row, "Obstacles/KillSurface_S_Both", new Vector3(22, 0, z));
            Place(row, "Platforms/Platform_Small_Day", new Vector3(25, 0, z));
            Place(row, "Obstacles/Shard_Day", new Vector3(25, 1.2f, z));
            Place(row, "Platforms/Platform_Small_Night", new Vector3(28, 0, z));
            Place(row, "Obstacles/Shard_Night", new Vector3(28, 1.2f, z));
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("[Sun Switch] Added the obstacle row to the sandbox (z = 36).");
        }

        // ---------------------------------------------------------------- prefabs

        static int BuildPhased(string name, System.Action<GameObject> build, Dictionary<string, Material> mats)
        {
            string basePath = $"{BaseDir}/{name}.prefab";
            GameObject basePrefab;
            if (AssetDatabase.LoadAssetAtPath<GameObject>(basePath) != null)
            {
                var root = PrefabUtility.LoadPrefabContents(basePath);
                PhaseHookup.RemoveMissingScripts(root);
                build(root);
                SetMaterial(root, mats.ContainsKey("Both") ? mats["Both"] : mats["Day"]);
                PhaseHookup.AddPhaseObject(root);
                basePrefab = PrefabUtility.SaveAsPrefabAsset(root, basePath);
                PrefabUtility.UnloadPrefabContents(root);
            }
            else
            {
                var root = new GameObject(name);
                build(root);
                SetMaterial(root, mats.ContainsKey("Both") ? mats["Both"] : mats["Day"]);
                PhaseHookup.AddPhaseObject(root);
                basePrefab = PrefabUtility.SaveAsPrefabAsset(root, basePath);
                Object.DestroyImmediate(root);
            }

            int count = 0;
            foreach (var (phase, mat) in mats)
            {
                string path = $"{VariantDir}/{name}_{phase}.prefab";
                if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
                {
                    var v = PrefabUtility.LoadPrefabContents(path);
                    SetMaterial(v, mat);
                    PhaseHookup.SetPhase(v, phase);
                    PrefabUtility.SaveAsPrefabAsset(v, path);
                    PrefabUtility.UnloadPrefabContents(v);
                }
                else
                {
                    var v = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);
                    v.name = $"{name}_{phase}";
                    SetMaterial(v, mat);
                    PhaseHookup.SetPhase(v, phase);
                    PrefabUtility.SaveAsPrefabAsset(v, path);
                    Object.DestroyImmediate(v);
                }
                count++;
            }
            return count;
        }

        /// One prefab with no phase variants (always solid / always active).
        static int BuildSingle(string name, System.Action<GameObject> build)
        {
            string path = $"{VariantDir}/{name}.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
            {
                var root = PrefabUtility.LoadPrefabContents(path);
                PhaseHookup.RemoveMissingScripts(root);
                build(root);
                PrefabUtility.SaveAsPrefabAsset(root, path);
                PrefabUtility.UnloadPrefabContents(root);
            }
            else
            {
                var root = new GameObject(name);
                build(root);
                PrefabUtility.SaveAsPrefabAsset(root, path);
                Object.DestroyImmediate(root);
            }
            return 1;
        }

        static void BuildKillBar(GameObject root)
        {
            var post = GetOrAdd<BoxCollider>(root);
            post.center = new Vector3(0, 0.3f, 0);
            post.size = new Vector3(0.6f, 0.6f, 0.6f);
            Visual(root, "Obstacle_KillBar_Post");

            var arm = Child(root, "Arm");
            arm.transform.localPosition = new Vector3(0, BarHeight, 0);
            var body = GetOrAdd<Rigidbody>(arm);
            body.isKinematic = true;
            body.useGravity = false;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            GetOrAdd<SpinningBar>(arm);
            var hit = GetOrAdd<BoxCollider>(arm);
            hit.isTrigger = true;
            hit.center = Vector3.zero;
            hit.size = new Vector3(BarLength, BarThickness, BarThickness);
            GetOrAdd<KillSurface>(arm);
            Visual(arm, "Obstacle_KillBar_Bar");
        }

        static void BuildJumpPad(GameObject root)
        {
            var stone = GetOrAdd<BoxCollider>(root);
            stone.center = new Vector3(0, 0.085f, 0);
            stone.size = new Vector3(2f, 0.17f, 2f);
            var visual = Visual(root, "Obstacle_JumpPad");

            var trigger = Child(root, "Trigger");
            trigger.transform.localPosition = new Vector3(0, 0.35f, 0);
            var box = GetOrAdd<BoxCollider>(trigger);
            box.isTrigger = true;
            box.center = Vector3.zero;
            box.size = new Vector3(1.7f, 0.4f, 1.7f);
            var pad = GetOrAdd<JumpPad>(trigger);
            var so = new SerializedObject(pad);
            so.FindProperty("visual").objectReferenceValue = visual.transform;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void BuildKillSurface(GameObject root, string mesh, float size)
        {
            var bed = GetOrAdd<BoxCollider>(root);
            bed.center = new Vector3(0, 0.05f, 0);
            bed.size = new Vector3(size, 0.1f, size);
            Visual(root, mesh);

            // From the base up to well above the spike tips (0.44 m), so a CharacterController
            // touching the spikes from any height is clearly inside it.
            var kill = Child(root, "Kill");
            kill.transform.localPosition = new Vector3(0, 0.35f, 0);
            var box = GetOrAdd<BoxCollider>(kill);
            box.isTrigger = true;
            box.center = Vector3.zero;
            box.size = new Vector3(size - 0.1f, 0.5f, size - 0.1f);
            GetOrAdd<KillSurface>(kill);
        }

        static void BuildKillPlane(GameObject root)
        {
            // Invisible catch-all under a section, so a fall mid-tower doesn't have to reach the
            // void line far below. Top face at the pivot; scale x/z per section if needed.
            var box = GetOrAdd<BoxCollider>(root);
            box.isTrigger = true;
            box.center = new Vector3(0, -0.5f, 0);
            box.size = new Vector3(40f, 1f, 40f);
            GetOrAdd<KillSurface>(root);
        }

        static void BuildShard(GameObject root)
        {
            var sphere = GetOrAdd<SphereCollider>(root);
            sphere.isTrigger = true;
            sphere.radius = 0.6f;
            var visual = Visual(root, "Obstacle_Shard");
            var shard = GetOrAdd<Shard>(root);
            var so = new SerializedObject(shard);
            so.FindProperty("visual").objectReferenceValue = visual.transform;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------------------------------------------------------------- helpers

        static GameObject Visual(GameObject parent, string meshName)
        {
            var visual = Child(parent, "Visual");
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>($"{MeshDir}/{meshName}.fbx");
            if (mesh == null) Debug.LogError($"[Sun Switch] {MeshDir}/{meshName}.fbx missing. Export ArtSource/obstacles.py.");
            GetOrAdd<MeshFilter>(visual).sharedMesh = mesh;
            GetOrAdd<MeshRenderer>(visual);
            return visual;
        }

        static void SetMaterial(GameObject root, Material mat)
        {
            foreach (var r in root.GetComponentsInChildren<MeshRenderer>(true)) r.sharedMaterial = mat;
        }

        static Material ShardMaterial(string name, Color color, Color emission)
        {
            string path = $"{MaterialDir}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(Shader.Find(LitShader));
                AssetDatabase.CreateAsset(m, path);
            }
            m.SetColor("_BaseColor", color);
            m.SetColor("_EmissionColor", emission);
            m.SetFloat("_ShadowThreshold", -0.4f);   // crystals stay mostly lit
            m.SetFloat("_RimStrength", 0.6f);
            m.SetFloat("_OutlineWidth", 0.012f);
            m.SetFloat("_EdgeInkWidth", 0.01f);
            m.SetFloat("_NoiseScale", 4f);
            m.enableInstancing = true;
            EditorUtility.SetDirty(m);
            return m;
        }

        static void Place(GameObject parent, string prefab, Vector3 position)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Prefabs/{prefab}.prefab");
            if (asset == null)
            {
                Debug.LogError($"[Sun Switch] Assets/Prefabs/{prefab}.prefab missing.");
                return;
            }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent.scene);
            go.transform.SetParent(parent.transform, false);
            go.transform.position = position;
        }

        static GameObject Child(GameObject parent, string name)
        {
            var t = parent.transform.Find(name);
            if (t != null) return t.gameObject;
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            return go;
        }

        static T GetOrAdd<T>(GameObject go) where T : Component =>
            go.TryGetComponent<T>(out var c) ? c : go.AddComponent<T>();

        static Color Hex(string hex) => ColorUtility.TryParseHtmlString("#" + hex, out var c) ? c : Color.magenta;
    }
}
