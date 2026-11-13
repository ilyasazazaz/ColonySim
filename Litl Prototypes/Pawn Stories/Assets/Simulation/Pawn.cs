using System;
using System.Collections.Generic;

namespace PawnStories
{
    public sealed class Pawn
    {
        readonly int[] desires = new int[4];
        public PawnProfile Profile { get; }
        public IReadOnlyList<int> Desires { get; }
        public int Energy { get; private set; }
        public Decision Intention { get; private set; }

        internal Pawn(PawnProfile profile, Random random)
        {
            Profile = profile;
            Desires = Array.AsReadOnly(desires);
            Energy = random.Next(55, 96);
            desires[(int)Motive.Connection] = 24 + profile.Sociability / 3;
            desires[(int)Motive.Security] = 24 + (100 - profile.Boldness) / 3;
            desires[(int)Motive.Discovery] = 24 + profile.Boldness / 3;
            desires[(int)Motive.Craft] = 24 + profile.Persistence / 3;
            desires[(int)profile.Value] += 22;
            for (int i = 0; i < desires.Length; i++) desires[i] += random.Next(-8, 9);
        }

        internal void Choose(IReadOnlyList<WorkTicket> tickets, bool considerTickets)
        {
            int highestMotive = 0;
            for (int i = 1; i < desires.Length; i++)
                if (desires[i] > desires[highestMotive]) highestMotive = i;

            int personalScore = desires[highestMotive];
            var choice = new Decision((Motive)highestMotive, null, false, "Самое сильное личное желание", personalScore);
            int restScore = 114 - Energy * 2;
            if (restScore > choice.Score)
                choice = new Decision((Motive)highestMotive, null, true, $"Энергия {Energy}/100, отдых важнее работы", restScore);

            Decision bestTicket = null;
            if (considerTickets && Energy >= 18)
            {
                foreach (var ticket in tickets)
                {
                    if (ticket.Priority == 0 || ticket.Remaining == 0) continue;
                    int desire = desires[(int)ticket.Motive];
                    int priority = ticket.Priority * 7;
                    int skill = Profile.Skills[(int)ticket.Skill] * 2;
                    int taste = Profile.Interest == ticket.Topic ? 10 : Profile.Dislike == ticket.Topic ? -18 : 0;
                    int commitment = Intention?.Ticket == ticket ? 4 + Profile.Persistence / 10 : 0;
                    int effort = 18 + (100 - Energy) / 5;
                    int score = desire + priority + skill + taste + commitment - effort;
                    if (bestTicket == null || score > bestTicket.Score)
                    {
                        string reason = $"Желание {desire} · приоритет +{priority} · навык +{skill} · " +
                            $"вкус {taste:+0;-0;0} · приверженность +{commitment} · усилие −{effort}";
                        bestTicket = new Decision(ticket.Motive, ticket, false, reason, score);
                    }
                }
            }

            if (bestTicket != null && bestTicket.Score > choice.Score)
            {
                Intention = new Decision(bestTicket.Goal, bestTicket.Ticket, false,
                    $"{bestTicket.Reason}. Итог {bestTicket.Score}, лучшая личная альтернатива {choice.Score}.",
                    bestTicket.Score);
            }
            else if (bestTicket != null)
            {
                Intention = new Decision(choice.Goal, null, choice.Rest,
                    $"{choice.Reason} ({choice.Score}). Лучшая задача «{bestTicket.Ticket.Title}» набрала {bestTicket.Score}.",
                    choice.Score);
            }
            else Intention = choice;
        }

        internal string Act()
        {
            for (int i = 0; i < desires.Length; i++)
                desires[i] = Math.Min(100, desires[i] + 3);
            if (Intention.Rest)
            {
                Energy = Math.Min(100, Energy + 34);
                return "Отдых";
            }

            if (Intention.Ticket is WorkTicket ticket)
            {
                int work = ticket.Contribute(1 + Profile.Skills[(int)ticket.Skill] / 3);
                if (work == 0)
                {
                    return $"«{ticket.Title}» уже завершена другими";
                }
                Energy = Math.Max(0, Energy - 18);
                Satisfy(Intention.Goal, 13);
                return $"{ticket.Title}  +{work}" + (ticket.Remaining == 0 ? "  ✓" : "");
            }

            Energy = Math.Max(0, Energy - 9);
            Satisfy(Intention.Goal, 21);
            return StoryText.PersonalAction(Intention.Goal);
        }

        void Satisfy(Motive motive, int amount) => desires[(int)motive] = Math.Max(0, desires[(int)motive] - amount);

    }
}
