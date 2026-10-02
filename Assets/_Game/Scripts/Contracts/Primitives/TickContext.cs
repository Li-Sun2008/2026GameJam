// Generated verbatim from Docs/Contracts/Spotlight.Contracts.cs (2.1.0).
using System;
using System.Collections.Generic;

namespace Spotlight.Contracts
{
    public struct TickContext
    {
        public readonly long TickIndex;
        public readonly double GameTime;
        public readonly float DeltaSeconds;
        public readonly TickStage Stage;
        public TickContext(long tickIndex, double gameTime, float deltaSeconds, TickStage stage)
        { TickIndex = tickIndex; GameTime = gameTime; DeltaSeconds = deltaSeconds; Stage = stage; }
    }
}

