using UnityEngine;
using UnityEngine.InputSystem;

namespace SunSwitch.Core
{
    /// <summary>Reads the phase switch button and asks WorldPhase to switch. Put it on the player.</summary>
    public class PhaseSwitchInput : MonoBehaviour
    {
        [SerializeField, Tooltip("Right Mouse by default. Add a Gamepad Left Shoulder binding in the Inspector.")]
        InputAction switchAction = new InputAction("PhaseSwitch", InputActionType.Button, "<Mouse>/rightButton");

        void OnEnable()
        {
            switchAction.performed += OnSwitch;
            switchAction.Enable();
        }

        void OnDisable()
        {
            switchAction.performed -= OnSwitch;
            switchAction.Disable();
        }

        void OnSwitch(InputAction.CallbackContext _)
        {
            if (WorldPhase.Instance != null) WorldPhase.Instance.TrySwitch();
        }
    }
}
