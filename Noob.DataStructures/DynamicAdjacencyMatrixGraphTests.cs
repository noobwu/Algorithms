// ***********************************************************************
// Assembly         : Noob.DataStructures
// Author           : noob
// Created          : 2025-07-02
//
// Last Modified By : noob
// Last Modified On : 2025-07-02
// ***********************************************************************
// <copyright file="DynamicAdjacencyMatrixGraphTests.cs" company="Noob.DataStructures">
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
    /// Class DynamicAdjacencyMatrixGraph.
    /// </summary>
    /// <typeparam name="TWeight">The type of the t weight.</typeparam>
    public class DynamicAdjacencyMatrixGraph<TWeight>
    {
        /// <summary>
        /// The matrix
        /// </summary>
        private List<List<TWeight>> matrix;
        /// <summary>
        /// The default value
        /// </summary>
        private readonly TWeight defaultValue;
        /// <summary>
        /// Gets a value indicating whether this instance is directed.
        /// </summary>
        /// <value><c>true</c> if this instance is directed; otherwise, <c>false</c>.</value>
        public bool IsDirected { get; }

        /// <summary>
        /// Gets the size.
        /// </summary>
        /// <value>The size.</value>
        public int Size => matrix.Count;

        /// <summary>
        /// Initializes a new instance of the <see cref="DynamicAdjacencyMatrixGraph{TWeight}" /> class.
        /// </summary>
        /// <param name="isDirected">if set to <c>true</c> [is directed].</param>
        /// <param name="defaultValue">The default value.</param>
        public DynamicAdjacencyMatrixGraph(bool isDirected, TWeight defaultValue)
        {
            this.IsDirected = isDirected;
            this.defaultValue = defaultValue;
            matrix = new List<List<TWeight>>();
        }

        /// <summary>
        /// Adds the vertex.
        /// </summary>
        /// <returns>System.Int32.</returns>
        public int AddVertex()
        {
            int idx = matrix.Count;
            // 扩展所有已有行
            foreach (var row in matrix) row.Add(defaultValue);
            // 新增一行
            var newRow = Enumerable.Repeat(defaultValue, idx + 1).ToList();
            matrix.Add(newRow);
            return idx;
        }

        /// <summary>
        /// Removes the vertex.
        /// </summary>
        /// <param name="idx">The index.</param>
        /// <exception cref="System.ArgumentOutOfRangeException"></exception>
        public void RemoveVertex(int idx)
        {
            if (idx < 0 || idx >= Size) throw new ArgumentOutOfRangeException();
            matrix.RemoveAt(idx);
            foreach (var row in matrix) row.RemoveAt(idx);
        }

        /// <summary>
        /// Adds the edge.
        /// </summary>
        /// <param name="from">From.</param>
        /// <param name="to">To.</param>
        /// <param name="weight">The weight.</param>
        public void AddEdge(int from, int to, TWeight weight)
        {
            matrix[from][to] = weight;
            if (!IsDirected)
                matrix[to][from] = weight;
        }

        /// <summary>
        /// Removes the edge.
        /// </summary>
        /// <param name="from">From.</param>
        /// <param name="to">To.</param>
        public void RemoveEdge(int from, int to)
        {
            matrix[from][to] = defaultValue;
            if (!IsDirected)
                matrix[to][from] = defaultValue;
        }

        /// <summary>
        /// Gets the weight.
        /// </summary>
        /// <param name="from">From.</param>
        /// <param name="to">To.</param>
        /// <returns>TWeight.</returns>
        public TWeight GetWeight(int from, int to) => matrix[from][to];


        /// <summary>
        /// Floyds the warshall.
        /// </summary>
        /// <returns>System.Double[,].</returns>
        public double[,] FloydWarshall()
        {
            int n = Size;
            var dist = new double[n, n];
            for (int i = 0; i < n; i++)
                for (int j = 0; j < n; j++)
                    dist[i, j] = EqualityComparer<TWeight>.Default.Equals(matrix[i][j], defaultValue) ? double.PositiveInfinity : Convert.ToDouble(matrix[i][j]);
            for (int i = 0; i < n; i++)
                dist[i, i] = 0.0; // 修复：自环显式为0

            for (int k = 0; k < n; k++)
                for (int i = 0; i < n; i++)
                    for (int j = 0; j < n; j++)
                        if (dist[i, k] + dist[k, j] < dist[i, j])
                            dist[i, j] = dist[i, k] + dist[k, j];
            return dist;
        }

        /// <summary>
        /// Gets the reachability.
        /// </summary>
        /// <returns>System.Boolean[,].</returns>
        public bool[,] GetReachability()
        {
            int n = Size;
            var reachable = new bool[n, n];
            for (int i = 0; i < n; i++)
                for (int j = 0; j < n; j++)
                    reachable[i, j] = !EqualityComparer<TWeight>.Default.Equals(matrix[i][j], defaultValue);

            for (int k = 0; k < n; k++)
                for (int i = 0; i < n; i++)
                    for (int j = 0; j < n; j++)
                        reachable[i, j] |= reachable[i, k] && reachable[k, j];
            return reachable;
        }

        /// <summary>
        /// Matrixes the power.
        /// </summary>
        /// <param name="k">The k.</param>
        /// <returns>System.Int32[,].</returns>
        public int[,] MatrixPower(int k)
        {
            int n = Size;
            int[,] result = new int[n, n];
            // 初始化为邻接矩阵（1/0形式）
            for (int i = 0; i < n; i++)
                for (int j = 0; j < n; j++)
                    result[i, j] = !EqualityComparer<TWeight>.Default.Equals(matrix[i][j], defaultValue) ? 1 : 0;
            int[,] power = (int[,])result.Clone();
            for (int step = 1; step < k; step++)
            {
                int[,] next = new int[n, n];
                for (int i = 0; i < n; i++)
                    for (int j = 0; j < n; j++)
                        for (int t = 0; t < n; t++)
                            next[i, j] += result[i, t] * power[t, j];
                result = next;
            }
            return result;
        }


    }

    /// <summary>
    /// Class EdgeData.
    /// </summary>
    public class EdgeData
    {
        /// <summary>
        /// Gets or sets the weight.
        /// </summary>
        /// <value>The weight.</value>
        public double Weight { get; set; }
        /// <summary>
        /// Gets or sets the created.
        /// </summary>
        /// <value>The created.</value>
        public DateTime Created { get; set; }
        /// <summary>
        /// Gets or sets the label.
        /// </summary>
        /// <value>The label.</value>
        public string Label { get; set; }
        // 可添加任意业务字段
    }




    /// <summary>
    /// Defines test class DynamicAdjacencyMatrixGraphTests.
    /// </summary>
    [TestFixture]
    public class DynamicAdjacencyMatrixGraphTests
    {
        /// <summary>
        /// Defines the test method AddVertex_DynamicallyIncreasesSize.
        /// </summary>
        [Test]
        public void AddVertex_DynamicallyIncreasesSize()
        {
            var graph = new DynamicAdjacencyMatrixGraph<int>(false, 0);
            int v0 = graph.AddVertex();
            int v1 = graph.AddVertex();
            int v2 = graph.AddVertex();

            Assert.That(graph.Size, Is.EqualTo(3));
            Assert.That(v2, Is.EqualTo(2));
        }

        /// <summary>
        /// Defines the test method RemoveVertex_UpdatesMatrixCorrectly.
        /// </summary>
        [Test]
        public void RemoveVertex_UpdatesMatrixCorrectly()
        {
            var graph = new DynamicAdjacencyMatrixGraph<int>(false, 0);
            var v0 = graph.AddVertex();
            var v1 = graph.AddVertex();
            var v2 = graph.AddVertex();
            graph.AddEdge(v0, v1, 1);
            graph.AddEdge(v1, v2, 2);

            graph.RemoveVertex(v1); // 删除中间点
            Assert.That(graph.Size, Is.EqualTo(2));
            // 边应消失
            Assert.That(graph.GetWeight(0, 1), Is.EqualTo(0));
        }

        /// <summary>
        /// Defines the test method AddEdge_WithEdgeData_StoresAndRetrievesCorrectly.
        /// </summary>
        [Test]
        public void AddEdge_WithEdgeData_StoresAndRetrievesCorrectly()
        {
            var graph = new DynamicAdjacencyMatrixGraph<EdgeData>(false, null);
            var a = graph.AddVertex();
            var b = graph.AddVertex();

            var edge = new EdgeData
            {
                Weight = 7.5,
                Created = new DateTime(2024, 1, 1),
                Label = "VIP"
            };
            graph.AddEdge(a, b, edge);

            var retrieved = graph.GetWeight(a, b);
            Assert.That(retrieved, Is.Not.Null);
            Assert.That(retrieved.Weight, Is.EqualTo(7.5));
            Assert.That(retrieved.Label, Is.EqualTo("VIP"));
            Assert.That(retrieved.Created, Is.EqualTo(new DateTime(2024, 1, 1)));
            // 无向图对称
            Assert.That(graph.GetWeight(b, a).Weight, Is.EqualTo(7.5));
        }

        /// <summary>
        /// Defines the test method RemoveEdge_SetsValueToDefault.
        /// </summary>
        [Test]
        public void RemoveEdge_SetsValueToDefault()
        {
            var graph = new DynamicAdjacencyMatrixGraph<int>(false, 0);
            var a = graph.AddVertex();
            var b = graph.AddVertex();

            graph.AddEdge(a, b, 2);
            graph.RemoveEdge(a, b);

            Assert.That(graph.GetWeight(a, b), Is.EqualTo(0));
            Assert.That(graph.GetWeight(b, a), Is.EqualTo(0));
        }

        /// <summary>
        /// Defines the test method FloydWarshall_ComputesAllPairsShortestPaths.
        /// </summary>
        [Test]
        public void FloydWarshall_ComputesAllPairsShortestPaths_DirectedGraph()
        {
            var graph = new DynamicAdjacencyMatrixGraph<double>(false, double.PositiveInfinity);
            var v0 = graph.AddVertex();
            var v1 = graph.AddVertex();
            var v2 = graph.AddVertex();

            graph.AddEdge(v0, v1, 3.0);
            graph.AddEdge(v1, v2, 4.0);
            graph.AddEdge(v0, v2, 10.0);

            var dist = graph.FloydWarshall();
            Assert.That(dist[v0, v2], Is.EqualTo(7.0).Within(0.0001)); // v0->v1->v2最短
            Assert.That(dist[v0, v0], Is.EqualTo(0.0));
            Assert.That(dist[v2, v0], Is.EqualTo(7.0).Within(0.0001)); // 无向
        }

        /// <summary>
        /// Defines the test method FloydWarshall_UndirectedGraph_ComputesAllPairsShortestPaths.
        /// </summary>
        [Test]
        public void FloydWarshall_UndirectedGraph_ComputesAllPairsShortestPaths()
        {
            // 有向图：A -> B (1), B -> C (2), A -> C (10)
            var graph = new DynamicAdjacencyMatrixGraph<double>(true, double.PositiveInfinity);
            int a = graph.AddVertex(); // 0
            int b = graph.AddVertex(); // 1
            int c = graph.AddVertex(); // 2

            graph.AddEdge(a, b, 1.0);
            graph.AddEdge(b, c, 2.0);
            graph.AddEdge(a, c, 10.0);

            var dist = graph.FloydWarshall();

            // 期望：A->C最短路径为A->B->C = 1+2=3，比A->C直接10短
            Assert.That(dist[a, c], Is.EqualTo(3.0).Within(1e-6));
            Assert.That(dist[a, b], Is.EqualTo(1.0).Within(1e-6));
            Assert.That(dist[b, c], Is.EqualTo(2.0).Within(1e-6));
            Assert.That(dist[a, a], Is.EqualTo(0.0));
            Assert.That(dist[b, b], Is.EqualTo(0.0));
            Assert.That(dist[c, c], Is.EqualTo(0.0));

            // 不可达：C->A, C->B
            Assert.That(double.IsPositiveInfinity(dist[c, a]), Is.True);
            Assert.That(double.IsPositiveInfinity(dist[c, b]), Is.True);

            // B不能到A（没有反向边）
            Assert.That(double.IsPositiveInfinity(dist[b, a]), Is.True);
        }

        /// <summary>
        /// Defines the test method ReachabilityMatrix_ComputesConnectedNodes.
        /// </summary>
        [Test]
        public void ReachabilityMatrix_ComputesConnectedNodes()
        {
            var graph = new DynamicAdjacencyMatrixGraph<int>(false, 0);
            var a = graph.AddVertex();
            var b = graph.AddVertex();
            var c = graph.AddVertex();

            graph.AddEdge(a, b, 1);

            var reach = graph.GetReachability();
            Assert.That(reach[a, b], Is.True);
            Assert.That(reach[b, a], Is.True);
            Assert.That(reach[a, c], Is.False);
            Assert.That(reach[b, c], Is.False);
        }

        /// <summary>
        /// Defines the test method MatrixPowers_DirectedGraph_CalculatesCorrectPathCounts.
        /// </summary>
        [Test]
        public void MatrixPowers_DirectedGraph_CalculatesCorrectPathCounts()
        {
            var graph = new DynamicAdjacencyMatrixGraph<int>(true, 0);
            var a = graph.AddVertex();
            var b = graph.AddVertex();
            var c = graph.AddVertex();

            graph.AddEdge(a, b, 1);
            graph.AddEdge(b, c, 1);

            var power2 = graph.MatrixPower(2);
            // a->b->c
            Assert.That(power2[a, c], Is.EqualTo(1));
            // b->a->b (不存在)
            Assert.That(power2[b, b], Is.EqualTo(0));
        }

        [Test]
        public void MatrixPower_UndirectedGraph_CorrectlyCountsPaths()
        {
            // 无向图：a-b-c
            var graph = new DynamicAdjacencyMatrixGraph<int>(false, 0);
            var a = graph.AddVertex();
            var b = graph.AddVertex();
            var c = graph.AddVertex();

            graph.AddEdge(a, b, 1);
            graph.AddEdge(b, c, 1);

            var power2 = graph.MatrixPower(2);

            // 2步路径统计
            Assert.That(power2[a, c], Is.EqualTo(1)); // a->b->c
            Assert.That(power2[a, a], Is.EqualTo(1)); // a->b->a
            Assert.That(power2[b, b], Is.EqualTo(2)); // b->a->b 和 b->c->b
            Assert.That(power2[c, a], Is.EqualTo(1)); // c->b->a
            Assert.That(power2[b, a], Is.EqualTo(0)); // b->c->a (不存在，因为没有c->a直接连通)
        }
    }


}
