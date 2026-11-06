using System;
using System.Collections.Generic;

namespace SimpleColony
{
    public sealed class Pawn
    {
        public int Id { get; private set; }
        public Profession Role { get; private set; }
        public Profession? PendingRole { get; private set; }
        public int At { get; private set; }
        public int Next { get; private set; } = -1;
        public int Target { get; private set; } = -1;
        public float Progress { get; private set; }
        public float Food { get; private set; } = 100;
        public float Energy { get; private set; } = 100;
        public float Health { get; private set; } = 100;
        public float Cargo { get; private set; }
        public Resource CargoType { get; private set; }
        public Activity State { get; private set; }
        internal float Work { get; private set; }

        internal Pawn(int id, Profession role) { Id = id; Role = role; }
        internal void Assign(int target) { Target = target; }
        internal void Release() { Target = -1; Work = 0; }
        internal void ConvertTo(Profession role)
        {
            PendingRole = Role == role ? (Profession?)null : role;
            Release();
        }
        internal void Load(Resource resource, float amount) { CargoType = resource; Cargo = amount; }
        internal void TakeDamage(float amount) { Health = Math.Max(0, Health - amount); }
        void Go(World world, int goal)
        {
            int next = world.Path(At, goal, Role == Profession.Defender);
            if (next < 0 || next == At) { Release(); State = Activity.Idle; return; }
            float food = world.Travel.FoodCost(world.Nodes[At], world.Nodes[next]), energy = world.Travel.EnergyCost(world.Nodes[At], world.Nodes[next]);
            if (Food < food || Energy < energy) { Release(); State = Activity.Resting; return; }
            Food -= food; Energy -= energy; Next = next; Progress = 0; State = Activity.Moving;
        }
        internal void Step(World world, JobCoordinator jobs, IReadOnlyDictionary<Profession, IProfessionBehavior> behaviors, float dt)
        {
            if (Health <= 0) { Release(); State = Activity.Dead; return; }
            Food = Math.Max(0, Food - dt * .08f);
            if (Food <= 0) Health = Math.Max(0, Health - dt * .6f);
            if (Health <= 0) { Release(); State = Activity.Dead; return; }
            if (Next >= 0)
            {
                Progress += dt * .18f * world.Travel.SpeedMultiplier / world.Travel.Length(world.Nodes[At], world.Nodes[Next]);
                if (Progress >= 1) { At = Next; Next = -1; Progress = 0; }
                return;
            }
            if (At == World.Home)
            {
                if (Cargo > 0) { world.Stores.Add(CargoType, Cargo); Cargo = 0; }
                if (PendingRole.HasValue) { Role = PendingRole.Value; PendingRole = null; Release(); }
                if (Food < 95 && world.Stores.Spend(Resource.Food, dt)) Food = Math.Min(100, Food + dt * 12);
                if (Health < 100 && world.Stores.Spend(Resource.Medicine, dt * .1f)) Health = Math.Min(100, Health + dt);
                if (Energy < 98 || (Food < 90 && world.Stores[Resource.Food] > 0)) { Energy = Math.Min(100, Energy + dt * world.RecoveryRate); State = Activity.Resting; Release(); return; }
            }
            if (State == Activity.Resting && At != World.Home) Energy = Math.Min(100, Energy + dt);
            if (!behaviors.ContainsKey(Role) && !PendingRole.HasValue && Cargo <= 0)
            {
                Release();
                State = Activity.Idle;
                if (At != World.Home) Go(world, World.Home);
                return;
            }
            if (Cargo > 0 || PendingRole.HasValue || Food < 30 || Energy < 25 || Health < 35 || (world.Nodes[At].Threat > 0 && Role != Profession.Defender))
            { Release(); Go(world, World.Home); return; }
            if (Target >= 0 && (!behaviors[Role].CanWork(world, world.Nodes[Target]) || world.Nodes[Target].Priority == 0 || !world.Travel.Accessible(world.Nodes[Target], Role == Profession.Defender))) Release();
            if (Target < 0) jobs.SelectWork(this);
            if (Target < 0)
            {
                if (Role == Profession.Defender)
                {
                    State = Activity.Guarding;
                    return;
                }
                State = Activity.Idle;
                if (At != World.Home) Go(world, World.Home);
                return;
            }
            if (At != Target) { Go(world, Target); return; }
            State = Activity.Working; Work += dt;
            if (behaviors[Role].Work(world, this, world.Nodes[At], dt)) Release();
        }
    }
}
