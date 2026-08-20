using System;
using System.Collections.Generic;

namespace ColonySim.Domain
{
    public interface INavigationMap
    {
        bool TryFindPath(GridPosition from, GridPosition to, out GridPosition[] path);
    }

    public sealed class GridNavigationMap : INavigationMap
    {
        private static readonly GridPosition[] Directions =
        {
            new GridPosition(1, 0),
            new GridPosition(0, 1),
            new GridPosition(-1, 0),
            new GridPosition(0, -1)
        };

        private readonly int width;
        private readonly int height;
        private readonly HashSet<GridPosition> blocked;

        public GridNavigationMap(int width, int height, IEnumerable<GridPosition> blockedPositions)
        {
            if (width <= 0 || height <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(width), "Grid dimensions must be positive.");
            }

            this.width = width;
            this.height = height;
            blocked = blockedPositions == null
                ? new HashSet<GridPosition>()
                : new HashSet<GridPosition>(blockedPositions);
        }

        public bool IsBlocked(GridPosition position) => blocked.Contains(position);

        public bool TryFindPath(GridPosition from, GridPosition to, out GridPosition[] path)
        {
            if (!IsInside(from) || !IsInside(to) || blocked.Contains(from) || blocked.Contains(to))
            {
                path = Array.Empty<GridPosition>();
                return false;
            }

            if (from == to)
            {
                path = Array.Empty<GridPosition>();
                return true;
            }

            var frontier = new Queue<GridPosition>();
            var previous = new Dictionary<GridPosition, GridPosition>();
            frontier.Enqueue(from);
            previous[from] = from;

            while (frontier.Count > 0)
            {
                var current = frontier.Dequeue();
                foreach (var direction in Directions)
                {
                    var next = new GridPosition(current.X + direction.X, current.Y + direction.Y);
                    if (!IsInside(next) || blocked.Contains(next) || previous.ContainsKey(next))
                    {
                        continue;
                    }

                    previous[next] = current;
                    if (next == to)
                    {
                        path = BuildPath(from, to, previous);
                        return true;
                    }

                    frontier.Enqueue(next);
                }
            }

            path = Array.Empty<GridPosition>();
            return false;
        }

        private bool IsInside(GridPosition position)
        {
            return position.X >= 0 && position.X < width && position.Y >= 0 && position.Y < height;
        }

        private static GridPosition[] BuildPath(
            GridPosition from,
            GridPosition to,
            IReadOnlyDictionary<GridPosition, GridPosition> previous)
        {
            var reversed = new List<GridPosition>();
            var cursor = to;
            while (cursor != from)
            {
                reversed.Add(cursor);
                cursor = previous[cursor];
            }

            reversed.Reverse();
            return reversed.ToArray();
        }
    }
}
