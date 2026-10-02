// Generated verbatim from Docs/Contracts/Spotlight.Contracts.cs (2.1.0).
using System;
using System.Collections.Generic;

namespace Spotlight.Contracts
{
    public interface IBossService
    {
        void RegisterBoss(EntityId actor);
        void RemoveBoss(EntityId actor);
        void Clear();
    }
}

