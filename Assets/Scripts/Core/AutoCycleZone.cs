using System.Collections;
using SunSwitch.Player;
using UnityEngine;

namespace SunSwitch.Core
{
    /// <summary>
    /// Section 4 finale: while the player is inside this trigger, the world flips on a fixed timer
    /// and manual switching is disabled.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class AutoCycleZone : MonoBehaviour
    {
        [SerializeField, Min(0.1f), Tooltip("Seconds between automatic flips")]
        float interval = 2f;

        Collider _zone;
        Transform _player;
        Coroutine _loop;

        public bool IsRunning => _loop != null;

        void Reset() => GetComponent<Collider>().isTrigger = true;

        void Awake()
        {
            _zone = GetComponent<Collider>();
            if (!_zone.isTrigger) Debug.LogWarning("AutoCycleZone: collider should be a trigger.", this);
        }

        void OnEnable()
        {
            GameEvents.OnPlayerDied += HandleDied;
            GameEvents.OnPlayerRespawned += HandleRespawned;
        }

        void OnDisable()
        {
            GameEvents.OnPlayerDied -= HandleDied;
            GameEvents.OnPlayerRespawned -= HandleRespawned;
            StopCycle();
        }

        void OnTriggerEnter(Collider other)
        {
            if (!other.TryGetComponent<PlayerDeath>(out _)) return;
            _player = other.transform;
            StartCycle();
        }

        void OnTriggerExit(Collider other)
        {
            if (other.TryGetComponent<PlayerDeath>(out _)) StopCycle();
        }

        void HandleDied(DeathCause _) => StopCycle();

        // A checkpoint inside the zone teleports the player in without a new OnTriggerEnter.
        void HandleRespawned()
        {
            if (_player != null && _zone.bounds.Contains(_player.position)) StartCycle();
        }

        void StartCycle()
        {
            if (_loop != null || WorldPhase.Instance == null) return;
            WorldPhase.Instance.ManualSwitchEnabled = false;
            _loop = StartCoroutine(Cycle());
        }

        void StopCycle()
        {
            if (_loop == null) return;
            StopCoroutine(_loop);
            _loop = null;
            if (WorldPhase.Instance != null) WorldPhase.Instance.ManualSwitchEnabled = true;
            GameEvents.RaiseAutoCycleTick(-1f);
        }

        IEnumerator Cycle()
        {
            while (true)
            {
                float remaining = interval;
                while (remaining > 0f)
                {
                    GameEvents.RaiseAutoCycleTick(remaining);
                    yield return null;
                    remaining -= Time.deltaTime;
                }

                var world = WorldPhase.Instance;
                world.ForceSet(PhaseUtil.Opposite(world.Current));
            }
        }
    }
}
