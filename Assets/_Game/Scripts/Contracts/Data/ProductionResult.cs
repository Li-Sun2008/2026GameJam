// Generated verbatim from Docs/Contracts/Spotlight.Contracts.cs (2.1.0).
using System;
using System.Collections.Generic;

namespace Spotlight.Contracts
{
    public sealed class ProductionResult
    {
        public string SettlementId;
        public long ExpectedRevision;
        public int CompletedNight;
        public IReadOnlyList<BoardMutation> Board;
        public IReadOnlyList<ResourceAmount> HandDeltas;
        public RandomState NextRandom;
        public IReadOnlyList<string> AppliedRuleIds;
    }
}

