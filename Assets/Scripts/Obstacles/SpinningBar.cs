using UnityEngine;

namespace SunSwitch.Obstacles
{
    /// <summary>
    /// Rotating bar. Put KillSurface (trigger) on its arms. Keeps spinning while a ghost
    /// so its timing stays predictable.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class SpinningBar : MonoBehaviour
    {
        [SerializeField] float degreesPerSecond = 90f;
        [SerializeField, Tooltip("Local axis to spin around")]
        Vector3 axis = Vector3.up;

        Rigidbody _rb;

        void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.isKinematic = true;   // moved by script, not physics
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
        }

        void FixedUpdate()
        {
            var step = Quaternion.AngleAxis(degreesPerSecond * Time.fixedDeltaTime, axis.normalized);
            _rb.MoveRotation(_rb.rotation * step);
        }
    }
}
