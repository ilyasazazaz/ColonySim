namespace PawnStories
{
    public enum DecisionKind { Personal, Ticket, Rest, Sleep, Eat }

    public sealed class Decision
    {
        public Motive Goal { get; }
        public WorkTicket Ticket { get; }
        public DecisionKind Kind { get; }
        public string Reason { get; }
        internal int Score { get; }

        internal Decision(Motive goal, WorkTicket ticket, DecisionKind kind, string reason, int score)
        {
            Goal = goal;
            Ticket = ticket;
            Kind = kind;
            Reason = reason;
            Score = score;
        }
    }
}
