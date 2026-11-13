using System;
using UnityEngine;

namespace SunSwitch.Art
{
    /// <summary>
    /// Marks a trigger collider that kills the player on contact (spinning bars, spike beds).
    ///
    /// This only reports the hit: the player / respawn system (P2) subscribes to PlayerKilled and
    /// handles death, lives and respawn. The player needs the "Player" tag. Moving kill zones
    /// carry a kinematic Rigidbody so their trigger events fire while they move.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class KillZone : MonoBehaviour
    {
        /// Raised when a collider tagged playerTag enters this zone: (zone, player collider).
        public static event Action<KillZone, Collider> PlayerKilled;

        [SerializeField] string playerTag = "Player";

        void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag(playerTag)) PlayerKilled?.Invoke(this, other);
        }
    }
}
