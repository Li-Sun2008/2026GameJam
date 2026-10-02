// Generated verbatim from Docs/Contracts/Spotlight.Contracts.cs (2.1.0).
using System;
using System.Collections.Generic;

namespace Spotlight.Contracts
{
    public struct RandomState
    {
        public readonly RandomStream Stream;
        public readonly uint State;
        public RandomState(RandomStream stream, uint state) { Stream = stream; State = state; }
    }
}

