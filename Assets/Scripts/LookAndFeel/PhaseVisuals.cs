using System;
using System.Collections;
using SunSwitch.Core;
using UnityEngine;

namespace SunSwitch.LookAndFeel
{
    [Serializable]
    public class PhaseLook
    {
        [Header("Directional light")]
        public Color lightColor = Color.white;
        public float lightIntensity = 1f;
        public Vector3 lightRotation = new Vector3(50f, -30f, 0f);

        [Header("Skybox")]
        public Color skyTint = Color.white;
        public float skyExposure = 1f;

        [Header("Fog and ambient")]
        public Color fogColor = Color.gray;
        public float fogDensity = 0.01f;
        public Color ambientColor = Color.gray;
    }

    /// <summary>Cross-fades light, sky, fog and ambient colour when the phase changes.</summary>
    public class PhaseVisuals : MonoBehaviour
    {
        [SerializeField, Tooltip("Empty = the scene's Sun Source (Lighting window)")]
        Light sun;
        [SerializeField, Min(0f)] float fadeTime = 0.25f;

        [SerializeField] PhaseLook day = new PhaseLook
        {
            lightColor = new Color(1f, 0.92f, 0.8f),
            lightIntensity = 1.2f,
            lightRotation = new Vector3(50f, -30f, 0f),
            skyTint = new Color(1f, 0.85f, 0.6f),
            skyExposure = 1.3f,
            fogColor = new Color(0.95f, 0.8f, 0.6f),
            fogDensity = 0.01f,
            ambientColor = new Color(0.75f, 0.65f, 0.5f),
        };

        [SerializeField] PhaseLook night = new PhaseLook
        {
            lightColor = new Color(0.55f, 0.6f, 1f),
            lightIntensity = 0.35f,
            lightRotation = new Vector3(40f, 150f, 0f),
            skyTint = new Color(0.25f, 0.25f, 0.55f),
            skyExposure = 0.35f,
            fogColor = new Color(0.12f, 0.11f, 0.25f),
            fogDensity = 0.015f,
            ambientColor = new Color(0.2f, 0.22f, 0.4f),
        };

        [Header("Skybox shader properties")]
        [SerializeField, Tooltip("_SkyTint for Skybox/Procedural, _Tint for 6 Sided / Cubemap")]
        string skyTintProperty = "_SkyTint";
        [SerializeField] string skyExposureProperty = "_Exposure";

        Material _sky;
        WorldPhase _world;
        Coroutine _fade;

        void Awake()
        {
            if (sun == null) sun = RenderSettings.sun;
            if (RenderSettings.skybox != null)
            {
                // Work on a copy so the material asset on disk never changes.
                _sky = new Material(RenderSettings.skybox);
                RenderSettings.skybox = _sky;
            }
        }

        void OnEnable() => Subscribe();
        void Start() => Subscribe();

        void OnDisable()
        {
            if (_world != null)
            {
                _world.OnPhaseChanged -= HandlePhaseChanged;
                _world = null;
            }
        }

        void Subscribe()
        {
            if (_world != null || WorldPhase.Instance == null) return;
            _world = WorldPhase.Instance;
            _world.OnPhaseChanged += HandlePhaseChanged;
            var target = LookFor(_world.Current);
            Blend(target, target, 1f);
        }

        PhaseLook LookFor(Phase phase) => phase == Phase.Day ? day : night;

        void HandlePhaseChanged(Phase phase)
        {
            var target = LookFor(phase);
            if (_fade != null) StopCoroutine(_fade);

            if (fadeTime <= 0f || _world.LastChangeInstant)
                Blend(target, target, 1f);
            else
                _fade = StartCoroutine(Fade(Capture(), target));
        }

        IEnumerator Fade(PhaseLook from, PhaseLook to)
        {
            for (float t = 0f; t < fadeTime; t += Time.unscaledDeltaTime)
            {
                Blend(from, to, Mathf.SmoothStep(0f, 1f, t / fadeTime));
                yield return null;
            }
            Blend(to, to, 1f);
            _fade = null;
        }

        PhaseLook Capture()
        {
            var look = new PhaseLook
            {
                fogColor = RenderSettings.fogColor,
                fogDensity = RenderSettings.fogDensity,
                ambientColor = RenderSettings.ambientLight,
            };
            if (sun != null)
            {
                look.lightColor = sun.color;
                look.lightIntensity = sun.intensity;
                look.lightRotation = sun.transform.eulerAngles;
            }
            if (_sky != null)
            {
                if (_sky.HasProperty(skyTintProperty)) look.skyTint = _sky.GetColor(skyTintProperty);
                if (_sky.HasProperty(skyExposureProperty)) look.skyExposure = _sky.GetFloat(skyExposureProperty);
            }
            return look;
        }

        void Blend(PhaseLook a, PhaseLook b, float t)
        {
            if (sun != null)
            {
                sun.color = Color.Lerp(a.lightColor, b.lightColor, t);
                sun.intensity = Mathf.Lerp(a.lightIntensity, b.lightIntensity, t);
                sun.transform.rotation = Quaternion.Slerp(Quaternion.Euler(a.lightRotation), Quaternion.Euler(b.lightRotation), t);
            }

            if (_sky != null)
            {
                if (_sky.HasProperty(skyTintProperty))
                    _sky.SetColor(skyTintProperty, Color.Lerp(a.skyTint, b.skyTint, t));
                if (_sky.HasProperty(skyExposureProperty))
                    _sky.SetFloat(skyExposureProperty, Mathf.Lerp(a.skyExposure, b.skyExposure, t));
            }

            RenderSettings.fogColor = Color.Lerp(a.fogColor, b.fogColor, t);
            RenderSettings.fogDensity = Mathf.Lerp(a.fogDensity, b.fogDensity, t);
            RenderSettings.ambientLight = Color.Lerp(a.ambientColor, b.ambientColor, t);   // used when Environment Lighting Source = Color
        }
    }
}
