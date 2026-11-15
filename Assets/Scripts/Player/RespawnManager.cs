using System.Collections;
using SunSwitch.Core;
using UnityEngine;

namespace SunSwitch.Player
{
    /// <summary>Remembers the last checkpoint and puts the player back there with its saved phase.</summary>
    [DefaultExecutionOrder(-50)]
    public class RespawnManager : MonoBehaviour
    {
        public static RespawnManager Instance { get; private set; }

        [SerializeField, Tooltip("Found automatically if empty")]
        LightkeeperController player;
        [SerializeField, Tooltip("Checkpoint for the start of a run. Empty = the checkpoint marked Is Start.")]
        Checkpoint startCheckpoint;
        [SerializeField, Tooltip("Seconds from death to respawn when respawning on its own")]
        float respawnDelay = 0.6f;
        [SerializeField, Tooltip("Seconds controls stay locked after the teleport")]
        float unlockDelay = 0.3f;
        [SerializeField, Tooltip("Sandbox only: respawn after every death. Untick once GameManager decides between respawn and Game Over.")]
        bool respawnOnDeath = true;

        public Checkpoint Current { get; private set; }

        Vector3 _fallbackPosition;
        Quaternion _fallbackRotation;
        Coroutine _routine;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Start()
        {
            if (player == null) player = FindAnyObjectByType<LightkeeperController>();
            if (player != null)
            {
                _fallbackPosition = player.transform.position;
                _fallbackRotation = player.transform.rotation;
            }
            else
            {
                Debug.LogWarning("RespawnManager: no LightkeeperController found.", this);
            }
        }

        void OnEnable() => GameEvents.OnPlayerDied += HandleDied;
        void OnDisable() => GameEvents.OnPlayerDied -= HandleDied;

        void HandleDied(DeathCause _)
        {
            if (respawnOnDeath) Respawn(respawnDelay);
        }

        public void SetCheckpoint(Checkpoint checkpoint) => Current = checkpoint;

        /// <summary>Respawn now at the current checkpoint.</summary>
        public void Respawn() => Respawn(0f);

        public void Respawn(float delay)
        {
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(RespawnRoutine(delay));
        }

        /// <summary>Full restart: back to the start checkpoint.</summary>
        public void ResetToStart()
        {
            Current = FindStartCheckpoint();
            // A new run: checkpoints from the last one are unreached again, except the start.
            foreach (var cp in FindObjectsByType<Checkpoint>(FindObjectsSortMode.None))
                cp.SetReached(cp == Current);
            Respawn(0f);
        }

        Checkpoint FindStartCheckpoint()
        {
            if (startCheckpoint != null) return startCheckpoint;
            foreach (var cp in FindObjectsByType<Checkpoint>(FindObjectsSortMode.None))
                if (cp.IsStart) return cp;
            return null;
        }

        IEnumerator RespawnRoutine(float delay)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);

            if (player != null)
            {
                if (Current != null)
                {
                    var spawn = Current.SpawnPoint;
                    player.Teleport(spawn.position, spawn.rotation);
                    if (WorldPhase.Instance != null) WorldPhase.Instance.ForceSet(Current.SavedPhase, true);
                }
                else
                {
                    player.Teleport(_fallbackPosition, _fallbackRotation);
                }
            }

            GameEvents.RaisePlayerRespawned();

            if (unlockDelay > 0f) yield return new WaitForSeconds(unlockDelay);
            InputLock.Pop(PlayerDeath.LockReason);
            _routine = null;
        }
    }
}
