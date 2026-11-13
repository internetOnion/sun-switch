using System;
using UnityEngine;

namespace SunSwitch.Art
{
    /// <summary>
    /// Collectible shard (GDD §4: 12 in total, 3 per section, each Day-only or Night-only).
    /// Floats and turns slowly; on touch it hides and raises Collected.
    ///
    /// The score / UI system (P3) subscribes to Collected and counts. On game over it calls
    /// ResetAll() so every shard comes back. Which phase a shard lives in is the prefab variant
    /// (Shard_Day / Shard_Night); the phase system shows / hides it like any phased object.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class Shard : MonoBehaviour
    {
        /// Raised once per shard when the player picks it up.
        public static event Action<Shard> Collected;

        /// Raised by ResetAll so listeners can reset their count too.
        public static event Action ShardsReset;

        [SerializeField] string playerTag = "Player";
        [SerializeField] Transform visual;
        [SerializeField] float bobHeight = 0.12f;
        [SerializeField] float bobSeconds = 2.2f;
        [SerializeField] float turnDegreesPerSecond = 45f;

        static readonly System.Collections.Generic.List<Shard> all = new System.Collections.Generic.List<Shard>();

        Vector3 visualPos;
        float phase;

        public bool IsCollected { get; private set; }

        /// Brings every shard in the loaded scenes back (game over → restart).
        public static void ResetAll()
        {
            foreach (var shard in all) shard.SetCollected(false);
            ShardsReset?.Invoke();
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
            if (IsCollected || !other.CompareTag(playerTag)) return;
            SetCollected(true);
            Collected?.Invoke(this);
        }

        void SetCollected(bool collected)
        {
            IsCollected = collected;
            GetComponent<Collider>().enabled = !collected;
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
