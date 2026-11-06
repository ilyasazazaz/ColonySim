using System;
using System.Collections.Generic;

namespace SimpleColony
{
    internal sealed class JobCoordinator
    {
        readonly World world;
        readonly Pathfinder pathfinder;
        readonly IReadOnlyDictionary<Profession, IProfessionBehavior> behaviors;
        public JobCoordinator(World world, Pathfinder pathfinder, IReadOnlyDictionary<Profession, IProfessionBehavior> behaviors)
        {
            this.world = world;
            this.pathfinder = pathfinder;
            this.behaviors = behaviors;
        }
        int Reservations(int id) { int count = 0; foreach (var pawn in world.Pawns) if (pawn.Health > 0 && pawn.Target == id) count++; return count; }
        bool DefenseOccupied(int nodeId)
        {
            foreach (var pawn in world.Pawns)
                if (pawn.Role == Profession.Defender && pawn.Health > 0 && !pawn.PendingRole.HasValue && pawn.Target == nodeId) return true;
            return false;
        }
        internal void SelectWork(Pawn pawn)
        {
            float best = float.MaxValue;
            foreach (var node in world.Nodes)
            {
                if (node.Id == World.Home || node.Priority == 0 || !world.Travel.Accessible(node, pawn.Role == Profession.Defender) || !behaviors[pawn.Role].CanWork(world, node)) continue;
                // An assignment reserves the defense job immediately, including travel time.
                if (pawn.Role == Profession.Defender && DefenseOccupied(node.Id)) continue;
                float score = (world.Travel.Length(world.Nodes[pawn.At], node) + Reservations(node.Id) * 4) / node.Priority;
                if (score >= best) continue;
                var route = pathfinder.Find(pawn.At, node.Id, pawn.Role == Profession.Defender);
                if (route == null) continue;
                float food = 0, energy = 0; int previous = pawn.At;
                foreach (int next in route) { food += world.Travel.FoodCost(world.Nodes[previous], world.Nodes[next]); energy += world.Travel.EnergyCost(world.Nodes[previous], world.Nodes[next]); previous = next; }
                var returnRoute = pathfinder.Find(node.Id, World.Home, pawn.Role == Profession.Defender);
                if (returnRoute == null) continue;
                previous = node.Id;
                foreach (int next in returnRoute) { food += world.Travel.FoodCost(world.Nodes[previous], world.Nodes[next]); energy += world.Travel.EnergyCost(world.Nodes[previous], world.Nodes[next]); previous = next; }
                if (pawn.Food < food + 20 || pawn.Energy < energy + 15) continue;
                best = score; pawn.Assign(node.Id);
            }
        }
    }
}
