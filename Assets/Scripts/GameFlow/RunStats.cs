using System;

namespace SunSwitch.GameFlow
{
    /// <summary>Results of one run, read by the end screens. GameManager owns and fills it.</summary>
    [Serializable]
    public class RunStats
    {
        public float time;
        public int deaths;
        public int shards;
        public int totalShards;
        public bool won;

        public void Clear()
        {
            time = 0f;
            deaths = shards = totalShards = 0;
            won = false;
        }

        public void Fill(float time, int shards, int totalShards, bool won)
        {
            this.time = time;
            this.shards = shards;
            this.totalShards = totalShards;
            this.won = won;
        }
    }
}
