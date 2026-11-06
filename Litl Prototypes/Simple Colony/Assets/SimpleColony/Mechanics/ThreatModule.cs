using System;
using System.Collections.Generic;

namespace SimpleColony
{
    internal sealed class ThreatModule : MechanicModule
    {
        readonly Random random;
        public ThreatModule(Random random) { this.random = random; }
        internal override void Install(World world)
        {
            world.RegisterBehavior(Profession.Defender, new CombatBehavior());
            world.RegisterBehavior(Profession.Stabilizer, new StabilizationBehavior());
            SpawnThreat(world);
        }
        internal override void Step(World world, float dt)
        {
            var nodes = world.Nodes;
            if (world.Tick % 900 == 0) SpawnThreat(world);
            if (world.Tick % 100 == 0)
            {
                var spreading = new float[nodes.Count];
                foreach (var node in nodes)
                {
                    if (node.Threat <= 0) continue;
                    node.GrowOutbreak();
                    if (node.Threat > 10 && node.Shield <= 0)
                    { int next = node.Links[random.Next(node.Links.Count)]; if (nodes[next].Shield <= 0) spreading[next] += 1; }
                }
                for (int i = 0; i < nodes.Count; i++) nodes[i].Infect(spreading[i]);
            }
        }
        void SpawnThreat(World world)
        {
            var nodes = world.Nodes;
            var distance = new float[nodes.Count]; var used = new bool[nodes.Count];
            for (int i = 1; i < distance.Length; i++) distance[i] = float.MaxValue;
            for (int step = 0; step < nodes.Count; step++)
            {
                int current = -1;
                for (int i = 0; i < nodes.Count; i++) if (!used[i] && (current < 0 || distance[i] < distance[current])) current = i;
                if (current < 0 || distance[current] == float.MaxValue) break;
                used[current] = true;
                foreach (int next in nodes[current].Links) distance[next] = Math.Min(distance[next], distance[current] + world.Travel.Length(nodes[current], nodes[next]));
            }
            int farthest = 1;
            for (int i = 1; i < distance.Length; i++) if (distance[i] > distance[farthest]) farthest = i;
            nodes[farthest].Infect(6);
            world.Report("На границе появился очаг");
        }
    }
}
