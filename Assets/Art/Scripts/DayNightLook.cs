using System;
using SunSwitch.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace SunSwitch.Art
{
    /// <summary>
    /// The world's Day / Night presentation (GDD §2 "Switch presentation"): cross-fades the
    /// sun/moon light (colour, intensity, angle), the StorybookSky, fog and ambient light.
    ///
    /// One per scene. In Play mode it follows WorldPhase by itself: every OnPhaseChanged fades
    /// to the new phase, or snaps when WorldPhase.LastChangeInstant is set (respawn). Without a
    /// WorldPhase (art sandboxes) SetDay can be called directly. Anything else that follows the
    /// phase (e.g. the Lightkeeper's lantern) listens to DayNightLook.Changed. Toggle "Is Day"
    /// in the Inspector to preview a phase.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class DayNightLook : MonoBehaviour
    {
        [Serializable]
        public struct Look
        {
            public Color lightColor;
            public float lightIntensity;
            public Vector3 lightAngles;
            [ColorUsage(false)] public Color fogColor;
            public float fogDensity;
            [ColorUsage(false)] public Color ambientSky;
            [ColorUsage(false)] public Color ambientEquator;
            [ColorUsage(false)] public Color ambientGround;
        }

        /// Raised when the phase changes: (isDay, instant). Instant = jump without fading.
        public static event Action<bool, bool> Changed;

        public static DayNightLook Instance { get; private set; }

        /// The current phase, or Day when the scene has no DayNightLook.
        public static bool CurrentIsDay => Instance == null || Instance.isDay;

        static readonly int NightId = Shader.PropertyToID("_SunSwitchNight");

        [SerializeField] Light sun;
        [SerializeField] bool isDay = true;
        [SerializeField] float fadeSeconds = 0.25f;

        [SerializeField] Look day = new Look
        {
            lightColor = new Color(1f, 0.95f, 0.84f),
            lightIntensity = 1.15f,
            lightAngles = new Vector3(50f, -30f, 0f),
            fogColor = new Color(0.86f, 0.89f, 0.90f),
            fogDensity = 0.006f,
            ambientSky = new Color(0.62f, 0.72f, 0.85f),
            ambientEquator = new Color(0.66f, 0.65f, 0.62f),
            ambientGround = new Color(0.42f, 0.38f, 0.34f),
        };

        [SerializeField] Look night = new Look
        {
            lightColor = new Color(0.62f, 0.68f, 1f),
            lightIntensity = 0.55f,
            lightAngles = new Vector3(60f, -10f, 0f),
            fogColor = new Color(0.20f, 0.23f, 0.42f),
            fogDensity = 0.008f,
            ambientSky = new Color(0.30f, 0.34f, 0.58f),
            ambientEquator = new Color(0.22f, 0.24f, 0.40f),
            ambientGround = new Color(0.12f, 0.12f, 0.22f),
        };

        float blend;        // 0 = Day, 1 = Night (what is shown now)
        float blendFrom;
        float fadeT = 1f;
        WorldPhase world;

        public bool IsDay => isDay;

        /// Switch the presentation to Day (true) or Night (false), fading over fadeSeconds.
        public void SetDay(bool toDay, bool instant = false)
        {
            if (toDay == isDay && !instant) return;
            isDay = toDay;
            blendFrom = blend;
            fadeT = instant || !Application.isPlaying || fadeSeconds <= 0f ? 1f : 0f;
            if (fadeT >= 1f) Apply(Target);
            Changed?.Invoke(isDay, fadeT >= 1f);
        }

        float Target => isDay ? 0f : 1f;

        void OnEnable()
        {
            if (Instance != null && Instance != this)
                Debug.LogWarning($"[Sun Switch] More than one DayNightLook ({Instance.name}, {name}); using {name}.", this);
            Instance = this;
            fadeT = 1f;
            Apply(Target);
            Changed?.Invoke(isDay, true);
            FollowWorldPhase();
        }

        // WorldPhase may not exist yet in OnEnable (scene load order), so try again here.
        void Start() => FollowWorldPhase();

        void OnDisable()
        {
            if (Instance == this) Instance = null;
            if (world != null)
            {
                world.OnPhaseChanged -= HandlePhaseChanged;
                world = null;
            }
        }

        void FollowWorldPhase()
        {
            if (!Application.isPlaying || world != null || WorldPhase.Instance == null) return;
            world = WorldPhase.Instance;
            world.OnPhaseChanged += HandlePhaseChanged;
            SetDay(world.Current == Phase.Day, instant: true);
        }

        void HandlePhaseChanged(Phase phase) => SetDay(phase == Phase.Day, world.LastChangeInstant);

        void OnValidate()
        {
            if (!isActiveAndEnabled) return;
            fadeT = 1f;
            Apply(Target);
            Changed?.Invoke(isDay, true);
        }

        void Update()
        {
            if (fadeT >= 1f) return;
            fadeT = Mathf.Min(1f, fadeT + Time.deltaTime / fadeSeconds);
            Apply(Mathf.Lerp(blendFrom, Target, Mathf.SmoothStep(0f, 1f, fadeT)));
        }

        void Apply(float t)
        {
            blend = t;
            Shader.SetGlobalFloat(NightId, t);

            if (sun != null)
            {
                sun.color = Color.Lerp(day.lightColor, night.lightColor, t);
                sun.intensity = Mathf.Lerp(day.lightIntensity, night.lightIntensity, t);
                sun.transform.rotation = Quaternion.Slerp(Quaternion.Euler(day.lightAngles), Quaternion.Euler(night.lightAngles), t);
            }

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = Color.Lerp(day.fogColor, night.fogColor, t);
            RenderSettings.fogDensity = Mathf.Lerp(day.fogDensity, night.fogDensity, t);

            // Sky ambient can't be re-baked every frame, so drive a three-colour ambient probe.
            var sky = Color.Lerp(day.ambientSky, night.ambientSky, t);
            var equator = Color.Lerp(day.ambientEquator, night.ambientEquator, t);
            var ground = Color.Lerp(day.ambientGround, night.ambientGround, t);
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = sky;
            RenderSettings.ambientEquatorColor = equator;
            RenderSettings.ambientGroundColor = ground;
            RenderSettings.ambientProbe = Trilight(sky, equator, ground);
        }

        static SphericalHarmonicsL2 Trilight(Color sky, Color equator, Color ground)
        {
            var sh = new SphericalHarmonicsL2();
            // Equator all round, plus the sky / ground difference from above / below.
            sh.AddAmbientLight(equator);
            sh.AddDirectionalLight(Vector3.up, (sky - equator) * 0.5f, 1f);
            sh.AddDirectionalLight(Vector3.down, (ground - equator) * 0.5f, 1f);
            return sh;
        }
    }
}
