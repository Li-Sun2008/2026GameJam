using System;
using System.Collections.Generic;
using Spotlight.Contracts;
using UnityEngine;
namespace Spotlight.Combat.Definitions
{
    [CreateAssetMenu(menuName="Spotlight/P3/TowerDefinition",fileName="TowerDefinition")]
    public sealed class TowerDefinitionSO : ScriptableObject
    {
        public string Id = "tower.basic";
        public string PrefabKey = "TowerBasic";
        public float HpPerStack = 100;
        public int InitialAttackStacks = 1;
        public int MaxAttackStacks = 10;
        public float BasePhysicalDamage = 5;
        public float DamagePerExtraStack = 5;
        public float AttackIntervalSeconds = 1.2f;
        public float RangeInCells = 3;
        public float CollisionRadiusInCells = 0.35f;
        public string ProjectileId = "projectile.basic";
        public ResourceAmountConfig[] BuildCost = new ResourceAmountConfig[0];
        public int PlacementLimit;
        public float RecallRefundRatio = 1;
        public TowerDefinition ToDefinition()
        {
            List<ResourceAmount> buildcostValues=new List<ResourceAmount>();
            if(BuildCost!=null)foreach(ResourceAmountConfig value in BuildCost)if(value!=null)buildcostValues.Add(value.ToDefinition());
            return new TowerDefinition { Id=Id,PrefabKey=PrefabKey,HpPerStack=HpPerStack,InitialAttackStacks=InitialAttackStacks,MaxAttackStacks=MaxAttackStacks,BasePhysicalDamage=BasePhysicalDamage,DamagePerExtraStack=DamagePerExtraStack,AttackIntervalSeconds=AttackIntervalSeconds,RangeInCells=RangeInCells,CollisionRadiusInCells=CollisionRadiusInCells,ProjectileId=ProjectileId,BuildCost=buildcostValues.AsReadOnly(),PlacementLimit=PlacementLimit,RecallRefundRatio=RecallRefundRatio };
        }
    }
}
