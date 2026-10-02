// Generated verbatim from Docs/Contracts/Spotlight.Contracts.cs (2.1.0).
using System;
using System.Collections.Generic;

namespace Spotlight.Contracts
{
    public sealed class ClockSnapshot
    {
        public long TickIndex;
        public double GameTime;
        public GameSpeed Speed;
        public bool IsPaused;
        public IReadOnlyList<PauseReason> Reasons;
    }
}

