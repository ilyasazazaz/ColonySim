using System;
using System.Collections.Generic;

namespace SimpleColony
{
    public interface IProfessionBehavior
    {
        bool CanWork(World world, Node node);
        bool Work(World world, Pawn pawn, Node node, float dt);
    }
    public sealed class GatheringBehavior : IProfessionBehavior
    {
        readonly Resource resource;
        public GatheringBehavior(Resource resource) { this.resource = resource; }
        public bool CanWork(World world, Node node) => node.Known && node.Threat <= 0 && node.Find(resource)?.Amount > 0;
        public bool Work(World world, Pawn pawn, Node node, float dt)
        {
            if (pawn.Work < 4) return false;
            pawn.CargoType = resource; pawn.Cargo = node.Find(resource).Take(world.CargoCapacity); return true;
        }
    }
    public sealed class CombatBehavior : IProfessionBehavior
    {
        public bool CanWork(World world, Node node) => (node.Known || world.Frontier(node.Id)) && node.Threat > 0;
        public bool Work(World world, Pawn pawn, Node node, float dt)
        { node.Threat = Math.Max(0, node.Threat - dt * world.DefensePower); pawn.Health -= dt * .1f; return node.Threat <= 0; }
    }
    public sealed class ExplorationBehavior : IProfessionBehavior
    {
        public bool CanWork(World world, Node node) => !node.Known && world.Frontier(node.Id) && node.Threat <= 0;
        public bool Work(World world, Pawn pawn, Node node, float dt)
        { if (pawn.Work < 5) return false; world.Discover(node.Id); return true; }
    }
    public sealed class ResearchBehavior : IProfessionBehavior
    {
        public bool CanWork(World world, Node node) => node.Known && node.Threat <= 0 && node.Find(Resource.Knowledge)?.Amount > 0;
        public bool Work(World world, Pawn pawn, Node node, float dt)
        { var deposit = node.Find(Resource.Knowledge); world.Stores.Add(Resource.Knowledge, deposit.Take(dt * .6f)); return deposit.Amount <= 0; }
    }
    public sealed class StabilizationBehavior : IProfessionBehavior
    {
        public bool CanWork(World world, Node node) => node.Known && node.Threat <= 0 && node.Find(Resource.Stability)?.Amount > 0;
        public bool Work(World world, Pawn pawn, Node node, float dt)
        {
            var deposit = node.Find(Resource.Stability); deposit.Take(dt * .3f); node.Shield = 3;
            foreach (int next in node.Links) world.Nodes[next].Shield = 3;
            return deposit.Amount <= 0;
        }
    }
}
