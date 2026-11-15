using UnityEngine;
using UnityEngine.UI;

namespace SunSwitch.UI
{
    // Small original vector illustrations; no texture imports or external artwork.
    [AddComponentMenu("")]
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class StorybookGraphic : MaskableGraphic
    {
        public enum Motif { Sky, Clouds, Crest, Ribbon, Frame, Parchment, Divider, Mouse, CloudCorner }
        public Motif motif;
        public Color skyBottom = Hex("C6D8DB"), skyTop = Hex("8DBAD8");
        [Tooltip("Sky only: day on the left half, night on the right half (the Start screen)")]
        public bool dayNightSplit;
        public Color nightBottom = Hex("4A5590"), nightTop = Hex("1F2452");
        static readonly Color Ink = Hex("282321");
        static readonly Color Cream = Hex("FFF5DE");
        static readonly Color Gold = Hex("E9B64F");
        VertexHelper mesh;

        public static Color Hex(string value) { ColorUtility.TryParseHtmlString("#" + value, out var c); return c; }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); mesh = vh;
            var r = rectTransform.rect;
            if (motif == Motif.Sky)
            {
                if (dayNightSplit)
                {
                    Quad(new Vector2(r.xMin, r.yMin), new Vector2(r.center.x, r.yMax), skyBottom, skyTop);
                    Quad(new Vector2(r.center.x, r.yMin), new Vector2(r.xMax, r.yMax), nightBottom, nightTop);
                    // Night stars, deterministic, kept to the upper sky of the right half.
                    for (int i = 0; i < 46; i++)
                    {
                        float x = r.center.x + Mathf.Repeat(i * 0.7548777f, 1) * r.width * 0.5f;
                        float y = r.yMin + (0.35f + 0.65f * Mathf.Repeat(i * 0.5698403f, 1)) * r.height;
                        if (i % 7 == 0) Star(new Vector2(x, y), 5, new Color(Cream.r, Cream.g, Cream.b, 0.9f));
                        else Disc(new Vector2(x, y), 1.4f, new Color(Cream.r, Cream.g, Cream.b, 0.75f), 8);
                    }
                }
                else Quad(new Vector2(r.xMin, r.yMin), new Vector2(r.xMax, r.yMax), skyBottom, skyTop);
                // Quiet, deterministic paper flecks. No per-frame random changes.
                for (int i = 0; i < 260; i++)
                {
                    float x = Mathf.Repeat(i * 0.618034f, 1) * r.width + r.xMin;
                    float y = Mathf.Repeat(i * 0.414214f, 1) * r.height + r.yMin;
                    Disc(new Vector2(x, y), 0.7f, new Color(1, 0.97f, 0.87f, 0.16f), 6);
                }
            }
            else if (motif == Motif.Clouds)
            {
                float cloudScale = Mathf.Min(1, r.height / 340);
                // Three outlined banks of scalloped clouds, kept outside the reading column.
                for (int layer = 0; layer < 3; layer++)
                {
                    float baseline = r.yMin + (12 + layer * 44) * cloudScale;
                    Color fill = layer == 0 ? Hex("A8C4D1") : layer == 1 ? Hex("DFDFCE") : Cream;
                    for (int i = 0; i < 17; i++)
                    {
                        float x = r.xMin + i * r.width / 16;
                        float edge = Mathf.Abs((i - 8) / 8f);
                        float radius = (65 + 60 * edge + Mathf.Sin(i * 4.7f + layer) * 18) * cloudScale;
                        Vector2 p = new Vector2(x, baseline + (edge * edge * 125 - layer * 38) * cloudScale);
                        Disc(p, radius + 2, new Color(Ink.r, Ink.g, Ink.b, 0.5f));
                        Disc(p, radius, fill);
                        Arc(p + new Vector2(15, 8), radius * 0.48f, 0.2f, 2.7f, 1.4f, new Color(Ink.r, Ink.g, Ink.b, 0.25f));
                    }
                    Quad(new Vector2(r.xMin, r.yMin), new Vector2(r.xMax, baseline - 15), fill, fill);
                }
            }
            else if (motif == Motif.CloudCorner)
            {
                float scale = Mathf.Min(r.width / 220, r.height / 90);
                Vector2[] centers = { new Vector2(-72, -9), new Vector2(-42, 8), new Vector2(-3, 17), new Vector2(37, 7), new Vector2(73, -9) };
                float[] radii = { 25, 31, 36, 30, 24 };
                for (int i = 0; i < centers.Length; i++) Disc(r.center + centers[i] * scale, (radii[i] + 1.5f) * scale, Ink);
                for (int i = 0; i < centers.Length; i++) Disc(r.center + centers[i] * scale, radii[i] * scale, Cream);
                Arc(r.center + new Vector2(-9, 10) * scale, 23 * scale, 0.2f, 2.8f, scale, Hex("645B52"));
                Arc(r.center + new Vector2(46, 0) * scale, 17 * scale, 0.1f, 2.5f, scale, Hex("645B52"));
            }
            else if (motif == Motif.Crest)
            {
                // Sun Switch logo: the sun, with a crescent moon sitting on its top right. No badge
                // behind it, so the screen's own background shows through.
                float s = Mathf.Min(r.width / 300, r.height / 180);
                Vector2 sun = r.center + new Vector2(-10, -8) * s;
                Disc(sun, 58 * s, Ink);
                Disc(sun, 54 * s, Gold);
                Arc(sun, 45 * s, 1.9f, 3.6f, 2.5f * s, Hex("F7D98A"));   // a little shine on the sun

                Vector2 moon = sun + new Vector2(44, 40) * s;
                Crescent(moon, 42 * s, Mathf.PI * 0.25f, 29 * s, Hex("E6ECF7"), Ink, 3f * s);

                Vector2 center = r.center;
                Star(center + new Vector2(-124, 9) * s, 8 * s, Ink);
                Star(center + new Vector2(122, -12) * s, 6 * s, Ink);
                Line(center + new Vector2(-105, 0) * s, center + new Vector2(-90, 0) * s, s, Ink);
                Line(center + new Vector2(89, 0) * s, center + new Vector2(104, 0) * s, s, Ink);
            }
            else if (motif == Motif.Ribbon)
            {
                Vector2[] points = {
                    new Vector2(r.xMin + 10, r.yMin), new Vector2(r.xMax - 8, r.yMin + 2),
                    new Vector2(r.xMax, r.yMin + 9), new Vector2(r.xMax - 12, r.center.y),
                    new Vector2(r.xMax - 2, r.yMax - 5), new Vector2(r.xMin + 5, r.yMax),
                    new Vector2(r.xMin + 13, r.center.y + 4), new Vector2(r.xMin, r.yMin + 7)
                };
                Polygon(points, color);
            }
            else if (motif == Motif.Parchment)
            {
                // Convex, uneven edges keep this paper scalable without stretching ornaments.
                Vector2[] edge = {
                    new Vector2(r.xMin + 8, r.yMin + 10), new Vector2(r.center.x - 60, r.yMin + 3),
                    new Vector2(r.xMax - 12, r.yMin + 7), new Vector2(r.xMax - 3, r.center.y + 45),
                    new Vector2(r.xMax - 9, r.yMax - 8), new Vector2(r.center.x + 80, r.yMax - 2),
                    new Vector2(r.xMin + 12, r.yMax - 6), new Vector2(r.xMin + 3, r.center.y - 30)
                };
                Polygon(edge, Hex("F1E5CA"));
                for (int i = 0; i < edge.Length; i++)
                    Line(edge[i], edge[(i + 1) % edge.Length], 1.8f, Ink);
                // Grain stays near the perimeter, clear of live text.
                for (int i = 0; i < 100; i++)
                {
                    float x = Mathf.Lerp(r.xMin + 20, r.xMax - 20, Mathf.Repeat(i * 0.618034f, 1));
                    float y = (i % 2 == 0 ? r.yMin + 20 : r.yMax - 20) + Mathf.Sin(i * 2.4f) * 7;
                    Disc(new Vector2(x, y), 1, new Color(Ink.r, Ink.g, Ink.b, 0.09f), 6);
                }
            }
            else if (motif == Motif.Divider)
            {
                float y = r.center.y;
                Line(new Vector2(r.xMin + 8, y), new Vector2(r.center.x - 20, y), 1.5f, Gold);
                Line(new Vector2(r.center.x + 20, y), new Vector2(r.xMax - 8, y), 1.5f, Gold);
                Star(r.center, 7, Gold);
            }
            else if (motif == Motif.Mouse)
            {
                Vector2 p = r.center;
                Arc(p + Vector2.up * 7, 13, 0, Mathf.PI, 2, Cream);
                Arc(p - Vector2.up * 7, 13, Mathf.PI, Mathf.PI * 2, 2, Cream);
                Line(p + new Vector2(-13, -7), p + new Vector2(-13, 7), 2, Cream);
                Line(p + new Vector2(13, -7), p + new Vector2(13, 7), 2, Cream);
                Line(p + new Vector2(-13, 4), p + new Vector2(13, 4), 1.5f, Cream);
                Line(p + new Vector2(0, 4), p + new Vector2(0, 19), 1.5f, Cream);
            }
            else
            {
                Color c = new Color(Ink.r, Ink.g, Ink.b, 0.35f);
                float inset = 64;
                Vector2 a = new Vector2(r.xMin + inset, r.yMin + inset);
                Vector2 b = new Vector2(r.xMax - inset, r.yMax - inset);
                Line(a, new Vector2(a.x + 110, a.y), 1, c);
                Line(a, new Vector2(a.x, a.y + 90), 1, c);
                Line(b, new Vector2(b.x - 110, b.y), 1, c);
                Line(b, new Vector2(b.x, b.y - 90), 1, c);
                Star(new Vector2(a.x, b.y), 5, c);
                Star(new Vector2(b.x, a.y), 5, c);
            }
        }

        void Polygon(Vector2[] points, Color c)
        {
            int start = mesh.currentVertCount;
            foreach (var p in points) mesh.AddVert(p, c, Vector2.zero);
            for (int i = 1; i < points.Length - 1; i++) mesh.AddTriangle(start, start + i, start + i + 1);
        }
        void Quad(Vector2 a, Vector2 b, Color bottom, Color top)
        {
            int n = mesh.currentVertCount;
            mesh.AddVert(a, bottom, Vector2.zero); mesh.AddVert(new Vector2(b.x, a.y), bottom, Vector2.zero);
            mesh.AddVert(b, top, Vector2.zero); mesh.AddVert(new Vector2(a.x, b.y), top, Vector2.zero);
            mesh.AddTriangle(n, n + 1, n + 2); mesh.AddTriangle(n, n + 2, n + 3);
        }
        void Disc(Vector2 p, float radius, Color c, int count = 48)
        {
            int n = mesh.currentVertCount;
            mesh.AddVert(p, c, Vector2.zero);
            for (int i = 0; i <= count; i++)
            {
                float a = i * Mathf.PI * 2 / count;
                mesh.AddVert(p + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius, c, Vector2.zero);
                if (i > 0) mesh.AddTriangle(n, n + i, n + i + 1);
            }
        }
        // A pie slice from angle "from" to "to" (radians, counter-clockwise).
        void Fan(Vector2 p, float radius, float from, float to, Color c, int count = 32)
        {
            int n = mesh.currentVertCount;
            mesh.AddVert(p, c, Vector2.zero);
            for (int i = 0; i <= count; i++)
            {
                float a = Mathf.Lerp(from, to, i / (float)count);
                mesh.AddVert(p + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius, c, Vector2.zero);
                if (i > 0) mesh.AddTriangle(n, n + i, n + i + 1);
            }
        }
        // A crescent: the circle (p, radius) minus the same-size circle moved by "cut" toward "toward"
        // (radians). Built as a strip between the outer and inner arcs, with an inked edge.
        void Crescent(Vector2 p, float radius, float toward, float cut, Color fill, Color ink, float inkWidth, int count = 40)
        {
            float phi = Mathf.Acos(Mathf.Clamp(cut / (2 * radius), -1, 1));
            Vector2 cutCenter = p + new Vector2(Mathf.Cos(toward), Mathf.Sin(toward)) * cut;
            Vector2 Outer(float t) { float a = toward + phi + t * (Mathf.PI * 2 - 2 * phi); return p + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius; }
            Vector2 Inner(float t) { float a = toward + Mathf.PI - phi + t * 2 * phi; return cutCenter + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius; }
            for (int i = 0; i < count; i++)
            {
                float t0 = i / (float)count, t1 = (i + 1) / (float)count;
                int n = mesh.currentVertCount;
                mesh.AddVert(Outer(t0), fill, Vector2.zero); mesh.AddVert(Outer(t1), fill, Vector2.zero);
                mesh.AddVert(Inner(t1), fill, Vector2.zero); mesh.AddVert(Inner(t0), fill, Vector2.zero);
                mesh.AddTriangle(n, n + 1, n + 2); mesh.AddTriangle(n, n + 2, n + 3);
            }
            for (int i = 0; i < count; i++)
            {
                float t0 = i / (float)count, t1 = (i + 1) / (float)count;
                Line(Outer(t0), Outer(t1), inkWidth, ink);
                Line(Inner(t0), Inner(t1), inkWidth, ink);
            }
        }
        void Line(Vector2 a, Vector2 b, float width, Color c)
        {
            Vector2 n = new Vector2(-(b - a).y, (b - a).x).normalized * width / 2;
            Polygon(new[] { a - n, b - n, b + n, a + n }, c);
        }
        void Arc(Vector2 p, float radius, float from, float to, float width, Color c)
        {
            for (int i = 0; i < 32; i++)
            {
                float a = Mathf.Lerp(from, to, i / 32f), b = Mathf.Lerp(from, to, (i + 1) / 32f);
                Line(p + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius,
                    p + new Vector2(Mathf.Cos(b), Mathf.Sin(b)) * radius, width, c);
            }
        }
        void Star(Vector2 p, float size, Color c)
        {
            Polygon(new[] { p + Vector2.up * size, p + Vector2.left * size * 0.3f,
                p + Vector2.down * size, p + Vector2.right * size * 0.3f }, c);
        }
    }
}
