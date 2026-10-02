// Generated verbatim from Docs/Contracts/Spotlight.Contracts.cs (2.1.0).
using System;
using System.Collections.Generic;

namespace Spotlight.Contracts
{
    public struct CommandContext
    {
        public readonly Guid CommandId;
        public readonly string RunId;
        public readonly long ExpectedRevision;
        public CommandContext(Guid commandId, string runId, long expectedRevision)
        { CommandId = commandId; RunId = runId; ExpectedRevision = expectedRevision; }
    }
}

