// Generated verbatim from Docs/Contracts/Spotlight.Contracts.cs (2.1.0).
using System;
using System.Collections.Generic;

namespace Spotlight.Contracts
{
    public interface IWaveService
    {
        WaveSnapshot GetSnapshot();
        OperationResult StartNight(int dayIndex, GameMode mode);
        OperationResult ScheduleSummons(EntityId owner, IReadOnlyList<SpawnGroupDefinition> groups);
        void CancelSummons(EntityId owner);
        void Stop();
    }
}

