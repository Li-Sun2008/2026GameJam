using System;
using System.Collections.Generic;
using Spotlight.Contracts;
using UnityEngine;
namespace Spotlight.Core.Data
{
    [CreateAssetMenu(menuName="Spotlight/P1/ResourceDefinition",fileName="ResourceDefinition")]
    public sealed class ResourceDefinitionSO : ScriptableObject
    {
        public string Id = "res.gold";
        public string DisplayNameKey = "金币";
        public string IconKey = "ResourceGold";
        public ResourceDefinition ToDefinition()
        {
            return new ResourceDefinition { Id=Id,DisplayNameKey=DisplayNameKey,IconKey=IconKey };
        }
    }
}
