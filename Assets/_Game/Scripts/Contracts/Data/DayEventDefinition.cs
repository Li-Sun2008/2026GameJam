// Generated verbatim from Docs/Contracts/Spotlight.Contracts.cs (2.1.0).
using System;
using System.Collections.Generic;

namespace Spotlight.Contracts
{
    public sealed class DayEventDefinition
    {
        public string Id;
        public int MinDayInclusive;
        public int MaxDayInclusive;
        public IReadOnlyList<GameMode> Modes;
        public int Weight;
        public string Text;
        public IReadOnlyList<EventOptionDefinition> Options;
    }
}

