using System;
using System.Collections.Generic;
using Spotlight.Contracts;
using Spotlight.Core.Data;

namespace Spotlight.Core.Events
{
    public sealed class GameEventBus : IEventBus, ITickSystem
    {
        private sealed class Subscription : IDisposable
        {
            public bool Active = true;
            public Action<object> Handler;
            public void Dispose() { Active = false; Handler = null; }
        }
        private readonly Dictionary<Type, List<Subscription>> listeners = new Dictionary<Type, List<Subscription>>();
        private readonly Queue<Action> pending = new Queue<Action>();
        private bool flushing;
        public Action<Exception> OnListenerError;
        public bool IsDispatching { get { return flushing; } }
        public ModuleId Module { get { return ModuleId.Core; } }
        public IReadOnlyList<TickStage> Stages { get { return new[] { TickStage.Publish }; } }
        public IDisposable Subscribe<TEvent>(Action<TEvent> handler) where TEvent : class
        {
            if (handler == null) throw new ArgumentNullException("handler");
            List<Subscription> list;
            if (!listeners.TryGetValue(typeof(TEvent), out list)) { list = new List<Subscription>(); listeners.Add(typeof(TEvent), list); }
            Subscription subscription = new Subscription { Handler = delegate(object value) { handler((TEvent)value); } };
            list.Add(subscription);
            return subscription;
        }
        public void Publish<TEvent>(TEvent value) where TEvent : class
        {
            if (value == null) throw new ArgumentNullException("value");
            TEvent copy = DtoCopy.Clone(value);
            pending.Enqueue(delegate { Dispatch(typeof(TEvent), copy); });
        }
        private void Dispatch(Type type, object value)
        {
            List<Subscription> list;
            if (!listeners.TryGetValue(type, out list)) return;
            Subscription[] copy = list.ToArray();
            foreach (Subscription item in copy)
            {
                if (!item.Active) continue;
                try { item.Handler(value); }
                catch (Exception error) { if (OnListenerError != null) OnListenerError(error); }
            }
            list.RemoveAll(delegate(Subscription item) { return !item.Active; });
        }
        public void Flush()
        {
            if (flushing) return;
            flushing = true;
            try
            {
                int count = pending.Count;
                for (int i = 0; i < count; i++) pending.Dequeue()();
            }
            finally { flushing = false; }
        }
        public void Tick(TickContext context) { if (context.Stage == TickStage.Publish) Flush(); }
        public void ClearPending() { pending.Clear(); }
        public void Clear() { pending.Clear(); foreach (List<Subscription> list in listeners.Values) foreach (Subscription item in list) item.Dispose(); listeners.Clear(); }
    }
}

