// Generated verbatim from Docs/Contracts/Spotlight.Contracts.cs (2.1.0).
using System;
using System.Collections.Generic;

namespace Spotlight.Contracts
{
    public struct CellCoord : IEquatable<CellCoord>
    {
        public readonly int X;
        public readonly int Y;
        public CellCoord(int x, int y) { X = x; Y = y; }
        public bool Equals(CellCoord other) { return X == other.X && Y == other.Y; }
        public override bool Equals(object obj) { return obj is CellCoord && Equals((CellCoord)obj); }
        public override int GetHashCode() { unchecked { return (X * 397) ^ Y; } }
    }
}

