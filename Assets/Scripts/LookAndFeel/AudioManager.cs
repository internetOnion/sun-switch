using System;
using SunSwitch.Core;
using UnityEngine;

namespace SunSwitch.LookAndFeel
{
    public enum SfxId { SwitchDay, SwitchNight, SwitchBlocked, Jump, JumpPad, Death, Checkpoint, Shard, UIClick, Win, GameOver }

    /// <summary>
    /// One-shot sound effects. Listens to WorldPhase and GameEvents itself;
    /// other scripts only call Play for sounds no event covers (UI clicks).
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        [Serializable]
        public class Sfx
        {
            public SfxId id;
            public AudioClip[] clips;
            [Range(0f, 1f)] public float volume = 1f;
        }

        public static AudioManager Instance { get; private set; }

        [SerializeField] Sfx[] sounds;
        [SerializeField, Min(1)] int voices = 8;
        [SerializeField, Range(0f, 0.2f), Tooltip("Random pitch spread so repeats don't sound robotic")]
        float pitchVariation = 0.05f;

        AudioSource[] _sources;
        int _next;
        WorldPhase _world;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            _sources = new AudioSource[voices];
            for (int i = 0; i < voices; i++)
            {
                _sources[i] = gameObject.AddComponent<AudioSource>();
                _sources[i].playOnAwake = false;
            }
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void OnEnable()
        {
            GameEvents.OnPlayerDied += OnDied;
            GameEvents.OnCheckpointReached += OnCheckpoint;
            GameEvents.OnShardCollected += OnShard;
            GameEvents.OnSummitReached += OnSummit;
            GameEvents.OnGameStateChanged += OnState;
            Subscribe();
        }

        void Start() => Subscribe();   // WorldPhase may not exist yet in OnEnable

        void OnDisable()
        {
            GameEvents.OnPlayerDied -= OnDied;
            GameEvents.OnCheckpointReached -= OnCheckpoint;
            GameEvents.OnShardCollected -= OnShard;
            GameEvents.OnSummitReached -= OnSummit;
            GameEvents.OnGameStateChanged -= OnState;
            if (_world != null)
            {
                _world.OnPhaseChanged -= OnPhase;
                _world.OnSwitchBlocked -= OnBlocked;
                _world = null;
            }
        }

        void Subscribe()
        {
            if (_world != null || WorldPhase.Instance == null) return;
            _world = WorldPhase.Instance;
            _world.OnPhaseChanged += OnPhase;
            _world.OnSwitchBlocked += OnBlocked;
        }

        void OnPhase(Phase p)
        {
            if (!_world.LastChangeInstant) Play(p == Phase.Day ? SfxId.SwitchDay : SfxId.SwitchNight);   // silent on respawn snap
        }

        void OnBlocked() => Play(SfxId.SwitchBlocked);
        void OnDied(DeathCause _) => Play(SfxId.Death);
        void OnCheckpoint(Player.Checkpoint _) => Play(SfxId.Checkpoint);
        void OnShard(int _, int __) => Play(SfxId.Shard);
        void OnSummit() => Play(SfxId.Win);
        void OnState(GameState s) { if (s == GameState.GameOver) Play(SfxId.GameOver); }

        public void Play(SfxId id)
        {
            if (sounds == null) return;
            foreach (var s in sounds)
            {
                if (s.id != id || s.clips == null || s.clips.Length == 0) continue;
                var clip = s.clips[UnityEngine.Random.Range(0, s.clips.Length)];
                if (clip == null) return;

                var source = _sources[_next];
                _next = (_next + 1) % _sources.Length;
                source.pitch = 1f + UnityEngine.Random.Range(-pitchVariation, pitchVariation);
                source.PlayOneShot(clip, s.volume * GameSettings.SfxVolume);   // SFX volume from Settings
                return;
            }
        }
    }
}
