using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Noob.Algorithms
{
    /// <summary>
    /// 0-1背包问题分支限界法（Branch & Bound）。
    /// </summary>
    public class KnapsackBranch
    {
        /// <summary>
        /// 结果类型：最大价值及最优选中方案。
        /// </summary>
        public class KnapsackResult
        {
            /// <summary>最大价值</summary>
            public int MaxValue { get; set; }
            /// <summary>选中物品方案（true表示选中）</summary>
            
            public bool[] Selected { get; set; }
        }

        /// <summary>
        /// 计算0-1背包分支限界法的最优解与选中方案
        /// </summary>
        /// <param name="weights">物品重量</param>
        /// <param name="values">物品价值</param>
        /// <param name="capacity">背包容量</param>
        /// <returns>最优解及物品选择方案</returns>
        public static KnapsackResult Calculate(int[] weights, int[] values, int capacity)
        {
            if (weights == null) throw new ArgumentNullException(nameof(weights));
            if (values == null) throw new ArgumentNullException(nameof(values));
            if (weights.Length != values.Length)
                throw new ArgumentException("weights和values数组长度必须一致");
            if (capacity < 0)
                throw new ArgumentException("背包容量不能为负数", nameof(capacity));

            int itemCount = weights.Length;
            int maxValue = 0;
            bool[] bestSelection = new bool[itemCount];

            // 优先队列，按节点上界降序（PriorityQueue默认小顶堆，优先级取负）
            var queue = new PriorityQueue<SearchNode, double>();

            // 初始节点
            var root = new SearchNode
            {
                Level = -1,
                Value = 0,
                Weight = 0,
                UpperBound = CalculateUpperBound(0, 0, 0, weights, values, capacity),
                Selection = new List<bool>()
            };
            queue.Enqueue(root, -root.UpperBound);

            while (queue.Count > 0)
            {
                var node = queue.Dequeue();
                int nextLevel = node.Level + 1;
                if (nextLevel >= itemCount)
                    continue;

                // 尝试选中当前物品
                int takenWeight = node.Weight + weights[nextLevel];
                int takenValue = node.Value + values[nextLevel];
                var selectionWithCurrent = new List<bool>(node.Selection) { true };

                if (takenWeight <= capacity)
                {
                    if (takenValue > maxValue)
                    {
                        maxValue = takenValue;
                        for (int i = 0; i < itemCount; i++)
                            bestSelection[i] = i < selectionWithCurrent.Count ? selectionWithCurrent[i] : false;
                    }
                    double takenBound = CalculateUpperBound(nextLevel + 1, takenWeight, takenValue, weights, values, capacity);
                    if (takenBound > maxValue)
                    {
                        queue.Enqueue(new SearchNode
                        {
                            Level = nextLevel,
                            Value = takenValue,
                            Weight = takenWeight,
                            UpperBound = takenBound,
                            Selection = selectionWithCurrent
                        }, -takenBound);
                    }
                }

                // 尝试不选当前物品
                var selectionWithoutCurrent = new List<bool>(node.Selection) { false };
                double skipBound = CalculateUpperBound(nextLevel + 1, node.Weight, node.Value, weights, values, capacity);
                if (skipBound > maxValue)
                {
                    queue.Enqueue(new SearchNode
                    {
                        Level = nextLevel,
                        Value = node.Value,
                        Weight = node.Weight,
                        UpperBound = skipBound,
                        Selection = selectionWithoutCurrent
                    }, -skipBound);
                }
            }

            return new KnapsackResult
            {
                MaxValue = maxValue,
                Selected = bestSelection
            };
        }

        /// <summary>
        /// 基于贪心填装的分支限界上界估算（允许装部分物品）
        /// </summary>
        private static double CalculateUpperBound(int startIndex, int currentWeight, int currentValue, int[] weights, int[] values, int capacity)
        {
            double bound = currentValue;
            int totalWeight = currentWeight;
            int n = weights.Length;

            // 构造剩余物品的单位价值降序队列
            var items = new List<(int Index, double Ratio)>();
            for (int i = startIndex; i < n; i++)
                items.Add((i, values[i] / (double)weights[i]));
            items.Sort((a, b) => b.Ratio.CompareTo(a.Ratio));

            foreach (var item in items)
            {
                int idx = item.Index;
                if (totalWeight + weights[idx] <= capacity)
                {
                    bound += values[idx];
                    totalWeight += weights[idx];
                }
                else
                {
                    // 装部分物品（分数背包思想）
                    bound += (capacity - totalWeight) * item.Ratio;
                    break;
                }
            }
            return bound;
        }

        /// <summary>
        /// 分支限界法搜索节点
        /// </summary>
        private class SearchNode
        {
            /// <summary>
            /// Gets or sets the level.
            /// </summary>
            /// <value>The level.</value>
            public int Level { get; set; }
            /// <summary>
            /// Gets or sets the value.
            /// </summary>
            /// <value>The value.</value>
            public int Value { get; set; }
            /// <summary>
            /// Gets or sets the weight.
            /// </summary>
            /// <value>The weight.</value>
            public int Weight { get; set; }
            /// <summary>
            /// Gets or sets the upper bound.
            /// </summary>
            /// <value>The upper bound.</value>
            public double UpperBound { get; set; }
            /// <summary>
            /// Gets or sets the selection.
            /// </summary>
            /// <value>The selection.</value>
            public List<bool> Selection { get; set; }
        }
    }

    /// <summary>
    /// Defines test class KnapsackBranchTests.
    /// </summary>
    [TestFixture]

    public class KnapsackBranchTests {
        /// <summary>
        /// Defines the test method Calculate.
        /// </summary>
        [Test]
        public void Calculate()
        {
            int[] weights = { 2, 2, 6, 5, 4 };//每个物品的重量
            int[] values = { 6, 3, 5, 4, 6 };//每个物品的价值
            int capacity = 10;//背包总容量
            int expected = 15;//最大化价值(6+3+6=15)
            var result = KnapsackBranch.Calculate(weights, values, capacity);
            Assert.AreEqual(expected, result.MaxValue);
            // 可断言result.Selected方案等
        }
    }
}
