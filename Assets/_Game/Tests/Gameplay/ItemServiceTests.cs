#if UNITY_INCLUDE_TESTS
using System;
using System.Collections.Generic;
using NUnit.Framework;
using Spotlight.Contracts;
using Spotlight.Gameplay;
namespace Spotlight.Tests.Gameplay {
public sealed class ItemServiceTests {
 sealed class Catalog : IGameCatalog {
  public ItemDefinition Item=new ItemDefinition{Id="item",AllowedPhases=new[]{GamePhase.Night},StackingGroup="buff",ActiveDurationSeconds=10,Effects=new EffectSpec[0]};
  public bool TryGetItem(string id,out ItemDefinition value){value=Item;return id==Item.Id;}
  public GameSettings GetSettings(){return null;}public EndlessDefinition GetEndlessDefinition(){return null;}
  public bool TryGetLevel(string id,out LevelDefinition v){v=null;return false;}public bool TryGetElement(string id,out ElementDefinition v){v=null;return false;}public bool TryGetTower(string id,out TowerDefinition v){v=null;return false;}public bool TryGetProjectile(string id,out ProjectileDefinition v){v=null;return false;}public bool TryGetStatus(string id,out StatusDefinition v){v=null;return false;}public bool TryGetEnemy(string id,out EnemyDefinition v){v=null;return false;}public bool TryGetResource(string id,out ResourceDefinition v){v=null;return false;}public bool TryGetEvent(string id,out DayEventDefinition v){v=null;return false;}public bool TryGetBossSkill(string id,out BossSkillDefinition v){v=null;return false;}public bool TryGetDropTable(string id,out DropTableDefinition v){v=null;return false;}public bool TryGetSkill(string id,out SpecialSkillDefinition v){v=null;return false;}
  public IReadOnlyList<ProductionRuleDefinition> GetProductionRules(){return new ProductionRuleDefinition[0];}public IReadOnlyList<ReactionDefinition> GetReactionRules(ReactionContext c){return new ReactionDefinition[0];}public IReadOnlyList<GaugeDefinition> GetGaugeDefinitions(){return new GaugeDefinition[0];}public IReadOnlyList<DayEventDefinition> GetDayEvents(){return new DayEventDefinition[0];}public IReadOnlyList<WaveDefinition> GetWaves(int d,GameMode m){return new WaveDefinition[0];}public IReadOnlyList<string> ValidateAll(){return new string[0];}
 }
 sealed class State : IStateTransactionService,ICommandContextFactory,IResourceQuery,IEffectService,IWorldQuery {
  public int Executions;public long Quantity=2;public long Revision{get{return 4;}}public EntityId SpringId{get{return new EntityId(1);}}
  public CommandContext Create(long r){return new CommandContext(Guid.NewGuid(),"run",r);}public ValidationResult Validate(StateMutationBatch b){return new ValidationResult(ErrorCode.None,null);}public OperationResult TryCommit(StateMutationBatch b){throw new NotSupportedException();}
  public long GetQuantity(ResourceBucket b,string id){return Quantity;}public IReadOnlyList<ResourceAmount> GetSnapshot(ResourceBucket b){return new ResourceAmount[0];}public ValidationResult Validate(EffectBatchRequest r){return new ValidationResult(ErrorCode.None,null);}public OperationResult TryExecute(EffectBatchRequest r){Executions++;throw new NotSupportedException();}
  public bool TryGetActor(EntityId id,out ActorSnapshot a){a=null;return false;}public IReadOnlyList<ActorSnapshot> GetActors(ActorKind k){return new ActorSnapshot[0];}public IReadOnlyList<ActorSnapshot> QueryRadius(WorldPoint p,float r,ActorKind k){return new ActorSnapshot[0];}public IReadOnlyList<ActorSnapshot> TraceActors(WorldPoint a,WorldPoint b,float r,ActorKind k){return new ActorSnapshot[0];}
 }
 [Test] public void NightPendingBlocksSameGroupAndSameCommandDoesNotQueueTwice(){var s=new State();var c=new Catalog();var service=new GameplayServices(c,s,s,s,null,s,null,null,delegate{return new FlowSnapshot{RunId="run",Phase=GamePhase.Night};},s,delegate{return null;});service.BeginSession(new SessionStartContext());var command=s.Create(4);var request=new ItemUseRequest{ItemId="item"};Assert.AreEqual(OperationState.Queued,service.TryUse(command,request).State);Assert.AreEqual(OperationState.Queued,service.TryUse(command,request).State);Assert.AreEqual(1,service.GetActiveGroups().Count);Assert.AreEqual(ErrorCode.DuplicateCommand,service.TryUse(command,new ItemUseRequest{ItemId="item",Target=new EntityId(99)}).Error);Assert.AreEqual(ErrorCode.EffectAlreadyActive,service.TryUse(s.Create(4),request).Error);Assert.AreEqual(0,s.Executions);Assert.AreEqual(2,s.Quantity);}
 [Test] public void InsufficientInventoryRejectsBeforeEffectExecution(){var s=new State{Quantity=0};var service=new GameplayServices(new Catalog(),s,s,s,null,s,null,null,delegate{return new FlowSnapshot{RunId="run",Phase=GamePhase.Night};},s,delegate{return null;});service.BeginSession(new SessionStartContext());Assert.AreEqual(ErrorCode.InsufficientQuantity,service.TryUse(s.Create(4),new ItemUseRequest{ItemId="item"}).Error);Assert.AreEqual(0,s.Executions);}
}
}
#endif

