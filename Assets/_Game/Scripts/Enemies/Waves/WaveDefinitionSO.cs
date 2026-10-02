using System;
using System.Collections.Generic;
using Spotlight.Contracts;
using UnityEngine;
namespace Spotlight.Enemies.Definitions
{
    [CreateAssetMenu(menuName="Spotlight/P4/WaveDefinition",fileName="WaveDefinition")]
    public sealed class WaveDefinitionSO : ScriptableObject
    {
        public string Id = "wave.day1";
        public int DayIndex = 1;
        public int WaveIndex = 1;
        public float DelayAfterPreviousWaveSeconds;
        public SpawnGroupConfig[] Groups = new SpawnGroupConfig[0];
        public WaveDefinition ToDefinition()
        {
            List<SpawnGroupDefinition> groupsValues=new List<SpawnGroupDefinition>();
            if(Groups!=null)foreach(SpawnGroupConfig value in Groups)if(value!=null)groupsValues.Add(value.ToDefinition());
            return new WaveDefinition { Id=Id,DayIndex=DayIndex,WaveIndex=WaveIndex,DelayAfterPreviousWaveSeconds=DelayAfterPreviousWaveSeconds,Groups=groupsValues.AsReadOnly() };
        }
    }
}
