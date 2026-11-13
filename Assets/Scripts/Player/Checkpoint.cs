using SunSwitch.Core;
using UnityEngine;

namespace SunSwitch.Player
{
    /// <summary>
    /// Save point. Put it on a trigger volume above a Both platform (not on the platform's solid collider).
    /// Saves the phase active when touched; respawn restores it.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class Checkpoint : MonoBehaviour
    {
        [SerializeField, Tooltip("Where the player appears. Empty = this object's position.")]
        Transform spawnPoint;
        [SerializeField, Tooltip("Tick on the very first checkpoint of Section 1")]
        bool isStart;
        [SerializeField, Tooltip("Optional object switched on once reached (light, flag)")]
        GameObject activeIndicator;

        public bool IsStart => isStart;
        public Phase SavedPhase { get; private set; } = Phase.Day;
        public Transform SpawnPoint => spawnPoint != null ? spawnPoint : transform;

        bool _reached;

        void Reset() => GetComponent<Collider>().isTrigger = true;

        void Awake()
        {
            if (!GetComponent<Collider>().isTrigger)
                Debug.LogWarning("Checkpoint: collider should be a trigger.", this);
            if (activeIndicator != null) activeIndicator.SetActive(false);
        }

        void Start()
        {
            if (WorldPhase.Instance != null) SavedPhase = WorldPhase.Instance.Current;
        }

        void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;

            if (WorldPhase.Instance != null) SavedPhase = WorldPhase.Instance.Current;

            var respawn = RespawnManager.Instance;
            bool isNew = respawn == null || respawn.Current != this;
            if (respawn != null) respawn.SetCheckpoint(this);

            if (!_reached)
            {
                _reached = true;
                if (activeIndicator != null) activeIndicator.SetActive(true);
            }

            if (isNew) GameEvents.RaiseCheckpointReached(this);
        }

        void OnDrawGizmos()
        {
            var spawn = SpawnPoint;
            Gizmos.color = isStart ? Color.green : Color.cyan;
            Gizmos.DrawWireSphere(spawn.position, 0.3f);
            Gizmos.DrawLine(spawn.position, spawn.position + spawn.forward);
        }
    }
}
