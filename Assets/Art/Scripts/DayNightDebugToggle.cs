using UnityEngine;
using UnityEngine.InputSystem;

namespace SunSwitch.Art
{
    /// <summary>
    /// Sandbox-only helper: press the key to flip DayNightLook between Day and Night, so the
    /// presentation can be checked before the real phase switch (Right Mouse, phase core) exists.
    /// Don't add this to gameplay scenes.
    /// </summary>
    public class DayNightDebugToggle : MonoBehaviour
    {
        [SerializeField] Key key = Key.T;

        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || DayNightLook.Instance == null) return;
            if (keyboard[key].wasPressedThisFrame)
                DayNightLook.Instance.SetDay(!DayNightLook.Instance.IsDay);
        }
    }
}
