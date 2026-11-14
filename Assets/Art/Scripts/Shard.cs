using System;
using System.Collections.Generic;
using SunSwitch.Player;
using UnityEngine;

namespace SunSwitch.Art
{
    /// <summary>
    /// Collectible shard (GDD §4: 12 in total, 3 per section, each Day-only or Night-only).
    /// Floats and turns slowly; on touch it hides and raises Collected.
    ///
    /// The game-flow side (ShardTracker) subscribes to Collected and counts. On game over it
    /// calls ResetAll() so every shard comes back. Which phase a shard lives in is the prefab
    /// variant (Shard_Day / Shard_Night): its PhaseObject turns the pickup trigger off and
    /// shows the ghost while the other phase is active, so a shard can only be collected in
    /// its own phase. This script never touches the collider for that reason.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class Shard : MonoBehaviour
    {
        /// Raised once per shard when the player picks it up.
        public static event Action<Shard> Collected;

        /// Raised by ResetAll so listeners can reset their count too.
        public static event Action ShardsReset;

        [Tooltip("Also accepted besides a PlayerDeath component on the collider")]
        [SerializeField] string playerTag = "Player";
        [SerializeField] Transform visual;
        [SerializeField] float bobHeight = 0.12f;
        [SerializeField] float bobSeconds = 2.2f;
        [SerializeField] float turnDegreesPerSecond = 45f;

        static readonly List<Shard> all = new List<Shard>();

        Vector3 visualPos;
        float phase;

        public bool IsCollected { get; private set; }

        /// Brings every shard in the loaded scenes back (game over → restart).
        public static void ResetAll()
        {
            foreach (var shard in all) shard.SetCollected(false);
            ShardsReset?.Invoke();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            all.Clear();
            Collected = null;
            ShardsReset = null;
        }

        void Awake()
        {
            if (visual != null) visualPos = visual.localPosition;
            // Spread neighbours so they don't bob in lockstep.
            phase = Mathf.Repeat(transform.position.x * 0.37f + transform.position.z * 0.61f, 1f);
        }

        void OnEnable() => all.Add(this);
        void OnDisable() => all.Remove(this);

        void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        void OnTriggerEnter(Collider other)
        {
            if (IsCollected || !IsPlayer(other)) return;
            SetCollected(true);
            Collected?.Invoke(this);
        }

        bool IsPlayer(Collider other) =>
            other.TryGetComponent<PlayerDeath>(out _) || (!string.IsNullOrEmpty(playerTag) && other.CompareTag(playerTag));

        void SetCollected(bool collected)
        {
            IsCollected = collected;
            if (visual != null) visual.gameObject.SetActive(!collected);
        }

        void Update()
        {
            if (visual == null || IsCollected) return;
            float t = Time.time / bobSeconds + phase;
            visual.localPosition = visualPos + Vector3.up * (Mathf.Sin(t * Mathf.PI * 2f) * bobHeight);
            visual.localRotation = Quaternion.Euler(0f, Time.time * turnDegreesPerSecond + phase * 360f, 0f);
        }
    }
}
