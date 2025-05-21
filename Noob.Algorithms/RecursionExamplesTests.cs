// ***********************************************************************
// Assembly         : Noob.Algorithms
// Author           : noob
// Created          : 2025-05-21
//
// Last Modified By : noob
// Last Modified On : 2025-05-21
// ***********************************************************************
// <copyright file="RecursionExamplesTests.cs" company="Noob.Algorithms">
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

namespace Noob.Algorithms
{
    /// <summary>
    /// Class RecursionSamples.
    /// </summary>
    public static class RecursionSamples
    {
        /// <summary>
        /// 普通递归
        /// </summary>
        /// <param name="n">The n.</param>
        /// <returns>System.Int64.</returns>
        public static long FactorialRecursive(int n)
        {
            if (n <= 1) return 1;
            return n * FactorialRecursive(n - 1);
        }

        /// <summary>
        /// 尾递归（C# 实际为“累加器参数”写法）
        /// </summary>
        /// <param name="n">The n.</param>
        /// <param name="acc">The acc.</param>
        /// <returns>System.Int64.</returns>
        public static long FactorialTailRecursive(int n, long acc = 1)
        {
            if (n <= 1) return acc;
            return FactorialTailRecursive(n - 1, n * acc);
        }

        /// <summary>
        /// 尾递归手动转为迭代（推荐！）
        /// </summary>
        /// <param name="n">The n.</param>
        /// <returns>System.Int64.</returns>
        public static long FactorialIterative(int n)
        {
            long acc = 1;
            for (int i = n; i > 1; i--)
            {
                acc *= i;
            }
            return acc;
        }

        /// <summary>
        /// 斐波那契数列（Fibonacci）
        /// </summary>
        /// <param name="n">The n.</param>
        /// <returns>System.Int64.</returns>
        public static long FibonacciRecursive(int n)
        {
            if (n <= 2) return 1;
            return FibonacciRecursive(n - 1) + FibonacciRecursive(n - 2);
        }

        /// <summary>
        /// 斐波那契数列(缓存)
        /// </summary>
        /// <param name="n">The n.</param>
        /// <param name="memo">The memo.</param>
        /// <returns>System.Int64.</returns>
        public static long FibonacciMemo(int n, Dictionary<int, long>? memo = null)
        {
            memo ??= new Dictionary<int, long>();
            if (n <= 2) return 1;
            if (memo.ContainsKey(n)) return memo[n];
            memo[n] = FibonacciMemo(n - 1, memo) + FibonacciMemo(n - 2, memo);
            return memo[n];
        }

        /// <summary>
        /// 迭代法
        /// </summary>
        /// <param name="n">The n.</param>
        /// <returns>System.Int64.</returns>
        public static long FibonacciIterate(int n)
        {
            if (n <= 2) return 1;
            long a = 1, b = 1;
            for (int i = 3; i <= n; i++)
            {
                long tmp = a + b;
                a = b;
                b = tmp;
            }
            return b;
        }

        /// <summary>
        /// 计算在给定容量下，最大化价值的0-1背包问题解
        /// </summary>
        /// <param name="weights">每个物品的重量</param>
        /// <param name="values">每个物品的价值</param>
        /// <param name="capacity">背包总容量</param>
        /// <returns>最大可获得价值</returns>
        public static int KnapsackMemo(int[] weights, int[] values, int capacity)
        {
            if (weights == null || values == null)
                throw new ArgumentNullException("weights/values不能为null");
            if (weights.Length != values.Length)
                throw new ArgumentException("weights与values长度不一致");
            if (capacity < 0)
                throw new ArgumentException("容量不能为负数");

            var memo = new Dictionary<(int, int), int>();

            int Dfs(int index, int remainingCapacity)
            {
                if (index < 0 || remainingCapacity <= 0)
                    return 0;
                if (memo.TryGetValue((index, remainingCapacity), out int cached))
                    return cached;

                // 不选当前物品
                int result = Dfs(index - 1, remainingCapacity);

                // 选当前物品（如果装得下）
                if (remainingCapacity >= weights[index])
                {
                    result = Math.Max(result, Dfs(index - 1, remainingCapacity - weights[index]) + values[index]);
                }
                memo[(index, remainingCapacity)] = result;
                return result;
            }

            return Dfs(weights.Length - 1, capacity);
        }

        /// <summary>
        /// 计算0-1背包的最大价值（二维动态规划法）
        /// </summary>
        /// <param name="weights">每个物品的重量</param>
        /// <param name="values">每个物品的价值</param>
        /// <param name="capacity">背包总容量</param>
        /// <returns>最大可获得的价值</returns>
        public static int KnapsackDP(int[] weights, int[] values, int capacity)
        {
            if (weights == null || values == null)
                throw new ArgumentNullException("weights/values不能为null");
            if (weights.Length != values.Length)
                throw new ArgumentException("weights与values长度不一致");
            if (capacity < 0)
                throw new ArgumentException("容量不能为负数");

            int itemCount = weights.Length;
            int[,] dp = new int[itemCount + 1, capacity + 1];

            for (int i = 1; i <= itemCount; i++)
            {
                for (int w = 0; w <= capacity; w++)
                {
                    // 不选第i-1件物品
                    dp[i, w] = dp[i - 1, w];
                    // 选第i-1件物品
                    if (w >= weights[i - 1])
                    {
                        dp[i, w] = Math.Max(dp[i, w], dp[i - 1, w - weights[i - 1]] + values[i - 1]);
                    }
                }
            }
            return dp[itemCount, capacity];
        }

    }


    /// <summary>
    /// Defines test class RecursionSamplesTests.
    /// </summary>
    [TestFixture]
    public class RecursionSamplesTests
    {
        /// <summary>
        /// Defines the test method Test_FactorialRecursive.
        /// </summary>
        /// <param name="n">The n.</param>
        /// <param name="expected">The expected.</param>
        [TestCase(1, 1)]
        [TestCase(5, 120)]
        [TestCase(10, 3628800)]
        public void Test_FactorialRecursive(int n, long expected)
        {
            Assert.AreEqual(expected, RecursionSamples.FactorialRecursive(n));
        }

        /// <summary>
        /// Defines the test method Test_FactorialTailRecursive.
        /// </summary>
        /// <param name="n">The n.</param>
        /// <param name="expected">The expected.</param>
        [TestCase(1, 1)]
        [TestCase(5, 120)]
        [TestCase(10, 3628800)]
        public void Test_FactorialTailRecursive(int n, long expected)
        {
            Assert.AreEqual(expected, RecursionSamples.FactorialTailRecursive(n));
        }

        /// <summary>
        /// Defines the test method Test_FactorialIterative.
        /// </summary>
        /// <param name="n">The n.</param>
        /// <param name="expected">The expected.</param>
        [TestCase(1, 1)]
        [TestCase(5, 120)]
        [TestCase(10, 3628800)]
        public void Test_FactorialIterative(int n, long expected)
        {
            Assert.AreEqual(expected, RecursionSamples.FactorialIterative(n));
        }

        /// <summary>
        /// Defines the test method Test_FibonacciRecursive.
        /// </summary>
        /// <param name="n">The n.</param>
        /// <param name="expected">The expected.</param>
        [TestCase(1, 1)]
        [TestCase(2, 1)]
        [TestCase(5, 5)]
        [TestCase(10, 55)]
        public void Test_FibonacciRecursive(int n, long expected)
        {
            Assert.AreEqual(expected, RecursionSamples.FibonacciRecursive(n));
        }

        /// <summary>
        /// Defines the test method Test_FibonacciMemo.
        /// </summary>
        /// <param name="n">The n.</param>
        /// <param name="expected">The expected.</param>
        [TestCase(1, 1)]
        [TestCase(2, 1)]
        [TestCase(5, 5)]
        [TestCase(10, 55)]
        public void Test_FibonacciMemo(int n, long expected)
        {
            Assert.AreEqual(expected, RecursionSamples.FibonacciMemo(n));
        }

        /// <summary>
        /// Defines the test method Test_FibonacciIterate.
        /// </summary>
        /// <param name="n">The n.</param>
        /// <param name="expected">The expected.</param>
        [TestCase(1, 1)]
        [TestCase(2, 1)]
        [TestCase(5, 5)]
        [TestCase(10, 55)]
        public void Test_FibonacciIterate(int n, long expected)
        {
            Assert.AreEqual(expected, RecursionSamples.FibonacciIterate(n));
        }

        /// <summary>
        /// Tests the knapsack memo.
        /// </summary>
        [Test]
        public void Test_KnapsackMemo()
        {
            int[] weights = { 1, 2, 3 };//每个物品的重量
            int[] values = { 6, 10, 12 };//每个物品的价值
            int capacity = 5;//背包总容量
            int expected = 22; //最大化价值(10+12=22)
            int maxValue = RecursionSamples.KnapsackMemo(weights, values, capacity);
            Assert.AreEqual(expected, maxValue);
        }

        /// <summary>
        /// Defines the test method Test_KnapsackDP.
        /// </summary>
        [Test]
        public void Test_KnapsackDP()
        {
            int[] weights = { 1, 2, 3 };//每个物品的重量
            int[] values = { 6, 10, 12 };//每个物品的价值
            int capacity = 5;//背包总容量
            int expected = 22; //最大化价值(10+12=22)
            int maxValue = RecursionSamples.KnapsackDP(weights, values, capacity);
            Assert.AreEqual(expected, maxValue);
        }
    }
}
