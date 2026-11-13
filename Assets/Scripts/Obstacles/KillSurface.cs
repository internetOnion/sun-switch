using SunSwitch.Core;
using SunSwitch.Player;
using UnityEngine;

namespace SunSwitch.Obstacles
{
    /// <summary>
    /// Kills the player on touch (spikes, lava, spinning bar arms). Use a trigger collider.
    /// While its PhaseObject is a ghost the collider is off, so it can't kill.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class KillSurface : MonoBehaviour
    {
        void Reset() => GetComponent<Collider>().isTrigger = true;

        void OnTriggerEnter(Collider other) => TryKill(other);

        // Catches a player already standing inside when a phase switch turns this hazard on.
        void OnTriggerStay(Collider other) => TryKill(other);

        static void TryKill(Collider other)
        {
            if (other.TryGetComponent(out PlayerDeath death)) death.Kill(DeathCause.Hazard);
        }
    }
}
