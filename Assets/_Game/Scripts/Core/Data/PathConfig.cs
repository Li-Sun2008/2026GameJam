using System;
using System.Collections.Generic;
using Spotlight.Contracts;
namespace Spotlight.Core.Data
{
    [Serializable]
    public sealed class PathConfig
    {
        public string Id;
        public MovementKind Movement;
        public string SpawnPointId;
        public CellCoordConfig[] Cells = new CellCoordConfig[0];
        public PathDefinition ToDefinition()
        {
            List<CellCoord> cellsValues=new List<CellCoord>();
            if(Cells!=null)foreach(CellCoordConfig value in Cells)if(value!=null)cellsValues.Add(value.ToDefinition());
            return new PathDefinition { Id=Id,Movement=Movement,SpawnPointId=SpawnPointId,Cells=cellsValues.AsReadOnly() };
        }
    }
}
