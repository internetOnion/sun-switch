using System;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using SunSwitch.UI.Preview;

namespace SunSwitch.UI.Editor
{
    public static class MenuSceneBuilder
    {
        public static void BuildAndVerify() { Build(); MenuVerification.Verify(); }
        [MenuItem("Sun Switch/UI/Rebuild isolated menu scenes")]
        public static void Build()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            BuildScene("Assets/Scenes/StartGame.unity", true);
            BuildScene("Assets/UI/Scenes/MenuPreview.unity", false);
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene("Assets/Scenes/StartGame.unity");
            Debug.Log("UI_SCENES_BUILT: editable Canvas, eight screens, preview fixtures, EventSystem.");
        }

        static void BuildScene(string path, bool savePrefab)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camera = new GameObject("Preview background camera").AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = StorybookGraphic.Hex("8B999D");
            camera.orthographic = true; camera.transform.position = new Vector3(0, 0, -10);
            var root = new GameObject("Menu Root");
            var view = root.AddComponent<StartMenuView>(); view.BuildCanvas();
            if (savePrefab) PrefabUtility.SaveAsPrefabAsset(root, "Assets/UI/Prefabs/MenuRoot.prefab");
            var preview = root.AddComponent<StartMenuPreview>(); view.BindFlow(preview);
            new GameObject("Preview EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            Toolbar(preview);
            EditorSceneManager.SaveScene(scene, path);
        }

        static void Toolbar(StartMenuPreview preview)
        {
            var root = new GameObject("Preview fixtures - excluded from menu prefab", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 20;
            var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            var label = Text(root.transform, "Preview notice", "MENU PREVIEW\nNo gameplay connected", new Vector2(64, -64), new Vector2(240, 62), 18);
            label.alignment = TextAnchor.UpperLeft;
            string[] captions = { "F1  Start", "F2  Pause", "F3  Game Over", "F4  Victory" };
            UnityEngine.Events.UnityAction[] actions = { preview.PreviewStart, preview.PreviewPause, preview.PreviewGameOver, preview.PreviewVictory };
            for (int i = 0; i < captions.Length; i++)
            {
                var buttonRoot = new GameObject(captions[i], typeof(RectTransform));
                var buttonRect = buttonRoot.GetComponent<RectTransform>(); buttonRect.SetParent(root.transform, false);
                buttonRect.anchorMin = buttonRect.anchorMax = new Vector2(0, 1); buttonRect.pivot = new Vector2(0, 1);
                buttonRect.anchoredPosition = new Vector2(64, -144 - i * 48); buttonRect.sizeDelta = new Vector2(200, 38);
                var image = buttonRoot.AddComponent<Image>(); image.color = StorybookGraphic.Hex("282321");
                var child = Text(image.transform, "Label", captions[i], Vector2.zero, new Vector2(200, 38), 18);
                var rect = child.rectTransform; rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f); rect.pivot = new Vector2(0.5f, 0.5f); rect.anchoredPosition = Vector2.zero;
                child.color = StorybookGraphic.Hex("FFF5DE");
                var button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image;
                UnityEventTools.AddPersistentListener(button.onClick, actions[i]);
            }
        }

        static Text Text(Transform parent, string name, string caption, Vector2 position, Vector2 size, int fontSize)
        {
            var root = new GameObject(name, typeof(RectTransform)); var rect = root.GetComponent<RectTransform>(); rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1); rect.pivot = new Vector2(0, 1); rect.anchoredPosition = position; rect.sizeDelta = size;
            var label = root.AddComponent<Text>(); label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.text = caption; label.fontSize = fontSize; label.color = StorybookGraphic.Hex("282321"); label.alignment = TextAnchor.MiddleCenter; label.raycastTarget = false; return label;
        }
    }
}
