using System.Text.RegularExpressions;
using SunSwitch.Core;
using SunSwitch.GameFlow;
using UnityEngine;
using UnityEngine.UI;

namespace SunSwitch.Art
{
    /// <summary>
    /// HUD parchment strip at the bottom of the screen for instructions:
    ///   - the message of the HintZone the player is standing in (HintZone.Active)
    ///   - "Can't switch while inside a ghost…" for a moment when a switch is blocked
    /// Cross-fades when the message changes, and hides outside play (menus, pause, end screens).
    /// Words in [brackets] are drawn bold in the key colour.
    /// Uses unscaled time, like the checkpoint notice.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class HintPanel : MonoBehaviour
    {
        [SerializeField] Text text;
        [SerializeField, TextArea(2, 3)] string blockedMessage = "Can't switch while inside a ghost. Step out of it first.";
        [SerializeField] float blockedSeconds = 2.5f;
        [SerializeField] Color keyColor = new Color(0.55f, 0.29f, 0.07f);
        [SerializeField] float fadeSeconds = 0.2f;
        [SerializeField] float rise = 12f;

        static readonly Regex Keys = new Regex(@"\[([^\]]+)\]");

        CanvasGroup group;
        RectTransform rect;
        Vector2 home;
        string shown;
        float blockedLeft;
        WorldPhase world;

        /// <summary>The message on screen once it has fully faded in, else null.</summary>
        public string Message => group != null && group.alpha >= 0.99f ? shown : null;

        void Awake()
        {
            group = GetComponent<CanvasGroup>();
            rect = (RectTransform)transform;
            home = rect.anchoredPosition;
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
        }

        void OnDisable()
        {
            if (world != null) world.OnSwitchBlocked -= HandleBlocked;
            world = null;
        }

        void HandleBlocked() => blockedLeft = blockedSeconds;

        void Update()
        {
            if (world == null && WorldPhase.Instance != null)
            {
                world = WorldPhase.Instance;
                world.OnSwitchBlocked += HandleBlocked;
            }

            float dt = Time.unscaledDeltaTime;
            if (blockedLeft > 0f) blockedLeft -= dt;
            var zone = HintZone.Active;
            string wanted = blockedLeft > 0f ? blockedMessage : zone != null ? zone.Message : null;
            // Only while playing: not under the Start screen, the Pause card or the end screens.
            var game = GameManager.Instance;
            if (game != null && game.State != GameState.Playing) wanted = null;

            float step = fadeSeconds > 0f ? dt / fadeSeconds : 1f;
            if (wanted != shown)
            {
                // Fade the old message out before the new one comes in.
                group.alpha = Mathf.MoveTowards(group.alpha, 0f, step);
                if (group.alpha <= 0f)
                {
                    shown = wanted;
                    if (text != null && shown != null)
                        text.text = Keys.Replace(shown, $"<color=#{ColorUtility.ToHtmlStringRGB(keyColor)}><b>$1</b></color>");
                }
            }
            else group.alpha = Mathf.MoveTowards(group.alpha, shown != null ? 1f : 0f, step);

            rect.anchoredPosition = home + Vector2.down * rise * (1f - group.alpha);
        }
    }
}
