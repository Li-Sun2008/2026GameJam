// Generated verbatim from Docs/Contracts/Spotlight.Contracts.cs (2.1.0).
using System;
using System.Collections.Generic;

namespace Spotlight.Contracts
{
    public sealed class ItemDefinition
    {
        public string Id;
        public string DisplayNameKey;
        public string IconKey;
        public IReadOnlyList<GamePhase> AllowedPhases;
        public string StackingGroup;
        public float ActiveDurationSeconds;
        public IReadOnlyList<EffectSpec> Effects;
    }
}

