using SunSwitch.Core;
using UnityEngine;

namespace SunSwitch.LookAndFeel
{
    /// <summary>
    /// Slow orbit behind the Start screen. Put it on a pivot at the tower's centre with the menu
    /// CinemachineCamera as a child. Enabling the camera makes Cinemachine cut/blend to it;
    /// disabling it blends back to the player camera (set the blend time on the CinemachineBrain).
    /// </summary>
    public class MenuCameraOrbit : MonoBehaviour
    {
        [SerializeField, Tooltip("The menu CinemachineCamera object (child of this pivot)")]
        GameObject menuCamera;
        [SerializeField] float degreesPerSecond = 6f;
        [SerializeField, Tooltip("Tick if the game opens on the Start screen and GameManager doesn't announce Menu")]
        bool startInMenu;

        void Awake() => SetMenu(startInMenu);
        void OnEnable() => GameEvents.OnGameStateChanged += OnState;
        void OnDisable() => GameEvents.OnGameStateChanged -= OnState;

        void OnState(GameState s) => SetMenu(s == GameState.Menu);

        void SetMenu(bool on)
        {
            if (menuCamera) menuCamera.SetActive(on);
        }

        void Update()
        {
            if (menuCamera && menuCamera.activeSelf)
                transform.Rotate(0f, degreesPerSecond * Time.unscaledDeltaTime, 0f, Space.World);
        }
    }
}
