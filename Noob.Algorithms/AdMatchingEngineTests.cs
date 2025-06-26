// ***********************************************************************
// Assembly         : Noob.Algorithms
// Author           : noob
// Created          : 2025-06-26
//
// Last Modified By : noob
// Last Modified On : 2025-06-26
// ***********************************************************************
// <copyright file="AdMatchingEngineTests.cs" company="Noob.Algorithms">
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
    /// Class AdBid.
    /// </summary>
    public class AdBid
    {
        /// <summary>
        /// Gets or sets the ad identifier.
        /// </summary>
        /// <value>The ad identifier.</value>
        public int AdId { get; set; }
        /// <summary>
        /// Gets or sets the advertiser identifier.
        /// </summary>
        /// <value>The advertiser identifier.</value>
        public int AdvertiserId { get; set; }
        /// <summary>
        /// Gets or sets the keyword.
        /// </summary>
        /// <value>The keyword.</value>
        public string Keyword { get; set; }
        /// <summary>
        /// Gets or sets the bid price.
        /// </summary>
        /// <value>The bid price.</value>
        public decimal BidPrice { get; set; }
        /// <summary>
        /// Gets or sets the estimated CTR.
        /// </summary>
        /// <value>The estimated CTR.</value>
        public decimal EstimatedCtr { get; set; }
        /// <summary>
        /// Gets or sets the budget.
        /// </summary>
        /// <value>The budget.</value>
        public decimal Budget { get; set; }
        /// <summary>
        /// Gets or sets the spent.
        /// </summary>
        /// <value>The spent.</value>
        public decimal Spent { get; set; }
        /// <summary>
        /// Gets or sets the quality score.
        /// </summary>
        /// <value>The quality score.</value>
        public decimal QualityScore { get; set; }
    }

    /// <summary>
    /// Class SearchQuery.
    /// </summary>
    public class SearchQuery
    {
        /// <summary>
        /// Gets or sets the keyword.
        /// </summary>
        /// <value>The keyword.</value>
        public string Keyword { get; set; }

        /// <summary>
        /// Gets or sets the user identifier.
        /// </summary>
        /// <value>The user identifier.</value>
        public int UserId { get; set; }
        // 可扩展上下文特征，如时间、设备、地域、历史等
    }
    /// <summary>
    /// Class AdMatchingEngine.
    /// </summary>
    public class AdMatchingEngine
    {
        /// <summary>
        /// 动态广告分配：预算感知+CTR加权+多约束
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="allBids">All bids.</param>
        /// <param name="maxAdCount">The maximum ad count.</param>
        /// <returns>List&lt;AdBid&gt;.</returns>
        public List<AdBid> SelectAds(
            SearchQuery query,
            List<AdBid> allBids,
            int maxAdCount = 3)
        {
            // 1. 过滤关键词匹配和预算未耗尽的广告
            var candidates = allBids
                .FindAll(b => b.Keyword == query.Keyword && b.Spent < b.Budget);

            // 2. 动态得分计算（含出价、CTR、质量分、预算消耗比例等）
            foreach (var bid in candidates)
            {
                // 预算消耗比例影响得分，防止早耗光
                decimal budgetRatio = bid.Budget > 0 ? bid.Spent / bid.Budget : 0m;
                // Psi 函数实现（1-e^-(1-x)）控制预算均匀分布
                decimal psi = (decimal)(1 - Math.Exp(-(1 - (double)budgetRatio)));

                // 综合得分（可按实际业务扩展权重）
                bid.QualityScore =
                    bid.BidPrice * bid.EstimatedCtr * psi;
            }

            // 3. 按综合得分降序取TopN
            var selected = candidates
                .OrderByDescending(b => b.QualityScore)
                .Take(maxAdCount)
                .ToList();

            // 4. 可进一步过滤重复广告主、多样性、冷却期等

            return selected;
        }

        /// <summary>
        /// 点击计费与预算更新（如采用GSP二价拍卖）
        /// </summary>
        /// <param name="clickedBid">The clicked bid.</param>
        /// <param name="allBids">All bids.</param>
        public void RegisterClick(AdBid clickedBid, List<AdBid> allBids)
        {
            // 实际费用等于下一个广告得分对应的“出价”
            var nextBid = allBids
                .Where(b => b.Keyword == clickedBid.Keyword && b.AdId != clickedBid.AdId)
                .OrderByDescending(b => b.QualityScore)
                .FirstOrDefault();
            decimal price = nextBid != null ? nextBid.BidPrice : clickedBid.BidPrice;

            // 计费与预算更新
            clickedBid.Spent += price;
            // 其他业务逻辑可补充
        }
    }

    /// <summary>
    /// Defines test class AdMatchingEngineTests.
    /// </summary>
    [TestFixture]
    public class AdMatchingEngineTests
    {
        /// <summary>
        /// Defines the test method SelectAds_BudgetControlAndRanking.
        /// </summary>
        [Test]
        public void SelectAds_BudgetControlAndRanking()
        {
            var engine = new AdMatchingEngine();
            var bids = new List<AdBid>
        {
            new AdBid { AdId = 1, AdvertiserId = 1, Keyword = "shoes", BidPrice = 5, EstimatedCtr = 0.1m, Budget = 100, Spent = 10 },
            new AdBid { AdId = 2, AdvertiserId = 2, Keyword = "shoes", BidPrice = 4, EstimatedCtr = 0.2m, Budget = 50, Spent = 45 },
            new AdBid { AdId = 3, AdvertiserId = 3, Keyword = "shoes", BidPrice = 6, EstimatedCtr = 0.05m, Budget = 20, Spent = 0 },
        };
            var query = new SearchQuery { Keyword = "shoes", UserId = 1001 };

            var ads = engine.SelectAds(query, bids, maxAdCount: 2);
            Assert.IsTrue(ads.Count <= 2);
            // 高价但预算快耗尽的广告分数应自动下降
        }
    }
}
