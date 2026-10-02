using System;
using System.Collections.Generic;
using Spotlight.Contracts;

namespace Spotlight.Core.Clock
{
    public sealed class GameClock : IGameClock
    {
        private readonly Func<FlowSnapshot> flow;
        private readonly IEventBus events;
        private readonly Dictionary<Guid, PauseReason> handles = new Dictionary<Guid, PauseReason>();
        private readonly Dictionary<Guid, KeyValuePair<GameSpeed, OperationResult>> commands = new Dictionary<Guid, KeyValuePair<GameSpeed, OperationResult>>();
        private GameSpeed speed = GameSpeed.Normal;
        private long index;
        private double time;
        public GameClock(Func<FlowSnapshot> flow, IEventBus events) { this.flow = flow; this.events = events; }
        public ClockSnapshot GetSnapshot()
        {
            List<PauseReason> reasons = new List<PauseReason>(handles.Values);
            reasons.Sort();
            return new ClockSnapshot { TickIndex = index, GameTime = time, Speed = speed, IsPaused = handles.Count > 0, Reasons = reasons.ToArray() };
        }
        public OperationResult TrySetSpeed(CommandContext context, GameSpeed requested)
        {
            FlowSnapshot state = flow();
            KeyValuePair<GameSpeed, OperationResult> previous;
            if (commands.TryGetValue(context.CommandId, out previous))
                return previous.Key == requested ? previous.Value : Result(context, ErrorCode.DuplicateCommand, state.Revision);
            ErrorCode error = ErrorCode.None;
            if (context.CommandId == Guid.Empty || (requested != GameSpeed.Normal && requested != GameSpeed.Double)) error = ErrorCode.InvalidArgument;
            else if (context.RunId != state.RunId) error = ErrorCode.WrongRun;
            else if (context.ExpectedRevision != state.Revision) error = ErrorCode.VersionConflict;
            OperationResult result = Result(context, error, state.Revision);
            if (error == ErrorCode.None) { speed = requested; commands[context.CommandId] = new KeyValuePair<GameSpeed, OperationResult>(requested, result); Notify(); }
            return result;
        }
        public Guid AcquirePause(PauseReason reason)
        {
            if (!Enum.IsDefined(typeof(PauseReason), reason)) throw new ArgumentOutOfRangeException("reason");
            Guid handle = Guid.NewGuid(); handles.Add(handle, reason); Notify(); return handle;
        }
        public void ReleasePause(Guid handle) { if (handles.Remove(handle)) Notify(); }
        internal TickContext Step(float delta, TickStage stage)
        { return new TickContext(index, time, delta, stage); }
        internal void AdvanceTick(float delta) { index++; time += delta; }
        public void Reset() { handles.Clear(); commands.Clear(); index = 0; time = 0; Notify(); }
        private void Notify() { events.Publish(new ClockChangedEvent { Snapshot = GetSnapshot() }); }
        private static OperationResult Result(CommandContext context, ErrorCode error, long revision)
        { return new OperationResult(error == ErrorCode.None ? OperationState.Committed : OperationState.Rejected, error, context.CommandId, revision, error == ErrorCode.None ? string.Empty : error.ToString()); }
    }
}

