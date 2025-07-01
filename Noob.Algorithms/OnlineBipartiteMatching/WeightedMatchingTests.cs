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
        /// <summary>
        /// 求解二分图带权最大匹配
        /// </summary>
        /// <param name="weights">权重矩阵（行=左侧实体，列=右侧实体，null表示不可分配）</param>
        /// <returns>匹配结果</returns>
        public WeightedMatchingResult Solve(double?[,] weights)
        {
            int n = weights.GetLength(0);
            int m = weights.GetLength(1);
            // 补成方阵
            int size = Math.Max(n, m);
            double[,] cost = new double[size, size];
            for (int i = 0; i < size; i++)
                for (int j = 0; j < size; j++)
                    cost[i, j] = (i < n && j < m && weights[i, j].HasValue) ? weights[i, j].Value : double.NegativeInfinity;

            int[] match = new int[size]; // match[i]=j：左i匹配右j
            double total = KuhnMunkres(cost, match);

            return new WeightedMatchingResult
            {
                MatchLeftToRight = match.Take(n).Select(idx => idx < m ? idx : -1).ToArray(),
                TotalWeight = total
            };
        }

        /// <summary>
        /// 平台工程级 Kuhn-Munkres (KM) 算法实现（最大权重匹配）。
        /// 支持权重为负/零/正，无权边用 double.NegativeInfinity 填充。
        /// </summary>
        /// <param name="cost">方阵权重（size*size）</param>
        /// <param name="match">输出：左侧到右侧的匹配关系</param>
        /// <returns>最大总权重</returns>
        private static double KuhnMunkres(double[,] cost, int[] match)
        {
            int n = cost.GetLength(0);
            double[] lx = new double[n]; // 左侧顶点标签
            double[] ly = new double[n]; // 右侧顶点标签
            int[] py = new int[n]; // 右侧点的前驱
            bool[] sx = new bool[n], sy = new bool[n];

            for (int i = 0; i < n; i++)
            {
                lx[i] = double.NegativeInfinity;
                for (int j = 0; j < n; j++)
                    lx[i] = Math.Max(lx[i], cost[i, j]);
            }
            for (int i = 0; i < n; i++) ly[i] = 0;
            Array.Fill(match, -1);

            for (int root = 0; root < n; root++)
            {
                Array.Fill(sx, false);
                Array.Fill(sy, false);
                Array.Fill(py, -1);

                int[] queue = new int[n];
                int front = 0, rear = 0;
                queue[rear++] = root;
                int[] prev = new int[n];
                Array.Fill(prev, -1);
                double[] slack = new double[n];
                int[] slackx = new int[n];

                for (int j = 0; j < n; j++)
                {
                    slack[j] = lx[root] + ly[j] - cost[root, j];
                    slackx[j] = root;
                }

                int x = -1, y = -1;
                while (true)
                {
                    while (front < rear)
                    {
                        x = queue[front++];
                        sx[x] = true;
                        for (y = 0; y < n; y++)
                        {
                            if (sy[y]) continue;
                            double t = lx[x] + ly[y] - cost[x, y];
                            if (t < 1e-8)
                            {
                                sy[y] = true;
                                py[y] = x;
                                if (match[y] == -1) goto finish;
                                queue[rear++] = match[y];
                                prev[match[y]] = y;
                            }
                            else if (t < slack[y])
                            {
                                slack[y] = t;
                                slackx[y] = x;
                            }
                        }
                    }

                    // 没有可增广路，调整顶标
                    double delta = double.PositiveInfinity;
                    for (int j = 0; j < n; j++)
                        if (!sy[j]) delta = Math.Min(delta, slack[j]);
                    for (int i1 = 0; i1 < n; i1++) if (sx[i1]) lx[i1] -= delta;
                    for (int j = 0; j < n; j++)
                    {
                        if (sy[j]) ly[j] += delta;
                        else slack[j] -= delta;
                    }
                    for (int y1 = 0; y1 < n; y1++)
                    {
                        if (!sy[y1] && slack[y1] < 1e-8)
                        {
                            sy[y1] = true;
                            py[y1] = slackx[y1];
                            if (match[y1] == -1) { y = y1; goto finish; }
                            queue[rear++] = match[y1];
                            prev[match[y1]] = y1;
                        }
                    }
                }
            finish:
                // 增广
                while (y != -1)
                {
                    int x2 = py[y];
                    int nextY = match[y];
                    match[y] = x2;
                    y = nextY;
                }
            }
            // 计算最大权重和
            double result = 0;
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

    [TestFixture]
    public class WeightedMatchingTests
    {
        /// <summary>
        /// Defines the test method Simple_Bipartite_WeightMatching_Should_Work.
        /// </summary>
        [Test]
        public void Simple_Bipartite_WeightMatching_Should_Work()
        {
            // 3 doctors, 3 shifts，简单匹配
            double?[,] weights = {
                { 8, 7, 6 },
                { 6, 8, 7 },
                { 7, 6, 8 }
            };
            var solver = new HungarianWeightedMatcher();
            var result = solver.Solve(weights);

            Assert.That(result.MatchLeftToRight.Length, Is.EqualTo(3));
            // 每个医生都能唯一分配一个班次
            Assert.That(new HashSet<int>(result.MatchLeftToRight).Count, Is.EqualTo(3));
            // 总权重应为24（对角线全8最优）
            Assert.That(result.TotalWeight, Is.EqualTo(8 + 8 + 8).Within(1e-8));
        }

    }

}
