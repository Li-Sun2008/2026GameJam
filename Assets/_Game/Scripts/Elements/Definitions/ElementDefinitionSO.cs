using System;
using System.Collections.Generic;
using Spotlight.Contracts;
using UnityEngine;
namespace Spotlight.Elements.Definitions
{
    [CreateAssetMenu(menuName="Spotlight/P2/ElementDefinition",fileName="ElementDefinition")]
    public sealed class ElementDefinitionSO : ScriptableObject
    {
        public string Id = "element.water";
        public ElementType Element = ElementType.Water;
        public string DisplayNameKey = "水";
        public string PrefabKey = "ElementWater";
        public bool BlocksGround;
        public string BaseStatusId = "status.water";
        public ProjectileModifierConfig ProjectileModifier = new ProjectileModifierConfig { Element=ElementType.Water,DamageAdd=new ElementAmountsConfig { Water=2 },GaugeAdd=new ElementAmountsConfig { Water=25 } };
        public EffectConfig[] ContactEffects = new EffectConfig[0];
        public float ContactIntervalSeconds = 1;
        public ElementDefinition ToDefinition()
        {
            List<EffectSpec> contacteffectsValues=new List<EffectSpec>();
            if(ContactEffects!=null)foreach(EffectConfig value in ContactEffects)if(value!=null)contacteffectsValues.Add(value.ToDefinition());
            return new ElementDefinition { Id=Id,Element=Element,DisplayNameKey=DisplayNameKey,PrefabKey=PrefabKey,BlocksGround=BlocksGround,BaseStatusId=BaseStatusId,ProjectileModifier=(ProjectileModifier==null?new ProjectileModifierConfig():ProjectileModifier).ToDefinition(),ContactEffects=contacteffectsValues.AsReadOnly(),ContactIntervalSeconds=ContactIntervalSeconds };
        }
    }
}
