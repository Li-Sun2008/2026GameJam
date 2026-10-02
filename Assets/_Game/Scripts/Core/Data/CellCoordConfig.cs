using System;
using System.Collections.Generic;
using Spotlight.Contracts;
namespace Spotlight.Core.Data
{
    [Serializable]
    public sealed class CellCoordConfig
    {
        public int X;
        public int Y;
        public CellCoord ToDefinition()
        {
            return new CellCoord(X,Y);
        }
    }
}
