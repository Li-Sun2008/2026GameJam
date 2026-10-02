using System;
using System.Collections.Generic;
using Spotlight.Contracts;
namespace Spotlight.Enemies.Definitions
{
    [Serializable]
    public sealed class DropEntryConfig
    {
        public string Id = "entry.1";
        public int Weight = 1;
        public ResourceBucket Bucket = ResourceBucket.Hand;
        public string ResourceId = "element.water";
        public int MinAmount = 1;
        public int MaxAmountInclusive = 1;
        public DropEntry ToDefinition()
        {
            return new DropEntry { Id=Id,Weight=Weight,Bucket=Bucket,ResourceId=ResourceId,MinAmount=MinAmount,MaxAmountInclusive=MaxAmountInclusive };
        }
    }
}
