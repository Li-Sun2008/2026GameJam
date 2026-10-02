// Generated verbatim from Docs/Contracts/Spotlight.Contracts.cs (2.1.0).
using System;
using System.Collections.Generic;

namespace Spotlight.Contracts
{
    public sealed class SaveReadResult
    {
        public ErrorCode Error;
        public CheckpointData Data;
        public bool UsedBackup;
        public string MessageKey;
    }
}

