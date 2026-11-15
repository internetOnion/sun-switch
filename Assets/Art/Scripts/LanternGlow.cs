using UnityEngine;

namespace SunSwitch.Art
{
    /// <summary>
    /// The Lightkeeper's lantern: glows sun gold in Day and moon blue in Night, and lights the
    /// ground around the player a little (helps reading ghost platforms at night).
    ///
    /// Follows DayNightLook automatically (its Changed event); SetDay(bool) also works on its own.
    /// In an auto-cycle zone it pulses brighter on PhaseFlipCue's beat just before each flip.
    /// The colour cross-fades over the same 0.25 s as the world lighting (GDD §2).
    /// Toggle "Is Day" in the Inspector to preview.
    /// </summary>
    [ExecuteAlways]
    public class LanternGlow : MonoBehaviour
    {
        [SerializeField] Renderer glass;
        [SerializeField] Light glowLight;

        [SerializeField] bool isDay = true;
        [SerializeField, ColorUsage(false, true)] Color dayGlow = new Color(2.2f, 1.45f, 0.45f);   // sun gold
        [SerializeField, ColorUsage(false, true)] Color nightGlow = new Color(0.75f, 1.05f, 2.2f); // moon blue
        [SerializeField] float lightIntensity = 1.2f;
        [SerializeField] float fadeSeconds = 0.25f;

        static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField, Tooltip("Extra brightness at the peak of a flip-warning pulse")] float flipPulse = 1.2f;

        MaterialPropertyBlock block;
        bool pulsing;
        Color current;
        Color from;
        float fadeT = 1f;

        public bool IsDay => isDay;

        public void SetDay(bool day)
        {
            if (day == isDay) return;
            isDay = day;
            from = current;
            fadeT = Application.isPlaying && fadeSeconds > 0f ? 0f : 1f;
            if (fadeT >= 1f) Apply(Target);
        }

        Color Target => isDay ? dayGlow : nightGlow;

        void OnEnable()
        {
            DayNightLook.Changed += OnPhaseChanged;
            if (DayNightLook.Instance != null) isDay = DayNightLook.CurrentIsDay;
            fadeT = 1f;
            Apply(Target);
        }

        void OnDisable()
        {
            DayNightLook.Changed -= OnPhaseChanged;
        }

        void OnPhaseChanged(bool day, bool instant)
        {
            if (instant)
            {
                isDay = day;
                fadeT = 1f;
                Apply(Target);
            }
            else
            {
                SetDay(day);
            }
        }

        void OnValidate()
        {
            fadeT = 1f;
            Apply(Target);
        }

        void Update()
        {
            float pulse = Application.isPlaying ? PhaseFlipCue.Warning * PhaseFlipCue.Blink : 0f;
            bool fading = fadeT < 1f;
            if (!fading && pulse <= 0f && !pulsing) return;
            if (fading)
            {
                fadeT = Mathf.Min(1f, fadeT + Time.deltaTime / fadeSeconds);
                current = Color.Lerp(from, Target, Mathf.SmoothStep(0f, 1f, fadeT));
            }
            pulsing = pulse > 0f;
            Draw(current, 1f + flipPulse * pulse);
        }

        void Apply(Color glow)
        {
            current = glow;
            Draw(glow, 1f);
        }

        // boost brightens the glow and the light without changing the phase colour.
        void Draw(Color glow, float boost)
        {
            if (glass != null)
            {
                block ??= new MaterialPropertyBlock();
                glass.GetPropertyBlock(block);
                block.SetColor(EmissionId, glow * boost);
                // Tint the glass itself too, so it reads as coloured light even in shadow.
                float max = Mathf.Max(glow.r, Mathf.Max(glow.g, glow.b), 0.001f);
                block.SetColor(BaseColorId, new Color(glow.r / max, glow.g / max, glow.b / max, 1f));
                glass.SetPropertyBlock(block);
            }
            if (glowLight != null)
            {
                float max = Mathf.Max(glow.r, Mathf.Max(glow.g, glow.b), 0.001f);
                glowLight.color = new Color(glow.r / max, glow.g / max, glow.b / max);
                glowLight.intensity = lightIntensity * boost;
            }
        }
    }
}
