using SunSwitch.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SunSwitch.UI
{
    /// <summary>
    /// Tints any UI Graphic (border, button, text) with the Day or Night accent and fades on switch.
    /// Tick Force Phase to pin a colour (end screens).
    /// </summary>
    [RequireComponent(typeof(Graphic))]
    public class PhaseThemedGraphic : MonoBehaviour
    {
        [SerializeField] Color dayColor = new Color(0.914f, 0.714f, 0.310f);    // #E9B64F gold
        [SerializeField] Color nightColor = new Color(0.561f, 0.659f, 0.878f);  // #8FA8E0 moon blue
        [SerializeField] bool forcePhase;
        [SerializeField] Phase forcedPhase = Phase.Day;
        [SerializeField] float fadeTime = 0.25f;

        Graphic _graphic;
        float _night;   // 0 = day colour, 1 = night colour

        void Awake() => _graphic = GetComponent<Graphic>();

        void OnEnable()
        {
            _night = TargetIsNight() ? 1f : 0f;   // snap when shown
            _graphic.color = Color.Lerp(dayColor, nightColor, _night);
        }

        void Update()
        {
            float target = TargetIsNight() ? 1f : 0f;
            if (Mathf.Approximately(_night, target)) return;
            _night = Mathf.MoveTowards(_night, target, Time.unscaledDeltaTime / Mathf.Max(0.01f, fadeTime));
            _graphic.color = Color.Lerp(dayColor, nightColor, _night);
        }

        bool TargetIsNight()
        {
            if (forcePhase) return forcedPhase == Phase.Night;
            var world = WorldPhase.Instance;
            return world != null && world.Current == Phase.Night;
        }
    }
}
