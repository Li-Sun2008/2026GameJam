using System;
using System.Collections.Generic;
using Spotlight.Contracts;
using UnityEngine;
namespace Spotlight.Combat.Definitions
{
    [CreateAssetMenu(menuName="Spotlight/P3/ProjectileDefinition",fileName="ProjectileDefinition")]
    public sealed class ProjectileDefinitionSO : ScriptableObject
    {
        public string Id = "projectile.basic";
        public string PrefabKey = "ProjectileBasic";
        public float SpeedInCellsPerSecond = 6;
        public float LifetimeSeconds = 10;
        public float CollisionRadiusInCells = 0.1f;
        public ProjectileDefinition ToDefinition()
        {
            return new ProjectileDefinition { Id=Id,PrefabKey=PrefabKey,SpeedInCellsPerSecond=SpeedInCellsPerSecond,LifetimeSeconds=LifetimeSeconds,CollisionRadiusInCells=CollisionRadiusInCells };
        }
    }
}
