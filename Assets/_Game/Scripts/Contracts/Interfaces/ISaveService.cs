// Generated verbatim from Docs/Contracts/Spotlight.Contracts.cs (2.1.0).
using System;
using System.Collections.Generic;

namespace Spotlight.Contracts
{
    public interface ISaveService
    {
        bool HasCheckpoint { get; }
        OperationResult SaveCheckpoint(CommandContext context, SaveReason reason);
        SaveReadResult ReadCheckpoint();
    }
}

