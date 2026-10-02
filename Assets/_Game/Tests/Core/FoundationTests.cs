using System;
using System.Collections.Generic;
using NUnit.Framework;
using Spotlight.Contracts;
using Spotlight.Core;
namespace Spotlight.Tests.Core
{
    public sealed class FoundationTests
    {
        CoreServices core;TestCatalog catalog;
        [SetUp] public void Setup(){catalog=new TestCatalog();core=new CoreServices(catalog,null);Assert.AreEqual(ErrorCode.None,core.Store.Initialize(new NewRunRequest {LevelId="level",Seed=123}).Error);}
        CommandContext Command(){return core.Commands.Create(core.Transactions.Revision);}
        [Test] public void NegativeWorldCoordinatesUseFloorAndNeighborsRespectRectangularBoard(){CellCoord c;Assert.IsFalse(core.Board.TryWorldToCell(new WorldPoint(-0.1f,0.5f),out c));Assert.AreEqual(-1,c.X);IReadOnlyList<CellCoord> ns=core.Board.GetNeighbors4(new CellCoord(1,1));Assert.AreEqual(new CellCoord(1,2),ns[0]);Assert.AreEqual(new CellCoord(2,1),ns[1]);Assert.AreEqual(4,ns.Count);}
        [Test] public void CornerTouchDoesNotCreateExtraSweptCells(){IReadOnlyList<CellSnapshot> cells=core.Board.TraceSegment(new WorldPoint(0.5f,0.5f),new WorldPoint(2.5f,2.5f));Assert.AreEqual(3,cells.Count);Assert.AreEqual(new CellCoord(1,1),cells[1].Cell);}
        [Test] public void SwapMovesTowerProjectionAndPreservesHealth(){Assert.AreEqual(OperationState.Committed,core.Deployment.TryPlace(Command(),new PlacementRequest {Kind=OccupantKind.Tower,DefinitionId="tower",Cell=new CellCoord(1,1)}).State);Assert.AreEqual(OperationState.Committed,core.Deployment.TryPlace(Command(),new PlacementRequest {Kind=OccupantKind.ElementBlock,DefinitionId="water",Cell=new CellCoord(2,1)}).State);ActorSnapshot tower=core.World.GetActors(ActorKind.Tower)[0];core.Transactions.TryCommit(new StateMutationBatch {Context=Command(),Actors=new ActorMutation[] {new ActorMutation {Kind=ActorMutationKind.SetHealth,ActorId=tower.Id,CurrentHp=4,MaxHp=10}}});Assert.AreEqual(OperationState.Committed,core.Deployment.TrySwap(Command(),new CellCoord(1,1),new CellCoord(2,1)).State);ActorSnapshot moved;core.World.TryGetActor(tower.Id,out moved);Assert.AreEqual(2.5f,moved.Position.X);Assert.AreEqual(4,moved.CurrentHp);}
        [Test] public void FailedBatchLeavesResourcesRandomAndBoardUntouched(){long revision=core.Transactions.Revision;RandomState before=core.Random.GetState(RandomStream.Drop);IRandomCursor cursor=core.Random.CreateCursor(before);cursor.NextInt(0,10);StateMutationBatch batch=new StateMutationBatch {Context=Command(),ResourceDeltas=new ResourceAmount[] {new ResourceAmount(ResourceBucket.Hand,"water",-100)},Random=new RandomStateWrite[] {new RandomStateWrite {Before=before,After=cursor.Capture()}},Board=new BoardMutation[] {new BoardMutation {Kind=BoardMutationKind.Place,EntityId=new EntityId(50),OccupantKind=OccupantKind.ElementBlock,DefinitionId="water",To=new CellCoord(1,1)}}};Assert.AreEqual(OperationState.Rejected,core.Transactions.TryCommit(batch).State);Assert.AreEqual(revision,core.Transactions.Revision);Assert.AreEqual(before.State,core.Random.GetState(RandomStream.Drop).State);CellSnapshot cell;core.Board.TryGetCell(new CellCoord(1,1),out cell);Assert.AreEqual(OccupantKind.None,cell.OccupantKind);Assert.AreEqual(5,core.Resources.GetQuantity(ResourceBucket.Hand,"water"));}
        [Test] public void IdempotencyReturnsOriginalRevisionAndRejectsAlteredBatch(){StateMutationBatch batch=new StateMutationBatch {Context=Command(),ResourceDeltas=new ResourceAmount[] {new ResourceAmount(ResourceBucket.Hand,"water",1)}};OperationResult first=core.Transactions.TryCommit(batch);Assert.AreEqual(first.Revision,core.Transactions.TryCommit(batch).Revision);Assert.AreEqual(6,core.Resources.GetQuantity(ResourceBucket.Hand,"water"));batch.ResourceDeltas=new ResourceAmount[] {new ResourceAmount(ResourceBucket.Hand,"water",2)};Assert.AreEqual(ErrorCode.DuplicateCommand,core.Transactions.TryCommit(batch).Error);}
        [Test] public void DetachedSnapshotsAndCursorsCannotMutateLiveState(){BoardSnapshot board=core.Board.GetSnapshot();board.Cells[0].Reserved=false;ActorSnapshot spring;core.World.TryGetActor(core.World.SpringId,out spring);spring.CurrentHp=0;Assert.IsTrue(core.Store.GetFlowSnapshot().CanStartNight);RandomState before=core.Random.GetState(RandomStream.Production);IRandomCursor a=core.Random.CreateCursor(before),b=core.Random.CreateCursor(before);for(int i=0;i<100;i++)Assert.AreEqual(a.NextInt(int.MinValue,int.MaxValue),b.NextInt(int.MinValue,int.MaxValue));Assert.AreEqual(before.State,core.Random.GetState(RandomStream.Production).State);}
        [Test] public void CorruptRestorePreservesCurrentRun(){CheckpointData saved=core.Store.CaptureCheckpoint(catalog);saved.SpringHp=-1;string run=core.Store.GetFlowSnapshot().RunId;Assert.AreEqual(ErrorCode.SaveCorrupt,core.Store.Restore(saved).Error);Assert.AreEqual(run,core.Store.GetFlowSnapshot().RunId);}
        [Test] public void SavedCommandHistoryRestoresExactReceiptAndRejectsDifferentBody()
        {
            StateMutationBatch batch=new StateMutationBatch {Context=Command(),ResourceDeltas=new ResourceAmount[] {new ResourceAmount(ResourceBucket.Hand,"water",1)}};
            OperationResult first=core.Transactions.TryCommit(batch);CheckpointData checkpoint=core.Store.CaptureCheckpoint(catalog);CommandHistoryRecord[] history=core.Store.ExportCommandHistory();CoreServices loaded=new CoreServices(catalog,null);
            Assert.AreEqual(ErrorCode.None,loaded.Store.Restore(checkpoint).Error);
            Assert.AreEqual(ErrorCode.DuplicateCommand,loaded.Transactions.TryCommit(batch).Error); // 旧档只有ID，不猜测请求内容。
            Assert.AreEqual(ErrorCode.None,loaded.Store.RestoreCommandHistory(history).Error);
            Assert.AreEqual(first.Revision,loaded.Transactions.TryCommit(batch).Revision);
            batch.ResourceDeltas=new ResourceAmount[] {new ResourceAmount(ResourceBucket.Hand,"water",2)};
            Assert.AreEqual(ErrorCode.DuplicateCommand,loaded.Transactions.TryCommit(batch).Error);
            Assert.AreEqual(6,loaded.Resources.GetQuantity(ResourceBucket.Hand,"water"));
        }
        [Test] public void EventSubscriberCannotReenterBusinessTransaction()
        {
            Spotlight.Core.Events.GameEventBus bus=new Spotlight.Core.Events.GameEventBus();CoreServices services=new CoreServices(catalog,bus);services.Store.Initialize(new NewRunRequest {LevelId="level",Seed=1});ErrorCode error=ErrorCode.None;
            bus.Subscribe<ResourcesChangedEvent>(delegate(ResourcesChangedEvent change){error=services.Transactions.TryCommit(new StateMutationBatch {Context=services.Commands.Create(-1),ResourceDeltas=new ResourceAmount[] {new ResourceAmount(ResourceBucket.Hand,"water",1)}}).Error;});
            services.Transactions.TryCommit(new StateMutationBatch {Context=services.Commands.Create(-1),ResourceDeltas=new ResourceAmount[] {new ResourceAmount(ResourceBucket.Hand,"water",1)}});bus.Flush();
            Assert.AreEqual(ErrorCode.WrongPhase,error);Assert.AreEqual(6,services.Resources.GetQuantity(ResourceBucket.Hand,"water"));
        }
        [Test] public void PublicLevelCopyCannotChangeWorldConversion()
        {
            LevelDefinition copy=core.Store.Level;copy.CellSize=99;copy.Origin=new WorldPoint(100,100);Assert.AreEqual(1.5f,core.Board.CellToWorld(new CellCoord(1,1)).X);
        }
        sealed class TestCatalog:IGameCatalog
        {
            public GameSettings GetSettings(){return new GameSettings {ConfigVersion="test",SpringMaxHp=100,SpringCollisionRadiusInCells=.35f};}
            public bool TryGetLevel(string id,out LevelDefinition value){value=new LevelDefinition {Id="level",Rows=3,Columns=5,CellSize=1,SpringCell=new CellCoord(0,0),InitialResources=new ResourceAmount[] {new ResourceAmount(ResourceBucket.Hand,"water",5)},InitialOccupants=new InitialOccupant[0],ReservedCells=new CellCoord[0],SpawnPoints=new SpawnPointDefinition[0],Paths=new PathDefinition[0]};return id=="level";}
            public bool TryGetElement(string id,out ElementDefinition value){value=new ElementDefinition {Id="water",Element=ElementType.Water};return id=="water";}
            public bool TryGetTower(string id,out TowerDefinition value){value=new TowerDefinition {Id="tower",HpPerStack=10,InitialAttackStacks=1,MaxAttackStacks=5,CollisionRadiusInCells=.35f,BuildCost=new ResourceAmount[0]};return id=="tower";}
            public bool TryGetProjectile(string id,out ProjectileDefinition value){value=null;return false;}public bool TryGetStatus(string id,out StatusDefinition value){value=null;return false;}public bool TryGetEnemy(string id,out EnemyDefinition value){value=null;return false;}public bool TryGetItem(string id,out ItemDefinition value){value=null;return false;}public bool TryGetResource(string id,out ResourceDefinition value){value=null;return false;}public bool TryGetEvent(string id,out DayEventDefinition value){value=null;return false;}public bool TryGetBossSkill(string id,out BossSkillDefinition value){value=null;return false;}public bool TryGetDropTable(string id,out DropTableDefinition value){value=null;return false;}public bool TryGetSkill(string id,out SpecialSkillDefinition value){value=null;return false;}public EndlessDefinition GetEndlessDefinition(){return null;}public IReadOnlyList<ProductionRuleDefinition> GetProductionRules(){return new ProductionRuleDefinition[0];}public IReadOnlyList<ReactionDefinition> GetReactionRules(ReactionContext context){return new ReactionDefinition[0];}public IReadOnlyList<GaugeDefinition> GetGaugeDefinitions(){return new GaugeDefinition[0];}public IReadOnlyList<DayEventDefinition> GetDayEvents(){return new DayEventDefinition[0];}public IReadOnlyList<WaveDefinition> GetWaves(int dayIndex,GameMode mode){return new WaveDefinition[0];}public IReadOnlyList<string> ValidateAll(){return new string[0];}
        }
    }
}
