// Generated verbatim from Docs/Contracts/Spotlight.Contracts.cs (2.1.0).
using System;
using System.Collections.Generic;

namespace Spotlight.Contracts
{
    public sealed class ActorMutation
    {
        public ActorMutationKind Kind;
        public EntityId ActorId;
        public ActorSnapshot Registration;
        public float CurrentHp;
        public float MaxHp;
        public int TowerAttackStacks;
    }
}

