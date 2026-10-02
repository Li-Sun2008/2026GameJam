// Generated verbatim from Docs/Contracts/Spotlight.Contracts.cs (2.1.0).
using System;
using System.Collections.Generic;

namespace Spotlight.Contracts
{
    public sealed class CheckpointData
    {
        public int SaveVersion;
        public string ContractVersion;
        public string ConfigVersion;
        public string RunId;
        public string LevelId;
        public GameMode Mode;
        public int DayIndex;
        public int LastSettledNight;
        public long Revision;
        public uint RunSeed;
        public IReadOnlyList<RandomState> RandomStates;
        public long NextEntityId;
        public long NextSpawnSequence;
        public EntityId SpringId;
        public float SpringHp;
        public float SpringMaxHp;
        public IReadOnlyList<SavedOccupant> Occupants;
        public IReadOnlyList<ResourceAmount> Resources;
        public DayEventState DayEvent;
        public bool TutorialShown;
        public bool Milestone50Claimed;
        public IReadOnlyList<Guid> CommittedCommandIds;
    }
}

