using System;
using System.Collections.Generic;

namespace SimpleColony
{
    public sealed class ResearchDefinition
    {
        public ResearchType Type { get; }
        public string Name { get; }
        public string Icon { get; }
        public string Description { get; }
        public int Cost { get; }
        public int MaxLevel => 3;

        public ResearchDefinition(ResearchType type, string name, string icon, string description, int cost)
        {
            Type = type;
            Name = name;
            Icon = icon;
            Description = description;
            Cost = cost;
        }
    }
}
