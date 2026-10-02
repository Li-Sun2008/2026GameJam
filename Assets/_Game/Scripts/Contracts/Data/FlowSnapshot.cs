// Generated verbatim from Docs/Contracts/Spotlight.Contracts.cs (2.1.0).
using System;
using System.Collections.Generic;

namespace Spotlight.Contracts
{
    public sealed class FlowSnapshot
    {
        public string RunId;
        public GameMode Mode;
        public GamePhase Phase;
        public int DayIndex;
        public int LastSettledNight;
        public bool Milestone50Claimed;
        public bool TutorialShown;
        public long Revision;
        public bool CanStartNight;
    }
}

