using System;
using UnityEngine;

namespace SunSwitch.Core
{
    /// <summary>
    /// Blocks a phase switch that would put the player inside solid geometry of the incoming phase.
    /// Put it on the player (needs the CharacterController). Plugs into WorldPhase.OverlapTest.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PhaseOverlapChecker : MonoBehaviour
    {
        [SerializeField, Tooltip("Shrinks the test capsule so just touching a surface doesn't block the switch")]
        float skin = 0.05f;

        CharacterController _cc;
        CapsuleCollider _probe;
        Func<Phase, bool> _test;

        void Awake()
        {
            _cc = GetComponent<CharacterController>();
            _test = WouldOverlap;

            // Disabled trigger capsule used only for the penetration test.
            var go = new GameObject("PhaseOverlapProbe") { layer = 2 };   // Ignore Raycast
            go.transform.SetParent(transform, false);
            _probe = go.AddComponent<CapsuleCollider>();
            _probe.isTrigger = true;
            _probe.direction = 1;   // Y axis
            _probe.enabled = false;
        }

        void Start()
        {
            if (WorldPhase.Instance != null) WorldPhase.Instance.OverlapTest = _test;
            else Debug.LogWarning("PhaseOverlapChecker: no WorldPhase in the scene.", this);
        }

        void OnDestroy()
        {
            var world = WorldPhase.Instance;
            if (world != null && world.OverlapTest == _test) world.OverlapTest = null;
        }

        public bool WouldOverlap(Phase incoming)
        {
            float radius = Mathf.Max(0.01f, _cc.radius - skin);
            float height = Mathf.Max(radius * 2f, _cc.height - skin * 2f);
            Vector3 center = transform.TransformPoint(_cc.center);

            _probe.radius = radius;
            _probe.height = height;
            _probe.center = Vector3.zero;
            var query = new Bounds(center, new Vector3(radius * 2f, height, radius * 2f));

            _probe.enabled = true;
            try
            {
                foreach (var po in PhaseObject.All)
                {
                    // Only objects that are a ghost now and become solid after the switch.
                    if (po.PhaseTag == PhaseTag.Both || po.IsActive || !PhaseUtil.IsActive(po.PhaseTag, incoming))
                        continue;

                    foreach (var col in po.Colliders)
                    {
                        if (col == null || col.isTrigger) continue;   // hazards don't block, they kill

                        bool wasEnabled = col.enabled;
                        col.enabled = true;   // disabled colliders report empty bounds
                        bool hit = col.bounds.Intersects(query) &&
                                   Physics.ComputePenetration(
                                       _probe, center, Quaternion.identity,
                                       col, col.transform.position, col.transform.rotation,
                                       out _, out float distance) &&
                                   distance > 0f;
                        col.enabled = wasEnabled;

                        if (hit) return true;
                    }
                }
            }
            finally
            {
                _probe.enabled = false;
            }

            return false;
        }
    }
}
