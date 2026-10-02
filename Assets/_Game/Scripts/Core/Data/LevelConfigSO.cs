using System;
using System.Collections.Generic;
using Spotlight.Contracts;
using UnityEngine;
namespace Spotlight.Core.Data
{
    [CreateAssetMenu(menuName="Spotlight/P1/LevelConfig",fileName="LevelConfig")]
    public sealed class LevelConfigSO : ScriptableObject
    {
        public string Id = "level.prototype";
        public int Rows = 10;
        public int Columns = 10;
        public float CellSize = 1;
        public WorldPointConfig Origin = new WorldPointConfig();
        public CellCoordConfig SpringCell = new CellCoordConfig { X=5,Y=5 };
        public CellCoordConfig[] ReservedCells = new CellCoordConfig[0];
        public SpawnPointConfig[] SpawnPoints = new SpawnPointConfig[0];
        public PathConfig[] Paths = new PathConfig[0];
        public InitialOccupantConfig[] InitialOccupants = new InitialOccupantConfig[0];
        public ResourceAmountConfig[] InitialResources = new ResourceAmountConfig[0];
        public LevelDefinition ToDefinition()
        {
            List<CellCoord> reservedcellsValues=new List<CellCoord>();
            if(ReservedCells!=null)foreach(CellCoordConfig value in ReservedCells)if(value!=null)reservedcellsValues.Add(value.ToDefinition());
            List<SpawnPointDefinition> spawnpointsValues=new List<SpawnPointDefinition>();
            if(SpawnPoints!=null)foreach(SpawnPointConfig value in SpawnPoints)if(value!=null)spawnpointsValues.Add(value.ToDefinition());
            List<PathDefinition> pathsValues=new List<PathDefinition>();
            if(Paths!=null)foreach(PathConfig value in Paths)if(value!=null)pathsValues.Add(value.ToDefinition());
            List<InitialOccupant> initialoccupantsValues=new List<InitialOccupant>();
            if(InitialOccupants!=null)foreach(InitialOccupantConfig value in InitialOccupants)if(value!=null)initialoccupantsValues.Add(value.ToDefinition());
            List<ResourceAmount> initialresourcesValues=new List<ResourceAmount>();
            if(InitialResources!=null)foreach(ResourceAmountConfig value in InitialResources)if(value!=null)initialresourcesValues.Add(value.ToDefinition());
            return new LevelDefinition { Id=Id,Rows=Rows,Columns=Columns,CellSize=CellSize,Origin=(Origin==null?new WorldPointConfig():Origin).ToDefinition(),SpringCell=(SpringCell==null?new CellCoordConfig():SpringCell).ToDefinition(),ReservedCells=reservedcellsValues.AsReadOnly(),SpawnPoints=spawnpointsValues.AsReadOnly(),Paths=pathsValues.AsReadOnly(),InitialOccupants=initialoccupantsValues.AsReadOnly(),InitialResources=initialresourcesValues.AsReadOnly() };
        }
    }
}
