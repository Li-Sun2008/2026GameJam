using System;
using System.Collections.Generic;
using Spotlight.Contracts;
using UnityEngine;
namespace Spotlight.Elements.Definitions
{
    [CreateAssetMenu(menuName="Spotlight/P2/ReactionDefinition",fileName="ReactionDefinition")]
    public sealed class ReactionDefinitionSO : ScriptableObject
    {
        public string Id = "reaction.unconfigured";
        public bool Enabled = false;
        public ReactionContext Context;
        public ElementType A = ElementType.Water;
        public ElementType B = ElementType.Fire;
        public int ConsumeA = 1;
        public int ConsumeB = 1;
        public int Priority = 100;
        public EffectConfig[] Effects = new EffectConfig[0];
        public ReactionDefinition ToDefinition()
        {
            List<EffectSpec> effectsValues=new List<EffectSpec>();
            if(Effects!=null)foreach(EffectConfig value in Effects)if(value!=null)effectsValues.Add(value.ToDefinition());
            return new ReactionDefinition { Id=Id,Enabled=Enabled,Context=Context,A=A,B=B,ConsumeA=ConsumeA,ConsumeB=ConsumeB,Priority=Priority,Effects=effectsValues.AsReadOnly() };
        }
    }
}
