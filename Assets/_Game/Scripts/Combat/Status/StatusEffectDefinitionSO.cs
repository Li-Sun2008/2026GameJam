using System;
using System.Collections.Generic;
using Spotlight.Contracts;
using UnityEngine;
namespace Spotlight.Combat.Definitions
{
    [CreateAssetMenu(menuName="Spotlight/P3/StatusEffectDefinition",fileName="StatusEffectDefinition")]
    public sealed class StatusEffectDefinitionSO : ScriptableObject
    {
        public string Id = "status.water";
        public ElementType ElementTag = ElementType.Water;
        public bool IsReactionInput = true;
        public bool IsReactionDamage;
        public float DurationSeconds = 6;
        public float TickIntervalSeconds = 1;
        public int MaxStacks = 10;
        public StatusRefreshPolicy RefreshPolicy = StatusRefreshPolicy.RefreshAllLayers;
        public float MoveSlowPerStack;
        public float AttackSlowPerStack;
        public float ArmorBreakPerStack;
        public ElementAmountsConfig ResistanceBreakPerStack = new ElementAmountsConfig();
        public bool PreventMovement;
        public bool PreventAttack;
        public float DotPhysicalPerStack;
        public ElementAmountsConfig DotElementPerStack = new ElementAmountsConfig();
        public StatusDefinition ToDefinition()
        {
            return new StatusDefinition { Id=Id,ElementTag=ElementTag,IsReactionInput=IsReactionInput,IsReactionDamage=IsReactionDamage,DurationSeconds=DurationSeconds,TickIntervalSeconds=TickIntervalSeconds,MaxStacks=MaxStacks,RefreshPolicy=RefreshPolicy,MoveSlowPerStack=MoveSlowPerStack,AttackSlowPerStack=AttackSlowPerStack,ArmorBreakPerStack=ArmorBreakPerStack,ResistanceBreakPerStack=(ResistanceBreakPerStack==null?new ElementAmountsConfig():ResistanceBreakPerStack).ToDefinition(),PreventMovement=PreventMovement,PreventAttack=PreventAttack,DotPhysicalPerStack=DotPhysicalPerStack,DotElementPerStack=(DotElementPerStack==null?new ElementAmountsConfig():DotElementPerStack).ToDefinition() };
        }
    }
}
