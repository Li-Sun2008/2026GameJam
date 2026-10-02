// Generated verbatim from Docs/Contracts/Spotlight.Contracts.cs (2.1.0).
using System;
using System.Collections.Generic;

namespace Spotlight.Contracts
{
    public sealed class ProjectileSpawnRequest
    {
        public EntityId Source;
        public EntityId InitialTarget;
        public string DefinitionId;
        public WorldPoint Origin;
        public WorldPoint AimPoint;
        public float PhysicalDamage;
        public ElementAmounts ElementDamage;
        public ElementAmounts Gauge;
        public DamageOrigin DamageOrigin;
    }
}

