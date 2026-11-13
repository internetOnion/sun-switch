using SunSwitch.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SunSwitch.Art
{
    /// <summary>
    /// Sandbox-only helper: press the key to flip between Day and Night without the player.
    /// Goes through WorldPhase when the scene has one (so platforms, ghosts and the sky all
    /// switch), otherwise flips only DayNightLook. Don't add this to gameplay scenes.
    /// </summary>
    public class DayNightDebugToggle : MonoBehaviour
    {
        [SerializeField] Key key = Key.T;

        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || !keyboard[key].wasPressedThisFrame) return;

            if (WorldPhase.Instance != null)
                WorldPhase.Instance.ForceSet(PhaseUtil.Opposite(WorldPhase.Instance.Current));
            else if (DayNightLook.Instance != null)
                DayNightLook.Instance.SetDay(!DayNightLook.Instance.IsDay);
        }
    }
}
