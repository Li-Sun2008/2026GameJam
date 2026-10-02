// Generated verbatim from Docs/Contracts/Spotlight.Contracts.cs (2.1.0).
using System;
using System.Collections.Generic;

namespace Spotlight.Contracts
{
    public sealed class BossSkillDefinition
    {
        public string Id;
        public float PeriodSeconds;
        public float FirstDelaySeconds;
        public IReadOnlyList<EffectSpec> Effects;
        public IReadOnlyList<SpawnGroupDefinition> Summons;
        public string ProjectileId;
        public float ProjectileDamage;
    }
}

