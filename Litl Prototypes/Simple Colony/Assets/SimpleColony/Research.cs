using System;
using System.Collections.Generic;

namespace SimpleColony
{
    public enum ResearchType { Transport, Logistics, Rations, Recovery, Defense }

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

    public sealed class ResearchProgress
    {
        static readonly IReadOnlyList<ResearchDefinition> definitions = Array.AsReadOnly(new[] {
            new ResearchDefinition(ResearchType.Transport, "Транспорт", "road", "Каждый уровень добавляет 15% к базовой скорости передвижения.", 15),
            new ResearchDefinition(ResearchType.Logistics, "Логистика", "cargo", "Каждый уровень увеличивает вместимость груза на 4.", 20),
            new ResearchDefinition(ResearchType.Rations, "Рационы", "food", "Каждый уровень снижает расход еды на дороге на 20% от базового.", 20),
            new ResearchDefinition(ResearchType.Recovery, "Отдых", "medicine", "Каждый уровень добавляет 3 силы в секунду к восстановлению на ядре.", 25),
            new ResearchDefinition(ResearchType.Defense, "Защита", "shield", "Каждый уровень добавляет 50% к базовой силе подавления угроз.", 30)
        });
        readonly Dictionary<ResearchType, int> levels = new Dictionary<ResearchType, int>();
        public static IReadOnlyList<ResearchDefinition> Definitions => definitions;
        public int Level(ResearchType type) => levels.TryGetValue(type, out int level) ? level : 0;
        public bool IsCompleted(ResearchType type) => (int)type >= 0 && (int)type < definitions.Count && Level(type) >= definitions[(int)type].MaxLevel;
        public int NextCost(ResearchType type) => definitions[(int)type].Cost * (Level(type) + 1);
        public bool CanPurchase(ResearchType type, Inventory stores)
        {
            int index = (int)type;
            return index >= 0 && index < definitions.Count && !IsCompleted(type) && stores[Resource.Knowledge] >= NextCost(type);
        }
        internal bool Purchase(ResearchType type, Inventory stores)
        {
            if (!CanPurchase(type, stores) || !stores.Spend(Resource.Knowledge, NextCost(type))) return false;
            levels[type] = Level(type) + 1;
            return true;
        }
    }

    public sealed class RuntimeStatistics
    {
        double elapsed;
        int frames, ticks;
        public double FramesPerSecond { get; private set; }
        public double TicksPerSecond { get; private set; }
        public void Record(double seconds, int executedTicks)
        {
            elapsed += seconds;
            frames++;
            ticks += executedTicks;
            if (elapsed < 1) return;
            FramesPerSecond = frames / elapsed;
            TicksPerSecond = ticks / elapsed;
            elapsed = 0;
            frames = 0;
            ticks = 0;
        }
        public void Reset()
        {
            elapsed = 0;
            frames = 0;
            ticks = 0;
            FramesPerSecond = 0;
            TicksPerSecond = 0;
        }
    }
}
