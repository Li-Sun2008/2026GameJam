// Generated verbatim from Docs/Contracts/Spotlight.Contracts.cs (2.1.0).
using System;
using System.Collections.Generic;

namespace Spotlight.Contracts
{
    public sealed class ActorSnapshot
    {
        public EntityId Id;
        public ActorKind Kind;
        public string DefinitionId;
        public WorldPoint Position;
        public WorldPoint PreviousPosition;
        public float CollisionRadius;
        public float CurrentHp;
        public float MaxHp;
        public float PhysicalReduction;
        public ElementAmounts ElementResistance;
        public int TowerAttackStacks;
        public long SpawnSequence;
        public bool Targetable;
        public bool DeathResolved;
    }
}

