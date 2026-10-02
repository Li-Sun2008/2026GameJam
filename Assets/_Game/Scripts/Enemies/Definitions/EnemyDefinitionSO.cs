using System;
using System.Collections.Generic;
using Spotlight.Contracts;
using UnityEngine;
namespace Spotlight.Enemies.Definitions
{
    [CreateAssetMenu(menuName="Spotlight/P4/EnemyDefinition",fileName="EnemyDefinition")]
    public sealed class EnemyDefinitionSO : ScriptableObject
    {
        public string Id = "enemy.goblin.melee";
        public string PrefabKey = "EnemyGoblinMelee";
        public MovementKind Movement;
        public bool IsFinalBoss;
        public bool Enabled = true;
        public float MaxHp = 30;
        public float PhysicalDamage = 5;
        public float SpeedInCellsPerSecond = 1;
        public float RangeInCells = 1;
        public float CollisionRadiusInCells = 0.35f;
        public float AttackIntervalSeconds = 1;
        public string ProjectileId;
        public ElementType Element;
        public float PhysicalReduction;
        public ElementAmountsConfig Resistance = new ElementAmountsConfig();
        public string DropTableId;
        public string[] BossSkillIds = new string[0];
        public EnemyDefinition ToDefinition()
        {
            List<string> bossskillidsValues=new List<string>();
            if(BossSkillIds!=null)bossskillidsValues.AddRange(BossSkillIds);
            return new EnemyDefinition { Id=Id,PrefabKey=PrefabKey,Movement=Movement,IsFinalBoss=IsFinalBoss,Enabled=Enabled,MaxHp=MaxHp,PhysicalDamage=PhysicalDamage,SpeedInCellsPerSecond=SpeedInCellsPerSecond,RangeInCells=RangeInCells,CollisionRadiusInCells=CollisionRadiusInCells,AttackIntervalSeconds=AttackIntervalSeconds,ProjectileId=ProjectileId,Element=Element,PhysicalReduction=PhysicalReduction,Resistance=(Resistance==null?new ElementAmountsConfig():Resistance).ToDefinition(),DropTableId=DropTableId,BossSkillIds=bossskillidsValues.AsReadOnly() };
        }
    }
}
