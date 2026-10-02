// Generated verbatim from Docs/Contracts/Spotlight.Contracts.cs (2.1.0).
using System;
using System.Collections.Generic;

namespace Spotlight.Contracts
{
    public interface IDeploymentService
    {
        PlacementCheck CheckPlacement(PlacementRequest request);
        OperationResult TryPlace(CommandContext context, PlacementRequest request);
        OperationResult TryMove(CommandContext context, CellCoord from, CellCoord to);
        OperationResult TrySwap(CommandContext context, CellCoord a, CellCoord b);
        OperationResult TryRecall(CommandContext context, CellCoord cell);
    }
}

