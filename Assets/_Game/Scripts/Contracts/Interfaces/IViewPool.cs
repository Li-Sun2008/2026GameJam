// Generated verbatim from Docs/Contracts/Spotlight.Contracts.cs (2.1.0).
using System;
using System.Collections.Generic;

namespace Spotlight.Contracts
{
    public interface IViewPool
    {
        PoolLease Rent(string prefabKey);
        OperationResult Return(long leaseId);
        void Clear();
    }
}

