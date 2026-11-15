using SunSwitch.Core;
using SunSwitch.Player;
using UnityEngine;

namespace SunSwitch.Art
{
    /// <summary>
    /// An area of the level that teaches something: while the player is inside it, the HUD's
    /// HintPanel shows its message at the bottom of the screen ("[Right Mouse] switch Day / Night…").
    ///
    /// A hint is done once the player walks out of the area alive, and isn't shown again. Leaving
    /// by falling out of the bottom or dying doesn't count, so the lesson comes back after the
    /// respawn. Words in [brackets] are drawn as keys.
    ///
    /// No collider: the box is checked against the player's position every frame, so teleports
    /// (respawns) are picked up too. Gizmo in the Scene view shows the box.
    /// </summary>
    public class HintZone : MonoBehaviour
    {
        [SerializeField, TextArea(2, 3)] string message = "[Right Mouse] switch Day / Night";
        [SerializeField, Tooltip("Size of the box around this transform (local, metres)")] Vector3 size = new Vector3(6f, 4f, 6f);

        /// <summary>The zone the player is in right now, if its hint isn't done yet.</summary>
        public static HintZone Active { get; private set; }

        public string Message => message;
        public bool Done { get; private set; }

        static Transform player;
        bool inside;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Active = null; player = null; }

        void OnEnable() => GameEvents.OnPlayerDied += HandleDied;

        void OnDisable()
        {
            GameEvents.OnPlayerDied -= HandleDied;
            Leave(false);
        }

        void HandleDied(DeathCause _) => Leave(false);

        void Update()
        {
            if (Done) return;
            if (player == null)
            {
                var p = FindAnyObjectByType<PlayerDeath>();
                if (p == null) return;
                player = p.transform;
            }

            // Chest height, so standing on the platform under the box counts.
            var local = transform.InverseTransformPoint(player.position + Vector3.up * 0.9f);
            var half = size * 0.5f;
            bool now = Mathf.Abs(local.x) <= half.x && Mathf.Abs(local.y) <= half.y && Mathf.Abs(local.z) <= half.z;
            if (now == inside) return;

            if (now) { inside = true; Active = this; }
            else Leave(local.y >= -half.y);   // walked out (done), or fell out of the bottom (not done)
        }

        void Leave(bool done)
        {
            if (!inside) return;
            inside = false;
            if (done) Done = true;
            if (Active == this) Active = null;
        }

        void OnDrawGizmos()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(0.95f, 0.75f, 0.3f, 0.12f);
            Gizmos.DrawCube(Vector3.zero, size);
            Gizmos.color = new Color(0.95f, 0.75f, 0.3f, 0.8f);
            Gizmos.DrawWireCube(Vector3.zero, size);
        }
    }
}
