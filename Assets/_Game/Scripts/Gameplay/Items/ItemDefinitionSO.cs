using System;
using System.Collections.Generic;
using Spotlight.Contracts;
using UnityEngine;
namespace Spotlight.Gameplay.Definitions
{
    [CreateAssetMenu(menuName="Spotlight/P5/ItemDefinition",fileName="ItemDefinition")]
    public sealed class ItemDefinitionSO : ScriptableObject
    {
        public string Id = "item.unconfigured";
        public string DisplayNameKey;
        public string IconKey;
        public GamePhase[] AllowedPhases = new GamePhase[] { GamePhase.Build,GamePhase.Night };
        public string StackingGroup;
        public float ActiveDurationSeconds;
        public EffectConfig[] Effects = new EffectConfig[0];
        public ItemDefinition ToDefinition()
        {
            List<GamePhase> allowedphasesValues=new List<GamePhase>();
            if(AllowedPhases!=null)allowedphasesValues.AddRange(AllowedPhases);
            List<EffectSpec> effectsValues=new List<EffectSpec>();
            if(Effects!=null)foreach(EffectConfig value in Effects)if(value!=null)effectsValues.Add(value.ToDefinition());
            return new ItemDefinition { Id=Id,DisplayNameKey=DisplayNameKey,IconKey=IconKey,AllowedPhases=allowedphasesValues.AsReadOnly(),StackingGroup=StackingGroup,ActiveDurationSeconds=ActiveDurationSeconds,Effects=effectsValues.AsReadOnly() };
        }
    }
}
