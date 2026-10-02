// Generated verbatim from Docs/Contracts/Spotlight.Contracts.cs (2.1.0).
using System;
using System.Collections.Generic;

namespace Spotlight.Contracts
{
    public interface IStatusService
    {
        IReadOnlyList<GaugeSnapshot> GetGauges(EntityId actor);
        IReadOnlyList<StatusSnapshot> GetStatuses(EntityId actor);
        CombatModifiers GetModifiers(EntityId actor);
        OperationResult EnqueueGauge(EntityId source, EntityId target, ElementAmounts amounts);
        OperationResult EnqueueStatus(StatusApplyRequest request);
        void ClearTransient(EntityId actor);
    }
}

