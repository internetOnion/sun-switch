using System.Collections.Generic;
using SunSwitch.Core;
using UnityEngine;

namespace SunSwitch.Art
{
    /// <summary>
    /// Warns that an automatic flip (AutoCycleZone) is coming, on the platforms themselves, where
    /// the player is looking. In the last moments before each flip:
    ///   - platforms about to appear fill in: their ghost gets more solid, beat by beat
    ///   - platforms about to vanish blink, faster as the flip gets close
    /// so "it's blinking" means "jump now": you take off before your platform goes and land after
    /// the next one comes. The Lightkeeper's lantern pulses on the same beat (LanternGlow).
    ///
    /// Listens to GameEvents.OnAutoCycleTick, so it only does anything inside an auto-cycle zone.
    /// Works through MaterialPropertyBlocks and clears them the moment the phase changes, so
    /// PhaseObject keeps full control of materials and colliders. Only phased (Day / Night)
    /// objects are touched; shards and Both objects are left alone.
    /// </summary>
    public class PhaseFlipCue : MonoBehaviour
    {
        [SerializeField, Tooltip("Seconds before the flip the warning starts; about one jump's air time")] float warnSeconds = 0.6f;
        [SerializeField, Range(0f, 1f), Tooltip("Opacity the ghosts of incoming platforms reach at the flip")] float incomingOpacity = 0.75f;
        [SerializeField, Tooltip("Blinks per second at the start and at the end of the warning")] Vector2 blinkRate = new Vector2(5f, 12f);
        [SerializeField, Range(0f, 1f), Tooltip("How far vanishing platforms fade toward white on a blink")] float blinkStrength = 0.7f;

        /// <summary>0 = no warning, rising to 1 at the flip. Shared with LanternGlow.</summary>
        public static float Warning { get; private set; }

        /// <summary>The blink beat, 1 at the start of each blink and 0 between. Shared with LanternGlow.</summary>
        public static float Blink { get; private set; }

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
        static readonly int HatchAlphaId = Shader.PropertyToID("_HatchAlpha");

        class Entry { public PhaseObject phase; public Renderer[] renderers; }

        readonly Dictionary<PhaseObject, Entry> entries = new Dictionary<PhaseObject, Entry>();
        readonly List<Renderer> tinted = new List<Renderer>();
        MaterialPropertyBlock block;
        WorldPhase world;
        float left = -1f, clock;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Warning = 0f; Blink = 0f; }

        void Awake() => block = new MaterialPropertyBlock();

        void OnEnable() => GameEvents.OnAutoCycleTick += HandleTick;

        void OnDisable()
        {
            GameEvents.OnAutoCycleTick -= HandleTick;
            if (world != null) world.OnPhaseChanged -= HandlePhase;
            world = null;
            Clear();
        }

        void HandleTick(float secondsLeft) => left = secondsLeft;

        // The flip itself: PhaseObject swaps materials now, so drop every override straight away.
        void HandlePhase(Phase _) { left = -1f; Clear(); }

        void LateUpdate()
        {
            if (world == null && WorldPhase.Instance != null)
            {
                world = WorldPhase.Instance;
                world.OnPhaseChanged += HandlePhase;
            }

            float k = left > 0f && left < warnSeconds ? 1f - left / warnSeconds : 0f;
            if (k <= 0f)
            {
                if (tinted.Count > 0 || Warning > 0f) Clear();
                return;
            }
            Warning = k;

            // Blink faster as the flip nears; the clock keeps the beat continuous.
            clock += Time.deltaTime * Mathf.Lerp(blinkRate.x, blinkRate.y, k);
            float blink = 0.5f + 0.5f * Mathf.Cos(clock * Mathf.PI * 2f);   // 1 at the start of each blink
            Blink = blink;

            foreach (var phase in PhaseObject.All)
            {
                if (phase == null || phase.PhaseTag == PhaseTag.Both) continue;
                var entry = Get(phase);
                if (entry == null) continue;
                foreach (var r in entry.renderers)
                {
                    if (r == null || !r.enabled) continue;
                    var mat = r.sharedMaterial;
                    if (mat == null || !mat.HasProperty(BaseColorId)) continue;
                    var baseColor = mat.GetColor(BaseColorId);
                    r.GetPropertyBlock(block);
                    if (phase.IsActive)
                    {
                        // About to vanish: blink toward a pale flash.
                        float f = blinkStrength * blink * Mathf.Lerp(0.5f, 1f, k);
                        block.SetColor(BaseColorId, Color.Lerp(baseColor, Color.white, f));
                        if (mat.HasProperty(EmissionId)) block.SetColor(EmissionId, baseColor * (0.6f * f));
                    }
                    else
                    {
                        // About to appear: the ghost fills in, pulsing on the same beat.
                        float a = Mathf.Lerp(baseColor.a, incomingOpacity, k * (0.75f + 0.25f * blink));
                        block.SetColor(BaseColorId, new Color(baseColor.r, baseColor.g, baseColor.b, a));
                        if (mat.HasProperty(HatchAlphaId)) block.SetFloat(HatchAlphaId, Mathf.Lerp(mat.GetFloat(HatchAlphaId), 0.8f, k));
                    }
                    r.SetPropertyBlock(block);
                    if (!tinted.Contains(r)) tinted.Add(r);
                }
            }
        }

        Entry Get(PhaseObject phase)
        {
            if (entries.TryGetValue(phase, out var entry)) return entry;
            entry = phase.GetComponentInChildren<Shard>(true) != null ? null   // shards have their own look
                : new Entry { phase = phase, renderers = phase.GetComponentsInChildren<Renderer>(true) };
            entries[phase] = entry;
            return entry;
        }

        void Clear()
        {
            Warning = 0f;
            Blink = 0f;
            clock = 0f;
            foreach (var r in tinted)
                if (r != null) r.SetPropertyBlock(null);
            tinted.Clear();
        }
    }
}
