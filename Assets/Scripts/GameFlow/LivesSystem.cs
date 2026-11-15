using SunSwitch.Core;
using UnityEngine;

namespace SunSwitch.GameFlow
{
    /// <summary>
    /// Takes a life on every death and hands the decision (respawn or Game Over) to GameManager.
    /// </summary>
    public class LivesSystem : MonoBehaviour
    {
        [SerializeField, Min(1)] int startLives = 3;

        public int Lives { get; private set; }
        public int StartLives => startLives;

        void Awake() => Lives = startLives;
        void OnEnable() => GameEvents.OnPlayerDied += HandleDied;
        void OnDisable() => GameEvents.OnPlayerDied -= HandleDied;

        void HandleDied(DeathCause _)
        {
            var game = GameManager.Instance;
            // No life lost outside play, or after the run has ended (falling after the summit).
            if (game != null && (game.State != GameState.Playing || game.IsEnding)) return;

            Lives = Mathf.Max(0, Lives - 1);
            GameEvents.RaiseLivesChanged(Lives);
            if (game != null) game.HandleLifeLost(Lives);
        }

        public void ResetLives()
        {
            Lives = startLives;
            GameEvents.RaiseLivesChanged(Lives);
        }
    }
}
