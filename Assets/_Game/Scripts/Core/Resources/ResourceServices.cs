using System;
using System.Collections.Generic;
using Spotlight.Contracts;
namespace Spotlight.Core
{
    public sealed class ResourceQuery : IResourceQuery
    {
        readonly SessionStateStore store;public ResourceQuery(SessionStateStore store){this.store=store;}public long GetQuantity(ResourceBucket bucket,string id){return store.GetQuantity(bucket,id);}
        public IReadOnlyList<ResourceAmount> GetSnapshot(ResourceBucket bucket){List<ResourceAmount> result=new List<ResourceAmount>();string prefix=((int)bucket).ToString()+":";foreach(var p in store.Quantities)if(p.Key.StartsWith(prefix,StringComparison.Ordinal))result.Add(new ResourceAmount(bucket,p.Key.Substring(prefix.Length),p.Value));result.Sort(delegate(ResourceAmount a,ResourceAmount b){return string.CompareOrdinal(a.DefinitionId,b.DefinitionId);});return result.ToArray();}
    }
    public sealed class EntityIdService : IEntityIdService
    {
        readonly SessionStateStore store;public EntityIdService(SessionStateStore store){this.store=store;}public EntityId Allocate(){long id=store.NextEntityId;store.NextEntityId=checked(id+1);return new EntityId(id);}public long AllocateSpawnSequence(){long id=store.NextSpawnSequence;store.NextSpawnSequence=checked(id+1);return id;}
    }
    public sealed class CommandContextFactory : ICommandContextFactory
    {
        readonly SessionStateStore store;public CommandContextFactory(SessionStateStore store){this.store=store;}public CommandContext Create(long expectedRevision){return new CommandContext(Guid.NewGuid(),store.Progress.RunId,expectedRevision);}
    }
}
