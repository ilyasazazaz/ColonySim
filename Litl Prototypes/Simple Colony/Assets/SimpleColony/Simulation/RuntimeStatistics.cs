using System;
using System.Collections.Generic;

namespace SimpleColony
{
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
