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

}
