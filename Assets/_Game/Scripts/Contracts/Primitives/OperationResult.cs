// Generated verbatim from Docs/Contracts/Spotlight.Contracts.cs (2.1.0).
using System;
using System.Collections.Generic;

namespace Spotlight.Contracts
{
    public struct OperationResult
    {
        public readonly OperationState State;
        public readonly ErrorCode Error;
        public readonly Guid CommandId;
        public readonly long Revision;
        public readonly string MessageKey;
        public OperationResult(OperationState state, ErrorCode error, Guid commandId, long revision, string messageKey)
        { State = state; Error = error; CommandId = commandId; Revision = revision; MessageKey = messageKey; }
    }
}

