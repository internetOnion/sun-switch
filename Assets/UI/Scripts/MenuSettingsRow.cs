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
        MenuFlowAdapter provider;
        MenuSetting setting;
        void OnEnable() { if (slider != null) { slider.onValueChanged.AddListener(Refresh); Refresh(slider.value); } }
        void OnDisable() { if (slider != null) slider.onValueChanged.RemoveListener(Refresh); }
        public void Configure(bool simulated)
        {
            preview = simulated; slider.interactable = simulated || provider != null; Refresh(slider.value);
        }
        /// <summary>Makes the slider live: shows the stored value and writes changes back to the provider.</summary>
        public void Bind(MenuFlowAdapter adapter, MenuSetting id)
        {
            provider = adapter != null && adapter.ProvidesSettings ? adapter : null;
            setting = id;
            if (provider != null) slider.SetValueWithoutNotify(provider.GetSetting(id));
            slider.interactable = preview || provider != null;
            Refresh(slider.value);
        }
        void Refresh(float value)
        {
            if (provider != null) provider.SetSetting(setting, value);
            valueLabel.text = !preview && provider == null ? "Unavailable" : sensitivity ? $"{value:0.0}×" : $"{Mathf.RoundToInt(value * 100)}%";
        }
    }
}
