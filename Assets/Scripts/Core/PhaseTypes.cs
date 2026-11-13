namespace SunSwitch.Core
{
    public enum Phase { Day, Night }
    public enum PhaseTag { Day, Night, Both }
    public enum DeathCause { Void, Hazard }
    public enum GameState { Menu, Playing, Paused, GameOver, Won }

    public static class PhaseUtil
    {
        public static bool IsActive(PhaseTag tag, Phase current) =>
            tag == PhaseTag.Both ||
            (tag == PhaseTag.Day && current == Phase.Day) ||
            (tag == PhaseTag.Night && current == Phase.Night);

        public static Phase Opposite(Phase p) => p == Phase.Day ? Phase.Night : Phase.Day;
    }
}
