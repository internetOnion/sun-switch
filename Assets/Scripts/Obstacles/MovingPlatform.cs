using UnityEngine;

namespace SunSwitch.Obstacles
{
    /// <summary>
    /// Moves back and forth between two points with a pause at each end.
    /// Keeps moving while a ghost so its timing stays predictable.
    /// </summary>
    [DefaultExecutionOrder(-20)]   // move before PlatformRider and the player
    public class MovingPlatform : MonoBehaviour
    {
        [SerializeField, Tooltip("End points. Can be children; their start positions are cached.")]
        Transform pointA, pointB;
        [SerializeField, Min(0.01f), Tooltip("m/s")] float speed = 2f;
        [SerializeField, Min(0f), Tooltip("Pause at each end, seconds")] float waitTime = 0.5f;
        [SerializeField, Range(0f, 1f), Tooltip("Where in the cycle to start, to de-sync platforms")] float startOffset;

        /// <summary>How far the platform moved this frame. Read by PlatformRider.</summary>
        public Vector3 DeltaPosition { get; private set; }

        Vector3 _a, _b;
        float _travel, _cycle, _t;

        void Awake()
        {
            _a = pointA ? pointA.position : transform.position;
            _b = pointB ? pointB.position : transform.position;
            _travel = Vector3.Distance(_a, _b) / speed;
            _cycle = 2f * (_travel + waitTime);
            _t = startOffset * _cycle;
            transform.position = PositionAt(_t);
        }

        // shortcut: moves a collider without a Rigidbody (fine for a few dozen platforms); add a kinematic Rigidbody if physics cost shows up.
        void Update()
        {
            _t = (_t + Time.deltaTime) % _cycle;
            Vector3 next = PositionAt(_t);
            DeltaPosition = next - transform.position;
            transform.position = next;
        }

        Vector3 PositionAt(float t)
        {
            if (_travel <= 0f) return _a;
            if (t < waitTime) return _a;
            t -= waitTime;
            if (t < _travel) return Vector3.Lerp(_a, _b, Mathf.SmoothStep(0f, 1f, t / _travel));
            t -= _travel;
            if (t < waitTime) return _b;
            t -= waitTime;
            return Vector3.Lerp(_b, _a, Mathf.SmoothStep(0f, 1f, t / _travel));
        }

        void OnDrawGizmos()
        {
            if (!pointA || !pointB) return;
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(Application.isPlaying ? _a : pointA.position, Application.isPlaying ? _b : pointB.position);
        }
    }
}
