// ***********************************************************************
// Assembly         : Noob.Algorithms
// Author           : noob
// Created          : 2025-06-28
//
// Last Modified By : noob
// Last Modified On : 2025-06-29
// ***********************************************************************
// <copyright file="GaleShapleyTests.cs" company="Noob.Algorithms">
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
    /// 用户（支持多属性兴趣，冷启动）
    /// </summary>
    public class User
    {
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>The identifier.</value>
        public int Id { get; set; }


        /// <summary>
        /// 是否冷启动用户（新用户）
        /// </summary>
        /// <value><c>true</c> if this instance is cold start; otherwise, <c>false</c>.</value>
        public bool IsColdStart { get; set; }

        /// <summary>
        /// 多属性兴趣/性格
        /// </summary>
        /// <value>The attributes.</value>
        public double[] Attributes { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="User" /> class.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="coldStart">if set to <c>true</c> [cold start].</param>
        /// <param name="attributes">The attributes.</param>
        public User(int id, bool coldStart, double[] attributes)
        {
            Id = id;
            IsColdStart = coldStart;
            Attributes = attributes;
        }
    }

    /// <summary>
    /// 匹配器支持GS、Greedy、多轮混合，可自定义属性权重和冷启动
    /// </summary>
    public class GaleShapleyAdvancedMatcher
    {
        /// <summary>
        /// The men
        /// </summary>
        private readonly List<User> _men;
        /// <summary>
        /// The women
        /// </summary>
        private readonly List<User> _women;
        /// <summary>
        /// The attribute weights
        /// </summary>
        private readonly double[] _attrWeights;
        /// <summary>
        /// The rand
        /// </summary>
        private readonly Random _rand;

        /// <summary>
        /// 常量定义
        /// </summary>
        private const double DefaultColdStartScore = 0.5;

        /// <summary>
        /// The unmatched
        /// </summary>
        private const int Unmatched = -1;

        /// <summary>
        /// Initializes a new instance of the <see cref="GaleShapleyAdvancedMatcher" /> class.
        /// </summary>
        /// <param name="men">The men.</param>
        /// <param name="women">The women.</param>
        /// <param name="attrWeights">The attribute weights.</param>
        /// <param name="seed">The seed.</param>
        public GaleShapleyAdvancedMatcher(List<User> men, List<User> women, double[] attrWeights, int? seed = null)
        {
            _men = men; _women = women;
            _attrWeights = attrWeights;
            _rand = seed.HasValue ? new Random(seed.Value) : new Random();
        }

        /// <summary>
        /// 生成满意度矩阵，支持多属性+冷启动
        /// </summary>
        /// <param name="groupA">The group a.</param>
        /// <param name="groupB">The group b.</param>
        /// <returns>System.Double[,].</returns>
        public double[,] BuildScoreMatrix(List<User> groupA, List<User> groupB)
        {
            int nA = groupA.Count, nB = groupB.Count;
            double[,] score = new double[nA, nB];
            for (int i = 0; i < nA; i++)
            {
                for (int j = 0; j < nB; j++)
                {
                    // 冷启动直接用均值0.5
                    if (groupA[i].IsColdStart || groupB[j].IsColdStart)
                    {
                        score[i, j] = 0.5;
                    }
                    else
                    {
                        score[i, j] = DotProduct(groupA[i].Attributes, groupB[j].Attributes, _attrWeights);
                    }
                }
            }
            return score;
        }


        /// <summary>
        /// 计算两个特征向量的加权点积（多属性相似度/满意度分数）。
        /// </summary>
        /// <param name="vectorA">第一个实体的属性特征向量</param>
        /// <param name="vectorB">第二个实体的属性特征向量</param>
        /// <param name="attributeWeights">各属性维度的权重</param>
        /// <returns>加权点积（相似度/满意度）</returns>
        private static double DotProduct(double[] vectorA, double[] vectorB, double[] attributeWeights)
        {
            if (vectorA == null || vectorB == null || attributeWeights == null)
                throw new ArgumentNullException("输入向量或权重不可为 null");
            if (vectorA.Length != vectorB.Length || vectorA.Length != attributeWeights.Length)
                throw new ArgumentException("属性向量与权重数组长度必须一致");

            double score = 0;
            for (int i = 0; i < vectorA.Length; i++)
            {
                // 可扩展：遇到 NaN/null 直接跳过或用冷启动默认值
                score += attributeWeights[i] * vectorA[i] * vectorB[i];
            }
            return score;
        }


        /// <summary>
        /// 多属性+冷启动的Gale-Shapley稳定婚姻匹配（男提案版）。
        /// </summary>
        /// <returns>配对结果：(男ID, 女ID, 男满意度, 女满意度)</returns>
        /// <exception cref="System.ArgumentException">男女人数必须一致，当前分别为" + nM + "与" + nW</exception>
        public List<(int Man, int Woman, double ScoreM, double ScoreW)> StableMatch()
        {
            int nM = _men.Count, nW = _women.Count;
            if (nM != nW) throw new ArgumentException("男女人数必须一致，当前分别为" + nM + "与" + nW);

            // Step 1: 生成满意度矩阵（男视角、女视角）
            var scoreM = BuildScoreMatrix(_men, _women);
            var scoreW = BuildScoreMatrix(_women, _men);

            // Step 2: 预排序生成偏好队列
            var manPrefs = new List<List<int>>(nM);
            var womanPrefs = new List<List<int>>(nW);
            for (int i = 0; i < nM; i++)
                manPrefs.Add(Enumerable.Range(0, nW).OrderByDescending(j => scoreM[i, j]).ToList());
            for (int j = 0; j < nW; j++)
                womanPrefs.Add(Enumerable.Range(0, nM).OrderByDescending(i => scoreW[j, i]).ToList());

            // Step 3: 初始化配对状态
            var engaged = Enumerable.Repeat(Unmatched, nW).ToArray(); // 女->男
            var next = new int[nM]; // 每个男生下一个要追求的女生index
            var free = new Queue<int>(Enumerable.Range(0, nM)); // 未配对男

            // Step 4: GS主循环
            while (free.Count > 0)
            {
                int m = free.Dequeue();
                if (next[m] >= manPrefs[m].Count)
                    continue; // 防越界（理论上不会发生，防御性编程）
                int w = manPrefs[m][next[m]++];
                if (engaged[w] == Unmatched)
                {
                    engaged[w] = m;
                }
                else
                {
                    int current = engaged[w];
                    // 女生偏好新男友胜过当前配偶，则替换
                    if (womanPrefs[w].IndexOf(m) < womanPrefs[w].IndexOf(current))
                    {
                        free.Enqueue(current); // 原配重回单身
                        engaged[w] = m;
                    }
                    else
                    {
                        free.Enqueue(m); // 继续追下一个
                    }
                }
            }

            // Step 5: 输出结果，分数Clamp到[0,1]
            var result = new List<(int, int, double, double)>();
            for (int w = 0; w < nW; w++)
            {
                int m = engaged[w];
                double sM = ClampScore(scoreM[m, w]);
                double sW = ClampScore(scoreW[w, m]);
                result.Add((_men[m].Id, _women[w].Id, sM, sW));
            }
            return result;
        }

        /// <summary>
        /// 分数强制限制在0-1区间（防止浮点误差/冷启动异常）
        /// </summary>
        /// <param name="score">The score.</param>
        /// <returns>System.Double.</returns>
        private static double ClampScore(double score) => Math.Max(0, Math.Min(1, score));

        /// <summary>
        /// 贪心算法(男优先，忽略女方意愿)
        /// </summary>
        /// <returns>List&lt;System.ValueTuple&lt;System.Int32, System.Int32, System.Double, System.Double&gt;&gt;.</returns>
        public List<(int Man, int Woman, double ScoreM, double ScoreW)> GreedyMatch()
        {
            int nM = _men.Count, nW = _women.Count;
            var scoreM = BuildScoreMatrix(_men, _women);
            var womanMatched = new HashSet<int>();
            var result = new List<(int Man, int Woman, double ScoreM, double ScoreW)>();
            for (int m = 0; m < nM; m++)
            {
                var order = Enumerable.Range(0, nW).OrderByDescending(j => scoreM[m, j]);
                foreach (var w in order)
                {
                    if (!womanMatched.Contains(w)) { womanMatched.Add(w); result.Add((m, w, scoreM[m, w], 0)); break; }
                }
            }
            // 女方满意度补齐
            var scoreW = BuildScoreMatrix(_women, _men);
            for (int i = 0; i < result.Count; i++)
                result[i] = (result[i].Man, result[i].Woman, result[i].ScoreM, scoreW[result[i].Woman, result[i].Man]);
            return result;
        }

        /// <summary>
        /// 多轮混合匹配(每轮GS，匹配过的下轮不参与)
        /// </summary>
        /// <param name="rounds">The rounds.</param>
        /// <returns>List&lt;System.ValueTuple&lt;System.Int32, System.Int32, System.Double, System.Double&gt;&gt;.</returns>
        public List<(int Man, int Woman, double ScoreM, double ScoreW)> MultiRoundStableMatch(int rounds)
        {
            var usedM = new HashSet<int>();
            var usedW = new HashSet<int>();
            var matches = new List<(int, int, double, double)>();
            for (int r = 0; r < rounds; r++)
            {
                var remMen = _men.FindAll(u => !usedM.Contains(u.Id));
                var remWomen = _women.FindAll(u => !usedW.Contains(u.Id));
                if (remMen.Count == 0 || remWomen.Count == 0) break;
                var sub = new GaleShapleyAdvancedMatcher(remMen, remWomen, _attrWeights);
                var pairs = sub.StableMatch();
                foreach (var (m, w, sm, sw) in pairs)
                {
                    int manId = remMen[m].Id, womanId = remWomen[w].Id;
                    matches.Add((manId, womanId, sm, sw));
                    usedM.Add(manId); usedW.Add(womanId);
                }
            }
            return matches;
        }

        /// <summary>
        /// 随机生成模拟用户
        /// </summary>
        /// <param name="n">The n.</param>
        /// <param name="attrCount">The attribute count.</param>
        /// <param name="coldRate">The cold rate.</param>
        /// <param name="rand">The rand.</param>
        /// <returns>List&lt;User&gt;.</returns>
        public static List<User> GenerateUsers(int n, int attrCount, double coldRate, Random rand)
        {
            int coldNum = (int)Math.Round(n * coldRate);
            var ids = Enumerable.Range(0, n).ToArray();
            if (coldNum > 0) ids = ids.OrderBy(_ => rand.Next()).ToArray();
            var coldSet = new HashSet<int>(ids.Take(coldNum));
            var users = new List<User>();
            for (int i = 0; i < n; i++)
            {
                bool cold = coldSet.Contains(i);
                double[] attrs = new double[attrCount];
                for (int j = 0; j < attrCount; j++)
                    attrs[j] = cold ? DefaultColdStartScore : rand.NextDouble();
                users.Add(new User(i, cold, attrs));
            }
            return users;
        }
    }




    /// <summary>
    /// Class GaleShapleyAdvancedMatcherTests.
    /// </summary>
    public class GaleShapleyAdvancedMatcherTests
    {
        /// <summary>
        /// Defines the test method StableMatch_Basic_ShouldBeStableAndFair.
        /// </summary>
        [Test]
        public void StableMatch_Basic_ShouldBeStableAndFair()
        {
            var men = GaleShapleyAdvancedMatcher.GenerateUsers(10, 2, 0, new Random(42));
            var women = GaleShapleyAdvancedMatcher.GenerateUsers(10, 2, 0, new Random(24));
            var weights = new double[] { 0.7, 0.3 };
            var matcher = new GaleShapleyAdvancedMatcher(men, women, weights, 123);
            var pairs = matcher.StableMatch();

            Assert.That(pairs.Count, Is.EqualTo(10), "应有10对配对");

            // 检查无重复匹配
            Assert.That(pairs.Select(p => p.Man).Distinct().Count(), Is.EqualTo(10), "每个男生唯一配对");
            Assert.That(pairs.Select(p => p.Woman).Distinct().Count(), Is.EqualTo(10), "每个女生唯一配对");

            // 满意度在0~1区间
            Assert.That(pairs.All(p => p.ScoreM >= 0 && p.ScoreM <= 1), Is.True, "男满意度应在0~1区间");
            Assert.That(pairs.All(p => p.ScoreW >= 0 && p.ScoreW <= 1), Is.True, "女满意度应在0~1区间");
        }

        /// <summary>
        /// Defines the test method GreedyMatch_ColdStart_ShouldNotThrow.
        /// </summary>
        [Test]
        public void GreedyMatch_ColdStart_ShouldNotThrow()
        {
            var men = GaleShapleyAdvancedMatcher.GenerateUsers(8, 3, 0.25, new Random(7));
            var women = GaleShapleyAdvancedMatcher.GenerateUsers(8, 3, 0.2, new Random(8));
            var weights = new double[] { 0.5, 0.3, 0.2 };
            var matcher = new GaleShapleyAdvancedMatcher(men, women, weights, 7);
            var pairs = matcher.GreedyMatch();

            Assert.That(pairs.Count, Is.EqualTo(8), "应有8对配对");

            // 冷启动满意度应为0.5
            Assert.That(
                pairs.Any(p => Math.Abs(p.ScoreM - 0.5) < 1e-8 || Math.Abs(p.ScoreW - 0.5) < 1e-8),
                Is.True, "应有冷启动满意度为0.5的对");
        }

        /// <summary>
        /// Defines the test method MultiRoundStableMatch_MatchMoreThanOneRound.
        /// </summary>
        [Test]
        public void MultiRoundStableMatch_MatchMoreThanOneRound()
        {
            var men = GaleShapleyAdvancedMatcher.GenerateUsers(20, 2, 0.15, new Random(11));
            var women = GaleShapleyAdvancedMatcher.GenerateUsers(20, 2, 0.12, new Random(12));
            var weights = new double[] { 0.6, 0.4 };
            var matcher = new GaleShapleyAdvancedMatcher(men, women, weights, 99);
            var pairs = matcher.MultiRoundStableMatch(2);

            // 至少完成一轮
            Assert.That(pairs.Count, Is.GreaterThan(10), "应至少完成一轮配对");

            // 检查匹配唯一性
            Assert.That(pairs.Select(p => p.Man).Distinct().Count(), Is.EqualTo(pairs.Count), "男配对应唯一");
            Assert.That(pairs.Select(p => p.Woman).Distinct().Count(), Is.EqualTo(pairs.Count), "女配对应唯一");
        }
    }
 
}
