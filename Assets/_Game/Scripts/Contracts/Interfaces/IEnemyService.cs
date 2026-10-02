// Generated verbatim from Docs/Contracts/Spotlight.Contracts.cs (2.1.0).
using System;
using System.Collections.Generic;

namespace Spotlight.Contracts
{
    public interface IEnemyService
    {
        OperationResult TrySpawn(EnemySpawnRequest request, out EntityId actor);
        IReadOnlyList<EnemySnapshot> GetSnapshot();
        void DespawnAll(DeathReason reason);
    }
}

