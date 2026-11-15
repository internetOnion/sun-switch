using SunSwitch.Core;
using UnityEngine;

namespace SunSwitch.LookAndFeel
{
    /// <summary>
    /// Day and Night music layers play in sync and cross-fade on phase change.
    /// A separate menu track plays in the Menu state. Tracks never restart, so restarts don't reset the song.
    /// </summary>
    public class MusicPlayer : MonoBehaviour
    {
        [SerializeField] AudioClip dayLayer;
        [SerializeField] AudioClip nightLayer;
        [SerializeField] AudioClip menuTrack;
        [SerializeField, Range(0f, 1f)] float volume = 0.1f;
        [SerializeField, Min(0.01f), Tooltip("Cross-fade time in seconds")] float fadeTime = 0.5f;
        [SerializeField, Tooltip("Tick if the game opens on the Start screen and GameManager doesn't announce Menu")]
        bool startInMenu;

        AudioSource _day, _night, _menu;
        bool _inMenu;

        void Awake()
        {
            _day = MakeSource(dayLayer);
            _night = MakeSource(nightLayer);
            _menu = MakeSource(menuTrack);
            _inMenu = startInMenu;
        }

        AudioSource MakeSource(AudioClip clip)
        {
            var s = gameObject.AddComponent<AudioSource>();
            s.clip = clip;
            s.loop = true;
            s.playOnAwake = false;
            s.volume = 0f;
            return s;
        }

        void Start()
        {
            // Start all layers in the same frame so Day and Night stay in sync.
            if (dayLayer) _day.Play();
            if (nightLayer) _night.Play();
            if (menuTrack) _menu.Play();
        }

        void OnEnable() => GameEvents.OnGameStateChanged += OnState;
        void OnDisable() => GameEvents.OnGameStateChanged -= OnState;

        void OnState(GameState s) => _inMenu = s == GameState.Menu;

        void Update()
        {
            var world = WorldPhase.Instance;
            bool day = world == null || world.Current == Phase.Day;
            float level = volume * GameSettings.MusicVolume;           // music volume from Settings
            float step = Mathf.Max(level, 0.01f) / fadeTime * Time.unscaledDeltaTime;   // unscaled: keeps fading while paused
            _menu.volume = Mathf.MoveTowards(_menu.volume, _inMenu ? level : 0f, step);
            _day.volume = Mathf.MoveTowards(_day.volume, !_inMenu && day ? level : 0f, step);
            _night.volume = Mathf.MoveTowards(_night.volume, !_inMenu && !day ? level : 0f, step);
        }
    }
}
