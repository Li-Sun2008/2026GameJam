// Generated verbatim from Docs/Contracts/Spotlight.Contracts.cs (2.1.0).
using System;
using System.Collections.Generic;

namespace Spotlight.Contracts
{
    public interface IDayEventService
    {
        DayEventSnapshot GetSnapshot();
        OperationResult EnsureEvent(int dayIndex, GameMode mode);
        OperationResult TryChoose(CommandContext context, string instanceId, string optionId);
    }
}

