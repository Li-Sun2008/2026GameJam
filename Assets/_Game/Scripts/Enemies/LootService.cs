using System;
using System.Collections.Generic;
using Spotlight.Contracts;
namespace Spotlight.Enemies {
public sealed class LootService : ILootService {
 readonly IGameCatalog catalog; readonly IRandomService random;
 public LootService(IGameCatalog catalog, IRandomService random) { this.catalog=catalog; this.random=random; }
 public LootResult Resolve(LootInput input) {
  DropTableDefinition table; var rewards=new List<ResourceAmount>(); var cursor=random.CreateCursor(input.Random);
  if (!string.IsNullOrEmpty(input.DropTableId) && catalog.TryGetDropTable(input.DropTableId,out table)) {
   var entries=new List<DropEntry>(); if(table.Entries!=null) foreach(var e in table.Entries) if(e.Weight>0 && e.MinAmount>=0 && e.MaxAmountInclusive>=e.MinAmount) entries.Add(e);
   entries.Sort(delegate(DropEntry a, DropEntry b){return string.CompareOrdinal(a.Id,b.Id);});
   long total=0; foreach(var e in entries) total+=e.Weight;
   for(int i=0;i<table.Rolls && total>0;i++) {
    long pick=Draw(cursor,total); DropEntry selected=null; foreach(var e in entries) { if(pick<e.Weight){selected=e;break;} pick-=e.Weight; }
    if(selected==null) continue; long amount=selected.MinAmount+Draw(cursor,(long)selected.MaxAmountInclusive-selected.MinAmount+1);
    ElementDefinition element; ResourceBucket bucket=catalog.TryGetElement(selected.ResourceId,out element)?ResourceBucket.Hand:ResourceBucket.Inventory;
    if(amount>0) rewards.Add(new ResourceAmount(bucket,selected.ResourceId,amount));
   }
  }
  return new LootResult { EnemyId=input.EnemyId,Rewards=rewards.ToArray(),NextRandom=cursor.Capture() };
 }
 internal static long Draw(IRandomCursor cursor,long bound) {
  if(bound<=int.MaxValue) return cursor.NextInt(0,(int)bound);
  ulong size=1UL<<62, limit=size-size%(ulong)bound; ulong value;
  do { value=((ulong)cursor.NextInt(0,1<<30)<<32)|((ulong)cursor.NextInt(0,1<<30)<<2)|(uint)cursor.NextInt(0,4); } while(value>=limit);
  return (long)(value%(ulong)bound);
 }
}
}
