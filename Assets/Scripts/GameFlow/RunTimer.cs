using UnityEngine;

namespace SunSwitch.GameFlow
{
    /// <summary>
    /// Run time. Uses scaled time, so pausing (timeScale 0) stops it.
    /// Named Begin/ResetTimer because Start and Reset are Unity messages.
    /// </summary>
    public class RunTimer : MonoBehaviour
    {
        public float Elapsed { get; private set; }
        public bool Running { get; private set; }

        void Update()
        {
            if (Running) Elapsed += Time.deltaTime;
        }

        public void Begin() => Running = true;
        public void Stop() => Running = false;

        public void ResetTimer()
        {
            Running = false;
            Elapsed = 0f;
        }

        /// <summary>mm:ss.ff</summary>
        public static string Format(float seconds)
        {
            seconds = Mathf.Max(0f, seconds);
            return $"{(int)(seconds / 60f):00}:{seconds % 60f:00.00}";
        }
    }
}
