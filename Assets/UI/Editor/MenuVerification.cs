using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using SunSwitch.UI.Preview;

namespace SunSwitch.UI.Editor
{
    public static class MenuVerification
    {
        [MenuItem("Sun Switch/UI/Verify navigation and render review")]
        public static void Verify()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/StartGame.unity");
            var view = UnityEngine.Object.FindFirstObjectByType<StartMenuView>();
            var preview = view.GetComponent<StartMenuPreview>();
            view.BindFlow(preview);
            view.ShowStart();
            view.Execute(MenuCommandId.Play); Expect(view, MenuScreen.Message); view.Back(); Expect(view, MenuScreen.Start);
            view.ShowPause();
            view.Execute(MenuCommandId.Settings); Expect(view, MenuScreen.Settings); view.Back(); Expect(view, MenuScreen.Pause);
            view.Execute(MenuCommandId.Controls); Expect(view, MenuScreen.Controls); view.Back(); Expect(view, MenuScreen.Pause);
            view.Execute(MenuCommandId.Restart); Expect(view, MenuScreen.Confirmation);
            Require(view.transform.Find("Menu Canvas/Confirmation/Parchment card/Cancel ribbon") != null, "Cancel is missing.");
            view.Execute(MenuCommandId.Cancel); Expect(view, MenuScreen.Pause);
            view.Execute(MenuCommandId.Restart); view.Execute(MenuCommandId.Confirm); Expect(view, MenuScreen.Message);
            view.Back(); Expect(view, MenuScreen.Pause);
            view.Execute(MenuCommandId.MainMenu); view.Execute(MenuCommandId.Confirm); Expect(view, MenuScreen.Start);
            var probe = view.gameObject.AddComponent<MenuFlowProbe>(); view.BindFlow(probe);
            view.Execute(MenuCommandId.Play); view.Execute(MenuCommandId.Play); Require(probe.requests == 1, "Duplicate start request dispatched.");
            view.CompleteRequest(); view.Execute(MenuCommandId.Play); Require(probe.requests == 2, "Start could not retry.");
            view.CompleteRequest(); UnityEngine.Object.DestroyImmediate(probe); view.BindFlow(preview);
            foreach (var command in view.GetComponentsInChildren<MenuCommand>(true))
            {
                var button = command.GetComponent<Button>();
                Require(command.view == view && button.onClick.GetPersistentEventCount() == 1, "Missing serialized button action.");
            }
            foreach (var row in view.GetComponentsInChildren<MenuSettingsRow>(true))
            {
                row.Configure(false); Require(!row.slider.interactable && row.valueLabel.text == "Unavailable", "Unbound settings are enabled.");
                row.Configure(true); Require(row.slider.interactable, "Preview settings are disabled.");
            }
            float timeScale = Time.timeScale;
            var camera = UnityEngine.Object.FindFirstObjectByType<Camera>();
            var canvas = view.GetComponentInChildren<Canvas>(true);
            var scaler = canvas.GetComponent<CanvasScaler>(); scaler.enabled = false;
            canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1;
            Directory.CreateDirectory("Review");
            foreach (var size in new[] { new Vector2Int(1280, 720), new Vector2Int(1920, 1080), new Vector2Int(1920, 1200), new Vector2Int(2560, 1080) })
            {
                view.ShowStart(); Render(view, camera, canvas, size, $"start-{size.x}x{size.y}");
            }
            var hd = new Vector2Int(1920, 1080);
            view.Execute(MenuCommandId.Settings); Render(view, camera, canvas, hd, "settings"); view.Back();
            view.Execute(MenuCommandId.Controls); Render(view, camera, canvas, hd, "controls"); view.Back();
            view.ShowPause(); camera.backgroundColor = Color.white; Render(view, camera, canvas, hd, "pause-bright");
            camera.backgroundColor = StorybookGraphic.Hex("282321"); Render(view, camera, canvas, hd, "pause-dark");
            view.Execute(MenuCommandId.Restart); Render(view, camera, canvas, hd, "confirmation");
            view.ShowResults(false, new MenuResults(327, 4, 7)); Render(view, camera, canvas, hd, "game-over");
            view.ShowResults(true, new MenuResults(492, 2, 12)); Render(view, camera, canvas, hd, "victory");
            Require(Time.timeScale == timeScale, "Preview changed global time scale.");
            Debug.Log("UI_VERIFIED: navigation, confirmation return paths, duplicate dispatch, settings availability, serialized actions and 11 rendered layouts.");
            EditorSceneManager.OpenScene("Assets/Scenes/StartGame.unity");
        }

        static void Render(StartMenuView view, Camera camera, Canvas canvas, Vector2Int size, string name)
        {
            var target = new RenderTexture(size.x, size.y, 24); camera.targetTexture = target;
            canvas.scaleFactor = Mathf.Min(size.x / 1920f, size.y / 1080f);
            foreach (var graphic in view.GetComponentsInChildren<Graphic>()) graphic.SetAllDirty();
            foreach (var group in view.GetComponentsInChildren<CanvasGroup>())
                if (group.GetComponent<StorybookGraphic>() == null) group.alpha = 1;
            Canvas.ForceUpdateCanvases(); camera.Render();
            foreach (var art in view.GetComponentsInChildren<StorybookGraphic>())
                Require(art.canvasRenderer != null && art.canvasRenderer.GetMesh().vertexCount > 0, $"Artwork mesh is missing: {art.name}");
            foreach (var label in view.GetComponentsInChildren<Text>())
                Require(label.preferredHeight <= label.rectTransform.rect.height + 3, $"Text overflows: {name}/{label.name} ({label.text})");
            var previous = RenderTexture.active; RenderTexture.active = target;
            var image = new Texture2D(size.x, size.y, TextureFormat.RGB24, false); image.ReadPixels(new Rect(0, 0, size.x, size.y), 0, 0); image.Apply();
            File.WriteAllBytes($"Review/{name}.png", image.EncodeToPNG());
            RenderTexture.active = previous; camera.targetTexture = null;
            UnityEngine.Object.DestroyImmediate(image); UnityEngine.Object.DestroyImmediate(target);
        }
        static void Expect(StartMenuView view, MenuScreen expected) => Require(view.CurrentScreen == expected, $"Expected {expected}, got {view.CurrentScreen}.");
        static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    }
}
