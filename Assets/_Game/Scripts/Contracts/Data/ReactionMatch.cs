// Generated verbatim from Docs/Contracts/Spotlight.Contracts.cs (2.1.0).
using System;
using System.Collections.Generic;

namespace Spotlight.Contracts
{
    public sealed class ReactionMatch
    {
        public string RuleId;
        public IReadOnlyList<long> ConsumedTokenIds;
        public IReadOnlyList<EffectSpec> Effects;
    }
}

