using System.Collections.Generic;
using SunSwitch.Art;
using SunSwitch.Core;
using UnityEngine;

namespace SunSwitch.GameFlow
{
    /// <summary>
    /// Counts the art team's Shard pickups (Shard_Day / Shard_Night prefabs on main)
    /// and raises GameEvents.OnShardCollected for the HUD and audio.
    /// </summary>
    public class ShardTracker : MonoBehaviour
    {
        [SerializeField, Min(1)] int total = 12;

        readonly HashSet<Shard> _collected = new HashSet<Shard>();

        public int Collected => _collected.Count;
        public int Total => total;

        void OnEnable() => Shard.Collected += Collect;
        void OnDisable() => Shard.Collected -= Collect;

        public void Collect(Shard shard)
        {
            if (shard == null || !_collected.Add(shard)) return;   // ignores duplicates
            GameEvents.RaiseShardCollected(Collected, total);
        }

        /// <summary>Brings every shard back. No event: AudioManager would play the pickup sound.</summary>
        public void ResetShards()
        {
            _collected.Clear();
            Shard.ResetAll();
        }
    }
}
