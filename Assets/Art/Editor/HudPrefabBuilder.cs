using System.IO;
using SunSwitch.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SunSwitch.Art.EditorTools
{
    /// <summary>
    /// Sun Switch > Art > Build HUD Prefab
    ///
    /// Builds the in-game HUD (GDD §6) as Assets/Prefabs/UI/HUD.prefab, in the menu's storybook
    /// style (Docs/UI_ART_STYLE.md: ink on parchment, sun gold, night indigo):
    ///
    ///   HUD                 Canvas (overlay, under the menu) + CanvasGroup + HUDController
    ///   ├─ Ink Wipe         full-screen ink iris that closes on a death and opens on respawn (InkIris)
    ///   ├─ Lives            parchment plate, three lantern flames (lost ones grey out)
    ///   ├─ Run              parchment plate: PhaseCooldownIcon (ring + sun / moon) and the timer
    ///   ├─ Shards           parchment plate: crystal icon and "x/12"
    ///   ├─ Checkpoint Notice  "Checkpoint" under the timer for a moment (CheckpointNotice)
    ///   └─ Hint             bottom strip: the instruction for the HintZone the player is in (HintPanel)
    ///
    /// The icons are small sprites baked here into Assets/Art/UI (ink outline, white fill, tinted
    /// by the Image colour). Safe to run again: sprites and prefab are rebuilt in place.
    /// </summary>
    public static class HudPrefabBuilder
    {
        const string SpriteDir = "Assets/Art/UI";
        const string PrefabDir = "Assets/Prefabs/UI";
        public const string PrefabPath = PrefabDir + "/HUD.prefab";
        const int SpriteSize = 128;

        static readonly Color Ink = Hex("282321");
        static readonly Color Gold = Hex("E9B64F");
        static readonly Color MoonBlue = Hex("B6CCE8");

        [MenuItem("Sun Switch/Art/Build HUD Prefab")]
        public static void Build()
        {
            PlatformPrefabBuilder.EnsureFolder(SpriteDir);
            PlatformPrefabBuilder.EnsureFolder(PrefabDir);
            var ring = BakeSprite("HUD_Ring", Ring);
            var flame = BakeSprite("HUD_Flame", Flame);
            var sun = BakeSprite("HUD_Sun", Sun);
            var moon = BakeSprite("HUD_Moon", Moon);
            var shard = BakeSprite("HUD_Shard", Crystal);

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var root = new GameObject("HUD", typeof(RectTransform));
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = -10;   // under the menu, so Pause shows over it
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            root.AddComponent<CanvasGroup>();

            // Ink wipe: first child, so the plates (and the life just lost) stay on top of the ink.
            var wipe = new GameObject("Ink Wipe", typeof(RectTransform)).GetComponent<RectTransform>();
            wipe.SetParent(root.transform, false);
            wipe.anchorMin = Vector2.zero;
            wipe.anchorMax = Vector2.one;
            wipe.offsetMin = wipe.offsetMax = Vector2.zero;
            var wipeImage = wipe.gameObject.AddComponent<Image>();
            wipeImage.material = IrisMaterial();
            wipeImage.raycastTarget = false;
            wipeImage.enabled = false;
            wipe.gameObject.AddComponent<SunSwitch.Art.InkIris>();

            // Lives: top left.
            var lives = Plate(root.transform, "Lives", new Vector2(0, 1), new Vector2(40, -36), new Vector2(300, 112));
            var lifeIcons = new Graphic[3];
            for (int i = 0; i < 3; i++)
            {
                var icon = Icon(lives, $"Life {i + 1}", flame, new Vector2(-80 + i * 80, 0), new Vector2(64, 64));
                icon.color = Gold;
                lifeIcons[i] = icon;
            }

            // Run: top centre, phase icon + timer.
            var run = Plate(root.transform, "Run", new Vector2(0.5f, 1), new Vector2(0, -36), new Vector2(440, 120));
            var phase = new GameObject("Phase", typeof(RectTransform)).GetComponent<RectTransform>();
            phase.SetParent(run, false);
            phase.anchoredPosition = new Vector2(-138, 0);
            phase.sizeDelta = new Vector2(92, 92);
            var track = Icon(phase, "Ring Track", ring, Vector2.zero, new Vector2(92, 92));
            track.color = new Color(Ink.r, Ink.g, Ink.b, 0.25f);
            var fill = Icon(phase, "Ring", ring, Vector2.zero, new Vector2(92, 92));
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Radial360;
            fill.fillOrigin = (int)Image.Origin360.Top;
            fill.fillClockwise = true;
            fill.color = Gold;
            var sunIcon = Icon(phase, "Sun", sun, Vector2.zero, new Vector2(58, 58));
            sunIcon.color = Gold;
            var moonIcon = Icon(phase, "Moon", moon, Vector2.zero, new Vector2(54, 54));
            moonIcon.color = MoonBlue;
            moonIcon.gameObject.SetActive(false);
            var cooldown = phase.gameObject.AddComponent<PhaseCooldownIcon>();
            Set(cooldown, "ring", fill);
            Set(cooldown, "sunIcon", sunIcon.gameObject);
            Set(cooldown, "moonIcon", moonIcon.gameObject);
            Set(cooldown, "blockedColor", Hex("AD5149"));

            var timer = Label(run, "Timer", font, "00:00.00", 46, new Vector2(52, 4), new Vector2(270, 64));
            timer.fontStyle = FontStyle.Bold;
            // Phase accent under the timer: gold by Day, moon blue by Night.
            var accent = new GameObject("Phase Accent", typeof(RectTransform)).AddComponent<Image>();
            accent.rectTransform.SetParent(run, false);
            accent.rectTransform.anchoredPosition = new Vector2(52, -32);
            accent.rectTransform.sizeDelta = new Vector2(220, 4);
            accent.raycastTarget = false;
            var themed = accent.gameObject.AddComponent<PhaseThemedGraphic>();
            Set(themed, "dayColor", Gold);
            Set(themed, "nightColor", Hex("8FA8E0"));

            // Shards: top right.
            var shards = Plate(root.transform, "Shards", new Vector2(1, 1), new Vector2(-40, -36), new Vector2(260, 112));
            var crystal = Icon(shards, "Shard Icon", shard, new Vector2(-62, 0), new Vector2(56, 70));
            crystal.color = Hex("F0C463");
            var count = Label(shards, "Count", font, "0/12", 42, new Vector2(38, 2), new Vector2(150, 60));
            count.fontStyle = FontStyle.Bold;

            // Checkpoint notice: under the timer, shown for a moment when a checkpoint is reached.
            var notice = Plate(root.transform, "Checkpoint Notice", new Vector2(0.5f, 1), new Vector2(0, -170), new Vector2(330, 84));
            var noticeIcon = Icon(notice, "Lamp Icon", flame, new Vector2(-104, 0), new Vector2(44, 44));
            noticeIcon.color = Gold;
            var noticeText = Label(notice, "Text", font, "Checkpoint", 34, new Vector2(24, 2), new Vector2(220, 50));
            noticeText.fontStyle = FontStyle.Bold;
            notice.gameObject.AddComponent<CanvasGroup>().alpha = 0f;
            notice.gameObject.AddComponent<SunSwitch.Art.CheckpointNotice>();

            // Hint: bottom centre, the instruction for where the player is (HintZone) or a blocked switch.
            var hint = Plate(root.transform, "Hint", new Vector2(0.5f, 0), new Vector2(0, 48), new Vector2(1100, 96));
            var hintText = Label(hint, "Text", font, "", 32, new Vector2(0, 2), new Vector2(1030, 84));
            hintText.supportRichText = true;
            hintText.horizontalOverflow = HorizontalWrapMode.Wrap;
            hintText.lineSpacing = 0.9f;
            hint.gameObject.AddComponent<CanvasGroup>().alpha = 0f;
            var hintPanel = hint.gameObject.AddComponent<SunSwitch.Art.HintPanel>();
            Set(hintPanel, "text", hintText);

            var hud = root.AddComponent<SunSwitch.UI.HUDController>();
            var so = new SerializedObject(hud);
            var icons = so.FindProperty("lifeIcons");
            icons.arraySize = lifeIcons.Length;
            for (int i = 0; i < lifeIcons.Length; i++) icons.GetArrayElementAtIndex(i).objectReferenceValue = lifeIcons[i];
            so.FindProperty("lostLifeTint").colorValue = new Color(0.45f, 0.42f, 0.42f, 0.45f);
            so.FindProperty("timerText").objectReferenceValue = timer;
            so.FindProperty("shardText").objectReferenceValue = count;
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Sun Switch] Built {PrefabPath}.");
        }

        // ---------------------------------------------------------------- layout

        static Material IrisMaterial()
        {
            const string path = SpriteDir + "/M_InkIris.mat";
            var shader = Shader.Find("SunSwitch/UI/InkIris");
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.shader = shader;
            mat.SetColor("_Color", Ink);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static RectTransform Plate(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var paper = rect.gameObject.AddComponent<StorybookGraphic>();
            paper.motif = StorybookGraphic.Motif.Parchment;
            paper.raycastTarget = false;
            return rect;
        }

        static Image Icon(Transform parent, string name, Sprite sprite, Vector2 position, Vector2 size)
        {
            var image = new GameObject(name, typeof(RectTransform)).AddComponent<Image>();
            image.rectTransform.SetParent(parent, false);
            image.rectTransform.anchoredPosition = position;
            image.rectTransform.sizeDelta = size;
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        static Text Label(Transform parent, string name, Font font, string text, int size, Vector2 position, Vector2 dimensions)
        {
            var label = new GameObject(name, typeof(RectTransform)).AddComponent<Text>();
            label.rectTransform.SetParent(parent, false);
            label.rectTransform.anchoredPosition = position;
            label.rectTransform.sizeDelta = dimensions;
            label.font = font;
            label.text = text;
            label.fontSize = size;
            label.color = Ink;
            label.alignment = TextAnchor.MiddleCenter;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.raycastTarget = false;
            return label;
        }

        static void Set(Object target, string property, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(property).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void Set(Object target, string property, Color value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(property).colorValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------------------------------------------------------------- sprites

        // Shapes are signed distances in a -1..1 square (negative inside). Each sprite gets a
        // white fill and an ink outline, so tinting the Image colours the fill and keeps the ink.
        const float Outline = 0.07f;

        static Sprite BakeSprite(string name, System.Func<Vector2, float> shape)
        {
            string path = $"{SpriteDir}/{name}.png";
            var tex = new Texture2D(SpriteSize, SpriteSize, TextureFormat.RGBA32, false);
            float px = 2f / SpriteSize;
            for (int y = 0; y < SpriteSize; y++)
            for (int x = 0; x < SpriteSize; x++)
            {
                var p = new Vector2((x + 0.5f) * px - 1f, (y + 0.5f) * px - 1f);
                float d = shape(p);
                float alpha = Mathf.Clamp01(0.5f - (d - Outline) / px);   // outer edge of the ink
                float ink = Mathf.Clamp01(0.5f + d / px);                 // fill -> ink at d = 0
                var c = Color.Lerp(Color.white, Ink, ink);
                c.a = alpha;
                tex.SetPixel(x, y, c);
            }
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static float Circle(Vector2 p, Vector2 c, float r) => (p - c).magnitude - r;

        static float Ring(Vector2 p) => Mathf.Abs(p.magnitude - 0.78f) - 0.1f;

        static float Sun(Vector2 p)
        {
            float d = Circle(p, Vector2.zero, 0.44f);
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI / 4f;
                var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                d = Mathf.Min(d, Taper(p, dir * 0.52f, dir * 0.86f, 0.09f, 0.02f));
            }
            return d;
        }

        static float Moon(Vector2 p)
        {
            float outer = Circle(p, new Vector2(-0.04f, 0f), 0.78f);
            float bite = Circle(p, new Vector2(0.34f, 0.22f), 0.64f);
            return Mathf.Max(outer, -bite);
        }

        static float Flame(Vector2 p)
        {
            float body = Circle(p, new Vector2(0f, -0.3f), 0.48f);
            float tip = Taper(p, new Vector2(0f, -0.25f), new Vector2(0.06f, 0.86f), 0.4f, 0.02f);
            return SmoothMin(body, tip, 0.18f);
        }

        static float Crystal(Vector2 p)
        {
            // A long diamond.
            const float w = 0.5f, h = 0.88f;
            return (Mathf.Abs(p.x) * h + Mathf.Abs(p.y) * w - w * h) / Mathf.Sqrt(w * w + h * h);
        }

        /// Distance to a segment a-b whose radius goes from ra to rb.
        static float Taper(Vector2 p, Vector2 a, Vector2 b, float ra, float rb)
        {
            var ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            return (p - (a + ab * t)).magnitude - Mathf.Lerp(ra, rb, t);
        }

        static float SmoothMin(float a, float b, float k)
        {
            float h = Mathf.Clamp01(0.5f + 0.5f * (b - a) / k);
            return Mathf.Lerp(b, a, h) - k * h * (1f - h);
        }

        static Color Hex(string hex) => ColorUtility.TryParseHtmlString("#" + hex, out var c) ? c : Color.magenta;
    }
}
