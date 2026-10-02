using System;
using System.Collections.Generic;
using Spotlight.Contracts;
using UnityEngine;
namespace Spotlight.Combat.Definitions
{
    [CreateAssetMenu(menuName="Spotlight/P3/GaugeConfig",fileName="GaugeConfig")]
    public sealed class GaugeConfigSO : ScriptableObject
    {
        public ElementType Element = ElementType.Water;
        public float MaxValue = 100;
        public float Threshold = 100;
        public float DecayPerSecond = 5;
        public float DecayDelaySeconds = 1;
        public float IncomingMultiplier = 1;
        public GaugeDefinition ToDefinition()
        {
            return new GaugeDefinition { Element=Element,MaxValue=MaxValue,Threshold=Threshold,DecayPerSecond=DecayPerSecond,DecayDelaySeconds=DecayDelaySeconds,IncomingMultiplier=IncomingMultiplier };
        }
    }
}
