using System.Collections.Generic;

namespace ColonySim.Domain
{
    public enum HealthModelSelection
    {
        Simple,
        Functional
    }

    public sealed class ColonyScenario
    {
        internal ColonyScenario(ColonySimulation simulation, GridNavigationMap map, GridPosition[] obstacles)
        {
            Simulation = simulation;
            Map = map;
            Obstacles = obstacles;
        }

        public ColonySimulation Simulation { get; }
        public GridNavigationMap Map { get; }
        public IReadOnlyList<GridPosition> Obstacles { get; }
    }

    public static class ColonyScenarioFactory
    {
        public const int Width = 8;
        public const int Height = 5;

        public static ColonyScenario Create(HealthModelSelection model, int seed)
        {
            var obstacles = new[]
            {
                new GridPosition(3, 1),
                new GridPosition(3, 2),
                new GridPosition(6, 4),
                new GridPosition(7, 3)
            };

            var map = new GridNavigationMap(Width, Height, obstacles);
            var pawns = new[]
            {
                new Pawn("ada", "Ада", new GridPosition(0, 1), CreateCondition(model, 1f, 1f, 1f, 1f)),
                new Pawn("bor", "Бор", new GridPosition(0, 3), CreateCondition(model, 0.55f, 0.9f, 0.55f, 0.55f)),
                new Pawn("cora", "Кора", new GridPosition(1, 4), CreateCondition(model, 0.65f, 1f, 1f, 0f))
            };

            var tasks = new[]
            {
                new WorkTask(
                    "repair-pump",
                    "Починить насос",
                    new GridPosition(6, 1),
                    WorkFunction.Consciousness | WorkFunction.Movement | WorkFunction.Manipulation,
                    100,
                    5f,
                    "инструмент:ключ"),
                new WorkTask(
                    "clear-intake",
                    "Очистить фильтр",
                    new GridPosition(6, 3),
                    WorkFunction.Consciousness | WorkFunction.Movement | WorkFunction.Manipulation,
                    90,
                    4f,
                    "инструмент:ключ"),
                new WorkTask(
                    "inspect-relay",
                    "Осмотреть реле",
                    new GridPosition(7, 4),
                    WorkFunction.Consciousness | WorkFunction.Movement,
                    70,
                    2f)
            };

            var simulation = new ColonySimulation(pawns, tasks, map, new ReservationBook(), seed);
            return new ColonyScenario(simulation, map, obstacles);
        }

        private static IWorkPerformanceSource CreateCondition(
            HealthModelSelection model,
            float simple,
            float consciousness,
            float movement,
            float manipulation)
        {
            return model == HealthModelSelection.Simple
                ? new SimpleCondition(simple)
                : new FunctionalCondition(consciousness, movement, manipulation);
        }
    }
}
