using System;
using System.Collections.Generic;

namespace SimpleColony
{
    internal sealed class ResearchModule : MechanicModule
    {
        public ResearchProgress Progress { get; } = new ResearchProgress();
        internal override void Install(World world)
        {
            world.RegisterBehavior(Profession.Researcher, new ResearchBehavior());
        }
        public bool Purchase(World world, ResearchType type)
        {
            if (world.Lost || !Progress.Purchase(type, world.Stores)) return false;
            if (type == ResearchType.Transport) world.Travel.ImproveTransportation(.15f);
            if (type == ResearchType.Rations) world.Travel.FoodMultiplier = 1 - .2f * Progress.Level(type);
            return true;
        }
    }
}
