// Generated verbatim from Docs/Contracts/Spotlight.Contracts.cs (2.1.0).
using System;
using System.Collections.Generic;

namespace Spotlight.Contracts
{
    public sealed class CellSnapshot
    {
        public CellCoord Cell;
        public string TerrainId;
        public bool Reserved;
        public bool OnGroundPath;
        public EntityId OccupantId;
        public OccupantKind OccupantKind;
        public string DefinitionId;
        public bool BlocksGround;
    }
}

