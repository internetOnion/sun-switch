using System;
using UnityEngine;

namespace SunSwitch.Core
{
    /// <summary>Single source of truth for the current Day/Night phase.</summary>
    [DefaultExecutionOrder(-100)]   // Awake before PhaseObjects and other listeners
    public class WorldPhase : MonoBehaviour
    {
        public static WorldPhase Instance { get; private set; }

        [SerializeField] Phase startPhase = Phase.Day;
        [SerializeField, Min(0f), Tooltip("Seconds between manual switches")]
        float cooldown = 0.4f;

        public Phase Current { get; private set; }
        public bool ManualSwitchEnabled { get; set; } = true;

        /// <summary>1 right after a switch, 0 when the next switch is allowed. Drives the HUD ring.</summary>
        public float CooldownRemaining01 => cooldown <= 0f ? 0f : Mathf.Clamp01(_cooldownLeft / cooldown);

        /// <summary>True when the latest change should snap instead of fade (e.g. respawn).</summary>
        public bool LastChangeInstant { get; private set; }

        public event Action<Phase> OnPhaseChanged;
        public event Action OnSwitchBlocked;

        /// <summary>Set by PhaseOverlapChecker. Returns true if switching to the given phase would overlap the player.</summary>
        public Func<Phase, bool> OverlapTest;

        float _cooldownLeft;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            Current = startPhase;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Update()
        {
            if (_cooldownLeft > 0f) _cooldownLeft -= Time.deltaTime;
        }

        /// <summary>Manual switch from player input. Fails on cooldown, lock, disabled switching or overlap.</summary>
        public bool TrySwitch()
        {
            if (!ManualSwitchEnabled || InputLock.IsLocked || _cooldownLeft > 0f) return false;

            Phase next = PhaseUtil.Opposite(Current);
            if (OverlapTest != null && OverlapTest(next))
            {
                OnSwitchBlocked?.Invoke();
                return false;
            }

            SetPhase(next, false);
            _cooldownLeft = cooldown;
            return true;
        }

        /// <summary>Respawn, auto-cycle, menu: ignores cooldown and overlap.</summary>
        public void ForceSet(Phase phase, bool instant = false)
        {
            _cooldownLeft = 0f;
            SetPhase(phase, instant);
        }

        void SetPhase(Phase phase, bool instant)
        {
            Current = phase;
            LastChangeInstant = instant;
            OnPhaseChanged?.Invoke(phase);
        }
    }
}
