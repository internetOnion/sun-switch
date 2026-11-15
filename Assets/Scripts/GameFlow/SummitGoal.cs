using SunSwitch.Core;
using SunSwitch.Player;
using UnityEngine;

namespace SunSwitch.GameFlow
{
    /// <summary>
    /// End of the course. Touching it plays the ending beat and raises OnSummitReached;
    /// GameManager waits its end-screen delay so the beat is seen before the Win screen.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class SummitGoal : MonoBehaviour
    {
        [SerializeField, Tooltip("Optional light burst / particles, switched on when reached")]
        GameObject endingBeat;

        void Reset() => GetComponent<Collider>().isTrigger = true;

        void Awake()
        {
            if (endingBeat) endingBeat.SetActive(false);
        }

        void OnEnable() => GameEvents.OnGameStateChanged += HandleState;
        void OnDisable() => GameEvents.OnGameStateChanged -= HandleState;

        void HandleState(GameState state)
        {
            if (state == GameState.Menu && endingBeat) endingBeat.SetActive(false);
        }

        void OnTriggerEnter(Collider other)
        {
            if (!other.TryGetComponent<PlayerDeath>(out _)) return;
            if (endingBeat) endingBeat.SetActive(true);
            GameEvents.RaiseSummitReached();   // GameManager ignores repeats
        }
    }
}
