using System;
using UnityEngine;

namespace SunSwitch.Art
{
    /// <summary>
    /// Launch stone (GDD §5 jump pad). Its trigger sits just above the stone's top.
    ///
    /// The player controller (P2) subscribes to PlayerLaunched and sets its vertical velocity to
    /// LaunchSpeed(its gravity), which reaches launchHeight metres above the pad. The pad itself
    /// only reports the hit and plays a small squash on its Visual.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class JumpPad : MonoBehaviour
    {
        /// Raised when a collider tagged playerTag touches the pad: (pad, player collider).
        public static event Action<JumpPad, Collider> PlayerLaunched;

        [Tooltip("How high the player is thrown above the pad, in metres.")]
        [SerializeField, Min(0.5f)] float launchHeight = 5f;
        [SerializeField] string playerTag = "Player";
        [SerializeField] Transform visual;

        float pulse = 1f;
        Vector3 visualScale = Vector3.one;

        public float LaunchHeight => launchHeight;

        /// Upward speed that reaches LaunchHeight under the given gravity (m/s², sign ignored).
        public float LaunchSpeed(float gravity) => Mathf.Sqrt(2f * Mathf.Abs(gravity) * launchHeight);

        void Awake()
        {
            if (visual != null) visualScale = visual.localScale;
        }

        void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag(playerTag)) return;
            PlayerLaunched?.Invoke(this, other);
            pulse = 0f;
        }

        void Update()
        {
            if (visual == null || pulse >= 1f) return;
            pulse = Mathf.Min(1f, pulse + Time.deltaTime / 0.3f);
            // Quick squash down, then a springy return.
            float s = 1f - 0.35f * Mathf.Sin(pulse * Mathf.PI) * (1f - pulse);
            visual.localScale = new Vector3(visualScale.x * (2f - s), visualScale.y * s, visualScale.z * (2f - s));
        }
    }
}
