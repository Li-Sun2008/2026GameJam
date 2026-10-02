// Generated verbatim from Docs/Contracts/Spotlight.Contracts.cs (2.1.0).
using System;
using System.Collections.Generic;

namespace Spotlight.Contracts
{
    public sealed class WaveDefinition
    {
        public string Id;
        public int DayIndex;
        public int WaveIndex;
        public float DelayAfterPreviousWaveSeconds;
        public IReadOnlyList<SpawnGroupDefinition> Groups;
    }
}

