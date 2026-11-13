using System;
using System.Collections.Generic;

namespace PawnStories
{
    public sealed class StorySession
    {
        readonly List<Pawn> pawns = new List<Pawn>();
        readonly List<StoryEvent> events = new List<StoryEvent>();
        readonly List<WorkTicket> tickets = new List<WorkTicket>
        {
            new WorkTicket("Подготовить общий ужин", Motive.Connection, Skill.Social, Topic.People, 15),
            new WorkTicket("Обустроить лазарет", Motive.Security, Skill.Care, Topic.Home, 18),
            new WorkTicket("Разведать окрестности", Motive.Discovery, Skill.Survey, Topic.Frontier, 16),
            new WorkTicket("Починить водяной насос", Motive.Craft, Skill.Repair, Topic.Tools, 20)
        };

        public int Seed { get; }
        public int Turn { get; private set; }
        public bool TicketsEnabled { get; private set; } = true;
        public IReadOnlyList<Pawn> Pawns { get; }
        public IReadOnlyList<WorkTicket> Tickets { get; }
        public IReadOnlyList<StoryEvent> Events { get; }

        public StorySession(int seed, int population = 8)
        {
            if (population < 1) throw new ArgumentOutOfRangeException(nameof(population));
            Seed = seed;
            Pawns = pawns.AsReadOnly();
            Tickets = tickets.AsReadOnly();
            Events = events.AsReadOnly();
            var generator = new PawnGenerator();
            for (int i = 0; i < population; i++)
            {
                var random = new Random(unchecked((seed ^ 0x5AAD34F7) + i * 397));
                pawns.Add(new Pawn(generator.Generate(seed, i), random));
            }
        }

        public void SetPriority(int ticket, int priority)
        {
            WorkTicket item = tickets[ticket];
            int previous = item.Priority;
            item.SetPriority(priority);
            if (previous == item.Priority) return;
            string change = item.Priority == 0 ? "Задача снята" : $"Приоритет {previous} → {item.Priority}";
            events.Add(new StoryEvent(Turn, null, item.Title, change));
        }

        public bool Publish(string title, Motive motive)
        {
            if (string.IsNullOrWhiteSpace(title) || !Enum.IsDefined(typeof(Motive), motive)) return false;
            var template = tickets[(int)motive];
            var ticket = new WorkTicket(title.Trim(), motive, template.Skill, template.Topic, template.Total);
            ticket.SetPriority(1);
            tickets.Add(ticket);
            events.Add(new StoryEvent(Turn, null, ticket.Title, "Опубликована · приоритет 1"));
            return true;
        }

        public void SetTicketsEnabled(bool enabled)
        {
            if (TicketsEnabled == enabled) return;
            TicketsEnabled = enabled;
            events.Add(new StoryEvent(Turn, null, enabled ? "Доска открыта" : "Доска скрыта", null));
        }

        public void Step()
        {
            Turn++;
            // Everyone chooses from the same public board, before any work is done.
            foreach (Pawn pawn in pawns) pawn.Choose(tickets, TicketsEnabled);
            // Rotate execution order so the same pawn does not always contribute first.
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[(i + Turn - 1) % pawns.Count];
                string action = pawn.Act();
                events.Add(new StoryEvent(Turn, pawn.Profile, action, pawn.Intention.Reason));
            }
        }
    }
}
