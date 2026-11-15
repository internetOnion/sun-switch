using System.Collections;
using SunSwitch.Core;
using SunSwitch.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SunSwitch.GameFlow
{
    /// <summary>
    /// Owns the game state (Menu, Playing, Paused, GameOver, Won) and the run lifecycle.
    /// Put it on Managers with LivesSystem, RunTimer and ShardTracker (and SectionLoader if used).
    /// Untick RespawnManager's "Respawn On Death": this script decides between respawn and Game Over.
    /// </summary>
    [DefaultExecutionOrder(-30)]
    public class GameManager : MonoBehaviour
    {
        public const string LockReason = "ui";

        public static GameManager Instance { get; private set; }

        [SerializeField] LivesSystem lives;
        [SerializeField] RunTimer timer;
        [SerializeField] ShardTracker shards;
        [SerializeField, Tooltip("Optional. Empty = sections are already in the scene.")]
        SectionLoader sections;
        [SerializeField, Tooltip("Untick to skip the Start screen and play right away (testing)")]
        bool startInMenu = true;
        [SerializeField, Tooltip("Seconds from death to respawn")]
        float respawnDelay = 0.6f;
        [SerializeField, Tooltip("Seconds between the run ending and the end screen, for the death or summit beat")]
        float endScreenDelay = 0.8f;
        [SerializeField] InputAction pauseAction = new InputAction("Pause", InputActionType.Button, "<Keyboard>/escape");

        public GameState State { get; private set; } = GameState.Menu;
        public RunStats Stats { get; } = new RunStats();
        public LivesSystem Lives => lives;
        public RunTimer Timer => timer;
        public ShardTracker Shards => shards;

        /// <summary>The menu UI sets this false when it handles Esc on the Pause screen itself.</summary>
        public bool EscapeResumes { get; set; } = true;

        /// <summary>True between the run ending (last life or summit) and the end screen.</summary>
        public bool IsEnding => _ending;

        int _stateFrame = -1;
        bool _ending;

        void Reset()
        {
            lives = GetComponent<LivesSystem>();
            timer = GetComponent<RunTimer>();
            shards = GetComponent<ShardTracker>();
            sections = GetComponent<SectionLoader>();
        }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            InputLock.Push(LockReason);   // locked until the run starts
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void OnEnable()
        {
            pauseAction.Enable();
            GameEvents.OnSummitReached += HandleSummit;
        }

        void OnDisable()
        {
            pauseAction.Disable();
            GameEvents.OnSummitReached -= HandleSummit;
        }

        IEnumerator Start()
        {
            while (sections != null && !sections.IsReady) yield return null;
            if (startInMenu) SetState(GameState.Menu);
            else StartRun();
        }

        void Update()
        {
            if (!pauseAction.WasPressedThisFrame()) return;
            if (State == GameState.Playing) Pause();
            else if (State == GameState.Paused && EscapeResumes) Resume();
        }

        /// <summary>Play, Restart, Try again and Play again all start a fresh run.</summary>
        public void StartRun()
        {
            StopAllCoroutines();
            _ending = false;
            Time.timeScale = 1f;
            Stats.Clear();
            if (lives) lives.ResetLives();
            if (shards) shards.ResetShards();
            if (timer) { timer.ResetTimer(); timer.Begin(); }
            if (RespawnManager.Instance) RespawnManager.Instance.ResetToStart();
            SetState(GameState.Playing);
        }

        public void RestartRun() => StartRun();

        public void Pause()
        {
            if (State != GameState.Playing || _ending || Time.frameCount == _stateFrame) return;
            Time.timeScale = 0f;
            SetState(GameState.Paused);
        }

        public void Resume()
        {
            // Same-frame guard: the Esc that paused must not also resume through the menu.
            if (State != GameState.Paused || Time.frameCount == _stateFrame) return;
            Time.timeScale = 1f;
            SetState(GameState.Playing);
        }

        public void QuitToMenu()
        {
            StopAllCoroutines();
            _ending = false;
            Time.timeScale = 1f;
            if (timer) timer.Stop();
            // Back to the start behind the menu, so the menu's Day/Night flips can't drop the
            // player off a phase platform mid-tower.
            if (RespawnManager.Instance) RespawnManager.Instance.ResetToStart();
            SetState(GameState.Menu);
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        /// <summary>Called by LivesSystem after it takes a life.</summary>
        public void HandleLifeLost(int livesLeft)
        {
            if (State != GameState.Playing || _ending) return;
            Stats.deaths++;
            if (livesLeft > 0)
            {
                if (RespawnManager.Instance) RespawnManager.Instance.Respawn(respawnDelay);
            }
            else
            {
                StartCoroutine(EndRun(false));
            }
        }

        void HandleSummit()
        {
            if (State == GameState.Playing && !_ending) StartCoroutine(EndRun(true));
        }

        IEnumerator EndRun(bool won)
        {
            _ending = true;
            if (timer) timer.Stop();
            InputLock.Push(LockReason);
            Stats.Fill(timer ? timer.Elapsed : 0f, shards ? shards.Collected : 0, shards ? shards.Total : 0, won);
            if (endScreenDelay > 0f) yield return new WaitForSecondsRealtime(endScreenDelay);
            _ending = false;
            SetState(won ? GameState.Won : GameState.GameOver);
        }

        void SetState(GameState state)
        {
            State = state;
            _stateFrame = Time.frameCount;
            if (state == GameState.Playing) InputLock.Pop(LockReason);
            else InputLock.Push(LockReason);
            GameEvents.RaiseGameStateChanged(state);
        }
    }
}
