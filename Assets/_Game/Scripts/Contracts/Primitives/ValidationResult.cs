// Generated verbatim from Docs/Contracts/Spotlight.Contracts.cs (2.1.0).
using System;
using System.Collections.Generic;

namespace Spotlight.Contracts
{
    public struct ValidationResult
    {
        public readonly ErrorCode Error;
        public readonly string MessageKey;
        public ValidationResult(ErrorCode error, string messageKey)
        { Error = error; MessageKey = messageKey; }
    }
}

