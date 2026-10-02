using System;
using Spotlight.Contracts;
namespace Spotlight.Enemies {
public sealed class EndlessWaveProvider {
 public WaveDefinition[] Resolve(int day,EndlessDefinition definition,Func<string,WaveDefinition> lookup) {
  if(day<1 || definition==null || definition.WaveTemplateIds==null || definition.WaveTemplateIds.Count==0) return new WaveDefinition[0];
  var source=lookup(definition.WaveTemplateIds[(day-1)%definition.WaveTemplateIds.Count]); if(source==null) return new WaveDefinition[0];
  var groups=new SpawnGroupDefinition[source.Groups.Count];
  for(int i=0;i<groups.Length;i++){var g=source.Groups[i]; groups[i]=new SpawnGroupDefinition{Id=g.Id,EnemyId=g.EnemyId,Count=g.Count,SpawnPointId=g.SpawnPointId,PathId=g.PathId,StartDelaySeconds=g.StartDelaySeconds,SpawnIntervalSeconds=g.SpawnIntervalSeconds,ApplyDayGrowth=false,HpMultiplier=g.HpMultiplier*(1+definition.HpGrowthPerNight*(day-1)),DamageMultiplier=g.DamageMultiplier*(1+definition.DamageGrowthPerNight*(day-1))};}
  return new[]{new WaveDefinition{Id=source.Id,DayIndex=day,WaveIndex=source.WaveIndex,DelayAfterPreviousWaveSeconds=source.DelayAfterPreviousWaveSeconds,Groups=groups}};
 }
}
}
