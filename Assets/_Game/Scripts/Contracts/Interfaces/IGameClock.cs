// Generated verbatim from Docs/Contracts/Spotlight.Contracts.cs (2.1.0).
using System;
using System.Collections.Generic;

namespace Spotlight.Contracts
{
    public interface IGameClock
    {
        ClockSnapshot GetSnapshot();
        OperationResult TrySetSpeed(CommandContext context, GameSpeed speed);
        Guid AcquirePause(PauseReason reason);
        void ReleasePause(Guid handle);
    }
}

