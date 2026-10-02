// Generated verbatim from Docs/Contracts/Spotlight.Contracts.cs (2.1.0).
using System;
using System.Collections.Generic;

namespace Spotlight.Contracts
{
    public sealed class EnemyDefinition
    {
        public string Id;
        public string PrefabKey;
        public MovementKind Movement;
        public bool IsFinalBoss;
        public bool Enabled;
        public float MaxHp;
        public float PhysicalDamage;
        public float SpeedInCellsPerSecond;
        public float RangeInCells;
        public float CollisionRadiusInCells;
        public float AttackIntervalSeconds;
        public string ProjectileId;
        public ElementType Element;
        public float PhysicalReduction;
        public ElementAmounts Resistance;
        public string DropTableId;
        public IReadOnlyList<string> BossSkillIds;
    }
}

