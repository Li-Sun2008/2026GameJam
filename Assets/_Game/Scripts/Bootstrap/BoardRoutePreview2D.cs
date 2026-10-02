using UnityEngine;
using Spotlight.Contracts;
using Spotlight.Core.Data;
namespace Spotlight.Bootstrap
{
    /// <summary>仅编辑器可视预览；不生成实体、不读写游戏运行状态。</summary>
    public sealed class BoardRoutePreview2D : MonoBehaviour
    {
        public LevelConfigSO Level;
        private void OnDrawGizmos()
        {
            if(Level==null||Level.Rows<=0||Level.Columns<=0||Level.CellSize<=0)return;
            LevelDefinition level=Level.ToDefinition();float size=level.CellSize;
            Gizmos.color=new Color(.45f,.55f,.7f,.55f);
            for(int x=0;x<=level.Columns;x++)Gizmos.DrawLine(new Vector3(level.Origin.X+x*size,level.Origin.Y,0),new Vector3(level.Origin.X+x*size,level.Origin.Y+level.Rows*size,0));
            for(int y=0;y<=level.Rows;y++)Gizmos.DrawLine(new Vector3(level.Origin.X,level.Origin.Y+y*size,0),new Vector3(level.Origin.X+level.Columns*size,level.Origin.Y+y*size,0));
            Gizmos.color=new Color(1,.7f,.2f);foreach(PathDefinition path in level.Paths)for(int i=1;i<path.Cells.Count;i++)Gizmos.DrawLine(Center(level,path.Cells[i-1]),Center(level,path.Cells[i]));
            Gizmos.color=new Color(1,.3f,.2f);foreach(SpawnPointDefinition spawn in level.SpawnPoints)Mark(Center(level,spawn.Cell),size*.28f);
            Gizmos.color=new Color(.2f,.9f,1);Mark(Center(level,level.SpringCell),size*.35f);
        }
        private static Vector3 Center(LevelDefinition level,CellCoord cell){return new Vector3(level.Origin.X+(cell.X+.5f)*level.CellSize,level.Origin.Y+(cell.Y+.5f)*level.CellSize,0);}
        private static void Mark(Vector3 center,float radius)
        {
            // 在XY平面用线框菱形标记，避免球体或碰撞体带来3D语义。
            Vector3 top=center+Vector3.up*radius,right=center+Vector3.right*radius,bottom=center-Vector3.up*radius,left=center-Vector3.right*radius;Gizmos.DrawLine(top,right);Gizmos.DrawLine(right,bottom);Gizmos.DrawLine(bottom,left);Gizmos.DrawLine(left,top);
        }
    }
}
