using System;
using System.Collections.Generic;

namespace SimpleColony
{
    public sealed class Node
    {
        public int Id { get; }
        public float X { get; }
        public float Y { get; }
        public bool Known { get; private set; }
        public bool Blocked { get; private set; }
        public int Priority { get; private set; } = 1;
        public float Threat { get; private set; }
        public float Shield { get; private set; }
        readonly List<int> links = new List<int>();
        readonly List<Deposit> deposits = new List<Deposit>();
        public IReadOnlyList<int> Links => links;
        public IReadOnlyList<Deposit> Deposits => deposits;
        internal bool Expanded { get; private set; }
        internal void MarkExpanded() { Expanded = true; }
        internal void Reveal() { Known = true; }
        internal void CyclePriority() { Priority = (Priority + 1) % 4; }
        internal void ToggleAccess() { Blocked = !Blocked; }
        internal void Infect(float amount) { Threat += amount; }
        internal void GrowOutbreak() { Threat = Math.Min(35, Threat + .5f); }
        internal void Suppress(float amount) { Threat = Math.Max(0, Threat - amount); }
        internal void Protect() { Shield = 3; }
        internal void AgeShield(float dt) { Shield = Math.Max(0, Shield - dt); }
        internal Node(int id, float x, float y) { Id = id; X = x; Y = y; }
        internal void Link(int id) { if (!links.Contains(id)) links.Add(id); }
        internal void Add(Deposit deposit) { deposits.Add(deposit); }
        public Deposit Find(Resource type) => deposits.Find(d => d.Type == type);
    }
}
