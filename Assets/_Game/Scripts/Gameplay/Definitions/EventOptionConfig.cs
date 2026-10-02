using System;
using System.Collections.Generic;
using Spotlight.Contracts;
namespace Spotlight.Gameplay.Definitions
{
    [Serializable]
    public sealed class EventOptionConfig
    {
        public string Id = "continue";
        public string Text = "继续";
        public ResourceAmountConfig[] Cost = new ResourceAmountConfig[0];
        public EffectConfig[] Effects = new EffectConfig[0];
        public EventOptionDefinition ToDefinition()
        {
            List<ResourceAmount> costValues=new List<ResourceAmount>();
            if(Cost!=null)foreach(ResourceAmountConfig value in Cost)if(value!=null)costValues.Add(value.ToDefinition());
            List<EffectSpec> effectsValues=new List<EffectSpec>();
            if(Effects!=null)foreach(EffectConfig value in Effects)if(value!=null)effectsValues.Add(value.ToDefinition());
            return new EventOptionDefinition { Id=Id,Text=Text,Cost=costValues.AsReadOnly(),Effects=effectsValues.AsReadOnly() };
        }
    }
}
