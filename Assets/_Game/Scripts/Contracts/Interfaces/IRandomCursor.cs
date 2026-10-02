// Generated verbatim from Docs/Contracts/Spotlight.Contracts.cs (2.1.0).
using System;
using System.Collections.Generic;

namespace Spotlight.Contracts
{
    public interface IRandomCursor
    {
        int NextInt(int minInclusive, int maxExclusive);
        float NextUnitFloat();
        RandomState Capture();
    }
}

