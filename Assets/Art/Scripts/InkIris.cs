using SunSwitch.Core;
using SunSwitch.Player;
using UnityEngine;
using UnityEngine.UI;

namespace SunSwitch.Art
{
    /// <summary>
    /// Storybook iris wipe for deaths (on the HUD, behind the plates so lives stay readable).
    /// When the player dies, ink closes in on where they fell; it stays shut while the camera jumps
    /// back to the checkpoint, and opens again on the player once they respawn.
    ///
    /// Listens to GameEvents.OnPlayerDied / OnPlayerRespawned. On the Start screen it is always
    /// open, so a run that ended on a death (Game Over) never starts the next one in the dark.
    /// Uses M_InkIris (SunSwitch/UI/InkIris); the material is copied at runtime to animate it.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class InkIris : MonoBehaviour
    {
        [SerializeField] float closeSeconds = 0.32f;
        [SerializeField] float openSeconds = 0.45f;
        [SerializeField, Tooltip("Closed radius in screen heights; below 0 so not even a speck shows")] float closedRadius = -0.03f;

        static readonly int CenterId = Shader.PropertyToID("_Center");
        static readonly int RadiusId = Shader.PropertyToID("_Radius");
        static readonly int AspectId = Shader.PropertyToID("_Aspect");

        Image image;
        Material material;
        Transform player;
        float radius, from, to, t = 1f, duration = 1f;
        Vector2 center = new Vector2(0.5f, 0.5f);

        /// <summary>0 = open, 1 = fully closed.</summary>
        public float Closed => Mathf.InverseLerp(OpenRadius, closedRadius, radius);

        float OpenRadius
        {
            get
            {
                // Far enough to clear the furthest screen corner from the centre, plus the wobble.
                float aspect = Screen.height > 0 ? (float)Screen.width / Screen.height : 1.78f;
                float dx = Mathf.Max(center.x, 1f - center.x) * aspect, dy = Mathf.Max(center.y, 1f - center.y);
                return Mathf.Sqrt(dx * dx + dy * dy) * 1.15f + 0.05f;
            }
        }

        void Awake()
        {
            image = GetComponent<Image>();
            image.raycastTarget = false;
            if (image.material != null && image.material.shader.name == "SunSwitch/UI/InkIris")
            {
                material = new Material(image.material) { name = "InkIris (runtime)" };
                image.material = material;
            }
            radius = from = to = OpenRadius;
            Apply();
        }

        void OnDestroy()
        {
            if (material != null) Destroy(material);
        }

        void OnEnable()
        {
            GameEvents.OnPlayerDied += HandleDied;
            GameEvents.OnPlayerRespawned += HandleRespawned;
            GameEvents.OnGameStateChanged += HandleState;
        }

        void OnDisable()
        {
            GameEvents.OnPlayerDied -= HandleDied;
            GameEvents.OnPlayerRespawned -= HandleRespawned;
            GameEvents.OnGameStateChanged -= HandleState;
        }

        void HandleDied(DeathCause _) { center = PlayerOnScreen(); Animate(closedRadius, closeSeconds); }

        void HandleRespawned()
        {
            if (Closed <= 0.01f) return;   // a new run straight from the menu: nothing to open
            center = PlayerOnScreen();
            Animate(OpenRadius, openSeconds);
        }

        void HandleState(GameState state)
        {
            if (state == GameState.Menu) { radius = to = OpenRadius; t = 1f; Apply(); }
        }

        void Animate(float target, float seconds)
        {
            from = radius;
            to = target;
            duration = Mathf.Max(0.01f, seconds);
            t = 0f;
        }

        void Update()
        {
            if (t >= 1f) return;
            t = Mathf.Min(1f, t + Time.deltaTime / duration);
            if (to > from)
            {
                // Opening: follow the camera settling on the player, and keep "fully open" big
                // enough for wherever the centre is now.
                center = PlayerOnScreen();
                to = OpenRadius;
            }
            // Closing speeds up into the dark; opening starts fast and settles.
            float k = to < from ? t * t : 1f - (1f - t) * (1f - t);
            radius = Mathf.Lerp(from, to, k);
            Apply();
        }

        void Apply()
        {
            if (image != null) image.enabled = radius < OpenRadius - 0.001f;
            if (material == null) return;
            material.SetVector(CenterId, center);
            material.SetFloat(RadiusId, radius);
            material.SetFloat(AspectId, Screen.height > 0 ? (float)Screen.width / Screen.height : 1.78f);
        }

        Vector2 PlayerOnScreen()
        {
            if (player == null)
            {
                var p = FindAnyObjectByType<PlayerDeath>();
                if (p != null) player = p.transform;
            }
            var cam = Camera.main;
            if (player == null || cam == null) return new Vector2(0.5f, 0.5f);
            var v = cam.WorldToViewportPoint(player.position + Vector3.up * 0.9f);
            if (v.z < 0f) return new Vector2(0.5f, 0.5f);
            return new Vector2(Mathf.Clamp(v.x, 0.1f, 0.9f), Mathf.Clamp(v.y, 0.1f, 0.9f));
        }
    }
}
