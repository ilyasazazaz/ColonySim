using System;
using System.Collections.Generic;

namespace SimpleColony
{
    public sealed class CombatBehavior : IProfessionBehavior
    {
        public bool CanWork(World world, Node node) => (node.Known || world.Frontier(node.Id)) && node.Threat > 0;
        public bool Work(World world, Pawn pawn, Node node, float dt)
        { node.Suppress(dt * world.DefensePower); pawn.TakeDamage(dt * .1f); return node.Threat <= 0; }
    }
}
