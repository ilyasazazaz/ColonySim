using System;
using System.Collections.Generic;

namespace SimpleColony
{
    public enum Profession { Gatherer, Defender, Scout, Researcher, Builder, Supplier, Medic, Stabilizer, Unemployed }
    public enum Resource { Food, Knowledge, Matter, Charge, Medicine, Stability }
    public enum Activity { Idle, Moving, Working, Resting, Dead, Guarding }

    public sealed class Deposit
    {
        public Resource Type { get; }
        public float Capacity { get; }
        public float Amount { get; private set; }
        public Deposit(Resource type, float capacity)
        {
            if (capacity <= 0 || float.IsNaN(capacity) || float.IsInfinity(capacity)) throw new ArgumentOutOfRangeException(nameof(capacity));
            Type = type;
            Capacity = capacity;
            Amount = capacity;
        }
        public float Take(float amount)
        {
            if (amount < 0 || float.IsNaN(amount) || float.IsInfinity(amount)) throw new ArgumentOutOfRangeException(nameof(amount));
            float taken = Math.Min(Amount, amount);
            Amount -= taken;
            return taken;
        }
        internal void Regenerate(float amount) { Amount = Math.Min(Capacity, Amount + amount); }
    }
    public sealed class Node
    {
        public int Id { get; }
        public float X { get; }
        public float Y { get; }
        public bool Known { get; internal set; }
        public bool Blocked { get; internal set; }
        public int Priority { get; internal set; } = 1;
        public float Threat { get; internal set; }
        public float Shield { get; internal set; }
        readonly List<int> links = new List<int>();
        readonly List<Deposit> deposits = new List<Deposit>();
        public IReadOnlyList<int> Links => links;
        public IReadOnlyList<Deposit> Deposits => deposits;
        internal bool Expanded;
        internal Node(int id, float x, float y) { Id = id; X = x; Y = y; }
        internal void Link(int id) { if (!links.Contains(id)) links.Add(id); }
        internal void Add(Deposit deposit) { deposits.Add(deposit); }
        public Deposit Find(Resource type) => deposits.Find(d => d.Type == type);
    }
    public sealed class Pawn
    {
        public int Id { get; internal set; }
        public Profession Role { get; internal set; }
        public Profession? PendingRole { get; internal set; }
        public int At { get; internal set; }
        public int Next { get; internal set; } = -1;
        public int Target { get; internal set; } = -1;
        public float Progress { get; internal set; }
        public float Food { get; internal set; } = 100;
        public float Energy { get; internal set; } = 100;
        public float Health { get; internal set; } = 100;
        public float Cargo { get; internal set; }
        public Resource CargoType { get; internal set; }
        public Activity State { get; internal set; }
        internal float Work;
    }
    public sealed class Inventory
    {
        readonly float[] amounts = new float[6];
        public float this[Resource type] => amounts[(int)type];
        internal void Add(Resource type, float amount) { amounts[(int)type] += amount; }
        internal bool Spend(Resource type, float amount)
        { if (amounts[(int)type] < amount) return false; amounts[(int)type] -= amount; return true; }
    }
}
