using UnityEngine;

namespace SunSwitch.Art
{
    /// <summary>
    /// A soft ink spot on the ground straight below the Lightkeeper, so the player can see where a
    /// jump will land. Raycasts down every frame and lays this quad on whatever solid surface it
    /// hits; the spot gets a little smaller and fainter the higher the player is.
    ///
    /// Ghost (inactive-phase) platforms have their colliders off, so the spot falls through them
    /// to whatever is below, or disappears over the void: "no spot, no ground". Triggers (kill
    /// planes, checkpoints, pads) are ignored.
    ///
    /// Lives on a child of the Lightkeeper prefab (LightkeeperBuilder) with a quad and
    /// M_LandingShadow (SunSwitch/LandingShadow). The colour follows Day / Night (DayNightLook).
    /// </summary>
    [RequireComponent(typeof(MeshRenderer))]
    public class LandingShadow : MonoBehaviour
    {
        [SerializeField, Tooltip("Spot diameter on the ground when standing (metres)")] float size = 0.95f;
        [SerializeField, Tooltip("Furthest drop that still shows a spot (metres)")] float maxDrop = 40f;
        [SerializeField, Tooltip("Height at which the spot reaches its smallest and faintest (metres)")] float fadeHeight = 8f;
        [SerializeField, Range(0f, 1f)] float smallestScale = 0.7f;
        [SerializeField, Tooltip("Opacity when on the ground, and at fadeHeight and above")] Vector2 opacity = new Vector2(0.55f, 0.32f);
        [SerializeField] Color dayInk = new Color(0.157f, 0.137f, 0.129f);
        [SerializeField] Color nightInk = new Color(0.02f, 0.02f, 0.07f);
        [SerializeField, Tooltip("Night ground is darker, so the spot is a bit stronger there")] float nightOpacityBoost = 1.5f;
        [SerializeField] LayerMask ground = Physics.DefaultRaycastLayers;

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        Transform owner;
        MeshRenderer spot;
        MaterialPropertyBlock block;

        /// <summary>True while the spot is on the ground below the player.</summary>
        public bool Showing => spot != null && spot.enabled;
        /// <summary>How far the player is above the spot (metres).</summary>
        public float Height { get; private set; }

        void Awake()
        {
            owner = transform.parent != null ? transform.parent : transform;
            spot = GetComponent<MeshRenderer>();
            spot.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            spot.receiveShadows = false;
            block = new MaterialPropertyBlock();
        }

        void LateUpdate()
        {
            // Start a little above the feet so a spot still shows when standing on a slope or step.
            var from = owner.position + Vector3.up * 0.3f;
            if (!Physics.Raycast(from, Vector3.down, out var hit, maxDrop + 0.3f, ground, QueryTriggerInteraction.Ignore)
                || hit.transform.IsChildOf(owner))
            {
                spot.enabled = false;
                return;
            }

            spot.enabled = true;
            Height = Mathf.Max(0f, owner.position.y - hit.point.y);
            float k = Mathf.Clamp01(Height / fadeHeight);

            transform.SetPositionAndRotation(hit.point + hit.normal * 0.02f, Quaternion.LookRotation(-hit.normal, owner.forward));
            transform.localScale = Vector3.one * size * Mathf.Lerp(1f, smallestScale, k);

            bool day = DayNightLook.Instance == null || DayNightLook.CurrentIsDay;
            var color = day ? dayInk : nightInk;
            color.a = Mathf.Clamp01(Mathf.Lerp(opacity.x, opacity.y, k) * (day ? 1f : nightOpacityBoost));
            spot.GetPropertyBlock(block);
            block.SetColor(BaseColorId, color);
            spot.SetPropertyBlock(block);
        }
    }
}
