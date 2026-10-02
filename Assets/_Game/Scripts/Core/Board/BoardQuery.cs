using System;
using System.Collections.Generic;
using Spotlight.Contracts;
namespace Spotlight.Core
{
    public sealed class BoardQuery : IBoardQuery
    {
        readonly SessionStateStore store;public BoardQuery(SessionStateStore store){this.store=store;}
        public BoardSnapshot GetSnapshot(){List<CellSnapshot> cells=new List<CellSnapshot>();foreach(CellSnapshot c in store.Cells.Values)cells.Add(SessionStateStore.Copy(c));cells.Sort(delegate(CellSnapshot a,CellSnapshot b){int c=a.Cell.Y.CompareTo(b.Cell.Y);return c!=0?c:a.Cell.X.CompareTo(b.Cell.X);});return new BoardSnapshot { Revision=store.Progress.Revision,LevelId=store.ActiveLevel==null?null:store.ActiveLevel.Id,Rows=store.ActiveLevel==null?0:store.ActiveLevel.Rows,Columns=store.ActiveLevel==null?0:store.ActiveLevel.Columns,Cells=cells.ToArray() };}
        public bool TryGetCell(CellCoord cell,out CellSnapshot snapshot){CellSnapshot c;bool found=store.Cells.TryGetValue(cell,out c);snapshot=found?SessionStateStore.Copy(c):null;return found;}
        public IReadOnlyList<CellCoord> GetNeighbors4(CellCoord cell){List<CellCoord> list=new List<CellCoord>();CellCoord[] candidates={new CellCoord(cell.X,cell.Y+1),new CellCoord(cell.X+1,cell.Y),new CellCoord(cell.X,cell.Y-1),new CellCoord(cell.X-1,cell.Y)};foreach(CellCoord c in candidates)if(store.Cells.ContainsKey(c))list.Add(c);return list.ToArray();}
        public WorldPoint CellToWorld(CellCoord cell){if(store.ActiveLevel==null)throw new InvalidOperationException("No active level");return store.Center(cell);}
        public bool TryWorldToCell(WorldPoint point,out CellCoord cell){cell=new CellCoord(0,0);if(store.ActiveLevel==null || float.IsNaN(point.X) || float.IsNaN(point.Y) || float.IsInfinity(point.X) || float.IsInfinity(point.Y))return false;double x=Math.Floor((point.X-store.ActiveLevel.Origin.X)/store.ActiveLevel.CellSize),y=Math.Floor((point.Y-store.ActiveLevel.Origin.Y)/store.ActiveLevel.CellSize);if(x<int.MinValue || x>int.MaxValue || y<int.MinValue || y>int.MaxValue)return false;cell=new CellCoord((int)x,(int)y);return store.Cells.ContainsKey(cell);}
        sealed class Hit { public double T;public CellSnapshot Cell; }
        internal static bool Clip(double start,double delta,double low,double high,ref double enter,ref double exit){if(delta==0)return start>=low && start<high;double a=(low-start)/delta,b=(high-start)/delta;if(a>b){double c=a;a=b;b=c;}enter=Math.Max(enter,a);exit=Math.Min(exit,b);return exit>enter;}
        public IReadOnlyList<CellSnapshot> TraceSegment(WorldPoint from,WorldPoint to){List<Hit> hits=new List<Hit>();if(store.ActiveLevel==null || (from.X==to.X && from.Y==to.Y))return new CellSnapshot[0];double dx=to.X-from.X,dy=to.Y-from.Y;if(double.IsNaN(dx) || double.IsNaN(dy) || double.IsInfinity(dx) || double.IsInfinity(dy))return new CellSnapshot[0];foreach(CellSnapshot c in store.Cells.Values){double x=store.ActiveLevel.Origin.X+c.Cell.X*store.ActiveLevel.CellSize,y=store.ActiveLevel.Origin.Y+c.Cell.Y*store.ActiveLevel.CellSize,enter=0,exit=1;if(Clip(from.X,dx,x,x+store.ActiveLevel.CellSize,ref enter,ref exit) && Clip(from.Y,dy,y,y+store.ActiveLevel.CellSize,ref enter,ref exit) && exit-enter>1e-12)hits.Add(new Hit { T=enter,Cell=c });}hits.Sort(delegate(Hit a,Hit b){int c=a.T.CompareTo(b.T);if(c!=0)return c;c=a.Cell.Cell.Y.CompareTo(b.Cell.Cell.Y);return c!=0?c:a.Cell.Cell.X.CompareTo(b.Cell.Cell.X);});List<CellSnapshot> result=new List<CellSnapshot>();foreach(Hit h in hits)result.Add(SessionStateStore.Copy(h.Cell));return result.ToArray();}
    }
}

