using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Spotlight.Contracts;
using Spotlight.Presentation;
using UnityEngine;
using UnityEngine.UI;

namespace Spotlight.Tests
{
    public sealed class PPresentationTests
    {
        [Test] public void PoolReturnsIndependentLeaseAndResetsPresentationTransform()
        {
            ViewPool pool=new ViewPool(delegate(string key){GameObject o=new GameObject("visual");o.transform.localScale=new Vector3(2,2,1);o.AddComponent<SpriteRenderer>();return o;});
            PoolLease first=pool.Rent("tower");GameObject view=(GameObject)first.View;view.transform.position=new Vector3(7,4,0);view.transform.localScale=Vector3.one;view.GetComponent<SpriteRenderer>().color=Color.red;
            Assert.AreEqual(OperationState.Committed,pool.Return(first.LeaseId).State);Assert.AreEqual(OperationState.Rejected,pool.Return(first.LeaseId).State);
            PoolLease second=pool.Rent("tower");Assert.AreSame(view,second.View);Assert.AreNotEqual(first.LeaseId,second.LeaseId);Assert.AreEqual(Vector3.zero,view.transform.position);Assert.AreEqual(new Vector3(2,2,1),view.transform.localScale);Assert.AreEqual(Color.white,view.GetComponent<SpriteRenderer>().color);
            Assert.AreEqual(OperationState.Rejected,pool.Return(first.LeaseId).State);Assert.IsTrue(view.activeSelf);pool.Return(second.LeaseId);pool.Clear();
        }
        [Test] public void PreviewWidgetsUseReferenceResolutionAndLeaveBusinessUnbound()
        {
            GameObject host=new GameObject("UI test");GameView view=host.AddComponent<GameView>();view.PreviewOnly=true;view.TestWidgets();
            CanvasScaler scaler=host.GetComponentInChildren<CanvasScaler>();Assert.AreEqual(CanvasScaler.ScaleMode.ScaleWithScreenSize,scaler.uiScaleMode);Assert.AreEqual(new Vector2(1280,720),scaler.referenceResolution);
            Assert.GreaterOrEqual(host.GetComponentsInChildren<Button>().Length,14);Assert.AreEqual("level.prototype",view.defaultLevelId);Assert.AreEqual("tower.basic",view.towerId);UnityEngine.Object.DestroyImmediate(host);
        }
        [Test] public void ItemCancellationReleasesOnlyItsOwnPauseHandle()
        {
            GameObject host=new GameObject("pause UI test");GameView view=host.AddComponent<GameView>();view.TestWidgets();PauseClock clock=new PauseClock();Guid external=clock.AcquirePause(PauseReason.Modal);
            typeof(GameView).GetField("services",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(view,new GameViewContext {Clock=clock,Resources=new AvailableItems()});
            typeof(GameView).GetMethod("BeginItem",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(view,new object[] {1});Assert.AreEqual(2,clock.Active.Count);
            host.SetActive(false);Assert.AreEqual(2,clock.Active.Count,"Hiding the view must not clear another system's pause");
            typeof(GameView).GetMethod("CancelSelection",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(view,null);Assert.AreEqual(1,clock.Active.Count);Assert.IsTrue(clock.Active.Contains(external));UnityEngine.Object.DestroyImmediate(host);Assert.IsTrue(clock.Active.Contains(external));
        }
        private sealed class AvailableItems : IResourceQuery
        { public long GetQuantity(ResourceBucket bucket,string id){return 1;}public IReadOnlyList<ResourceAmount> GetSnapshot(ResourceBucket bucket){return new ResourceAmount[0];} }
        private sealed class PauseClock : IGameClock
        {
            internal readonly HashSet<Guid> Active=new HashSet<Guid>();
            public ClockSnapshot GetSnapshot(){return new ClockSnapshot {IsPaused=Active.Count>0};}
            public Guid AcquirePause(PauseReason reason){Guid id=Guid.NewGuid();Active.Add(id);return id;}
            public void ReleasePause(Guid id){Active.Remove(id);}
            public OperationResult TrySetSpeed(CommandContext c,GameSpeed s){return new OperationResult();}
        }
    }
}
