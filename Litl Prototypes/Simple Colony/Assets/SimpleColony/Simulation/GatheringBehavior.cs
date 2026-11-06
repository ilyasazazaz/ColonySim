using System;
using System.Collections.Generic;

namespace SimpleColony
{
    public sealed class GatheringBehavior : IProfessionBehavior
    {
        readonly Resource resource;
        public GatheringBehavior(Resource resource) { this.resource = resource; }
        public bool CanWork(World world, Node node) => node.Known && node.Threat <= 0 && node.Find(resource)?.Amount > 0;
        public bool Work(World world, Pawn pawn, Node node, float dt)
        {
            if (pawn.Work < 4) return false;
            pawn.Load(resource, node.Find(resource).Take(world.CargoCapacity)); return true;
        }
    }
}
