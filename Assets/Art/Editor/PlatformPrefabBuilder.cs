using System.Collections.Generic;
using SunSwitch.Obstacles;
using SunSwitch.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SunSwitch.Art.EditorTools
{
    /// <summary>
    /// Builds and updates the platform prefabs, their Day / Night / Both variants,
    /// the phase materials and the platform sandbox scene.
    ///
    /// Prefab layout (pivot = top surface centre, except walls = bottom centre):
    ///   Platform_X            root: BoxCollider (never scaled), PhaseObject
    ///   └─ Visual             real-size mesh + material, safe to replace with final art
    ///   Platform_Moving_X     + MovingPlatform, with PointA / PointB children for its ends
    ///   Checkpoint            Both platform + Beacon + CheckpointLamp; Trigger child: Checkpoint,
    ///                         Spawn point, Lamp (ember until reached, then lit), Lit (the light,
    ///                         switched on once reached), Flare/Beam + Ring (the reached burst)
    ///
    /// Variants set the phase material and the PhaseObject tag + ghost material.
    ///
    /// Menu commands:
    ///   Build Grey-box Platforms  – creates everything from scratch. Overwrites the prefabs,
    ///                               so placed instances may lose overrides. Grey-box only.
    ///   Update Platform Prefabs   – safe at any time: updates materials, meshes and gameplay
    ///                               components inside the existing prefabs without replacing them.
    /// </summary>
    public static class PlatformPrefabBuilder
    {
        const string MaterialDir = "Assets/Art/Materials/Phase";
        const string RuinDir = "Assets/Art/Meshes/Ruins";
        const string BaseDir = "Assets/Prefabs/Platforms/Base";
        const string VariantDir = "Assets/Prefabs/Platforms";
        const string SandboxDir = "Assets/Scenes/Sandbox";
        const string SandboxPath = SandboxDir + "/Platforms_Sandbox.unity";
        const string LitShader = "SunSwitch/Storybook";
        const string GhostShader = "SunSwitch/StorybookGhost";

        enum Phase { Day, Night, Both }
        enum Shape { Box, Cylinder }

        sealed class Spec
        {
            public string Name;
            public Shape Shape;
            public Vector3 Size;      // metres: width (x), thickness/height (y), depth (z)
            public bool PivotOnTop;   // true for platforms you stand on, false for walls
            public bool Phased = true;
            public Vector3 Centre => new Vector3(0, PivotOnTop ? -Size.y / 2f : Size.y / 2f, 0);
        }

        static readonly Spec[] Specs =
        {
            new Spec { Name = "Platform_Small",  Shape = Shape.Box,      Size = new Vector3(2, 0.5f, 2), PivotOnTop = true },
            new Spec { Name = "Platform_Medium", Shape = Shape.Box,      Size = new Vector3(4, 0.5f, 4), PivotOnTop = true },
            new Spec { Name = "Platform_Wide",   Shape = Shape.Box,      Size = new Vector3(6, 0.5f, 6), PivotOnTop = true },
            new Spec { Name = "Platform_Bridge", Shape = Shape.Box,      Size = new Vector3(2, 0.4f, 8), PivotOnTop = true },
            new Spec { Name = "Platform_Pillar", Shape = Shape.Cylinder, Size = new Vector3(2, 1f, 2),   PivotOnTop = true },
            new Spec { Name = "Platform_Moving", Shape = Shape.Box,      Size = new Vector3(3, 0.4f, 3), PivotOnTop = true },
            new Spec { Name = "Wall",            Shape = Shape.Box,      Size = new Vector3(4, 4f, 0.5f), PivotOnTop = false },
            new Spec { Name = "Checkpoint",      Shape = Shape.Box,      Size = new Vector3(4, 0.5f, 4), PivotOnTop = true, Phased = false },
        };

        // Colours follow the GDD colour code, tuned toward the palette in Docs/UI_ART_STYLE.md.
        static readonly Color DayColor = Hex("E9B64F");   // sun gold
        static readonly Color NightColor = Hex("6670B8"); // night indigo, lifted so it reads under lighting
        static readonly Color BothColor = Hex("9A958C");  // warm grey stone
        static readonly Color CreamColor = Hex("FFF5DE"); // cloud cream
        const float BeaconHeight = 2.5f;
        const float BeaconRadius = 0.125f;
        const float MovingTravel = 6f;   // default distance between PointA and PointB (m)

        // ---------------------------------------------------------------- menu commands

        [MenuItem("Sun Switch/Art/Build Grey-box Platforms")]
        public static void Build()
        {
            bool exists = AssetDatabase.LoadAssetAtPath<GameObject>($"{BaseDir}/Platform_Small.prefab") != null;
            if (exists && !EditorUtility.DisplayDialog(
                    "Rebuild platform prefabs?",
                    "This replaces every prefab in Assets/Prefabs/Platforms. Hand edits are lost and platforms " +
                    "already placed in scenes may lose their overrides.\n\nTo only refresh the look, use " +
                    "Sun Switch > Art > Update Platform Prefabs instead.",
                    "Rebuild", "Cancel"))
                return;

            EnsureFolder(BaseDir);
            EnsureFolder(SandboxDir);
            var mats = ConfigureMaterials(out var beacon, out var beaconLit);

            var variants = new List<(Spec spec, Phase phase, GameObject prefab)>();
            foreach (var spec in Specs)
            {
                var root = new GameObject(spec.Name);
                var box = spec.Shape == Shape.Box ? root.AddComponent<BoxCollider>() : null;
                if (box != null) { box.center = spec.Centre; box.size = spec.Size; }
                ApplyGeometry(root, spec, mats[Phase.Both], beacon);
                ApplyGameplay(root, spec, beaconLit);
                var basePrefab = PrefabUtility.SaveAsPrefabAsset(root, $"{BaseDir}/{spec.Name}.prefab");
                Object.DestroyImmediate(root);

                if (!spec.Phased)
                {
                    // Checkpoints always sit on Both platforms (GDD 4), so the base is the usable prefab.
                    variants.Add((spec, Phase.Both, basePrefab));
                    continue;
                }

                foreach (Phase phase in System.Enum.GetValues(typeof(Phase)))
                    variants.Add((spec, phase, SaveVariant(basePrefab, spec, phase, mats[phase])));
            }

            AssetDatabase.SaveAssets();
            if (!System.IO.File.Exists(SandboxPath)) BuildSandbox(variants);
            Debug.Log($"[Sun Switch] Built {variants.Count} platform prefabs in {VariantDir}.");
        }

        [MenuItem("Sun Switch/Art/Update Platform Prefabs")]
        public static void UpdatePrefabs()
        {
            var mats = ConfigureMaterials(out var beacon, out var beaconLit);
            int updated = 0, variants = 0;
            foreach (var spec in Specs)
            {
                string path = $"{BaseDir}/{spec.Name}.prefab";
                if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
                {
                    Debug.LogWarning($"[Sun Switch] {path} not found. Run Build Grey-box Platforms first.");
                    continue;
                }
                var root = PrefabUtility.LoadPrefabContents(path);
                ApplyGeometry(root, spec, mats[Phase.Both], beacon);
                ApplyGameplay(root, spec, beaconLit);
                PrefabUtility.SaveAsPrefabAsset(root, path);
                PrefabUtility.UnloadPrefabContents(root);
                updated++;

                if (!spec.Phased) continue;
                foreach (Phase phase in System.Enum.GetValues(typeof(Phase)))
                {
                    string vpath = $"{VariantDir}/{spec.Name}_{phase}.prefab";
                    if (AssetDatabase.LoadAssetAtPath<GameObject>(vpath) == null) continue;
                    var v = PrefabUtility.LoadPrefabContents(vpath);
                    PhaseHookup.SetPhase(v, phase.ToString());
                    PrefabUtility.SaveAsPrefabAsset(v, vpath);
                    PrefabUtility.UnloadPrefabContents(v);
                    variants++;
                }
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[Sun Switch] Updated {updated} base prefabs and {variants} phase variants.");
        }

        // ---------------------------------------------------------------- geometry

        /// <summary>Creates or updates Visual (and Collision / Beacon) children in place.</summary>
        static void ApplyGeometry(GameObject root, Spec spec, Material baseMat, Material beaconMat)
        {
            // Grey-box mesh: the fallback visual, and always the pillar's collision shape.
            var mesh = spec.Shape == Shape.Cylinder
                ? GreyboxMeshes.Cylinder($"Mesh_{spec.Name}", spec.Size.x / 2f, spec.Size.y)
                : GreyboxMeshes.Box($"Mesh_{spec.Name}", spec.Size);
            // Ruin model from Blender (ArtSource/ruin_platforms.py), pivoted like the prefab.
            var ruin = AssetDatabase.LoadAssetAtPath<Mesh>($"{RuinDir}/Ruin_{spec.Name}.fbx");

            var visual = GetOrCreateChild(root, "Visual");
            visual.transform.localPosition = ruin != null ? Vector3.zero : spec.Centre;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;
            GetOrAdd<MeshFilter>(visual).sharedMesh = ruin != null ? ruin : mesh;
            var renderer = GetOrAdd<MeshRenderer>(visual);
            if (renderer.sharedMaterial == null) renderer.sharedMaterial = baseMat;

            if (spec.Shape == Shape.Cylinder)
            {
                // Round collider lives on its own child so Visual can still be swapped freely.
                var collision = GetOrCreateChild(root, "Collision");
                collision.transform.localPosition = spec.Centre;
                collision.transform.localScale = Vector3.one;
                var mc = GetOrAdd<MeshCollider>(collision);
                mc.sharedMesh = mesh;
                mc.convex = true;
            }

            if (spec.Name == "Checkpoint")
            {
                var beacon = GetOrCreateChild(root, "Beacon");
                beacon.transform.localPosition = new Vector3(spec.Size.x / 2f - 0.5f, BeaconHeight / 2f, spec.Size.z / 2f - 0.5f);
                beacon.transform.localScale = Vector3.one;
                GetOrAdd<MeshFilter>(beacon).sharedMesh = GreyboxMeshes.Cylinder("Mesh_Checkpoint_Beacon", BeaconRadius, BeaconHeight, 12);
                GetOrAdd<MeshRenderer>(beacon).sharedMaterial = beaconMat;
            }
        }

        /// Gameplay components from Assets/Scripts, set up in place on a base prefab.
        static void ApplyGameplay(GameObject root, Spec spec, Material beaconLit)
        {
            if (spec.Name == "Platform_Moving")
            {
                var mover = GetOrAdd<MovingPlatform>(root);
                var a = GetOrCreateChild(root, "PointA");
                var b = GetOrCreateChild(root, "PointB");
                var so = new SerializedObject(mover);
                // Only place the ends the first time; designers move them per instance.
                if (so.FindProperty("pointA").objectReferenceValue == null) a.transform.localPosition = Vector3.zero;
                if (so.FindProperty("pointB").objectReferenceValue == null) b.transform.localPosition = new Vector3(MovingTravel, 0, 0);
                so.FindProperty("pointA").objectReferenceValue = a.transform;
                so.FindProperty("pointB").objectReferenceValue = b.transform;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            if (spec.Name == "Checkpoint") ApplyCheckpoint(root, spec, beaconLit);

            if (spec.Phased) PhaseHookup.AddPhaseObject(root);
        }

        static void ApplyCheckpoint(GameObject root, Spec spec, Material beaconLit)
        {
            // The lamp on top of the beacon: always there (a faint ember until reached), driven by
            // CheckpointLamp. Lit holds the light, which Checkpoint switches on when reached.
            var beacon = root.transform.Find("Beacon");
            var top = beacon.localPosition + new Vector3(0, BeaconHeight / 2f, 0);
            var lamp = GetOrCreateChild(root, "Lamp");
            lamp.transform.localPosition = top;
            lamp.transform.localRotation = Quaternion.identity;
            GetOrAdd<MeshFilter>(lamp).sharedMesh = GreyboxMeshes.Cylinder("Mesh_Checkpoint_Glow", 0.24f, 0.4f, 12);
            var lampRenderer = GetOrAdd<MeshRenderer>(lamp);
            lampRenderer.sharedMaterial = beaconLit;
            lampRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            var lit = GetOrCreateChild(root, "Lit");
            lit.transform.localPosition = top;
            lit.transform.localRotation = Quaternion.identity;
            var oldCap = lit.transform.Find("Glow");   // the cap used to live here; Lamp replaces it
            if (oldCap != null) Object.DestroyImmediate(oldCap.gameObject, true);
            var light = GetOrAdd<Light>(lit);
            light.type = LightType.Point;
            light.color = DayColor;
            light.range = 5f;
            light.intensity = 2f;
            light.shadows = LightShadows.None;
            lit.SetActive(false);   // Checkpoint switches it on; saved off so scenes show the unreached look

            // Reached burst: a beam from the lamp and a ring across the platform (saved off).
            var flare = GetOrCreateChild(root, "Flare");
            flare.transform.localPosition = top + new Vector3(0, 0.2f, 0);
            flare.transform.localRotation = Quaternion.identity;
            var beam = GetOrCreateChild(flare, "Beam");
            beam.transform.localPosition = Vector3.zero;
            beam.transform.localScale = new Vector3(0.6f, 1f, 0.6f);   // CheckpointLamp stretches y
            GetOrAdd<MeshFilter>(beam).sharedMesh = GreyboxMeshes.Cylinder("Mesh_Checkpoint_Beam", 0.14f, 1f, 12);
            var beamRenderer = GetOrAdd<MeshRenderer>(beam);
            beamRenderer.sharedMaterial = beaconLit;
            beamRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            beam.SetActive(false);
            var ring = GetOrCreateChild(root, "Ring");
            ring.transform.localPosition = new Vector3(0, 0.04f, 0);
            ring.transform.localScale = Vector3.one;
            GetOrAdd<MeshFilter>(ring).sharedMesh = GreyboxMeshes.Ring("Mesh_Checkpoint_Ring", 0.86f, 0.08f);
            var ringRenderer = GetOrAdd<MeshRenderer>(ring);
            ringRenderer.sharedMaterial = beaconLit;
            ringRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ring.SetActive(false);

            var spawn = GetOrCreateChild(root, "Spawn");
            spawn.transform.localPosition = Vector3.zero;
            spawn.transform.localRotation = Quaternion.identity;

            // Trigger sits above the platform, not on its solid collider (see Checkpoint).
            var trigger = GetOrCreateChild(root, "Trigger");
            trigger.transform.localPosition = new Vector3(0, 1f, 0);
            var box = GetOrAdd<BoxCollider>(trigger);
            box.isTrigger = true;
            box.center = Vector3.zero;
            box.size = new Vector3(spec.Size.x - 0.4f, 2f, spec.Size.z - 0.4f);
            var checkpoint = GetOrAdd<Checkpoint>(trigger);
            var so = new SerializedObject(checkpoint);
            so.FindProperty("spawnPoint").objectReferenceValue = spawn.transform;
            so.FindProperty("activeIndicator").objectReferenceValue = lit;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Recreated each time so its tuning always matches the script's defaults.
            var oldLamp = root.GetComponent<SunSwitch.Art.CheckpointLamp>();
            if (oldLamp != null) Object.DestroyImmediate(oldLamp, true);
            var lampScript = root.AddComponent<SunSwitch.Art.CheckpointLamp>();
            var lso = new SerializedObject(lampScript);
            lso.FindProperty("checkpoint").objectReferenceValue = checkpoint;
            lso.FindProperty("litIndicator").objectReferenceValue = lit;
            lso.FindProperty("lamp").objectReferenceValue = lampRenderer;
            lso.FindProperty("glowLight").objectReferenceValue = light;
            lso.FindProperty("beam").objectReferenceValue = beam.transform;
            lso.FindProperty("ring").objectReferenceValue = ring.transform;
            lso.FindProperty("dayGlow").colorValue = new Color(2.2f, 1.45f, 0.45f);
            lso.FindProperty("nightGlow").colorValue = new Color(0.5f, 0.75f, 1.6f);
            lso.ApplyModifiedPropertiesWithoutUndo();
        }

        static GameObject SaveVariant(GameObject basePrefab, Spec spec, Phase phase, Material mat)
        {
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);
            inst.name = $"{spec.Name}_{phase}";
            inst.transform.Find("Visual").GetComponent<MeshRenderer>().sharedMaterial = mat;
            PhaseHookup.SetPhase(inst, phase.ToString());
            var prefab = PrefabUtility.SaveAsPrefabAsset(inst, $"{VariantDir}/{inst.name}.prefab");
            Object.DestroyImmediate(inst);
            return prefab;
        }

        static void BuildSandbox(List<(Spec spec, Phase phase, GameObject prefab)> variants)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Additive);

            // One row per phase, laid out along x.
            var cursor = new Dictionary<Phase, float> { [Phase.Day] = 0, [Phase.Night] = 0, [Phase.Both] = 0 };
            var rowZ = new Dictionary<Phase, float> { [Phase.Day] = 0, [Phase.Night] = 12, [Phase.Both] = 24 };
            foreach (var (spec, phase, prefab) in variants)
            {
                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                go.transform.position = new Vector3(cursor[phase] + spec.Size.x / 2f, 0, rowZ[phase]);
                cursor[phase] += spec.Size.x + 2f;
            }

            // 1.8 m capsule for judging scale and jump gaps.
            var player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "ScaleRef_Player_1.8m";
            Object.DestroyImmediate(player.GetComponent<Collider>());
            player.transform.position = new Vector3(-2f, 0.9f, 0f);
            player.transform.localScale = new Vector3(0.6f, 0.9f, 0.6f);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(player, scene);

            EditorSceneManager.SaveScene(scene, SandboxPath);
            EditorSceneManager.CloseScene(scene, true);
        }

        // ---------------------------------------------------------------- materials

        static Dictionary<Phase, Material> ConfigureMaterials(out Material beacon, out Material beaconLit)
        {
            EnsureFolder(MaterialDir);
            var lit = Shader.Find(LitShader);
            var ghost = Shader.Find(GhostShader);
            if (lit == null || ghost == null)
                throw new System.InvalidOperationException(
                    $"[Sun Switch] Shader '{LitShader}' or '{GhostShader}' is missing or failed to compile. Check the Console.");

            var mats = new Dictionary<Phase, Material>
            {
                [Phase.Day] = Storybook("M_Phase_Day", lit, DayColor, new Color(0.80f, 0.56f, 0.52f)),
                [Phase.Night] = Storybook("M_Phase_Night", lit, NightColor, new Color(0.48f, 0.44f, 0.72f)),
                [Phase.Both] = Storybook("M_Phase_Both", lit, BothColor, new Color(0.62f, 0.60f, 0.74f)),
            };

            Ghost("M_Ghost_Day", ghost, DayColor, new Color(1f, 0.9f, 0.62f));
            Ghost("M_Ghost_Night", ghost, NightColor, new Color(0.75f, 0.8f, 1f));

            // The pole is plain cream; the gold glow (beaconLit) only shows once the checkpoint is reached.
            beacon = Storybook("M_Checkpoint_Beacon", lit, CreamColor, new Color(0.85f, 0.75f, 0.65f));
            beacon.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            beaconLit = Storybook("M_Checkpoint_Lit", lit, CreamColor, new Color(0.95f, 0.85f, 0.7f));
            beaconLit.SetColor("_EmissionColor", DayColor * 2.5f);
            beaconLit.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;

            AssetDatabase.SaveAssets();
            return mats;
        }

        static Material Storybook(string name, Shader shader, Color baseColor, Color shadowTint)
        {
            var m = GetOrCreateMaterial(name, shader);
            m.SetColor("_BaseColor", baseColor);
            m.SetColor("_ShadowTint", shadowTint);
            m.SetColor("_EmissionColor", Color.black);
            // Ink tuning lives here so every platform material stays consistent.
            m.SetFloat("_OutlineWidth", 0.02f);
            m.SetFloat("_OutlinePixels", 1.2f);
            m.SetFloat("_EdgeInkWidth", 0.02f);
            m.SetFloat("_EdgeInkPixels", 1.0f);
            m.SetFloat("_EdgeInkStrength", 0.85f);
            m.SetFloat("_EdgeInkWobble", 0.5f);
            return m;
        }

        static void Ghost(string name, Shader shader, Color fill, Color edge)
        {
            var m = GetOrCreateMaterial(name, shader);
            fill.a = 0.12f;
            m.SetColor("_BaseColor", fill);
            m.SetColor("_EdgeColor", edge);
        }

        static Material GetOrCreateMaterial(string name, Shader shader)
        {
            string path = $"{MaterialDir}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(shader);
                AssetDatabase.CreateAsset(m, path);
            }
            else if (m.shader != shader)
            {
                // Clear leftovers from the previous shader (e.g. URP Lit transparency settings).
                m.shader = shader;
                m.shaderKeywords = new string[0];
                m.SetOverrideTag("RenderType", "");
                m.renderQueue = -1;
            }
            EditorUtility.SetDirty(m);
            return m;
        }

        // ---------------------------------------------------------------- helpers

        static GameObject GetOrCreateChild(GameObject parent, string name)
        {
            var t = parent.transform.Find(name);
            if (t != null) return t.gameObject;
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            return go;
        }

        static T GetOrAdd<T>(GameObject go) where T : Component =>
            go.TryGetComponent<T>(out var c) ? c : go.AddComponent<T>();

        internal static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int slash = path.LastIndexOf('/');
            string parent = path.Substring(0, slash);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(slash + 1));
        }

        static Color Hex(string hex) => ColorUtility.TryParseHtmlString("#" + hex, out var c) ? c : Color.magenta;
    }
}
