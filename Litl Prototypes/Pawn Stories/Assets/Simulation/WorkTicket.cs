using System;

namespace PawnStories
{
    public sealed class WorkTicket
    {
        public string Title { get; }
        public Motive Motive { get; }
        public Skill Skill { get; }
        public Topic Topic { get; }
        public int Priority { get; private set; }
        public int Remaining { get; private set; }
        public int Total { get; }

        internal WorkTicket(string title, Motive motive, Skill skill, Topic topic, int work)
        {
            Title = title;
            Motive = motive;
            Skill = skill;
            Topic = topic;
            Total = Remaining = work;
        }

        internal void SetPriority(int priority) => Priority = Math.Max(0, Math.Min(3, priority));

        internal int Contribute(int amount)
        {
            int actual = Math.Min(Remaining, amount);
            Remaining -= actual;
            return actual;
        }
    }
}
