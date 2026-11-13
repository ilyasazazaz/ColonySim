namespace PawnStories
{
    public sealed class Decision
    {
        public Motive Goal { get; }
        public WorkTicket Ticket { get; }
        public bool Rest { get; }
        public string Reason { get; }
        internal int Score { get; }

        internal Decision(Motive goal, WorkTicket ticket, bool rest, string reason, int score)
        {
            Goal = goal;
            Ticket = ticket;
            Rest = rest;
            Reason = reason;
            Score = score;
        }
    }
}
