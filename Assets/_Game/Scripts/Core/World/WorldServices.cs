using System;
using System.Collections.Generic;
using Spotlight.Contracts;
namespace Spotlight.Core
{
    public sealed class WorldQuery : IWorldQuery
    {
        readonly SessionStateStore store;public WorldQuery(SessionStateStore store){this.store=store;}public EntityId SpringId { get{return store.SpringId;} }
        public bool TryGetActor(EntityId id,out ActorSnapshot snapshot){ActorSnapshot a;bool found=store.Actors.TryGetValue(id.Value,out a);snapshot=found?SessionStateStore.Copy(a):null;return found;}
        public IReadOnlyList<ActorSnapshot> GetActors(ActorKind kind){List<ActorSnapshot> result=new List<ActorSnapshot>();foreach(ActorSnapshot a in store.Actors.Values)if(a.Kind==kind)result.Add(SessionStateStore.Copy(a));result.Sort(delegate(ActorSnapshot a,ActorSnapshot b){return a.Id.Value.CompareTo(b.Id.Value);});return result.ToArray();}
        public IReadOnlyList<ActorSnapshot> QueryRadius(WorldPoint center,float radius,ActorKind kind){List<ActorSnapshot> result=new List<ActorSnapshot>();if(radius<0 || float.IsNaN(radius))return result.ToArray();foreach(ActorSnapshot a in GetActors(kind)){double x=a.Position.X-center.X,y=a.Position.Y-center.Y;if(x*x+y*y<=(double)radius*radius)result.Add(a);}return result.ToArray();}
        sealed class Hit {public double T;public ActorSnapshot Actor;}
        public IReadOnlyList<ActorSnapshot> TraceActors(WorldPoint from,WorldPoint to,float radius,ActorKind kind){List<Hit> hits=new List<Hit>();if(radius<0 || float.IsNaN(radius))return new ActorSnapshot[0];double dx=to.X-from.X,dy=to.Y-from.Y,aa=dx*dx+dy*dy;foreach(ActorSnapshot a in GetActors(kind)){double x=from.X-a.Position.X,y=from.Y-a.Position.Y,r=radius+a.CollisionRadius,c=x*x+y*y-r*r,t;if(c<=0)t=0;else{if(aa==0)continue;double b=2*(x*dx+y*dy),disc=b*b-4*aa*c;if(disc<0)continue;t=(-b-Math.Sqrt(disc))/(2*aa);if(t<0 || t>1)continue;}hits.Add(new Hit {T=t,Actor=a});}hits.Sort(delegate(Hit a,Hit b){int c=a.T.CompareTo(b.T);return c!=0?c:a.Actor.Id.Value.CompareTo(b.Actor.Id.Value);});List<ActorSnapshot> result=new List<ActorSnapshot>();foreach(Hit h in hits)result.Add(h.Actor);return result.ToArray();}
    }
    public sealed class WorldMotionWriter : IWorldMotionWriter
    {
        readonly SessionStateStore store;readonly IEventBus events;public WorldMotionWriter(SessionStateStore store,IEventBus events){this.store=store;this.events=events;}
        OperationResult Result(ErrorCode e){return new OperationResult(e==ErrorCode.None?OperationState.Committed:OperationState.Rejected,e,Guid.Empty,store.Progress.Revision,e==ErrorCode.None?null:"motion."+e.ToString());}
        public OperationResult TrySetPosition(EntityId id,WorldPoint p){Spotlight.Core.Events.GameEventBus bus=events as Spotlight.Core.Events.GameEventBus;if(bus!=null&&bus.IsDispatching)return Result(ErrorCode.WrongPhase);ActorSnapshot a;if(!store.Actors.TryGetValue(id.Value,out a))return Result(ErrorCode.InvalidTarget);if(float.IsNaN(p.X)||float.IsNaN(p.Y)||float.IsInfinity(p.X)||float.IsInfinity(p.Y))return Result(ErrorCode.InvalidArgument);if(a.Kind!=ActorKind.Enemy)return Result(ErrorCode.InvalidTarget);a.Position=p;if(events!=null)try{events.Publish(new ActorMovedEvent {Actor=id,Position=p});}catch(Exception){}return Result(ErrorCode.None);}
        public OperationResult TrySetTargetable(EntityId id,bool targetable){Spotlight.Core.Events.GameEventBus bus=events as Spotlight.Core.Events.GameEventBus;if(bus!=null&&bus.IsDispatching)return Result(ErrorCode.WrongPhase);ActorSnapshot a;if(!store.Actors.TryGetValue(id.Value,out a))return Result(ErrorCode.InvalidTarget);if(targetable && a.CurrentHp<=0)return Result(ErrorCode.TargetDead);a.Targetable=targetable;return Result(ErrorCode.None);}
    }
}


