using System;
using Spotlight.Contracts;
namespace Spotlight.Core
{
    // 存档私有扩展，不改变固定跨模块契约。
    [Serializable]
    public sealed class CommandHistoryRecord
    {
        public string CommandId;
        public string Fingerprint;
        public string DeploymentRequest;
        public OperationState State;
        public ErrorCode Error;
        public long Revision;
        public string MessageKey;
    }
}
