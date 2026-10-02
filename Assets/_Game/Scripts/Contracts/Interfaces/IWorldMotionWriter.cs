// Generated verbatim from Docs/Contracts/Spotlight.Contracts.cs (2.1.0).
using System;
using System.Collections.Generic;

namespace Spotlight.Contracts
{
    public interface IWorldMotionWriter
    {
        OperationResult TrySetPosition(EntityId actor, WorldPoint position);
        OperationResult TrySetTargetable(EntityId actor, bool targetable);
    }
}

