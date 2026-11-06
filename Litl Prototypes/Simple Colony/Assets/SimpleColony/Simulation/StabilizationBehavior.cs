using System;
using System.Collections.Generic;

namespace SimpleColony
{
    public sealed class StabilizationBehavior : IProfessionBehavior
    {
        public bool CanWork(World world, Node node) => node.Known && node.Threat <= 0 && node.Find(Resource.Stability)?.Amount > 0;
        public bool Work(World world, Pawn pawn, Node node, float dt)
        {
            var deposit = node.Find(Resource.Stability); deposit.Take(dt * .3f); node.Protect();
            foreach (int next in node.Links) world.Nodes[next].Protect();
            return deposit.Amount <= 0;
        }
    }
}
