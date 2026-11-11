using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace SunSwitch.Art.EditorTools
{
    /// <summary>
    /// Generates the grey-box platform prefabs, their Day / Night / Both variants,
    /// the phase materials and a sandbox scene to preview them.
    ///
    /// Prefab layout (pivot = top surface centre, except walls = bottom centre):
    ///   Platform_X            root: BoxCollider (never scaled)
    ///   └─ Visual             mesh + material, safe to replace with final art
    ///
    /// Re-running overwrites the prefabs and materials in place (GUIDs are kept), so
    /// only re-run it during grey-box. Once level designers use the prefabs, edit them by hand.
    /// </summary>
    public static class PlatformPrefabBuilder
    {
        const string MaterialDir = "Assets/Art/Materials/Phase";
        const string BaseDir = "Assets/Prefabs/Platforms/Base";
        const string VariantDir = "Assets/Prefabs/Platforms";
        const string SandboxDir = "Assets/Scenes/Sandbox";
        const string SandboxPath = SandboxDir + "/Platforms_Sandbox.unity";

        enum Phase { Day, Night, Both }
        enum Shape { Box, Cylinder }

        sealed class Spec
        {
            public string Name;
            public Shape Shape;
            public Vector3 Size;      // metres: width (x), thickness/height (y), depth (z)
            public bool PivotOnTop;   // true for platforms you stand on, false for walls
            public bool Phased = true;
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
        const float GhostAlpha = 0.3f;

        [MenuItem("Sun Switch/Art/Build Grey-box Platforms")]
        public static void Build()
        {
            bool exists = AssetDatabase.LoadAssetAtPath<GameObject>($"{BaseDir}/Platform_Small.prefab") != null;
            if (exists && !EditorUtility.DisplayDialog(
                    "Rebuild platform prefabs?",
                    "This overwrites every prefab in Assets/Prefabs/Platforms. Any hand edits to them will be lost.",
                    "Rebuild", "Cancel"))
                return;

            EnsureFolder(MaterialDir);
            EnsureFolder(BaseDir);
            EnsureFolder(SandboxDir);

            var mats = new Dictionary<Phase, Material>
            {
                [Phase.Day] = GetOrCreateMaterial("M_Phase_Day", DayColor),
                [Phase.Night] = GetOrCreateMaterial("M_Phase_Night", NightColor),
                [Phase.Both] = GetOrCreateMaterial("M_Phase_Both", BothColor),
            };
            GetOrCreateMaterial("M_Ghost_Day", WithAlpha(DayColor, GhostAlpha), ghost: true);
            GetOrCreateMaterial("M_Ghost_Night", WithAlpha(NightColor, GhostAlpha), ghost: true);
            var beacon = GetOrCreateMaterial("M_Checkpoint_Beacon", Hex("FFF5DE"), emission: DayColor * 2f);

            var variants = new List<(Spec spec, Phase phase, GameObject prefab)>();
            foreach (var spec in Specs)
            {
                var root = BuildBase(spec, mats[Phase.Both]);
                if (spec.Name == "Checkpoint") AddBeacon(root, spec, beacon);
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

        static GameObject BuildBase(Spec spec, Material mat)
        {
            var root = new GameObject(spec.Name);
            var centre = new Vector3(0, spec.PivotOnTop ? -spec.Size.y / 2f : spec.Size.y / 2f, 0);

            var visual = GameObject.CreatePrimitive(spec.Shape == Shape.Cylinder ? PrimitiveType.Cylinder : PrimitiveType.Cube);
            visual.name = "Visual";
            Object.DestroyImmediate(visual.GetComponent<Collider>());
            visual.transform.SetParent(root.transform, false);
            visual.transform.localPosition = centre;
            // Unity's cylinder primitive is 2 units tall at scale 1.
            visual.transform.localScale = spec.Shape == Shape.Cylinder
                ? new Vector3(spec.Size.x, spec.Size.y / 2f, spec.Size.z)
                : spec.Size;
            visual.GetComponent<MeshRenderer>().sharedMaterial = mat;

            if (spec.Shape == Shape.Box)
            {
                var box = root.AddComponent<BoxCollider>();
                box.center = centre;
                box.size = spec.Size;
            }
            else
            {
                // Round collider lives on its own child so Visual can still be swapped freely.
                var collision = new GameObject("Collision");
                collision.transform.SetParent(root.transform, false);
                collision.transform.localPosition = visual.transform.localPosition;
                collision.transform.localScale = visual.transform.localScale;
                var mc = collision.AddComponent<MeshCollider>();
                mc.sharedMesh = visual.GetComponent<MeshFilter>().sharedMesh;
                mc.convex = true;
            }
            return root;
        }

        static void AddBeacon(GameObject root, Spec spec, Material mat)
        {
            var pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.name = "Beacon";
            Object.DestroyImmediate(pole.GetComponent<Collider>());
            pole.transform.SetParent(root.transform, false);
            const float height = 2.5f;
            pole.transform.localPosition = new Vector3(spec.Size.x / 2f - 0.5f, height / 2f, spec.Size.z / 2f - 0.5f);
            pole.transform.localScale = new Vector3(0.25f, height / 2f, 0.25f);
            pole.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }

        static GameObject SaveVariant(GameObject basePrefab, Spec spec, Phase phase, Material mat)
        {
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);
            inst.name = $"{spec.Name}_{phase}";
            inst.transform.Find("Visual").GetComponent<MeshRenderer>().sharedMaterial = mat;
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
            SceneManager_Move(player, scene);

            EditorSceneManager.SaveScene(scene, SandboxPath);
            EditorSceneManager.CloseScene(scene, true);
        }

        static void SceneManager_Move(GameObject go, UnityEngine.SceneManagement.Scene scene) =>
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, scene);

        static Material GetOrCreateMaterial(string name, Color color, bool ghost = false, Color? emission = null)
        {
            string path = $"{MaterialDir}/{name}.mat";
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            else mat.shader = shader;

            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Smoothness", 0.15f);
            if (ghost) MakeTransparent(mat);
            if (emission.HasValue)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emission.Value);
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static void MakeTransparent(Material mat)
        {
            mat.SetFloat("_Surface", 1);   // Transparent
            mat.SetFloat("_Blend", 0);     // Alpha
            mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            mat.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_ZWrite", 0);
            mat.SetFloat("_CastShadows", 0);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.SetShaderPassEnabled("ShadowCaster", false);
            mat.SetShaderPassEnabled("DepthOnly", false);
            mat.renderQueue = (int)RenderQueue.Transparent;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int slash = path.LastIndexOf('/');
            string parent = path.Substring(0, slash);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(slash + 1));
        }

        static Color Hex(string hex) => ColorUtility.TryParseHtmlString("#" + hex, out var c) ? c : Color.magenta;
        static Color WithAlpha(Color c, float a) { c.a = a; return c; }
    }
}
