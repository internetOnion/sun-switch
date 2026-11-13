using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SunSwitch.Art.EditorTools
{
    /// <summary>
    /// Import step for the Lightkeeper (player character) in Assets/Art/Characters/Lightkeeper/.
    ///
    /// KayKit/RogueHooded.fbx (KayKit Adventurers, CC0) is used as-is from the pack. On import:
    ///   - Humanoid avatar, so any humanoid animation (e.g. Starter Assets) can drive it
    ///   - only the clips the player needs are kept, looping where they should
    ///   - the weapons that ship inside the model are removed
    ///   - the gradient-atlas texture is baked into vertex colours, recoloured to the
    ///     Lightkeeper palette (the Storybook shader reads vertex colours, not textures)
    ///   - Storybook ink data is baked (MeshInk, skinned mode)
    /// Lantern.fbx comes from ArtSource/lantern.py and gets the ink bake like the ruins.
    /// </summary>
    public class LightkeeperPostprocessor : AssetPostprocessor
    {
        public const string Folder = "Assets/Art/Characters/Lightkeeper/";
        public const string CharacterPath = Folder + "KayKit/RogueHooded.fbx";
        public const string LanternPath = Folder + "Lantern.fbx";
        const string TexturePath = Folder + "KayKit/rogue_texture.png";

        // The KayKit model is ~2.2 m tall; this makes it ~1.8 m, the Starter Assets controller height.
        public const float CharacterScale = 0.82f;

        // Bump when the bake changes so Unity reimports the models.
        public override uint GetVersion() => 3;

        // Clip name in the KayKit file -> loops?
        static readonly Dictionary<string, bool> Clips = new Dictionary<string, bool>
        {
            { "Idle", true },
            { "Walking_A", true },
            { "Running_A", true },
            { "Jump_Start", false },
            { "Jump_Idle", true },
            { "Jump_Land", false },
            { "Death_A", false },
            { "Cheer", true },
        };

        static readonly HashSet<string> Weapons = new HashSet<string>
        {
            "Knife", "Knife_Offhand", "1H_Crossbow", "2H_Crossbow", "Throwable",
        };

        // Texture atlas: 8 x 4 swatches, 128 x 256 px each, row 0 at the top.
        // Swatch (column, row) -> new colour (sRGB). Everything else keeps the KayKit colour.
        const int SwatchColumns = 8, SwatchRows = 4;
        static readonly Dictionary<Vector2Int, Color> Recolor = new Dictionary<Vector2Int, Color>
        {
            { new Vector2Int(1, 1), Hex("F4E8CF") },   // hood + cape: parchment cream (was green)
            { new Vector2Int(0, 1), Hex("AD5149") },   // tunic: muted red (was teal)
        };

        bool IsCharacter => assetPath == CharacterPath;
        bool IsLantern => assetPath == LanternPath;

        void OnPreprocessModel()
        {
            if (!IsCharacter && !IsLantern) return;
            var importer = (ModelImporter)assetImporter;
            importer.useFileScale = true;
            importer.bakeAxisConversion = true;
            importer.importNormals = ModelImporterNormals.Import;
            importer.importBlendShapes = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.meshCompression = ModelImporterMeshCompression.Off;
            importer.addCollider = false;
            // MeshInk rewrites the triangles, which Unity only allows on readable meshes.
            importer.isReadable = true;

            if (IsCharacter)
            {
                context.DependsOnSourceAsset(TexturePath);
                importer.globalScale = CharacterScale;
                importer.animationType = ModelImporterAnimationType.Human;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                AddChestMapping(importer);
                importer.importAnimation = true;
                importer.optimizeGameObjects = false;   // the lantern hangs from the chest bone
            }
            else
            {
                importer.globalScale = CharacterScale;   // modelled in the KayKit model's units
                importer.importAnimation = false;
                importer.animationType = ModelImporterAnimationType.None;
            }
        }

        void OnPreprocessAnimation()
        {
            if (!IsCharacter) return;
            var importer = (ModelImporter)assetImporter;
            var keep = new List<ModelImporterClipAnimation>();
            foreach (var clip in importer.defaultClipAnimations)
            {
                string name = ClipName(clip.takeName);
                if (!Clips.TryGetValue(name, out bool loop)) continue;
                clip.name = name;
                clip.loopTime = loop;
                clip.loopPose = loop;
                // Movement comes from the CharacterController, so keep every clip in place.
                clip.lockRootRotation = true;
                clip.lockRootHeightY = true;
                clip.lockRootPositionXZ = true;
                // "Body Orientation" keeps the body facing the GameObject's +Z, which is the
                // direction the Starter Assets controller moves in.
                clip.keepOriginalOrientation = false;
                clip.keepOriginalPositionY = true;
                clip.keepOriginalPositionXZ = true;
                keep.Add(clip);
            }
            if (keep.Count != Clips.Count)
                Debug.LogWarning($"[Sun Switch] {assetPath}: found {keep.Count} of {Clips.Count} Lightkeeper clips.");
            importer.clipAnimations = keep.ToArray();
        }

        void OnPostprocessModel(GameObject root)
        {
            if (!IsCharacter && !IsLantern) return;

            if (IsCharacter)
            {
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    if (t != null && Weapons.Contains(t.name)) Object.DestroyImmediate(t.gameObject);

                // KayKit faces -Z in Unity. Turn the skeleton (the skinned meshes and the cape
                // follow it) so the Lightkeeper faces +Z like every Unity character controller.
                var rig = root.transform.Find("Rig");
                if (rig != null) rig.localRotation = Quaternion.Euler(0f, 180f, 0f) * rig.localRotation;
                else Debug.LogError($"[Sun Switch] {assetPath}: no 'Rig' node; the Lightkeeper will face -Z.");
            }

            var atlas = IsCharacter ? LoadAtlas() : null;
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                bool skinned = renderer is SkinnedMeshRenderer;
                var mesh = skinned
                    ? ((SkinnedMeshRenderer)renderer).sharedMesh
                    : renderer.GetComponent<MeshFilter>()?.sharedMesh;
                if (mesh == null) continue;
                if (!mesh.isReadable)
                {
                    Debug.LogError($"[Sun Switch] {assetPath}: mesh {mesh.name} is not readable, ink data not baked.");
                    continue;
                }
                if (atlas != null) BakeAtlasColors(mesh, atlas);
                MeshInk.Bake(mesh, skinned);
                Debug.Log($"[Sun Switch] Lightkeeper mesh {renderer.name}: {mesh.triangles.Length / 3} tris, " +
                          $"skinned {skinned}, bones {(skinned ? ((SkinnedMeshRenderer)renderer).bones.Length : 0)}");
            }
            if (atlas != null) Object.DestroyImmediate(atlas);
        }

        // Unity's automatic Humanoid mapping leaves KayKit's "chest" bone out (it has no neck or
        // shoulder bones), so the chest would stay stiff. The mapping only exists after the first
        // import, so add Chest then and reimport once.
        static readonly HashSet<string> ChestReimported = new HashSet<string>();

        static bool AddChestMapping(ModelImporter importer)
        {
            var hd = importer.humanDescription;
            if (hd.human == null || hd.human.Length == 0) return false;
            foreach (var bone in hd.human)
                if (bone.humanName == "Chest") return false;
            var list = new List<HumanBone>(hd.human)
            {
                new HumanBone { humanName = "Chest", boneName = "chest", limit = new HumanLimit { useDefaultValues = true } },
            };
            hd.human = list.ToArray();
            importer.humanDescription = hd;
            return true;
        }

        static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            foreach (var path in imported)
            {
                if (path != CharacterPath) continue;
                var importer = (ModelImporter)AssetImporter.GetAtPath(path);
                if (ChestReimported.Add(path) && AddChestMapping(importer))
                {
                    importer.SaveAndReimport();
                    continue;
                }
                var avatar = AssetDatabase.LoadAssetAtPath<Avatar>(path);
                if (avatar == null || !avatar.isValid || !avatar.isHuman)
                    Debug.LogError($"[Sun Switch] {path}: Humanoid avatar is missing or invalid. " +
                                   "Open the model's Rig tab > Configure to fix the bone mapping.");
                else
                    Debug.Log($"[Sun Switch] {path}: Humanoid avatar OK.");
            }
        }

        static string ClipName(string takeName)
        {
            int bar = takeName.LastIndexOf('|');
            return bar >= 0 ? takeName.Substring(bar + 1) : takeName;
        }

        static Texture2D LoadAtlas()
        {
            if (!File.Exists(TexturePath))
            {
                Debug.LogError($"[Sun Switch] {TexturePath} is missing; the Lightkeeper will be white.");
                return null;
            }
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false, false);
            tex.LoadImage(File.ReadAllBytes(TexturePath));
            return tex;
        }

        /// Point-samples the atlas at each vertex's UV into vertex colours (linear, as the shader reads them raw).
        static void BakeAtlasColors(Mesh mesh, Texture2D atlas)
        {
            var uv = mesh.uv;
            if (uv == null || uv.Length != mesh.vertexCount) return;
            int w = atlas.width, h = atlas.height;
            int cellW = w / SwatchColumns, cellH = h / SwatchRows;
            var pixels = atlas.GetPixels32();

            var cellMaxLum = new Dictionary<Vector2Int, float>();
            foreach (var cell in Recolor.Keys)
            {
                float max = 0.001f;
                int y0 = h - (cell.y + 1) * cellH;   // texture rows start at the bottom
                for (int y = y0; y < y0 + cellH; y++)
                for (int x = cell.x * cellW; x < (cell.x + 1) * cellW; x++)
                    max = Mathf.Max(max, Luma(pixels[y * w + x]));
                cellMaxLum[cell] = max;
            }

            var colors = new Color[uv.Length];
            for (int i = 0; i < uv.Length; i++)
            {
                int x = Mathf.Clamp(Mathf.FloorToInt(Mathf.Repeat(uv[i].x, 1f) * w), 0, w - 1);
                int y = Mathf.Clamp(Mathf.FloorToInt(Mathf.Repeat(uv[i].y, 1f) * h), 0, h - 1);
                Color c = pixels[y * w + x];
                var cell = new Vector2Int(x / cellW, SwatchRows - 1 - y / cellH);
                if (Recolor.TryGetValue(cell, out var target))
                {
                    // Keep the swatch's light-to-dark gradient, softened.
                    float shade = 0.55f + 0.45f * Luma(pixels[y * w + x]) / cellMaxLum[cell];
                    c = target * shade;
                }
                c.a = 1f;
                colors[i] = c.linear;
            }
            mesh.colors = colors;
        }

        static float Luma(Color32 c) => (0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b) / 255f;

        static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out var c);
            return c;
        }
    }
}
