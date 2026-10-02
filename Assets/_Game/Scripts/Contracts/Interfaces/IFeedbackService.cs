// Generated verbatim from Docs/Contracts/Spotlight.Contracts.cs (2.1.0).
using System;
using System.Collections.Generic;

namespace Spotlight.Contracts
{
    public interface IFeedbackService
    {
        void Bind(IEventBus events, IWorldQuery world);
        void SetMasterVolume(float volume);
        void Unbind();
    }
}

