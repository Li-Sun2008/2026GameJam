using System;
using System.Collections.Generic;
using Spotlight.Contracts;
using UnityEngine;
namespace Spotlight.Combat.Definitions
{
    [CreateAssetMenu(menuName="Spotlight/P3/SpecialSkillDefinition",fileName="SpecialSkillDefinition")]
    public sealed class SpecialSkillDefinitionSO : ScriptableObject
    {
        public string Id = "skill.spotlight";
        public int UsesPerNight = 1;
        public float DurationSeconds = 10;
        public float ReactionDamageMultiplier = 1.2f;
        public SpecialSkillDefinition ToDefinition()
        {
            return new SpecialSkillDefinition { Id=Id,UsesPerNight=UsesPerNight,DurationSeconds=DurationSeconds,ReactionDamageMultiplier=ReactionDamageMultiplier };
        }
    }
}
