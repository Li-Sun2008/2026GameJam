// Generated verbatim from Docs/Contracts/Spotlight.Contracts.cs (2.1.0).
using System;
using System.Collections.Generic;

namespace Spotlight.Contracts
{
    public sealed class GameViewContext
    {
        public ICommandContextFactory Commands;
        public IGameFlowService Flow;
        public IGameClock Clock;
        public IBoardQuery Board;
        public IDeploymentService Deployment;
        public IResourceQuery Resources;
        public IWorldQuery World;
        public IStatusService Status;
        public ISpecialSkillService Skill;
        public IWaveService Waves;
        public IItemService Items;
        public IDayEventService DayEvents;
        public ISaveService Saves;
        public IEventBus Events;
        public IGameCatalog Catalog;
    }
}

