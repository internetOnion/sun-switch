using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SunSwitch.UI
{
    /// <summary>
    /// For the Start screen's day / night background: redraws the part of a graphic (text or a
    /// StorybookGraphic) that lies right of the screen's centre line in a night colour, cut
    /// exactly at the line, so ink text stays readable on both halves. Only ink-coloured parts
    /// change (the logo's sun and moon keep their colours); the left part is untouched. Alpha is
    /// kept, so faint ink stays faint.
    /// </summary>
    [AddComponentMenu("")]
    public sealed class SplitTint : BaseMeshEffect
    {
        public Color right = new Color(0.945f, 0.898f, 0.792f);   // parchment cream
        [Tooltip("Only vertices of this colour change on the right")] public Color ink = new Color(0.157f, 0.137f, 0.129f);

        static readonly List<UIVertex> stream = new List<UIVertex>();
        static readonly List<UIVertex> output = new List<UIVertex>();
        static readonly List<UIVertex> polygon = new List<UIVertex>();
        static readonly List<UIVertex> leftPart = new List<UIVertex>();
        static readonly List<UIVertex> rightPart = new List<UIVertex>();

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive() || graphic.canvas == null) return;
            // The screen's centre line, in this graphic's local space.
            var root = (RectTransform)graphic.canvas.rootCanvas.transform;
            float split = transform.InverseTransformPoint(root.TransformPoint(root.rect.center)).x;

            stream.Clear(); output.Clear();
            vh.GetUIVertexStream(stream);
            for (int i = 0; i + 2 < stream.Count; i += 3)
            {
                polygon.Clear();
                polygon.Add(stream[i]); polygon.Add(stream[i + 1]); polygon.Add(stream[i + 2]);
                Clip(polygon, split, leftPart, true);
                Clip(polygon, split, rightPart, false);
                Fan(leftPart, false);
                Fan(rightPart, true);
            }
            vh.Clear();
            vh.AddUIVertexTriangleStream(output);
        }

        // Sutherland-Hodgman against the vertical line x = split, keeping the left or right side.
        static void Clip(List<UIVertex> poly, float split, List<UIVertex> result, bool keepLeft)
        {
            result.Clear();
            for (int i = 0; i < poly.Count; i++)
            {
                var a = poly[i];
                var b = poly[(i + 1) % poly.Count];
                bool aIn = keepLeft ? a.position.x <= split : a.position.x >= split;
                bool bIn = keepLeft ? b.position.x <= split : b.position.x >= split;
                if (aIn) result.Add(a);
                if (aIn != bIn)
                {
                    float t = (split - a.position.x) / (b.position.x - a.position.x);
                    result.Add(Lerp(a, b, t));
                }
            }
        }

        void Fan(List<UIVertex> poly, bool tint)
        {
            for (int i = 1; i + 1 < poly.Count; i++)
            {
                output.Add(Tint(poly[0], tint));
                output.Add(Tint(poly[i], tint));
                output.Add(Tint(poly[i + 1], tint));
            }
        }

        UIVertex Tint(UIVertex v, bool tint)
        {
            if (!tint) return v;
            Color vc = v.color;
            if (Mathf.Abs(vc.r - ink.r) + Mathf.Abs(vc.g - ink.g) + Mathf.Abs(vc.b - ink.b) > 0.12f) return v;
            Color32 c = right;
            c.a = v.color.a;
            v.color = c;
            return v;
        }

        static UIVertex Lerp(UIVertex a, UIVertex b, float t)
        {
            var v = new UIVertex
            {
                position = Vector3.Lerp(a.position, b.position, t),
                normal = Vector3.Lerp(a.normal, b.normal, t),
                tangent = Vector4.Lerp(a.tangent, b.tangent, t),
                color = Color32.Lerp(a.color, b.color, t),
                uv0 = Vector4.Lerp(a.uv0, b.uv0, t),
                uv1 = Vector4.Lerp(a.uv1, b.uv1, t),
                uv2 = Vector4.Lerp(a.uv2, b.uv2, t),
                uv3 = Vector4.Lerp(a.uv3, b.uv3, t),
            };
            return v;
        }
    }
}
