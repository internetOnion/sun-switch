using System.Collections;
using SunSwitch.Core;
using SunSwitch.GameFlow;
using UnityEngine;
using UnityEngine.UI;

namespace SunSwitch.UI
{
    /// <summary>
    /// In-game info: life icons, run timer and shard counter. Shown while Playing or Paused.
    /// Reads GameManager every frame, so it is always right after restarts without extra events.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class HUDController : MonoBehaviour
    {
        [SerializeField, Tooltip("One icon per life, left to right")] Graphic[] lifeIcons;
        [SerializeField] Color lostLifeTint = new Color(0.4f, 0.4f, 0.4f, 0.35f);
        [SerializeField] Text timerText;
        [SerializeField] Text shardText;
        [SerializeField, Tooltip("Bounce size when a value changes")] float popScale = 1.25f;

        CanvasGroup _group;
        Color[] _lifeColors;
        int _lives = -1, _shards = -1;

        void Awake()
        {
            _group = GetComponent<CanvasGroup>();
            _group.interactable = false;
            _group.blocksRaycasts = false;
            _lifeColors = new Color[lifeIcons.Length];
            for (int i = 0; i < lifeIcons.Length; i++) _lifeColors[i] = lifeIcons[i].color;
            Show(GameManager.Instance != null && GameManager.Instance.State == GameState.Playing);
        }

        void OnEnable() => GameEvents.OnGameStateChanged += HandleState;
        void OnDisable() => GameEvents.OnGameStateChanged -= HandleState;

        void HandleState(GameState state) => Show(state == GameState.Playing || state == GameState.Paused);
        void Show(bool on) => _group.alpha = on ? 1f : 0f;

        void Update()
        {
            var game = GameManager.Instance;
            if (game == null) return;

            if (game.Timer && timerText) timerText.text = RunTimer.Format(game.Timer.Elapsed);

            if (game.Lives && game.Lives.Lives != _lives)
            {
                int lost = _lives - game.Lives.Lives;
                _lives = game.Lives.Lives;
                for (int i = 0; i < lifeIcons.Length; i++)
                {
                    lifeIcons[i].color = i < _lives ? _lifeColors[i] : _lifeColors[i] * lostLifeTint;
                    if (lost > 0 && i == _lives) Pop(lifeIcons[i].transform);   // the life just lost
                }
            }

            if (game.Shards && game.Shards.Collected != _shards)
            {
                bool gained = game.Shards.Collected > _shards && _shards >= 0;
                _shards = game.Shards.Collected;
                if (shardText)
                {
                    shardText.text = $"{_shards}/{game.Shards.Total}";
                    if (gained) Pop(shardText.transform);
                }
            }
        }

        void Pop(Transform target) => StartCoroutine(PopRoutine(target));

        IEnumerator PopRoutine(Transform target)
        {
            const float duration = 0.2f;
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                target.localScale = Vector3.one * Mathf.Lerp(popScale, 1f, t / duration);
                yield return null;
            }
            target.localScale = Vector3.one;
        }
    }
}
