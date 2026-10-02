// Generated verbatim from Docs/Contracts/Spotlight.Contracts.cs (2.1.0).
using System;
using System.Collections.Generic;

namespace Spotlight.Contracts
{
    public sealed class TowerDefinition
    {
        public string Id;
        public string PrefabKey;
        public float HpPerStack;
        public int InitialAttackStacks;
        public int MaxAttackStacks;
        public float BasePhysicalDamage;
        public float DamagePerExtraStack;
        public float AttackIntervalSeconds;
        public float RangeInCells;
        public float CollisionRadiusInCells;
        public string ProjectileId;
        public IReadOnlyList<ResourceAmount> BuildCost;
        public int PlacementLimit;
        public float RecallRefundRatio;
    }
}

