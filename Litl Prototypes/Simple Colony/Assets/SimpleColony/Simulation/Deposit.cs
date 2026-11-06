using System;
using System.Collections.Generic;

namespace SimpleColony
{
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
}
