// Generated verbatim from Docs/Contracts/Spotlight.Contracts.cs (2.1.0).
using System;
using System.Collections.Generic;

namespace Spotlight.Contracts
{
    public sealed class SavedOccupant
    {
        public EntityId Id;
        public string DefinitionId;
        public OccupantKind Kind;
        public CellCoord Cell;
        public float CurrentHp;
        public float MaxHp;
        public int AttackStacks;
    }
}

