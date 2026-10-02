// Generated verbatim from Docs/Contracts/Spotlight.Contracts.cs (2.1.0).
using System;
using System.Collections.Generic;

namespace Spotlight.Contracts
{
    public sealed class PathDefinition
    {
        public string Id;
        public MovementKind Movement;
        public string SpawnPointId;
        public IReadOnlyList<CellCoord> Cells;
    }
}

