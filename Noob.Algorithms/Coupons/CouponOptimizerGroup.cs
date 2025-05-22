// ***********************************************************************
// Assembly         : Noob.Algorithms
// Author           : noob
// Created          : 2025-05-21
//
// Last Modified By : noob
// Last Modified On : 2025-05-21
// ***********************************************************************
// <copyright file="CouponOptimizerGroup.cs" company="Noob.Algorithms">
//     Copyright (c) . All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Noob.Algorithms.Coupons
{
    /// <summary>
    /// Class CouponOptimizer.
    /// </summary>
    public partial class CouponOptimizer
    {
        /// <summary>
        /// Selects the best coupons group prune.
        /// 总入口方法融合
        /// </summary>
        /// <param name="order">The order.</param>
        /// <param name="availableCoupons">The available coupons.</param>
        /// <returns>CouponApplyResult.</returns>
        /// <exception cref="System.ArgumentNullException">order</exception>
        /// <exception cref="System.ArgumentNullException">availableCoupons</exception>
        public static CouponApplyResult SelectBestCouponsGroupPrune(Order order, List<Coupon> availableCoupons)
        {
            if (order == null) throw new ArgumentNullException(nameof(order));
            if (availableCoupons == null) throw new ArgumentNullException(nameof(availableCoupons));

            var itemsCopy = order.Items.Select(CloneOrderItem).ToList();
            var groups = GroupCoupons(itemsCopy, availableCoupons);

            return MergeAndPruneGlobal(groups, availableCoupons);
        }

        /// <summary>
        /// Groups the coupons.
        /// 券分组（品类/商品/品牌等）
        /// </summary>
        /// <param name="items">The items.</param>
        /// <param name="coupons">The coupons.</param>
        /// <returns>List&lt;CouponGroup&gt;.</returns>
        private static List<CouponGroup> GroupCoupons(List<OrderItem> items, List<Coupon> coupons)
        {
            var groups = new Dictionary<string, CouponGroup>();
            foreach (var coupon in coupons)
            {
                // 按品类/商品/自定义Key分组。此例可按适用商品/品类key分
                string key = coupon.ApplicableProductIds != null && coupon.ApplicableProductIds.Any()
                    ? string.Join(",", coupon.ApplicableProductIds.OrderBy(x => x))
                    : "ALL";
                if (!groups.ContainsKey(key))
                    groups[key] = new CouponGroup { GroupKey = key };
                groups[key].Coupons.Add(coupon);
            }

            foreach (var group in groups.Values)
            {
                if (group.GroupKey == "ALL")
                    group.Items = items.ToList();
                else
                    group.Items = items.Where(i => group.Coupons.Any(c => c.ApplicableProductIds != null && c.ApplicableProductIds.Contains(i.ProductId))).ToList();
            }
            return groups.Values.ToList();
        }

        /// <summary>
        /// Groups the optimal.
        /// 组内最优（贪心/DP）
        /// </summary>
        /// <param name="items">The items.</param>
        /// <param name="coupons">The coupons.</param>
        /// <returns>CouponApplyResult.</returns>
        private static CouponApplyResult GroupOptimal(List<OrderItem> items, List<Coupon> coupons)
        {
            // 简单用贪心，也可切到DP（券数少时）
            var used = new HashSet<int>();
            var applied = new List<Coupon>();
            decimal saved = 0;
            decimal current = items.Sum(i => i.Price * i.Quantity);

            while (true)
            {
                Coupon best = null;
                int bestIdx = -1;
                decimal maxSave = 0;
                for (int i = 0; i < coupons.Count; i++)
                {
                    if (used.Contains(i)) continue;
                    var coupon = coupons[i];
                    if (current < coupon.Threshold) continue;

                    decimal after = current;
                    if (coupon.Type == CouponType.Cash)
                        after = Math.Max(0, current - coupon.Amount);
                    else if (coupon.Type == CouponType.Discount && coupon.DiscountRate > 0 && coupon.DiscountRate < 1)
                        after = current * coupon.DiscountRate;
                    decimal save = current - after;
                    if (save > maxSave)
                    {
                        maxSave = save;
                        best = coupon;
                        bestIdx = i;
                    }
                }
                if (best != null && maxSave > 0)
                {
                    applied.Add(best);
                    used.Add(bestIdx);
                    if (best.Type == CouponType.Cash)
                        current = Math.Max(0, current - best.Amount);
                    else if (best.Type == CouponType.Discount && best.DiscountRate > 0 && best.DiscountRate < 1)
                        current = current * best.DiscountRate;
                    saved += maxSave;
                }
                else
                {
                    break;
                }
            }

            return new CouponApplyResult
            {
                PayableAmount = current,
                SavedAmount = saved,
                AppliedCoupons = applied
            };
        }
        /// <summary>
        /// Merges the and prune global.
        /// 全局归并穷举（兼容全场券/分组券/互斥等）
        /// </summary>
        /// <param name="groups">The groups.</param>
        /// <param name="allCoupons">All coupons.</param>
        /// <returns>CouponApplyResult.</returns>
        private static CouponApplyResult MergeAndPruneGlobal(List<CouponGroup> groups, List<Coupon> allCoupons)
        {
            // 单独抽全场券/平台券
            var globalCoupons = allCoupons
                .Where(c => c.ApplicableProductIds == null || c.ApplicableProductIds.Count == 0)
                .ToList();

            // 先算每个分组最优
            var groupResults = groups
                .Where(g => g.GroupKey != "ALL")
                .Select(g => GroupOptimal(g.Items, g.Coupons))
                .ToList();

            // 归并方案1：仅用各组券（不叠加全场券）
            var basePay = groupResults.Sum(r => r.PayableAmount);
            var baseCoupons = groupResults.SelectMany(r => r.AppliedCoupons).ToList();
            decimal baseSaved = groupResults.Sum(r => r.SavedAmount);

            // 归并方案2：尝试将全场券加入所有商品（与所有组券叠加/互斥，需业务规则控制）
            CouponApplyResult bestResult = new()
            {
                PayableAmount = basePay,
                SavedAmount = baseSaved,
                AppliedCoupons = new List<Coupon>(baseCoupons)
            };

            // 穷举全场券组合（如允许叠加多张，或互斥，只允许一张）
            int n = globalCoupons.Count;
            for (int state = 1; state < (1 << n); state++)
            {
                var selectedGlobals = new List<Coupon>();
                for (int i = 0; i < n; i++)
                {
                    if ((state & (1 << i)) != 0)
                        selectedGlobals.Add(globalCoupons[i]);
                }

                // 全场券直接作用于全订单金额
                decimal totalPay = basePay;
                decimal totalSaved = baseSaved;
                var allApplied = new List<Coupon>(baseCoupons);

                // 全场券叠加（可扩展复杂规则，比如最多一张，互斥，顺序优先级）
                decimal after = totalPay;
                foreach (var coupon in selectedGlobals)
                {
                    if (after < coupon.Threshold) continue; // 门槛
                    if (coupon.Type == CouponType.Cash)
                        after = Math.Max(0, after - coupon.Amount);
                    else if (coupon.Type == CouponType.Discount && coupon.DiscountRate > 0 && coupon.DiscountRate < 1)
                        after = after * coupon.DiscountRate;
                    allApplied.Add(coupon);
                }
                decimal save = totalPay - after;
                if (after < bestResult.PayableAmount)
                {
                    bestResult.PayableAmount = after;
                    bestResult.SavedAmount = baseSaved + save;
                    bestResult.AppliedCoupons = new List<Coupon>(allApplied);
                }
            }

            return bestResult;
        }


    }
}
