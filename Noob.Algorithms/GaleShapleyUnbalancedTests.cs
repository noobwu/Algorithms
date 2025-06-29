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
        /// 支持两侧人数不等的稳定匹配算法
        /// </summary>
        /// <param name="proposers">主动方（如男）</param>
        /// <param name="receivers">被动方（如女）</param>
        /// <param name="proposerScores">主动方对被动方的分数矩阵</param>
        /// <param name="receiverScores">被动方对主动方的分数矩阵</param>
        /// <returns>返回List<(主动方索引, 被动方索引, 主动满意度, 被动满意度)>，未匹配用-1表示</returns>
        public static List<(int proposer, int receiver, double proposerScore, double receiverScore)> StableMatchUnbalanced(
            int proposerCount,
            int receiverCount,
            Func<int, int, double> proposerScore,
            Func<int, int, double> receiverScore)
        {
            var proposerPrefs = new List<List<int>>(proposerCount);
            var receiverPrefs = new List<List<int>>(receiverCount);
            // 构建偏好列表（降序）
            for (int i = 0; i < proposerCount; i++)
            {
                proposerPrefs.Add(Enumerable.Range(0, receiverCount)
                    .Where(j => proposerScore(i, j) > 0)   // 只加可接受者
                    .OrderByDescending(j => proposerScore(i, j))
                    .ToList());
            }

            for (int j = 0; j < receiverCount; j++)
            {
                receiverPrefs.Add(Enumerable.Range(0, proposerCount)
                    .OrderByDescending(i => receiverScore(j, i)).ToList());
            }

            var engaged = Enumerable.Repeat(-1, receiverCount).ToArray(); // -1 表示未匹配
            var proposerNext = new int[proposerCount]; // 每人下一个追求对象
            var free = new Queue<int>(Enumerable.Range(0, proposerCount)); // 未配对 proposer

            while (free.Count > 0)
            {
                int p = free.Dequeue();
                while (proposerNext[p] < proposerPrefs[p].Count)
                {
                    int r = proposerPrefs[p][proposerNext[p]++];
                    if (engaged[r] == -1)
                    {
                        engaged[r] = p;
                        break;
                    }
                    else
                    {
                        int currentP = engaged[r];
                        // 若新求婚者更受欢迎
                        if (receiverPrefs[r].IndexOf(p) < receiverPrefs[r].IndexOf(currentP))
                        {
                            engaged[r] = p;
                            free.Enqueue(currentP);
                            break;
                        }
                    }
                }
                // 若p已穷尽所有对象，自动单身
            }

            // 输出结果（仅输出已配对方）
            var result = new List<(int, int, double, double)>();
            var proposerMatched = new bool[proposerCount];
            for (int r = 0; r < receiverCount; r++)
            {
                int p = engaged[r];
                if (p != -1)
                {
                    proposerMatched[p] = true;
                    double sP = proposerScore(p, r);
                    double sR = receiverScore(r, p);
                    result.Add((p, r, sP, sR));
                }
            }
            // 如需输出单身
            for (int p = 0; p < proposerCount; p++)
                if (!proposerMatched[p])
                    result.Add((p, -1, 0, 0));
            return result;
        }
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
