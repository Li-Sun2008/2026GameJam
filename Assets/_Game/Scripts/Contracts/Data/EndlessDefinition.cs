// Generated verbatim from Docs/Contracts/Spotlight.Contracts.cs (2.1.0).
using System;
using System.Collections.Generic;

namespace Spotlight.Contracts
{
    public sealed class EndlessDefinition
    {
        public string Id;
        public IReadOnlyList<string> WaveTemplateIds;
        public float HpGrowthPerNight;
        public float DamageGrowthPerNight;
        public int MilestoneNight;
        public IReadOnlyList<ResourceAmount> MilestoneRewards;
    }
}

