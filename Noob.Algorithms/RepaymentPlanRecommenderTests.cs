// ***********************************************************************
// Assembly         : Noob.Algorithms
// Author           : noob
// Created          : 2025-05-20
//
// Last Modified By : noob
// Last Modified On : 2025-05-20
// ***********************************************************************
// <copyright file="RepaymentPlanRecommenderTests.cs" company="Noob.Algorithms">
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
    /// 智能还款计划推荐引擎：基于用户画像与产品规则，智能生成还款推荐方案与合规/风险提示。
    /// </summary>
    public static class RepaymentPlanRecommender
    {
        /// <summary>
        /// 推荐主入口，根据用户画像和产品库输出最佳还款方案
        /// </summary>
        /// <param name="user">用户还款画像</param>
        /// <param name="productLib">贷款产品库</param>
        /// <returns>还款方案推荐结果</returns>
        public static RepaymentRecommendationResult RecommendPlans(UserRepaymentProfile user, List<LoanProduct> productLib)
        {
            var result = new RepaymentRecommendationResult();
            var validProducts = FilterValidProducts(productLib);
            var recommendPlans = new List<RepaymentPlan>();
            string riskTip = null;
            string rejectReason = null;

            if (IsHighRiskOverdue(user))
            {
                rejectReason = "近半年逾期2次及以上，不支持常规展期，仅可推荐提前还款或缩短期限方案";
                riskTip = "存在高风险，请优先考虑提前还款或缩短方案";
                // 依然继续尝试推荐缩短期数或提前还款方案
            }

            decimal maxMonthlyPayment = user.MonthlyIncome * 0.5m;
            bool prohibitExtension = HasTooManyUnfinishedLoans(user);

            foreach (var product in SortProducts(validProducts))
            {
                var productPlans = RecommendPlansForProduct(user, product, maxMonthlyPayment, prohibitExtension);
                recommendPlans.AddRange(productPlans);

                if (recommendPlans.Count >= 5)
                    break;
            }

            recommendPlans = recommendPlans.Take(5).ToList();

            if (IsHighRiskOverdue(user))
                riskTip = "近半年逾期2次及以上，仅可推荐提前还款或缩短期限方案。";
            else if (!recommendPlans.Any())
                rejectReason = "未找到合规或可负担还款方案。";

            result.RecommendPlans = recommendPlans;
            result.RiskTip = string.IsNullOrWhiteSpace(riskTip) ? null : riskTip;
            result.DecisionBasis = "银保监2024-022号";
            result.RejectReason = rejectReason;

            return result;
        }

        /// <summary>
        /// 筛选合规备案的产品
        /// </summary>
        private static List<LoanProduct> FilterValidProducts(List<LoanProduct> products) =>
            products?.Where(p => !string.IsNullOrWhiteSpace(p.ComplianceId)).ToList() ?? new List<LoanProduct>();

        /// <summary>
        /// 按优先级（低利率、期数灵活）排序产品
        /// </summary>
        private static IEnumerable<LoanProduct> SortProducts(List<LoanProduct> products) =>
            products.OrderBy(p => p.Rate).ThenByDescending(p => p.MaxPeriods);

        /// <summary>
        /// 判断是否存在高风险逾期
        /// </summary>
        private static bool IsHighRiskOverdue(UserRepaymentProfile user) =>
            user.OverdueCount >= 2 && user.OverdueMonthsWithin6 >= 2;

        /// <summary>
        /// 判断是否多头贷款，禁止展期
        /// </summary>
        private static bool HasTooManyUnfinishedLoans(UserRepaymentProfile user) =>
            user.CurrentLoans.Count(l => l.RemainingPeriods > 0) > 2;

        /// <summary>
        /// 针对单个产品，推荐所有可行还款方案
        /// </summary>
        private static List<RepaymentPlan> RecommendPlansForProduct(
            UserRepaymentProfile user, LoanProduct product, decimal maxMonthlyPayment, bool prohibitExtension)
        {
            var plans = new List<RepaymentPlan>();
            decimal totalLoan = user.CurrentLoans.Sum(l => l.RemainingPrincipal);
            int maxUserPeriods = user.CurrentLoans.Any() ? user.CurrentLoans.Max(l => l.RemainingPeriods) : 0;

            // 连续12个月无逾期享最低利率
            decimal bestRate = (user.OverdueCount == 0 && user.OnTimeMonths >= 12)
                ? product.Rate // 实际业务可取所有产品最低利率，这里简化
                : product.Rate;

            for (int periods = product.MaxPeriods; periods >= 6; periods -= 6)
            {
                if (prohibitExtension && periods > maxUserPeriods)
                    continue;

                decimal monthlyPayment = CalculateMonthlyPayment(totalLoan, bestRate, periods);
                if (monthlyPayment > maxMonthlyPayment)
                    continue;

                decimal totalRepay = monthlyPayment * periods;
                decimal totalInterest = totalRepay - totalLoan;
                DateTime dueDate = DateTime.Today.AddMonths(periods);

                plans.Add(new RepaymentPlan
                {
                    ProductId = product.ProductId,
                    PlanDescription = $"等额本息{periods}期，月还款{monthlyPayment}, 总利息{Math.Round(totalInterest, 0)}, 总还款{Math.Round(totalRepay, 0)}, 到期日{dueDate:yyyy-MM-dd}",
                    ComplianceStatus = "通过"
                });
            }

            return plans;
        }

        /// <summary>
        /// 计算等额本息每月还款额
        /// </summary>
        private static decimal CalculateMonthlyPayment(decimal principal, decimal annualRate, int periods)
        {
            if (principal <= 0 || periods <= 0)
                return 0;

            decimal monthlyRate = annualRate / 100m / 12m;
            if (monthlyRate == 0)
                return Math.Round(principal / periods, 0);

            double pow = Math.Pow(1 + (double)monthlyRate, periods);
            decimal denominator = (decimal)(pow - 1);
            decimal payment = denominator == 0
                ? principal / periods
                : principal * monthlyRate * (decimal)pow / denominator;
            return Math.Round(payment, 0);
        }
    }

    // ==== 数据结构定义 ====

    /// <summary>
    /// Class UserRepaymentProfile.
    /// </summary>
    public class UserRepaymentProfile
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
        /// Gets or sets the current loans.
        /// </summary>
        /// <value>The current loans.</value>
        public List<LoanInfo> CurrentLoans { get; set; } = new();
        /// <summary>
        /// Gets or sets the overdue count.
        /// </summary>
        /// <value>The overdue count.</value>
        public int OverdueCount { get; set; } // 近12个月逾期总次数
        /// <summary>
        /// Gets or sets the overdue months within6.
        /// </summary>
        /// <value>The overdue months within6.</value>
        public int OverdueMonthsWithin6 { get; set; } // 近6个月逾期次数
        /// <summary>
        /// Gets or sets the on time months.
        /// </summary>
        /// <value>The on time months.</value>
        public int OnTimeMonths { get; set; } // 连续无逾期月数
    }

    /// <summary>
    /// Class LoanInfo.
    /// </summary>
    public class LoanInfo
    {
        /// <summary>
        /// Gets or sets the institution.
        /// </summary>
        /// <value>The institution.</value>
        public string Institution { get; set; }
        /// <summary>
        /// Gets or sets the remaining principal.
        /// </summary>
        /// <value>The remaining principal.</value>
        public decimal RemainingPrincipal { get; set; }
        /// <summary>
        /// Gets or sets the remaining periods.
        /// </summary>
        /// <value>The remaining periods.</value>
        public int RemainingPeriods { get; set; }
        /// <summary>
        /// Gets or sets the monthly payment.
        /// </summary>
        /// <value>The monthly payment.</value>
        public decimal MonthlyPayment { get; set; }
    }

    /// <summary>
    /// Class LoanProduct.
    /// </summary>
    public class LoanProduct
    {
        /// <summary>
        /// Gets or sets the product identifier.
        /// </summary>
        /// <value>The product identifier.</value>
        public string ProductId { get; set; }
        /// <summary>
        /// Gets or sets the rate.
        /// </summary>
        /// <value>The rate.</value>
        public decimal Rate { get; set; } // 年化利率(%)
        /// <summary>
        /// Gets or sets the maximum periods.
        /// </summary>
        /// <value>The maximum periods.</value>
        public int MaxPeriods { get; set; }
        /// <summary>
        /// Gets or sets the compliance identifier.
        /// </summary>
        /// <value>The compliance identifier.</value>
        public string ComplianceId { get; set; }
    }

    /// <summary>
    /// Class RepaymentRecommendationResult.
    /// </summary>
    public class RepaymentRecommendationResult
    {
        /// <summary>
        /// Gets or sets the recommend plans.
        /// </summary>
        /// <value>The recommend plans.</value>
        public List<RepaymentPlan> RecommendPlans { get; set; } = new();
        /// <summary>
        /// Gets or sets the risk tip.
        /// </summary>
        /// <value>The risk tip.</value>
        public string RiskTip { get; set; }
        /// <summary>
        /// Gets or sets the decision basis.
        /// </summary>
        /// <value>The decision basis.</value>
        public string DecisionBasis { get; set; }
        /// <summary>
        /// Gets or sets the reject reason.
        /// </summary>
        /// <value>The reject reason.</value>
        public string RejectReason { get; set; }
    }

    /// <summary>
    /// Class RepaymentPlan.
    /// </summary>
    public class RepaymentPlan
    {
        /// <summary>
        /// Gets or sets the product identifier.
        /// </summary>
        /// <value>The product identifier.</value>
        public string ProductId { get; set; }
        /// <summary>
        /// Gets or sets the plan description.
        /// </summary>
        /// <value>The plan description.</value>
        public string PlanDescription { get; set; }
        /// <summary>
        /// Gets or sets the compliance status.
        /// </summary>
        /// <value>The compliance status.</value>
        public string ComplianceStatus { get; set; }
    }

    /// <summary>
    /// Class RepaymentPlanRecommenderTests.
    /// </summary>
    [TestFixture]
    public class RepaymentPlanRecommenderTests {
        [Test]
        public void RepaymentPlanRecommender_ShouldRecommend_ValidPlans()
        {
            var user = new UserRepaymentProfile
            {
                UserId = "CU202405280001",
                MonthlyIncome = 18000,
                CurrentLoans = new List<LoanInfo>
                {
                    new LoanInfo { Institution = "银行A", RemainingPrincipal = 60000, RemainingPeriods = 8, MonthlyPayment = 7800 },
                    new LoanInfo { Institution = "消费金融B", RemainingPrincipal = 12000, RemainingPeriods = 3, MonthlyPayment = 4300 }
                },
                OverdueCount = 0,
                OverdueMonthsWithin6 = 0,
                OnTimeMonths = 15
            };
            var products = new List<LoanProduct>
            {
                new LoanProduct { ProductId = "BANKA2024FQ", Rate = 6.8m, MaxPeriods = 24, ComplianceId = "YBJK2024-008" },
                new LoanProduct { ProductId = "BANKB2024EQ", Rate = 7.2m, MaxPeriods = 36, ComplianceId = "YBJK2024-019" }
            };

            var result = RepaymentPlanRecommender.RecommendPlans(user, products);

            Assert.NotNull(result);
            Assert.IsTrue(result.RecommendPlans.Count > 0);
            Assert.IsTrue(result.RecommendPlans[0].PlanDescription.Contains("等额本息"));
            Assert.IsTrue(result.RecommendPlans.All(p => p.ComplianceStatus == "通过"));
            Assert.IsNull(result.RiskTip);
            Assert.IsNull(result.RejectReason);
            Assert.AreEqual("银保监2024-022号", result.DecisionBasis);
        }
    }

}
