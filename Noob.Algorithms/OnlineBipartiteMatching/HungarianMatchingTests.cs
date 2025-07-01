// ***********************************************************************
// Assembly         : Noob.Algorithms
// Author           : noob
// Created          : 2025-07-01
//
// Last Modified By : noob
// Last Modified On : 2025-07-01
// ***********************************************************************
// <copyright file="WeightedMatchingTests.cs" company="Noob.Algorithms">
//     Copyright (c) . All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************
using NUnit.Framework;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Noob.Algorithms.OnlineBipartiteMatching
{
    /// <summary>
    /// Class WeightFactors.
    /// </summary>
    public class WeightFactors
    {
        /// <summary>
        /// 距离重要性
        /// </summary>
        /// <value>The distance weight.</value>
        public double DistanceWeight { get; set; } = 0.5;

        /// <summary>
        ///  收益重要性
        /// </summary>
        /// <value>The profit weight.</value>
        public double ProfitWeight { get; set; } = 0.3;

        /// <summary>
        ///  偏好重要性
        /// </summary>
        /// <value>The preference weight.</value>
        public double PreferenceWeight { get; set; } = 0.2;

        /// <summary>
        /// 最大可接单距离
        /// </summary>
        /// <value>The maximum distance km.</value>
        public double MaxDistanceKm { get; set; } = 30;
    }

    /// <summary>
    /// Interface IWeightMatrixGenerator
    /// </summary>
    /// <typeparam name="TLeft">The type of the t left.</typeparam>
    /// <typeparam name="TRight">The type of the t right.</typeparam>
    public interface IWeightMatrixGenerator<TLeft, TRight>
    {
        /// <summary>
        /// 动态生成分配权重矩阵
        /// </summary>
        /// <param name="leftItems">左侧实体列表</param>
        /// <param name="rightItems">右侧实体列表</param>
        /// <returns>权重矩阵（行:左, 列:右, null=不可分配）</returns>
        double?[,] Generate(IReadOnlyList<TLeft> leftItems, IReadOnlyList<TRight> rightItems);
    }

    /// <summary>
    /// Class DoctorShiftWeightMatrixGenerator.
    /// Implements the <see cref="Noob.Algorithms.OnlineBipartiteMatching.IWeightMatrixGenerator{Noob.Algorithms.OnlineBipartiteMatching.Doctor, Noob.Algorithms.OnlineBipartiteMatching.Shift}" />
    /// </summary>
    /// <seealso cref="Noob.Algorithms.OnlineBipartiteMatching.IWeightMatrixGenerator{Noob.Algorithms.OnlineBipartiteMatching.Doctor, Noob.Algorithms.OnlineBipartiteMatching.Shift}" />
    public class DoctorShiftWeightMatrixGenerator : IWeightMatrixGenerator<Doctor, Shift>
    {
        /// <summary>
        /// The factors
        /// </summary>
        private readonly WeightFactors _factors;

        /// <summary>
        /// 是否符合条件
        /// </summary>
        private readonly Func<Doctor, Shift, bool> _isEligible;

        /// <summary>
        /// Initializes a new instance of the <see cref="DoctorShiftWeightMatrixGenerator"/> class.
        /// </summary>
        /// <param name="factors">The factors.</param>
        /// <param name="isEligible">The is eligible.</param>
        public DoctorShiftWeightMatrixGenerator(
            WeightFactors factors,
            Func<Doctor, Shift, bool> isEligible)
        {
            _factors = factors;
            _isEligible = isEligible;
        }

        /// <summary>
        /// Generates the specified doctors.
        /// </summary>
        /// <param name="doctors">The doctors.</param>
        /// <param name="shifts">The shifts.</param>
        /// <returns>System.Nullable&lt;System.Double&gt;[,].</returns>
        public double?[,] Generate(IReadOnlyList<Doctor> doctors, IReadOnlyList<Shift> shifts)
        {
            int n = doctors.Count, m = shifts.Count;
            var matrix = new double?[n, m];
            for (int i = 0; i < n; i++)
            {
                var doc = doctors[i];
                for (int j = 0; j < m; j++)
                {
                    var shift = shifts[j];
                    if (!_isEligible(doc, shift))
                    {
                        matrix[i, j] = null; // 不可分配
                        continue;
                    }

                    // 多因子加权——按实际业务可灵活组合
                    double distanceScore = 1.0 - Math.Min(shift.DistanceKm, _factors.MaxDistanceKm) / _factors.MaxDistanceKm;
                    double profitScore = shift.EstimatedProfit / (shift.EstimatedProfit + 10); // 归一化
                    double prefScore = doc.Preference?.GetScore(shift) ?? 0.5; // 偏好可为0~1
                    double total =
                        _factors.DistanceWeight * distanceScore +
                        _factors.ProfitWeight * profitScore +
                        _factors.PreferenceWeight * prefScore;

                    matrix[i, j] = total;
                }
            }
            return matrix;
        }
    }

    /// <summary>
    /// 匹配结果对象
    /// </summary>
    public class WeightedMatchingResult
    {
        /// <summary>
        /// 左侧实体对应匹配的右侧实体下标（-1为未匹配）
        /// </summary>
        public int[] MatchLeftToRight { get; set; }
        /// <summary>
        /// 匹配最大总权重
        /// </summary>
        public double TotalWeight { get; set; }
    }

    /// <summary>
    /// 带权最大匹配求解器接口（Kuhn-Munkres）。左侧和右侧元素可不等长，权重可为负或null。
    /// </summary>
    public interface IWeightedMatchingSolver
    {
        /// <summary>
        /// 求解二分图带权最大匹配
        /// </summary>
        /// <param name="weights">权重矩阵（行=左侧实体，列=右侧实体，null表示不可分配）</param>
        /// <returns>匹配结果</returns>
        WeightedMatchingResult Solve(double?[,] weights);
    }


    /// <summary>
    /// 基于KM(Hungarian)算法的带权最大匹配求解器，适用于二分图，权重可为负或null。
    /// </summary>
    public class HungarianWeightedMatcher : IWeightedMatchingSolver
    {
        /// <summary>数值零容忍误差（浮点比较）</summary>
        private const double Epsilon = 1e-8;


        /// <summary>
        /// 求解二分图带权最大匹配
        /// </summary>
        /// <param name="weights">权重矩阵（行=左侧实体，列=右侧实体，null表示不可分配）</param>
        /// <returns>匹配结果</returns>
        /// <exception cref="System.ArgumentNullException">weights - 权重矩阵不可为null</exception>
        /// <exception cref="System.ArgumentException">权重矩阵行列不能为空</exception>
        public WeightedMatchingResult Solve(double?[,] weights)
        {
            if (weights == null)
                throw new ArgumentNullException(nameof(weights), "权重矩阵不可为null");
            int nRows = weights.GetLength(0), nCols = weights.GetLength(1);
            if (nRows == 0 || nCols == 0)
                throw new ArgumentException("权重矩阵行列不能为空");

            // 【新增】全为null直接返回
            if (IsAllNull(weights))
            {
                return new WeightedMatchingResult
                {
                    MatchLeftToRight = Enumerable.Repeat(-1, nRows).ToArray(),
                    TotalWeight = 0.0
                };
            }

            // 稀疏邻接表，仅保留可分配边
            var adj = new List<(int Right, double Weight)>[nRows];
            for (int i = 0; i < nRows; i++)
            {
                adj[i] = new List<(int, double)>();
                for (int j = 0; j < nCols; j++)
                    if (weights[i, j].HasValue)
                        adj[i].Add((j, weights[i, j].Value));
            }

            // 【新增】逐行检测
            bool[] canAssign = new bool[nRows];
            for (int i = 0; i < nRows; i++)
                for (int j = 0; j < nCols; j++)
                    if (weights[i, j].HasValue) { canAssign[i] = true; break; }

            // KM仍需补全为方阵，所以最大节点数
            int n = Math.Max(nRows, nCols);
            double[,] cost = new double[n, n];
            for (int i = 0; i < n; i++)
                for (int j = 0; j < n; j++)
                    cost[i, j] = (i < nRows && j < nCols && weights[i, j].HasValue) ? weights[i, j].Value : double.NegativeInfinity;

            // 对象池：租用大对象，减少GC
            int[] match = ArrayPool<int>.Shared.Rent(n);
            double maxWeight = KuhnMunkres(cost, match, n, canAssign);

            // 返回真实匹配，归还对象池
            var result = new WeightedMatchingResult
            {
                MatchLeftToRight = match.Take(nRows).Select(idx => idx < nCols ? idx : -1).ToArray(),
                TotalWeight = maxWeight
            };
            ArrayPool<int>.Shared.Return(match);

            return result;
        }
      
        /// <summary>
        /// Determines whether [is all null] [the specified matrix].
        /// </summary>
        /// <param name="matrix">The matrix.</param>
        /// <returns><c>true</c> if [is all null] [the specified matrix]; otherwise, <c>false</c>.</returns>
        private static bool IsAllNull(double?[,] matrix)
        {
            foreach (var x in matrix) if (x.HasValue) return false;
            return true;
        }
        /// <summary>
        /// Kuhn-Munkres (KM) 算法实现（最大权重匹配）。
        /// 支持权重为负/零/正，无权边用 double.NegativeInfinity 填充。
        /// </summary>
        /// <param name="cost">方阵权重（size*size）</param>
        /// <param name="match">输出：左侧到右侧的匹配关系</param>
        /// <param name="n"></param>
        /// <param name="canAssign">是否允许匹配</param>
        /// <param name="nRows">行数</param>
        /// <returns>最大总权重</returns>
        private static double KuhnMunkres(double[,] cost, int[] match, int n, bool[] canAssign)
        {
            var labelX = new double[n];
            var labelY = new double[n];
            var parentY = new int[n];
            var committedX = new bool[n];
            var committedY = new bool[n];
            var prev = new int[n];
            var slack = new double[n];
            var slackX = new int[n];
            var queue = new int[n];

            // 初始化顶标
            for (int i = 0; i < n; i++)
            {
                labelX[i] = double.NegativeInfinity;
                for (int j = 0; j < n; j++)
                    labelX[i] = Math.Max(labelX[i], cost[i, j]);
            }

            Array.Fill(labelY, 0.0);
            Array.Fill(match, -1);

            for (int root = 0; root < n; root++)
            {
                if (root < canAssign.Length && !canAssign[root]) continue;

                Array.Fill(committedX, false);
                Array.Fill(committedY, false);
                Array.Fill(parentY, -1);
                Array.Fill(prev, -1);
                for (int j = 0; j < n; j++)
                {
                    slack[j] = labelX[root] + labelY[j] - cost[root, j];
                    slackX[j] = root;
                }

                int front = 0, rear = 0;
                queue[rear++] = root;

                int x = -1, y = -1;
                bool augmentFound = false;
                while (true)
                {
                    while (front < rear)
                    {
                        x = queue[front++];
                        committedX[x] = true;
                        for (y = 0; y < n; y++)
                        {
                            if (committedY[y]) continue;
                            double gap = labelX[x] + labelY[y] - cost[x, y];
                            if (gap < Epsilon)
                            {
                                committedY[y] = true;
                                parentY[y] = x;
                                if (match[y] == -1) { augmentFound = true; goto Augment; }
                                queue[rear++] = match[y];
                                prev[match[y]] = y;
                            }
                            else if (gap < slack[y])
                            {
                                slack[y] = gap;
                                slackX[y] = x;
                            }
                        }
                    }
                    // 没有增广路，调整顶标
                    double delta = double.PositiveInfinity;
                    for (int j = 0; j < n; j++)
                        if (!committedY[j]) delta = Math.Min(delta, slack[j]);

                    if (double.IsPositiveInfinity(delta)) break; // 【防死循环】

                    for (int i = 0; i < n; i++) if (committedX[i]) labelX[i] -= delta;
                    for (int j = 0; j < n; j++)
                    {
                        if (committedY[j]) labelY[j] += delta;
                        else slack[j] -= delta;
                    }
                    for (int y1 = 0; y1 < n; y1++)
                    {
                        if (!committedY[y1] && slack[y1] < Epsilon)
                        {
                            committedY[y1] = true;
                            parentY[y1] = slackX[y1];
                            if (match[y1] == -1) { y = y1; augmentFound = true; goto Augment; }
                            queue[rear++] = match[y1];
                            prev[match[y1]] = y1;
                        }
                    }
                }
            Augment:
                if (!augmentFound) continue; // 【若未增广直接跳过，保证不会死循环】

                while (y != -1)
                {
                    int x2 = parentY[y];
                    int nextY = match[y];
                    match[y] = x2;
                    y = nextY;
                }
            }
            // 计算最大权重和
            double result = 0.0;
            for (int y = 0; y < n; y++)
                if (match[y] != -1 && cost[match[y], y] > double.NegativeInfinity / 2)
                    result += cost[match[y], y];
            return result;
        }
    }


    /// <summary>
    /// Defines test class WeightMatrixGeneratorTests.
    /// </summary>
    [TestFixture]
    public class WeightMatrixGeneratorTests
    {
        [Test]
        public void Generator_Basic_Matrix_Correctness()
        {
            var doctors = new List<Doctor>
            {
                new Doctor{Id=1, Preference=new PreferenceSim(101)},
                new Doctor{Id=2, Preference=new PreferenceSim(102)}
            };
            var shifts = new List<Shift>
            {
                new Shift{Id=101, DistanceKm=10, EstimatedProfit=20, RequiredSkill=null},
                new Shift{Id=102, DistanceKm=20, EstimatedProfit=30, RequiredSkill=null}
            };
            var factors = new WeightFactors { DistanceWeight = 0.4, ProfitWeight = 0.4, PreferenceWeight = 0.2, MaxDistanceKm = 25 };
            var generator = new DoctorShiftWeightMatrixGenerator(
                factors,
                (doc, shift) => true // 全部可分配
            );
            var matrix = generator.Generate(doctors, shifts);

            Assert.That(matrix.GetLength(0), Is.EqualTo(2));
            Assert.That(matrix.GetLength(1), Is.EqualTo(2));
            // 检查偏好：第一个医生偏好101，第二个偏好102
            Assert.That(matrix[0, 0], Is.GreaterThan(matrix[0, 1]));
            Assert.That(matrix[1, 1], Is.GreaterThan(matrix[1, 0]));
        }

        /// <summary>
        /// Defines the test method Generator_Eligibility_Respects_Filtering.
        /// </summary>
        [Test]
        public void Generator_Eligibility_Respects_Filtering()
        {
            var doctors = new List<Doctor> { new Doctor { Id = 1 }, new Doctor { Id = 2 } };
            var shifts = new List<Shift> { new Shift { Id = 10 }, new Shift { Id = 11 } };
            var generator = new DoctorShiftWeightMatrixGenerator(
                new WeightFactors(),
                (doc, shift) => doc.Id == shift.Id - 9 // 仅允许 (1,10),(2,11)
            );
            var matrix = generator.Generate(doctors, shifts);

            Assert.That(matrix[0, 1], Is.Null);
            Assert.That(matrix[1, 0], Is.Null);
            Assert.That(matrix[0, 0], Is.Not.Null);
            Assert.That(matrix[1, 1], Is.Not.Null);
        }

    }

    /// <summary>
    /// Defines test class HungarianMatchingTests.
    /// </summary>
    [TestFixture]
    public class HungarianMatchingTests
    {
        /// <summary>
        /// 标准方阵输入，所有节点都有唯一最优匹配。校验算法能找到最大权重总和（对角线）。
        /// </summary>
        [Test]
        public void FullMatch_ExactMatrix_ShouldWork()
        {
            double?[,] weights = {
                { 9, 7, 6 },
                { 6, 8, 7 },
                { 7, 6, 8 }
            };
            var solver = new HungarianWeightedMatcher();
            var result = solver.Solve(weights);

            // 断言每个左侧节点都能唯一分配一个右侧节点
            Assert.That(result.MatchLeftToRight.Length, Is.EqualTo(3));
            Assert.That(new HashSet<int>(result.MatchLeftToRight).Count, Is.EqualTo(3));

            // 最大总权重应为25（全部主对角线）
            Assert.That(result.TotalWeight, Is.EqualTo(25).Within(1e-8));
        }

        /// <summary>
        /// 左侧实体多于右侧，测试部分节点匹配不到（单身）。
        /// </summary>
        [Test]
        public void MoreLeftThanRight_ShouldHaveSingle()
        {
            double?[,] weights = {
                { 10, null },
                { 6,  11 },
                { null,  9 }
            };
            var solver = new HungarianWeightedMatcher();
            var result = solver.Solve(weights);

            // 匹配数量不得超过右侧节点数量
            Assert.That(result.MatchLeftToRight.Length, Is.EqualTo(3));
            Assert.That(result.MatchLeftToRight.Count(x => x >= 0), Is.EqualTo(2));

            // 剩余节点为单身（-1）
            Assert.That(result.MatchLeftToRight.Count(x => x < 0), Is.EqualTo(1));
            Assert.That(result.TotalWeight, Is.GreaterThan(0));
        }


        /// <summary>
        /// 全为null（无可分配对），应返回所有单身，总权重为0。
        /// </summary>
        [Test]
        public void AllNulls_ShouldAllSingle()
        {
            double?[,] weights = {
                { null, null },
                { null, null }
            };
            var solver = new HungarianWeightedMatcher();
            var result = solver.Solve(weights);

            // 所有节点都未分配
            Assert.That(result.MatchLeftToRight, Is.All.LessThan(0));
            Assert.That(result.TotalWeight, Is.EqualTo(0));
        }

        /// <summary>
        /// 权重均为负值，算法应能正确找到最大（最小损失）匹配。
        /// </summary>
        [Test]
        public void NegativeWeights_CorrectMax()
        {
            double?[,] weights = {
                { -1, -2 },
                { -2, -1 }
            };
            var solver = new HungarianWeightedMatcher();
            var result = solver.Solve(weights);

            // 只能选择 -1 和 -1 的配对
            Assert.That(result.TotalWeight, Is.EqualTo(-2).Within(1e-8));
            Assert.That(new HashSet<int>(result.MatchLeftToRight).Count, Is.EqualTo(2));
        }
    }

}
