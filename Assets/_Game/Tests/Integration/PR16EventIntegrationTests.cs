#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using Spotlight.Bootstrap;
using Spotlight.Contracts;

namespace Spotlight.Tests.Integration
{
    public sealed class PR16EventIntegrationTests
    {
        private ModuleComposition runtime;
        private string directory;
        private CommandContext Command() { return runtime.Core.Commands.Create(runtime.Core.Transactions.Revision); }
        [SetUp] public void Setup()
        {
            directory = Path.Combine(Path.GetTempPath(), "Spotlight_PR16Events_" + Guid.NewGuid().ToString("N"));
            var asset = AssetDatabase.LoadAssetAtPath<PrototypeCatalogSO>(PrototypeCatalogFactory.CatalogPath);
            Assert.NotNull(asset);
            CollectionAssert.IsEmpty(asset.Validate());
            runtime = new ModuleComposition(asset.BuildCatalogData(), directory);
        }
        [TearDown] public void Cleanup()
        {
            if (runtime != null) runtime.Dispose();
            if (directory != null && Directory.Exists(directory))
            {
                string target = Path.GetFullPath(directory);
                string root = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
                Assert.IsTrue(target.StartsWith(root, StringComparison.OrdinalIgnoreCase));
                Assert.IsTrue(Path.GetFileName(target).StartsWith("Spotlight_PR16Events_"));
                Directory.Delete(target, true);
            }
        }
        // A transaction fixture selects a specific real event; catalog, effects, transactions and flow remain production services.
        private void LoadEvent(string id, long gold = 30, float hp = 60)
        {
            if (runtime.Flow.GetSnapshot().Phase != GamePhase.Menu)
                Assert.AreEqual(OperationState.Committed, runtime.Flow.TryReturnToMenu(Command()).State);
            Assert.AreEqual(OperationState.Committed, runtime.Flow.TryNewRun(Command(), new NewRunRequest { LevelId = "level.prototype", Mode = GameMode.Story, Seed = 17 }).State);
            Assert.AreEqual(OperationState.Committed, runtime.Core.Transactions.TryCommit(new StateMutationBatch
            {
                Context = Command(), Reason = "pr16.event.fixture",
                Progress = new ProgressPatch { Phase = GamePhase.DayEvent, DayIndex = 2, LastSettledNight = 1 },
                ResourceDeltas = new[] { new ResourceAmount(ResourceBucket.Inventory, "res.gold", gold - runtime.Core.Resources.GetQuantity(ResourceBucket.Inventory, "res.gold")) },
                Actors = new[] { new ActorMutation { ActorId = runtime.Core.World.SpringId, Kind = ActorMutationKind.SetHealth, CurrentHp = hp, MaxHp = 100 } },
                DayEvent = new DayEventState { DayIndex = 2, DefinitionId = id, InstanceId = "pr16-fixture-" + id, Resolved = false }
            }).State);
            Assert.AreEqual(GamePhase.DayEvent, runtime.Flow.GetSnapshot().Phase);
            Assert.AreEqual(id, runtime.Gameplay.DayEvents.GetSnapshot().State.DefinitionId);
        }
        private OperationResult Choose(string option)
        {
            var snapshot = runtime.Gameplay.DayEvents.GetSnapshot();
            return runtime.Gameplay.DayEvents.TryChoose(Command(), snapshot.State.InstanceId, option);
        }
        [TestCase(GameMode.Story)] [TestCase(GameMode.Endless)]
        public void ProductionCatalogStartsWithQuietMorningAndRegistersThreeEligibleEvents(GameMode mode)
        {
            foreach (string id in new[] { "event.element_trade", "event.spring_blessing", "event.supply_merchant" })
            {
                DayEventDefinition definition;
                Assert.IsTrue(runtime.Catalog.TryGetEvent(id, out definition));
                Assert.AreEqual(2, definition.MinDayInclusive); Assert.AreEqual(7, definition.MaxDayInclusive);
                Assert.AreEqual(1, definition.Weight); CollectionAssert.Contains(definition.Modes, mode);
            }
            Assert.AreEqual(OperationState.Committed, runtime.Flow.TryNewRun(Command(), new NewRunRequest { LevelId = "level.prototype", Mode = mode, Seed = 17 }).State);
            var day = runtime.Gameplay.DayEvents.GetSnapshot();
            Assert.AreEqual("event.quiet_morning", day.State.DefinitionId);
            Assert.AreEqual("continue", day.Options.Single().Id);
            Assert.AreEqual(OperationState.Committed, Choose("continue").State);
            runtime.Advance(0); Assert.AreEqual(GamePhase.Build, runtime.Flow.GetSnapshot().Phase);
        }
        [TestCase("event.element_trade", "buy_wood", "elm.wood", ResourceBucket.Hand, 10, 2)]
        [TestCase("event.supply_merchant", "buy_heal", "item.heal", ResourceBucket.Inventory, 20, 1)]
        [TestCase("event.supply_merchant", "buy_stack", "item.tower_stack", ResourceBucket.Inventory, 20, 1)]
        [TestCase("event.spring_blessing", "take_water", "elm.water", ResourceBucket.Hand, 0, 1)]
        public void RealOptionChargesCostAndGrantsRewardExactlyOnce(string id, string option, string reward, ResourceBucket bucket, long price, long amount)
        {
            LoadEvent(id); long before = runtime.Core.Resources.GetQuantity(bucket, reward);
            var state = runtime.Gameplay.DayEvents.GetSnapshot().State; var command = Command();
            var first = runtime.Gameplay.DayEvents.TryChoose(command, state.InstanceId, option);
            Assert.AreEqual(OperationState.Committed, first.State);
            Assert.AreEqual(first.Revision, runtime.Gameplay.DayEvents.TryChoose(command, state.InstanceId, option).Revision);
            Assert.AreEqual(30 - price, runtime.Core.Resources.GetQuantity(ResourceBucket.Inventory, "res.gold"));
            Assert.AreEqual(before + amount, runtime.Core.Resources.GetQuantity(bucket, reward));
            runtime.Advance(0); Assert.AreEqual(GamePhase.Build, runtime.Flow.GetSnapshot().Phase);
            Assert.AreEqual(OperationState.Committed, runtime.Saves.SaveCheckpoint(Command(), SaveReason.Manual).State);
            var read = runtime.Saves.ReadCheckpoint(); Assert.AreEqual(ErrorCode.None, read.Error);
            Assert.AreEqual(OperationState.Committed, runtime.Flow.TryReturnToMenu(Command()).State);
            Assert.AreEqual(OperationState.Committed, runtime.Flow.TryLoadRun(Command(), read.Data).State);
            runtime.Advance(0);
            Assert.AreEqual(before + amount, runtime.Core.Resources.GetQuantity(bucket, reward));
            Assert.AreEqual(30 - price, runtime.Core.Resources.GetQuantity(ResourceBucket.Inventory, "res.gold"));
            Assert.AreEqual(option, runtime.Gameplay.DayEvents.GetSnapshot().State.ChosenOptionId);
            Assert.AreEqual(OperationState.Rejected, Choose(option).State);
        }
        [TestCase(GameMode.Story)] [TestCase(GameMode.Endless)]
        public void RealEventDrawCanSelectEveryRegisteredEventAndRepeatedEnsureDoesNotRedraw(GameMode mode)
        {
            var observed = new System.Collections.Generic.HashSet<string>();
            for (uint seed = 1; seed <= 128 && observed.Count < 4; seed++)
            {
                if (runtime.Flow.GetSnapshot().Phase != GamePhase.Menu)
                    Assert.AreEqual(OperationState.Committed, runtime.Flow.TryReturnToMenu(Command()).State);
                Assert.AreEqual(OperationState.Committed, runtime.Flow.TryNewRun(Command(), new NewRunRequest { LevelId = "level.prototype", Mode = mode, Seed = seed }).State);
                Assert.AreEqual(OperationState.Committed, runtime.Core.Transactions.TryCommit(new StateMutationBatch
                {
                    Context = Command(), Reason = "pr16.day.fixture",
                    Progress = new ProgressPatch { Phase = GamePhase.DayEvent, DayIndex = 2, LastSettledNight = 1 }
                }).State);
                Assert.AreEqual(OperationState.Committed, runtime.Gameplay.DayEvents.EnsureEvent(2, mode).State);
                var drawn = runtime.Gameplay.DayEvents.GetSnapshot().State;
                observed.Add(drawn.DefinitionId);
                long revision = runtime.Core.Transactions.Revision;
                var random = runtime.Core.Random.GetState(RandomStream.DayEvent);
                Assert.AreEqual(OperationState.Committed, runtime.Gameplay.DayEvents.EnsureEvent(2, mode).State);
                Assert.AreEqual(revision, runtime.Core.Transactions.Revision);
                Assert.AreEqual(random.State, runtime.Core.Random.GetState(RandomStream.DayEvent).State);
                Assert.AreEqual(drawn.InstanceId, runtime.Gameplay.DayEvents.GetSnapshot().State.InstanceId);
            }
            CollectionAssert.AreEquivalent(new[] { "event.quiet_morning", "event.element_trade", "event.spring_blessing", "event.supply_merchant" }, observed);
        }
        [TestCase("event.element_trade", "buy_wood", "elm.wood", ResourceBucket.Hand, 9)]
        [TestCase("event.supply_merchant", "buy_heal", "item.heal", ResourceBucket.Inventory, 19)]
        [TestCase("event.supply_merchant", "buy_stack", "item.tower_stack", ResourceBucket.Inventory, 19)]
        public void InsufficientGoldKeepsEventUnresolvedAndAllowsFreeLeave(string id, string option, string reward, ResourceBucket bucket, long gold)
        {
            LoadEvent(id, gold); long before = runtime.Core.Resources.GetQuantity(bucket, reward);
            long revision = runtime.Core.Transactions.Revision;
            Assert.AreEqual(ErrorCode.InsufficientQuantity, Choose(option).Error);
            Assert.AreEqual(revision, runtime.Core.Transactions.Revision);
            Assert.AreEqual(gold, runtime.Core.Resources.GetQuantity(ResourceBucket.Inventory, "res.gold"));
            Assert.AreEqual(before, runtime.Core.Resources.GetQuantity(bucket, reward));
            Assert.IsFalse(runtime.Gameplay.DayEvents.GetSnapshot().State.Resolved);
            Assert.AreEqual(ErrorCode.WrongPhase, runtime.Flow.TryStartNight(Command()).Error);
            Assert.AreEqual(OperationState.Committed, Choose("leave").State);
            runtime.Advance(0); Assert.AreEqual(GamePhase.Build, runtime.Flow.GetSnapshot().Phase);
            Assert.AreEqual(gold, runtime.Core.Resources.GetQuantity(ResourceBucket.Inventory, "res.gold"));
        }
        [TestCase(60f, 75f)] [TestCase(95f, 100f)] [TestCase(100f, 100f)]
        public void BlessingActuallyHealsSpringAndClampsToMaximum(float before, float after)
        {
            LoadEvent("event.spring_blessing", 30, before);
            Assert.AreEqual(OperationState.Committed, Choose("heal").State);
            ActorSnapshot spring; Assert.IsTrue(runtime.Core.World.TryGetActor(runtime.Core.World.SpringId, out spring));
            Assert.AreEqual(after, spring.CurrentHp); Assert.AreEqual(100, spring.MaxHp);
            Assert.AreEqual(30, runtime.Core.Resources.GetQuantity(ResourceBucket.Inventory, "res.gold"));
        }
    }
}
#endif
