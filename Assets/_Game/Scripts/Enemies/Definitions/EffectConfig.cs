using System;
using System.Collections.Generic;
using Spotlight.Contracts;
namespace Spotlight.Enemies.Definitions
{
    [Serializable]
    public sealed class EffectConfig
    {
        public string Id;
        public EffectKind Kind;
        public EffectTargetPolicy TargetPolicy;
        public string ReferenceId;
        public ResourceBucket ResourceBucket;
        public long ResourceAmount;
        public float PhysicalDamage;
        public ElementAmountsConfig ElementDamage = new ElementAmountsConfig();
        public ElementAmountsConfig Gauge = new ElementAmountsConfig();
        public float Amount;
        public int Stacks;
        public float DurationOverride;
        public float DamageMultiplier = 1f;
        public float SpeedMultiplier = 1f;
        public EffectSpec ToDefinition()
        {
            return new EffectSpec { Id=Id,Kind=Kind,TargetPolicy=TargetPolicy,ReferenceId=ReferenceId,ResourceBucket=ResourceBucket,ResourceAmount=ResourceAmount,PhysicalDamage=PhysicalDamage,ElementDamage=(ElementDamage==null?new ElementAmountsConfig():ElementDamage).ToDefinition(),Gauge=(Gauge==null?new ElementAmountsConfig():Gauge).ToDefinition(),Amount=Amount,Stacks=Stacks,DurationOverride=DurationOverride,DamageMultiplier=DamageMultiplier,SpeedMultiplier=SpeedMultiplier };
        }
    }
}
