using System;
using System.Collections.Generic;
namespace SimpleColony
{
    public sealed class Node
    {
        public float X, Y, Stock, Threat, Boost, Shield; public int Kind, Priority = 1; public bool Known, Blocked; public List<int> Links = new List<int>();
    }
    public sealed class Pawn
    {
        public int Role, At = 14, Next = -1, Target = -1, PendingRole = -1; public float Progress, Food = 100, Energy = 100, Health = 100, Work, Cargo; public string State = "Ожидание";
    }
    public sealed class World
    {
        public const int Home = 14; public readonly Node[] Nodes = new Node[30]; public readonly List<Pawn> Pawns = new List<Pawn>();
        public readonly float[] Stores = new float[8]; public int Seed, Tick, Searches, Tech; public float Time; public bool Lost; public string Event = "Колония основана";
        readonly Random rng; readonly int[] reserved = new int[30]; public float SpeedBonus = 1;
        public World(int seed)
        {
            Seed = seed; rng = new Random(seed);
            for (int i = 0; i < 30; i++) Nodes[i] = new Node { X = i % 6 + (float)rng.NextDouble() * .32f, Y = i / 6 + (float)rng.NextDouble() * .32f, Kind = new[] { 0, 3, 4, 5, 6, 7 }[rng.Next(6)], Stock = 35 + rng.Next(30) };
            var visited = new HashSet<int> { Home }; var stack = new Stack<int>(); stack.Push(Home);
            while (stack.Count > 0) { int a = stack.Peek(); var candidates = Adjacent(a).FindAll(b => !visited.Contains(b)); if (candidates.Count == 0) { stack.Pop(); continue; } int b = candidates[rng.Next(candidates.Count)]; Link(a, b); visited.Add(b); stack.Push(b); }
            for (int k = 0; k < 200 && Edges() < 37; k++) { int a = rng.Next(30); var ns = Adjacent(a); int b = ns[rng.Next(ns.Count)]; if (!Nodes[a].Links.Contains(b)) Link(a, b); }
            Nodes[Home].Known = true; Nodes[Home].Kind = -1; foreach (int n in Nodes[Home].Links) { Nodes[n].Known = true; Nodes[n].Kind = 0; }
            Stores[0] = 100; for (int i = 0; i < 8; i++) Pawns.Add(new Pawn { Role = i }); Pawns.Add(new Pawn { Role = 0 }); Pawns.Add(new Pawn { Role = 0 });
        }
        List<int> Adjacent(int i) { var r = new List<int>(); if (i % 6 > 0) r.Add(i - 1); if (i % 6 < 5) r.Add(i + 1); if (i >= 6) r.Add(i - 6); if (i < 24) r.Add(i + 6); return r; }
        void Link(int a, int b) { Nodes[a].Links.Add(b); Nodes[b].Links.Add(a); }
        public int Edges() { int n = 0; foreach (var v in Nodes) n += v.Links.Count; return n / 2; }
        public bool Frontier(int n) { foreach (int i in Nodes[n].Links) if (Nodes[i].Known) return true; return false; }
        public int Path(int start, int goal, bool fighter)
        {
            Searches++; var d = new float[30]; var prev = new int[30]; var used = new bool[30]; for (int i = 0; i < 30; i++) { d[i] = float.MaxValue; prev[i] = -1; }
            d[start] = 0;
            for (int k = 0; k < 30; k++)
            {
                int a = -1; for (int i = 0; i < 30; i++) if (!used[i] && (a < 0 || d[i] < d[a])) a = i; if (a < 0 || d[a] == float.MaxValue) break; if (a == goal) break; used[a] = true;
                foreach (int b in Nodes[a].Links) { if (b != goal && !Nodes[b].Known) continue; if (!fighter && (Nodes[b].Blocked || Nodes[b].Threat > 1)) continue; float dx = Nodes[a].X - Nodes[b].X, dy = Nodes[a].Y - Nodes[b].Y; float cost = (float)Math.Sqrt(dx * dx + dy * dy); if (d[a] + cost < d[b]) { d[b] = d[a] + cost; prev[b] = a; } }
            }
            if (start == goal) return start; if (prev[goal] < 0) return -1; int p = goal; while (prev[p] != start) { p = prev[p]; if (p < 0) return -1; }
            return p;
        }
        void Release(Pawn p) { if (p.Target >= 0) reserved[p.Target] = Math.Max(0, reserved[p.Target] - 1); p.Target = -1; p.Work = 0; }
        public bool Recruit(int role) { if (Stores[0] < 25 || Pawns.Count >= 60) return false; Stores[0] -= 25; Pawns.Add(new Pawn { Role = role }); return true; }
        public bool Upgrade() { if (Stores[3] < 15) return false; Stores[3] -= 15; Tech++; SpeedBonus += .15f; return true; }
        public void Step()
        {
            if (Lost) return; Tick++; Time += .1f;
            foreach (var n in Nodes) { n.Boost = Math.Max(0, n.Boost - .1f); n.Shield = Math.Max(0, n.Shield - .1f); if (n.Kind == 0 || n.Kind == 5) n.Stock = Math.Min(80, n.Stock + .1f * (n.Boost > 0 ? 1.5f : .35f)); if (n.Threat > 0) n.Stock = Math.Max(0, n.Stock - n.Threat * .002f); }
            if (Tick % 300 == 0 && Time > 60) { int a = rng.Next(30); if (Math.Abs(a % 6 - Home % 6) + Math.Abs(a / 6 - Home / 6) >= 4) { Nodes[a].Threat += 8; Event = "На окраине появился очаг"; } }
            if (Tick % 100 == 0) { var add = new float[30]; for (int i = 0; i < 30; i++) if (Nodes[i].Threat > 0) { Nodes[i].Threat = Math.Min(40, Nodes[i].Threat + 1); if (Nodes[i].Threat > 10 && Nodes[i].Shield <= 0) { int b = Nodes[i].Links[rng.Next(Nodes[i].Links.Count)]; if (Nodes[b].Shield <= 0) add[b] += 2; } } for (int i = 0; i < 30; i++) Nodes[i].Threat += add[i]; }
            foreach (var p in Pawns)
            {
                if (p.Health <= 0) continue; p.Food -= .035f; p.Energy -= .02f; if (p.Food <= 0) p.Health -= .08f;
                if (p.Next >= 0) { float dx = Nodes[p.At].X - Nodes[p.Next].X, dy = Nodes[p.At].Y - Nodes[p.Next].Y; float length = (float)Math.Sqrt(dx * dx + dy * dy); p.Progress += .035f * SpeedBonus / length; if (p.Progress >= 1) { p.At = p.Next; p.Next = -1; p.Progress = 0; } continue; }
                if (p.At == Home) { if (p.Cargo > 0) { Stores[p.Role] += p.Cargo; p.Cargo = 0; } if (p.PendingRole >= 0) { Release(p); p.Role = p.PendingRole; p.PendingRole = -1; } if (p.Food < 85 && Stores[0] >= 1) { Stores[0]--; p.Food = Math.Min(100, p.Food + 12); } if (p.Health < 100 && Stores[6] >= .1f) { Stores[6] -= .1f; p.Health = Math.Min(100, p.Health + 1); } if (p.Energy < 95) { p.Energy += .6f; p.State = "Отдых"; Release(p); continue; } }
                bool rest = ((Time + (Pawns.IndexOf(p) % 2) * 60) % 120) > 95;
                if (p.Cargo > 0 || p.Food < 30 || p.Energy < 20 || p.Health < 35 || p.PendingRole >= 0 || rest) { Release(p); Go(p, Home); continue; }
                if (Nodes[p.At].Threat > 0 && p.Role != 1) { Release(p); Go(p, Home); continue; }
                if (p.Target < 0) { float best = float.MaxValue; for (int i = 0; i < 30; i++) { var n = Nodes[i]; bool valid = p.Role == 2 ? !n.Known && Frontier(i) : n.Known && !n.Blocked && n.Priority > 0 && (p.Role == 1 ? n.Threat > 0 : n.Kind == p.Role && n.Stock > reserved[i] * 4 && n.Threat <= 0); if (!valid || i == Home) continue; float score = (Math.Abs(n.X - Nodes[p.At].X) + Math.Abs(n.Y - Nodes[p.At].Y) + reserved[i] * 3) / Math.Max(1, n.Priority); if (score < best && Path(p.At, i, p.Role == 1) >= 0) { best = score; p.Target = i; } } if (p.Target >= 0) reserved[p.Target]++; }
                if (p.Target < 0) { p.State = "Нет работы"; continue; }
                if (p.At != p.Target) { Go(p, p.Target); continue; }
                var node = Nodes[p.At]; p.State = "Работает"; p.Work += .1f;
                if (p.Role == 1) { node.Threat = Math.Max(0, node.Threat - .15f); p.Health -= .025f; if (node.Threat <= 0) Release(p); }
                else if (p.Role == 2) { if (p.Work >= 3) { node.Known = true; Event = "Открыта территория " + p.At; Release(p); } }
                else if (p.Role == 3) { float take = Math.Min(node.Stock, .1f); node.Stock -= take; Stores[3] += take; if (node.Stock <= 0) Release(p); }
                else if (p.Role == 7) { float take = Math.Min(node.Stock, .05f); node.Stock -= take; node.Shield = 3; foreach (int b in node.Links) Nodes[b].Shield = 3; if (node.Stock <= 0) Release(p); }
                else if (p.Work >= 2) { float take = Math.Min(4, node.Stock); node.Stock -= take; p.Cargo = take; Release(p); }
            }
            if (Tick % 50 == 0) { if (Stores[5] >= 1) { foreach (var n in Nodes) if (n.Known && n.Kind == 0) { n.Boost = 10; Stores[5]--; break; } } if (Stores[4] >= 10) { Stores[4] -= 10; SpeedBonus += .02f; Event = "Улучшена транспортная сеть"; } }
            int alive = 0; foreach (var p in Pawns) if (p.Health > 0) alive++; Lost = alive == 0 || Nodes[Home].Threat > 25;
        }
        void Go(Pawn p, int goal) { if (p.At == goal) { p.State = "На базе"; return; } p.Next = Path(p.At, goal, p.Role == 1); p.State = p.Next < 0 ? "Нет пути" : "В пути"; if (p.Next < 0) Release(p); }
    }
}
