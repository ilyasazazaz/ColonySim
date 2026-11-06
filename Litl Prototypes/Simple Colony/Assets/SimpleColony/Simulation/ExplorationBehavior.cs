using System;
using System.Collections.Generic;

namespace SimpleColony
{
    public sealed class ExplorationBehavior : IProfessionBehavior
    {
        public bool CanWork(World world, Node node) => !node.Known && world.Frontier(node.Id) && node.Threat <= 0;
        public bool Work(World world, Pawn pawn, Node node, float dt)
        { if (pawn.Work < 5) return false; world.Discover(node.Id); return true; }
    }
}
