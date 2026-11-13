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
        [SerializeField, Tooltip("Starting angle, so neighbouring bars can be out of step")]
        float startAngle;

        Rigidbody _rb;

        void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.isKinematic = true;   // moved by script, not physics
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
            transform.rotation *= Quaternion.AngleAxis(startAngle, axis.normalized);
            _rb.rotation = transform.rotation;   // physics pose isn't synced yet; FixedUpdate reads it
        }

        void FixedUpdate()
        {
            var step = Quaternion.AngleAxis(degreesPerSecond * Time.fixedDeltaTime, axis.normalized);
            _rb.MoveRotation(_rb.rotation * step);
        }
    }
}
