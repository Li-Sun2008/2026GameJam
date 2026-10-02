// Generated verbatim from Docs/Contracts/Spotlight.Contracts.cs (2.1.0).
using System;
using System.Collections.Generic;

namespace Spotlight.Contracts
{
    public interface IEventBus
    {
        IDisposable Subscribe<TEvent>(Action<TEvent> handler) where TEvent : class;
        void Publish<TEvent>(TEvent value) where TEvent : class;
    }
}

