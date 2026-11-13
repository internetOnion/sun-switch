using SunSwitch.Core;
using UnityEngine;

namespace SunSwitch.Player
{
    /// <summary>
    /// Single entry point for every death. Only raises OnPlayerDied; whoever listens decides
    /// what happens next (RespawnManager in the sandbox, GameManager once Part B is in).
    /// </summary>
    public class PlayerDeath : MonoBehaviour
    {
        public const string LockReason = "death";

        [SerializeField, Tooltip("Falling below this world height counts as a void death")]
        float voidY = -20f;

        public bool IsDying { get; private set; }

        void OnEnable() => GameEvents.OnPlayerRespawned += HandleRespawned;
        void OnDisable() => GameEvents.OnPlayerRespawned -= HandleRespawned;

        void Update()
        {
            if (!IsDying && transform.position.y < voidY) Kill(DeathCause.Void);
        }

        public void Kill(DeathCause cause)
        {
            if (IsDying) return;
            IsDying = true;
            InputLock.Push(LockReason);
            GameEvents.RaisePlayerDied(cause);
        }

        void HandleRespawned() => IsDying = false;
    }
}
