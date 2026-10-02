using System;
using System.Collections.Generic;
using Spotlight.Contracts;

namespace Spotlight.Core.Clock
{
    public sealed class SimulationDriver : ISimulationDriver
    {
        private readonly List<ITickSystem> systems = new List<ITickSystem>();
        private readonly GameClock clock;
        private readonly Func<FlowSnapshot> flow;
        private readonly float fixedDelta;
        private double accumulated;
        private static readonly TickStage[] OrderedStages = (TickStage[])Enum.GetValues(typeof(TickStage));
        public int MaxTicksPerFrame = 240;
        public SimulationDriver(GameClock clock, Func<FlowSnapshot> flow, int ticksPerSecond)
        {
            if (ticksPerSecond <= 0) throw new ArgumentOutOfRangeException("ticksPerSecond");
            this.clock = clock; this.flow = flow; fixedDelta = 1f / ticksPerSecond;
        }
        public void Register(ITickSystem system)
        {
            if (system == null) throw new ArgumentNullException("system");
            if (systems.Contains(system)) throw new InvalidOperationException("Tick system already registered.");
            systems.Add(system);
        }
        public void Advance(float realDeltaSeconds)
        {
            if (float.IsNaN(realDeltaSeconds) || float.IsInfinity(realDeltaSeconds) || realDeltaSeconds < 0) return;
            ClockSnapshot snapshot = clock.GetSnapshot();
            if (snapshot.IsPaused || flow().Phase != GamePhase.Night) return;
            accumulated += realDeltaSeconds * (int)snapshot.Speed;
            int count = 0;
            while (accumulated + 1e-9 >= fixedDelta && count < MaxTicksPerFrame && !clock.GetSnapshot().IsPaused && flow().Phase == GamePhase.Night)
            {
                accumulated -= fixedDelta; if (accumulated < 0) accumulated = 0;
                clock.AdvanceTick(fixedDelta);
                foreach (TickStage stage in OrderedStages)
                {
                    for (int module = 0; module <= (int)ModuleId.Presentation; module++)
                    {
                        foreach (ITickSystem system in systems)
                        {
                            if ((int)system.Module != module || !Contains(system.Stages, stage)) continue;
                            system.Tick(clock.Step(fixedDelta, stage));
                        }
                    }
                }
                count++;
            }
        }
        public void Reset() { accumulated = 0; clock.Reset(); }
        private static bool Contains(IReadOnlyList<TickStage> stages, TickStage stage)
        { if (stages == null) return false; for (int i = 0; i < stages.Count; i++) if (stages[i] == stage) return true; return false; }
    }
}

