// Generated verbatim from Docs/Contracts/Spotlight.Contracts.cs (2.1.0).
using System;
using System.Collections.Generic;

namespace Spotlight.Contracts
{
    public sealed class ProductionRuleDefinition
    {
        public string Id;
        public bool Enabled;
        public ProductionMatch Match;
        public ElementType A;
        public ElementType B;
        public bool ConsumeA;
        public bool ConsumeB;
        public string OutputElementId;
        public int OutputCount;
        public int Priority;
        public bool RandomTieBreak;
    }
}

