using System.Linq;
using ColonySim.Domain;
using NUnit.Framework;

namespace ColonySim.Tests.EditMode
{
    public sealed class ColonySimulationTests
    {
        private static readonly WorkFunction PhysicalWork =
            WorkFunction.Consciousness | WorkFunction.Movement | WorkFunction.Manipulation;

        [Test]
        public void SimpleCondition_ImplementsMinimalPerformanceContract()
        {
            var available = new SimpleCondition(0.5f).Evaluate(PhysicalWork);
            var unavailable = new SimpleCondition(0f).Evaluate(PhysicalWork);

            Assert.That(available.Allowed, Is.True);
            Assert.That(available.Speed, Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(unavailable.Allowed, Is.False);
            Assert.That(unavailable.Reason, Is.Not.Empty);
        }

        [Test]
        public void FunctionalCondition_ImplementsMinimalPerformanceContract()
        {
            var condition = new FunctionalCondition(1f, 0.6f, 0.4f);

            var available = condition.Evaluate(PhysicalWork);
            condition.Manipulation = 0f;
            var unavailable = condition.Evaluate(PhysicalWork);

            Assert.That(available.Allowed, Is.True);
            Assert.That(available.Speed, Is.EqualTo(0.4f).Within(0.0001f));
            Assert.That(unavailable.Allowed, Is.False);
            Assert.That(unavailable.Reason, Does.Contain("манипулировать"));
        }

        [Test]
        public void FunctionalStateChange_PredictablyChangesWorkDuration()
        {
            var condition = new FunctionalCondition(1f, 1f, 1f);
            var fullSpeed = condition.Evaluate(PhysicalWork).Speed;

            condition.Movement = 0.25f;
            var impairedSpeed = condition.Evaluate(PhysicalWork).Speed;

            Assert.That(fullSpeed, Is.EqualTo(1f));
            Assert.That(impairedSpeed, Is.EqualTo(0.25f));
            Assert.That(4f / impairedSpeed, Is.EqualTo(16f));
        }

        [Test]
        public void UnavailablePawn_DoesNotReceiveIncompatibleTask()
        {
            var pawn = new Pawn("pawn", "Пешка", new GridPosition(0, 0), new SimpleCondition(0f));
            var task = new WorkTask("task", "Работа", new GridPosition(1, 0), PhysicalWork, 1, 1f);
            var simulation = CreateSimulation(new[] { pawn }, new[] { task });

            simulation.Step();

            Assert.That(pawn.Assignment, Is.Null);
            Assert.That(task.AssignedPawnId, Is.Null);
            Assert.That(pawn.Status, Does.Contain("не позволяет"));
        }

        [Test]
        public void SharedResource_IsReservedByOnlyOneAssignment()
        {
            var pawns = new[]
            {
                new Pawn("a", "А", new GridPosition(0, 0), new SimpleCondition(1f)),
                new Pawn("b", "Б", new GridPosition(0, 1), new SimpleCondition(1f))
            };
            var tasks = new[]
            {
                new WorkTask("one", "Первая", new GridPosition(2, 0), PhysicalWork, 2, 3f, "tool"),
                new WorkTask("two", "Вторая", new GridPosition(2, 1), PhysicalWork, 1, 3f, "tool")
            };
            var reservations = new ReservationBook();
            var simulation = new ColonySimulation(
                pawns,
                tasks,
                new GridNavigationMap(3, 2, null),
                reservations,
                10);

            simulation.Step();

            Assert.That(tasks.Count(task => task.AssignedPawnId != null), Is.EqualTo(1));
            Assert.That(reservations.OwnerOf("tool"), Is.Not.Null);
            Assert.That(tasks.Single(task => task.AssignedPawnId == null).Status, Does.Contain("закреплён"));
        }

        [Test]
        public void BlockedTarget_ProducesExplicitPathFailure()
        {
            var pawn = new Pawn("pawn", "Пешка", new GridPosition(0, 0), new SimpleCondition(1f));
            var task = new WorkTask("task", "Работа", new GridPosition(1, 1), PhysicalWork, 1, 1f);
            var map = new GridNavigationMap(
                2,
                2,
                new[] { new GridPosition(1, 0), new GridPosition(0, 1) });
            var simulation = new ColonySimulation(new[] { pawn }, new[] { task }, map, new ReservationBook(), 1);

            simulation.Step();

            Assert.That(pawn.Assignment, Is.Null);
            Assert.That(pawn.Status, Does.Contain("нет пути"));
            Assert.That(task.Status, Does.Contain("нет пути"));
        }

        [Test]
        public void SameScenarioAndSeed_ProduceSameDomainResult()
        {
            var first = ColonyScenarioFactory.Create(HealthModelSelection.Functional, 123).Simulation;
            var second = ColonyScenarioFactory.Create(HealthModelSelection.Functional, 123).Simulation;

            for (var step = 0; step < 24; step++)
            {
                first.Step();
                second.Step();
            }

            Assert.That(first.CreateDeterministicSnapshot(), Is.EqualTo(second.CreateDeterministicSnapshot()));
        }

        [Test]
        public void ModelReplacement_ChangesBehaviorWithoutChangingWorkConsumer()
        {
            var simple = ColonyScenarioFactory.Create(HealthModelSelection.Simple, 123).Simulation;
            var functional = ColonyScenarioFactory.Create(HealthModelSelection.Functional, 123).Simulation;

            simple.Step();
            functional.Step();

            var simpleCora = simple.Pawns.Single(pawn => pawn.Id == "cora");
            var functionalCora = functional.Pawns.Single(pawn => pawn.Id == "cora");

            Assert.That(simpleCora.Performance.Evaluate(PhysicalWork).Allowed, Is.True);
            Assert.That(functionalCora.Performance.Evaluate(PhysicalWork).Allowed, Is.False);
            Assert.That(simple.GetType(), Is.EqualTo(functional.GetType()));
        }

        private static ColonySimulation CreateSimulation(Pawn[] pawns, WorkTask[] tasks)
        {
            return new ColonySimulation(
                pawns,
                tasks,
                new GridNavigationMap(3, 3, null),
                new ReservationBook(),
                1);
        }
    }
}
