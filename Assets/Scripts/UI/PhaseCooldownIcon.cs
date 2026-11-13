using System.Collections;
using SunSwitch.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SunSwitch.UI
{
    /// <summary>
    /// Sun/moon icon with a ring that fills as the switch cooldown ends. Shakes red when a switch is blocked.
    /// In the Summit auto-cycle the ring drains to show the time until the next flip.
    /// </summary>
    public class PhaseCooldownIcon : MonoBehaviour
    {
        [SerializeField, Tooltip("Image with Fill Method = Radial 360")] Image ring;
        [SerializeField] GameObject sunIcon;
        [SerializeField] GameObject moonIcon;
        [SerializeField] Color blockedColor = new Color(0.85f, 0.25f, 0.2f);
        [SerializeField] float shakePixels = 8f;

        RectTransform _rect;
        Vector2 _home;
        Color _ringColor;
        WorldPhase _world;
        float _autoLeft = -1f, _autoInterval;

        void Awake()
        {
            _rect = (RectTransform)transform;
            _home = _rect.anchoredPosition;
            if (ring) _ringColor = ring.color;
        }

        void OnEnable()
        {
            GameEvents.OnAutoCycleTick += HandleAutoTick;
            Subscribe();
        }

        void Start() => Subscribe();   // WorldPhase may not exist yet in OnEnable

        void OnDisable()
        {
            GameEvents.OnAutoCycleTick -= HandleAutoTick;
            if (_world != null) _world.OnSwitchBlocked -= HandleBlocked;
            _world = null;
            _rect.anchoredPosition = _home;
            if (ring) ring.color = _ringColor;
        }

        void Subscribe()
        {
            if (_world != null || WorldPhase.Instance == null) return;
            _world = WorldPhase.Instance;
            _world.OnSwitchBlocked += HandleBlocked;
        }

        void HandleAutoTick(float secondsLeft)
        {
            if (secondsLeft > _autoLeft) _autoInterval = secondsLeft;   // first tick of each cycle is the full interval
            _autoLeft = secondsLeft;
        }

        void Update()
        {
            var world = WorldPhase.Instance;
            if (world == null) return;

            bool day = world.Current == Phase.Day;
            if (sunIcon) sunIcon.SetActive(day);
            if (moonIcon) moonIcon.SetActive(!day);

            if (!ring) return;
            ring.fillAmount = _autoLeft >= 0f && _autoInterval > 0f
                ? _autoLeft / _autoInterval
                : 1f - world.CooldownRemaining01;
        }

        void HandleBlocked()
        {
            StopAllCoroutines();
            StartCoroutine(Shake());
        }

        IEnumerator Shake()
        {
            const float duration = 0.3f;
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                float k = t / duration;
                _rect.anchoredPosition = _home + Vector2.right * (Mathf.Sin(k * Mathf.PI * 6f) * shakePixels);   // 3 shakes
                if (ring) ring.color = Color.Lerp(blockedColor, _ringColor, k);
                yield return null;
            }
            _rect.anchoredPosition = _home;
            if (ring) ring.color = _ringColor;
        }
    }
}
