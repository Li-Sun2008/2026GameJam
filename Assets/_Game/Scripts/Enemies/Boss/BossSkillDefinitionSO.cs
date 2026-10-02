using System;
using System.Collections.Generic;
using Spotlight.Contracts;
using UnityEngine;
namespace Spotlight.Enemies.Definitions
{
    [CreateAssetMenu(menuName="Spotlight/P4/BossSkillDefinition",fileName="BossSkillDefinition")]
    public sealed class BossSkillDefinitionSO : ScriptableObject
    {
        public string Id = "boss.skill.unconfigured";
        public float PeriodSeconds = 5;
        public float FirstDelaySeconds = 5;
        public EffectConfig[] Effects = new EffectConfig[0];
        public SpawnGroupConfig[] Summons = new SpawnGroupConfig[0];
        public string ProjectileId;
        public float ProjectileDamage;
        public BossSkillDefinition ToDefinition()
        {
            List<EffectSpec> effectsValues=new List<EffectSpec>();
            if(Effects!=null)foreach(EffectConfig value in Effects)if(value!=null)effectsValues.Add(value.ToDefinition());
            List<SpawnGroupDefinition> summonsValues=new List<SpawnGroupDefinition>();
            if(Summons!=null)foreach(SpawnGroupConfig value in Summons)if(value!=null)summonsValues.Add(value.ToDefinition());
            return new BossSkillDefinition { Id=Id,PeriodSeconds=PeriodSeconds,FirstDelaySeconds=FirstDelaySeconds,Effects=effectsValues.AsReadOnly(),Summons=summonsValues.AsReadOnly(),ProjectileId=ProjectileId,ProjectileDamage=ProjectileDamage };
        }
    }
}
