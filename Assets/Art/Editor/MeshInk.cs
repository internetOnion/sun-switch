using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SunSwitch.Art.EditorTools
{
    /// <summary>
    /// Bakes the data the SunSwitch/Storybook shader needs for hand-drawn ink, for any mesh.
    ///
    /// Every triangle gets its own vertices. Each vertex stores the exact distance (metres) to
    /// the three edges of its triangle; edges that should not be inked get a huge distance.
    ///   UV1 = (distance to edge opposite corner 0, ... corner 1)
    ///   UV2 = (distance to edge opposite corner 2, flags)   flags: 1 = baked, 2 = baked + outline hull
    ///   UV3 = smoothed normal (for the outline hull)
    /// An edge is inked when it is a mesh boundary or its two faces meet at more than InkAngle.
    /// Triangles next to a gentle (curved) edge also get the outline hull for their silhouette.
    ///
    /// Skinned meshes (skinned = true) keep their bone weights and bind poses. Their smoothed
    /// normal goes in the TANGENT instead, because skinning bends tangents with the bones but
    /// leaves UV3 in the bind pose; UV3 is then (0, 0, 0, 1) to tell the shader so.
    /// The edge distances stay valid as long as each triangle moves rigidly, which holds for
    /// low-poly characters whose parts follow one bone each.
    /// </summary>
    internal static class MeshInk
    {
        public const float InkAngle = 30f;     // degrees between faces to draw an ink line
        public const float CurveAngle = 4f;    // gentler than InkAngle but not flat = curved surface
        const float NoInk = 1000f;

        public static void Bake(Mesh mesh, bool skinned = false)
        {
            var positions = mesh.vertices;
            var boneWeights = skinned ? mesh.boneWeights : null;
            var bindposes = skinned ? mesh.bindposes : null;
            bool hasWeights = boneWeights != null && boneWeights.Length == positions.Length;
            var normals = mesh.normals;
            if (normals == null || normals.Length != positions.Length)
            {
                mesh.RecalculateNormals();
                normals = mesh.normals;
            }
            var colors = mesh.colors;
            bool hasColors = colors != null && colors.Length == positions.Length;
            var uv0 = mesh.uv;
            bool hasUv0 = uv0 != null && uv0.Length == positions.Length;

            // Gather all triangles (keeping sub-mesh ranges).
            var subTris = new List<int[]>();
            var tris = new List<int>();
            for (int s = 0; s < mesh.subMeshCount; s++)
            {
                var t = mesh.GetTriangles(s);
                subTris.Add(t);
                tris.AddRange(t);
            }
            int triCount = tris.Count / 3;

            // Face normals and edge adjacency by welded position.
            var faceNormals = new Vector3[triCount];
            var edgeFaces = new Dictionary<(Vector3Int, Vector3Int), List<int>>();
            for (int t = 0; t < triCount; t++)
            {
                Vector3 a = positions[tris[t * 3]], b = positions[tris[t * 3 + 1]], c = positions[tris[t * 3 + 2]];
                faceNormals[t] = Vector3.Cross(b - a, c - a).normalized;
                for (int k = 0; k < 3; k++)
                {
                    var key = EdgeKey(t, k, tris, positions);
                    if (!edgeFaces.TryGetValue(key, out var list)) edgeFaces[key] = list = new List<int>(2);
                    list.Add(t);
                }
            }

            // Classify each triangle's edges (edge k is opposite corner k).
            var inked = new bool[triCount, 3];
            var curved = new bool[triCount];
            for (int t = 0; t < triCount; t++)
            {
                for (int k = 0; k < 3; k++)
                {
                    var list = edgeFaces[EdgeKey(t, k, tris, positions)];
                    if (list.Count != 2) { inked[t, k] = true; continue; }
                    int other = list[0] == t ? list[1] : list[0];
                    float angle = Vector3.Angle(faceNormals[t], faceNormals[other]);
                    inked[t, k] = angle > InkAngle;
                    if (!inked[t, k] && angle > CurveAngle) curved[t] = true;
                }
            }

            // Smoothed normals: average of the distinct face directions meeting at each position.
            var smoothSum = new Dictionary<Vector3Int, Vector3>();
            var seen = new HashSet<(Vector3Int, Vector3Int)>();
            for (int t = 0; t < triCount; t++)
            {
                var n = faceNormals[t];
                var nKey = Vector3Int.RoundToInt(n * 1000f);
                for (int k = 0; k < 3; k++)
                {
                    var pKey = PosKey(positions[tris[t * 3 + k]]);
                    if (!seen.Add((pKey, nKey))) continue;
                    smoothSum.TryGetValue(pKey, out var sum);
                    smoothSum[pKey] = sum + n;
                }
            }

            // Build the split mesh.
            int vCount = triCount * 3;
            var outPos = new Vector3[vCount];
            var outNrm = new Vector3[vCount];
            var outCol = new Color[vCount];
            var outUv0 = new Vector2[vCount];
            var outUv1 = new Vector2[vCount];
            var outUv2 = new Vector2[vCount];
            var outUv3 = new Vector4[vCount];
            var outTangents = skinned ? new Vector4[vCount] : null;
            var outWeights = hasWeights ? new BoneWeight[vCount] : null;

            for (int t = 0; t < triCount; t++)
            {
                var p = new Vector3[3];
                for (int k = 0; k < 3; k++) p[k] = positions[tris[t * 3 + k]];
                float doubleArea = Vector3.Cross(p[1] - p[0], p[2] - p[0]).magnitude;
                float flags = curved[t] ? 2f : 1f;

                for (int k = 0; k < 3; k++)
                {
                    int src = tris[t * 3 + k];
                    int dst = t * 3 + k;
                    outPos[dst] = p[k];
                    outNrm[dst] = normals[src];
                    outCol[dst] = hasColors ? colors[src] : Color.white;
                    outUv0[dst] = hasUv0 ? uv0[src] : Vector2.zero;

                    // Distance from this corner to each of the triangle's three edges.
                    var dist = new float[3];
                    for (int e = 0; e < 3; e++)
                    {
                        if (!inked[t, e]) { dist[e] = NoInk; continue; }
                        if (e != k) { dist[e] = 0f; continue; }   // corner lies on that edge
                        float edgeLength = (p[(e + 2) % 3] - p[(e + 1) % 3]).magnitude;
                        dist[e] = edgeLength > 1e-6f ? doubleArea / edgeLength : NoInk;
                    }
                    outUv1[dst] = new Vector2(dist[0], dist[1]);
                    outUv2[dst] = new Vector2(dist[2], flags);

                    var s = smoothSum[PosKey(p[k])].normalized;
                    if (skinned)
                    {
                        outTangents[dst] = new Vector4(s.x, s.y, s.z, 1f);
                        outUv3[dst] = new Vector4(0f, 0f, 0f, 1f);
                    }
                    else
                    {
                        outUv3[dst] = new Vector4(s.x, s.y, s.z, 0f);
                    }
                    if (hasWeights) outWeights[dst] = boneWeights[src];
                }
            }

            mesh.Clear();
            mesh.indexFormat = vCount > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16;
            mesh.vertices = outPos;
            mesh.normals = outNrm;
            mesh.colors = outCol;
            mesh.SetUVs(0, outUv0);
            mesh.SetUVs(1, outUv1);
            mesh.SetUVs(2, outUv2);
            mesh.SetUVs(3, new List<Vector4>(outUv3));
            mesh.subMeshCount = subTris.Count;
            int start = 0;
            for (int s = 0; s < subTris.Count; s++)
            {
                var idx = new int[subTris[s].Length];
                for (int i = 0; i < idx.Length; i++) idx[i] = start + i;
                mesh.SetTriangles(idx, s);
                start += idx.Length;
            }
            mesh.RecalculateBounds();
            if (skinned)
            {
                mesh.tangents = outTangents;
                if (hasWeights)
                {
                    mesh.boneWeights = outWeights;
                    mesh.bindposes = bindposes;
                }
            }
            else
            {
                mesh.RecalculateTangents();
            }
        }

        static Vector3Int PosKey(Vector3 p) => Vector3Int.RoundToInt(p * 10000f);

        static (Vector3Int, Vector3Int) EdgeKey(int tri, int oppositeCorner, List<int> tris, Vector3[] positions)
        {
            var a = PosKey(positions[tris[tri * 3 + (oppositeCorner + 1) % 3]]);
            var b = PosKey(positions[tris[tri * 3 + (oppositeCorner + 2) % 3]]);
            bool swap = a.x > b.x || (a.x == b.x && (a.y > b.y || (a.y == b.y && a.z > b.z)));
            return swap ? (b, a) : (a, b);
        }
    }
}
