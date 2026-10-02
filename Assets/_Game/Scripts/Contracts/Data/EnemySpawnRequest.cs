// Generated verbatim from Docs/Contracts/Spotlight.Contracts.cs (2.1.0).
using System;
using System.Collections.Generic;

namespace Spotlight.Contracts
{
    public sealed class EnemySpawnRequest
    {
        public string DefinitionId;
        public string SpawnPointId;
        public string PathId;
        public float HpMultiplier = 1f;
        public float DamageMultiplier = 1f;
        public bool IsSummon;
    }
}

