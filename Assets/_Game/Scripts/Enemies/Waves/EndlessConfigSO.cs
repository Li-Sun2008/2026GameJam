using System;
using System.Collections.Generic;
using Spotlight.Contracts;
using UnityEngine;
namespace Spotlight.Enemies.Definitions
{
    [CreateAssetMenu(menuName="Spotlight/P4/EndlessConfig",fileName="EndlessConfig")]
    public sealed class EndlessConfigSO : ScriptableObject
    {
        public string Id = "endless.default";
        public string[] WaveTemplateIds = new string[0];
        public float HpGrowthPerNight = 0.25f;
        public float DamageGrowthPerNight = 0.25f;
        public int MilestoneNight = 50;
        public ResourceAmountConfig[] MilestoneRewards = new ResourceAmountConfig[0];
        public EndlessDefinition ToDefinition()
        {
            List<string> wavetemplateidsValues=new List<string>();
            if(WaveTemplateIds!=null)wavetemplateidsValues.AddRange(WaveTemplateIds);
            List<ResourceAmount> milestonerewardsValues=new List<ResourceAmount>();
            if(MilestoneRewards!=null)foreach(ResourceAmountConfig value in MilestoneRewards)if(value!=null)milestonerewardsValues.Add(value.ToDefinition());
            return new EndlessDefinition { Id=Id,WaveTemplateIds=wavetemplateidsValues.AsReadOnly(),HpGrowthPerNight=HpGrowthPerNight,DamageGrowthPerNight=DamageGrowthPerNight,MilestoneNight=MilestoneNight,MilestoneRewards=milestonerewardsValues.AsReadOnly() };
        }
    }
}
