#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using Spotlight.Bootstrap;
using Spotlight.Contracts;
using Spotlight.Core.Data;

namespace Spotlight.Tests.Integration
{
    public sealed class RuntimeLoopTests
    {
        private const float Step = 1f / 30;
        private PrototypeCatalogSO asset;
        private ModuleComposition runtime;
        private string directory;
        [SetUp] public void Setup()
        {
            asset = AssetDatabase.LoadAssetAtPath<PrototypeCatalogSO>(PrototypeCatalogFactory.CatalogPath);
            Assert.NotNull(asset, "Generate the prototype catalog before running these asset-backed integration tests.");
            directory = Path.Combine(Path.GetTempPath(), "Spotlight_Runtime_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
        }
        [TearDown] public void Cleanup()
        {
            if(runtime!=null)runtime.Dispose();runtime=null;
            if(directory!=null&&Directory.Exists(directory))
            {
                string absolute=Path.GetFullPath(directory), root=Path.GetFullPath(Path.GetTempPath());
                Assert.IsTrue(absolute.StartsWith(root,StringComparison.OrdinalIgnoreCase)&&Path.GetFileName(absolute).StartsWith("Spotlight_Runtime_",StringComparison.Ordinal));
                Directory.Delete(absolute,true);
            }
        }
        private CommandContext Command(){return runtime.Core.Commands.Create(runtime.Core.Transactions.Revision);}
        private void NewStory()
        {
            Assert.AreEqual(OperationState.Committed,runtime.Flow.TryNewRun(Command(),new NewRunRequest {LevelId="level.prototype",Mode=GameMode.Story,Seed=123}).State);
            runtime.Advance(0);Assert.AreEqual(GamePhase.DayEvent,runtime.Flow.GetSnapshot().Phase);ChooseEvent();
        }
        private void ChooseEvent()
        {
            DayEventSnapshot e=runtime.Gameplay.DayEvents.GetSnapshot();Assert.NotNull(e.State);Assert.IsFalse(e.State.Resolved);Assert.Greater(e.Options.Count,0);
            Assert.AreEqual(OperationState.Committed,runtime.Gameplay.DayEvents.TryChoose(Command(),e.State.InstanceId,e.Options[0].Id).State);
            runtime.Advance(0);Assert.AreEqual(GamePhase.Build,runtime.Flow.GetSnapshot().Phase);
        }
        private void Place(string id,OccupantKind kind,CellCoord cell)
        {Assert.AreEqual(OperationState.Committed,runtime.Core.Deployment.TryPlace(Command(),new PlacementRequest {DefinitionId=id,Kind=kind,Cell=cell}).State);runtime.Advance(0);}
        private ActorSnapshot Spring(){ActorSnapshot a;Assert.IsTrue(runtime.Core.World.TryGetActor(runtime.Core.World.SpringId,out a));return a;}
        [Test] public void DeploymentAdvanceAutomaticallyPersistsBoardAndResourceCosts()
        {
            runtime=new ModuleComposition(asset.BuildCatalogData(),directory);NewStory();
            Assert.AreEqual(OperationState.Committed,runtime.Core.Deployment.TryPlace(Command(),new PlacementRequest {DefinitionId="tower.basic",Kind=OccupantKind.Tower,Cell=new CellCoord(2,1)}).State);
            Assert.AreEqual(OperationState.Committed,runtime.Core.Deployment.TryPlace(Command(),new PlacementRequest {DefinitionId="elm.water",Kind=OccupantKind.ElementBlock,Cell=new CellCoord(2,2)}).State);
            runtime.Advance(0);
            Assert.IsTrue(File.Exists(Path.Combine(directory,"checkpoint.json")),"Stable deployment must write a real checkpoint without a manual Save command.");
            SaveReadResult saved=runtime.Saves.ReadCheckpoint();Assert.AreEqual(ErrorCode.None,saved.Error);Assert.AreEqual(runtime.Core.Transactions.Revision,saved.Data.Revision);Assert.AreEqual(2,saved.Data.Occupants.Count);
            long gold=-1,water=-1;foreach(ResourceAmount resource in saved.Data.Resources){if(resource.Bucket==ResourceBucket.Inventory&&resource.DefinitionId=="res.gold")gold=resource.Amount;if(resource.Bucket==ResourceBucket.Hand&&resource.DefinitionId=="elm.water")water=resource.Amount;}
            Assert.AreEqual(80,gold);Assert.AreEqual(2,water);
            int savedEvents=0;IDisposable subscription=runtime.Events.Subscribe<CheckpointSavedEvent>(delegate(CheckpointSavedEvent e){savedEvents++;});runtime.Advance(0);runtime.Advance(0);Assert.AreEqual(0,savedEvents,"Unchanged revision must not write another checkpoint.");subscription.Dispose();
        }
        [Test] public void AssetBackedNightQueuesItemsSimulatesProjectilesSettlesOnceAndRestoresCheckpoint()
        {
            CatalogData original=asset.BuildCatalogData(), data=asset.BuildCatalogData();float originalDamage=original.Towers[0].BasePhysicalDamage, originalRange=original.Towers[0].RangeInCells;
            foreach(TowerDefinition tower in data.Towers){tower.BasePhysicalDamage=500;tower.RangeInCells=20;}
            runtime=new ModuleComposition(data,directory);NewStory();
            Assert.AreEqual(0,runtime.Flow.GetSnapshot().LastSettledNight);Assert.AreEqual(3,runtime.Core.Resources.GetQuantity(ResourceBucket.Hand,"elm.water"),"Day one must not produce elements");
            Place("tower.basic",OccupantKind.Tower,new CellCoord(2,1));Place("elm.water",OccupantKind.ElementBlock,new CellCoord(2,2));
            Assert.AreEqual(OperationState.Committed,runtime.Saves.SaveCheckpoint(Command(),SaveReason.Manual).State);
            SaveReadResult read=runtime.Saves.ReadCheckpoint();Assert.AreEqual(ErrorCode.None,read.Error);CheckpointData checkpoint=read.Data;Assert.AreEqual(2,checkpoint.Occupants.Count);
            long handBefore=runtime.Core.Resources.GetQuantity(ResourceBucket.Hand,"elm.water");
            Assert.AreEqual(OperationState.Committed,runtime.Flow.TryStartNight(Command()).State);
            runtime.Combat.Damage.Enqueue(new DamagePacket {PacketId="runtime.test.spring.damage",Source=runtime.Core.World.SpringId,Target=runtime.Core.World.SpringId,Origin=DamageOrigin.Event,PhysicalDamage=20});
            runtime.Advance(Step);Assert.AreEqual(80,Spring().CurrentHp);
            bool sawEnemy=runtime.Enemies.Enemies.GetSnapshot().Count>0, sawProjectile=runtime.Combat.Projectiles.GetSnapshot().Count>0;
            ActorSnapshot enemy=runtime.Core.World.GetActors(ActorKind.Enemy)[0];ClockSnapshot pausedBefore=runtime.Clock.GetSnapshot();Guid pause=runtime.Clock.AcquirePause(PauseReason.Player);
            runtime.Advance(1);Assert.AreEqual(pausedBefore.TickIndex,runtime.Clock.GetSnapshot().TickIndex);Assert.AreEqual(pausedBefore.GameTime,runtime.Clock.GetSnapshot().GameTime);
            ActorSnapshot afterPause;runtime.Core.World.TryGetActor(enemy.Id,out afterPause);Assert.AreEqual(enemy.Position.X,afterPause.Position.X);Assert.AreEqual(enemy.Position.Y,afterPause.Position.Y);runtime.Clock.ReleasePause(pause);
            long oneBefore=runtime.Clock.GetSnapshot().TickIndex;runtime.Advance(Step);Assert.AreEqual(oneBefore+1,runtime.Clock.GetSnapshot().TickIndex);
            Assert.AreEqual(OperationState.Committed,runtime.Clock.TrySetSpeed(Command(),GameSpeed.Double).State);long doubleBefore=runtime.Clock.GetSnapshot().TickIndex;runtime.Advance(Step);Assert.AreEqual(doubleBefore+2,runtime.Clock.GetSnapshot().TickIndex);
            runtime.Clock.TrySetSpeed(Command(),GameSpeed.Normal);
            CommandContext skillCommand=Command();OperationResult queuedSkill=runtime.Combat.Skill.TryUse(skillCommand);Assert.AreEqual(OperationState.Queued,queuedSkill.State);Assert.AreEqual(queuedSkill.Revision,runtime.Combat.Skill.TryUse(skillCommand).Revision);
            Assert.AreEqual(ErrorCode.DuplicateCommand,runtime.Combat.Skill.TryUse(new CommandContext(skillCommand.CommandId,skillCommand.RunId,skillCommand.ExpectedRevision+1)).Error);
            long stock=runtime.Core.Resources.GetQuantity(ResourceBucket.Inventory,"item.heal");CommandContext item=Command();OperationResult finished=new OperationResult();
            IDisposable finishSubscription=runtime.Events.Subscribe<CommandFinishedEvent>(delegate(CommandFinishedEvent e){if(e.Result.CommandId==item.CommandId)finished=e.Result;});
            Assert.AreEqual(OperationState.Queued,runtime.Gameplay.Items.TryUse(item,new ItemUseRequest {ItemId="item.heal",Target=runtime.Core.World.SpringId}).State);Assert.AreEqual(stock,runtime.Core.Resources.GetQuantity(ResourceBucket.Inventory,"item.heal"));
            runtime.Advance(Step);Assert.AreEqual(OperationState.Committed,finished.State);Assert.AreEqual(stock-1,runtime.Core.Resources.GetQuantity(ResourceBucket.Inventory,"item.heal"));Assert.AreEqual(100,Spring().CurrentHp);finishSubscription.Dispose();
            Assert.AreEqual(OperationState.Committed,runtime.Combat.Skill.TryUse(skillCommand).State);Assert.AreEqual(0,runtime.Combat.Skill.GetSnapshot().UsesRemaining);
            for(int i=0;i<3600&&runtime.Flow.GetSnapshot().Phase==GamePhase.Night;i++){runtime.Advance(Step);sawEnemy|=runtime.Enemies.Enemies.GetSnapshot().Count>0;sawProjectile|=runtime.Combat.Projectiles.GetSnapshot().Count>0;}
            Assert.IsTrue(sawEnemy);Assert.IsTrue(sawProjectile);Assert.AreEqual(GamePhase.NightResult,runtime.Flow.GetSnapshot().Phase,"Real waves/projectiles/damage/deaths must reach the outcome stage");
            CommandContext confirm=Command();OperationResult first=runtime.Flow.TryConfirmNightResult(confirm);Assert.AreEqual(OperationState.Committed,first.State);Assert.AreEqual(first.Revision,runtime.Flow.TryConfirmNightResult(confirm).Revision);
            Assert.AreEqual(2,runtime.Flow.GetSnapshot().DayIndex);Assert.AreEqual(1,runtime.Flow.GetSnapshot().LastSettledNight);Assert.AreEqual(handBefore+1,runtime.Core.Resources.GetQuantity(ResourceBucket.Hand,"elm.water"));Assert.AreEqual(GamePhase.DayEvent,runtime.Flow.GetSnapshot().Phase);ChooseEvent();
            Assert.AreEqual(OperationState.Committed,runtime.Flow.TryReturnToMenu(Command()).State);runtime.Advance(0);Assert.AreEqual(GamePhase.Menu,runtime.Flow.GetSnapshot().Phase);
            Assert.AreEqual(OperationState.Committed,runtime.Flow.TryLoadRun(Command(),checkpoint).State);runtime.Advance(0);Assert.AreEqual(GamePhase.Build,runtime.Flow.GetSnapshot().Phase);Assert.AreEqual(1,runtime.Flow.GetSnapshot().DayIndex);Assert.AreEqual(0,runtime.Flow.GetSnapshot().LastSettledNight);Assert.AreEqual(handBefore,runtime.Core.Resources.GetQuantity(ResourceBucket.Hand,"elm.water"));Assert.IsTrue(runtime.Gameplay.DayEvents.GetSnapshot().State.Resolved);
            RandomState random=runtime.Core.Random.GetState(RandomStream.DayEvent);runtime.Advance(0);Assert.AreEqual(random.State,runtime.Core.Random.GetState(RandomStream.DayEvent).State,"Loading resolved day event must not draw again");
            CatalogData untouched=asset.BuildCatalogData();Assert.AreEqual(originalDamage,untouched.Towers[0].BasePhysicalDamage);Assert.AreEqual(originalRange,untouched.Towers[0].RangeInCells);
        }
        [Test] public void ActualSpringLethalPacketImmediatelyProducesGameOver()
        {
            runtime=new ModuleComposition(asset.BuildCatalogData(),directory);NewStory();Assert.AreEqual(OperationState.Committed,runtime.Flow.TryStartNight(Command()).State);
            runtime.Combat.Damage.Enqueue(new DamagePacket {PacketId="runtime.test.lethal",Source=runtime.Core.World.SpringId,Target=runtime.Core.World.SpringId,Origin=DamageOrigin.Event,PhysicalDamage=100000});runtime.Advance(Step);
            Assert.AreEqual(GamePhase.GameOver,runtime.Flow.GetSnapshot().Phase);Assert.AreEqual(0,Spring().CurrentHp);Assert.AreEqual(0,runtime.Combat.Projectiles.GetSnapshot().Count);
        }
        [Test] public void RealStoryReachesEndingOnSeventhNight()
        {
            CatalogData data=asset.BuildCatalogData();
            foreach(TowerDefinition tower in data.Towers){tower.BasePhysicalDamage=100000;tower.RangeInCells=20;tower.AttackIntervalSeconds=0.05f;}
            runtime=new ModuleComposition(data,directory);NewStory();Place("tower.basic",OccupantKind.Tower,new CellCoord(2,1));
            bool finalBossSpawned=false,finalBossKilled=false;
            HashSet<long> bossActors=new HashSet<long>();
            IDisposable spawn=runtime.Events.Subscribe<ActorSpawnedEvent>(delegate(ActorSpawnedEvent e){EnemyDefinition enemy;if(e.Kind==ActorKind.Enemy&&runtime.Catalog.TryGetEnemy(e.DefinitionId,out enemy)&&enemy.IsFinalBoss){finalBossSpawned=true;bossActors.Add(e.Actor.Value);}});
            IDisposable hit=runtime.Events.Subscribe<DamageAppliedEvent>(delegate(DamageAppliedEvent e){if(e.Result!=null&&e.Result.NewlyKilled&&bossActors.Contains(e.Result.Target.Value))finalBossKilled=true;});
            try
            {
                for(int day=1;day<=7;day++)
                {
                    Assert.AreEqual(day,runtime.Flow.GetSnapshot().DayIndex);Assert.AreEqual(GamePhase.Build,runtime.Flow.GetSnapshot().Phase);
                    Assert.AreEqual(OperationState.Committed,runtime.Flow.TryStartNight(Command()).State);
                    for(int tick=0;tick<3000&&runtime.Flow.GetSnapshot().Phase==GamePhase.Night;tick++)runtime.Advance(Step);
                    Assert.Greater(Spring().CurrentHp,0,"Spring must survive real story night "+day);
                    if(day<7)
                    {
                        Assert.AreEqual(GamePhase.NightResult,runtime.Flow.GetSnapshot().Phase,"Night "+day+" must complete through real waves/death/outcome stages");
                        Assert.AreEqual(OperationState.Committed,runtime.Flow.TryConfirmNightResult(Command()).State);Assert.AreEqual(day+1,runtime.Flow.GetSnapshot().DayIndex);Assert.AreEqual(GamePhase.DayEvent,runtime.Flow.GetSnapshot().Phase);ChooseEvent();
                    }
                    else Assert.AreEqual(GamePhase.Ending,runtime.Flow.GetSnapshot().Phase,"Seventh night must reach Ending through the actual final Boss kill");
                }
                Assert.IsTrue(finalBossSpawned,"Configured final Boss must really spawn");Assert.IsTrue(finalBossKilled,"Final Boss must die from an actual damage packet");Assert.AreEqual(0,runtime.Enemies.Enemies.GetSnapshot().Count);Assert.AreEqual(0,runtime.Combat.Projectiles.GetSnapshot().Count);
            }
            finally{spawn.Dispose();hit.Dispose();}
        }
    }
}
#endif
