using SunSwitch.Core;
using SunSwitch.GameFlow;
using UnityEngine;

namespace SunSwitch.UI
{
    /// <summary>
    /// Connects the designer's StartMenuView (Start, Pause, Controls, Game Over, Victory screens)
    /// to GameManager. Replaces StartMenuPreview on the MenuRoot prefab: remove that one.
    /// Covers UIManager, StartMenu, PauseMenu, HowToPlayPanel and EndScreen from the spec.
    /// </summary>
    [RequireComponent(typeof(StartMenuView))]
    public sealed class GameMenuFlow : MenuFlowAdapter
    {
        [SerializeField, Tooltip("Seconds between automatic Day/Night flips behind the Start screen; 0 = off")]
        float menuFlipInterval = 4f;

        StartMenuView _view;
        float _flipTimer;

        // Subscribed in Awake, not OnEnable: this object is switched off during play.
        void Awake()
        {
            _view = GetComponent<StartMenuView>();
            _view.BindFlow(this);
            GameEvents.OnGameStateChanged += HandleState;
            if (GameManager.Instance != null) GameManager.Instance.EscapeResumes = false;   // the view handles Esc on Pause
        }

        void OnDestroy() => GameEvents.OnGameStateChanged -= HandleState;

        void HandleState(GameState state)
        {
            gameObject.SetActive(state != GameState.Playing);
            switch (state)
            {
                case GameState.Menu: _view.ShowStart(); break;
                case GameState.Paused: _view.ShowPause(); break;
                case GameState.GameOver:
                case GameState.Won:
                    var s = GameManager.Instance.Stats;
                    _view.ShowResults(state == GameState.Won, new MenuResults(s.time, s.deaths, s.shards));
                    break;
            }
        }

        void Update()
        {
            var game = GameManager.Instance;
            var world = WorldPhase.Instance;
            if (menuFlipInterval <= 0f || game == null || world == null || game.State != GameState.Menu) return;

            _flipTimer += Time.unscaledDeltaTime;
            if (_flipTimer < menuFlipInterval) return;
            _flipTimer = 0f;
            world.ForceSet(PhaseUtil.Opposite(world.Current));
        }

        public override void RequestStart(StartMenuView view) => Request(MenuIntent.Play, view);
        public override void RequestQuit(StartMenuView view) => Request(MenuIntent.Quit, view);

        public override void Request(MenuIntent intent, StartMenuView view)
        {
            view.CompleteRequest();
            var game = GameManager.Instance;
            if (game == null)
            {
                view.ShowMessage("Action unavailable", "No GameManager in the scene.");
                return;
            }

            switch (intent)
            {
                case MenuIntent.Play:
                case MenuIntent.Restart: game.StartRun(); break;
                case MenuIntent.Resume: game.Resume(); break;
                case MenuIntent.MainMenu: game.QuitToMenu(); break;
                case MenuIntent.Quit: game.QuitGame(); break;
            }
        }
    }
}
