// Generated verbatim from Docs/Contracts/Spotlight.Contracts.cs (2.1.0).
using System;
using System.Collections.Generic;

namespace Spotlight.Contracts
{
    public struct ResourceAmount
    {
        public readonly ResourceBucket Bucket;
        public readonly string DefinitionId;
        public readonly long Amount;
        public ResourceAmount(ResourceBucket bucket, string definitionId, long amount)
        { Bucket = bucket; DefinitionId = definitionId; Amount = amount; }
    }
}

