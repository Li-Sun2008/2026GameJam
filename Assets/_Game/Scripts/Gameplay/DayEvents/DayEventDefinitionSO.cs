using System;
using System.Collections.Generic;
using Spotlight.Contracts;
using UnityEngine;
namespace Spotlight.Gameplay.Definitions
{
    [CreateAssetMenu(menuName="Spotlight/P5/DayEventDefinition",fileName="DayEventDefinition")]
    public sealed class DayEventDefinitionSO : ScriptableObject
    {
        public string Id = "event.unconfigured";
        public int MinDayInclusive = 1;
        public int MaxDayInclusive = 7;
        public GameMode[] Modes = new GameMode[] { GameMode.Story,GameMode.Endless };
        public int Weight = 1;
        public string Text;
        public EventOptionConfig[] Options = new EventOptionConfig[] { new EventOptionConfig() };
        public DayEventDefinition ToDefinition()
        {
            List<GameMode> modesValues=new List<GameMode>();
            if(Modes!=null)modesValues.AddRange(Modes);
            List<EventOptionDefinition> optionsValues=new List<EventOptionDefinition>();
            if(Options!=null)foreach(EventOptionConfig value in Options)if(value!=null)optionsValues.Add(value.ToDefinition());
            return new DayEventDefinition { Id=Id,MinDayInclusive=MinDayInclusive,MaxDayInclusive=MaxDayInclusive,Modes=modesValues.AsReadOnly(),Weight=Weight,Text=Text,Options=optionsValues.AsReadOnly() };
        }
    }
}
