using System;
using SunSwitch.Player;
using UnityEngine;

namespace SunSwitch.Core
{
    /// <summary>
    /// Shared contract between Part A and Part B. Systems talk through these events
    /// instead of referencing each other. Change only after agreeing with the other programmer.
    /// </summary>
    public static class GameEvents
    {
        public static event Action<GameState> OnGameStateChanged;
        public static event Action<DeathCause> OnPlayerDied;
        public static event Action OnPlayerRespawned;
        public static event Action<Checkpoint> OnCheckpointReached;
        public static event Action<int> OnLivesChanged;
        public static event Action<int, int> OnShardCollected;   // collected, total
        public static event Action<float> OnAutoCycleTick;       // seconds to next flip; negative = auto-cycle stopped
        public static event Action OnSummitReached;

        public static void RaiseGameStateChanged(GameState state) => OnGameStateChanged?.Invoke(state);
        public static void RaisePlayerDied(DeathCause cause) => OnPlayerDied?.Invoke(cause);
        public static void RaisePlayerRespawned() => OnPlayerRespawned?.Invoke();
        public static void RaiseCheckpointReached(Checkpoint checkpoint) => OnCheckpointReached?.Invoke(checkpoint);
        public static void RaiseLivesChanged(int lives) => OnLivesChanged?.Invoke(lives);
        public static void RaiseShardCollected(int collected, int total) => OnShardCollected?.Invoke(collected, total);
        public static void RaiseAutoCycleTick(float secondsToNextFlip) => OnAutoCycleTick?.Invoke(secondsToNextFlip);
        public static void RaiseSummitReached() => OnSummitReached?.Invoke();

        // Private on purpose: clearing on restart would silence managers that subscribe only once (respawn, audio).
        static void ClearAll()
        {
            OnGameStateChanged = null;
            OnPlayerDied = null;
            OnPlayerRespawned = null;
            OnCheckpointReached = null;
            OnLivesChanged = null;
            OnShardCollected = null;
            OnAutoCycleTick = null;
            OnSummitReached = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => ClearAll();
    }
}
