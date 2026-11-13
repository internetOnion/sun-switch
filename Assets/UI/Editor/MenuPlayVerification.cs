using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SunSwitch.UI.Editor
{
    [InitializeOnLoad]
    public static class MenuPlayVerification
    {
        const string Running = "SunSwitch.UI.PlayVerification";
        static int frames;
        static MenuPlayVerification() { EditorApplication.update += Tick; }
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/StartGame.unity");
            SessionState.SetBool(Running, true); EditorApplication.EnterPlaymode();
        }
        static void Tick()
        {
            if (!SessionState.GetBool(Running, false) || !EditorApplication.isPlaying || ++frames < 30) return;
            SessionState.SetBool(Running, false);
            try
            {
                var view = UnityEngine.Object.FindFirstObjectByType<StartMenuView>();
                var events = EventSystem.current;
                Require(events != null && events.currentSelectedGameObject != null, "No initial keyboard focus.");
                Require(events.currentSelectedGameObject.GetComponent<MenuCommand>().command == MenuCommandId.Play, "Play is not initially selected.");
                ExecuteEvents.Execute(events.currentSelectedGameObject, new BaseEventData(events), ExecuteEvents.submitHandler);
                Require(view.CurrentScreen == MenuScreen.Message, "Keyboard submit did not activate Play.");
                view.Back();
                var settings = view.transform.Find("Menu Canvas/Start/Centered start layout/Settings ribbon").GetComponent<Button>();
                settings.Select(); settings.onClick.Invoke();
                Require(view.CurrentScreen == MenuScreen.Settings, "Settings button did not open.");
                view.Back(); Require(events.currentSelectedGameObject == settings.gameObject, "Back did not restore focus.");
                view.ShowPause(); view.Execute(MenuCommandId.Restart);
                Require(events.currentSelectedGameObject.GetComponent<MenuCommand>().command == MenuCommandId.Cancel, "Confirmation did not default to Cancel.");
                var pause = view.transform.Find("Menu Canvas/Pause").GetComponent<CanvasGroup>();
                Require(!pause.interactable && !pause.blocksRaycasts, "Modal allows input to parent.");
                view.Execute(MenuCommandId.Cancel); Require(pause.interactable && pause.blocksRaycasts, "Cancel did not restore parent input.");
                view.ShowResults(true, new MenuResults(492, 2, 12));
                Require(events.currentSelectedGameObject.GetComponent<MenuCommand>().command == MenuCommandId.Restart, "Results focus is incorrect.");
                Require(UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length == 1, "Duplicate EventSystems.");
                Debug.Log("UI_PLAY_VERIFIED: initial focus, keyboard submit, settings return focus, safe confirmation focus, modal input blocking and results focus.");
                if (Application.isBatchMode) EditorApplication.Exit(0); else EditorApplication.ExitPlaymode();
            }
            catch (Exception error)
            {
                Debug.LogException(error);
                if (Application.isBatchMode) EditorApplication.Exit(1); else EditorApplication.ExitPlaymode();
            }
        }
        static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    }
}
