using UnityEditor;
using UnityEngine;

namespace SunSwitch.Art.EditorTools
{
    /// <summary>
    /// Import step for the Blender models in Assets/Art/Meshes/Ruins/ (ArtSource/ruin_platforms.py)
    /// and Assets/Art/Meshes/Obstacles/ (ArtSource/obstacles.py).
    /// Applies consistent import settings and bakes the Storybook ink data (see MeshInk).
    /// Re-export from Blender and Unity redoes this.
    /// </summary>
    public class RuinMeshPostprocessor : AssetPostprocessor
    {
        static readonly string[] Folders = { "Assets/Art/Meshes/Ruins/", "Assets/Art/Meshes/Obstacles/" };

        bool Handles => System.Array.Exists(Folders, f => assetPath.StartsWith(f));

        // Bump when the bake changes so Unity reimports the models.
        public override uint GetVersion() => 2;

        void OnPreprocessModel()
        {
            if (!Handles) return;
            var importer = (ModelImporter)assetImporter;
            importer.globalScale = 1f;
            importer.useFileScale = true;
            importer.bakeAxisConversion = true;
            importer.importNormals = ModelImporterNormals.Import;
            importer.importBlendShapes = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.importAnimation = false;
            importer.animationType = ModelImporterAnimationType.None;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.meshCompression = ModelImporterMeshCompression.Off;
            importer.addCollider = false;
            // MeshInk rewrites the triangles, which Unity only allows on readable meshes.
            importer.isReadable = true;
        }

        void OnPostprocessModel(GameObject root)
        {
            if (!Handles) return;
            foreach (var filter in root.GetComponentsInChildren<MeshFilter>())
            {
                var mesh = filter.sharedMesh;
                if (!mesh.isReadable)
                {
                    Debug.LogError($"[Sun Switch] {assetPath}: mesh is not readable, ink data not baked.");
                    continue;
                }
                MeshInk.Bake(mesh);
                var b = mesh.bounds;
                Debug.Log($"[Sun Switch] Mesh {mesh.name}: {mesh.triangles.Length / 3} tris, " +
                          $"bounds min {b.min.ToString("F2")} max {b.max.ToString("F2")}");
            }
        }
    }
}
