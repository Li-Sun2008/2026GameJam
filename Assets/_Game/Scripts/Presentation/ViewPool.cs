using System;
using System.Collections.Generic;
using Spotlight.Contracts;
using UnityEngine;

namespace Spotlight.Presentation
{
    /// <summary>Leases presentation instances only; returning a lease has no gameplay meaning.</summary>
    public sealed class ViewPool : IViewPool
    {
        private sealed class Entry
        {
            internal string Key; internal GameObject View; internal Vector3 Scale;
            internal SpriteRenderer[] Sprites; internal Color[] Colors;
        }
        private readonly Func<string,GameObject> factory;
        private readonly Dictionary<string,Stack<Entry>> free = new Dictionary<string,Stack<Entry>>(StringComparer.Ordinal);
        private readonly Dictionary<long,Entry> leased = new Dictionary<long,Entry>();
        private GameObject holder;
        private long sequence;
        public ViewPool(Func<string,GameObject> factories)
        { if(factories==null)throw new ArgumentNullException("factories");factory=factories;holder=new GameObject("Presentation View Pool");holder.SetActive(false); }
        public ViewPool(IDictionary<string,GameObject> factories) : this(MakeFactory(factories)) { }
        private static Func<string,GameObject> MakeFactory(IDictionary<string,GameObject> values)
        {
            if(values==null)throw new ArgumentNullException("factories");Dictionary<string,GameObject> copy=new Dictionary<string,GameObject>(values,StringComparer.Ordinal);
            return delegate(string key) { GameObject prefab;return copy.TryGetValue(key,out prefab)&&prefab!=null?UnityEngine.Object.Instantiate(prefab):null; };
        }
        public PoolLease Rent(string prefabKey)
        {
            if(holder==null){holder=new GameObject("Presentation View Pool");holder.SetActive(false);}
            if(String.IsNullOrEmpty(prefabKey))throw new ArgumentException("pool.prefab_key");Stack<Entry> stack;Entry entry=null;
            if(free.TryGetValue(prefabKey,out stack))while(stack.Count>0&&entry==null){Entry candidate=stack.Pop();if(candidate.View!=null)entry=candidate;}
            if(entry==null)
            {
                GameObject view=factory(prefabKey);if(view==null)throw new ArgumentException("pool.prefab_missing:"+prefabKey);
                entry=new Entry {Key=prefabKey,View=view,Scale=view.transform.localScale,Sprites=view.GetComponentsInChildren<SpriteRenderer>(true)};
                entry.Colors=new Color[entry.Sprites.Length];for(int i=0;i<entry.Sprites.Length;i++)entry.Colors[i]=entry.Sprites[i].color;
            }
            entry.View.transform.SetParent(null,false);entry.View.transform.localPosition=Vector3.zero;entry.View.transform.localRotation=Quaternion.identity;entry.View.transform.localScale=entry.Scale;entry.View.SetActive(true);
            long lease=++sequence;leased.Add(lease,entry);return new PoolLease {LeaseId=lease,PrefabKey=prefabKey,View=entry.View};
        }
        public OperationResult Return(long leaseId)
        {
            Entry entry;if(!leased.TryGetValue(leaseId,out entry))return new OperationResult(OperationState.Rejected,ErrorCode.InvalidArgument,Guid.Empty,0,"pool.invalid_lease");leased.Remove(leaseId);
            if(entry.View!=null)
            {
                foreach(ParticleSystem p in entry.View.GetComponentsInChildren<ParticleSystem>(true))p.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
                foreach(AudioSource a in entry.View.GetComponentsInChildren<AudioSource>(true))a.Stop();
                foreach(TrailRenderer trail in entry.View.GetComponentsInChildren<TrailRenderer>(true))trail.Clear();
                foreach(Rigidbody2D body in entry.View.GetComponentsInChildren<Rigidbody2D>(true)){body.velocity=Vector2.zero;body.angularVelocity=0;}
                for(int i=0;i<entry.Sprites.Length;i++)if(entry.Sprites[i]!=null)entry.Sprites[i].color=entry.Colors[i];
                entry.View.SetActive(false);entry.View.transform.SetParent(holder.transform,false);entry.View.transform.localPosition=Vector3.zero;entry.View.transform.localRotation=Quaternion.identity;entry.View.transform.localScale=entry.Scale;
                Stack<Entry> stack;if(!free.TryGetValue(entry.Key,out stack)){stack=new Stack<Entry>();free.Add(entry.Key,stack);}stack.Push(entry);
            }
            return new OperationResult(OperationState.Committed,ErrorCode.None,Guid.Empty,0,"pool.returned");
        }
        public void Clear()
        {
            foreach(Entry entry in leased.Values)if(entry.View!=null)PresentationObjects.Release(entry.View);leased.Clear();
            foreach(Stack<Entry> stack in free.Values)foreach(Entry entry in stack)if(entry.View!=null)PresentationObjects.Release(entry.View);free.Clear();
            if(holder!=null)PresentationObjects.Release(holder);holder=null;
        }
    }
}

