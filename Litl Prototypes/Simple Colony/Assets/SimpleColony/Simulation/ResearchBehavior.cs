using System;
using System.Collections.Generic;

namespace SimpleColony
{
    public sealed class ResearchBehavior : IProfessionBehavior
    {
        public bool CanWork(World world, Node node) => node.Known && node.Threat <= 0 && node.Find(Resource.Knowledge)?.Amount > 0;
        public bool Work(World world, Pawn pawn, Node node, float dt)
        { var deposit = node.Find(Resource.Knowledge); world.Stores.Add(Resource.Knowledge, deposit.Take(dt * .6f)); return deposit.Amount <= 0; }
    }
}
