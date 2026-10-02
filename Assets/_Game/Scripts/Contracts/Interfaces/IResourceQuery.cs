// Generated verbatim from Docs/Contracts/Spotlight.Contracts.cs (2.1.0).
using System;
using System.Collections.Generic;

namespace Spotlight.Contracts
{
    public interface IResourceQuery
    {
        long GetQuantity(ResourceBucket bucket, string definitionId);
        IReadOnlyList<ResourceAmount> GetSnapshot(ResourceBucket bucket);
    }
}

