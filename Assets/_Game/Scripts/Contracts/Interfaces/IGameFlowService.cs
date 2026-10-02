// Generated verbatim from Docs/Contracts/Spotlight.Contracts.cs (2.1.0).
using System;
using System.Collections.Generic;

namespace Spotlight.Contracts
{
    public interface IGameFlowService
    {
        FlowSnapshot GetSnapshot();
        OperationResult TryNewRun(CommandContext context, NewRunRequest request);
        OperationResult TryLoadRun(CommandContext context, CheckpointData checkpoint);
        OperationResult TryStartNight(CommandContext context);
        OperationResult TryConfirmNightResult(CommandContext context);
        OperationResult TryContinueEndless(CommandContext context);
        OperationResult TryDismissTutorial(CommandContext context);
        OperationResult TryReturnToMenu(CommandContext context);
        void EvaluateNight(NightOutcome outcome);
    }
}

