namespace PawnStories
{
    public sealed class StoryEvent
    {
        public int Turn { get; }
        public int? PawnId { get; }
        public string Actor { get; }
        public string Action { get; }
        public string Reason { get; }

        internal StoryEvent(int turn, PawnProfile pawn, string action, string reason)
        {
            Turn = turn;
            PawnId = pawn?.Id;
            Actor = pawn?.Name ?? "Доска";
            Action = action;
            Reason = reason;
        }
    }
}
