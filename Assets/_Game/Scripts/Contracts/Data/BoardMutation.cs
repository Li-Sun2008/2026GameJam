// Generated verbatim from Docs/Contracts/Spotlight.Contracts.cs (2.1.0).
using System;
using System.Collections.Generic;

namespace Spotlight.Contracts
{
    public sealed class BoardMutation
    {
        public BoardMutationKind Kind;
        public EntityId EntityId;
        public OccupantKind OccupantKind;
        public string DefinitionId;
        public CellCoord From;
        public CellCoord To;
    }
}

