using UnityEngine;

namespace SunSwitch.UI
{
    public sealed class MenuCommand : MonoBehaviour
    {
        public StartMenuView view;
        public MenuCommandId command;
        public void Invoke() { view.Execute(command); }
    }
}
