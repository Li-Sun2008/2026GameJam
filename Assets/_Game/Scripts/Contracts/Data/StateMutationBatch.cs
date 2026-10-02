// Generated verbatim from Docs/Contracts/Spotlight.Contracts.cs (2.1.0).
using System;
using System.Collections.Generic;

namespace Spotlight.Contracts
{
    public sealed class StateMutationBatch
    {
        public CommandContext Context;
        public string Reason;
        public IReadOnlyList<BoardMutation> Board;
        public IReadOnlyList<ResourceAmount> ResourceDeltas;
        public IReadOnlyList<ActorMutation> Actors;
        public IReadOnlyList<RandomStateWrite> Random;
        public ProgressPatch Progress;
        public DayEventState DayEvent;
    }
}

