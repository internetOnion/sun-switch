using UnityEngine;

namespace SunSwitch.UI
{
    // The owning game-flow system implements this boundary when integration is agreed.
    public abstract class MenuFlowAdapter : MonoBehaviour
    {
        public virtual bool IsPreview => false;
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
