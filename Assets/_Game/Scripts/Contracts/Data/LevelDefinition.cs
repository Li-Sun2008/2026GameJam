// Generated verbatim from Docs/Contracts/Spotlight.Contracts.cs (2.1.0).
using System;
using System.Collections.Generic;

namespace Spotlight.Contracts
{
    public sealed class LevelDefinition
    {
        public string Id;
        public int Rows;
        public int Columns;
        public float CellSize;
        public WorldPoint Origin;
        public CellCoord SpringCell;
        public IReadOnlyList<CellCoord> ReservedCells;
        public IReadOnlyList<SpawnPointDefinition> SpawnPoints;
        public IReadOnlyList<PathDefinition> Paths;
        public IReadOnlyList<InitialOccupant> InitialOccupants;
        public IReadOnlyList<ResourceAmount> InitialResources;
    }
}

