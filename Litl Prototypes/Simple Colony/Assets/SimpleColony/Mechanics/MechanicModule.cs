using System;
using System.Collections.Generic;

namespace SimpleColony
{
    [Flags]
    public enum Mechanics { None = 0, Threats = 1, Research = 2, All = Threats | Research }

    // Each world owns its modules and their state. Install runs after map generation.
    internal abstract class MechanicModule
    {
        internal abstract void Install(World world);
        internal virtual void Step(World world, float dt) { }
    }
}
