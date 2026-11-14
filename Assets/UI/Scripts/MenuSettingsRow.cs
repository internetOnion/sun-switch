using UnityEngine;
using UnityEngine.UI;

namespace SunSwitch.UI
{
    public sealed class MenuSettingsRow : MonoBehaviour
    {
        public Slider slider;
        public Text valueLabel;
        public bool sensitivity;
        bool preview;
        void OnEnable() { if (slider != null) { slider.onValueChanged.AddListener(Refresh); Refresh(slider.value); } }
        void OnDisable() { if (slider != null) slider.onValueChanged.RemoveListener(Refresh); }
        public void Configure(bool simulated)
        {
            preview = simulated; slider.interactable = simulated; Refresh(slider.value);
        }
        void Refresh(float value)
        {
            valueLabel.text = !preview ? "Unavailable" : sensitivity ? $"{value:0.0}×" : $"{Mathf.RoundToInt(value * 100)}%";
        }
    }
}
