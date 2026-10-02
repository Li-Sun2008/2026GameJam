// Generated verbatim from Docs/Contracts/Spotlight.Contracts.cs (2.1.0).
using System;
using System.Collections.Generic;

namespace Spotlight.Contracts
{
    public interface ISimulationDriver
    {
        void Register(ITickSystem system);
        void Advance(float realDeltaSeconds);
        void Reset();
    }
}

