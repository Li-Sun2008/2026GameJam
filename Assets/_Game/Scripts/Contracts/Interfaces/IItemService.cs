// Generated verbatim from Docs/Contracts/Spotlight.Contracts.cs (2.1.0).
using System;
using System.Collections.Generic;

namespace Spotlight.Contracts
{
    public interface IItemService
    {
        ValidationResult CheckUse(ItemUseRequest request);
        OperationResult TryUse(CommandContext context, ItemUseRequest request);
        IReadOnlyList<ItemGroupSnapshot> GetActiveGroups();
        void ResetTransient();
    }
}

