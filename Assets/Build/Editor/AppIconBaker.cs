using System.IO;
using UnityEditor;
using UnityEngine;

namespace SunSwitch.Build
{
    /// <summary>
    /// Draws the app icon (1024 x 1024 PNG) in the same style as the Start screen: a rounded tile,
    /// day sky on the left and night sky with stars on the right, the gold sun with a crescent
    /// moon on its top right, inked outlines. Drawn with signed distances, so it stays crisp and
    /// needs no art file. ReleaseBuild bakes it when applying the release settings.
    /// </summary>
    public static class AppIconBaker
    {
        const int Size = 1024;

        static readonly Color Ink = Hex("282321");
        static readonly Color Gold = Hex("E9B64F");
        static readonly Color Shine = Hex("F7D98A");
        static readonly Color MoonFill = Hex("E6ECF7");
        static readonly Color Cream = Hex("FFF5DE");
        static readonly Color DayTop = Hex("8DBAD8"), DayBottom = Hex("C6D8DB");
        static readonly Color NightTop = Hex("1F2452"), NightBottom = Hex("4A5590");

        static readonly Vector2[] Stars = {
            new Vector2(0.62f, 0.72f), new Vector2(0.8f, 0.38f), new Vector2(0.45f, -0.55f),
            new Vector2(0.78f, -0.2f), new Vector2(0.2f, 0.82f), new Vector2(0.66f, -0.68f),
        };

        public static Texture2D Bake(string path)
        {
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            var pixels = new Color[Size * Size];
            float px = 2f / Size;   // one pixel in -1..1 units, for anti-aliasing
            for (int y = 0; y < Size; y++)
            for (int x = 0; x < Size; x++)
            {
                var p = new Vector2((x + 0.5f) / Size * 2f - 1f, (y + 0.5f) / Size * 2f - 1f);
                pixels[y * Size + x] = Shade(p, px);
            }
            tex.SetPixels(pixels);
            tex.Apply();
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            if (AssetImporter.GetAtPath(path) is TextureImporter importer)
            {
                importer.textureType = TextureImporterType.Default;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.maxTextureSize = Size;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static Color Shade(Vector2 p, float px)
        {
            // Rounded tile with an ink border.
            float tile = RoundBox(p, 0.94f, 0.22f);
            if (tile > px) return Color.clear;
            float t = (p.y + 1f) * 0.5f;
            Color c = p.x < 0f ? Color.Lerp(DayBottom, DayTop, t) : Color.Lerp(NightBottom, NightTop, t);
            if (p.x >= 0f)
                foreach (var s in Stars)
                    c = Over(c, Cream, Fill(Star(p - s, 0.035f), px) * 0.9f);
            c = Over(c, Ink, Fill(Mathf.Abs(tile + 0.02f) - 0.02f, px));

            // Sun: ink rim, gold disc, a little shine.
            var sun = new Vector2(-0.1f, -0.1f);
            float dSun = (p - sun).magnitude - 0.46f;
            c = Over(c, Ink, Fill(dSun, px));
            c = Over(c, Gold, Fill(dSun + 0.045f, px));
            float ring = Mathf.Abs((p - sun).magnitude - 0.36f) - 0.02f;
            float angle = Mathf.Atan2(p.y - sun.y, p.x - sun.x);
            if (angle > 1.9f && angle < 3.6f) c = Over(c, Shine, Fill(ring, px));

            // Moon: a crescent on the sun's top right (the moon disc minus a disc moved up-right).
            var moon = sun + new Vector2(0.37f, 0.35f);
            var cut = moon + new Vector2(0.707f, 0.707f) * 0.25f;
            float dMoon = Mathf.Max((p - moon).magnitude - 0.36f, 0.36f - (p - cut).magnitude);
            c = Over(c, Ink, Fill(dMoon, px));
            c = Over(c, MoonFill, Fill(dMoon + 0.04f, px));

            c.a *= Fill(tile, px);
            return c;
        }

        static float RoundBox(Vector2 p, float half, float radius)
        {
            var q = new Vector2(Mathf.Abs(p.x), Mathf.Abs(p.y)) - new Vector2(half - radius, half - radius);
            return new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f)).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - radius;
        }

        // A four-pointed star like the menu's: distance-ish to a thin diamond pair.
        static float Star(Vector2 p, float size) =>
            Mathf.Min(Mathf.Abs(p.x) * 3.3f + Mathf.Abs(p.y), Mathf.Abs(p.x) + Mathf.Abs(p.y) * 3.3f) - size;

        static float Fill(float d, float px) => Mathf.Clamp01(0.5f - d / px);
        static Color Over(Color under, Color over, float a) => Color.Lerp(under, over, a);
        static Color Hex(string hex) => ColorUtility.TryParseHtmlString("#" + hex, out var c) ? c : Color.magenta;
    }
}
