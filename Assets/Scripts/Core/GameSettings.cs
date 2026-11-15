using System;
using UnityEngine;

namespace SunSwitch.Core
{
    /// <summary>
    /// Player settings from the Settings screen: music volume, sound effect volume and mouse
    /// sensitivity. Saved on this device (PlayerPrefs) and read where they apply: MusicPlayer,
    /// AudioManager and LightkeeperController. GameMenuFlow connects the menu sliders to it.
    /// </summary>
    public static class GameSettings
    {
        public enum Id { MusicVolume, SfxVolume, MouseSensitivity }

        const string KeyPrefix = "SunSwitch.Settings.";
        static readonly float[] Defaults = { 0.75f, 0.75f, 1f };
        static readonly Vector2[] Ranges = { new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.1f, 3f) };

        static float[] _values;

        /// <summary>Raised when a value changes (from the Settings screen or code).</summary>
        public static event Action<Id, float> Changed;

        public static float MusicVolume => Get(Id.MusicVolume);
        public static float SfxVolume => Get(Id.SfxVolume);
        public static float MouseSensitivity => Get(Id.MouseSensitivity);

        public static float Get(Id id)
        {
            Load();
            return _values[(int)id];
        }

        public static void Set(Id id, float value)
        {
            Load();
            var range = Ranges[(int)id];
            value = Mathf.Clamp(value, range.x, range.y);
            if (Mathf.Approximately(_values[(int)id], value)) return;
            _values[(int)id] = value;
            PlayerPrefs.SetFloat(KeyPrefix + id, value);   // written to disk on quit, or by Save()
            Changed?.Invoke(id, value);
        }

        /// <summary>Writes the settings to disk now (the Settings screen calls this on close).</summary>
        public static void Save() => PlayerPrefs.Save();

        static void Load()
        {
            if (_values != null) return;
            _values = new float[Defaults.Length];
            for (int i = 0; i < _values.Length; i++)
            {
                var range = Ranges[i];
                _values[i] = Mathf.Clamp(PlayerPrefs.GetFloat(KeyPrefix + (Id)i, Defaults[i]), range.x, range.y);
            }
        }

        // Static state survives between Play sessions when Domain Reload is off.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            _values = null;
            Changed = null;
        }
    }
}
