using System;
using System.Collections.Generic;
using UnityEngine;

namespace SunSwitch.Core
{
    /// <summary>
    /// One place to freeze player control. Each system pushes its own reason
    /// ("pause", "menu", "death", "endScreen"); controls unlock only when every reason is popped.
    /// Also owns the cursor: locked + hidden while playing, free + visible while locked.
    /// </summary>
    public static class InputLock
    {
        static readonly HashSet<string> _reasons = new HashSet<string>();

        public static bool IsLocked => _reasons.Count > 0;
        public static event Action<bool> OnLockChanged;

        public static void Push(string reason)
        {
            if (_reasons.Add(reason) && _reasons.Count == 1) Apply();
        }

        public static void Pop(string reason)
        {
            if (_reasons.Remove(reason) && _reasons.Count == 0) Apply();
        }

        public static void Clear()
        {
            _reasons.Clear();
            Apply();
        }

        static void Apply()
        {
            bool locked = IsLocked;
            Cursor.lockState = locked ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = locked;
            OnLockChanged?.Invoke(locked);
        }

        // Static fields survive between Play sessions when Domain Reload is off, so reset them.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            _reasons.Clear();
            OnLockChanged = null;
        }

        // Lock the cursor for gameplay once the first scene is loaded.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void InitCursor() => Apply();
    }
}
