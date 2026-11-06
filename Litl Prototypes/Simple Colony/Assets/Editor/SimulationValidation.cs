using System;
using System.Collections.Generic;
using SimpleColony;

public static class SimulationValidation
{
    static void Require(bool condition, string message)
    { if (!condition) throw new Exception(message); }

    public static void Run()
    {
        for (int seed = 0; seed < 30; seed++)
        {
            var world = new World(seed);
            Require(world.Pawns.Count == 4, "Starting population");
            ValidateGraph(world);
            ValidateFrontierThreat(world);
            int initialCount = world.Nodes.Count;
            // Survey successive frontier territories, exercising expansion beyond a fixed array.
            for (int step = 0; step < 40; step++)
            {
                int candidate = -1;
                for (int i = world.Nodes.Count - 1; i > 0; i--)
                    if (!world.Nodes[i].Known && world.Frontier(i)) { candidate = i; break; }
                Require(candidate >= 0, "Expansion frontier exhausted");
                world.Discover(candidate);
            }
            Require(world.Nodes.Count > initialCount && world.Nodes.Count > 32, "Graph growth");
            ValidateGraph(world);
            Require(world.Convert(0, Profession.Medic), "Conversion command");
            world.Step();
            Require(world.Pawns[0].Role == Profession.Medic, "Conversion on core");
            Require(!world.Convert(0, (Profession)99), "Invalid profession accepted");
            for (int step = 0; step < 4000; step++) world.Step();
            foreach (var node in world.Nodes)
                foreach (var deposit in node.Deposits)
                    Require(!float.IsNaN(deposit.Amount) && deposit.Amount >= 0 && deposit.Amount <= deposit.Capacity, "Invalid deposit");
            foreach (var pawn in world.Pawns)
            {
                Require(pawn.Food >= 0 && pawn.Energy >= 0 && pawn.Health >= 0, "Invalid needs");
                Require(pawn.Next < world.Nodes.Count && pawn.At < world.Nodes.Count, "Invalid location");
                if (pawn.Next >= 0) Require(Contains(world.Nodes[pawn.At].Links, pawn.Next), "Moving off road");
            }
        }
        ValidateDeterminism();
        ValidateDeposits();
        ValidateTravelAndConversion();
        ValidateEconomyCommands();
        ValidateResearchEffects();
        ValidateStatistics();
        ValidateDefenseOccupancy();
        ValidateWorkforce();
    }
    static bool Contains(IReadOnlyList<int> values, int target)
    { foreach (int value in values) if (value == target) return true; return false; }
    static void ValidateGraph(World world)
    {
        var visited = new HashSet<int> { World.Home }; var pending = new Queue<int>(); pending.Enqueue(World.Home);
        while (pending.Count > 0)
        {
            var node = world.Nodes[pending.Dequeue()]; var unique = new HashSet<int>();
            foreach (int next in node.Links)
            {
                Require(next != node.Id && next >= 0 && next < world.Nodes.Count && unique.Add(next), "Invalid edge");
                Require(Contains(world.Nodes[next].Links, node.Id), "One-way edge");
                Require(world.Travel.Length(node, world.Nodes[next]) > 0, "Zero-length edge");
                if (visited.Add(next)) pending.Enqueue(next);
            }
        }
        Require(visited.Count == world.Nodes.Count, "Disconnected graph");
    }
    static void ValidateDeterminism()
    {
        var first = new World(4271); var second = new World(4271);
        for (int i = 0; i < 2000; i++) { first.Step(); second.Step(); }
        Require(first.Nodes.Count == second.Nodes.Count, "Nondeterministic graph");
        for (int i = 0; i < first.Nodes.Count; i++)
            Require(first.Nodes[i].Threat == second.Nodes[i].Threat && first.Nodes[i].X == second.Nodes[i].X, "Nondeterministic state");
        for (int i = 0; i < first.Pawns.Count; i++)
            Require(first.Pawns[i].At == second.Pawns[i].At && first.Pawns[i].Food == second.Pawns[i].Food, "Nondeterministic pawn");
    }
    static void ValidateDeposits()
    {
        var food = new Deposit(Resource.Food, 10); var matter = new Deposit(Resource.Matter, 20);
        Require(food.Take(15) == 10 && food.Amount == 0 && matter.Amount == 20, "Independent depletion");
    }
    static void ValidateFrontierThreat(World world)
    {
        var distances = new float[world.Nodes.Count];
        for (int i = 1; i < distances.Length; i++) distances[i] = float.MaxValue;
        for (int pass = 0; pass < world.Nodes.Count - 1; pass++)
            foreach (var node in world.Nodes)
                foreach (int next in node.Links)
                    distances[next] = Math.Min(distances[next], distances[node.Id] + world.Travel.Length(node, world.Nodes[next]));
        float maximum = 0;
        foreach (float distance in distances) maximum = Math.Max(maximum, distance);
        bool found = false;
        foreach (var node in world.Nodes)
            if (node.Threat > 0) { found = true; Require(Math.Abs(distances[node.Id] - maximum) < .001f, "Threat is not at the farthest graph node"); }
        Require(found, "Missing initial threat");
    }
    static void ValidateTravelAndConversion()
    {
        var world = new World(4271);
        var pawn = world.Pawns[0];
        world.Step();
        Require(pawn.Next >= 0, "Gatherer cannot start working");
        float expectedFood = 100 - .008f - world.Travel.FoodCost(world.Nodes[pawn.At], world.Nodes[pawn.Next]);
        Require(Math.Abs(pawn.Food - expectedFood) < .001f, "Incorrect entry cost");
        float food = pawn.Food, energy = pawn.Energy;
        world.Step();
        Require(Math.Abs(pawn.Food - (food - .008f)) < .001f && pawn.Energy == energy, "Road cost charged twice");
        Require(world.Convert(0, Profession.Medic), "Conversion during journey rejected");
        world.Step();
        Require(pawn.Role == Profession.Gatherer && pawn.PendingRole == Profession.Medic, "Converted away from core");
        for (int step = 0; step < 3000 && pawn.Role != Profession.Medic; step++) world.Step();
        Require(pawn.Role == Profession.Medic && pawn.At == World.Home && !pawn.PendingRole.HasValue, "Failed to convert on return");
    }
    static void ValidateEconomyCommands()
    {
        var world = new World(4271);
        Require(!world.Recruit((Profession)99), "Invalid recruitment accepted");
        Require(world.Stores[Resource.Food] == 120, "Invalid recruitment spent resources");
        for (int i = 0; i < 4; i++) Require(world.Recruit(Profession.Gatherer), "Recruitment failed");
        Require(world.Pawns.Count == 8 && world.Stores[Resource.Food] == 20, "Recruitment accounting");
        Require(!world.Recruit(Profession.Gatherer) && world.Pawns.Count == 8 && world.Stores[Resource.Food] == 20, "Unaffordable recruitment changed state");

        world = new World(4271);
        Require(!world.ResearchTransport() && world.Travel.SpeedMultiplier == 1, "Unaffordable research changed state");
        Require(world.Recruit(Profession.Researcher), "Could not recruit researcher");
        for (int step = 0; step < 2000 && !world.CanResearch; step++) world.Step();
        float knowledge = world.Stores[Resource.Knowledge];
        float speed = world.Travel.SpeedMultiplier;
        Require(world.ResearchTransport(), "Earned research unavailable");
        Require(Math.Abs(world.Stores[Resource.Knowledge] - (knowledge - World.ResearchCost)) < .001f, "Research accounting");
        Require(Math.Abs(world.Travel.SpeedMultiplier - (speed + .15f)) < .001f, "Research effect");
        float remaining = world.Stores[Resource.Knowledge];
        Require(!world.ResearchTransport() && world.Stores[Resource.Knowledge] == remaining, "Research purchased twice");
    }
    static void ValidateResearchEffects()
    {
        foreach (var definition in ResearchProgress.Definitions)
        {
            var world = new World(4271);
            Require(!world.ResearchTechnology((ResearchType)99), "Invalid research accepted");
            // Seed resources only in the fixture, keeping production inventory mutations internal.
            typeof(Inventory).GetMethod("Add", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .Invoke(world.Stores, new object[] { Resource.Knowledge, 1000f });
            float roadFood = world.Travel.FoodCost(world.Nodes[0], world.Nodes[1]);
            for (int level = 1; level <= definition.MaxLevel; level++)
            {
                float knowledge = world.Stores[Resource.Knowledge];
                Require(world.ResearchTechnology(definition.Type), "Research unavailable: " + definition.Name);
                Require(world.Research.Level(definition.Type) == level, "Research level missing");
                Require(Math.Abs(world.Stores[Resource.Knowledge] - knowledge + definition.Cost * level) < .001f, "Research price mismatch");
                switch (definition.Type)
                {
                    case ResearchType.Transport: Require(Math.Abs(world.Travel.SpeedMultiplier - 1 - .15f * level) < .001f, "Transport effect"); break;
                    case ResearchType.Logistics: Require(world.CargoCapacity == 8 + 4 * level, "Logistics effect"); break;
                    case ResearchType.Rations: Require(Math.Abs(world.Travel.FoodCost(world.Nodes[0], world.Nodes[1]) - roadFood * (1 - .2f * level)) < .001f, "Rations effect"); break;
                    case ResearchType.Recovery: Require(world.RecoveryRate == 5 + 3 * level, "Recovery effect"); break;
                    case ResearchType.Defense: Require(world.DefensePower == 1 + .5f * level, "Defense effect"); break;
                }
            }
            float remaining = world.Stores[Resource.Knowledge];
            Require(world.Research.IsCompleted(definition.Type), "Research maximum missing");
            Require(!world.ResearchTechnology(definition.Type) && world.Stores[Resource.Knowledge] == remaining, "Research exceeded maximum");
            Require(!new World(4271).Research.IsCompleted(definition.Type), "Research leaked into another session");
        }
    }
    static void ValidateStatistics()
    {
        var statistics = new RuntimeStatistics();
        for (int frame = 0; frame < 8; frame++) statistics.Record(.125, 2);
        Require(statistics.FramesPerSecond == 8 && statistics.TicksPerSecond == 16, "Incorrect real-time FPS/TPS");
        for (int frame = 0; frame < 8; frame++) statistics.Record(.125, 0);
        Require(statistics.FramesPerSecond == 8 && statistics.TicksPerSecond == 0, "Paused statistics");
        statistics.Reset();
        Require(statistics.FramesPerSecond == 0 && statistics.TicksPerSecond == 0, "Statistics reset");
    }
    static void SetFixtureProperty(object target, string name, object value)
    { target.GetType().GetProperty(name).SetValue(target, value); }
    static int AssignedDefenders(World world, int node)
    {
        int count = 0;
        foreach (var pawn in world.Pawns)
            if (pawn.Health > 0 && pawn.Role == Profession.Defender && pawn.Target == node) count++;
        return count;
    }
    static void ValidateDefenseOccupancy()
    {
        var world = new World(4271);
        for (int i = 0; i < 3; i++) Require(world.Recruit(Profession.Defender), "Defender recruitment");
        foreach (var node in world.Nodes) SetFixtureProperty(node, "Threat", 0f);
        SetFixtureProperty(world.Nodes[1], "Threat", 6f);
        SetFixtureProperty(world.Pawns[4], "At", 3);
        world.Step();
        Require(AssignedDefenders(world, 1) == 1, "Threat attracted multiple defenders");
        Require(world.Pawns[4].At == 3 && world.Pawns[4].Next == -1 && world.Pawns[4].State == Activity.Guarding, "Idle guard abandoned post");
        SetFixtureProperty(world.Nodes[2], "Threat", 6f);
        world.Step();
        Require(AssignedDefenders(world, 1) == 1 && AssignedDefenders(world, 2) == 1, "Threat assignments not distributed");
        Pawn assigned = null;
        foreach (var pawn in world.Pawns) if (pawn.Role == Profession.Defender && pawn.Target == 1) assigned = pawn;
        SetFixtureProperty(assigned, "Health", 0f);
        world.Step();
        Require(AssignedDefenders(world, 1) == 1 && assigned.Target == -1, "Dead defender kept reservation");
        foreach (var pawn in world.Pawns) if (pawn.Health > 0 && pawn.Role == Profession.Defender && pawn.Target == 1) assigned = pawn;
        Require(world.Convert(assigned.Id, Profession.Unemployed), "Defender dismissal");
        world.Step();
        Require(AssignedDefenders(world, 1) == 1, "Dismissed defender kept reservation");
    }
    static void ValidateWorkforce()
    {
        var world = new World(4271);
        for (int i = 0; i < 3; i++) Require(world.Recruit(Profession.Unemployed), "Unemployed recruitment");
        for (int i = 0; i < 10; i++) world.Step();
        for (int i = 4; i < 7; i++) Require(world.Pawns[i].Target == -1 && world.Pawns[i].Next == -1 && world.Pawns[i].Role == Profession.Unemployed, "Unassigned worker started job");
        Require(world.AdjustProfession(Profession.Gatherer, 3), "Assign three workers");
        Require(world.ProfessionCount(Profession.Gatherer) == 5 && world.ProfessionCount(Profession.Unemployed) == 0, "Pending workforce counts");
        Require(!world.AdjustProfession(Profession.Gatherer, 3), "Assigned unavailable workers");
        world.Step();
        for (int i = 4; i < 7; i++) Require(world.Pawns[i].Role == Profession.Gatherer, "Assigned worker did not switch role at core");
        Require(world.AdjustProfession(Profession.Gatherer, -1), "Release worker");
        Require(!world.AdjustProfession(Profession.Medic, 3) && world.ProfessionCount(Profession.Unemployed) == 1, "Partial batch assignment");
        Require(world.AdjustProfession(Profession.Medic, 1), "Reassign returning worker");
        Require(world.ProfessionCount(Profession.Medic) == 1 && world.ProfessionCount(Profession.Unemployed) == 0, "Workforce accounting");
    }
}
