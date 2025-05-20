// ***********************************************************************
// Assembly         : Noob.Algorithms
// Author           : noob
// Created          : 2025-05-19
//
// Last Modified By : noob
// Last Modified On : 2025-05-19
// ***********************************************************************
// <copyright file="BucketMedianTests.cs" company="Noob.Algorithms">
//     Copyright (c) . All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// The Algorithms namespace.
/// </summary>
namespace Noob.Algorithms
{
    /// <summary>
    /// 提供基于分桶法的高效中值（Median）查找算法，支持多种数值类型。
    /// </summary>
    public static class BucketMedian
    {
        /// <summary>
        /// 使用分桶法查找数值序列的中值（Median），支持 int/long/double。
        /// </summary>
        /// <typeparam name="T">数值类型（int、long、double）</typeparam>
        /// <param name="source">输入数据集合</param>
        /// <param name="bucketCount">分桶数量（建议100~1000，默认1000）</param>
        /// <param name="toDouble">类型转 double 委托（如 x =&gt; (double)x）</param>
        /// <returns>中值</returns>
        /// <exception cref="System.ArgumentException">输入数据不能为空！</exception>
        public static double FindMedianByBucket<T>(
            IEnumerable<T> source,
            int bucketCount = 1000,
            Func<T, double>? toDouble = null)
            where T : IComparable<T>
        {
            if (source == null) throw new ArgumentException("输入数据不能为空！");
            var values = source.ToArray();
            if (values.Length == 0) throw new ArgumentException("输入数据不能为空！");
            if (toDouble == null) toDouble = x => Convert.ToDouble(x);

            int totalCount = values.Length;
            double minValue = values.Min(toDouble);
            double maxValue = values.Max(toDouble);
            if (minValue == maxValue) return minValue;

            double bucketWidth = (maxValue - minValue + 1) / bucketCount;
            int[] bucketCounts = new int[bucketCount];

            // 1. 统计每个桶的数量
            foreach (var value in values)
            {
                int bucketIndex = Math.Min((int)((toDouble(value) - minValue) / bucketWidth), bucketCount - 1);
                bucketCounts[bucketIndex]++;
            }

            // 2. 计算中值目标位置
            int leftMedianPosition = (totalCount - 1) / 2;
            int rightMedianPosition = totalCount / 2;
            int accumulatedCount = 0;
            int leftMedianBucket = -1, rightMedianBucket = -1;

            for (int i = 0; i < bucketCount; i++)
            {
                accumulatedCount += bucketCounts[i];
                if (leftMedianBucket == -1 && accumulatedCount > leftMedianPosition) leftMedianBucket = i;
                if (rightMedianBucket == -1 && accumulatedCount > rightMedianPosition) { rightMedianBucket = i; break; }
            }

            // 3. 收集目标桶元素
            List<double> leftBucketValues = new List<double>();
            List<double> rightBucketValues = leftMedianBucket == rightMedianBucket ? leftBucketValues : new List<double>();
            foreach (var value in values)
            {
                int bucketIndex = Math.Min((int)((toDouble(value) - minValue) / bucketWidth), bucketCount - 1);
                if (bucketIndex == leftMedianBucket) leftBucketValues.Add(toDouble(value));
                else if (bucketIndex == rightMedianBucket) rightBucketValues.Add(toDouble(value));
            }

            // 4. 排序并取中值
            leftBucketValues.Sort();
            if (leftMedianBucket == rightMedianBucket)
            {
                int leftOffset = leftMedianPosition - (accumulatedCount - bucketCounts[leftMedianBucket]);
                int rightOffset = rightMedianPosition - (accumulatedCount - bucketCounts[rightMedianBucket]);
                return leftOffset == rightOffset
                    ? leftBucketValues[leftOffset]
                    : (leftBucketValues[leftOffset] + leftBucketValues[rightOffset]) / 2.0;
            }
            else
            {
                rightBucketValues.Sort();
                int leftOffset = leftMedianPosition - (accumulatedCount - bucketCounts[rightMedianBucket]);
                int rightOffset = rightMedianPosition - (accumulatedCount - bucketCounts[rightMedianBucket]);
                return (leftBucketValues[leftOffset] + rightBucketValues[rightOffset]) / 2.0;
            }
        }
    }


    /// <summary>
    /// Class DataSimulator.
    /// </summary>
    public static class DataSimulator
    {
        /// <summary>
        /// 生成正态分布（高斯分布）数据
        /// </summary>
        public static int[] GenerateNormal(int count, double mean = 0, double stdDev = 1)
        {
            var random = new Random();
            var result = new int[count];
            for (int i = 0; i < count; i++)
            {
                // Box-Muller变换
                double u1 = 1.0 - random.NextDouble();
                double u2 = 1.0 - random.NextDouble();
                double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) *
                                       Math.Sin(2.0 * Math.PI * u2);
                double value = mean + stdDev * randStdNormal;
                result[i] = (int)Math.Round(value);
            }
            return result;
        }

        /// <summary>
        /// 生成均匀分布数据
        /// </summary>
        public static int[] GenerateUniform(int count, int min, int max)
        {
            var random = new Random();
            var result = new int[count];
            for (int i = 0; i < count; i++)
                result[i] = random.Next(min, max + 1);
            return result;
        }

        /// <summary>
        /// 生成指数分布数据
        /// </summary>
        public static int[] GenerateExponential(int count, double lambda = 1.0)
        {
            var random = new Random();
            var result = new int[count];
            for (int i = 0; i < count; i++)
            {
                double u = random.NextDouble();
                double value = -Math.Log(1 - u) / lambda;
                result[i] = (int)Math.Round(value);
            }
            return result;
        }

        /// <summary>
        /// 生成双峰分布（混合两种正态分布）
        /// </summary>
        public static int[] GenerateBimodal(int count, double mean1, double std1, double mean2, double std2)
        {
            var random = new Random();
            var result = new int[count];
            for (int i = 0; i < count; i++)
            {
                if (random.NextDouble() < 0.5)
                {
                    // 第一峰
                    double u1 = 1.0 - random.NextDouble();
                    double u2 = 1.0 - random.NextDouble();
                    double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) *
                                           Math.Sin(2.0 * Math.PI * u2);
                    double value = mean1 + std1 * randStdNormal;
                    result[i] = (int)Math.Round(value);
                }
                else
                {
                    // 第二峰
                    double u1 = 1.0 - random.NextDouble();
                    double u2 = 1.0 - random.NextDouble();
                    double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) *
                                           Math.Sin(2.0 * Math.PI * u2);
                    double value = mean2 + std2 * randStdNormal;
                    result[i] = (int)Math.Round(value);
                }
            }
            return result;
        }
    }

    /// <summary>
    /// Class MedianTest.
    /// </summary>
    public class MedianTest
    {
        /// <summary>
        /// Defines the test method RunAll.
        /// </summary>
        [Test]
        public void RunAll()
        {
            TestMedian("Normal", DataSimulator.GenerateNormal(1_000_000, 500, 200));
            TestMedian("Uniform", DataSimulator.GenerateUniform(1_000_000, 1, 1000));
            TestMedian("Exponential", DataSimulator.GenerateExponential(1_000_000, 0.01));
            TestMedian("Bimodal", DataSimulator.GenerateBimodal(1_000_000, 300, 30, 800, 40));
        }

        /// <summary>
        /// Tests the median.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="data">The data.</param>
        public void TestMedian(string name, int[] data)
        {
            Console.WriteLine($"== {name} Distribution ==");
            var sw = Stopwatch.StartNew();
            double bucketMedian = BucketMedian.FindMedianByBucket(data);
            sw.Stop();
            Console.WriteLine($"Bucket Median: {bucketMedian,10:0.000} | Time: {sw.ElapsedMilliseconds} ms");

            sw.Restart();
            double classicMedian = FindExactMedian(data);
            sw.Stop();
            Console.WriteLine($"Exact Median : {classicMedian,10:0.000} | Time: {sw.ElapsedMilliseconds} ms");

            double error = Math.Abs(bucketMedian - classicMedian);
            Console.WriteLine($"Absolute Error: {error:0.000}\n");
        }

        // 标准中值算法，直接排序取中位数
        public double FindExactMedian(int[] data)
        {
            var sorted = data.OrderBy(x => x).ToArray();
            int n = sorted.Length;
            return n % 2 == 1
                ? sorted[n / 2]
                : (sorted[n / 2 - 1] + sorted[n / 2]) / 2.0;
        }
    }


    /// <summary>
    /// Class ParallelMedianSimulation.
    /// </summary>
    public static class ParallelMedianSimulation
    {
        /// <summary>
        /// 并行测试多分布大样本下分桶法中值算法
        /// </summary>
        public static void RunAllSimulations(int sampleCount = 10_000_000)
        {
            Parallel.Invoke(
                () => SimulateAndAssert("Normal", DataGen.GenerateNormal(sampleCount, 1000, 200)),
                () => SimulateAndAssert("Uniform", DataGen.GenerateUniform(sampleCount, 1, 2000)),
                () => SimulateAndAssert("Exponential", DataGen.GenerateExponential(sampleCount, 0.01)),
                () => SimulateAndAssert("Bimodal", DataGen.GenerateBimodal(sampleCount, 500, 50, 1500, 60))
            );
        }

        /// <summary>
        /// 单分布下分桶法与精确中值性能和误差对比
        /// </summary>
        public static void SimulateAndAssert(string distName, int[] data)
        {
            Console.WriteLine($"\n== {distName} Distribution == [样本数: {data.Length:N0}]");

            var sw = Stopwatch.StartNew();
            double bucketMedian = BucketMedian.FindMedianByBucket(data);
            sw.Stop();
            Console.WriteLine($"Bucket Median: {bucketMedian,12:0.000} | Time: {sw.ElapsedMilliseconds,5} ms");

            sw.Restart();
            double exactMedian = ExactMedian(data);
            sw.Stop();
            Console.WriteLine($"Exact Median : {exactMedian,12:0.000} | Time: {sw.ElapsedMilliseconds,5} ms");

            double absError = Math.Abs(bucketMedian - exactMedian);
            Console.WriteLine($"Absolute Error: {absError:0.000}\n");

            // 单元测试断言：误差阈值不大于桶宽度
            double allowedError = (data.Max() - data.Min()) / 1000.0; // 可调整
            Assert.LessOrEqual(absError, allowedError, $"{distName} 分布分桶法中值误差过大！");
        }

        /// <summary>
        /// 标准精确中值（排序法）
        /// </summary>
        public static double ExactMedian(int[] data)
        {
            var sorted = data.AsParallel().OrderBy(x => x).ToArray();
            int n = sorted.Length;
            return n % 2 == 1 ? sorted[n / 2] : (sorted[n / 2 - 1] + sorted[n / 2]) / 2.0;
        }
    }

    /// <summary>
    /// Class DataGen.
    /// </summary>
    public static class DataGen
    {
        /// <summary>
        /// 并行生成正态分布（高斯分布）整数数据。
        /// </summary>
        public static int[] GenerateNormal(int count, double mean, double stdDev)
        {
            int[] data = new int[count];
            int processorCount = Environment.ProcessorCount;
            int chunkSize = count / processorCount;

            Parallel.For(0, processorCount, t =>
            {
                var rand = new Random(Guid.NewGuid().GetHashCode() + t);
                int start = t * chunkSize;
                int end = (t == processorCount - 1) ? count : (t + 1) * chunkSize;
                for (int i = start; i < end; i++)
                {
                    // Box-Muller变换生成正态分布
                    double u1 = 1.0 - rand.NextDouble();
                    double u2 = 1.0 - rand.NextDouble();
                    double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) *
                                           Math.Sin(2.0 * Math.PI * u2);
                    double value = mean + stdDev * randStdNormal;
                    data[i] = (int)Math.Round(value);
                }
            });
            return data;
        }

        /// <summary>
        /// 并行生成均匀分布整数数据。
        /// </summary>
        public static int[] GenerateUniform(int count, int min, int max)
        {
            int[] data = new int[count];
            int processorCount = Environment.ProcessorCount;
            int chunkSize = count / processorCount;

            Parallel.For(0, processorCount, t =>
            {
                var rand = new Random(Guid.NewGuid().GetHashCode() + t);
                int start = t * chunkSize;
                int end = (t == processorCount - 1) ? count : (t + 1) * chunkSize;
                for (int i = start; i < end; i++)
                {
                    data[i] = rand.Next(min, max + 1);
                }
            });
            return data;
        }

        /// <summary>
        /// 并行生成指数分布整数数据（参数lambda越大，分布越靠近零）。
        /// </summary>
        public static int[] GenerateExponential(int count, double lambda)
        {
            int[] data = new int[count];
            int processorCount = Environment.ProcessorCount;
            int chunkSize = count / processorCount;

            Parallel.For(0, processorCount, t =>
            {
                var rand = new Random(Guid.NewGuid().GetHashCode() + t);
                int start = t * chunkSize;
                int end = (t == processorCount - 1) ? count : (t + 1) * chunkSize;
                for (int i = start; i < end; i++)
                {
                    double u = rand.NextDouble();
                    double value = -Math.Log(1 - u) / lambda;
                    data[i] = (int)Math.Round(value);
                }
            });
            return data;
        }

        /// <summary>
        /// 并行生成双峰（混合正态）分布整数数据。
        /// </summary>
        public static int[] GenerateBimodal(int count, double mean1, double std1, double mean2, double std2)
        {
            int[] data = new int[count];
            int processorCount = Environment.ProcessorCount;
            int chunkSize = count / processorCount;

            Parallel.For(0, processorCount, t =>
            {
                var rand = new Random(Guid.NewGuid().GetHashCode() + t);
                int start = t * chunkSize;
                int end = (t == processorCount - 1) ? count : (t + 1) * chunkSize;
                for (int i = start; i < end; i++)
                {
                    if (rand.NextDouble() < 0.5)
                    {
                        // 第一峰
                        double u1 = 1.0 - rand.NextDouble();
                        double u2 = 1.0 - rand.NextDouble();
                        double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) *
                                               Math.Sin(2.0 * Math.PI * u2);
                        double value = mean1 + std1 * randStdNormal;
                        data[i] = (int)Math.Round(value);
                    }
                    else
                    {
                        // 第二峰
                        double u1 = 1.0 - rand.NextDouble();
                        double u2 = 1.0 - rand.NextDouble();
                        double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) *
                                               Math.Sin(2.0 * Math.PI * u2);
                        double value = mean2 + std2 * randStdNormal;
                        data[i] = (int)Math.Round(value);
                    }
                }
            });
            return data;
        }
    }

    /// <summary>
    /// Defines test class BucketMedianParallelSimulationTests.
    /// </summary>
    [TestFixture]
    public class BucketMedianParallelSimulationTests
    {
        /// <summary>
        /// 大规模分布仿真并断言分桶法的中值误差均小于允许值（桶宽）。
        /// </summary>
        [Test]
        public void Test_BucketMedian_HighVolume_Distributions()
        {
            ParallelMedianSimulation.RunAllSimulations(sampleCount: 20_000_000); // 2000万样本量仿真
        }
    }
}
