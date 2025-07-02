// ***********************************************************************
// Assembly         : Noob.DataStructures
// Author           : noob
// Created          : 2025-07-02
//
// Last Modified By : noob
// Last Modified On : 2025-07-02
// ***********************************************************************
// <copyright file="AdjacencyListGraphTests.cs" company="Noob.DataStructures">
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
    /// 通用邻接表图结构，支持有向/无向、动态增删、邻居查找等功能
    /// </summary>
    /// <typeparam name="T">节点类型，需支持作为字典Key</typeparam>
    public class AdjacencyListGraph<T>
    {
        /// <summary>
        /// 邻接表主结构，Key为节点，Value为邻居集合
        /// </summary>
        private readonly Dictionary<T, HashSet<T>> adjList;

        /// <summary>
        /// 是否为有向图
        /// </summary>
        /// <value><c>true</c> if this instance is directed; otherwise, <c>false</c>.</value>
        public bool IsDirected { get; }

        /// <summary>
        /// 构造方法，指定是否为有向图
        /// </summary>
        /// <param name="isDirected">是否有向</param>
        public AdjacencyListGraph(bool isDirected = false)
        {
            IsDirected = isDirected;
            adjList = new Dictionary<T, HashSet<T>>();
        }

        /// <summary>
        /// 添加节点（如果已存在则忽略）
        /// </summary>
        /// <param name="node">节点</param>
        public void AddNode(T node)
        {
            if (!adjList.ContainsKey(node))
                adjList[node] = new HashSet<T>();
        }

        /// <summary>
        /// 添加边
        /// </summary>
        /// <param name="from">起点</param>
        /// <param name="to">终点</param>
        public void AddEdge(T from, T to)
        {
            AddNode(from);
            AddNode(to);
            adjList[from].Add(to);
            if (!IsDirected)
                adjList[to].Add(from);
        }

        /// <summary>
        /// 移除边
        /// </summary>
        /// <param name="from">起点</param>
        /// <param name="to">终点</param>
        public void RemoveEdge(T from, T to)
        {
            if (adjList.ContainsKey(from))
                adjList[from].Remove(to);
            if (!IsDirected && adjList.ContainsKey(to))
                adjList[to].Remove(from);
        }

        /// <summary>
        /// 移除节点及相关边
        /// </summary>
        /// <param name="node">节点</param>
        public void RemoveNode(T node)
        {
            if (!adjList.ContainsKey(node)) return;
            adjList.Remove(node);
            foreach (var neighbors in adjList.Values)
                neighbors.Remove(node);
        }

        /// <summary>
        /// 判断是否存在边
        /// </summary>
        /// <param name="from">起点</param>
        /// <param name="to">终点</param>
        /// <returns>是否存在</returns>
        public bool HasEdge(T from, T to)
            => adjList.ContainsKey(from) && adjList[from].Contains(to);

        /// <summary>
        /// 获取某节点的所有邻居
        /// </summary>
        /// <param name="node">节点</param>
        /// <returns>邻居集合</returns>
        public IEnumerable<T> GetNeighbors(T node)
            => adjList.ContainsKey(node) ? adjList[node] : new HashSet<T>();

        /// <summary>
        /// 获取所有节点
        /// </summary>
        /// <value>The nodes.</value>
        public IEnumerable<T> Nodes => adjList.Keys;
    }

    /// <summary>
    /// 表示图的边，可扩展业务属性（权重、标签、时间等）
    /// </summary>
    public class EdgeInfo
    {
        /// <summary>
        /// 边的权重
        /// </summary>
        /// <value>The weight.</value>
        public double Weight { get; set; }
        /// <summary>
        /// 可选标签（如"好友"、"推荐"等）
        /// </summary>
        /// <value>The label.</value>
        public string Label { get; set; }
        /// <summary>
        /// 边创建时间（可选）
        /// </summary>
        /// <value>The created at.</value>
        public DateTime? CreatedAt { get; set; }
    }

    /// <summary>
    /// 支持带权重、多属性的邻接表图结构（平台工程级）
    /// </summary>
    /// <typeparam name="TNode">节点类型</typeparam>
    public class WeightedAdjacencyListGraph<TNode>
    {
        /// <summary>
        /// 主邻接表。Key为起点，Value为终点到边属性的字典
        /// </summary>
        private readonly Dictionary<TNode, Dictionary<TNode, EdgeInfo>> _adjacencyList;
        /// <summary>
        /// 是否为有向图
        /// </summary>
        /// <value><c>true</c> if this instance is directed; otherwise, <c>false</c>.</value>
        public bool IsDirected { get; }

        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="isDirected">是否有向</param>
        public WeightedAdjacencyListGraph(bool isDirected = false)
        {
            IsDirected = isDirected;
            _adjacencyList = new Dictionary<TNode, Dictionary<TNode, EdgeInfo>>();
        }

        /// <summary>
        /// 添加节点（若已存在则忽略）
        /// </summary>
        /// <param name="node">The node.</param>
        public void AddNode(TNode node)
        {
            if (!_adjacencyList.ContainsKey(node))
                _adjacencyList[node] = new Dictionary<TNode, EdgeInfo>();
        }

        /// <summary>
        /// 添加带权边，支持多属性
        /// </summary>
        /// <param name="from">From.</param>
        /// <param name="to">To.</param>
        /// <param name="edge">The edge.</param>
        public void AddEdge(TNode from, TNode to, EdgeInfo edge)
        {
            AddNode(from);
            AddNode(to);
            _adjacencyList[from][to] = edge;
            if (!IsDirected)
                _adjacencyList[to][from] = edge;
        }

        /// <summary>
        /// 判断是否存在边
        /// </summary>
        /// <param name="from">From.</param>
        /// <param name="to">To.</param>
        /// <returns><c>true</c> if the specified from has edge; otherwise, <c>false</c>.</returns>
        public bool HasEdge(TNode from, TNode to) =>
            _adjacencyList.ContainsKey(from) && _adjacencyList[from].ContainsKey(to);

        /// <summary>
        /// 获取边的属性（无则返回null）
        /// </summary>
        /// <param name="from">From.</param>
        /// <param name="to">To.</param>
        /// <returns>EdgeInfo.</returns>
        public EdgeInfo GetEdge(TNode from, TNode to) =>
            HasEdge(from, to) ? _adjacencyList[from][to] : null;

        /// <summary>
        /// 删除边
        /// </summary>
        /// <param name="from">From.</param>
        /// <param name="to">To.</param>
        public void RemoveEdge(TNode from, TNode to)
        {
            if (_adjacencyList.ContainsKey(from))
                _adjacencyList[from].Remove(to);
            if (!IsDirected && _adjacencyList.ContainsKey(to))
                _adjacencyList[to].Remove(from);
        }

        /// <summary>
        /// 获取所有邻居及边属性
        /// </summary>
        /// <param name="node">The node.</param>
        /// <returns>IReadOnlyDictionary&lt;TNode, EdgeInfo&gt;.</returns>
        public IReadOnlyDictionary<TNode, EdgeInfo> GetNeighbors(TNode node) =>
            _adjacencyList.ContainsKey(node) ? _adjacencyList[node] : new Dictionary<TNode, EdgeInfo>();

        /// <summary>
        /// 获取所有节点
        /// </summary>
        /// <value>The nodes.</value>
        public IEnumerable<TNode> Nodes => _adjacencyList.Keys;
    }


    /// <summary>
    /// Defines test class AdjacencyListGraphTests.
    /// </summary>
    [TestFixture]
    public class AdjacencyListGraphTests
    {
        /// <summary>
        /// Defines the test method AddEdge_And_HasEdge_Works_Directed.
        /// </summary>
        [Test]
        public void AddEdge_And_HasEdge_Works_Directed()
        {
            var g = new AdjacencyListGraph<int>(isDirected: true);
            g.AddEdge(1, 2);
            g.AddEdge(1, 3);

            Assert.That(g.HasEdge(1, 2), Is.True);
            Assert.That(g.HasEdge(2, 1), Is.False);
            Assert.That(g.GetNeighbors(1), Is.EquivalentTo(new[] { 2, 3 }));
        }

        /// <summary>
        /// Defines the test method AddEdge_And_HasEdge_Works_Undirected.
        /// </summary>
        [Test]
        public void AddEdge_And_HasEdge_Works_Undirected()
        {
            var g = new AdjacencyListGraph<int>(isDirected: false);
            g.AddEdge(1, 2);

            Assert.That(g.HasEdge(1, 2), Is.True);
            Assert.That(g.HasEdge(2, 1), Is.True); // 无向图自动双向
        }

        /// <summary>
        /// Defines the test method RemoveEdge_RemovesEdgeProperly.
        /// </summary>
        [Test]
        public void RemoveEdge_RemovesEdgeProperly()
        {
            var g = new AdjacencyListGraph<int>(false);
            g.AddEdge(1, 2);
            g.RemoveEdge(1, 2);

            Assert.That(g.HasEdge(1, 2), Is.False);
            Assert.That(g.HasEdge(2, 1), Is.False);
        }

        /// <summary>
        /// Defines the test method RemoveNode_RemovesAllIncidentEdges.
        /// </summary>
        [Test]
        public void RemoveNode_RemovesAllIncidentEdges()
        {
            var g = new AdjacencyListGraph<int>(false);
            g.AddEdge(1, 2);
            g.AddEdge(2, 3);

            g.RemoveNode(2);

            Assert.That(g.HasEdge(1, 2), Is.False);
            Assert.That(g.HasEdge(2, 3), Is.False);
            Assert.That(g.GetNeighbors(1).Any(), Is.False);
        }

        /// <summary>
        /// Defines the test method GetNeighbors_ReturnsAllConnectedNodes.
        /// </summary>
        [Test]
        public void GetNeighbors_ReturnsAllConnectedNodes()
        {
            var g = new AdjacencyListGraph<int>(true);
            g.AddEdge(1, 2);
            g.AddEdge(1, 3);

            var neighbors = g.GetNeighbors(1).ToList();
            Assert.That(neighbors, Has.Count.EqualTo(2));
            Assert.That(neighbors, Is.EquivalentTo(new[] { 2, 3 }));
        }

        /// <summary>
        /// Defines the test method AddNode_DoesNotDuplicate.
        /// </summary>
        [Test]
        public void AddNode_DoesNotDuplicate()
        {
            var g = new AdjacencyListGraph<int>(false);
            g.AddNode(1);
            g.AddNode(1);
            Assert.That(g.Nodes.Count(), Is.EqualTo(1));
        }
    }



    /// <summary>
    /// Defines test class WeightedAdjacencyListGraphTests.
    /// </summary>
    [TestFixture]
    public class WeightedAdjacencyListGraphTests
    {
        /// <summary>
        /// Defines the test method AddEdge_WithWeightAndLabel_WorksUndirected.
        /// </summary>
        [Test]
        public void AddEdge_WithWeightAndLabel_WorksUndirected()
        {
            var graph = new WeightedAdjacencyListGraph<int>(isDirected: false);

            var edge = new EdgeInfo
            {
                Weight = 3.5,
                Label = "StrongTie",
                CreatedAt = new DateTime(2024, 1, 1)
            };

            graph.AddEdge(1, 2, edge);

            Assert.That(graph.HasEdge(1, 2), Is.True);
            Assert.That(graph.HasEdge(2, 1), Is.True);
            var info = graph.GetEdge(1, 2);
            Assert.That(info, Is.Not.Null);
            Assert.That(info.Weight, Is.EqualTo(3.5));
            Assert.That(info.Label, Is.EqualTo("StrongTie"));
            Assert.That(info.CreatedAt, Is.EqualTo(new DateTime(2024, 1, 1)));
            // 对称检查
            Assert.That(graph.GetEdge(2, 1), Is.SameAs(info));
        }

        /// <summary>
        /// Defines the test method AddEdge_WithWeightAndLabel_WorksDirected.
        /// </summary>
        [Test]
        public void AddEdge_WithWeightAndLabel_WorksDirected()
        {
            var graph = new WeightedAdjacencyListGraph<string>(isDirected: true);

            var edge = new EdgeInfo { Weight = 1.2, Label = "follow" };
            graph.AddEdge("Alice", "Bob", edge);

            Assert.That(graph.HasEdge("Alice", "Bob"), Is.True);
            Assert.That(graph.HasEdge("Bob", "Alice"), Is.False);
            Assert.That(graph.GetEdge("Alice", "Bob").Label, Is.EqualTo("follow"));
            Assert.That(graph.GetEdge("Bob", "Alice"), Is.Null);
        }

        /// <summary>
        /// Defines the test method RemoveEdge_RemovesFromBothSides_Undirected.
        /// </summary>
        [Test]
        public void RemoveEdge_RemovesFromBothSides_Undirected()
        {
            var graph = new WeightedAdjacencyListGraph<int>(isDirected: false);
            var edge = new EdgeInfo { Weight = 2.0 };
            graph.AddEdge(1, 3, edge);

            graph.RemoveEdge(1, 3);

            Assert.That(graph.HasEdge(1, 3), Is.False);
            Assert.That(graph.HasEdge(3, 1), Is.False);
        }

        /// <summary>
        /// Defines the test method GetNeighbors_ReturnsCorrectEdgeInfos.
        /// </summary>
        [Test]
        public void GetNeighbors_ReturnsCorrectEdgeInfos()
        {
            var graph = new WeightedAdjacencyListGraph<int>();
            var e1 = new EdgeInfo { Weight = 5 };
            var e2 = new EdgeInfo { Weight = 7 };

            graph.AddEdge(1, 2, e1);
            graph.AddEdge(1, 3, e2);

            var neighbors = graph.GetNeighbors(1);
            Assert.That(neighbors.Count, Is.EqualTo(2));
            Assert.That(neighbors[2].Weight, Is.EqualTo(5));
            Assert.That(neighbors[3].Weight, Is.EqualTo(7));
        }

        /// <summary>
        /// Defines the test method AddEdge_MultiAttributes_DistinctObjects.
        /// </summary>
        [Test]
        public void AddEdge_MultiAttributes_DistinctObjects()
        {
            var graph = new WeightedAdjacencyListGraph<int>();
            var e1 = new EdgeInfo { Weight = 1, Label = "A" };
            var e2 = new EdgeInfo { Weight = 2, Label = "B" };
            graph.AddEdge(1, 2, e1);
            graph.AddEdge(1, 3, e2);

            Assert.That(graph.GetEdge(1, 2).Label, Is.EqualTo("A"));
            Assert.That(graph.GetEdge(1, 3).Label, Is.EqualTo("B"));
        }
    }

}
