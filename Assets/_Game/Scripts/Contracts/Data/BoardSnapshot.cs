// Generated verbatim from Docs/Contracts/Spotlight.Contracts.cs (2.1.0).
using System;
using System.Collections.Generic;

namespace Spotlight.Contracts
{
    public sealed class BoardSnapshot
    {
        public long Revision;
        public string LevelId;
        public int Rows;
        public int Columns;
        public IReadOnlyList<CellSnapshot> Cells;
    }
}

