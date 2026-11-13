using SunSwitch.Obstacles;
using UnityEngine;

namespace SunSwitch.Player
{
    /// <summary>Carries the player along with the MovingPlatform they stand on. Put it on the player.</summary>
    [DefaultExecutionOrder(-10)]   // after MovingPlatform moves, before LightkeeperController moves the player
    [RequireComponent(typeof(CharacterController))]
    public class PlatformRider : MonoBehaviour
    {
        CharacterController _cc;
        MovingPlatform _platform;

        void Awake() => _cc = GetComponent<CharacterController>();

        void Update()
        {
            if (_platform != null && _cc.enabled) _cc.Move(_platform.DeltaPosition);
            _platform = null;   // set again below by this frame's ground hit; cleared when airborne or the platform turns ghost
        }

        void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (hit.normal.y > 0.5f) _platform = hit.collider.GetComponentInParent<MovingPlatform>();
        }
    }
}
