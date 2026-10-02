// Generated verbatim from Docs/Contracts/Spotlight.Contracts.cs (2.1.0).
using System;
using System.Collections.Generic;

namespace Spotlight.Contracts
{
    public enum ErrorCode
    {
        None = 0, InvalidArgument = 1, WrongPhase = 2, WrongRun = 3,
        VersionConflict = 4, DuplicateCommand = 5, InvalidDefinition = 6,
        OutOfBounds = 10, ReservedCell = 11, Occupied = 12, EmptyCell = 13,
        InvalidPathPlacement = 14, InsufficientQuantity = 15, InsufficientCost = 16,
        LimitReached = 17, InvalidTarget = 20, TargetDead = 21,
        EffectAlreadyActive = 22, SkillAlreadyUsed = 23, EventUnresolved = 24,
        InvalidOption = 25, QueueFull = 26, SaveUnavailable = 30,
        SaveBusy = 31, SaveIoFailure = 32, SaveCorrupt = 33,
        SaveVersionUnsupported = 34, ConfigMismatch = 35, InternalFailure = 99
    }
}

