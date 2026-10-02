// Generated verbatim from Docs/Contracts/Spotlight.Contracts.cs (2.1.0).
using System;
using System.Collections.Generic;

namespace Spotlight.Contracts
{
    public struct EntityId : IEquatable<EntityId>
    {
        public readonly long Value;
        public EntityId(long value) { Value = value; }
        public bool Equals(EntityId other) { return Value == other.Value; }
        public override bool Equals(object obj) { return obj is EntityId && Equals((EntityId)obj); }
        public override int GetHashCode() { return Value.GetHashCode(); }
    }
}

