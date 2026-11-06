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
}
