using System;
using System.Collections.Generic;

namespace SimpleColony
{
    public sealed class World
    {
        public const int Home = 0;
        readonly WorldGraph graph;
        readonly JobCoordinator jobs;
        readonly List<MechanicModule> modules = new List<MechanicModule>();
        readonly ResearchModule researchModule;
        IReadOnlyList<Node> nodes => graph.Nodes;
        public Mechanics EnabledMechanics { get; }
        internal void Report(string message) { Event = message; }
        internal void RegisterBehavior(Profession role, IProfessionBehavior behavior) { behaviors.Add(role, behavior); }
        readonly List<Pawn> pawns = new List<Pawn>();
        readonly Random random;
        readonly Pathfinder pathfinder;
        readonly Dictionary<Profession, IProfessionBehavior> behaviors;
        public IReadOnlyList<Node> Nodes => nodes;
        public IReadOnlyList<Pawn> Pawns => pawns;
        public Inventory Stores { get; } = new Inventory();
        public TravelPolicy Travel { get; } = new TravelPolicy();
        public int Seed { get; }
        public int Tick { get; private set; }
        public int Searches { get; internal set; }
        public int GraphRevision => graph.Revision;
        public float Time { get; private set; }
        public bool Lost { get; private set; }
        public ResearchProgress Research { get; }
        public bool TransportResearched => Research.Level(ResearchType.Transport) > 0;
        public int CargoCapacity => 8 + 4 * Research.Level(ResearchType.Logistics);
        public float RecoveryRate => 5 + 3 * Research.Level(ResearchType.Recovery);
        public float DefensePower => 1 + .5f * Research.Level(ResearchType.Defense);
        public const int RecruitCost = 25;
        public const int ResearchCost = 15;
        public bool CanRecruit => !Lost && pawns.Count < 60 && Stores[Resource.Food] >= RecruitCost;
        public bool CanResearch => CanResearchTechnology(ResearchType.Transport);
        public bool CanResearchTechnology(ResearchType type) => researchModule != null && !Lost && Research.CanPurchase(type, Stores);
        public string Event { get; private set; } = "Колония основана";
        public World(int seed, Mechanics mechanics = Mechanics.All)
        {
            if ((mechanics & ~Mechanics.All) != 0) throw new ArgumentOutOfRangeException(nameof(mechanics));
            EnabledMechanics = mechanics;
            Seed = seed; random = new Random(seed); pathfinder = new Pathfinder(this);
            behaviors = new Dictionary<Profession, IProfessionBehavior> {
                { Profession.Gatherer, new GatheringBehavior(Resource.Food) },
                { Profession.Scout, new ExplorationBehavior() },
                { Profession.Builder, new GatheringBehavior(Resource.Matter) }, { Profession.Supplier, new GatheringBehavior(Resource.Charge) },
                { Profession.Medic, new GatheringBehavior(Resource.Medicine) }
            };
            graph = new WorldGraph(random);
            jobs = new JobCoordinator(this, pathfinder, behaviors);
            if ((mechanics & Mechanics.Threats) != 0) modules.Add(new ThreatModule(random));
            if ((mechanics & Mechanics.Research) != 0)
            {
                researchModule = new ResearchModule();
                modules.Add(researchModule);
            }
            Research = researchModule?.Progress ?? new ResearchProgress();
            Stores.Add(Resource.Food, 120);
            AddPawn(Profession.Gatherer); AddPawn(Profession.Gatherer); AddPawn(Profession.Scout); AddPawn((mechanics & Mechanics.Threats) != 0 ? Profession.Defender : Profession.Unemployed);
            foreach (var module in modules) module.Install(this);
        }
        void AddPawn(Profession role) { pawns.Add(new Pawn(pawns.Count, role)); }
        public bool Supports(Profession role) => role == Profession.Unemployed || behaviors.ContainsKey(role);
        public bool Recruit(Profession role)
        {
            if (!CanRecruit || !Supports(role)) return false;
            if (!Stores.Spend(Resource.Food, RecruitCost)) return false;
            AddPawn(role);
            return true;
        }
        public bool ResearchTransport()
        { return ResearchTechnology(ResearchType.Transport); }
        public bool ResearchTechnology(ResearchType type) => researchModule != null && researchModule.Purchase(this, type);
        public void Discover(int id) { graph.Discover(id); Report("Открыта территория"); }
        public bool Frontier(int id) => graph.Frontier(id);
        public int Edges() => graph.Edges();
        public int Path(int start, int goal, bool fighter) { var route = pathfinder.Find(start, goal, fighter); return route == null ? -1 : route.Count == 0 ? start : route[0]; }
        public bool Convert(int pawnId, Profession role)
        {
            if (Lost || pawnId < 0 || pawnId >= pawns.Count || !Supports(role)) return false;
            var pawn = pawns[pawnId];
            if (pawn.Health <= 0 || (pawn.PendingRole ?? pawn.Role) == role) return false;
            pawn.ConvertTo(role);
            return true;
        }
        public int ProfessionCount(Profession role)
        {
            int count = 0;
            foreach (var pawn in pawns)
                if (pawn.Health > 0 && (pawn.PendingRole ?? pawn.Role) == role) count++;
            return count;
        }
        public bool AdjustProfession(Profession role, int change)
        {
            if (Lost || role == Profession.Unemployed || !Supports(role) || (change != 1 && change != 3 && change != -1)) return false;
            Profession source = change > 0 ? Profession.Unemployed : role;
            var candidates = pawns.FindAll(p => p.Health > 0 && (p.PendingRole ?? p.Role) == source);
            int requested = Math.Abs(change);
            if (candidates.Count < requested) return false;
            candidates.Sort((a, b) => {
                int first = a.At == Home && a.Next < 0 ? 0 : 1;
                int second = b.At == Home && b.Next < 0 ? 0 : 1;
                int order = first.CompareTo(second);
                return order == 0 ? a.Id.CompareTo(b.Id) : order;
            });
            for (int i = 0; i < requested; i++) Convert(candidates[i].Id, change > 0 ? role : Profession.Unemployed);
            return true;
        }
        public void SetPriority(int id) { if (id != Home && nodes[id].Known) nodes[id].CyclePriority(); }
        public void ToggleAccess(int id) { if (id != Home && nodes[id].Known) nodes[id].ToggleAccess(); }
        public void Step() => Step(.1f);
        public void Step(float dt)
        {
            if (Lost || dt <= 0 || float.IsInfinity(dt) || float.IsNaN(dt)) return;
            Tick++; Time += dt;
            foreach (var node in nodes)
            {
                node.AgeShield(dt);
                foreach (var deposit in node.Deposits)
                {
                    if (deposit.Type == Resource.Food || deposit.Type == Resource.Charge) deposit.Regenerate(dt * .025f);
                    if (node.Threat > 0) deposit.Take(node.Threat * dt * .002f);
                }
            }
            foreach (var module in modules) module.Step(this, dt);
            foreach (var pawn in pawns) pawn.Step(this, jobs, behaviors, dt);
            if (Tick % 50 == 0 && Stores.Spend(Resource.Matter, 10)) Travel.ImproveTransportation();
            if (Tick % 100 == 0)
            {
                foreach (var node in nodes)
                {
                    var food = node.Find(Resource.Food);
                    if (node.Known && food != null && food.Amount < food.Capacity && Stores.Spend(Resource.Charge, .5f)) { food.Regenerate(3); break; }
                }
            }
            Lost = pawns.TrueForAll(p => p.Health <= 0) || nodes[Home].Threat > 25;
        }
    }
}
