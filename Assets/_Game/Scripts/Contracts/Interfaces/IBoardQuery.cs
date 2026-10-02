// Generated verbatim from Docs/Contracts/Spotlight.Contracts.cs (2.1.0).
using System;
using System.Collections.Generic;

namespace Spotlight.Contracts
{
    public interface IBoardQuery
    {
        BoardSnapshot GetSnapshot();
        bool TryGetCell(CellCoord cell, out CellSnapshot snapshot);
        IReadOnlyList<CellCoord> GetNeighbors4(CellCoord cell);
        WorldPoint CellToWorld(CellCoord cell);
        bool TryWorldToCell(WorldPoint point, out CellCoord cell);
        IReadOnlyList<CellSnapshot> TraceSegment(WorldPoint from, WorldPoint to);
    }
}

