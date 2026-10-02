using System;
using System.Collections.Generic;
using Spotlight.Contracts;
using Spotlight.Combat;
using Spotlight.Elements;

namespace Spotlight.Tests
{
    public static class CombatBoundaryTests
    {
        private static void Assert(bool value, string message) { if (!value) throw new Exception(message); }
        public static void Run()
        {
            CombatFixture f = new CombatFixture();
            CombatServices c = new CombatServices(f, f, f, f, f, f, f, new ElementRuleService(f, f), f);
            c.BeginSession(new SessionStartContext { RunId = "run", SpringId = new EntityId(1) });
            DamagePacket packet = new DamagePacket { PacketId = "one", Source = new EntityId(1), Target = new EntityId(2), PhysicalDamage = 10, ElementDamage = new ElementAmounts(10, 0, 0, 0, 0, 0) };
            DamageResult preview = c.Damage.Preview(packet);
            Assert(preview.FinalDamage == 10 && f.Actors[2].CurrentHp == 100 && c.PendingCount == 0, "Preview must apply independent reduction without mutation");
            c.Damage.Enqueue(packet); c.Damage.Enqueue(packet);
            c.Tick(new TickContext(1, 0, 0.1f, TickStage.DamageAndReaction));
            Assert(f.Actors[2].CurrentHp == 90 && c.PendingCount == 0, "Duplicate packet must not damage twice");
            EffectBatchRequest atomic = new EffectBatchRequest { Context = f.Create(f.Revision), Source = new EntityId(1), SelectedTarget = new EntityId(2),
                Effects = new EffectSpec[] { new EffectSpec { Kind = EffectKind.Damage, PhysicalDamage = 20 }, new EffectSpec { Kind = EffectKind.ApplyStatus, ReferenceId = "base.water", Stacks = 1 } } };
            f.FailCommit = true;
            Assert(c.Effects.TryExecute(atomic).State == OperationState.Rejected && f.Actors[2].CurrentHp == 90 && c.Status.GetStatuses(new EntityId(2)).Count == 0, "Rejected transaction must leave HP and statuses unchanged");
            f.FailCommit = false; OperationResult effectFirst=c.Effects.TryExecute(atomic);OperationResult effectRetry=c.Effects.TryExecute(atomic);
            Assert(f.Actors[2].CurrentHp == 80 && c.Status.GetStatuses(new EntityId(2))[0].Layers.Count == 1, "Effect duplicate must not replay local layers");
            Assert(effectFirst.Revision==effectRetry.Revision,"Same body retry must return the original effect receipt");
            atomic.Effects[0].PhysicalDamage=21;
            Assert(c.Effects.Validate(atomic).Error==ErrorCode.DuplicateCommand&&c.Effects.TryExecute(atomic).Error==ErrorCode.DuplicateCommand&&f.Actors[2].CurrentHp==80,"Same ID with changed effect body must reject without replay");
            atomic.Effects[0].PhysicalDamage=20;atomic.Context=new CommandContext(atomic.Context.CommandId,"other-run",atomic.Context.ExpectedRevision);
            Assert(c.Effects.TryExecute(atomic).Error==ErrorCode.DuplicateCommand,"Effect fingerprint includes run identity");
            c.Status.EnqueueGauge(new EntityId(1), new EntityId(2), new ElementAmounts(500, 0, 0, 0, 0, 0));
            c.Tick(new TickContext(2, 0, 0.1f, TickStage.DamageAndReaction));
            Assert(c.Status.GetGauges(new EntityId(2))[0].Value == 0 && c.Status.GetStatuses(new EntityId(2))[0].Layers.Count == 2, "Gauge threshold must clear overflow and create one layer");
            c.Status.ClearTransient(new EntityId(2));
            c.Status.EnqueueStatus(new StatusApplyRequest { Target = new EntityId(2), Source = new EntityId(1), StatusId = "base.water", Stacks = 4 });
            f.Reactions.Add(new ReactionDefinition { Id = "waterwater", Enabled = true, Context = ReactionContext.EnemyStatus, A = ElementType.Water, B = ElementType.Water, ConsumeA = 1, ConsumeB = 1,
                Effects = new EffectSpec[] { new EffectSpec { Kind = EffectKind.ApplyStatus, ReferenceId = "dot", Stacks = 1 } } });
            c.Tick(new TickContext(3, 0, 0.1f, TickStage.DamageAndReaction));
            Assert(c.PendingCount == 1 && c.Status.GetStatuses(new EntityId(2))[0].Layers.Count == 2, "Budget must preserve unconsumed reaction layers and pending work");
            Assert(c.Damage.PendingCount==c.PendingCount,"Frozen damage PendingCount must expose all pending gauge/status/reaction work to the outcome service");
            c.Tick(new TickContext(4, 4, 4, TickStage.StatusAndGauge));
            Assert(c.PendingCount >= 1 && c.Status.GetStatuses(new EntityId(2))[0].Layers.Count == 2, "Deferred exact input layers must survive expiry until execution");
            c.Tick(new TickContext(4, 4, 4, TickStage.DamageAndReaction));
            Assert(c.PendingCount == 0 && c.Status.GetStatuses(new EntityId(2)).Count == 1 && c.Status.GetStatuses(new EntityId(2))[0].Layers.Count == 1, "Deferred reaction must finish after expiry without losing reserved inputs");
            float before = f.Actors[2].CurrentHp;
            c.Tick(new TickContext(5, 5, 1, TickStage.StatusAndGauge));
            c.Tick(new TickContext(5, 5, 1, TickStage.DamageAndReaction));
            Assert(f.Actors[2].CurrentHp == before - 2 && c.Status.GetStatuses(new EntityId(2)).Count == 0, "DOT at exact expiry must execute final tick before removing layers");
            c.EndSession();
            NightBoundaryClearsTransientButRetainsReceipts();
        }
        private static void NightBoundaryClearsTransientButRetainsReceipts()
        {
            CombatFixture f=new CombatFixture();CombatServices c=new CombatServices(f,f,f,f,f,f,f,new ElementRuleService(f,f),f);EntityId target=new EntityId(2);
            c.BeginSession(new SessionStartContext {RunId="run",SpringId=new EntityId(1)});f.Publish(new PhaseChangedEvent {NewPhase=GamePhase.Night});
            EffectBatchRequest effect=new EffectBatchRequest {Context=f.Create(f.Revision),Source=new EntityId(1),SelectedTarget=target,Effects=new EffectSpec[] {new EffectSpec {Kind=EffectKind.ApplyStatus,ReferenceId="base.water",Stacks=1},new EffectSpec {Kind=EffectKind.AddGauge,Gauge=new ElementAmounts(5,0,0,0,0,0)}}};
            OperationResult effectResult=c.Effects.TryExecute(effect);Assert(effectResult.State==OperationState.Committed,"Night setup effect must commit");long layer=c.Status.GetStatuses(target)[0].Layers[0].LayerId;
            CommandContext skill=f.Create(f.Revision);Assert(c.Skill.TryUse(skill).State==OperationState.Queued,"Night skill must queue");c.Tick(new TickContext(1,0,0.1f,TickStage.PlayerCommands));c.Tick(new TickContext(1,0,0.1f,TickStage.DamageAndReaction));
            Assert(c.Status.GetGauges(target)[0].Value==5&&c.Skill.GetSnapshot().RemainingSeconds==10,"Night transient setup must be live");
            f.Publish(new PhaseChangedEvent {OldPhase=GamePhase.Night,NewPhase=GamePhase.NightResult});
            Assert(c.Status.GetStatuses(target).Count==0&&c.Status.GetGauges(target)[0].Value==0&&c.Skill.GetSnapshot().RemainingSeconds==0&&c.PendingCount==0,"Normal night result must clear statuses, gauges, skill duration and pending reactions");
            Assert(c.Effects.TryExecute(effect).Revision==effectResult.Revision&&c.Status.GetStatuses(target).Count==0,"A prior effect receipt survives night cleanup without reapplying layers");
            f.Publish(new PhaseChangedEvent {OldPhase=GamePhase.Build,NewPhase=GamePhase.Night});
            Assert(c.Skill.GetSnapshot().UsesRemaining==1&&c.Skill.TryUse(skill).State==OperationState.Committed&&c.Skill.GetSnapshot().UsesRemaining==1,"Old skill receipt cannot consume next night's use");
            c.Status.EnqueueStatus(new StatusApplyRequest {Source=new EntityId(1),Target=target,StatusId="base.water",Stacks=1});c.Tick(new TickContext(2,1,0.1f,TickStage.DamageAndReaction));
            Assert(c.Status.GetStatuses(target)[0].Layers[0].LayerId>layer,"Layer identities must not be reused across nights");c.EndSession();
        }
    }

    public sealed class CombatFixture : IGameCatalog, IBoardQuery, IWorldQuery, IStateTransactionService, ICommandContextFactory, IEntityIdService, IEventBus, IGameClock, IRandomService
    {
        public readonly Dictionary<long, ActorSnapshot> Actors = new Dictionary<long, ActorSnapshot>();
        public readonly List<ReactionDefinition> Reactions = new List<ReactionDefinition>();
        public readonly List<ProductionRuleDefinition> Production = new List<ProductionRuleDefinition>();
        public readonly Dictionary<string, ElementDefinition> Elements = new Dictionary<string, ElementDefinition>();
        public bool FailCommit; public long Revision { get; private set; }
        private long sequence = 10;
        public CombatFixture()
        {
            Actors.Add(1, new ActorSnapshot { Id = new EntityId(1), Kind = ActorKind.Spring, CurrentHp = 100, MaxHp = 100, Targetable = true });
            Actors.Add(2, new ActorSnapshot { Id = new EntityId(2), Kind = ActorKind.Enemy, CurrentHp = 100, MaxHp = 100, Targetable = true, PhysicalReduction = 0.5f, ElementResistance = new ElementAmounts(0.5f, 0, 0, 0, 0, 0) });
            Elements.Add("elm.water", new ElementDefinition { Id = "elm.water", Element = ElementType.Water, BaseStatusId = "base.water", ProjectileModifier = new ProjectileModifierDefinition { Element = ElementType.Water } });
        }
        public GameSettings GetSettings() { return new GameSettings { MaxReactionActionsPerTick = 1, SpecialSkillId = "skill", LogicTicksPerGameSecond = 10 }; }
        public bool TryGetElement(string id, out ElementDefinition value) { return Elements.TryGetValue(id, out value); }
        public bool TryGetStatus(string id, out StatusDefinition value)
        { value = null; if (id == "base.water") value = new StatusDefinition { Id = id, ElementTag = ElementType.Water, IsReactionInput = true, MaxStacks = 5, DurationSeconds = 3, RefreshPolicy = StatusRefreshPolicy.IndependentLayers }; if (id == "dot") value = new StatusDefinition { Id = id, IsReactionDamage = true, MaxStacks = 5, DurationSeconds = 1, TickIntervalSeconds = 1, DotElementPerStack = new ElementAmounts(0, 0, 0, 0, 0, 2) }; return value != null; }
        public IReadOnlyList<ProductionRuleDefinition> GetProductionRules() { return Production.AsReadOnly(); }
        public IReadOnlyList<ReactionDefinition> GetReactionRules(ReactionContext context) { return Reactions.FindAll(delegate(ReactionDefinition r) { return r.Context == context; }).AsReadOnly(); }
        public IReadOnlyList<GaugeDefinition> GetGaugeDefinitions() { return new GaugeDefinition[] { new GaugeDefinition { Element = ElementType.Water, Threshold = 10, MaxValue = 100, IncomingMultiplier = 1, DecayDelaySeconds = 1, DecayPerSecond = 1 } }; }
        public bool TryGetLevel(string id, out LevelDefinition value) { value = new LevelDefinition { Id = "level", CellSize = 1 }; return true; }
        public bool TryGetSkill(string id, out SpecialSkillDefinition value) { value = new SpecialSkillDefinition { Id = "skill", UsesPerNight = 1, DurationSeconds = 10, ReactionDamageMultiplier = 1.2f }; return true; }
        public bool TryGetTower(string id, out TowerDefinition value) { value = null; return false; }
        public bool TryGetProjectile(string id, out ProjectileDefinition value) { value = null; return false; }
        public bool TryGetEnemy(string id, out EnemyDefinition value) { value = null; return false; }
        public bool TryGetItem(string id, out ItemDefinition value) { value = null; return false; }
        public bool TryGetResource(string id, out ResourceDefinition value) { value = null; return false; }
        public bool TryGetEvent(string id, out DayEventDefinition value) { value = null; return false; }
        public bool TryGetBossSkill(string id, out BossSkillDefinition value) { value = null; return false; }
        public bool TryGetDropTable(string id, out DropTableDefinition value) { value = null; return false; }
        public EndlessDefinition GetEndlessDefinition() { return null; }
        public IReadOnlyList<DayEventDefinition> GetDayEvents() { return new DayEventDefinition[0]; }
        public IReadOnlyList<WaveDefinition> GetWaves(int dayIndex, GameMode mode) { return new WaveDefinition[0]; }
        public IReadOnlyList<string> ValidateAll() { return new string[0]; }
        public BoardSnapshot GetSnapshot() { return new BoardSnapshot { LevelId = "level", Revision = Revision, Cells = new CellSnapshot[0] }; }
        public bool TryGetCell(CellCoord cell, out CellSnapshot value) { value = null; return false; }
        public IReadOnlyList<CellCoord> GetNeighbors4(CellCoord cell) { return new CellCoord[0]; }
        public WorldPoint CellToWorld(CellCoord cell) { return new WorldPoint(cell.X + 0.5f, cell.Y + 0.5f); }
        public bool TryWorldToCell(WorldPoint point, out CellCoord cell) { cell = new CellCoord((int)Math.Floor(point.X), (int)Math.Floor(point.Y)); return true; }
        public IReadOnlyList<CellSnapshot> TraceSegment(WorldPoint a, WorldPoint b) { return new CellSnapshot[0]; }
        public EntityId SpringId { get { return new EntityId(1); } }
        public bool TryGetActor(EntityId id, out ActorSnapshot value) { return Actors.TryGetValue(id.Value, out value); }
        public IReadOnlyList<ActorSnapshot> GetActors(ActorKind kind) { List<ActorSnapshot> a = new List<ActorSnapshot>(); foreach (ActorSnapshot actor in Actors.Values) if (actor.Kind == kind) a.Add(actor); return a.AsReadOnly(); }
        public IReadOnlyList<ActorSnapshot> QueryRadius(WorldPoint p, float r, ActorKind kind) { return GetActors(kind); }
        public IReadOnlyList<ActorSnapshot> TraceActors(WorldPoint a, WorldPoint b, float r, ActorKind kind) { return GetActors(kind); }
        public ValidationResult Validate(StateMutationBatch batch) { return new ValidationResult(ErrorCode.None, "valid"); }
        public OperationResult TryCommit(StateMutationBatch batch)
        {
            if (FailCommit) return new OperationResult(OperationState.Rejected, ErrorCode.VersionConflict, batch.Context.CommandId, Revision, "test.fail");
            if (batch.Actors != null) foreach (ActorMutation m in batch.Actors) if (m.Kind == ActorMutationKind.SetHealth) { Actors[m.ActorId.Value].CurrentHp = m.CurrentHp; Actors[m.ActorId.Value].MaxHp = m.MaxHp; }
            Revision++; return new OperationResult(OperationState.Committed, ErrorCode.None, batch.Context.CommandId, Revision, "done");
        }
        public CommandContext Create(long revision) { return new CommandContext(Guid.NewGuid(), "run", revision); }
        public EntityId Allocate() { return new EntityId(++sequence); }
        public long AllocateSpawnSequence() { return ++sequence; }
        private readonly Dictionary<Type,List<object>> listeners=new Dictionary<Type,List<object>>();
        private sealed class Disposable : IDisposable { internal Action Remove;public void Dispose() {if(Remove!=null)Remove();Remove=null;} }
        public IDisposable Subscribe<T>(Action<T> handler) where T : class
        {List<object> list;if(!listeners.TryGetValue(typeof(T),out list)){list=new List<object>();listeners.Add(typeof(T),list);}list.Add(handler);return new Disposable {Remove=delegate {list.Remove(handler);}};}
        public void Publish<T>(T value) where T : class
        {List<object> list;if(listeners.TryGetValue(typeof(T),out list))foreach(object handler in new List<object>(list))((Action<T>)handler)(value);}
        ClockSnapshot IGameClock.GetSnapshot() { return new ClockSnapshot(); }
        public OperationResult TrySetSpeed(CommandContext context, GameSpeed speed) { return new OperationResult(); }
        public Guid AcquirePause(PauseReason reason) { return Guid.NewGuid(); }
        public void ReleasePause(Guid handle) { }
        public RandomState GetState(RandomStream stream) { return new RandomState(stream, 1); }
        private sealed class Cursor : IRandomCursor { private RandomState state; internal Cursor(RandomState state) { this.state = state; } public int NextInt(int a, int b) { state = new RandomState(state.Stream, state.State + 1); return a; } public float NextUnitFloat() { return 0; } public RandomState Capture() { return state; } }
        public IRandomCursor CreateCursor(RandomState state) { return new Cursor(state); }
    }
}
