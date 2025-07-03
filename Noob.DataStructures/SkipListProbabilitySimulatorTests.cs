// ***********************************************************************
// Assembly         : Noob.DataStructures
// Author           : noob
// Created          : 2025-07-03
//
// Last Modified By : noob
// Last Modified On : 2025-07-03
// ***********************************************************************
// <copyright file="SkipListProbabilitySimulatorTests.cs" company="Noob.DataStructures">
//     Copyright (c) . All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************
using NUnit.Framework;
using System;
using System.Collections.Generic;


namespace Noob.DataStructures
{

    /// <summary>
    /// 跳表晋升概率与空间、查找性能模拟器（支持泛型与自定义比较器）
    /// </summary>
    /// <typeparam name="T">节点值类型</typeparam>
    public class SkipListProbabilitySimulator<T> where T : IComparable<T>
    {
        /// <summary>
        /// 跳表模拟节点
        /// </summary>
        private class Node
        {
            /// <summary>
            /// The value
            /// </summary>
            public T Value;
            /// <summary>
            /// The forwards
            /// </summary>
            public List<Node> Forwards;

            /// <summary>
            /// Initializes a new instance of the <see cref="Node"/> class.
            /// </summary>
            /// <param name="value">The value.</param>
            public Node(T value)
            {
                Value = value;
                Forwards = new List<Node>();
            }
        }

        /// <summary>
        /// 跳表主体结构
        /// </summary>
        private class SkipList
        {
            /// <summary>
            /// Gets or sets the level count.
            /// </summary>
            /// <value>The level count.</value>
            public int LevelCount { get; private set; }
            /// <summary>
            /// Gets or sets the node count.
            /// </summary>
            /// <value>The node count.</value>
            public int NodeCount { get; private set; }
            /// <summary>
            /// Gets or sets the index node counts.
            /// </summary>
            /// <value>The index node counts.</value>
            public int[] IndexNodeCounts { get; private set; }
            /// <summary>
            /// The promotion probability
            /// </summary>
            private readonly double _promotionProbability;
            /// <summary>
            /// The maximum level
            /// </summary>
            private readonly int _maxLevel;
            /// <summary>
            /// The head
            /// </summary>
            private readonly Node _head;
            /// <summary>
            /// The comparer
            /// </summary>
            private readonly IComparer<T> _comparer;
            /// <summary>
            /// The random
            /// </summary>
            private readonly Random _random;

            /// <summary>
            /// 跳表初始化
            /// </summary>
            /// <param name="maxLevel">最大层数</param>
            /// <param name="promotionProbability">晋升概率</param>
            /// <param name="comparer">自定义比较器</param>
            public SkipList(int maxLevel, double promotionProbability, IComparer<T> comparer = null)
            {
                _maxLevel = maxLevel;
                _promotionProbability = promotionProbability;
                _head = new Node(default(T));
                _comparer = comparer ?? Comparer<T>.Default;
                _random = new Random();
                IndexNodeCounts = new int[maxLevel];
                LevelCount = 1;
                NodeCount = 0;
            }

            /// <summary>
            /// 随机生成节点层数
            /// </summary>
            /// <returns>System.Int32.</returns>
            private int RandomLevel()
            {
                int level = 1;
                while (_random.NextDouble() < _promotionProbability && level < _maxLevel)
                    level++;
                return level;
            }

            /// <summary>
            /// 插入节点，并统计每层索引节点数
            /// </summary>
            /// <param name="value">The value.</param>
            public void Insert(T value)
            {
                var update = new Node[_maxLevel];
                var curr = _head;
                for (int i = LevelCount - 1; i >= 0; i--)
                {
                    while (curr.Forwards.Count > i && curr.Forwards[i] != null
                           && _comparer.Compare(curr.Forwards[i].Value, value) < 0)
                        curr = curr.Forwards[i];
                    update[i] = curr;
                }

                int nodeLevel = RandomLevel();
                if (nodeLevel > LevelCount)
                {
                    for (int i = LevelCount; i < nodeLevel; i++)
                        update[i] = _head;
                    LevelCount = nodeLevel;
                }
                var newNode = new Node(value);
                for (int i = 0; i < nodeLevel; i++)
                {
                    if (update[i].Forwards.Count > i)
                    {
                        newNode.Forwards.Add(update[i].Forwards[i]);
                        update[i].Forwards[i] = newNode;
                    }
                    else
                    {
                        newNode.Forwards.Add(null);
                        update[i].Forwards.Add(newNode);
                    }
                    IndexNodeCounts[i]++;
                }
                NodeCount++;
            }

            /// <summary>
            /// 查找某值，并统计步数
            /// </summary>
            /// <param name="target">The target.</param>
            /// <returns>System.Int32.</returns>
            public int SearchSteps(T target)
            {
                int steps = 0;
                var curr = _head;
                for (int i = LevelCount - 1; i >= 0; i--)
                {
                    while (curr.Forwards.Count > i && curr.Forwards[i] != null
                           && _comparer.Compare(curr.Forwards[i].Value, target) < 0)
                    {
                        curr = curr.Forwards[i];
                        steps++;
                    }
                    steps++; // 每层至少一次比较
                }
                return steps;
            }
        }

        /// <summary>
        /// 运行多组不同晋升概率模拟
        /// </summary>
        /// <param name="data">要插入的数据集合</param>
        /// <param name="promotionProbabilities">晋升概率数组</param>
        /// <param name="searchSamples">查找样本个数</param>
        public static void RunSimulation(IList<T> data, double[] promotionProbabilities, int searchSamples = 100)
        {
            Console.WriteLine("晋升概率\t平均层数\t总索引节点\t平均查找步数");
            foreach (var p in promotionProbabilities)
            {
                int maxLevel = (int)Math.Ceiling(Math.Log(data.Count, 1.0 / p)) + 2;
                var skipList = new SkipList(maxLevel, p);

                // 构建节点
                foreach (var item in data)
                    skipList.Insert(item);

                // 采样查找
                int totalSteps = 0;
                var rand = new Random();
                for (int i = 0; i < searchSamples; i++)
                {
                    int idx = rand.Next(0, data.Count);
                    totalSteps += skipList.SearchSteps(data[idx]);
                }

                int indexNodeSum = 0;
                for (int i = 0; i < skipList.LevelCount; i++)
                    indexNodeSum += skipList.IndexNodeCounts[i];

                Console.WriteLine($"{p:F2}\t\t{skipList.LevelCount}\t\t{indexNodeSum}\t\t{totalSteps / (double)searchSamples:F2}");
            }
        }
    }

    /// <summary>
    /// 跳表晋升概率模拟器单元测试
    /// </summary>
    [TestFixture]
    public class SkipListProbabilitySimulatorTests
    {
        /// <summary>
        /// 测试不同晋升概率下，空间和查找性能合理性
        /// </summary>
        [Test]
        public void PromotionProbability_SpaceAndSearchSteps_Reasonable()
        {
            var data = new List<int>();
            for (int i = 1; i <= 10000; i++)
                data.Add(i);

            double[] probs = { 0.5, 0.333, 0.25 };

            var output = new List<(double prob, int levels, int indexNodes, double avgSteps)>();
            int searchSamples = 3000; // 增大采样量！
            foreach (var p in probs)
            {
                int maxLevel = (int)Math.Ceiling(Math.Log(data.Count, 1.0 / p)) + 2;
                var skipListRawType = typeof(SkipListProbabilitySimulator<int>)
                    .GetNestedType("SkipList", System.Reflection.BindingFlags.NonPublic);
                var skipListType = skipListRawType.MakeGenericType(typeof(int));
                var skipList = Activator.CreateInstance(skipListType, maxLevel, p, null);

                var insertMethod = skipListType.GetMethod("Insert");
                foreach (var item in data)
                    insertMethod.Invoke(skipList, new object[] { item });

                var searchMethod = skipListType.GetMethod("SearchSteps");
                var rand = new Random();
                int totalSteps = 0;
                for (int i = 0; i < searchSamples; i++)
                {
                    int idx = rand.Next(0, data.Count);
                    totalSteps += (int)searchMethod.Invoke(skipList, new object[] { data[idx] });
                }
                var levelCount = (int)skipListType.GetProperty("LevelCount").GetValue(skipList);
                var indexNodeCounts = (int[])skipListType.GetProperty("IndexNodeCounts").GetValue(skipList);
                int indexSum = 0;
                for (int i = 0; i < levelCount; i++) indexSum += indexNodeCounts[i];

                output.Add((p, levelCount, indexSum, totalSteps / (double)searchSamples));
            }

            // 增大采样量后，理论趋势应更稳定
            Assert.That(output[0].indexNodes, Is.GreaterThan(output[1].indexNodes));
            Assert.That(output[1].indexNodes, Is.GreaterThan(output[2].indexNodes));
            Assert.That(output[0].avgSteps, Is.LessThan(output[1].avgSteps + 0.5));
            Assert.That(output[1].avgSteps, Is.LessThan(output[2].avgSteps + 0.5));
            Assert.That(output[0].levels, Is.GreaterThan(output[1].levels));
            Assert.That(output[1].levels, Is.GreaterThan(output[2].levels));
        }
    }


}
