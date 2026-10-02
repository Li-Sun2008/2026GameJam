using System;
using System.Collections.Generic;
using Spotlight.Contracts;
namespace Spotlight.Core.Data
{
    [Serializable]
    public sealed class WorldPointConfig
    {
        public float X;
        public float Y;
        public WorldPoint ToDefinition()
        {
            return new WorldPoint(X,Y);
        }
    }
}
