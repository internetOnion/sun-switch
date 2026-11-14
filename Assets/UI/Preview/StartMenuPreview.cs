using UnityEngine;
using UnityEngine.InputSystem;

namespace SunSwitch.UI.Preview
{
    [RequireComponent(typeof(StartMenuView))]
    public sealed class StartMenuPreview : MenuFlowAdapter
    {
        StartMenuView view;
        public override bool IsPreview => true;
        void Awake() { view = GetComponent<StartMenuView>(); view.BindFlow(this); }
        void Update()
        {
            if (Keyboard.current == null) return;
            if (Keyboard.current.f1Key.wasPressedThisFrame) PreviewStart();
            if (Keyboard.current.f2Key.wasPressedThisFrame) PreviewPause();
            if (Keyboard.current.f3Key.wasPressedThisFrame) PreviewGameOver();
            if (Keyboard.current.f4Key.wasPressedThisFrame) PreviewVictory();
        }
        public void PreviewStart() { GetComponent<StartMenuView>().ShowStart(); }
        public void PreviewPause() { GetComponent<StartMenuView>().ShowPause(); }
        public void PreviewGameOver() { GetComponent<StartMenuView>().ShowResults(false, new MenuResults(327, 4, 7)); }
        public void PreviewVictory() { GetComponent<StartMenuView>().ShowResults(true, new MenuResults(492, 2, 12)); }
        public override void RequestStart(StartMenuView source)
        {
            source.CompleteRequest();
            source.ShowMessage("Your journey begins here", "PREVIEW ONLY\nPlay was requested successfully.\nGameplay is not connected.");
        }
        public override void RequestQuit(StartMenuView source)
        {
            source.CompleteRequest();
            source.ShowMessage("Until the next sunrise", "PREVIEW ONLY\nQuit was requested successfully.\nThe preview stays open for review.");
        }
        public override void Request(MenuIntent intent, StartMenuView source)
        {
            Debug.Log($"Menu preview intent: {intent}. No gameplay state was changed.", this);
            if (intent == MenuIntent.Play) { RequestStart(source); return; }
            if (intent == MenuIntent.Quit) { RequestQuit(source); return; }
            source.CompleteRequest();
            if (intent == MenuIntent.MainMenu) source.ShowStart();
            else source.ShowMessage(intent == MenuIntent.Resume ? "Resume requested" : "A new beginning",
                "PREVIEW ONLY\n" + (intent == MenuIntent.Resume ? "Resume" : "Restart") + " was requested successfully.\nNo active gameplay run is connected.");
        }
    }
}
