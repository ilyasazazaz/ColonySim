using System;
using System.Collections.Generic;

namespace SimpleColony
{
    public sealed class World
    {
        public const int Home = 0;
        readonly List<Node> nodes = new List<Node>();
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
        public int GraphRevision { get; private set; }
        public float Time { get; private set; }
        public bool Lost { get; private set; }
        public ResearchProgress Research { get; } = new ResearchProgress();
        public bool TransportResearched => Research.Level(ResearchType.Transport) > 0;
        public int CargoCapacity => 8 + 4 * Research.Level(ResearchType.Logistics);
        public float RecoveryRate => 5 + 3 * Research.Level(ResearchType.Recovery);
        public float DefensePower => 1 + .5f * Research.Level(ResearchType.Defense);
        public const int RecruitCost = 25;
        public const int ResearchCost = 15;
        public bool CanRecruit => !Lost && pawns.Count < 60 && Stores[Resource.Food] >= RecruitCost;
        public bool CanResearch => CanResearchTechnology(ResearchType.Transport);
        public bool CanResearchTechnology(ResearchType type) => !Lost && Research.CanPurchase(type, Stores);
        public string Event { get; private set; } = "Колония основана";
        public World(int seed)
        {
            Seed = seed; random = new Random(seed); pathfinder = new Pathfinder(this);
            behaviors = new Dictionary<Profession, IProfessionBehavior> {
                { Profession.Gatherer, new GatheringBehavior(Resource.Food) }, { Profession.Defender, new CombatBehavior() },
                { Profession.Scout, new ExplorationBehavior() }, { Profession.Researcher, new ResearchBehavior() },
                { Profession.Builder, new GatheringBehavior(Resource.Matter) }, { Profession.Supplier, new GatheringBehavior(Resource.Charge) },
                { Profession.Medic, new GatheringBehavior(Resource.Medicine) }, { Profession.Stabilizer, new StabilizationBehavior() }
            };
            nodes.Add(new Node(0, 0, 0) { Known = true }); Expand(0);
            int initial = nodes.Count;
            for (int i = 1; i < initial; i++) { nodes[i].Known = true; Expand(i); }
            if (nodes[1].Find(Resource.Food) == null) nodes[1].Add(new Deposit(Resource.Food, 80));
            Stores.Add(Resource.Food, 120);
            AddPawn(Profession.Gatherer); AddPawn(Profession.Gatherer); AddPawn(Profession.Scout); AddPawn(Profession.Defender);
            SpawnThreat();
        }
        void AddPawn(Profession role) { pawns.Add(new Pawn { Id = pawns.Count, Role = role, At = Home }); }
        public bool Recruit(Profession role)
        {
            if (!CanRecruit || !Enum.IsDefined(typeof(Profession), role)) return false;
            if (!Stores.Spend(Resource.Food, RecruitCost)) return false;
            AddPawn(role);
            return true;
        }
        public bool ResearchTransport()
        { return ResearchTechnology(ResearchType.Transport); }
        public bool ResearchTechnology(ResearchType type)
        {
            if (Lost || !Research.Purchase(type, Stores)) return false;
            if (type == ResearchType.Transport) Travel.ImproveTransportation(.15f);
            if (type == ResearchType.Rations) Travel.FoodMultiplier = 1 - .2f * Research.Level(type);
            return true;
        }
        void Connect(int a, int b) { nodes[a].Link(b); nodes[b].Link(a); GraphRevision++; }
        void Expand(int id)
        {
            var origin = nodes[id]; if (origin.Expanded) return; origin.Expanded = true;
            int directions = id == Home ? 6 : 3;
            double offset = random.NextDouble() * Math.PI * 2;
            for (int i = 0; i < directions; i++)
            {
                double angle = offset + Math.PI * 2 * i / directions;
                float x = origin.X + (float)Math.Cos(angle) * 1.8f, y = origin.Y + (float)Math.Sin(angle) * 1.8f;
                Node nearby = nodes.Find(n => n.Id != id && (n.X - x) * (n.X - x) + (n.Y - y) * (n.Y - y) < 1.3f);
                if (nearby != null) { Connect(id, nearby.Id); continue; }
                var node = new Node(nodes.Count, x, y);
                int first = random.Next(6); node.Add(new Deposit((Resource)first, 35 + random.Next(40)));
                if (random.NextDouble() < .65) node.Add(new Deposit((Resource)((first + 1 + random.Next(5)) % 6), 25 + random.Next(35)));
                nodes.Add(node); Connect(id, node.Id);
            }
        }
        public void Discover(int id) { nodes[id].Known = true; Expand(id); GraphRevision++; Event = "Открыта территория"; }
        public bool Frontier(int id) { foreach (int next in nodes[id].Links) if (nodes[next].Known) return true; return false; }
        public int Edges() { int count = 0; foreach (var node in nodes) count += node.Links.Count; return count / 2; }
        public int Path(int start, int goal, bool fighter) { var route = pathfinder.Find(start, goal, fighter); return route == null ? -1 : route.Count == 0 ? start : route[0]; }
        public bool Convert(int pawnId, Profession role)
        {
            if (Lost || pawnId < 0 || pawnId >= pawns.Count || !Enum.IsDefined(typeof(Profession), role)) return false;
            var pawn = pawns[pawnId];
            if (pawn.Health <= 0 || (pawn.PendingRole ?? pawn.Role) == role) return false;
            pawn.PendingRole = pawn.Role == role ? (Profession?)null : role;
            Release(pawn);
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
            if (Lost || role == Profession.Unemployed || !Enum.IsDefined(typeof(Profession), role) || (change != 1 && change != 3 && change != -1)) return false;
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
        public void SetPriority(int id) { if (id != Home && nodes[id].Known) nodes[id].Priority = (nodes[id].Priority + 1) % 4; }
        public void ToggleAccess(int id) { if (id != Home && nodes[id].Known) nodes[id].Blocked = !nodes[id].Blocked; }
        void Release(Pawn pawn) { pawn.Target = -1; pawn.Work = 0; }
        int Reservations(int id) { int count = 0; foreach (var pawn in pawns) if (pawn.Health > 0 && pawn.Target == id) count++; return count; }
        bool DefenseOccupied(int nodeId)
        {
            foreach (var pawn in pawns)
                if (pawn.Role == Profession.Defender && pawn.Health > 0 && !pawn.PendingRole.HasValue && pawn.Target == nodeId) return true;
            return false;
        }
        void SelectWork(Pawn pawn)
        {
            float best = float.MaxValue;
            foreach (var node in nodes)
            {
                if (node.Id == Home || node.Priority == 0 || !Travel.Accessible(node, pawn.Role == Profession.Defender) || !behaviors[pawn.Role].CanWork(this, node)) continue;
                // An assignment reserves the defense job immediately, including travel time.
                if (pawn.Role == Profession.Defender && DefenseOccupied(node.Id)) continue;
                float score = (Travel.Length(nodes[pawn.At], node) + Reservations(node.Id) * 4) / node.Priority;
                if (score >= best) continue;
                var route = pathfinder.Find(pawn.At, node.Id, pawn.Role == Profession.Defender);
                if (route == null) continue;
                float food = 0, energy = 0; int previous = pawn.At;
                foreach (int next in route) { food += Travel.FoodCost(nodes[previous], nodes[next]); energy += Travel.EnergyCost(nodes[previous], nodes[next]); previous = next; }
                var returnRoute = pathfinder.Find(node.Id, Home, pawn.Role == Profession.Defender);
                if (returnRoute == null) continue;
                previous = node.Id;
                foreach (int next in returnRoute) { food += Travel.FoodCost(nodes[previous], nodes[next]); energy += Travel.EnergyCost(nodes[previous], nodes[next]); previous = next; }
                if (pawn.Food < food + 20 || pawn.Energy < energy + 15) continue;
                best = score; pawn.Target = node.Id;
            }
        }
        void Go(Pawn pawn, int goal)
        {
            int next = Path(pawn.At, goal, pawn.Role == Profession.Defender);
            if (next < 0 || next == pawn.At) { Release(pawn); pawn.State = Activity.Idle; return; }
            float food = Travel.FoodCost(nodes[pawn.At], nodes[next]), energy = Travel.EnergyCost(nodes[pawn.At], nodes[next]);
            if (pawn.Food < food || pawn.Energy < energy) { Release(pawn); pawn.State = Activity.Resting; return; }
            pawn.Food -= food; pawn.Energy -= energy; pawn.Next = next; pawn.Progress = 0; pawn.State = Activity.Moving;
        }
        void SpawnThreat()
        {
            var distance = new float[nodes.Count]; var used = new bool[nodes.Count];
            for (int i = 1; i < distance.Length; i++) distance[i] = float.MaxValue;
            for (int step = 0; step < nodes.Count; step++)
            {
                int current = -1;
                for (int i = 0; i < nodes.Count; i++) if (!used[i] && (current < 0 || distance[i] < distance[current])) current = i;
                if (current < 0 || distance[current] == float.MaxValue) break;
                used[current] = true;
                foreach (int next in nodes[current].Links) distance[next] = Math.Min(distance[next], distance[current] + Travel.Length(nodes[current], nodes[next]));
            }
            int farthest = 1;
            for (int i = 1; i < distance.Length; i++) if (distance[i] > distance[farthest]) farthest = i;
            nodes[farthest].Threat += 6; Event = "На границе появился очаг";
        }
        public void Step() => Step(.1f);
        public void Step(float dt)
        {
            if (Lost || dt <= 0) return;
            Tick++; Time += dt;
            foreach (var node in nodes)
            {
                node.Shield = Math.Max(0, node.Shield - dt);
                foreach (var deposit in node.Deposits)
                {
                    if (deposit.Type == Resource.Food || deposit.Type == Resource.Charge) deposit.Regenerate(dt * .025f);
                    if (node.Threat > 0) deposit.Take(node.Threat * dt * .002f);
                }
            }
            if (Tick % 900 == 0) SpawnThreat();
            if (Tick % 100 == 0)
            {
                var spreading = new float[nodes.Count];
                foreach (var node in nodes)
                {
                    if (node.Threat <= 0) continue;
                    node.Threat = Math.Min(35, node.Threat + .5f);
                    if (node.Threat > 10 && node.Shield <= 0)
                    { int next = node.Links[random.Next(node.Links.Count)]; if (nodes[next].Shield <= 0) spreading[next] += 1; }
                }
                for (int i = 0; i < nodes.Count; i++) nodes[i].Threat += spreading[i];
            }
            foreach (var pawn in pawns) UpdatePawn(pawn, dt);
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
        void UpdatePawn(Pawn pawn, float dt)
        {
            if (pawn.Health <= 0) { Release(pawn); pawn.State = Activity.Dead; return; }
            pawn.Food = Math.Max(0, pawn.Food - dt * .08f);
            if (pawn.Food <= 0) pawn.Health = Math.Max(0, pawn.Health - dt * .6f);
            if (pawn.Health <= 0) { Release(pawn); pawn.State = Activity.Dead; return; }
            if (pawn.Next >= 0)
            {
                pawn.Progress += dt * .18f * Travel.SpeedMultiplier / Travel.Length(nodes[pawn.At], nodes[pawn.Next]);
                if (pawn.Progress >= 1) { pawn.At = pawn.Next; pawn.Next = -1; pawn.Progress = 0; }
                return;
            }
            if (pawn.At == Home)
            {
                if (pawn.Cargo > 0) { Stores.Add(pawn.CargoType, pawn.Cargo); pawn.Cargo = 0; }
                if (pawn.PendingRole.HasValue) { pawn.Role = pawn.PendingRole.Value; pawn.PendingRole = null; Release(pawn); }
                if (pawn.Food < 95 && Stores.Spend(Resource.Food, dt)) pawn.Food = Math.Min(100, pawn.Food + dt * 12);
                if (pawn.Health < 100 && Stores.Spend(Resource.Medicine, dt * .1f)) pawn.Health = Math.Min(100, pawn.Health + dt);
                if (pawn.Energy < 98 || (pawn.Food < 90 && Stores[Resource.Food] > 0)) { pawn.Energy = Math.Min(100, pawn.Energy + dt * RecoveryRate); pawn.State = Activity.Resting; Release(pawn); return; }
            }
            if (pawn.State == Activity.Resting && pawn.At != Home) pawn.Energy = Math.Min(100, pawn.Energy + dt);
            if (pawn.Role == Profession.Unemployed && !pawn.PendingRole.HasValue && pawn.Cargo <= 0)
            {
                Release(pawn);
                pawn.State = Activity.Idle;
                if (pawn.At != Home) Go(pawn, Home);
                return;
            }
            if (pawn.Cargo > 0 || pawn.PendingRole.HasValue || pawn.Food < 30 || pawn.Energy < 25 || pawn.Health < 35 || (nodes[pawn.At].Threat > 0 && pawn.Role != Profession.Defender))
            { Release(pawn); Go(pawn, Home); return; }
            if (pawn.Target >= 0 && (!behaviors[pawn.Role].CanWork(this, nodes[pawn.Target]) || nodes[pawn.Target].Priority == 0 || !Travel.Accessible(nodes[pawn.Target], pawn.Role == Profession.Defender))) Release(pawn);
            if (pawn.Target < 0) SelectWork(pawn);
            if (pawn.Target < 0)
            {
                if (pawn.Role == Profession.Defender)
                {
                    pawn.State = Activity.Guarding;
                    return;
                }
                pawn.State = Activity.Idle;
                if (pawn.At != Home) Go(pawn, Home);
                return;
            }
            if (pawn.At != pawn.Target) { Go(pawn, pawn.Target); return; }
            pawn.State = Activity.Working; pawn.Work += dt;
            if (behaviors[pawn.Role].Work(this, pawn, nodes[pawn.At], dt)) Release(pawn);
        }
    }
}
