using System;
using System.Collections.Generic;
using Spotlight.Contracts;
namespace Spotlight.Gameplay.Definitions
{
    [Serializable]
    public sealed class ElementAmountsConfig
    {
        public float Water;
        public float Fire;
        public float Earth;
        public float Wood;
        public float Wind;
        public float Thunder;
        public ElementAmounts ToDefinition()
        {
            return new ElementAmounts(Water,Fire,Earth,Wood,Wind,Thunder);
        }
    }
}
