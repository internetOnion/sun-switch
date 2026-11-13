using UnityEngine;

namespace SunSwitch.Art
{
    /// <summary>
    /// Spins its GameObject around its local up axis at a steady speed (spinning kill bars).
    /// Uses the kinematic Rigidbody when there is one, so triggers and physics see the motion.
    /// </summary>
    public class Spinner : MonoBehaviour
    {
        [Tooltip("Degrees per second; negative spins the other way.")]
        [SerializeField] float degreesPerSecond = 90f;
        [Tooltip("Starting angle, so neighbouring bars can be out of step.")]
        [SerializeField] float startAngle;

        Rigidbody body;
        Quaternion baseRotation;
        float angle;

        public float DegreesPerSecond { get => degreesPerSecond; set => degreesPerSecond = value; }

        void Awake()
        {
            body = GetComponent<Rigidbody>();
            baseRotation = transform.localRotation;
            angle = startAngle;
            transform.localRotation = baseRotation * Quaternion.Euler(0f, angle, 0f);
        }

        void FixedUpdate()
        {
            angle = Mathf.Repeat(angle + degreesPerSecond * Time.fixedDeltaTime, 360f);
            var local = baseRotation * Quaternion.Euler(0f, angle, 0f);
            if (body != null && body.isKinematic)
            {
                var parent = transform.parent;
                body.MoveRotation(parent != null ? parent.rotation * local : local);
            }
            else
            {
                transform.localRotation = local;
            }
        }
    }
}
