// Generated verbatim from Docs/Contracts/Spotlight.Contracts.cs (2.1.0).
using System;
using System.Collections.Generic;

namespace Spotlight.Contracts
{
    public sealed class SpawnGroupDefinition
    {
        public string Id;
        public string EnemyId;
        public int Count;
        public string SpawnPointId;
        public string PathId;
        public float StartDelaySeconds;
        public float SpawnIntervalSeconds;
        public bool ApplyDayGrowth;
        public float HpMultiplier = 1f;
        public float DamageMultiplier = 1f;
    }
}

