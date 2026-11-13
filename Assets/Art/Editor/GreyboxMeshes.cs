using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SunSwitch.Art.EditorTools
{
    /// <summary>
    /// Real-size grey-box meshes (scale 1, so outlines and noise stay even on every face).
    /// Each mesh gets smoothed normals baked into UV3 for the Storybook ink outline.
    /// Existing mesh assets are refilled in place, so references to them survive.
    /// </summary>
    internal static class GreyboxMeshes
    {
        const string MeshDir = "Assets/Art/Meshes/Greybox";

        public static Mesh Box(string name, Vector3 size) => Save(name, m => FillBox(m, size));

        public static Mesh Cylinder(string name, float radius, float height, int segments = 24) =>
            Save(name, m => FillCylinder(m, radius, height, segments));

        static Mesh Save(string name, System.Action<Mesh> fill)
        {
            PlatformPrefabBuilder.EnsureFolder(MeshDir);
            string path = $"{MeshDir}/{name}.asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            bool isNew = mesh == null;
            if (isNew) mesh = new Mesh();
            mesh.Clear();
            mesh.name = name;
            fill(mesh);
            BakeSmoothNormals(mesh);
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            if (isNew) AssetDatabase.CreateAsset(mesh, path);
            else EditorUtility.SetDirty(mesh);
            return mesh;
        }

        static void FillBox(Mesh mesh, Vector3 size)
        {
            var half = size * 0.5f;
            var verts = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();

            void Face(Vector3 n, Vector3 uDir)
            {
                // v = u x n keeps the winding clockwise when seen from outside (Unity front face).
                var vDir = Vector3.Cross(uDir, n);
                var u = Vector3.Scale(uDir, half);
                var v = Vector3.Scale(vDir, half);
                var c = Vector3.Scale(n, half);
                int i = verts.Count;
                verts.Add(c - u - v); verts.Add(c + u - v); verts.Add(c + u + v); verts.Add(c - u + v);
                for (int k = 0; k < 4; k++) normals.Add(n);
                float w = u.magnitude * 2f, h = v.magnitude * 2f;
                uvs.Add(new Vector2(0, 0)); uvs.Add(new Vector2(w, 0)); uvs.Add(new Vector2(w, h)); uvs.Add(new Vector2(0, h));
                tris.AddRange(new[] { i, i + 2, i + 1, i, i + 3, i + 2 });
            }

            Face(Vector3.up, Vector3.right);
            Face(Vector3.down, Vector3.right);
            Face(Vector3.right, Vector3.up);
            Face(Vector3.left, Vector3.up);
            Face(Vector3.forward, Vector3.up);
            Face(Vector3.back, Vector3.up);

            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
        }

        static void FillCylinder(Mesh mesh, float radius, float height, int segments)
        {
            float hy = height * 0.5f;
            var verts = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();
            float circumference = 2f * Mathf.PI * radius;

            Vector3 Dir(int s)
            {
                float a = s / (float)segments * Mathf.PI * 2f;
                return new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
            }

            // Side: bottom/top vertex pairs.
            for (int s = 0; s <= segments; s++)
            {
                var d = Dir(s);
                verts.Add(d * radius + Vector3.down * hy); normals.Add(d); uvs.Add(new Vector2(s / (float)segments * circumference, 0));
                verts.Add(d * radius + Vector3.up * hy);   normals.Add(d); uvs.Add(new Vector2(s / (float)segments * circumference, height));
            }
            for (int s = 0; s < segments; s++)
            {
                int a = 2 * s, b = a + 1, c = a + 2, d = a + 3;
                tris.AddRange(new[] { a, b, d, a, d, c });
            }

            void Cap(bool top)
            {
                var n = top ? Vector3.up : Vector3.down;
                int centre = verts.Count;
                verts.Add(n * hy); normals.Add(n); uvs.Add(new Vector2(radius, radius));
                for (int s = 0; s <= segments; s++)
                {
                    var d = Dir(s);
                    verts.Add(d * radius + n * hy); normals.Add(n); uvs.Add(new Vector2(radius + d.x * radius, radius + d.z * radius));
                }
                for (int s = 0; s < segments; s++)
                {
                    int p0 = centre + 1 + s, p1 = p0 + 1;
                    if (top) tris.AddRange(new[] { centre, p1, p0 });
                    else tris.AddRange(new[] { centre, p0, p1 });
                }
            }
            Cap(true);
            Cap(false);

            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
        }

        /// <summary>Averages normals of vertices that share a position and stores them in UV3.</summary>
        public static void BakeSmoothNormals(Mesh mesh)
        {
            var verts = mesh.vertices;
            var normals = mesh.normals;
            var sums = new Dictionary<Vector3Int, Vector3>();
            Vector3Int Key(Vector3 p) => Vector3Int.RoundToInt(p * 10000f);

            for (int i = 0; i < verts.Length; i++)
            {
                var key = Key(verts[i]);
                sums.TryGetValue(key, out var sum);
                sums[key] = sum + normals[i];
            }

            var smooth = new List<Vector4>(verts.Length);
            for (int i = 0; i < verts.Length; i++)
            {
                var s = sums[Key(verts[i])].normalized;
                smooth.Add(new Vector4(s.x, s.y, s.z, 0));
            }
            mesh.SetUVs(3, smooth);
        }
    }
}
