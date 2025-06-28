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
        /// Initializes a new instance of the <see cref="User"/> class.
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
        private readonly List<User> _men;
        private readonly List<User> _women;
        private readonly double[] _attrWeights;
        private readonly Random _rand;

        /// <summary>
        /// Initializes a new instance of the <see cref="GaleShapleyAdvancedMatcher"/> class.
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
                    if (groupA[i].IsColdStart || groupB[j].IsColdStart) score[i, j] = 0.5;
                    else score[i, j] = DotProduct(groupA[i].Attributes, groupB[j].Attributes, _attrWeights);
                }
            }
            return score;
        }

        /// <summary>
        /// Dots the product.
        /// </summary>
        /// <param name="a">a.</param>
        /// <param name="b">The b.</param>
        /// <param name="w">The w.</param>
        /// <returns>System.Double.</returns>
        private static double DotProduct(double[] a, double[] b, double[] w)
        {
            double s = 0;
            for (int i = 0; i < a.Length; i++) s += w[i] * a[i] * b[i];
            return s;
        }
        /// <summary>
        /// 稳定婚姻算法(男提案)
        /// </summary>
        /// <returns>List&lt;System.ValueTuple&lt;System.Int32, System.Int32, System.Double, System.Double&gt;&gt;.</returns>
        public List<(int Man, int Woman, double ScoreM, double ScoreW)> StableMatch()
        {
            int nM = _men.Count, nW = _women.Count;
            var scoreM = BuildScoreMatrix(_men, _women);
            var scoreW = BuildScoreMatrix(_women, _men);
            var manPrefs = new List<List<int>>();
            var womanPrefs = new List<List<int>>();
            for (int i = 0; i < nM; i++)
                manPrefs.Add(Enumerable.Range(0, nW).OrderByDescending(j => scoreM[i, j]).ToList());
            for (int j = 0; j < nW; j++)
                womanPrefs.Add(Enumerable.Range(0, nM).OrderByDescending(i => scoreW[j, i]).ToList());
            var engaged = new int[nW]; Array.Fill(engaged, -1);
            var next = new int[nM];
            var free = new Queue<int>(Enumerable.Range(0, nM));
            while (free.Count > 0)
            {
                int m = free.Dequeue();
                int w = manPrefs[m][next[m]++];
                if (engaged[w] == -1) engaged[w] = m;
                else if (womanPrefs[w].IndexOf(m) < womanPrefs[w].IndexOf(engaged[w]))
                { free.Enqueue(engaged[w]); engaged[w] = m; }
                else free.Enqueue(m);
            }
            var result = new List<(int, int, double, double)>();
            for (int w = 0; w < nW; w++)
            {
                int m = engaged[w];
                result.Add((m, w, scoreM[m, w], scoreW[w, m]));
            }
            return result;
        }
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
                var remMen = _men.Where(u => !usedM.Contains(u.Id)).ToList();
                var remWomen = _women.Where(u => !usedW.Contains(u.Id)).ToList();
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
                    attrs[j] = cold ? 0.5 : rand.NextDouble();
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
            Assert.AreEqual(10, pairs.Count);
            // 检查无重复匹配
            Assert.AreEqual(10, pairs.Select(p => p.Man).Distinct().Count());
            Assert.AreEqual(10, pairs.Select(p => p.Woman).Distinct().Count());
            // 满意度在0~1区间
            Assert.IsTrue(pairs.All(p => p.ScoreM >= 0 && p.ScoreM <= 1));
            Assert.IsTrue(pairs.All(p => p.ScoreW >= 0 && p.ScoreW <= 1));
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
            Assert.AreEqual(8, pairs.Count);
            // 冷启动满意度应为0.5
            Assert.IsTrue(pairs.Any(p => Math.Abs(p.ScoreM - 0.5) < 1e-8 || Math.Abs(p.ScoreW - 0.5) < 1e-8));
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
            Assert.IsTrue(pairs.Count > 10); // 至少完成一轮
            // 检查匹配唯一性
            Assert.AreEqual(pairs.Select(p => p.Man).Distinct().Count(), pairs.Count);
            Assert.AreEqual(pairs.Select(p => p.Woman).Distinct().Count(), pairs.Count);
        }
    }
}
