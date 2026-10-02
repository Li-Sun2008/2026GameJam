using System;
using System.Collections.Generic;
using Spotlight.Contracts;
namespace Spotlight.Core.Data
{
    [Serializable]
    public sealed class SpawnPointConfig
    {
        public string Id;
        public CellCoordConfig Cell = new CellCoordConfig();
        public SpawnPointDefinition ToDefinition()
        {
            return new SpawnPointDefinition { Id=Id,Cell=(Cell==null?new CellCoordConfig():Cell).ToDefinition() };
        }
    }
}
