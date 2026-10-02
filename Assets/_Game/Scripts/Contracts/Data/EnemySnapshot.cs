// Generated verbatim from Docs/Contracts/Spotlight.Contracts.cs (2.1.0).
using System;
using System.Collections.Generic;

namespace Spotlight.Contracts
{
    public sealed class EnemySnapshot
    {
        public EntityId ActorId;
        public string DefinitionId;
        public string PathId;
        public MovementKind Movement;
        public int PathSegment;
        public float SegmentProgress;
        public bool IsSummon;
    }
}

