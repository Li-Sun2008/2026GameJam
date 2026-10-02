// Generated verbatim from Docs/Contracts/Spotlight.Contracts.cs (2.1.0).
using System;
using System.Collections.Generic;

namespace Spotlight.Contracts
{
    public sealed class ReactionDefinition
    {
        public string Id;
        public bool Enabled;
        public ReactionContext Context;
        public ElementType A;
        public ElementType B;
        public int ConsumeA;
        public int ConsumeB;
        public int Priority;
        public IReadOnlyList<EffectSpec> Effects;
    }
}

