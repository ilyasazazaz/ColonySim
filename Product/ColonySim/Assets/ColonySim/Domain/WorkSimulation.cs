using System;
using System.Collections.Generic;
using System.Linq;

namespace ColonySim.Domain
{
    public interface IReservationService
    {
        bool TryReserve(string ownerId, IReadOnlyList<string> resourceIds, out string reason);
        void Release(string ownerId);
    }

    public sealed class ReservationBook : IReservationService
    {
        private readonly Dictionary<string, string> resourceOwners = new Dictionary<string, string>();

        public bool TryReserve(string ownerId, IReadOnlyList<string> resourceIds, out string reason)
        {
            if (string.IsNullOrWhiteSpace(ownerId))
            {
                throw new ArgumentException("Reservation owner is required.", nameof(ownerId));
            }

            foreach (var resourceId in resourceIds)
            {
                if (resourceOwners.TryGetValue(resourceId, out var existingOwner) && existingOwner != ownerId)
                {
                    reason = $"ресурс «{resourceId}» уже закреплён за {existingOwner}";
                    return false;
                }
            }

            foreach (var resourceId in resourceIds)
            {
                resourceOwners[resourceId] = ownerId;
            }

            reason = string.Empty;
            return true;
        }

        public void Release(string ownerId)
        {
            var ownedResources = resourceOwners
                .Where(pair => pair.Value == ownerId)
                .Select(pair => pair.Key)
                .ToArray();

            foreach (var resourceId in ownedResources)
            {
                resourceOwners.Remove(resourceId);
            }
        }

        public string OwnerOf(string resourceId)
        {
            return resourceOwners.TryGetValue(resourceId, out var owner) ? owner : null;
        }
    }

    public sealed class ColonySimulation
    {
        private readonly INavigationMap navigation;
        private readonly IReservationService reservations;
        private readonly List<Pawn> pawns;
        private readonly List<WorkTask> tasks;
        private readonly List<string> events = new List<string>();
        private readonly Dictionary<string, uint> tieBreakers;

        public ColonySimulation(
            IEnumerable<Pawn> pawns,
            IEnumerable<WorkTask> tasks,
            INavigationMap navigation,
            IReservationService reservations,
            int seed)
        {
            this.pawns = pawns?.OrderBy(pawn => pawn.Id, StringComparer.Ordinal).ToList()
                ?? throw new ArgumentNullException(nameof(pawns));
            this.tasks = tasks?.ToList() ?? throw new ArgumentNullException(nameof(tasks));
            this.navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
            this.reservations = reservations ?? throw new ArgumentNullException(nameof(reservations));
            tieBreakers = CreateTieBreakers(this.tasks, seed);
        }

        public IReadOnlyList<Pawn> Pawns => pawns;
        public IReadOnlyList<WorkTask> Tasks => tasks;
        public IReadOnlyList<string> Events => events;
        public int StepNumber { get; private set; }

        public void Step()
        {
            StepNumber++;

            foreach (var pawn in pawns)
            {
                if (pawn.Assignment == null)
                {
                    TryAssign(pawn);
                }
            }

            foreach (var pawn in pawns)
            {
                Advance(pawn);
            }
        }

        public string CreateDeterministicSnapshot()
        {
            var pawnState = string.Join(
                ";",
                pawns.Select(pawn => $"{pawn.Id}:{pawn.Position}:{pawn.Assignment?.Task.Id ?? "-"}:{pawn.Status}"));
            var taskState = string.Join(
                ";",
                tasks.OrderBy(task => task.Id, StringComparer.Ordinal)
                    .Select(task => $"{task.Id}:{task.Progress:0.000}:{task.Completed}:{task.AssignedPawnId ?? "-"}:{task.Status}"));
            return $"step={StepNumber}|pawns={pawnState}|tasks={taskState}|events={string.Join("/", events)}";
        }

        private void TryAssign(Pawn pawn)
        {
            var candidates = new List<Candidate>();
            var rejection = "нет доступной работы";

            foreach (var task in tasks.Where(task => !task.Completed && task.AssignedPawnId == null))
            {
                var performance = pawn.Performance.Evaluate(task.Requirements);
                if (!performance.Allowed)
                {
                    task.Status = $"{pawn.DisplayName}: {performance.Reason}";
                    rejection = performance.Reason;
                    continue;
                }

                if (!navigation.TryFindPath(pawn.Position, task.Target, out var path))
                {
                    task.Status = $"нет пути для {pawn.DisplayName}";
                    rejection = $"до «{task.DisplayName}» нет пути";
                    continue;
                }

                candidates.Add(new Candidate(task, path));
            }

            foreach (var candidate in candidates
                         .OrderByDescending(item => item.Task.Priority)
                         .ThenBy(item => item.Path.Length)
                         .ThenBy(item => tieBreakers[item.Task.Id])
                         .ThenBy(item => item.Task.Id, StringComparer.Ordinal))
            {
                var resources = RequiredReservations(candidate.Task);
                if (!reservations.TryReserve(pawn.Id, resources, out var reservationFailure))
                {
                    candidate.Task.Status = reservationFailure;
                    rejection = reservationFailure;
                    continue;
                }

                candidate.Task.AssignedPawnId = pawn.Id;
                candidate.Task.Status = $"назначено: {pawn.DisplayName}";
                pawn.Assignment = new WorkAssignment(candidate.Task, candidate.Path);
                pawn.Status = $"идёт к «{candidate.Task.DisplayName}»";
                events.Add($"{StepNumber}: {pawn.Id} получил {candidate.Task.Id}");
                return;
            }

            pawn.Status = rejection;
        }

        private void Advance(Pawn pawn)
        {
            var assignment = pawn.Assignment;
            if (assignment == null)
            {
                return;
            }

            var performance = pawn.Performance.Evaluate(assignment.Task.Requirements);
            if (!performance.Allowed)
            {
                CancelAssignment(pawn, performance.Reason);
                return;
            }

            if (assignment.NextPathIndex < assignment.Path.Length)
            {
                pawn.Position = assignment.Path[assignment.NextPathIndex++];
                pawn.Status = $"движется к «{assignment.Task.DisplayName}»";
                return;
            }

            assignment.Task.Progress += performance.Speed;
            assignment.Task.Status = $"{pawn.DisplayName} выполняет: {assignment.Task.Progress:0.0}/{assignment.Task.Effort:0.0}";
            pawn.Status = $"выполняет «{assignment.Task.DisplayName}» ×{performance.Speed:0.00}";

            if (assignment.Task.Progress + 0.0001f < assignment.Task.Effort)
            {
                return;
            }

            assignment.Task.Progress = assignment.Task.Effort;
            assignment.Task.Completed = true;
            assignment.Task.Status = $"завершено: {pawn.DisplayName}";
            events.Add($"{StepNumber}: {pawn.Id} завершил {assignment.Task.Id}");
            reservations.Release(pawn.Id);
            pawn.Assignment = null;
            pawn.Status = $"завершил «{assignment.Task.DisplayName}»";
        }

        private void CancelAssignment(Pawn pawn, string reason)
        {
            var task = pawn.Assignment.Task;
            task.AssignedPawnId = null;
            task.Status = $"отказ: {reason}";
            events.Add($"{StepNumber}: {pawn.Id} отказался от {task.Id}: {reason}");
            reservations.Release(pawn.Id);
            pawn.Assignment = null;
            pawn.Status = $"отказ: {reason}";
        }

        private static string[] RequiredReservations(WorkTask task)
        {
            return string.IsNullOrWhiteSpace(task.RequiredResourceId)
                ? new[] { $"цель:{task.Id}" }
                : new[] { $"цель:{task.Id}", task.RequiredResourceId };
        }

        private static Dictionary<string, uint> CreateTieBreakers(IEnumerable<WorkTask> tasks, int seed)
        {
            var sequence = new DeterministicSequence((uint)seed);
            return tasks.OrderBy(task => task.Id, StringComparer.Ordinal)
                .ToDictionary(task => task.Id, task => sequence.Next());
        }

        private readonly struct Candidate
        {
            public Candidate(WorkTask task, GridPosition[] path)
            {
                Task = task;
                Path = path;
            }

            public WorkTask Task { get; }
            public GridPosition[] Path { get; }
        }

        private sealed class DeterministicSequence
        {
            private uint state;

            public DeterministicSequence(uint seed)
            {
                state = seed == 0 ? 0x9E3779B9u : seed;
            }

            public uint Next()
            {
                state ^= state << 13;
                state ^= state >> 17;
                state ^= state << 5;
                return state;
            }
        }
    }
}
