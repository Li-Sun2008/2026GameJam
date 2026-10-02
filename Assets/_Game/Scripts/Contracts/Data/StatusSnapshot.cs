// Generated verbatim from Docs/Contracts/Spotlight.Contracts.cs (2.1.0).
using System;
using System.Collections.Generic;

namespace Spotlight.Contracts
{
    public sealed class StatusSnapshot
    {
        public string DefinitionId;
        public EntityId Target;
        public IReadOnlyList<StatusLayerSnapshot> Layers;
    }
}

