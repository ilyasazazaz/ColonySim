using System;
using System.Collections.Generic;

namespace SimpleColony
{
    public sealed class Inventory
    {
        readonly float[] amounts = new float[6];
        public float this[Resource type] => amounts[(int)type];
        static void ValidateAmount(float amount)
        {
            if (amount < 0 || float.IsNaN(amount) || float.IsInfinity(amount))
                throw new ArgumentOutOfRangeException(nameof(amount));
        }
        internal void Add(Resource type, float amount)
        {
            ValidateAmount(amount);
            amounts[(int)type] += amount;
        }
        internal bool Spend(Resource type, float amount)
        { ValidateAmount(amount); if (amounts[(int)type] < amount) return false; amounts[(int)type] -= amount; return true; }
    }
}
