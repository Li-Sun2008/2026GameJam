// Generated verbatim from Docs/Contracts/Spotlight.Contracts.cs (2.1.0).
using System;
using System.Collections.Generic;

namespace Spotlight.Contracts
{
    public sealed class WaveSnapshot
    {
        public int DayIndex;
        public int WaveIndex;
        public int PendingSpawnCount;
        public int PendingSummonCount;
        public int AliveCount;
        public bool AllGroupsFinished;
        public bool Complete;
        public bool FinalBossKilled;
    }
}

