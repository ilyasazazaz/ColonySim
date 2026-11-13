namespace PawnStories
{
    public enum DailyPhase { Sleep, Awake, Work }

    public sealed class DailySchedule
    {
        public int WakeHour { get; }
        public int WorkHour => (WakeHour + 2) % 24;
        public int WorkEnd => (WakeHour + 10) % 24;
        public int SleepHour => (WakeHour + 16) % 24;

        internal DailySchedule(int wakeHour) { WakeHour = wakeHour; }

        public DailyPhase At(int hour)
        {
            int sinceWake = (hour + 24 - WakeHour) % 24;
            if (sinceWake >= 16) return DailyPhase.Sleep;
            return sinceWake >= 2 && sinceWake < 10 ? DailyPhase.Work : DailyPhase.Awake;
        }
    }
}
