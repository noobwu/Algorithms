// ***********************************************************************
// Assembly         : Noob.Algorithms
// Author           : noob
// Created          : 2025-05-20
//
// Last Modified By : noob
// Last Modified On : 2025-05-20
// ***********************************************************************
// <copyright file="CreditDecisionEngineTests.cs" company="Noob.Algorithms">
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
    /// 智能授信决策引擎，支持多机构额度智能拆分与最优匹配。
    /// </summary>
    public static class CreditDecisionEngine
    {
        /// <summary>
        /// The LPR rate
        /// </summary>
        private const decimal LprRate = 3.85m;
        /// <summary>
        /// The maximum annual rate
        /// </summary>
        private const decimal MaxAnnualRate = LprRate * 4; // 15.4%
        /// <summary>
        /// The maximum credit amount
        /// </summary>
        private const decimal MaxCreditAmount = 1_000_000m; // 100万

        /// <summary>
        /// 智能授信决策主入口
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="institutionRules">The institution rules.</param>
        /// <returns>CreditDecisionResult.</returns>
        public static CreditDecisionResult MakeCreditDecision(UserProfile user, List<InstitutionRule> institutionRules)
        {
            var result = new CreditDecisionResult();
            var riskTags = new List<string>();
            var adjustDetails = new List<string>();

            // 反欺诈评分拦截
            if (user.AntiFraudScore < 60)
            {
                result.RejectReason = "反欺诈评分过低，自动拒绝";
                result.RiskTags = new List<string> { "疑似高风险" };
                return result;
            }

            // 计算基础额度
            decimal baseAmount = Math.Min(user.MonthlyIncome * 36, user.NetAsset * 0.5m);
            decimal dynamicAmount = baseAmount;

            // 信用修正
            if (user.SocialSecurityMonths > 24)
            {
                dynamicAmount *= 1.15m;
                adjustDetails.Add("+15%（社保加成）");
                riskTags.Add("社保稳定");
            }
            if (user.OverdueCount > 3)
            {
                dynamicAmount *= 0.7m;
                adjustDetails.Add("-30%（多次逾期）");
                riskTags.Add("逾期历史");
            }
            else if (user.OverdueCount == 0)
            {
                riskTags.Add("优质客户");
            }
            // 多头借贷
            if (user.ActiveLoanPlatformCount >= 5)
            {
                dynamicAmount *= 0.5m;
                adjustDetails.Add("-50%（多头借贷）");
                riskTags.Add("多头借贷");
            }
            else if (user.ActiveLoanPlatformCount <= 2)
            {
                riskTags.Add("低负债率");
            }
            // 征信查询频繁，触发人工复核
            if (user.CreditInquiryCount > 6)
            {
                result.RejectReason = "征信查询频繁，需人工复核";
                riskTags.Add("征信频繁查询");
                result.RiskTags = riskTags;
                return result;
            }
            // 合规额度封顶
            if (dynamicAmount > MaxCreditAmount)
            {
                adjustDetails.Add("超限，按监管封顶调整");
                dynamicAmount = MaxCreditAmount;
            }

            // 利率策略区间
            string rateRange = "8.9%-12.3%";

            // === 智能额度拆分 ===
            decimal totalMatched;
            var institutionMatches = SplitCreditToInstitutions(dynamicAmount, institutionRules, out totalMatched);

            // 输出决策结果
            result.CreditDecision = new CreditDecision
            {
                InitialAmount = baseAmount,
                AdjustDetails = string.Join("，", adjustDetails),
                FinalAmount = dynamicAmount,
                RateRange = rateRange
            };
            result.InstitutionMatches = institutionMatches;
            result.RiskTags = riskTags;
            result.RejectReason = null;
            result.DecisionBasis = "央行征信规则第2025-038号";

            // 如拆分后实际额度低于终审额度，则附原因（可选增强）
            if (totalMatched < dynamicAmount)
            {
                result.RejectReason = "机构额度不足，无法满足全部授信";
            }

            return result;
        }

        /// <summary>
        /// 多机构额度智能拆分，优先低利率、高上限、审批优先级高的机构。
        /// </summary>
        /// <param name="finalAmount">The final amount.</param>
        /// <param name="institutions">The institutions.</param>
        /// <param name="totalMatchedAmount">The total matched amount.</param>
        /// <returns>List&lt;InstitutionMatch&gt;.</returns>
        public static List<InstitutionMatch> SplitCreditToInstitutions(
            decimal finalAmount, List<InstitutionRule> institutions, out decimal totalMatchedAmount)
        {
            var sortedInstitutions = institutions
                .OrderBy(i => i.Rate)
                .ThenByDescending(i => i.MaxAmount)
                .ThenByDescending(i => i.Priority)
                .ToList();

            var results = new List<InstitutionMatch>();
            decimal remain = finalAmount;

            foreach (var inst in sortedInstitutions)
            {
                if (remain <= 0)
                    break;
                decimal assign = Math.Min(remain, inst.MaxAmount);

                // 这里可加审批仿真/准入判断，如: if(!inst.CanApprove(user)) continue;

                if (assign > 0)
                {
                    results.Add(new InstitutionMatch
                    {
                        Name = inst.Name,
                        Amount = assign,
                        Term = inst.Term,
                        Rate = inst.Rate
                    });
                    remain -= assign;
                }
            }

            totalMatchedAmount = finalAmount - remain;
            return results;
        }
    }

    // ==== 数据结构定义 ====

    /// <summary>
    /// 用户画像（主要用于授信决策）
    /// </summary>
    public class UserProfile
    {
        /// <summary>
        /// Gets or sets the user identifier.
        /// </summary>
        /// <value>The user identifier.</value>
        public string UserId { get; set; }
        /// <summary>
        /// Gets or sets the monthly income.
        /// </summary>
        /// <value>The monthly income.</value>
        public decimal MonthlyIncome { get; set; }
        /// <summary>
        /// Gets or sets the net asset.
        /// </summary>
        /// <value>The net asset.</value>
        public decimal NetAsset { get; set; }
        /// <summary>
        /// Gets or sets the social security months.
        /// </summary>
        /// <value>The social security months.</value>
        public int SocialSecurityMonths { get; set; }
        /// <summary>
        /// Gets or sets the overdue count.
        /// </summary>
        /// <value>The overdue count.</value>
        public int OverdueCount { get; set; }
        /// <summary>
        /// Gets or sets the active loan platform count.
        /// </summary>
        /// <value>The active loan platform count.</value>
        public int ActiveLoanPlatformCount { get; set; }
        /// <summary>
        /// Gets or sets the credit inquiry count.
        /// </summary>
        /// <value>The credit inquiry count.</value>
        public int CreditInquiryCount { get; set; }
        /// <summary>
        /// Gets or sets the anti fraud score.
        /// </summary>
        /// <value>The anti fraud score.</value>
        public int AntiFraudScore { get; set; }
    }

    /// <summary>
    /// 授信决策主输出
    /// </summary>
    public class CreditDecisionResult
    {
        /// <summary>
        /// Gets or sets the credit decision.
        /// </summary>
        /// <value>The credit decision.</value>
        public CreditDecision CreditDecision { get; set; }
        /// <summary>
        /// Gets or sets the institution matches.
        /// </summary>
        /// <value>The institution matches.</value>
        public List<InstitutionMatch> InstitutionMatches { get; set; } = new();
        /// <summary>
        /// Gets or sets the risk tags.
        /// </summary>
        /// <value>The risk tags.</value>
        public List<string> RiskTags { get; set; } = new();
        /// <summary>
        /// Gets or sets the reject reason.
        /// </summary>
        /// <value>The reject reason.</value>
        public string RejectReason { get; set; }
        /// <summary>
        /// Gets or sets the decision basis.
        /// </summary>
        /// <value>The decision basis.</value>
        public string DecisionBasis { get; set; }
    }

    /// <summary>
    /// 授信额度与定价细节
    /// </summary>
    public class CreditDecision
    {
        /// <summary>
        /// Gets or sets the initial amount.
        /// </summary>
        /// <value>The initial amount.</value>
        public decimal InitialAmount { get; set; }
        /// <summary>
        /// Gets or sets the adjust details.
        /// </summary>
        /// <value>The adjust details.</value>
        public string AdjustDetails { get; set; }
        /// <summary>
        /// Gets or sets the final amount.
        /// </summary>
        /// <value>The final amount.</value>
        public decimal FinalAmount { get; set; }
        /// <summary>
        /// Gets or sets the rate range.
        /// </summary>
        /// <value>The rate range.</value>
        public string RateRange { get; set; }
    }

    /// <summary>
    /// 金融机构匹配结果
    /// </summary>
    public class InstitutionMatch
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }
        /// <summary>
        /// Gets or sets the amount.
        /// </summary>
        /// <value>The amount.</value>
        public decimal Amount { get; set; }
        /// <summary>
        /// Gets or sets the term.
        /// </summary>
        /// <value>The term.</value>
        public int Term { get; set; }
        /// <summary>
        /// Gets or sets the rate.
        /// </summary>
        /// <value>The rate.</value>
        public decimal Rate { get; set; }
    }

    /// <summary>
    /// 机构放款规则（每家产品可配置上限/期限/利率/优先级）
    /// </summary>
    public class InstitutionRule
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }
        /// <summary>
        /// Gets or sets the maximum amount.
        /// </summary>
        /// <value>The maximum amount.</value>
        public decimal MaxAmount { get; set; }
        /// <summary>
        /// Gets or sets the term.
        /// </summary>
        /// <value>The term.</value>
        public int Term { get; set; }
        /// <summary>
        /// Gets or sets the rate.
        /// </summary>
        /// <value>The rate.</value>
        public decimal Rate { get; set; }
        /// <summary>
        /// Gets or sets the priority.
        /// </summary>
        /// <value>The priority.</value>
        public int Priority { get; set; }
        // 可扩展：public Func<UserProfile, bool> CanApprove { get; set; }
    }

    /// <summary>
    /// Defines test class CreditDecisionEngineTests.
    /// </summary>
    [TestFixture]
    public class CreditDecisionEngineTests
    {
        [Test]
        public void MakeCreditDecision_MultiInstitution_Split_ShouldBeCorrect()
        {
            var institutionRules = new List<InstitutionRule>
    {
        new InstitutionRule { Name = "机构A", MaxAmount = 500_000m, Term = 36, Rate = 8.9m, Priority = 2 },
        new InstitutionRule { Name = "机构B", MaxAmount = 300_000m, Term = 24, Rate = 9.1m, Priority = 1 },
        new InstitutionRule { Name = "机构C", MaxAmount = 600_000m, Term = 12, Rate = 10.5m, Priority = 0 }
    };

            var user = new UserProfile
            {
                UserId = "CU202505201234",
                MonthlyIncome = 25000,
                NetAsset = 800000,
                SocialSecurityMonths = 38,
                OverdueCount = 0,
                ActiveLoanPlatformCount = 2,  // <=2 保证有“低负债率”
                CreditInquiryCount = 2,
                AntiFraudScore = 78
            };

            var result = CreditDecisionEngine.MakeCreditDecision(user, institutionRules);

            Assert.NotNull(result);
            Assert.NotNull(result.CreditDecision);

            // 校验额度
            Assert.AreEqual(400_000m, result.CreditDecision.InitialAmount);// 40万
            Assert.That(result.CreditDecision.AdjustDetails, Does.Contain("+15%"));
            Assert.AreEqual(460_000m, result.CreditDecision.FinalAmount);// 40万 × 1.15 = 46万

            // 校验机构拆分
            Assert.AreEqual(1, result.InstitutionMatches.Count);
            Assert.AreEqual("机构A", result.InstitutionMatches[0].Name);
            Assert.AreEqual(460_000m, result.InstitutionMatches[0].Amount);

            // 校验利率区间
            Assert.AreEqual("8.9%-12.3%", result.CreditDecision.RateRange);

            // 校验风险标签
            CollectionAssert.Contains(result.RiskTags, "优质客户");
            CollectionAssert.Contains(result.RiskTags, "社保稳定");
            CollectionAssert.Contains(result.RiskTags, "低负债率");

            Assert.IsNull(result.RejectReason);
            Assert.AreEqual("央行征信规则第2025-038号", result.DecisionBasis);
        }
    }


}
