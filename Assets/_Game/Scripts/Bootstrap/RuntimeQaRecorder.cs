using System;
using System.Collections.Generic;
using System.IO;
using Spotlight.Contracts;
using UnityEngine;

namespace Spotlight.Bootstrap
{
    /// <summary>Read-only QA observer. No commands, state mutations or per-frame log output.</summary>
    public sealed class RuntimeQaRecorder : MonoBehaviour
    {
        [Serializable] public sealed class Quantity { public string bucket, definition; public long amount; }
        [Serializable] public sealed class Actor { public long id; public string kind, definition; public float hp, maxHp, x, y; }
        [Serializable] public sealed class Projectile { public long id, source; public float x, y, directionX, directionY; public int mask; }
        [Serializable] public sealed class Birth { public long id; public string definition, kind, phase, source; public int day; public double gameTime; }
        [Serializable] public sealed class Settlement
        {
            public string id, boundaryNote; public int night;
            public Quantity[] beforeLastObservedNightResult, rewards, afterAtEventDispatch;
        }
        [Serializable] public sealed class Snapshot
        {
            public string capturedUtc, runId, phase, speed, observationNote; public int day, aliveEnemies, totalObservedEnemyBirths;
            public bool attachedDuringActiveRun;
            public double gameTime; public long tick; public bool paused;
            public Quantity[] resources; public Actor[] actors; public Projectile[] projectiles; public Birth[] births; public Settlement[] settlements;
        }
        private ModuleComposition services;
        private readonly List<IDisposable> subscriptions = new List<IDisposable>();
        private readonly List<Birth> births = new List<Birth>();
        private readonly List<Settlement> settlements = new List<Settlement>();
        private readonly HashSet<long> seen = new HashSet<long>();
        private string runId;
        private bool attachedLate;
        private Quantity[] beforeSettlement;

        public static bool Requested
        {
            get
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                return true;
#else
                return HasArgument("-spotlightQa") || HasArgument("-spotlightSmoke");
#endif
            }
        }
        public static bool HasArgument(string value) { return Array.Exists(Environment.GetCommandLineArgs(), x => string.Equals(x, value, StringComparison.OrdinalIgnoreCase)); }
        public static string Argument(string value)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i + 1 < args.Length; i++) if (string.Equals(args[i], value, StringComparison.OrdinalIgnoreCase)) return args[i + 1];
            return null;
        }
        public void Bind(ModuleComposition value)
        {
            Unbind(); services = value; TrackRun(true);
            subscriptions.Add(services.Events.Subscribe<ActorSpawnedEvent>(OnBorn));
            subscriptions.Add(services.Events.Subscribe<DawnSettledEvent>(OnSettlement));
            Observe();
        }
        private void TrackRun(bool initialBind=false)
        {
            FlowSnapshot f = services.Flow.GetSnapshot(); if (runId == f.RunId) return;
            runId = f.RunId; births.Clear(); settlements.Clear(); seen.Clear(); beforeSettlement = null;
            // Only attaching to an already active session is a late observer. A subsequent
            // new run is observed from its beginning, even if its first event dispatch is DayEvent.
            attachedLate = initialBind && f.Phase != GamePhase.Menu && f.DayIndex > 0;
        }
        private void OnBorn(ActorSpawnedEvent e)
        {
            TrackRun(); if (!seen.Add(e.Actor.Value)) return;
            FlowSnapshot f = services.Flow.GetSnapshot();
            births.Add(new Birth { id=e.Actor.Value, definition=e.DefinitionId, kind=e.Kind.ToString(), day=f.DayIndex, phase=f.Phase.ToString(), gameTime=services.Clock.GetSnapshot().GameTime, source="ActorSpawnedEvent (dispatch time)" });
        }
        private void OnSettlement(DawnSettledEvent e)
        {
            TrackRun(); settlements.Add(new Settlement { id=e.SettlementId, night=e.CompletedNight, beforeLastObservedNightResult=beforeSettlement,
                rewards=Quantities(e.Rewards), afterAtEventDispatch=ReadResources(),
                boundaryNote="Before is last observed NightResult, not transaction interception; after is actual event-dispatch read. Queued events or commands in the same frame may change resources before dispatch. Rewards are the authoritative production deltas. Null before means observer missed the boundary." });
            beforeSettlement=null;
        }
        private void LateUpdate() { if (services != null) Observe(); }
        public void Observe()
        {
            if (services == null) return; TrackRun(); FlowSnapshot f=services.Flow.GetSnapshot();
            if (f.Phase==GamePhase.NightResult) beforeSettlement=ReadResources();
            foreach (ActorKind kind in Enum.GetValues(typeof(ActorKind))) foreach (ActorSnapshot a in services.Core.World.GetActors(kind))
                if (seen.Add(a.Id.Value)) births.Add(new Birth { id=a.Id.Value, definition=a.DefinitionId, kind=kind.ToString(), day=f.DayIndex, phase=f.Phase.ToString(), gameTime=services.Clock.GetSnapshot().GameTime, source="first seen snapshot (birth time unknown)" });
        }
        public Snapshot Capture()
        {
            Observe(); FlowSnapshot f=services.Flow.GetSnapshot(); ClockSnapshot clock=services.Clock.GetSnapshot();
            var actors=new List<Actor>(); foreach(ActorKind kind in Enum.GetValues(typeof(ActorKind))) foreach(ActorSnapshot a in services.Core.World.GetActors(kind))
                actors.Add(new Actor { id=a.Id.Value, definition=a.DefinitionId, kind=kind.ToString(), hp=a.CurrentHp, maxHp=a.MaxHp, x=a.Position.X, y=a.Position.Y });
            var projectiles=new List<Projectile>(); foreach(ProjectileSnapshot p in services.Combat.Projectiles.GetSnapshot())
                projectiles.Add(new Projectile { id=p.ProjectileId, source=p.Source.Value, x=p.Position.X, y=p.Position.Y, directionX=p.Direction.X, directionY=p.Direction.Y, mask=p.AppliedElementMask });
            int total=0; foreach(Birth b in births) if(b.kind==ActorKind.Enemy.ToString()) total++;
            return new Snapshot { capturedUtc=DateTime.UtcNow.ToString("o"),runId=runId,phase=f.Phase.ToString(),speed=clock.Speed.ToString(),attachedDuringActiveRun=attachedLate,day=f.DayIndex,gameTime=clock.GameTime,tick=clock.TickIndex,paused=clock.IsPaused,
                resources=ReadResources(),actors=actors.ToArray(),projectiles=projectiles.ToArray(),births=births.ToArray(),settlements=settlements.ToArray(),aliveEnemies=services.Core.World.GetActors(ActorKind.Enemy).Count,totalObservedEnemyBirths=total,
                observationNote=attachedLate ? "Attached during active run: past despawned births unavailable; totals are observed only. Day/phase/time are dispatch or first-observation values." : "Bound before run; ActorSpawnedEvent records persist after death. Day/phase/time are dispatch or first-observation values; loaded runs do not contain past birth history." };
        }
        private Quantity[] ReadResources()
        {
            var result=new List<Quantity>(); foreach(ResourceBucket bucket in Enum.GetValues(typeof(ResourceBucket))) result.AddRange(Quantities(services.Core.Resources.GetSnapshot(bucket))); return result.ToArray();
        }
        private static Quantity[] Quantities(IReadOnlyList<ResourceAmount> values)
        {
            var result=new List<Quantity>(); if(values!=null)foreach(ResourceAmount r in values)result.Add(new Quantity {bucket=r.Bucket.ToString(),definition=r.DefinitionId,amount=r.Amount});return result.ToArray();
        }
        public string Export()
        {
#if UNITY_EDITOR
            string directory=Path.GetFullPath(Path.Combine(Application.dataPath,"../QA/R3/Runtime"));
#else
            string directory=Path.Combine(Application.persistentDataPath,"QA","R3","Runtime");
#endif
            Directory.CreateDirectory(directory); string path=Path.Combine(directory,"snapshot-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff")+"-"+Guid.NewGuid().ToString("N")+".json");
            File.WriteAllText(path,JsonUtility.ToJson(Capture(),true));return path;
        }
        public void Unbind() { foreach(IDisposable subscription in subscriptions) subscription.Dispose();subscriptions.Clear();services=null;runId=null;births.Clear();settlements.Clear();seen.Clear();beforeSettlement=null;attachedLate=false; }
        private void OnDestroy() { Unbind(); }
    }
}
