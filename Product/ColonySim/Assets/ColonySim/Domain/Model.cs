using System;

namespace ColonySim.Domain
{
    [Flags]
    public enum WorkFunction
    {
        None = 0,
        Consciousness = 1 << 0,
        Movement = 1 << 1,
        Manipulation = 1 << 2
    }

    public readonly struct GridPosition : IEquatable<GridPosition>
    {
        public GridPosition(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int X { get; }
        public int Y { get; }

        public int ManhattanDistance(GridPosition other)
        {
            return Math.Abs(X - other.X) + Math.Abs(Y - other.Y);
        }

        public bool Equals(GridPosition other) => X == other.X && Y == other.Y;
        public override bool Equals(object obj) => obj is GridPosition other && Equals(other);
        public override int GetHashCode() => (X * 397) ^ Y;
        public override string ToString() => $"({X}, {Y})";

        public static bool operator ==(GridPosition left, GridPosition right) => left.Equals(right);
        public static bool operator !=(GridPosition left, GridPosition right) => !left.Equals(right);
    }

    public readonly struct WorkPerformance
    {
        private WorkPerformance(bool allowed, float speed, string reason)
        {
            Allowed = allowed;
            Speed = speed;
            Reason = reason ?? string.Empty;
        }

        public bool Allowed { get; }
        public float Speed { get; }
        public string Reason { get; }

        public static WorkPerformance Available(float speed)
        {
            return new WorkPerformance(true, Math.Max(0.01f, speed), string.Empty);
        }

        public static WorkPerformance Denied(string reason)
        {
            return new WorkPerformance(false, 0f, reason);
        }
    }

    public interface IWorkPerformanceSource
    {
        WorkPerformance Evaluate(WorkFunction requirements);
    }

    public sealed class SimpleCondition : IWorkPerformanceSource
    {
        private float condition;

        public SimpleCondition(float condition)
        {
            Condition = condition;
        }

        public float Condition
        {
            get => condition;
            set => condition = Clamp01(value);
        }

        public WorkPerformance Evaluate(WorkFunction requirements)
        {
            return condition <= 0f
                ? WorkPerformance.Denied("общее состояние не позволяет действовать")
                : WorkPerformance.Available(condition);
        }

        private static float Clamp01(float value) => Math.Max(0f, Math.Min(1f, value));
    }

    public sealed class FunctionalCondition : IWorkPerformanceSource
    {
        public FunctionalCondition(float consciousness, float movement, float manipulation)
        {
            Consciousness = consciousness;
            Movement = movement;
            Manipulation = manipulation;
        }

        public float Consciousness { get; set; }
        public float Movement { get; set; }
        public float Manipulation { get; set; }

        public WorkPerformance Evaluate(WorkFunction requirements)
        {
            var speed = 1f;

            if (!Include(requirements, WorkFunction.Consciousness, Consciousness, ref speed))
            {
                return WorkPerformance.Denied("нет сознания");
            }

            if (!Include(requirements, WorkFunction.Movement, Movement, ref speed))
            {
                return WorkPerformance.Denied("невозможно перемещаться");
            }

            if (!Include(requirements, WorkFunction.Manipulation, Manipulation, ref speed))
            {
                return WorkPerformance.Denied("невозможно манипулировать");
            }

            return WorkPerformance.Available(speed);
        }

        private static bool Include(
            WorkFunction requirements,
            WorkFunction function,
            float value,
            ref float speed)
        {
            if ((requirements & function) == 0)
            {
                return true;
            }

            var clamped = Math.Max(0f, Math.Min(1f, value));
            if (clamped <= 0f)
            {
                return false;
            }

            speed = Math.Min(speed, clamped);
            return true;
        }
    }

    public sealed class Pawn
    {
        public Pawn(string id, string displayName, GridPosition position, IWorkPerformanceSource performance)
        {
            Id = RequireText(id, nameof(id));
            DisplayName = RequireText(displayName, nameof(displayName));
            Position = position;
            Performance = performance ?? throw new ArgumentNullException(nameof(performance));
            Status = "свободен";
        }

        public string Id { get; }
        public string DisplayName { get; }
        public GridPosition Position { get; internal set; }
        public IWorkPerformanceSource Performance { get; }
        public WorkAssignment Assignment { get; internal set; }
        public string Status { get; internal set; }

        private static string RequireText(string value, string parameter)
        {
            return string.IsNullOrWhiteSpace(value)
                ? throw new ArgumentException("Value cannot be empty.", parameter)
                : value;
        }
    }

    public sealed class WorkTask
    {
        public WorkTask(
            string id,
            string displayName,
            GridPosition target,
            WorkFunction requirements,
            int priority,
            float effort,
            string requiredResourceId = null)
        {
            Id = string.IsNullOrWhiteSpace(id) ? throw new ArgumentException("Task id is required.", nameof(id)) : id;
            DisplayName = string.IsNullOrWhiteSpace(displayName)
                ? throw new ArgumentException("Task name is required.", nameof(displayName))
                : displayName;
            Target = target;
            Requirements = requirements;
            Priority = priority;
            Effort = Math.Max(0.01f, effort);
            RequiredResourceId = requiredResourceId;
            Status = "ожидает";
        }

        public string Id { get; }
        public string DisplayName { get; }
        public GridPosition Target { get; }
        public WorkFunction Requirements { get; }
        public int Priority { get; }
        public float Effort { get; }
        public string RequiredResourceId { get; }
        public float Progress { get; internal set; }
        public bool Completed { get; internal set; }
        public string AssignedPawnId { get; internal set; }
        public string Status { get; internal set; }
    }

    public sealed class WorkAssignment
    {
        internal WorkAssignment(WorkTask task, GridPosition[] path)
        {
            Task = task;
            Path = path;
        }

        public WorkTask Task { get; }
        internal GridPosition[] Path { get; }
        internal int NextPathIndex { get; set; }
    }
}
