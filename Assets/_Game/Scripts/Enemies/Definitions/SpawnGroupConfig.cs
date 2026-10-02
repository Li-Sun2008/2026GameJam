using System;
using System.Collections.Generic;
using Spotlight.Contracts;
namespace Spotlight.Enemies.Definitions
{
    [Serializable]
    public sealed class SpawnGroupConfig
    {
        public string Id = "group.1";
        public string EnemyId = "enemy.goblin.melee";
        public int Count = 1;
        public string SpawnPointId = "spawn.west";
        public string PathId = "path.west";
        public float StartDelaySeconds;
        public float SpawnIntervalSeconds = 1;
        public bool ApplyDayGrowth = true;
        public float HpMultiplier = 1;
        public float DamageMultiplier = 1;
        public SpawnGroupDefinition ToDefinition()
        {
            return new SpawnGroupDefinition { Id=Id,EnemyId=EnemyId,Count=Count,SpawnPointId=SpawnPointId,PathId=PathId,StartDelaySeconds=StartDelaySeconds,SpawnIntervalSeconds=SpawnIntervalSeconds,ApplyDayGrowth=ApplyDayGrowth,HpMultiplier=HpMultiplier,DamageMultiplier=DamageMultiplier };
        }
    }
}
