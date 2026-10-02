// Generated verbatim from Docs/Contracts/Spotlight.Contracts.cs (2.1.0).
using System;
using System.Collections.Generic;

namespace Spotlight.Contracts
{
    public interface IElementRuleService
    {
        ProductionResult ResolveProduction(ProductionInput input);
        ReactionPlan ResolveReactions(ReactionInput input);
        ProjectileModifierDefinition GetProjectileModifier(ElementType element);
    }
}

