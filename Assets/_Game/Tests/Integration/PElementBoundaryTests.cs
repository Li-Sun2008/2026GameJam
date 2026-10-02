using System;
using Spotlight.Contracts;
using Spotlight.Elements;
namespace Spotlight.Tests
{
    public static class ElementBoundaryTests
    {
        private static void Assert(bool value, string message) { if (!value) throw new Exception(message); }
        public static void Run()
        {
            CombatFixture f = new CombatFixture(); ElementRuleService service = new ElementRuleService(f, f);
            f.Production.Add(new ProductionRuleDefinition { Id = "single", Enabled = true, Match = ProductionMatch.SingleBlock, A = ElementType.Water, OutputElementId = "elm.water", OutputCount = 1 });
            f.Production.Add(new ProductionRuleDefinition { Id = "pair", Enabled = true, Match = ProductionMatch.AdjacentPair, A = ElementType.Water, B = ElementType.Water, ConsumeA = true, ConsumeB = true, OutputElementId = "elm.water", OutputCount = 1 });
            CellSnapshot[] cells = new CellSnapshot[3]; for (int i = 0; i < 3; i++) cells[i] = new CellSnapshot { Cell = new CellCoord(i, 0), OccupantId = new EntityId(i + 1), OccupantKind = OccupantKind.ElementBlock, DefinitionId = "elm.water" };
            ProductionResult p = service.ResolveProduction(new ProductionInput { Board = new BoardSnapshot { Cells = cells, Revision = 7 }, Random = new RandomState(RandomStream.Production, 123), CompletedNight = 1 });
            Assert(p.HandDeltas.Count == 4 && p.Board.Count == 2 && p.NextRandom.State == 123 && p.ExpectedRevision == 7, "Nonconsuming singles precede one reserved unordered pair; inputs and RNG untouched");
            Assert(cells[0].OccupantKind == OccupantKind.ElementBlock, "Production must not mutate input board");
            f.Reactions.Add(new ReactionDefinition { Id = "same", Enabled = true, Context = ReactionContext.Projectile, A = ElementType.Water, B = ElementType.Water, Effects = new EffectSpec[0] });
            ReactionPlan one = service.ResolveReactions(new ReactionInput { Context = ReactionContext.Projectile, Tokens = new ReactionToken[] { new ReactionToken { TokenId = 1, Element = ElementType.Water } } });
            Assert(one.Matches.Count == 0, "A single token cannot satisfy same-element pair");
            ReactionToken[] two = { new ReactionToken { TokenId = 2, Element = ElementType.Water }, new ReactionToken { TokenId = 1, Element = ElementType.Water } };
            ReactionPlan twice = service.ResolveReactions(new ReactionInput { Context = ReactionContext.Projectile, Tokens = two });
            Assert(twice.Matches.Count == 1 && twice.RemainingTokens.Count == 2, "Zero consumption projectile reaction executes once without losing tokens");
            ReactionPlan excluded = service.ResolveReactions(new ReactionInput { Context = ReactionContext.Projectile, Tokens = two, AppliedRuleIds = new string[] { "same" } });
            Assert(excluded.Matches.Count == 0, "Applied projectile rule cannot repeat");
        }
    }
}
