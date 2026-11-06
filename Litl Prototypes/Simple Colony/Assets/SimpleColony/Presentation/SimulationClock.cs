using UnityEngine;
namespace SimpleColony
{
    internal sealed class SimulationClock
    {
        float accumulator;
        public float LastTickMilliseconds { get; private set; }
        public void Reset() { accumulator = 0; }
        public int Advance(World world, float elapsed, int speed, bool paused)
        {
            int executedTicks = 0;
            if (!paused && !world.Lost)
            {
                bool maximum = speed == 0;
                if (!maximum) accumulator = Mathf.Min(accumulator + elapsed * speed, speed);
                var budget = System.Diagnostics.Stopwatch.StartNew();
                while (!world.Lost && (maximum || accumulator >= .1f) && budget.Elapsed.TotalMilliseconds < 8)
                {
                    long before = budget.ElapsedTicks;
                    world.Step();
                    LastTickMilliseconds = (float)((budget.ElapsedTicks - before) * 1000.0 / System.Diagnostics.Stopwatch.Frequency);
                    executedTicks++;
                    if (!maximum) accumulator -= .1f;
                }
            }
            return executedTicks;
        }
    }
}
