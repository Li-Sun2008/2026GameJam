using System;
using System.Collections.Generic;
using Spotlight.Contracts;
namespace Spotlight.Core.Save {
[Serializable] public sealed class CheckpointFileModel {
 public int SaveVersion;public string ContractVersion,ConfigVersion,RunId,LevelId;public int Mode,DayIndex,LastSettledNight;public long Revision;public uint RunSeed;public RandomEntry[] RandomStates;public long NextEntityId,NextSpawnSequence,SpringId;public float SpringHp,SpringMaxHp;public OccupantEntry[] Occupants;public ResourceEntry[] Resources;public EventEntry DayEvent;public bool HasDayEvent;public bool TutorialShown,Milestone50Claimed;public string[] CommittedCommandIds; public CommandHistoryRecord[] CommandHistory;
 [Serializable]public sealed class RandomEntry{public int Stream;public uint State;}
 [Serializable]public sealed class OccupantEntry{public long Id;public string DefinitionId;public int Kind,X,Y;public float CurrentHp,MaxHp;public int AttackStacks;}
 [Serializable]public sealed class ResourceEntry{public int Bucket;public string DefinitionId;public long Amount;}
 [Serializable]public sealed class EventEntry{public int DayIndex;public string InstanceId,DefinitionId,ChosenOptionId;public bool Resolved;}
 public static CheckpointFileModel FromCheckpoint(CheckpointData d){
  var m=new CheckpointFileModel{SaveVersion=d.SaveVersion,ContractVersion=d.ContractVersion,ConfigVersion=d.ConfigVersion,RunId=d.RunId,LevelId=d.LevelId,Mode=(int)d.Mode,DayIndex=d.DayIndex,LastSettledNight=d.LastSettledNight,Revision=d.Revision,RunSeed=d.RunSeed,NextEntityId=d.NextEntityId,NextSpawnSequence=d.NextSpawnSequence,SpringId=d.SpringId.Value,SpringHp=d.SpringHp,SpringMaxHp=d.SpringMaxHp,TutorialShown=d.TutorialShown,Milestone50Claimed=d.Milestone50Claimed};
  var random=new List<RandomEntry>();if(d.RandomStates!=null)foreach(var r in d.RandomStates)random.Add(new RandomEntry{Stream=(int)r.Stream,State=r.State});m.RandomStates=random.ToArray();
  var occupants=new List<OccupantEntry>();if(d.Occupants!=null)foreach(var o in d.Occupants)occupants.Add(new OccupantEntry{Id=o.Id.Value,DefinitionId=o.DefinitionId,Kind=(int)o.Kind,X=o.Cell.X,Y=o.Cell.Y,CurrentHp=o.CurrentHp,MaxHp=o.MaxHp,AttackStacks=o.AttackStacks});m.Occupants=occupants.ToArray();
  var resources=new List<ResourceEntry>();if(d.Resources!=null)foreach(var r in d.Resources)resources.Add(new ResourceEntry{Bucket=(int)r.Bucket,DefinitionId=r.DefinitionId,Amount=r.Amount});m.Resources=resources.ToArray();
  m.HasDayEvent=d.DayEvent!=null;if(d.DayEvent!=null)m.DayEvent=new EventEntry{DayIndex=d.DayEvent.DayIndex,InstanceId=d.DayEvent.InstanceId,DefinitionId=d.DayEvent.DefinitionId,ChosenOptionId=d.DayEvent.ChosenOptionId,Resolved=d.DayEvent.Resolved};
  var ids=new List<string>();if(d.CommittedCommandIds!=null)foreach(var id in d.CommittedCommandIds)ids.Add(id.ToString("D"));m.CommittedCommandIds=ids.ToArray();return m;
 }
 public CheckpointData ToCheckpoint(){
  var d=new CheckpointData{SaveVersion=SaveVersion,ContractVersion=ContractVersion,ConfigVersion=ConfigVersion,RunId=RunId,LevelId=LevelId,Mode=(GameMode)Mode,DayIndex=DayIndex,LastSettledNight=LastSettledNight,Revision=Revision,RunSeed=RunSeed,NextEntityId=NextEntityId,NextSpawnSequence=NextSpawnSequence,SpringId=new EntityId(SpringId),SpringHp=SpringHp,SpringMaxHp=SpringMaxHp,TutorialShown=TutorialShown,Milestone50Claimed=Milestone50Claimed};
  if(RandomStates==null||Occupants==null||Resources==null||CommittedCommandIds==null)throw new FormatException("save.collections.missing");
  var random=new List<RandomState>();foreach(var r in RandomStates){if(r==null)throw new FormatException("save.random.null");random.Add(new RandomState((RandomStream)r.Stream,r.State));}d.RandomStates=random.ToArray();
  var occupants=new List<SavedOccupant>();foreach(var o in Occupants){if(o==null)throw new FormatException("save.occupant.null");occupants.Add(new SavedOccupant{Id=new EntityId(o.Id),DefinitionId=o.DefinitionId,Kind=(OccupantKind)o.Kind,Cell=new CellCoord(o.X,o.Y),CurrentHp=o.CurrentHp,MaxHp=o.MaxHp,AttackStacks=o.AttackStacks});}d.Occupants=occupants.ToArray();
  var resources=new List<ResourceAmount>();foreach(var r in Resources){if(r==null)throw new FormatException("save.resource.null");resources.Add(new ResourceAmount((ResourceBucket)r.Bucket,r.DefinitionId,r.Amount));}d.Resources=resources.ToArray();
  if(HasDayEvent&&DayEvent==null)throw new FormatException("save.event.missing");if(HasDayEvent||(DayEvent!=null&&DayEvent.DayIndex>0))d.DayEvent=new DayEventState{DayIndex=DayEvent.DayIndex,InstanceId=DayEvent.InstanceId,DefinitionId=DayEvent.DefinitionId,ChosenOptionId=DayEvent.ChosenOptionId,Resolved=DayEvent.Resolved};
  var ids=new List<Guid>();var unique=new HashSet<Guid>();foreach(var id in CommittedCommandIds){Guid parsed;if(!Guid.TryParse(id,out parsed)||parsed==Guid.Empty||!unique.Add(parsed))throw new FormatException("save.command.invalid");ids.Add(parsed);}d.CommittedCommandIds=ids.ToArray();return d;
 }
}
}


