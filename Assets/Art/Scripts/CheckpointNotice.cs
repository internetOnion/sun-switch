using SunSwitch.Core;
using SunSwitch.Player;
using UnityEngine;

namespace SunSwitch.Art
{
    /// <summary>
    /// HUD notice under the timer: "Checkpoint" fades in, drops a little and fades out when the
    /// player reaches a new checkpoint (not the start one). Pairs with CheckpointLamp's burst in
    /// the world. Uses unscaled time, so it finishes even if the game pauses straight away.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class CheckpointNotice : MonoBehaviour
    {
        [SerializeField] float fadeIn = 0.18f;
        [SerializeField] float hold = 1.2f;
        [SerializeField] float fadeOut = 0.45f;
        [SerializeField] float drop = 14f;

        CanvasGroup group;
        RectTransform rect;
        Vector2 home;
        float t = -1f;

        public bool Showing => t >= 0f;

        void Awake()
        {
            group = GetComponent<CanvasGroup>();
            rect = (RectTransform)transform;
            home = rect.anchoredPosition;
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
        }

        void OnEnable() => GameEvents.OnCheckpointReached += HandleReached;
        void OnDisable() => GameEvents.OnCheckpointReached -= HandleReached;

        void HandleReached(Checkpoint checkpoint)
        {
            if (checkpoint != null && checkpoint.IsStart) return;
            t = 0f;
        }

        void Update()
        {
            if (t < 0f) return;
            t += Time.unscaledDeltaTime;
            float a;
            if (t < fadeIn) a = t / fadeIn;
            else if (t < fadeIn + hold) a = 1f;
            else a = 1f - (t - fadeIn - hold) / fadeOut;
            group.alpha = Mathf.Clamp01(a);
            float settle = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / (fadeIn * 1.5f)));
            rect.anchoredPosition = home + Vector2.up * drop * (1f - settle);
            if (t >= fadeIn + hold + fadeOut) { t = -1f; group.alpha = 0f; rect.anchoredPosition = home; }
        }
    }
}
