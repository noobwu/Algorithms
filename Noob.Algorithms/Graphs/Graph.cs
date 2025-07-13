using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Noob.Algorithms.Graphs
{
    /// <summary>
    /// 图节点，支持任意业务属性扩展
    /// </summary>
    public class GraphNode
    {
        /// <summary>节点唯一ID</summary>
        public int Id { get; set; }

        /// <summary>邻接边集合</summary>
        public List<GraphEdge> Neighbors { get; } = new List<GraphEdge>();
    }

    /// <summary>
    /// 图边，支持权重（距离/耗时/费用等）
    /// </summary>
    public class GraphEdge
    {
        /// <summary>目标节点ID</summary>
        public int TargetNodeId { get; set; }

        /// <summary>边权重（必须非负）</summary>
        public double Weight { get; set; }
    }


    /// <summary>
    /// 支持复杂业务属性的节点（如坐标、类型、动态属性）
    /// </summary>
    public class AttributeNode : GraphNode
    {
        /// <summary>节点类型，如“加油站”、“路口”</summary>
        public string Category { get; set; }
        /// <summary>地理坐标（可扩展为三维）</summary>
        public double Longitude { get; set; }
        public double Latitude { get; set; }
        // 更多业务属性可扩展
    }

    /// <summary>
    /// 支持动态权重/属性的边
    /// </summary>
    public class AttributeEdge : GraphEdge
    {
        /// <summary>实时权重调整（如拥堵/封路）</summary>
        public bool IsOpen { get; set; } = true;

        /// <summary>动态权重因子（如拥堵、施工系数）</summary>
        public double Factor { get; set; } = 1.0;
        // 可扩展更多属性
    }
}
