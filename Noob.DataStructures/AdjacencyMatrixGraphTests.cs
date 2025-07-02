// ***********************************************************************
// Assembly         : Noob.DataStructures
// Author           : noob
// Created          : 2025-07-02
//
// Last Modified By : noob
// Last Modified On : 2025-07-02
// ***********************************************************************
// <copyright file="AdjacencyMatrixGraphTests.cs" company="Noob.DataStructures">
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
    /// Class AdjacencyMatrixGraph.
    /// </summary>
    public class AdjacencyMatrixGraph
    {
        /// <summary>
        /// The matrix
        /// </summary>
        private readonly int[,] matrix;
        /// <summary>
        /// Gets the size.
        /// </summary>
        /// <value>The size.</value>
        public int Size { get; }
        /// <summary>
        /// Gets a value indicating whether this instance is directed.
        /// </summary>
        /// <value><c>true</c> if this instance is directed; otherwise, <c>false</c>.</value>
        public bool IsDirected { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AdjacencyMatrixGraph" /> class.
        /// </summary>
        /// <param name="size">The size.</param>
        /// <param name="isDirected">是否有向图</param>
        /// <exception cref="System.ArgumentException">Graph size must be positive.</exception>
        public AdjacencyMatrixGraph(int size, bool isDirected = false)
        {
            if (size <= 0) throw new ArgumentException("Graph size must be positive.");
            Size = size;
            IsDirected = isDirected;
            matrix = new int[size, size];
        }

        /// <summary>
        /// 添加一条边，weight默认为1
        /// </summary>
        /// <param name="from">From.</param>
        /// <param name="to">To.</param>
        /// <param name="weight">The weight.</param>
        /// <exception cref="System.ArgumentOutOfRangeException"></exception>
        public void AddEdge(int from, int to, int weight = 1)
        {
            if (!IsValidIndex(from) || !IsValidIndex(to))
                throw new ArgumentOutOfRangeException();

            matrix[from, to] = weight;
            if (!IsDirected)
                matrix[to, from] = weight;
        }

        /// <summary>
        /// 移除边
        /// </summary>
        /// <param name="from">From.</param>
        /// <param name="to">To.</param>
        /// <exception cref="System.ArgumentOutOfRangeException"></exception>
        public void RemoveEdge(int from, int to)
        {
            if (!IsValidIndex(from) || !IsValidIndex(to))
                throw new ArgumentOutOfRangeException();

            matrix[from, to] = 0;
            if (!IsDirected)
                matrix[to, from] = 0;
        }

        /// <summary>
        /// 查询边权重，0表示无边
        /// </summary>
        /// <param name="from">From.</param>
        /// <param name="to">To.</param>
        /// <returns>System.Int32.</returns>
        /// <exception cref="System.ArgumentOutOfRangeException"></exception>
        public int GetWeight(int from, int to)
        {
            if (!IsValidIndex(from) || !IsValidIndex(to))
                throw new ArgumentOutOfRangeException();

            return matrix[from, to];
        }

        /// <summary>
        /// 是否存在边
        /// </summary>
        /// <param name="from">From.</param>
        /// <param name="to">To.</param>
        /// <returns><c>true</c> if the specified from has edge; otherwise, <c>false</c>.</returns>
        public bool HasEdge(int from, int to) => GetWeight(from, to) != 0;

        /// <summary>
        /// Determines whether [is valid index] [the specified index].
        /// </summary>
        /// <param name="idx">The index.</param>
        /// <returns><c>true</c> if [is valid index] [the specified index]; otherwise, <c>false</c>.</returns>
        private bool IsValidIndex(int idx) => idx >= 0 && idx < Size;

        /// <summary>
        /// 输出邻接矩阵（调试用）
        /// </summary>
        /// <returns>System.Int32[,].</returns>
        public int[,] GetMatrixCopy()
        {
            var copy = new int[Size, Size];
            Array.Copy(matrix, copy, matrix.Length);
            return copy;
        }
    }




    /// <summary>
    /// Defines test class AdjacencyMatrixGraphTests.
    /// </summary>
    [TestFixture]
    public class AdjacencyMatrixGraphTests
    {
        /// <summary>
        /// Defines the test method UndirectedGraph_AddAndRemoveEdge_WorksCorrectly.
        /// </summary>
        [Test]
        public void UndirectedGraph_AddAndRemoveEdge_WorksCorrectly()
        {
            var graph = new AdjacencyMatrixGraph(4, isDirected: false);
            graph.AddEdge(0, 1);
            graph.AddEdge(1, 2);
            graph.AddEdge(2, 3);

            Assert.That(graph.HasEdge(0, 1), Is.True);
            Assert.That(graph.HasEdge(1, 0), Is.True);  // 无向
            Assert.That(graph.HasEdge(0, 2), Is.False);

            graph.RemoveEdge(1, 2);
            Assert.That(graph.HasEdge(1, 2), Is.False);
            Assert.That(graph.HasEdge(2, 1), Is.False);
        }

        /// <summary>
        /// Defines the test method DirectedGraph_AddEdge_OneDirectionOnly.
        /// </summary>
        [Test]
        public void DirectedGraph_AddEdge_OneDirectionOnly()
        {
            var graph = new AdjacencyMatrixGraph(3, isDirected: true);
            graph.AddEdge(0, 2);

            Assert.That(graph.HasEdge(0, 2), Is.True);
            Assert.That(graph.HasEdge(2, 0), Is.False); // 有向图只单向
        }

        /// <summary>
        /// Defines the test method WeightedGraph_AddWeightedEdge_StoresWeight.
        /// </summary>
        [Test]
        public void WeightedGraph_AddWeightedEdge_StoresWeight()
        {
            var graph = new AdjacencyMatrixGraph(4, isDirected: false);
            graph.AddEdge(1, 3, weight: 5);
            graph.AddEdge(0, 2, weight: 8);

            Assert.That(graph.GetWeight(1, 3), Is.EqualTo(5));
            Assert.That(graph.GetWeight(3, 1), Is.EqualTo(5)); // 无向
            Assert.That(graph.GetWeight(0, 2), Is.EqualTo(8));
            Assert.That(graph.HasEdge(2, 0), Is.True);
        }

        /// <summary>
        /// Defines the test method AddEdge_InvalidIndex_ThrowsException.
        /// </summary>
        [Test]
        public void AddEdge_InvalidIndex_ThrowsException()
        {
            var graph = new AdjacencyMatrixGraph(2, isDirected: false);
            Assert.That(() => graph.AddEdge(0, 2), Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        /// <summary>
        /// Defines the test method RemoveEdge_ThenHasEdge_ReturnsFalse.
        /// </summary>
        [Test]
        public void RemoveEdge_ThenHasEdge_ReturnsFalse()
        {
            var graph = new AdjacencyMatrixGraph(2);
            graph.AddEdge(0, 1);
            graph.RemoveEdge(0, 1);
            Assert.That(graph.HasEdge(0, 1), Is.False);
        }
    }


}
