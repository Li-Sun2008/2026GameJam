using System;
using System.Collections.Generic;
using Spotlight.Contracts;
namespace Spotlight.Elements.Definitions
{
    [Serializable]
    public sealed class ProjectileModifierConfig
    {
        public ElementType Element;
        public float PhysicalAdd;
        public ElementAmountsConfig DamageAdd = new ElementAmountsConfig();
        public ElementAmountsConfig GaugeAdd = new ElementAmountsConfig();
        public float DamageMultiplier = 1f;
        public float SpeedMultiplier = 1f;
        public ProjectileModifierDefinition ToDefinition()
        {
            return new ProjectileModifierDefinition { Element=Element,PhysicalAdd=PhysicalAdd,DamageAdd=(DamageAdd==null?new ElementAmountsConfig():DamageAdd).ToDefinition(),GaugeAdd=(GaugeAdd==null?new ElementAmountsConfig():GaugeAdd).ToDefinition(),DamageMultiplier=DamageMultiplier,SpeedMultiplier=SpeedMultiplier };
        }
    }
}
