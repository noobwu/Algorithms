// ***********************************************************************
// Assembly         : Noob.Algorithms
// Author           : noob
// Created          : 2025-05-21
//
// Last Modified By : noob
// Last Modified On : 2025-05-21
// ***********************************************************************
// <copyright file="CouponOptimizer.cs" company="Noob.Algorithms">
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
    /// 优惠券自动优化器：计算订单最优优惠组合。
    /// </summary>
    public partial class CouponOptimizer
    {
        /// <summary>
        /// 券数<=此值用DP，否则用贪心
        /// </summary>
        private const int DPThreshold = 20;

        /// <summary>
        /// 自动选择最大优惠券组合（DP+贪心混合策略）
        /// </summary>
        /// <param name="order">The order.</param>
        /// <param name="availableCoupons">The available coupons.</param>
        /// <returns>CouponApplyResult.</returns>
        /// <exception cref="ArgumentNullException">order</exception>
        /// <exception cref="ArgumentNullException">availableCoupons</exception>
        public static CouponApplyResult SelectBestCouponsSmart(Order order, List<Coupon> availableCoupons)
        {
            if (order == null) throw new ArgumentNullException(nameof(order));
            if (availableCoupons == null) throw new ArgumentNullException(nameof(availableCoupons));

            // Step 1：先应用所有特价/兑换券
            var itemsCopy = order.Items.Select(CloneOrderItem).ToList();
            var appliedCoupons = new List<Coupon>();
            decimal preSaved = 0;

            foreach (var coupon in availableCoupons.Where(c => c.Type == CouponType.SpecialPrice || c.Type == CouponType.Gift).ToList())
            {
                ApplySingleCoupon(coupon, itemsCopy, ref preSaved);
                appliedCoupons.Add(coupon);
            }

            // Step 2：Cash/Discount券池
            var coreCoupons = availableCoupons
                .Where(c => (c.Type == CouponType.Cash || c.Type == CouponType.Discount) && c.IsStackable)
                .ToList();

            CouponApplyResult coreResult;

            if (coreCoupons.Count <= DPThreshold)
            {
                coreResult = SelectBestCouponsDPInternal(itemsCopy, coreCoupons);
            }
            else
            {
                coreResult = SelectBestCouponsGreedyInternal(itemsCopy, coreCoupons);
            }

            // 合并最终结果
            appliedCoupons.AddRange(coreResult.AppliedCoupons);
            return new CouponApplyResult
            {
                PayableAmount = coreResult.PayableAmount,
                SavedAmount = preSaved + coreResult.SavedAmount,
                AppliedCoupons = appliedCoupons
            };
        }

        /// <summary>
        /// DP求解Cash/Discount券最优组合（券数较少时,防止内存溢出）
        /// </summary>
        /// <param name="itemsCopy">The items copy.</param>
        /// <param name="coupons">The coupons.</param>
        /// <returns>CouponApplyResult.</returns>
        private static CouponApplyResult SelectBestCouponsDPInternal(List<OrderItem> itemsCopy, List<Coupon> coupons)
        {
            decimal currentAmount = itemsCopy.Sum(i => i.Price * i.Quantity);
            int n = coupons.Count;
            /*
            风险：
            - 当n = 20时，decimal[maxState] 数组占用内存：2 ^ 20 * 16 bytes ≈ 16MB
            */
            int maxState = 1 << n;//券数=20时消耗16MB内存，n=25时达512MB

            var minPay = new decimal[maxState];
            for (int i = 0; i < maxState; i++) minPay[i] = decimal.MaxValue;
            minPay[0] = currentAmount;

            var prevState = new int[maxState];
            var prevCoupon = new int[maxState];
            Array.Fill(prevState, -1);
            Array.Fill(prevCoupon, -1);

            // DP主循环
            for (int state = 0; state < maxState; state++)
            {
                if (minPay[state] == decimal.MaxValue) continue; // 不可达状态，跳过
                for (int i = 0; i < n; i++)
                {
                    /*
                     * 1 << i 表示一个只在第i位为1，其余位为0的二进制数。
                     * state & (1 << i) 结果为非0，只有在state的第i位本来就是1时才会成立。
                     */
                    if ((state & 1 << i) != 0) continue; // 已用该券
                    var coupon = coupons[i];
                    decimal now = minPay[state];
                    if (now < coupon.Threshold) continue;

                    decimal after = now;
                    if (coupon.Type == CouponType.Cash)
                        after = Math.Max(0, now - coupon.Amount);
                    else if (coupon.Type == CouponType.Discount && coupon.DiscountRate > 0 && coupon.DiscountRate < 1)
                        after = now * coupon.DiscountRate;

                    /*
                     * 1 << i 把第 i 位设为 1，其余为 0。
                     * state | (1 << i) 表示“在当前状态 state 的基础上，再把第 i 个选项标记为已用”，生成新状态。
                     */
                    int newState = state | 1 << i;
                    if (after < minPay[newState])
                    {
                        minPay[newState] = after;
                        prevState[newState] = state;
                        prevCoupon[newState] = i;
                    }
                }
            }

            // 回溯
            decimal minFinal = currentAmount;
            int bestS = 0;
            for (int s = 0; s < maxState; s++)
            {
                if (minPay[s] < minFinal)
                {
                    minFinal = minPay[s];
                    bestS = s;
                }
            }
            var bestList = new List<Coupon>();
            int state2 = bestS;
            HashSet<int> guard = new HashSet<int>(); // 防死循环回溯
            while (state2 != 0 && prevState[state2] != -1)
            {
                if (!guard.Add(state2)) break; // 若状态重复，断链防止死循环
                int cidx = prevCoupon[state2];
                bestList.Add(coupons[cidx]);
                state2 = prevState[state2];
            }
            bestList.Reverse();

            decimal totalSaved = currentAmount - minFinal;
            decimal payable = minFinal;
            return new CouponApplyResult
            {
                PayableAmount = payable,
                SavedAmount = totalSaved,
                AppliedCoupons = bestList
            };
        }


        /// <summary>
        /// 贪心法求解Cash/Discount券最优组合（券数较多时）
        /// </summary>
        /// <param name="itemsCopy">The items copy.</param>
        /// <param name="coupons">The coupons.</param>
        /// <returns>CouponApplyResult.</returns>
        private static CouponApplyResult SelectBestCouponsGreedyInternal(List<OrderItem> itemsCopy, List<Coupon> coupons)
        {
            var used = new HashSet<int>();
            var applied = new List<Coupon>();
            decimal saved = 0;
            decimal current = itemsCopy.Sum(i => i.Price * i.Quantity);

            int lastUsedCount = -1; // 死循环防护，监控used是否有增加

            while (used.Count != lastUsedCount)
            {
                lastUsedCount = used.Count;
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
        /// 贪心法自动选择最大优惠券组合（适合券数量较多、业务可容忍次优解时）。
        /// </summary>
        /// <param name="order">The order.</param>
        /// <param name="availableCoupons">The available coupons.</param>
        /// <returns>CouponApplyResult.</returns>
        /// <exception cref="ArgumentNullException">order</exception>
        /// <exception cref="ArgumentNullException">availableCoupons</exception>
        public static CouponApplyResult SelectBestCouponsGreedy(Order order, List<Coupon> availableCoupons)
        {
            if (order == null) throw new ArgumentNullException(nameof(order));
            if (availableCoupons == null) throw new ArgumentNullException(nameof(availableCoupons));

            var itemsCopy = order.Items.Select(CloneOrderItem).ToList();
            var validCoupons = availableCoupons.Where(c => c.IsValidFor(order)).ToList();
            var appliedCoupons = new List<Coupon>();
            decimal saved = 0;

            // Step 1: 批量应用Gift和SpecialPrice券（类型优先处理）
            foreach (var coupon in validCoupons.Where(c => c.Type == CouponType.SpecialPrice || c.Type == CouponType.Gift).ToList())
            {
                ApplySingleCoupon(coupon, itemsCopy, ref saved);
                appliedCoupons.Add(coupon);
                validCoupons.Remove(coupon);
            }

            // Step 2: 只选最优一张不可叠加券（非Gift/SpecialPrice）
            var nonStackable = validCoupons.Where(c => !c.IsStackable).ToList();
            Coupon bestNonStackable = null;
            decimal bestNonStackableSave = 0;
            foreach (var coupon in nonStackable)
            {
                decimal tempSaved = 0;
                var testItems = itemsCopy.Select(CloneOrderItem).ToList();
                ApplySingleCoupon(coupon, testItems, ref tempSaved);
                if (tempSaved > bestNonStackableSave)
                {
                    bestNonStackableSave = tempSaved;
                    bestNonStackable = coupon;
                }
            }
            if (bestNonStackable != null)
            {
                ApplySingleCoupon(bestNonStackable, itemsCopy, ref saved);
                appliedCoupons.Add(bestNonStackable);
                validCoupons.Remove(bestNonStackable);
            }

            // Step 3: 动态边际贡献贪心叠加券
            var stackable = validCoupons.Where(c => c.IsStackable).ToList();
            var used = new HashSet<int>();
            while (stackable.Any(c => !used.Contains(c.CouponId)))
            {
                Coupon best = null;
                decimal maxSave = 0;
                foreach (var coupon in stackable.Where(c => !used.Contains(c.CouponId)))
                {
                    decimal tempSaved = 0;
                    var testItems = itemsCopy.Select(CloneOrderItem).ToList();
                    ApplySingleCoupon(coupon, testItems, ref tempSaved);
                    if (tempSaved > maxSave)
                    {
                        maxSave = tempSaved;
                        best = coupon;
                    }
                }
                if (best != null && maxSave > 0)
                {
                    ApplySingleCoupon(best, itemsCopy, ref saved);
                    appliedCoupons.Add(best);
                    used.Add(best.CouponId);

                    // 如有券冲突互斥表，可此处过滤stackable池
                }
                else
                {
                    break;
                }
            }

            decimal payable = Math.Max(order.TotalAmount - saved, 0);
            return new CouponApplyResult
            {
                PayableAmount = payable,
                SavedAmount = saved,
                AppliedCoupons = appliedCoupons
            };
        }
        /// <summary>
        /// 应用单张优惠券到订单项集合。
        /// </summary>
        /// <param name="coupon">The coupon.</param>
        /// <param name="items">The items.</param>
        /// <param name="saved">The saved.</param>
        private static void ApplySingleCoupon(Coupon coupon, List<OrderItem> items, ref decimal saved)
        {
            switch (coupon.Type)
            {
                case CouponType.Gift:
                    ApplyGiftCoupons(new[] { coupon }, items, ref saved);
                    break;
                case CouponType.SpecialPrice:
                    ApplySpecialPriceCoupons(new[] { coupon }, items, ref saved);
                    break;
                case CouponType.Discount:
                    ApplyDiscountCoupons(new[] { coupon }, items, ref saved);
                    break;
                case CouponType.Cash:
                    ApplyCashCoupons(new[] { coupon }, items, ref saved);
                    break;
            }
        }

        /// <summary>
        /// 穷举组合，自动选择最大优惠券组合（最优解，适合券数量较少时）。
        /// </summary>
        /// <param name="order">The order.</param>
        /// <param name="availableCoupons">The available coupons.</param>
        /// <returns>CouponApplyResult.</returns>
        /// <exception cref="ArgumentNullException">order</exception>
        /// <exception cref="ArgumentNullException">availableCoupons</exception>
        public static CouponApplyResult SelectBestCoupons(Order order, List<Coupon> availableCoupons)
        {
            if (order == null) throw new ArgumentNullException(nameof(order));
            if (availableCoupons == null) throw new ArgumentNullException(nameof(availableCoupons));

            var validCoupons = availableCoupons.Where(c => c.IsValidFor(order)).ToList();
            var bestResult = new CouponApplyResult { SavedAmount = 0, PayableAmount = order.TotalAmount };

            // 分组
            var stackableCoupons = validCoupons.Where(c => c.IsStackable).ToList();
            var nonStackableCoupons = validCoupons.Where(c => !c.IsStackable).ToList();

            // 所有候选组合（非叠加券+叠加券，或只用叠加券）
            var allCandidates = BuildAllCouponCombinations(stackableCoupons, nonStackableCoupons);

            foreach (var coupons in allCandidates)
            {
                var distinctCoupons = coupons.Distinct().ToList();
                if (!distinctCoupons.Any()) continue;

                // 复制购物车（避免券应用副作用）
                var itemsCopy = order.Items.Select(CloneOrderItem).ToList();

                decimal saved = 0;
                ApplyGiftCoupons(distinctCoupons, itemsCopy, ref saved);
                ApplySpecialPriceCoupons(distinctCoupons, itemsCopy, ref saved);
                ApplyDiscountCoupons(distinctCoupons, itemsCopy, ref saved);
                ApplyCashCoupons(distinctCoupons, itemsCopy, ref saved);

                decimal payable = Math.Max(order.TotalAmount - saved, 0);

                if (IsBetter(payable, bestResult, distinctCoupons))
                {
                    bestResult.PayableAmount = payable;
                    bestResult.SavedAmount = saved;
                    bestResult.AppliedCoupons = distinctCoupons;
                }
            }
            return bestResult;
        }

        /// <summary>
        /// 组合生成所有券应用场景（叠加券+每个不可叠加券，或单纯叠加券）。
        /// </summary>
        /// <param name="stackableCoupons">The stackable coupons.</param>
        /// <param name="nonStackableCoupons">The non stackable coupons.</param>
        /// <returns>List&lt;List&lt;Coupon&gt;&gt;.</returns>
        private static List<List<Coupon>> BuildAllCouponCombinations(
            List<Coupon> stackableCoupons, List<Coupon> nonStackableCoupons)
        {
            var combinations = new List<List<Coupon>>();
            foreach (var single in nonStackableCoupons)
                combinations.Add(new List<Coupon> { single }.Concat(stackableCoupons).Distinct().ToList());
            if (stackableCoupons.Any())
                combinations.Add(stackableCoupons);
            return combinations;
        }

        /// <summary>
        /// 判断方案是否更优。
        /// </summary>
        /// <param name="payable">The payable.</param>
        /// <param name="currentBest">The current best.</param>
        /// <param name="coupons">The coupons.</param>
        /// <returns><c>true</c> if the specified payable is better; otherwise, <c>false</c>.</returns>
        private static bool IsBetter(decimal payable, CouponApplyResult currentBest, List<Coupon> coupons)
        {
            return payable < currentBest.PayableAmount ||
                   payable == currentBest.PayableAmount &&
                    coupons.Count > (currentBest.AppliedCoupons?.Count ?? 0);
        }
        /// <summary>
        /// 深复制订单项
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>OrderItem.</returns>
        private static OrderItem CloneOrderItem(OrderItem item)
        {
            return new OrderItem
            {
                ProductId = item.ProductId,
                Price = item.Price,
                Quantity = item.Quantity
            };
        }

        /// <summary>
        /// 处理Gift兑换券
        /// </summary>
        /// <param name="coupons">The coupons.</param>
        /// <param name="items">The items.</param>
        /// <param name="saved">The saved.</param>
        private static void ApplyGiftCoupons(IEnumerable<Coupon> coupons, List<OrderItem> items, ref decimal saved)
        {
            foreach (var coupon in coupons.Where(c => c.Type == CouponType.Gift))
            {
                foreach (var item in items)
                {
                    if (coupon.ApplicableProductIds != null &&
                        coupon.ApplicableProductIds.Contains(item.ProductId) &&
                        item.Quantity > 0)
                    {
                        int redeemQty = 1;
                        decimal redeemValue = Math.Min(item.Quantity, redeemQty) * item.Price;
                        saved += redeemValue;
                        item.Quantity -= redeemQty;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// 处理特价券
        /// </summary>
        /// <param name="coupons">The coupons.</param>
        /// <param name="items">The items.</param>
        /// <param name="saved">The saved.</param>
        private static void ApplySpecialPriceCoupons(IEnumerable<Coupon> coupons, List<OrderItem> items, ref decimal saved)
        {
            foreach (var coupon in coupons.Where(c => c.Type == CouponType.SpecialPrice))
            {
                var item = items.FirstOrDefault(i => coupon.ApplicableProductIds?.Contains(i.ProductId) ?? false);
                if (item != null && coupon.SpecialPrice.HasValue && item.Price > coupon.SpecialPrice.Value)
                {
                    saved += (item.Price - coupon.SpecialPrice.Value) * item.Quantity;
                    item.Price = coupon.SpecialPrice.Value;
                }
            }
        }

        /// <summary>
        /// 处理折扣券（同一商品单张生效，优先取最大折扣）
        /// </summary>
        /// <param name="coupons">The coupons.</param>
        /// <param name="items">The items.</param>
        /// <param name="saved">The saved.</param>
        private static void ApplyDiscountCoupons(IEnumerable<Coupon> coupons, List<OrderItem> items, ref decimal saved)
        {
            var discountCoupon = coupons.Where(c => c.Type == CouponType.Discount)
                                       .OrderBy(c => c.DiscountRate)
                                       .FirstOrDefault();
            if (discountCoupon != null && discountCoupon.DiscountRate > 0 && discountCoupon.DiscountRate < 1)
            {
                foreach (var item in items)
                {
                    if (discountCoupon.ApplicableProductIds == null ||
                        discountCoupon.ApplicableProductIds.Count == 0 ||
                        discountCoupon.ApplicableProductIds.Contains(item.ProductId))
                    {
                        decimal discountAmount = item.Price * item.Quantity * (1 - discountCoupon.DiscountRate);
                        saved += discountAmount;
                        item.Price *= discountCoupon.DiscountRate;
                    }
                }
            }
        }

        /// <summary>
        /// 处理现金券（满减/代金券）
        /// </summary>
        /// <param name="coupons">The coupons.</param>
        /// <param name="items">The items.</param>
        /// <param name="saved">The saved.</param>
        private static void ApplyCashCoupons(IEnumerable<Coupon> coupons, List<OrderItem> items, ref decimal saved)
        {
            foreach (var coupon in coupons.Where(c => c.Type == CouponType.Cash))
            {
                decimal amount = items
                    .Where(item => coupon.ApplicableProductIds == null || coupon.ApplicableProductIds.Count == 0 || coupon.ApplicableProductIds.Contains(item.ProductId))
                    .Sum(item => item.Price * item.Quantity);
                if (amount >= coupon.Threshold)
                {
                    saved += Math.Min(coupon.Amount, amount);
                }
            }
        }
    }
}
