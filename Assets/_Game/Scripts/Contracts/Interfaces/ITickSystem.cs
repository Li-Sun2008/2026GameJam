// Generated verbatim from Docs/Contracts/Spotlight.Contracts.cs (2.1.0).
using System;
using System.Collections.Generic;

namespace Spotlight.Contracts
{
    public interface ITickSystem
    {
        ModuleId Module { get; }
        IReadOnlyList<TickStage> Stages { get; }
        void Tick(TickContext context);
    }
}

