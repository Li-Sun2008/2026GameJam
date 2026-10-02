// Generated verbatim from Docs/Contracts/Spotlight.Contracts.cs (2.1.0).
using System;
using System.Collections.Generic;

namespace Spotlight.Contracts
{
    public interface IWorldQuery
    {
        EntityId SpringId { get; }
        bool TryGetActor(EntityId id, out ActorSnapshot snapshot);
        IReadOnlyList<ActorSnapshot> GetActors(ActorKind kind);
        IReadOnlyList<ActorSnapshot> QueryRadius(WorldPoint center, float radius, ActorKind kind);
        IReadOnlyList<ActorSnapshot> TraceActors(WorldPoint from, WorldPoint to, float radius, ActorKind kind);
    }
}

