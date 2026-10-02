// Generated verbatim from Docs/Contracts/Spotlight.Contracts.cs (2.1.0).
using System;
using System.Collections.Generic;

namespace Spotlight.Contracts
{
    public sealed class DamagePacket
    {
        public string PacketId;
        public EntityId Source;
        public EntityId Target;
        public DamageOrigin Origin;
        public string ReactionId;
        public float PhysicalDamage;
        public ElementAmounts ElementDamage;
        public ElementAmounts Gauge;
        public float DamageMultiplier = 1f;
    }
}

