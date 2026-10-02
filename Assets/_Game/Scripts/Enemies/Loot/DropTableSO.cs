using System;
using System.Collections.Generic;
using Spotlight.Contracts;
using UnityEngine;
namespace Spotlight.Enemies.Definitions
{
    [CreateAssetMenu(menuName="Spotlight/P4/DropTable",fileName="DropTable")]
    public sealed class DropTableSO : ScriptableObject
    {
        public string Id = "drop.empty";
        public int Rolls = 0;
        public DropEntryConfig[] Entries = new DropEntryConfig[0];
        public DropTableDefinition ToDefinition()
        {
            List<DropEntry> entriesValues=new List<DropEntry>();
            if(Entries!=null)foreach(DropEntryConfig value in Entries)if(value!=null)entriesValues.Add(value.ToDefinition());
            return new DropTableDefinition { Id=Id,Rolls=Rolls,Entries=entriesValues.AsReadOnly() };
        }
    }
}
