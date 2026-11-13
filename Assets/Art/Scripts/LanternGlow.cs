using UnityEngine;

namespace SunSwitch.Art
{
    /// <summary>
    /// The Lightkeeper's lantern: glows sun gold in Day and moon blue in Night, and lights the
    /// ground around the player a little (helps reading ghost platforms at night).
    ///
    /// The phase system calls SetDay(bool) when the world switches; the colour cross-fades over
    /// the same 0.25 s as the world lighting (GDD §2). Toggle "Is Day" in the Inspector to preview.
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

        MaterialPropertyBlock block;
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
            fadeT = 1f;
            Apply(Target);
        }

        void OnValidate()
        {
            fadeT = 1f;
            Apply(Target);
        }

        void Update()
        {
            if (fadeT >= 1f) return;
            fadeT = Mathf.Min(1f, fadeT + Time.deltaTime / fadeSeconds);
            Apply(Color.Lerp(from, Target, Mathf.SmoothStep(0f, 1f, fadeT)));
        }

        void Apply(Color glow)
        {
            current = glow;
            if (glass != null)
            {
                block ??= new MaterialPropertyBlock();
                glass.GetPropertyBlock(block);
                block.SetColor(EmissionId, glow);
                // Tint the glass itself too, so it reads as coloured light even in shadow.
                float max = Mathf.Max(glow.r, Mathf.Max(glow.g, glow.b), 0.001f);
                block.SetColor(BaseColorId, new Color(glow.r / max, glow.g / max, glow.b / max, 1f));
                glass.SetPropertyBlock(block);
            }
            if (glowLight != null)
            {
                float max = Mathf.Max(glow.r, Mathf.Max(glow.g, glow.b), 0.001f);
                glowLight.color = new Color(glow.r / max, glow.g / max, glow.b / max);
                glowLight.intensity = lightIntensity;
            }
        }
    }
}
