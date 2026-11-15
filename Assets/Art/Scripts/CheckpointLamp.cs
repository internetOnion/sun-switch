using SunSwitch.Core;
using SunSwitch.Player;
using UnityEngine;

namespace SunSwitch.Art
{
    /// <summary>
    /// The checkpoint's lamp: shows the player which checkpoints exist, when one is reached, and
    /// which one they'll come back to.
    ///
    ///   Ember    not reached yet: a faint, slowly pulsing glow ("light me")
    ///   Reached  the moment it's touched: light flash, the lamp pops, a beam shoots up, a ring
    ///            rolls out across the platform and sparks rise (about 1 s)
    ///   Current  the respawn point: bright, gently breathing, lighting the platform
    ///   Kept     reached earlier but not the respawn point any more: lit, but dimmer
    ///
    /// Gold by Day, moon blue by Night, like the Lightkeeper's lantern (follows DayNightLook).
    /// Reads the gameplay side without changing it: Checkpoint switches its "Lit" indicator on
    /// when reached, RespawnManager.Current says which one is current, and
    /// GameEvents.OnCheckpointReached starts the burst.
    /// </summary>
    public class CheckpointLamp : MonoBehaviour
    {
        public enum State { Ember, Current, Kept }

        [SerializeField] Checkpoint checkpoint;
        [SerializeField, Tooltip("Switched on by Checkpoint once reached (holds the light)")] GameObject litIndicator;
        [SerializeField] Renderer lamp;
        [SerializeField] Light glowLight;
        [SerializeField] Transform beam;
        [SerializeField] Transform ring;

        [Header("Colour")]
        [SerializeField, ColorUsage(false, true)] Color dayGlow = new Color(2.2f, 1.45f, 0.45f);
        [SerializeField, ColorUsage(false, true)] Color nightGlow = new Color(0.5f, 0.75f, 1.6f);   // deeper than the lantern's, so it stays blue when bright

        [Header("Brightness (emission multiplier, light intensity)")]
        [SerializeField] Vector2 ember = new Vector2(0.5f, 0f);
        [SerializeField] Vector2 current = new Vector2(1.6f, 2.4f);
        [SerializeField] Vector2 kept = new Vector2(0.75f, 0.9f);
        [SerializeField] Vector2 flash = new Vector2(2.6f, 10f);

        [Header("Burst")]
        [SerializeField] float burstSeconds = 1.1f;
        [SerializeField] float beamHeight = 7f;
        [SerializeField] float ringRadius = 2.3f;
        [SerializeField] int sparks = 12;
        [SerializeField, Tooltip("Emission of the beam, ring and sparks; higher washes the colour out to white")] float fxGlow = 1.15f;

        static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        MaterialPropertyBlock block;
        float dayBlend = 1f, dayTarget = 1f;
        float burstT = -1f;
        Vector3 lampScale, beamScale, ringScale;
        Transform[] sparkObjects;
        Vector3[] sparkVelocity;
        Renderer[] fxRenderers;
        Material fxMaterial;

        public State Current { get; private set; }
        public bool Bursting => burstT >= 0f && burstT < burstSeconds;

        void Awake()
        {
            block = new MaterialPropertyBlock();
            if (lamp != null) lampScale = lamp.transform.localScale;
            if (beam != null) { beamScale = beam.localScale; beam.gameObject.SetActive(false); }
            if (ring != null) { ringScale = ring.localScale; ring.gameObject.SetActive(false); }
            BuildSparks();
        }

        void OnEnable()
        {
            GameEvents.OnCheckpointReached += HandleReached;
            DayNightLook.Changed += HandlePhase;
            dayTarget = dayBlend = DayNightLook.CurrentIsDay ? 1f : 0f;
        }

        void OnDisable()
        {
            GameEvents.OnCheckpointReached -= HandleReached;
            DayNightLook.Changed -= HandlePhase;
        }

        void OnDestroy()
        {
            if (fxMaterial != null) Destroy(fxMaterial);
        }

        void HandlePhase(bool isDay, bool instant)
        {
            dayTarget = isDay ? 1f : 0f;
            if (instant) dayBlend = dayTarget;
        }

        void HandleReached(Checkpoint reached)
        {
            if (reached != checkpoint) return;
            burstT = 0f;
            if (beam != null) beam.gameObject.SetActive(true);
            if (ring != null) ring.gameObject.SetActive(true);
            var origin = lamp != null ? lamp.transform.position : transform.position;
            for (int i = 0; i < sparkObjects.Length; i++)
            {
                sparkObjects[i].position = origin;
                sparkObjects[i].gameObject.SetActive(true);
                float a = (i + Random.value * 0.5f) / sparkObjects.Length * Mathf.PI * 2f;
                sparkVelocity[i] = new Vector3(Mathf.Cos(a) * 1.4f, 3.2f + Random.value * 1.6f, Mathf.Sin(a) * 1.4f);
            }
        }

        void Update()
        {
            dayBlend = Mathf.MoveTowards(dayBlend, dayTarget, Time.deltaTime / 0.25f);
            var glow = Color.Lerp(nightGlow, dayGlow, dayBlend);

            bool reached = litIndicator != null && litIndicator.activeInHierarchy;
            var respawn = RespawnManager.Instance;
            Current = !reached ? State.Ember : (respawn != null && respawn.Current == checkpoint ? State.Current : State.Kept);

            // Steady brightness for the state, breathing a little.
            Vector2 level = Current == State.Ember ? ember : Current == State.Current ? current : kept;
            float breathe = Current == State.Ember
                ? 0.7f + 0.3f * Mathf.Sin(Time.time * 2.2f)               // slow "light me" pulse
                : Current == State.Current ? 1f + 0.12f * Mathf.Sin(Time.time * 3.1f) : 1f;
            float emission = level.x * breathe, intensity = level.y * breathe;
            float pop = 1f;

            if (burstT >= 0f)
            {
                burstT += Time.deltaTime;
                float k = Mathf.Clamp01(burstT / burstSeconds);
                float fade = 1f - Mathf.SmoothStep(0f, 1f, k);
                emission = Mathf.Lerp(emission, flash.x, fade);
                intensity = Mathf.Lerp(intensity, flash.y, fade);
                pop = 1f + 0.45f * Mathf.Sin(Mathf.Min(1f, burstT / 0.35f) * Mathf.PI) ;
                AnimateBurst(k);
                if (k >= 1f) EndBurst();
            }

            if (lamp != null)
            {
                lamp.transform.localScale = lampScale * pop;
                Tint(lamp, glow, emission);
            }
            if (glowLight != null)
            {
                float max = Mathf.Max(glow.r, Mathf.Max(glow.g, glow.b), 0.001f);
                glowLight.color = new Color(glow.r / max, glow.g / max, glow.b / max);
                glowLight.intensity = intensity;
                glowLight.range = Current == State.Current || burstT >= 0f ? 6f : 4f;
            }
            if (burstT >= 0f && fxRenderers != null)
                foreach (var r in fxRenderers) if (r != null && r.gameObject.activeInHierarchy) Tint(r, glow, fxGlow);
        }

        void AnimateBurst(float k)
        {
            // Beam: shoots up in the first quarter, then thins out.
            if (beam != null)
            {
                float up = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(k / 0.25f));
                float thin = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((k - 0.25f) / 0.75f));
                float h = beamHeight * up;
                beam.localScale = new Vector3(beamScale.x * thin, Mathf.Max(0.001f, h), beamScale.z * thin);
                beam.localPosition = new Vector3(beam.localPosition.x, h * 0.5f, beam.localPosition.z);
            }
            // Ring: rolls out across the platform and flattens away.
            if (ring != null)
            {
                float grow = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(k / 0.7f));
                float flat = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((k - 0.45f) / 0.55f));
                float r = Mathf.Lerp(0.3f, ringRadius, grow);
                ring.localScale = new Vector3(ringScale.x * r, ringScale.y * flat, ringScale.z * r);
            }
            // Sparks: rise, slow down and shrink.
            for (int i = 0; i < sparkObjects.Length; i++)
            {
                var s = sparkObjects[i];
                sparkVelocity[i] += Vector3.down * 3.5f * Time.deltaTime;
                sparkVelocity[i] *= 1f - 1.2f * Time.deltaTime;
                s.position += sparkVelocity[i] * Time.deltaTime;
                s.localScale = Vector3.one * 0.55f * (1f - k);
                s.Rotate(160f * Time.deltaTime, 220f * Time.deltaTime, 0f);
            }
        }

        void EndBurst()
        {
            burstT = -1f;
            if (beam != null) beam.gameObject.SetActive(false);
            if (ring != null) ring.gameObject.SetActive(false);
            foreach (var s in sparkObjects) s.gameObject.SetActive(false);
        }

        void BuildSparks()
        {
            int n = Mathf.Max(0, sparks);
            sparkObjects = new Transform[n];
            sparkVelocity = new Vector3[n];
            var mesh = lamp != null ? lamp.GetComponent<MeshFilter>()?.sharedMesh : null;
            // The effects use the lamp's material without the ink outline: on small, fast pieces
            // the outline turns them into black specks.
            Material mat = null;
            if (lamp != null && lamp.sharedMaterial != null)
            {
                mat = new Material(lamp.sharedMaterial) { name = lamp.sharedMaterial.name + " (FX)" };
                if (mat.HasProperty("_OutlineWidth")) mat.SetFloat("_OutlineWidth", 0f);
                if (mat.HasProperty("_EdgeInkWidth")) mat.SetFloat("_EdgeInkWidth", 0f);
            }
            for (int i = 0; i < n; i++)
            {
                var go = new GameObject("Spark");
                go.transform.SetParent(transform, false);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var r = go.AddComponent<MeshRenderer>();
                r.sharedMaterial = mat;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                go.SetActive(false);
                sparkObjects[i] = go.transform;
            }
            var list = new System.Collections.Generic.List<Renderer>();
            if (beam != null) list.AddRange(beam.GetComponentsInChildren<Renderer>(true));
            if (ring != null) list.AddRange(ring.GetComponentsInChildren<Renderer>(true));
            if (mat != null) foreach (var r in list) r.sharedMaterial = mat;
            fxMaterial = mat;
            foreach (var s in sparkObjects) list.Add(s.GetComponent<Renderer>());
            fxRenderers = list.ToArray();
        }

        void Tint(Renderer r, Color glow, float emission)
        {
            r.GetPropertyBlock(block);
            block.SetColor(EmissionId, glow * emission);
            float max = Mathf.Max(glow.r, Mathf.Max(glow.g, glow.b), 0.001f);
            var tint = new Color(glow.r / max, glow.g / max, glow.b / max, 1f);
            // Unlit lamps stay creamy; lit ones take the phase colour.
            block.SetColor(BaseColorId, Color.Lerp(new Color(1f, 0.96f, 0.87f, 1f), tint, Mathf.Clamp01(emission / 1.2f)));
            r.SetPropertyBlock(block);
        }
    }
}
