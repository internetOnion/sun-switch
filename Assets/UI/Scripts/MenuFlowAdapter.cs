using UnityEngine;

namespace SunSwitch.UI
{
    /// <summary>The Settings screen's sliders, in their order on the screen.</summary>
    public enum MenuSetting { MusicVolume, SfxVolume, MouseSensitivity }

    // The owning game-flow system implements this boundary when integration is agreed.
    public abstract class MenuFlowAdapter : MonoBehaviour
    {
        public virtual bool IsPreview => false;

        /// <summary>True when the game stores the Settings screen's values (sliders become live).</summary>
        public virtual bool ProvidesSettings => false;
        public virtual float GetSetting(MenuSetting setting) => 0f;
        public virtual void SetSetting(MenuSetting setting, float value) { }
        /// <summary>Called when the Settings screen closes, to write the values to disk.</summary>
        public virtual void SaveSettings() { }
        public abstract void RequestStart(StartMenuView view);
        public abstract void RequestQuit(StartMenuView view);
        public virtual void Request(MenuIntent intent, StartMenuView view)
        {
            if (intent == MenuIntent.Play) RequestStart(view);
            else if (intent == MenuIntent.Quit) RequestQuit(view);
            else
            {
                view.CompleteRequest();
                view.ShowMessage("Action unavailable", "This action is not connected to game flow yet.");
            }
        }
    }
}
