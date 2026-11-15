using System.Collections;
using SunSwitch.Player;
using SunSwitch.LookAndFeel;
using UnityEngine;

namespace SunSwitch.Obstacles
{
    /// <summary>
    /// Launches the player. Put it on a thin trigger volume sitting on top of the pad's solid collider.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class JumpPad : MonoBehaviour
    {
        [SerializeField, Tooltip("Launch speed in m/s")]
        float launchSpeed = 14f;
        [SerializeField, Tooltip("Launch direction in local space")]
        Vector3 direction = Vector3.up;
        [SerializeField, Tooltip("Seconds before the pad can fire again")]
        float retriggerDelay = 0.2f;
        [SerializeField, Tooltip("Optional mesh that squashes on launch")]
        Transform visual;
        [SerializeField, Tooltip("Optional launch sound")]
        AudioSource launchSound;

        float _nextAllowedTime;
        Vector3 _visualScale;
        Coroutine _squash;

        void Reset() => GetComponent<Collider>().isTrigger = true;

        void Awake()
        {
            if (visual != null) _visualScale = visual.localScale;
        }

        void OnTriggerEnter(Collider other)
        {
            if (Time.time < _nextAllowedTime) return;
            if (!other.TryGetComponent(out LightkeeperController player)) return;

            _nextAllowedTime = Time.time + retriggerDelay;
            player.AddExternalVelocity(transform.TransformDirection(direction.normalized) * launchSpeed);

            if (launchSound != null) launchSound.Play();
            else AudioManager.Instance?.Play(SfxId.JumpPad);
            if (visual != null)
            {
                if (_squash != null) StopCoroutine(_squash);
                _squash = StartCoroutine(Squash());
            }
        }

        IEnumerator Squash()
        {
            const float duration = 0.2f;
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float k = Mathf.Sin(t / duration * Mathf.PI);
                visual.localScale = Vector3.Scale(_visualScale, new Vector3(1f + 0.15f * k, 1f - 0.3f * k, 1f + 0.15f * k));
                yield return null;
            }
            visual.localScale = _visualScale;
            _squash = null;
        }
    }
}
