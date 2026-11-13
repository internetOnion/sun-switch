using System.Collections.Generic;
using UnityEngine;

namespace SunSwitch.Core
{
    /// <summary>
    /// Makes an object exist only in its phase. Active: colliders on, normal materials.
    /// Inactive: colliders off, ghost material (or hidden if no ghost material is set).
    /// </summary>
    public class PhaseObject : MonoBehaviour
    {
        public static readonly List<PhaseObject> All = new List<PhaseObject>();

        [SerializeField] PhaseTag phaseTag = PhaseTag.Both;
        [SerializeField, Tooltip("Transparent material shown when inactive. Empty = hide the object instead.")]
        Material ghostMaterial;
        [SerializeField, Tooltip("Filled from children when the component is added (or press Reset)")]
        Renderer[] renderers;
        [SerializeField, Tooltip("Filled from children when the component is added (or press Reset)")]
        Collider[] colliders;

        public PhaseTag PhaseTag => phaseTag;
        public bool IsActive { get; private set; } = true;
        public IReadOnlyList<Collider> Colliders => colliders;

        Material[][] _original;
        Material[][] _ghost;
        WorldPhase _world;

        void Reset()
        {
            renderers = GetComponentsInChildren<Renderer>();
            colliders = GetComponentsInChildren<Collider>();
        }

        void Awake()
        {
            if (renderers == null || renderers.Length == 0) renderers = GetComponentsInChildren<Renderer>();
            if (colliders == null || colliders.Length == 0) colliders = GetComponentsInChildren<Collider>();

            _original = new Material[renderers.Length][];
            _ghost = new Material[renderers.Length][];
            for (int i = 0; i < renderers.Length; i++)
            {
                _original[i] = renderers[i] ? renderers[i].sharedMaterials : new Material[0];
                _ghost[i] = new Material[_original[i].Length];
                for (int j = 0; j < _ghost[i].Length; j++) _ghost[i][j] = ghostMaterial;
            }
        }

        void OnEnable()
        {
            All.Add(this);
            Subscribe();
        }

        void Start() => Subscribe();   // in case WorldPhase didn't exist yet in OnEnable

        void OnDisable()
        {
            All.Remove(this);
            if (_world != null)
            {
                _world.OnPhaseChanged -= Apply;
                _world = null;
            }
        }

        void Subscribe()
        {
            if (_world != null || WorldPhase.Instance == null) return;
            _world = WorldPhase.Instance;
            _world.OnPhaseChanged += Apply;
            Apply(_world.Current);
        }

        void Apply(Phase current)
        {
            IsActive = PhaseUtil.IsActive(phaseTag, current);

            foreach (var c in colliders)
                if (c) c.enabled = IsActive;

            for (int i = 0; i < renderers.Length; i++)
            {
                var r = renderers[i];
                if (!r) continue;

                if (IsActive)
                {
                    r.enabled = true;
                    r.sharedMaterials = _original[i];
                }
                else if (ghostMaterial)
                {
                    r.enabled = true;
                    r.sharedMaterials = _ghost[i];
                }
                else
                {
                    r.enabled = false;
                }
            }
        }
    }
}
