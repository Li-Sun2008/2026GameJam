#if UNITY_EDITOR
using System;
using System.IO;
using NUnit.Framework;
using Spotlight.Bootstrap;
using Spotlight.Contracts;
using Spotlight.Enemies;
using Spotlight.Core.Clock;
using Spotlight.Core.Data;
using UnityEditor;

namespace Spotlight.Tests.Integration
{
    public sealed class R3BCombatIntegrationTests
    {
        ModuleComposition runtime; string directory; long tick;
        sealed class CapturedDamage : IDamageService
        {
            readonly IDamageService inner; public DamagePacket Last;
            public CapturedDamage(IDamageService value){inner=value;}
            public int PendingCount {get{return inner.PendingCount;}}
            public DamageResult Preview(DamagePacket packet){return inner.Preview(packet);}
            public OperationResult Enqueue(DamagePacket packet){Last=packet;return inner.Enqueue(packet);}
        }
        sealed class CapturedProjectiles : IProjectileService
        {
            readonly IProjectileService inner;public ProjectileSpawnRequest Last;
            public CapturedProjectiles(IProjectileService value){inner=value;}
            public OperationResult Spawn(ProjectileSpawnRequest request){Last=request;return inner.Spawn(request);}
            public System.Collections.Generic.IReadOnlyList<ProjectileSnapshot> GetSnapshot(){return inner.GetSnapshot();}
            public void Clear(){inner.Clear();}
        }
        [SetUp] public void Setup()
        {
            directory=Path.Combine(Path.GetTempPath(),"Spotlight_R3BCombat_"+Guid.NewGuid().ToString("N"));
            var asset=AssetDatabase.LoadAssetAtPath<PrototypeCatalogSO>(PrototypeCatalogFactory.CatalogPath);
            Assert.NotNull(asset); runtime=new ModuleComposition(asset.BuildCatalogData(),directory);
            Assert.AreEqual(ErrorCode.None,runtime.Core.Store.Initialize(new NewRunRequest {LevelId="level.prototype",Mode=GameMode.Story,Seed=604}).Error);
            var context=new SessionStartContext {RunId=runtime.Core.Store.GetFlowSnapshot().RunId,LevelId="level.prototype",Mode=GameMode.Story,DayIndex=1,SpringId=runtime.Core.World.SpringId};
            runtime.Combat.BeginSession(context); runtime.Enemies.BeginSession(context); tick=0;
        }
        [TearDown] public void Cleanup()
        {
            if(runtime!=null)runtime.Dispose();
            if(directory!=null&&Directory.Exists(directory))
            {
                string path=Path.GetFullPath(directory),root=Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar;
                Assert.IsTrue(path.StartsWith(root,StringComparison.OrdinalIgnoreCase)&&Path.GetFileName(path).StartsWith("Spotlight_R3BCombat_",StringComparison.Ordinal));Directory.Delete(path,true);
            }
        }
        CommandContext Command(){return runtime.Core.Commands.Create(runtime.Core.Transactions.Revision);}
        void Stage(double time,float dt,TickStage stage){runtime.Combat.Tick(new TickContext(++tick,time,dt,stage));runtime.Events.Flush();}
        EntityId Register(ActorKind kind,WorldPoint position)
        {
            EntityId id=runtime.Core.Ids.Allocate();
            var actor=new ActorSnapshot {Id=id,Kind=kind,DefinitionId=kind==ActorKind.Tower?"tower.basic":"enemy.goblin.melee",Position=position,PreviousPosition=position,CurrentHp=100,MaxHp=100,Targetable=true,SpawnSequence=runtime.Core.Ids.AllocateSpawnSequence()};
            Assert.AreEqual(OperationState.Committed,runtime.Core.Transactions.TryCommit(new StateMutationBatch {Context=Command(),Reason="test.register",Actors=new[]{new ActorMutation {Kind=ActorMutationKind.Register,ActorId=id,Registration=actor}}}).State);return id;
        }
        EntityId PlaceTower(CellCoord cell)
        {
            Assert.AreEqual(OperationState.Committed,runtime.Core.Deployment.TryPlace(Command(),new PlacementRequest {DefinitionId="tower.basic",Kind=OccupantKind.Tower,Cell=cell}).State);
            CellSnapshot snapshot;Assert.IsTrue(runtime.Core.Board.TryGetCell(cell,out snapshot));return snapshot.OccupantId;
        }
        [TestCase("elm.fire","status.contact.burn",10f,1f)]
        [TestCase("elm.water","status.contact.water_slow",0f,.85f)]
        public void CatalogContactRunsOnceThenTicksOrExpires(string element,string status,float contactDamage,float moveMultiplier)
        {
            var cell=new CellCoord(2,4);
            Assert.AreEqual(OperationState.Committed,runtime.Core.Deployment.TryPlace(Command(),new PlacementRequest {DefinitionId=element,Kind=OccupantKind.ElementBlock,Cell=cell}).State);
            EntityId enemy;
            Assert.AreEqual(OperationState.Committed,runtime.Enemies.TrySpawn(new EnemySpawnRequest {DefinitionId="enemy.goblin.melee",PathId="path.ground",SpawnPointId="spawn.ground",HpMultiplier=1,DamageMultiplier=1},out enemy).State);
            // Enter through the real motion writer so this also covers a traced contact.
            Assert.AreEqual(OperationState.Committed,runtime.Core.Motion.TrySetPosition(enemy,runtime.Core.Board.CellToWorld(cell)).State);
            ActorSnapshot beforeContact;Assert.IsTrue(runtime.Core.World.TryGetActor(enemy,out beforeContact));float initialHp=beforeContact.CurrentHp;
            Stage(0,0,TickStage.Attacks);Stage(0,0,TickStage.DamageAndReaction);
            ActorSnapshot actor;Assert.IsTrue(runtime.Core.World.TryGetActor(enemy,out actor));Assert.AreEqual(initialHp-contactDamage,actor.CurrentHp);
            Assert.AreEqual(status,runtime.Combat.Status.GetStatuses(enemy)[0].DefinitionId);Assert.AreEqual(moveMultiplier,runtime.Combat.Status.GetModifiers(enemy).MoveMultiplier,.0001f);
            Stage(.1,.1f,TickStage.Attacks);Stage(.1,.1f,TickStage.DamageAndReaction);
            Assert.IsTrue(runtime.Core.World.TryGetActor(enemy,out actor));Assert.AreEqual(initialHp-contactDamage,actor.CurrentHp,"Staying contact has its own cooldown");
            Stage(1, .9f,TickStage.StatusAndGauge);Stage(1,0,TickStage.DamageAndReaction);
            Assert.IsTrue(runtime.Core.World.TryGetActor(enemy,out actor));Assert.AreEqual(initialHp-contactDamage-(element=="elm.fire"?10:0),actor.CurrentHp,"Burn deals ten fire damage on its first tick");
            Stage(10,9,TickStage.StatusAndGauge);Assert.AreEqual(0,runtime.Combat.Status.GetStatuses(enemy).Count);Assert.AreEqual(1,runtime.Combat.Status.GetModifiers(enemy).MoveMultiplier);
        }
        EntityId SpawnContactEnemy(string element)
        {
            var cell=new CellCoord(2,4);
            Assert.AreEqual(OperationState.Committed,runtime.Core.Deployment.TryPlace(Command(),new PlacementRequest {DefinitionId=element,Kind=OccupantKind.ElementBlock,Cell=cell}).State);
            EntityId enemy;
            // Fixture-only HP multiplier permits repeated contact and DOT observations without death.
            Assert.AreEqual(OperationState.Committed,runtime.Enemies.TrySpawn(new EnemySpawnRequest {DefinitionId="enemy.goblin.melee",PathId="path.ground",SpawnPointId="spawn.ground",HpMultiplier=4,DamageMultiplier=1},out enemy).State);
            Assert.AreEqual(OperationState.Committed,runtime.Core.Motion.TrySetPosition(enemy,runtime.Core.Board.CellToWorld(cell)).State);
            runtime.Core.Store.AdvancePreviousPositions();return enemy;
        }
        float Hp(EntityId actor){ActorSnapshot value;Assert.IsTrue(runtime.Core.World.TryGetActor(actor,out value));return value.CurrentHp;}
        [Test] public void StationaryFireContactRepeatsOnIntervalWithoutDoublingOrContactOnEmptyCell()
        {
            EntityId enemy=SpawnContactEnemy("elm.fire");ActorSnapshot actor;Assert.IsTrue(runtime.Core.World.TryGetActor(enemy,out actor));
            Assert.AreEqual(actor.Position,actor.PreviousPosition);Assert.AreEqual(0,runtime.Core.Board.TraceSegment(actor.PreviousPosition,actor.Position).Count);
            Stage(0,0,TickStage.Attacks);Stage(0,0,TickStage.DamageAndReaction);Assert.AreEqual(110,Hp(enemy),"Stationary first contact still hits");
            runtime.Core.Store.AdvancePreviousPositions();Stage(.5,.5f,TickStage.Attacks);Stage(.5,0,TickStage.DamageAndReaction);Assert.AreEqual(110,Hp(enemy),"Cooldown prevents per-tick repeats");
            runtime.Core.Store.AdvancePreviousPositions();Stage(1,.5f,TickStage.StatusAndGauge);Stage(1,0,TickStage.Attacks);Stage(1,0,TickStage.DamageAndReaction);
            Assert.AreEqual(90,Hp(enemy),"First DOT and second contact each deal ten");Assert.AreEqual(1,runtime.Combat.Status.GetStatuses(enemy)[0].Layers.Count);
            Assert.AreEqual(OperationState.Committed,runtime.Core.Motion.TrySetPosition(enemy,runtime.Core.Board.CellToWorld(new CellCoord(3,4))).State);
            Stage(1.1,.1f,TickStage.Attacks);Stage(1.1,0,TickStage.DamageAndReaction);runtime.Core.Store.AdvancePreviousPositions();
            Stage(1.2,.1f,TickStage.Attacks);Stage(1.2,0,TickStage.DamageAndReaction);Assert.AreEqual(90,Hp(enemy));
            Stage(2,.8f,TickStage.StatusAndGauge);Stage(2,0,TickStage.Attacks);Stage(2,0,TickStage.DamageAndReaction);Assert.AreEqual(80,Hp(enemy),"Empty cell supplies no contact; retained burn only ticks once");
        }
        [Test] public void StationaryWaterContactRefreshesUntilLeavingThenExpires()
        {
            EntityId enemy=SpawnContactEnemy("elm.water");Stage(0,0,TickStage.Attacks);Stage(0,0,TickStage.DamageAndReaction);
            Assert.AreEqual(.85f,runtime.Combat.Status.GetModifiers(enemy).MoveMultiplier,.0001f);
            runtime.Core.Store.AdvancePreviousPositions();Stage(1,1,TickStage.StatusAndGauge);Stage(1,0,TickStage.Attacks);Stage(1,0,TickStage.DamageAndReaction);
            Assert.AreEqual(1.5f,runtime.Combat.Status.GetStatuses(enemy)[0].Layers[0].RemainingSeconds,.0001f,"One-second contact refreshes slow duration");
            Assert.AreEqual(OperationState.Committed,runtime.Core.Motion.TrySetPosition(enemy,runtime.Core.Board.CellToWorld(new CellCoord(3,4))).State);
            Stage(1.1,.1f,TickStage.Attacks);Stage(1.1,0,TickStage.DamageAndReaction);runtime.Core.Store.AdvancePreviousPositions();
            Stage(2,.9f,TickStage.StatusAndGauge);Stage(2,0,TickStage.Attacks);Stage(2,0,TickStage.DamageAndReaction);Assert.AreEqual(.85f,runtime.Combat.Status.GetModifiers(enemy).MoveMultiplier,.0001f);
            Stage(2.49,.49f,TickStage.StatusAndGauge);Assert.AreEqual(.85f,runtime.Combat.Status.GetModifiers(enemy).MoveMultiplier,.0001f);
            Stage(2.5,.01f,TickStage.StatusAndGauge);Assert.AreEqual(1,runtime.Combat.Status.GetModifiers(enemy).MoveMultiplier);Assert.AreEqual(0,runtime.Combat.Status.GetStatuses(enemy).Count);Assert.AreEqual(120,Hp(enemy));
        }
        [Test] public void ThreeColdWaveReactionsStackThenExpireTogether()
        {
            EntityId enemy=Register(ActorKind.Enemy,new WorldPoint(0,0));
            ElementDefinition water;Assert.IsTrue(runtime.Catalog.TryGetElement("elm.water",out water));
            for(int i=0;i<3;i++)
            {
                Assert.AreEqual(OperationState.Queued,runtime.Combat.Status.EnqueueStatus(new StatusApplyRequest {Source=enemy,Target=enemy,StatusId=water.BaseStatusId,Stacks=2}).State);
                Stage(0,0,TickStage.DamageAndReaction);Stage(0,0,TickStage.DamageAndReaction);
            }
            var statuses=runtime.Combat.Status.GetStatuses(enemy);Assert.AreEqual(1,statuses.Count);Assert.AreEqual("status.reaction.cold_wave",statuses[0].DefinitionId);Assert.AreEqual(3,statuses[0].Layers.Count);
            Assert.AreEqual(.55f,runtime.Combat.Status.GetModifiers(enemy).MoveMultiplier,.0001f);
            Stage(2.9,2.9f,TickStage.StatusAndGauge);Assert.AreEqual(3,runtime.Combat.Status.GetStatuses(enemy)[0].Layers.Count);
            Stage(3,.1f,TickStage.StatusAndGauge);Assert.AreEqual(0,runtime.Combat.Status.GetStatuses(enemy).Count);Assert.AreEqual(1,runtime.Combat.Status.GetModifiers(enemy).MoveMultiplier);
        }
        [Test] public void CurseTargetsEveryTowerAndClearsOnExpiryRemovalAndSessionEnd()
        {
            EntityId a=PlaceTower(new CellCoord(1,1)),b=PlaceTower(new CellCoord(2,1));
            BossSkillDefinition curse;Assert.IsTrue(runtime.Catalog.TryGetBossSkill("boss.curse",out curse));
            Assert.AreEqual(OperationState.Committed,runtime.Combat.Effects.TryExecute(new EffectBatchRequest {Context=Command(),Source=runtime.Core.World.SpringId,Origin=DamageOrigin.Boss,Effects=curse.Effects}).State);
            Assert.AreEqual(.5f,runtime.Combat.Status.GetModifiers(a).AttackRateMultiplier);Assert.AreEqual(.5f,runtime.Combat.Status.GetModifiers(b).AttackRateMultiplier);
            Assert.AreEqual(ErrorCode.InvalidTarget,runtime.Combat.Status.EnqueueGauge(a,a,new ElementAmounts(100,0,0,0,0,0)).Error);
            ActorSnapshot spring;Assert.IsTrue(runtime.Core.World.TryGetActor(runtime.Core.World.SpringId,out spring));Assert.AreEqual(100,spring.CurrentHp);
            Assert.AreEqual(OperationState.Committed,runtime.Core.Deployment.TryRecall(Command(),new CellCoord(1,1)).State);Stage(1,1,TickStage.StatusAndGauge);Assert.AreEqual(0,runtime.Combat.Status.GetStatuses(a).Count);
            Stage(9.9,8.9f,TickStage.StatusAndGauge);Assert.AreEqual(.5f,runtime.Combat.Status.GetModifiers(b).AttackRateMultiplier);
            Stage(10,.1f,TickStage.StatusAndGauge);Assert.AreEqual(1,runtime.Combat.Status.GetModifiers(b).AttackRateMultiplier);
            runtime.Combat.Status.EnqueueStatus(new StatusApplyRequest {Source=b,Target=b,StatusId="status.boss.curse",Stacks=1});Stage(10,0,TickStage.DamageAndReaction);
            runtime.Events.Publish(new PhaseChangedEvent {OldPhase=GamePhase.Night,NewPhase=GamePhase.NightResult});runtime.Events.Flush();Assert.AreEqual(0,runtime.Combat.Status.GetStatuses(b).Count);
            runtime.Combat.Status.EnqueueStatus(new StatusApplyRequest {Source=b,Target=b,StatusId="status.boss.curse",Stacks=1});Stage(10,0,TickStage.DamageAndReaction);runtime.Combat.EndSession();Assert.AreEqual(0,runtime.Combat.Status.GetStatuses(b).Count);
        }
        [Test] public void CurseDurationUsesPausedAndDoubleSpeedGameClock()
        {
            EntityId tower=PlaceTower(new CellCoord(2,1));BossSkillDefinition curse;Assert.IsTrue(runtime.Catalog.TryGetBossSkill("boss.curse",out curse));
            Assert.AreEqual(OperationState.Committed,runtime.Combat.Effects.TryExecute(new EffectBatchRequest {Context=Command(),Source=runtime.Core.World.SpringId,Origin=DamageOrigin.Boss,Effects=curse.Effects}).State);
            // Use the production clock and driver with a night flow, isolating curse duration from waves/outcomes.
            var driver=new SimulationDriver(runtime.Clock,()=>new FlowSnapshot {Phase=GamePhase.Night},10);driver.Register(runtime.Combat);
            Guid pause=runtime.Clock.AcquirePause(PauseReason.Player);driver.Advance(30);
            Assert.AreEqual(0,runtime.Clock.GetSnapshot().GameTime);Assert.AreEqual(.5f,runtime.Combat.Status.GetModifiers(tower).AttackRateMultiplier);runtime.Clock.ReleasePause(pause);
            Assert.AreEqual(OperationState.Committed,runtime.Clock.TrySetSpeed(Command(),GameSpeed.Double).State);
            // Leave a small accumulator margin above ninety fixed ticks; fixedDelta is a float.
            driver.Advance(4.501f);Assert.AreEqual(9,runtime.Clock.GetSnapshot().GameTime,.0001);Assert.AreEqual(.5f,runtime.Combat.Status.GetModifiers(tower).AttackRateMultiplier);
            driver.Advance(.5f);Assert.AreEqual(10,runtime.Clock.GetSnapshot().GameTime,.0001);Assert.AreEqual(1,runtime.Combat.Status.GetModifiers(tower).AttackRateMultiplier);
        }
        [TestCase("enemy.slime.water",ElementType.Water,false)]
        [TestCase("enemy.slime.fire",ElementType.Fire,false)]
        [TestCase("enemy.slime.thunder",ElementType.Thunder,false)]
        [TestCase("enemy.slime.water",ElementType.Water,true)]
        [TestCase("enemy.slime.fire",ElementType.Fire,true)]
        [TestCase("enemy.slime.thunder",ElementType.Thunder,true)]
        public void SlimeAttackUsesElementResistanceAndNeverAttachesToSpring(string enemyId,ElementType element,bool ranged)
        {
            EnemyDefinition definition;Assert.IsTrue(runtime.Catalog.TryGetEnemy(enemyId,out definition));Assert.AreEqual(element,definition.Element);
            // Fixture-only ranged variant exercises the same fire path without changing delivered assets.
            IGameCatalog enemyCatalog=runtime.Catalog;
            if(ranged)
            {
                var asset=AssetDatabase.LoadAssetAtPath<PrototypeCatalogSO>(PrototypeCatalogFactory.CatalogPath);var data=asset.BuildCatalogData();
                foreach(var enemy in data.Enemies)if(enemy.Id==enemyId)enemy.ProjectileId="projectile.enemy";
                enemyCatalog=new GameCatalog(data);
            }
            var core=runtime.Core;var captured=new CapturedDamage(runtime.Combat.Damage);
            var capturedProjectiles=new CapturedProjectiles(runtime.Combat.Projectiles);
            var enemies=new EnemyServices(enemyCatalog,core.Board,core.World,core.Motion,core.Transactions,core.Commands,core.Ids,core.Random,runtime.Combat.Status,captured,capturedProjectiles,runtime.Combat.Effects,runtime.Events,runtime.Clock);
            enemies.BeginSession(new SessionStartContext {RunId=core.Store.GetFlowSnapshot().RunId,LevelId="level.prototype",Mode=GameMode.Story,DayIndex=1,SpringId=core.World.SpringId});
            Assert.AreEqual(OperationState.Committed,enemies.StartNight(1,GameMode.Story).State);EntityId slime;
            Assert.AreEqual(OperationState.Committed,enemies.TrySpawn(new EnemySpawnRequest {DefinitionId=enemyId,PathId="path.ground",SpawnPointId="spawn.ground",HpMultiplier=1,DamageMultiplier=1},out slime).State);
            ActorSnapshot spring;Assert.IsTrue(runtime.Core.World.TryGetActor(runtime.Core.World.SpringId,out spring));
            LevelDefinition level;Assert.IsTrue(runtime.Catalog.TryGetLevel("level.prototype",out level));
            Assert.AreEqual(OperationState.Committed,runtime.Core.Motion.TrySetPosition(slime,new WorldPoint(spring.Position.X-.25f*level.CellSize,spring.Position.Y)).State);
            float hit=-1;var subscription=runtime.Events.Subscribe<DamageAppliedEvent>(e=>{if(e.Result.Source.Equals(slime))hit=e.Result.FinalDamage;});
            enemies.Tick(new TickContext(++tick,0,0,TickStage.Attacks));Stage(0,0,TickStage.DamageAndReaction);
            ElementAmounts elementDamage;
            if(ranged)
            {
                Assert.NotNull(capturedProjectiles.Last);Assert.AreEqual(0,capturedProjectiles.Last.PhysicalDamage);Assert.AreEqual(default(ElementAmounts),capturedProjectiles.Last.Gauge);elementDamage=capturedProjectiles.Last.ElementDamage;
                for(int i=1;i<=20&&hit<0;i++){Stage(i*.1,.1f,TickStage.Projectiles);Stage(i*.1,0,TickStage.DamageAndReaction);}
            }
            else {Assert.NotNull(captured.Last);Assert.AreEqual(0,captured.Last.PhysicalDamage);Assert.AreEqual(default(ElementAmounts),captured.Last.Gauge);elementDamage=captured.Last.ElementDamage;}
            Assert.AreEqual(definition.PhysicalDamage,element==ElementType.Water?elementDamage.Water:element==ElementType.Fire?elementDamage.Fire:elementDamage.Thunder);
            Assert.AreEqual(definition.PhysicalDamage,hit);Assert.IsTrue(runtime.Core.World.TryGetActor(spring.Id,out spring));Assert.AreEqual(100-definition.PhysicalDamage,spring.CurrentHp);
            Assert.AreEqual(0,runtime.Combat.Status.GetStatuses(spring.Id).Count);foreach(var gauge in runtime.Combat.Status.GetGauges(spring.Id))Assert.AreEqual(0,gauge.Value);subscription.Dispose();enemies.EndSession();
        }
    }
}
#endif
