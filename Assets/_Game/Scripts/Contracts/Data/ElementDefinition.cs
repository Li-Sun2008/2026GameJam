// Generated verbatim from Docs/Contracts/Spotlight.Contracts.cs (2.1.0).
using System;
using System.Collections.Generic;

namespace Spotlight.Contracts
{
    public sealed class ElementDefinition
    {
        public string Id;
        public ElementType Element;
        public string DisplayNameKey;
        public string PrefabKey;
        public bool BlocksGround;
        public string BaseStatusId;
        public ProjectileModifierDefinition ProjectileModifier;
        public IReadOnlyList<EffectSpec> ContactEffects;
        public float ContactIntervalSeconds;
    }
}

