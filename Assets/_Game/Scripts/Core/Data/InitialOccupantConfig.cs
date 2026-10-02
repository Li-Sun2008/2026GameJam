using System;
using System.Collections.Generic;
using Spotlight.Contracts;
namespace Spotlight.Core.Data
{
    [Serializable]
    public sealed class InitialOccupantConfig
    {
        public string DefinitionId;
        public OccupantKind Kind;
        public CellCoordConfig Cell = new CellCoordConfig();
        public InitialOccupant ToDefinition()
        {
            return new InitialOccupant { DefinitionId=DefinitionId,Kind=Kind,Cell=(Cell==null?new CellCoordConfig():Cell).ToDefinition() };
        }
    }
}
