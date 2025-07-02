// ***********************************************************************
// Assembly         : Noob.DataStructures
// Author           : noob
// Created          : 2025-07-02
//
// Last Modified By : noob
// Last Modified On : 2025-07-02
// ***********************************************************************
// <copyright file="EdgeListGraphTests.cs" company="Noob.DataStructures">
//     Copyright (c) . All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Noob.DataStructures
{

    /// <summary>
    /// 表示图的一条边（支持有向/无向、权重、多属性扩展）
    /// </summary>
    public class Edge<TNode>
    {
        /// <summary>
        /// 起点节点
        /// </summary>
        public TNode From { get; set; }
        /// <summary>
        /// 终点节点
        /// </summary>
        public TNode To { get; set; }
        /// <summary>
        /// 权重（可选，默认1.0）
        /// </summary>
        public double Weight { get; set; }
        /// <summary>
        /// 边创建时间（可选）
        /// </summary>
        public DateTime? CreatedAt { get; set; }
        /// <summary>
        /// 标签或业务属性（可选）
        /// </summary>
        public string Label { get; set; }
    }

    /// <summary>
    /// 边缘列表存储图结构，支持有向/无向图，动态增删边
    /// </summary>
    /// <typeparam name="TNode">节点类型</typeparam>
    public class EdgeListGraph<TNode>
    {
        /// <summary>
        /// 边列表主结构
        /// </summary>
        private readonly List<Edge<TNode>> _edges;

        /// <summary>
        /// 是否为有向图
        /// </summary>
        public bool IsDirected { get; }

        /// <summary>
        /// 构造函数，指定是否为有向图
        /// </summary>
        /// <param name="isDirected">是否有向</param>
        public EdgeListGraph(bool isDirected = false)
        {
            IsDirected = isDirected;
            _edges = new List<Edge<TNode>>();
        }

        /// <summary>
        /// 添加一条边（支持权重与多属性）
        /// </summary>
        /// <param name="from">起点</param>
        /// <param name="to">终点</param>
        /// <param name="weight">权重（可选）</param>
        /// <param name="label">标签（可选）</param>
        /// <param name="createdAt">创建时间（可选）</param>
        public void AddEdge(TNode from, TNode to, double weight = 1.0, string label = null, DateTime? createdAt = null)
        {
            _edges.Add(new Edge<TNode>
            {
                From = from,
                To = to,
                Weight = weight,
                Label = label,
                CreatedAt = createdAt
            });
            // 无向图自动加反向边
            if (!IsDirected)
            {
                _edges.Add(new Edge<TNode>
                {
                    From = to,
                    To = from,
                    Weight = weight,
                    Label = label,
                    CreatedAt = createdAt
                });
            }
        }

        /// <summary>
        /// 删除一条边（若多条，删除全部）
        /// </summary>
        /// <param name="from">起点</param>
        /// <param name="to">终点</param>
        public void RemoveEdge(TNode from, TNode to)
        {
            _edges.RemoveAll(e => EqualityComparer<TNode>.Default.Equals(e.From, from)
                               && EqualityComparer<TNode>.Default.Equals(e.To, to));
            if (!IsDirected)
            {
                _edges.RemoveAll(e => EqualityComparer<TNode>.Default.Equals(e.From, to)
                                   && EqualityComparer<TNode>.Default.Equals(e.To, from));
            }
        }

        /// <summary>
        /// 查询是否存在指定边
        /// </summary>
        /// <param name="from">起点</param>
        /// <param name="to">终点</param>
        /// <returns>是否存在</returns>
        public bool HasEdge(TNode from, TNode to)
        {
            return _edges.Exists(e => EqualityComparer<TNode>.Default.Equals(e.From, from)
                                   && EqualityComparer<TNode>.Default.Equals(e.To, to));
        }

        /// <summary>
        /// 获取所有边（只读集合）
        /// </summary>
        public IReadOnlyList<Edge<TNode>> Edges => _edges.AsReadOnly();

        /// <summary>
        /// 获取某节点的所有邻居（只返回去重后的邻居）
        /// </summary>
        /// <param name="node">节点</param>
        /// <returns>邻居节点集合</returns>
        public IEnumerable<TNode> GetNeighbors(TNode node)
        {
            var set = new HashSet<TNode>();
            foreach (var e in _edges)
                if (EqualityComparer<TNode>.Default.Equals(e.From, node))
                    set.Add(e.To);
            return set;
        }

        /// <summary>
        /// 获取所有节点（只读集合）
        /// </summary>
        public IEnumerable<TNode> Nodes
        {
            get
            {
                var set = new HashSet<TNode>();
                foreach (var e in _edges)
                {
                    set.Add(e.From);
                    set.Add(e.To);
                }
                return set;
            }
        }
    }

    /// <summary>
    /// 表示并查集结构（Disjoint Set Union, DSU/Union-Find）
    /// 用于Kruskal MST算法高效判断环路与合并集合
    /// </summary>
    /// <typeparam name="TNode">节点类型</typeparam>
    public class DisjointSet<TNode>
    {
        /// <summary>
        /// The parent
        /// </summary>
        private readonly Dictionary<TNode, TNode> _parent = new();

        /// <summary>
        /// The rank
        /// </summary>
        private readonly Dictionary<TNode, int> _rank = new();

        /// <summary>
        /// 新增一个集合节点
        /// </summary>
        public void MakeSet(TNode node)
        {
            if (!_parent.ContainsKey(node))
            {
                _parent[node] = node;
                _rank[node] = 0;
            }
        }

        /// <summary>
        /// 查找节点的集合代表（带路径压缩）
        /// </summary>
        public TNode Find(TNode node)
        {
            if (!EqualityComparer<TNode>.Default.Equals(_parent[node], node))
                _parent[node] = Find(_parent[node]);
            return _parent[node];
        }

        /// <summary>
        /// 合并两个集合（按秩合并）
        /// </summary>
        public bool Union(TNode a, TNode b)
        {
            var pa = Find(a);
            var pb = Find(b);
            if (EqualityComparer<TNode>.Default.Equals(pa, pb))
                return false; // 已在同集合
            if (_rank[pa] < _rank[pb])
                _parent[pa] = pb;
            else
            {
                _parent[pb] = pa;
                if (_rank[pa] == _rank[pb])
                    _rank[pa]++;
            }
            return true;
        }
    }

    /// <summary>
    /// 平台工程级 Kruskal 最小生成树算法（适用于无向带权图的边缘列表实现）
    /// </summary>
    /// <typeparam name="TNode">节点类型</typeparam>
    public static class KruskalMstSolver
    {
        /// <summary>
        /// 计算最小生成树
        /// </summary>
        /// <param name="nodes">所有节点集合</param>
        /// <param name="edges">边列表（每条边需有权重）</param>
        /// <returns>MST的边列表（无向图，边数=n-1，权重和最小）</returns>
        public static List<Edge<TNode>> ComputeMinimumSpanningTree<TNode>(IEnumerable<TNode> nodes, IEnumerable<Edge<TNode>> edges)
        {
            var mst = new List<Edge<TNode>>();
            var dsu = new DisjointSet<TNode>();
            foreach (var node in nodes)
                dsu.MakeSet(node);

            // 按权重升序排序边
            var sortedEdges = edges.OrderBy(e => e.Weight).ToList();

            foreach (var edge in sortedEdges)
            {
                // Kruskal核心：若不在同集合则合并
                if (dsu.Union(edge.From, edge.To))
                    mst.Add(edge);
                // 优化：若已足够n-1条边则可提前退出
                if (mst.Count == nodes.Count() - 1)
                    break;
            }
            return mst;
        }
    }


    /// <summary>
    /// EdgeListGraph平台工程级功能单元测试
    /// </summary>
    [TestFixture]
    public class EdgeListGraphTests
    {
        /// <summary>
        /// 测试无向图添加、查询、邻居
        /// </summary>
        [Test]
        public void AddEdge_UndirectedGraph_WorksAndSymmetric()
        {
            var graph = new EdgeListGraph<int>(isDirected: false);
            graph.AddEdge(1, 2, weight: 3.5, label: "friend");
            Assert.That(graph.HasEdge(1, 2), Is.True);
            Assert.That(graph.HasEdge(2, 1), Is.True);
            var neighbors1 = graph.GetNeighbors(1).ToList();
            Assert.That(neighbors1, Contains.Item(2));
            var neighbors2 = graph.GetNeighbors(2).ToList();
            Assert.That(neighbors2, Contains.Item(1));
            // 查询边属性
            var edge = graph.Edges.FirstOrDefault(e => e.From == 1 && e.To == 2);
            Assert.That(edge, Is.Not.Null);
            Assert.That(edge.Weight, Is.EqualTo(3.5));
            Assert.That(edge.Label, Is.EqualTo("friend"));
        }

        /// <summary>
        /// 测试有向图添加与查找（单向）
        /// </summary>
        [Test]
        public void AddEdge_DirectedGraph_Works()
        {
            var graph = new EdgeListGraph<string>(isDirected: true);
            graph.AddEdge("A", "B", weight: 2.1, label: "follow");
            Assert.That(graph.HasEdge("A", "B"), Is.True);
            Assert.That(graph.HasEdge("B", "A"), Is.False);
            var neighbors = graph.GetNeighbors("A").ToList();
            Assert.That(neighbors, Contains.Item("B"));
            Assert.That(graph.GetNeighbors("B"), Is.Empty);
        }

        /// <summary>
        /// 测试删除边功能
        /// </summary>
        [Test]
        public void RemoveEdge_RemovesBothSides_Undirected()
        {
            var graph = new EdgeListGraph<int>();
            graph.AddEdge(1, 3);
            graph.RemoveEdge(1, 3);
            Assert.That(graph.HasEdge(1, 3), Is.False);
            Assert.That(graph.HasEdge(3, 1), Is.False);
        }

        /// <summary>
        /// 测试多属性边（创建时间等）
        /// </summary>
        [Test]
        public void AddEdge_MultiAttributes_Success()
        {
            var graph = new EdgeListGraph<int>();
            var now = DateTime.UtcNow;
            graph.AddEdge(2, 4, weight: 10, label: "biz", createdAt: now);
            var edge = graph.Edges.FirstOrDefault(e => e.From == 2 && e.To == 4);
            Assert.That(edge, Is.Not.Null);
            Assert.That(edge.Label, Is.EqualTo("biz"));
            Assert.That(edge.CreatedAt, Is.EqualTo(now));
        }

        /// <summary>
        /// 测试获取所有节点
        /// </summary>
        [Test]
        public void GetNodes_ReturnsAllUniqueNodes()
        {
            var graph = new EdgeListGraph<int>();
            graph.AddEdge(1, 2);
            graph.AddEdge(2, 3);
            var nodes = graph.Nodes.ToList();
            Assert.That(nodes, Has.Count.EqualTo(3));
            Assert.That(nodes, Is.EquivalentTo(new[] { 1, 2, 3 }));
        }
    }

}
