using System;
using System.Collections.Generic;

namespace SimpleColony
{
    public sealed class TravelPolicy
    {
        public float SpeedMultiplier { get; private set; } = 1;
        public float FoodMultiplier { get; internal set; } = 1;
        internal void ImproveTransportation(float bonus = .02f) { SpeedMultiplier += bonus; }
        public float Length(Node a, Node b)
        { float x = a.X - b.X, y = a.Y - b.Y; return (float)Math.Sqrt(x * x + y * y); }
        public float FoodCost(Node a, Node b) => Length(a, b) * .6f * FoodMultiplier;
        public float EnergyCost(Node a, Node b) => Length(a, b) * 5;
        public bool Accessible(Node node, bool defender) => defender || (!node.Blocked && node.Threat <= 0);
    }
    public sealed class Pathfinder
    {
        readonly World world;
        public Pathfinder(World world) { this.world = world; }
        public List<int> Find(int start, int goal, bool defender)
        {
            world.Searches++;
            int count = world.Nodes.Count;
            var distances = new float[count]; var previous = new int[count]; var used = new bool[count];
            for (int i = 0; i < count; i++) { distances[i] = float.MaxValue; previous[i] = -1; }
            distances[start] = 0;
            for (int iteration = 0; iteration < count; iteration++)
            {
                int current = -1;
                for (int i = 0; i < count; i++) if (!used[i] && (current < 0 || distances[i] < distances[current])) current = i;
                if (current < 0 || distances[current] == float.MaxValue || current == goal) break;
                used[current] = true;
                foreach (int next in world.Nodes[current].Links)
                {
                    var node = world.Nodes[next];
                    if ((!node.Known && next != goal) || !world.Travel.Accessible(node, defender)) continue;
                    float distance = distances[current] + world.Travel.Length(world.Nodes[current], node);
                    if (distance < distances[next]) { distances[next] = distance; previous[next] = current; }
                }
            }
            if (start != goal && previous[goal] < 0) return null;
            var path = new List<int>();
            for (int current = goal; current != start; current = previous[current]) path.Add(current);
            path.Reverse(); return path;
        }
    }
}
