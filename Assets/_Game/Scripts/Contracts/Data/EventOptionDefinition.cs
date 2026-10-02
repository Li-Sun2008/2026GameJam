// Generated verbatim from Docs/Contracts/Spotlight.Contracts.cs (2.1.0).
using System;
using System.Collections.Generic;

namespace Spotlight.Contracts
{
    public sealed class EventOptionDefinition
    {
        public string Id;
        public string Text;
        public IReadOnlyList<ResourceAmount> Cost;
        public IReadOnlyList<EffectSpec> Effects;
    }
}

