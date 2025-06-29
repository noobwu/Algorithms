using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Noob.Algorithms
{
    /// <summary>
    /// “不完全匹配”/“最大匹配”
    /// </summary>
    public class GaleShapleyUnbalancedMatcher
    {
        /// <summary>
        /// Enum Role
        /// </summary>
        public enum Role { Proposer, Receiver }

        /// <summary>
        /// 不等人数稳定匹配（最大稳定匹配，主导方提案）。
        /// 主导方与被动方任意数量，允许不可接受（分数≤阈值）直接单身。
        /// </summary>
        /// <param name="proposerCount">主导方数量</param>
        /// <param name="receiverCount">被动方数量</param>
        /// <param name="proposerScore">主导方对被动方的分数函数</param>
        /// <param name="receiverScore">被动方对主导方的分数函数</param>
        /// <param name="acceptanceThreshold">分数大于此值才视为可接受，默认0</param>
        /// <returns>主导方为主输出，(proposer, receiver, proposerScore, receiverScore)。单身用receiver == -1。</returns>
        public static List<(int proposer, int receiver, double proposerScore, double receiverScore)> StableMatchUnbalanced(
            int proposerCount,
            int receiverCount,
            Func<int, int, double> proposerScore,
            Func<int, int, double> receiverScore,
            double acceptanceThreshold = 0.0
        )
        {
            if (proposerCount < 0 || receiverCount < 0)
                throw new ArgumentException("参与者数量不能为负");
            if (proposerScore == null || receiverScore == null)
                throw new ArgumentNullException("评分函数不能为空");

            const int Unmatched = -1;

            // Step 1: 构建偏好表，仅纳入可接受对象
            var proposerPrefs = new List<List<int>>(proposerCount);
            for (int i = 0; i < proposerCount; i++)
            {
                var prefs = Enumerable.Range(0, receiverCount)
                    .Where(j => proposerScore(i, j) > acceptanceThreshold)
                    .OrderByDescending(j => proposerScore(i, j))
                    .ToList();
                proposerPrefs.Add(prefs);
            }

            var receiverPrefs = new List<List<int>>(receiverCount);
            for (int j = 0; j < receiverCount; j++)
            {
                var prefs = Enumerable.Range(0, proposerCount)
                    .Where(i => receiverScore(j, i) > acceptanceThreshold)
                    .OrderByDescending(i => receiverScore(j, i))
                    .ToList();
                receiverPrefs.Add(prefs);
            }

            // Step 2: 初始化配对状态
            var engaged = Enumerable.Repeat(Unmatched, receiverCount).ToArray(); // receiver -> proposer
            var proposerNext = new int[proposerCount]; // 主导方下一个要追求的receiver序号
            var free = new Queue<int>(Enumerable.Range(0, proposerCount)
                .Where(p => proposerPrefs[p].Count > 0)); // 只把有偏好对象的 proposer 入队

            // Step 3: GS主循环
            while (free.Count > 0)
            {
                int p = free.Dequeue();
                while (proposerNext[p] < proposerPrefs[p].Count)
                {
                    int r = proposerPrefs[p][proposerNext[p]++];
                    // 被动方不接受该 proposer 也视为失败（即使 proposerScore > threshold）
                    if (!receiverPrefs[r].Contains(p))
                        continue;

                    if (engaged[r] == Unmatched)
                    {
                        engaged[r] = p;
                        break;
                    }
                    else
                    {
                        int currentP = engaged[r];
                        // 被动方更喜欢新 proposer
                        if (receiverPrefs[r].IndexOf(p) < receiverPrefs[r].IndexOf(currentP))
                        {
                            engaged[r] = p;
                            free.Enqueue(currentP);
                            break;
                        }
                    }
                }
                // 没有可追求对象或全部被拒，则 p 单身
            }

            // Step 4: 输出（主导方为主，单身者 receiver == -1，分数Clamp[0,1]）
            var result = new List<(int, int, double, double)>();
            var proposerMatched = new bool[proposerCount];
            for (int r = 0; r < receiverCount; r++)
            {
                int p = engaged[r];
                if (p != Unmatched)
                {
                    proposerMatched[p] = true;
                    double sP = Clamp(proposerScore(p, r));
                    double sR = Clamp(receiverScore(r, p));
                    result.Add((p, r, sP, sR));
                }
            }
            for (int p = 0; p < proposerCount; p++)
            {
                if (!proposerMatched[p])
                    result.Add((p, Unmatched, 0, 0));
            }
            return result;
        }

        /// <summary>
        /// Clamps the specified v.
        /// </summary>
        /// <param name="v">The v.</param>
        /// <returns>System.Double.</returns>
        private static double Clamp(double v) => Math.Max(0, Math.Min(1, v));
    }

    /// <summary>
    /// Defines test class GaleShapleyUnbalancedMatcherTests.
    /// </summary>
    [TestFixture]
    public class GaleShapleyUnbalancedMatcherTests
    {
        /// <summary>
        /// Defines the test method MenMoreThanWomen_PartialSingle.
        /// </summary>
        [Test]
        public void MenMoreThanWomen_PartialSingle()
        {
            int men = 4, women = 3;
            var matches = GaleShapleyUnbalancedMatcher.StableMatchUnbalanced(
                men, women,
                (m, w) => 1.0 - 0.1 * m,
                (w, m) => 1.0 - 0.05 * w
            );

            Assert.That(matches.Count, Is.EqualTo(4), "总输出条数应等于男生数量");
            Assert.That(matches.Count(x => x.receiver == -1), Is.EqualTo(1), "应有1名男生单身");
            Assert.That(matches.Count(x => x.receiver != -1), Is.EqualTo(3), "应有3对配对");
            Assert.That(matches.All(x => x.proposer >= 0), Is.True, "所有男编号应合法");
            Assert.That(matches.All(x => x.receiver < women || x.receiver == -1), Is.True, "女编号应在范围内或为-1");
        }

        /// <summary>
        /// Defines the test method WomenMoreThanMen_PartialSingle.
        /// </summary>
        [Test]
        public void WomenMoreThanMen_PartialSingle()
        {
            int men = 2, women = 5;
            var matches = GaleShapleyUnbalancedMatcher.StableMatchUnbalanced(
                men, women,
                (m, w) => 1.0,
                (w, m) => 1.0
            );

            Assert.That(matches.Count, Is.EqualTo(men), "总输出应等于男生数量");
            Assert.That(matches.Count(x => x.receiver == -1), Is.EqualTo(Math.Max(men - women, 0)), "单身男生数应为男-女（如为负则0）");
            Assert.That(matches.Count(x => x.receiver != -1), Is.EqualTo(Math.Min(men, women)), "配对男生数应为 min(男,女)");
            Assert.That(matches.All(x => (x.receiver < women && x.receiver >= 0) || x.receiver == -1), Is.True, "女编号应合法或为-1");
            Assert.That(matches.All(x => x.proposer < men && x.proposer >= 0), Is.True, "男编号应合法");
        }

        /// <summary>
        /// Defines the test method ZeroUsers_NoMatch.
        /// </summary>
        [Test]
        public void ZeroUsers_NoMatch()
        {
            var matches = GaleShapleyUnbalancedMatcher.StableMatchUnbalanced(
                0, 0, (i, j) => 0, (j, i) => 0);

            Assert.That(matches, Is.Empty, "零人数应无配对");
        }

        /// <summary>
        /// Defines the test method AllSingle_NoAcceptance.
        /// </summary>
        [Test]
        public void AllSingle_NoAcceptance()
        {
            int men = 3, women = 4;
            // 使所有人均不可接受（分数≤0）
            var matches = GaleShapleyUnbalancedMatcher.StableMatchUnbalanced(
                men, women,
                (m, w) => 0,        // 或 -100
                (w, m) => 0         // 或 -100
            );
            Assert.That(matches.All(x => x.receiver == -1), Is.True, "所有男生均应单身（receiver == -1）");
            Assert.That(matches.Count, Is.EqualTo(men), "总输出应等于男生数量");
        }

        /// <summary>
        /// Defines the test method EqualNumber_AllMatched.
        /// </summary>
        [Test]
        public void EqualNumber_AllMatched()
        {
            int men = 3, women = 3;
            var matches = GaleShapleyUnbalancedMatcher.StableMatchUnbalanced(
                men, women,
                (m, w) => 3 - Math.Abs(m - w),
                (w, m) => 3 - Math.Abs(w - m)
            );

            Assert.That(matches.Count(x => x.receiver != -1 && x.proposer != -1), Is.EqualTo(3), "应有3对配对");
            Assert.That(matches.All(x => x.receiver != -1 && x.proposer != -1), Is.True, "无单身");
        }

        /// <summary>
        /// Defines the test method ColdStart_ScoreZero.
        /// </summary>
        [Test]
        public void ColdStart_ScoreZero()
        {
            int men = 2, women = 2;
            var matches = GaleShapleyUnbalancedMatcher.StableMatchUnbalanced(
                men, women,
                (m, w) => m == 0 ? 0.0 : 1.0,
                (w, m) => w == 1 ? 0.0 : 1.0
            );

            Assert.That(matches.Count, Is.EqualTo(2));
            Assert.That(matches.Any(x => Math.Abs(x.proposerScore) < 1e-6 || Math.Abs(x.receiverScore) < 1e-6),
                Is.True, "至少有一对冷启动分数为0");
        }
    }
}
