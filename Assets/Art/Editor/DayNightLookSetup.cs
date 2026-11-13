using SunSwitch.Art;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SunSwitch.Art.EditorTools
{
    /// <summary>
    /// Sun Switch > Art > Add Day-Night Look To Open Scene
    ///
    /// Gives the open scene the Day / Night presentation: the StorybookSky skybox, a DayNightLook
    /// driving the scene's directional light, fog and ambient light, and (in sandbox scenes only)
    /// a DayNightDebugToggle on T. Safe to run again; it reuses what is already there.
    /// </summary>
    public static class DayNightLookSetup
    {
        const string SkyDir = "Assets/Art/Materials/Sky";
        const string SkyPath = SkyDir + "/M_StorybookSky.mat";
        const string SkyShader = "SunSwitch/StorybookSky";

        [MenuItem("Sun Switch/Art/Add Day-Night Look To Open Scene")]
        public static void AddToOpenScene()
        {
            var scene = EditorSceneManager.GetActiveScene();

            var sky = AssetDatabase.LoadAssetAtPath<Material>(SkyPath);
            if (sky == null)
            {
                if (!AssetDatabase.IsValidFolder(SkyDir)) AssetDatabase.CreateFolder("Assets/Art/Materials", "Sky");
                sky = new Material(Shader.Find(SkyShader));
                AssetDatabase.CreateAsset(sky, SkyPath);
            }
            RenderSettings.skybox = sky;

            var sun = RenderSettings.sun;
            if (sun == null)
                foreach (var light in Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    if (light.type == LightType.Directional) { sun = light; break; }
            if (sun == null)
            {
                sun = new GameObject("Directional Light").AddComponent<Light>();
                sun.type = LightType.Directional;
                Undo.RegisterCreatedObjectUndo(sun.gameObject, "Add sun");
            }
            sun.shadows = LightShadows.Soft;
            RenderSettings.sun = sun;

            var look = Object.FindFirstObjectByType<DayNightLook>(FindObjectsInactive.Include);
            if (look == null)
            {
                look = new GameObject("DayNightLook").AddComponent<DayNightLook>();
                Undo.RegisterCreatedObjectUndo(look.gameObject, "Add DayNightLook");
            }
            var so = new SerializedObject(look);
            so.FindProperty("sun").objectReferenceValue = sun;
            so.ApplyModifiedProperties();

            bool sandbox = scene.path.Contains("/Sandbox/");
            if (sandbox && look.GetComponent<DayNightDebugToggle>() == null)
                Undo.AddComponent<DayNightDebugToggle>(look.gameObject);

            // Re-apply so the scene saves the Day look (fog, ambient, light) as its starting state.
            look.enabled = false;
            look.enabled = true;

            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log($"[Sun Switch] Day-Night look set up in {scene.name}" + (sandbox ? " (press T in Play mode to switch)." : "."));
        }
    }
}
