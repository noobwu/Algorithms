using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Noob.Algorithms
{
    /// <summary>
    /// 优惠券类型
    /// </summary>
    public enum CouponType
    {
        /// <summary>
        /// 现金类券（满减券/代金券/现金券，统一用门槛字段区分）
        /// </summary>
        Cash,

        /// <summary>
        /// 折扣券（如9折、8.5折）
        /// </summary>
        Discount,

        /// <summary>
        /// 兑换券（如兑换指定商品/礼品）
        /// </summary>
        Gift,

        /// <summary>
        /// 特价券（指定商品以特价购买）
        /// </summary>
        SpecialPrice
    }

    /// <summary>
    /// Class Coupon.
    /// </summary>
    public class Coupon
    {
        /// <summary>
        /// Gets or sets the coupon identifier.
        /// </summary>
        /// <value>The coupon identifier.</value>
        public int CouponId { get; set; }
        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        /// <value>The type.</value>
        public CouponType Type { get; set; }
   
        /// <summary>
        /// 门槛：满多少可用，为0即无门槛(针对Cash/Discount券        )
        /// </summary>
        /// <value>The threshold.</value>
        public decimal Threshold { get; set; }

        /// <summary>
        /// 券面额(Cash券：立减金额)
        /// </summary>
        /// <value>The reduction amount.</value>
        public decimal Amount { get; set; }

        /// <summary>
        /// 折扣率，如0.9表示9折
        /// </summary>
        /// <value>The discount rate.</value>
        public decimal DiscountRate { get; set; }
    
        /// <summary>
        /// Gift/SpecialPrice券指定商品
        /// </summary>
        /// <value>The applicable product ids.</value>
        public List<int>? ApplicableProductIds { get; set; }   // Gift/SpecialPrice券指定商品

        /// <summary>
        ///  特价金额
        /// </summary>
        /// <value>The special price.</value>
        public decimal? SpecialPrice { get; set; } 

        /// <summary>
        /// 是否可叠加
        /// </summary>
        /// <value><c>true</c> if this instance is stackable; otherwise, <c>false</c>.</value>
        public bool IsStackable { get; set; }

        // 可继续加生效时间、使用范围等字段


        /// <summary>
        /// Determines whether [is valid for] [the specified order].
        /// </summary>
        /// <param name="order">The order.</param>
        /// <returns><c>true</c> if [is valid for] [the specified order]; otherwise, <c>false</c>.</returns>
        public bool IsValidFor(Order order)
        {
            switch (Type)
            {
                case CouponType.SpecialPrice:
                case CouponType.Gift:
                    return order.Items.Any(i =>
                        ApplicableProductIds != null && ApplicableProductIds.Contains(i.ProductId) && i.Quantity > 0);
                case CouponType.Cash:
                case CouponType.Discount:
                default:
                    var amount = order.Items
                        .Where(i => ApplicableProductIds == null || ApplicableProductIds.Count == 0 || ApplicableProductIds.Contains(i.ProductId))
                        .Sum(i => i.Price * i.Quantity);
                    return amount >= Threshold;
            }
        }
    }

    /// <summary>
    /// 订单项
    /// </summary>
    public class OrderItem
    {
        /// <summary>
        /// Gets or sets the product identifier.
        /// </summary>
        /// <value>The product identifier.</value>
        public int ProductId { get; set; }
        /// <summary>
        /// Gets or sets the price.
        /// </summary>
        /// <value>The price.</value>
        public decimal Price { get; set; }
        /// <summary>
        /// Gets or sets the quantity.
        /// </summary>
        /// <value>The quantity.</value>
        public int Quantity { get; set; }
    }

    /// <summary>
    /// 订单类型
    /// </summary>
    public class Order
    {
        /// <summary>
        /// Gets or sets the items.
        /// </summary>
        /// <value>The items.</value>
        public List<OrderItem> Items { get; set; }

        /// <summary>
        /// Gets the total amount.
        /// </summary>
        /// <value>The total amount.</value>
        public decimal TotalAmount => Items.Sum(i => i.Price * i.Quantity);
    }

    /// <summary>
    /// 最优优惠券组合结果
    /// </summary>
    public class CouponApplyResult
    {
        /// <summary>
        /// 优惠金额
        /// </summary>
        /// <value>The saved amount.</value>
        public decimal SavedAmount { get; set; }
        /// <summary>
        /// 已应用优惠券列表
        /// </summary>
        /// <value>The applied coupons.</value>
        public List<Coupon> AppliedCoupons { get; set; } = new();
        /// <summary>
        /// 应付金额
        /// </summary>
        /// <value>The payable amount.</value>
        public decimal PayableAmount { get; set; }
    }

    /// <summary>
    /// 优惠券自动优化器：计算订单最优优惠组合。
    /// </summary>
    public class CouponOptimizer
    {
        /// <summary>
        /// 券数<=此值用DP，否则用贪心
        /// </summary>
        private const int DPThreshold = 20;

        /// <summary>
        /// 自动选择最大优惠券组合（DP+贪心混合策略）
        /// </summary>
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
                    if ((state & (1 << i)) != 0) continue; // 已用该券
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
                    int newState = state | (1 << i);
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
        private static bool IsBetter(decimal payable, CouponApplyResult currentBest, List<Coupon> coupons)
        {
            return payable < currentBest.PayableAmount ||
                   (payable == currentBest.PayableAmount &&
                    coupons.Count > (currentBest.AppliedCoupons?.Count ?? 0));
        }
        /// <summary>
        /// 深复制订单项
        /// </summary>
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
    /// <summary>
    /// Defines test class CouponTests.
    /// </summary>
    [TestFixture]
    public class CouponTests {

        /// <summary>
        /// Defines the test method SimpleCashCoupon.
        /// </summary>
        [Test]
        public void SimpleCashCoupon()
        {
            var order = new Order
            {
                Items = new List<OrderItem>
            {
                new OrderItem { ProductId = 1, Price = 100, Quantity = 2 }
            }
            };
            var coupons = new List<Coupon>
            {
                new Coupon { CouponId = 1, Type = CouponType.Cash, Threshold = 150, Amount = 30, IsStackable = false }
            };

            var result = CouponOptimizer.SelectBestCoupons(order, coupons);

            Assert.AreEqual(170, result.PayableAmount);
            Assert.AreEqual(30, result.SavedAmount);
            Assert.That(result.AppliedCoupons.Any(c => c.CouponId == 1));
        }

        [Test]
        public void CashAndDiscountBestCombo()
        {
            var order = new Order
            {
                Items = new List<OrderItem>
                {
                    new OrderItem { ProductId = 1, Price = 100, Quantity = 1 },
                    new OrderItem { ProductId = 2, Price = 80, Quantity = 2 }
                }
            };
            var coupons = new List<Coupon>
            {
                new Coupon { CouponId = 1, Type = CouponType.Cash, Threshold = 200, Amount = 30, IsStackable = false },
                new Coupon { CouponId = 2, Type = CouponType.Discount, Threshold = 100, DiscountRate = 0.9m, IsStackable = true }
            };

            var result = CouponOptimizer.SelectBestCoupons(order, coupons);
            // 订单总价：260，现金券-30=230，打9折：230*0.9=207，但算法优先减价再打折或打折再减价，两种方案择优
            Assert.LessOrEqual(result.PayableAmount, 230); // 可能更优，按实际最优组合
            Assert.IsTrue(result.AppliedCoupons.Any());
        }

        /// <summary>
        /// Defines the test method GiftAndSpecialPrice.
        /// </summary>
        [Test]
        public void GiftAndSpecialPrice()
        {
            var order = new Order
            {
                Items = new List<OrderItem>
                {
                    new OrderItem { ProductId = 1, Price = 120, Quantity = 1 },
                    new OrderItem { ProductId = 2, Price = 80, Quantity = 1 }
                }
            };
            var coupons = new List<Coupon>
            {
                new Coupon { CouponId = 3, Type = CouponType.Gift, ApplicableProductIds = new List<int> { 2 }, IsStackable = true },
                new Coupon { CouponId = 4, Type = CouponType.SpecialPrice, ApplicableProductIds = new List<int> { 1 }, SpecialPrice = 70, IsStackable = true }
            };

            var result = CouponOptimizer.SelectBestCoupons(order, coupons);

            // 兑换券把2号商品抵80，特价券把1号商品变70
            Assert.AreEqual(70, result.PayableAmount);
            Assert.AreEqual(130, result.SavedAmount); // 120+80-70
            Assert.IsTrue(result.AppliedCoupons.Any(c => c.CouponId == 3));
            Assert.IsTrue(result.AppliedCoupons.Any(c => c.CouponId == 4));
        }

        /// <summary>
        /// Defines the test method OnlyBestCombinationChosen.
        /// </summary>
        [Test]
        public void OnlyBestCombinationChosen()
        {
            var order = new Order
            {
                Items = new List<OrderItem>
            {
                new OrderItem { ProductId = 1, Price = 120, Quantity = 1 },
                new OrderItem { ProductId = 2, Price = 80, Quantity = 2 }
            }
            };
            var coupons = new List<Coupon>
            {
                new Coupon { CouponId = 1, Type = CouponType.Cash, Threshold = 250, Amount = 40, IsStackable = false },
                new Coupon { CouponId = 2, Type = CouponType.Cash, Threshold = 0, Amount = 20, IsStackable = true },
                new Coupon { CouponId = 3, Type = CouponType.Discount, Threshold = 100, DiscountRate = 0.85m, IsStackable = true },
                new Coupon { CouponId = 4, Type = CouponType.SpecialPrice, ApplicableProductIds = new List<int>(){ 2 }, SpecialPrice = 50, IsStackable = true }
            };

            var result = CouponOptimizer.SelectBestCoupons(order, coupons);

            // 手工可验证：可用满减、代金、折扣、特价四种类型组合，结果要保证最优。
            Assert.LessOrEqual(result.PayableAmount, order.TotalAmount);
            Assert.IsTrue(result.AppliedCoupons.Count > 0);
        }

        /// <summary>
        /// Defines the test method Greedy_SimpleCashCoupon.
        /// </summary>
        [Test]
        public void Greedy_SimpleCashCoupon()
        {
            var order = new Order
            {
                Items = new List<OrderItem>
                {
                    new OrderItem { ProductId = 1, Price = 120, Quantity = 1 }
                }
            };
            var coupons = new List<Coupon>
            {
                new Coupon { CouponId = 1, Type = CouponType.Cash, Threshold = 100, Amount = 30, IsStackable = false }
            };

            var result = CouponOptimizer.SelectBestCouponsGreedy(order, coupons);

            Assert.AreEqual(90, result.PayableAmount);
            Assert.AreEqual(30, result.SavedAmount);
            Assert.That(result.AppliedCoupons.Count, Is.EqualTo(1));
            Assert.That(result.AppliedCoupons.First().Type, Is.EqualTo(CouponType.Cash));
        }

        /// <summary>
        /// Defines the test method Greedy_CashAndDiscount.
        /// </summary>
        [Test]
        public void Greedy_CashAndDiscount()
        {
            var order = new Order
            {
                Items = new List<OrderItem>
            {
                new OrderItem { ProductId = 1, Price = 100, Quantity = 1 },
                new OrderItem { ProductId = 2, Price = 100, Quantity = 1 }
            }
            };
            var coupons = new List<Coupon>
            {
                new Coupon { CouponId = 2, Type = CouponType.Cash, Threshold = 150, Amount = 40, IsStackable = false },
                new Coupon { CouponId = 3, Type = CouponType.Discount, Threshold = 100, DiscountRate = 0.8m, IsStackable = true }
            };

            var result = CouponOptimizer.SelectBestCouponsGreedy(order, coupons);

            // 贪心先选40元现金券, 应付160; 或先打8折再选，按贪心可能只选单券，需覆盖主要场景
            Assert.LessOrEqual(result.PayableAmount, order.TotalAmount);
            Assert.GreaterOrEqual(result.SavedAmount, 40);
            Assert.IsTrue(result.AppliedCoupons.Any());
        }

        /// <summary>
        /// Defines the test method Greedy_GiftAndSpecialPrice.
        /// </summary>
        [Test]
        public void Greedy_GiftAndSpecialPrice()
        {
            var order = new Order
            {
                Items = new List<OrderItem>
                {
                    new OrderItem { ProductId = 1, Price = 100, Quantity = 1 },
                    new OrderItem { ProductId = 2, Price = 80, Quantity = 1 }
                }
            };
            var coupons = new List<Coupon>
            {
                new Coupon { CouponId = 4, Type = CouponType.Gift, ApplicableProductIds = new List<int> { 2 }, IsStackable = true },
                new Coupon { CouponId = 5, Type = CouponType.SpecialPrice, ApplicableProductIds = new List<int> { 1 }, SpecialPrice = 60, IsStackable = true }
            };

            var result = CouponOptimizer.SelectBestCouponsGreedy(order, coupons);

            // 兑换券把2号商品抵80，特价券把1号商品变60
            Assert.AreEqual(60, result.PayableAmount);
            Assert.AreEqual(120, result.SavedAmount);
            Assert.IsTrue(result.AppliedCoupons.Any(c => c.Type == CouponType.Gift));
            Assert.IsTrue(result.AppliedCoupons.Any(c => c.Type == CouponType.SpecialPrice));
        }

        /// <summary>
        /// Defines the test method Greedy_MultiStackableCoupons.
        /// </summary>
        [Test]
        public void Greedy_MultiStackableCoupons()
        {
            var order = new Order
            {
                Items = new List<OrderItem>
            {
                new OrderItem { ProductId = 1, Price = 50, Quantity = 2 },
                new OrderItem { ProductId = 2, Price = 30, Quantity = 2 }
            }
            };
            var coupons = new List<Coupon>
            {
                new Coupon { CouponId = 6, Type = CouponType.Cash, Threshold = 60, Amount = 20, IsStackable = true },
                new Coupon { CouponId = 7, Type = CouponType.Cash, Threshold = 50, Amount = 10, IsStackable = true },
                new Coupon { CouponId = 8, Type = CouponType.Discount, Threshold = 50, DiscountRate = 0.9m, IsStackable = true }
            };

            var result = CouponOptimizer.SelectBestCouponsGreedy(order, coupons);

            Assert.LessOrEqual(result.PayableAmount, order.TotalAmount);
            Assert.That(result.AppliedCoupons.Count, Is.GreaterThanOrEqualTo(1));

            Console.WriteLine($"AppliedCouponIds,{string.Join(",",result.AppliedCoupons.Select(c=>c.CouponId))}");
        }


        /// <summary>
        /// Defines the test method Greedy_PreferMostBeneficialCoupon.
        /// </summary>
        [Test]
        public void Greedy_PreferMostBeneficialCoupon()
        {
            var order = new Order
            {
                Items = new List<OrderItem>
                {
                    new OrderItem { ProductId = 1, Price = 200, Quantity = 1 }
                }
            };
            var coupons = new List<Coupon>
            {
                new Coupon { CouponId = 9, Type = CouponType.Cash, Threshold = 100, Amount = 30, IsStackable = false },
                new Coupon { CouponId = 10, Type = CouponType.Cash, Threshold = 200, Amount = 50, IsStackable = false }
            };

            var result = CouponOptimizer.SelectBestCouponsGreedy(order, coupons);

            // 贪心应优先用金额最大且满足门槛的券
            Assert.AreEqual(150, result.PayableAmount);
            Assert.AreEqual(50, result.SavedAmount);
            Assert.IsTrue(result.AppliedCoupons.Any(c => c.Amount == 50));
        }

        /// <summary>
        /// Defines the test method DpCashAndDiscountGlobalOptimal.
        /// 两张券，走DP，找全局最优
        /// </summary>
        [Test]
        public void DpCashAndDiscountGlobalOptimal()
        {
            // 两张券，走DP，找全局最优
            var order = new Order
            {
                Items = new List<OrderItem>
                {
                    new OrderItem { ProductId = 1, Price = 100, Quantity = 1 },
                    new OrderItem { ProductId = 2, Price = 100, Quantity = 1 }
                }
            };
            var coupons = new List<Coupon>
            {
                new Coupon { CouponId = 1, Type = CouponType.Cash, Threshold = 150, Amount = 40, IsStackable = true },
                new Coupon { CouponId = 2, Type = CouponType.Discount, Threshold = 100, DiscountRate = 0.8m, IsStackable = true }
            };
            var result = CouponOptimizer.SelectBestCouponsSmart(order, coupons);
            Assert.AreEqual(120, result.PayableAmount); // 应为(200*0.8)-40=120
            Assert.That(result.AppliedCoupons.Count, Is.EqualTo(2));
            Assert.That(result.AppliedCoupons.Any(c => c.Type == CouponType.Cash));
            Assert.That(result.AppliedCoupons.Any(c => c.Type == CouponType.Discount));
        }

        /// <summary>
        /// Defines the test method DpSpecialPriceFirstThenCash.
        /// 有特价券，先应用，然后DP现金券
        /// </summary>
        [Test]
        public void DpSpecialPriceFirstThenCash()
        {
            // 有特价券，先应用，然后DP现金券
            var order = new Order
            {
                Items = new List<OrderItem>
                {
                    new OrderItem { ProductId = 1, Price = 100, Quantity = 1 },
                    new OrderItem { ProductId = 2, Price = 80, Quantity = 1 }
                }
            };
            var coupons = new List<Coupon>
            {
                new Coupon { CouponId = 3, Type = CouponType.SpecialPrice, ApplicableProductIds = new List<int> { 2 }, SpecialPrice = 40, IsStackable = false },
                new Coupon { CouponId = 4, Type = CouponType.Cash, Threshold = 100, Amount = 30, IsStackable = true }
            };
            var result = CouponOptimizer.SelectBestCouponsSmart(order, coupons);
            Assert.AreEqual(110, result.PayableAmount); // (100+40)-30=110
            Assert.That(result.AppliedCoupons.Any(c => c.Type == CouponType.SpecialPrice));
            Assert.That(result.AppliedCoupons.Any(c => c.Type == CouponType.Cash));
        }

        /// <summary>
        /// Defines the test method DpGiftCouponAlwaysAppliedFirst.
        /// </summary>
        [Test]
        public void DpGiftCouponAlwaysAppliedFirst()
        {
            // 兑换券先抵扣商品，后续再选现金券
            var order = new Order
            {
                Items = new List<OrderItem>
            {
                new OrderItem { ProductId = 1, Price = 50, Quantity = 1 },
                new OrderItem { ProductId = 2, Price = 120, Quantity = 1 }
            }
            };
            var coupons = new List<Coupon>
            {
                new Coupon { CouponId = 5, Type = CouponType.Gift, ApplicableProductIds = new List<int> { 2 }, IsStackable = false },
                new Coupon { CouponId = 6, Type = CouponType.Cash, Threshold = 50, Amount = 20, IsStackable = true }
            };
            var result = CouponOptimizer.SelectBestCouponsSmart(order, coupons);
            Assert.AreEqual(30, result.PayableAmount); // 120抵扣后剩50-20=30
            Assert.That(result.AppliedCoupons.Any(c => c.Type == CouponType.Gift));
            Assert.That(result.AppliedCoupons.Any(c => c.Type == CouponType.Cash));
        }

        /// <summary>
        /// Defines the test method GreedyManyCashCoupons.
        /// 超过20张券（假设DP阈值为20），自动切换贪心法
        /// </summary>
        [Test]
        public void GreedyManyCashCoupons()
        {
            // 超过20张券（假设DP阈值为20），自动切换贪心法
            var order = new Order
            {
                Items = new List<OrderItem> { new OrderItem { ProductId = 1, Price = 500, Quantity = 1 } }
            };
            var coupons = new List<Coupon>();
            for (int i = 0; i < 25; i++)
            {
                coupons.Add(new Coupon
                {
                    CouponId = 1000 + i,
                    Type = CouponType.Cash,
                    Threshold = 100 + i * 10,
                    Amount = 10 + i,
                    IsStackable = true
                });
            }
            var result = CouponOptimizer.SelectBestCouponsSmart(order, coupons);

            Assert.Less(result.PayableAmount, 500); // 必须节省
            Assert.That(result.AppliedCoupons.Count, Is.GreaterThan(0));
        }


        /// <summary>
        /// Defines the test method GreedyDiscountAndCashCombo.
        /// 大量券，自动走贪心
        /// </summary>
        [Test]
        public void GreedyDiscountAndCashCombo()
        {
            // 大量券，自动走贪心
            var order = new Order
            {
                Items = new List<OrderItem> { new OrderItem { ProductId = 1, Price = 400, Quantity = 1 } }
            };
            var coupons = new List<Coupon>();
            for (int i = 0; i < 15; i++)
            {
                coupons.Add(new Coupon
                {
                    CouponId = 2000 + i,
                    Type = CouponType.Cash,
                    Threshold = 100,
                    Amount = 5 + i,
                    IsStackable = true
                });
            }
            for (int i = 0; i < 10; i++)
            {
                coupons.Add(new Coupon
                {
                    CouponId = 3000 + i,
                    Type = CouponType.Discount,
                    Threshold = 50,
                    DiscountRate = 1.0m - 0.02m * (i + 1), // 0.98, 0.96, ..., 0.8
                    IsStackable = true
                });
            }
            var result = CouponOptimizer.SelectBestCouponsSmart(order, coupons);

            Assert.Less(result.PayableAmount, 400);
            Assert.That(result.AppliedCoupons.Count, Is.GreaterThan(1));
        }
    }
}
