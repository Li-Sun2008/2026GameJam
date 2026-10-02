using System;
using System.Collections.Generic;
using Spotlight.Contracts;
namespace Spotlight.Core.Data
{
    [Serializable]
    public sealed class ResourceAmountConfig
    {
        public ResourceBucket Bucket;
        public string DefinitionId;
        public long Amount;
        public ResourceAmount ToDefinition()
        {
            return new ResourceAmount(Bucket,DefinitionId,Amount);
        }
    }
}
