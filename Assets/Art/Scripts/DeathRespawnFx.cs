using SunSwitch.Core;
using SunSwitch.Player;
using UnityEngine;

namespace SunSwitch.Art
{
    /// <summary>
    /// What a death and a respawn look like on the Lightkeeper:
    ///   Death    the character bursts into ink scraps that fly out and fade, and disappears
    ///            (InkIris on the HUD closes in on the spot)
    ///   Respawn  the character reappears at the checkpoint inside a ring of sparkles that rise
    ///            in the phase colour (gold by Day, moon blue by Night)
    ///
    /// Listens to GameEvents.OnPlayerDied / OnPlayerRespawned. The scraps and sparkles are soft
    /// discs drawn with M_LandingShadow, facing the camera, pooled under a scene object so they
    /// stay where the death happened while the player keeps falling.
    /// Lives on a child of the Lightkeeper prefab (LightkeeperBuilder).
    /// </summary>
    public class DeathRespawnFx : MonoBehaviour
    {
        [SerializeField] Material discMaterial;
        [SerializeField] int scraps = 16;
        [SerializeField] float scrapSeconds = 0.55f;
        [SerializeField] int sparkles = 18;
        [SerializeField] float sparkleSeconds = 1.1f;
        [SerializeField] Color ink = new Color(0.157f, 0.137f, 0.129f);
        [SerializeField] Color daySparkle = new Color(1f, 0.82f, 0.38f);
        [SerializeField] Color nightSparkle = new Color(0.62f, 0.76f, 1f);

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        class Piece { public Transform t; public Renderer r; public Vector3 v; public float size; }

        Transform owner;
        Renderer[] body;
        Light[] lights;
        Piece[] scrapPool, sparklePool;
        MaterialPropertyBlock block;
        float scrapT = -1f, sparkleT = -1f;
        bool hidden;

        /// <summary>True while the character is hidden after a death.</summary>
        public bool Hidden => hidden;
        public bool Bursting => scrapT >= 0f;
        public bool Sparkling => sparkleT >= 0f;

        void Awake()
        {
            owner = transform.parent != null ? transform.parent : transform;
            block = new MaterialPropertyBlock();
            // Everything that draws the character, but not the landing shadow or this effect.
            body = System.Array.FindAll(owner.GetComponentsInChildren<Renderer>(true),
                r => r.GetComponentInParent<LandingShadow>() == null && !r.transform.IsChildOf(transform));
            lights = owner.GetComponentsInChildren<Light>(true);

            var holder = new GameObject("Lightkeeper FX").transform;   // a scene root: stays put when the player moves
            scrapPool = Pool(holder, "Scrap", scraps);
            sparklePool = Pool(holder, "Sparkle", sparkles);
        }

        Piece[] Pool(Transform holder, string name, int count)
        {
            var mesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
            var pool = new Piece[Mathf.Max(0, count)];
            for (int i = 0; i < pool.Length; i++)
            {
                var go = new GameObject(name);
                go.transform.SetParent(holder, false);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var r = go.AddComponent<MeshRenderer>();
                r.sharedMaterial = discMaterial;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
                go.SetActive(false);
                pool[i] = new Piece { t = go.transform, r = r };
            }
            return pool;
        }

        void OnEnable()
        {
            GameEvents.OnPlayerDied += HandleDied;
            GameEvents.OnPlayerRespawned += HandleRespawned;
        }

        void OnDisable()
        {
            GameEvents.OnPlayerDied -= HandleDied;
            GameEvents.OnPlayerRespawned -= HandleRespawned;
        }

        void HandleDied(DeathCause _)
        {
            var center = owner.position + Vector3.up * 0.9f;
            for (int i = 0; i < scrapPool.Length; i++)
            {
                var p = scrapPool[i];
                var dir = Random.onUnitSphere;
                dir.y = Mathf.Abs(dir.y) * 0.8f + 0.2f;
                p.t.position = center + dir * 0.25f;
                p.v = dir * Random.Range(2.5f, 4.5f);
                p.size = Random.Range(0.18f, 0.34f);
                p.t.gameObject.SetActive(true);
            }
            scrapT = 0f;
            SetBodyVisible(false);
        }

        void HandleRespawned()
        {
            bool cameBack = hidden;
            SetBodyVisible(true);
            if (!cameBack) return;   // a fresh run from the menu: no sparkle show
            var feet = owner.position;
            for (int i = 0; i < sparklePool.Length; i++)
            {
                var p = sparklePool[i];
                float a = (i + Random.value * 0.4f) / sparklePool.Length * Mathf.PI * 2f;
                // A ring wider than the character, so the sparkles show around it from the follow camera.
                p.t.position = feet + new Vector3(Mathf.Cos(a) * 0.85f, Random.Range(0.1f, 0.9f), Mathf.Sin(a) * 0.85f);
                p.v = new Vector3(Mathf.Cos(a) * 0.25f, Random.Range(1.8f, 3f), Mathf.Sin(a) * 0.25f);
                p.size = Random.Range(0.18f, 0.3f);
                p.t.gameObject.SetActive(true);
            }
            sparkleT = 0f;
        }

        void SetBodyVisible(bool visible)
        {
            hidden = !visible;
            foreach (var r in body) if (r != null) r.enabled = visible;
            foreach (var l in lights) if (l != null) l.enabled = visible;
        }

        void LateUpdate()
        {
            var cam = Camera.main;
            var facing = cam != null ? cam.transform.rotation : Quaternion.identity;
            if (scrapT >= 0f)
            {
                scrapT += Time.deltaTime;
                float k = Mathf.Clamp01(scrapT / scrapSeconds);
                foreach (var p in scrapPool)
                {
                    p.v *= 1f - 3f * Time.deltaTime;
                    p.v += Vector3.down * 2f * Time.deltaTime;
                    Draw(p, facing, p.size * (1f - 0.5f * k), ink, 0.95f * (1f - k * k));
                }
                if (k >= 1f) { scrapT = -1f; foreach (var p in scrapPool) p.t.gameObject.SetActive(false); }
            }
            if (sparkleT >= 0f)
            {
                sparkleT += Time.deltaTime;
                float k = Mathf.Clamp01(sparkleT / sparkleSeconds);
                bool day = DayNightLook.Instance == null || DayNightLook.CurrentIsDay;
                foreach (var p in sparklePool)
                {
                    p.v *= 1f - 1.2f * Time.deltaTime;
                    float twinkle = 0.75f + 0.25f * Mathf.Sin((sparkleT + p.size * 40f) * 18f);
                    Draw(p, facing, p.size * (1f - 0.4f * k), day ? daySparkle : nightSparkle, (1f - k * k) * twinkle);
                }
                if (k >= 1f) { sparkleT = -1f; foreach (var p in sparklePool) p.t.gameObject.SetActive(false); }
            }
        }

        void Draw(Piece p, Quaternion facing, float size, Color color, float alpha)
        {
            p.t.position += p.v * Time.deltaTime;
            p.t.rotation = facing;
            p.t.localScale = Vector3.one * size;
            color.a = Mathf.Clamp01(alpha);
            p.r.GetPropertyBlock(block);
            block.SetColor(BaseColorId, color);
            p.r.SetPropertyBlock(block);
        }
    }
}
