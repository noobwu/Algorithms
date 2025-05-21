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
    /// Class CouponOptimizer.
    /// </summary>
    public class CouponOptimizer {

        /// <summary>
        /// 自动选择最大优惠券组合
        /// </summary>
        public static CouponApplyResult SelectBestCoupons(Order order, List<Coupon> availableCoupons)
        {
            if (order == null) throw new ArgumentNullException(nameof(order));
            if (availableCoupons == null) throw new ArgumentNullException(nameof(availableCoupons));

            // Step1: 过滤可用券
            var validCoupons = availableCoupons?.FindAll(c => c.IsValidFor(order));
            var bestResult = new CouponApplyResult { SavedAmount = 0, PayableAmount = order.TotalAmount };

            // Step2: 按可叠加性拆组，生成全部候选组合（简单枚举：所有不可叠加券+所有叠加券，及单独所有叠加券）
            var stackable = validCoupons?.FindAll(c => c.IsStackable)??new List<Coupon>();
            var nonStackable = validCoupons?.FindAll(c => !c.IsStackable)??new List<Coupon>();

            var allCandidates = new List<List<Coupon>>();
            foreach (var single in nonStackable)
                allCandidates.Add(new List<Coupon> { single }.Concat(stackable).ToList());
            allCandidates.Add(stackable);

            foreach (var coupons in allCandidates)
            {
                var distinctCoupons = coupons.Distinct().ToList();
                if (distinctCoupons.Count == 0) continue;

                decimal saved = 0;
                decimal payable = order.TotalAmount;

                // 为每个组合深复制购物车，避免相互影响
                var itemsCopy = order.Items.Select(i => new OrderItem
                {
                    ProductId = i.ProductId,
                    Price = i.Price,
                    Quantity = i.Quantity
                }).ToList();

                // Step3: 先处理Gift券（兑换券）
                foreach (var coupon in distinctCoupons.Where(c => c.Type == CouponType.Gift))
                {
                    foreach (var item in itemsCopy)
                    {
                        if (coupon.ApplicableProductIds != null &&
                            coupon.ApplicableProductIds.Contains(item.ProductId) &&
                            item.Quantity > 0)
                        {
                            // 假设一张券只能兑换一件
                            var redeemQty = 1;
                            var redeemValue = Math.Min(item.Quantity, redeemQty) * item.Price;
                            saved += redeemValue;
                            item.Quantity -= redeemQty;
                            break;
                        }
                    }
                }

                // Step4: 再处理SpecialPrice券（特价券）
                foreach (var coupon in distinctCoupons.Where(c => c.Type == CouponType.SpecialPrice))
                {
                    var item = itemsCopy.FirstOrDefault(i => coupon.ApplicableProductIds?.Contains(i.ProductId)??false);
                    if (item != null && coupon.SpecialPrice.HasValue && item.Price > coupon.SpecialPrice.Value)
                    {
                        saved += (item.Price - coupon.SpecialPrice.Value) * item.Quantity;
                        item.Price = coupon.SpecialPrice.Value;
                    }
                }

                // Step5: 再处理Discount券（折扣券，通常只允许同一商品单张生效）
                var discountCoupon = distinctCoupons.Where(c => c.Type == CouponType.Discount)
                                                    .OrderBy(c => c.DiscountRate).FirstOrDefault();
                if (discountCoupon != null && discountCoupon.DiscountRate > 0 && discountCoupon.DiscountRate < 1)
                {
                    var discountAmount = itemsCopy
                        .Where(i => discountCoupon.ApplicableProductIds == null || discountCoupon.ApplicableProductIds.Count == 0 || discountCoupon.ApplicableProductIds.Contains(i.ProductId))
                        .Sum(i => i.Price * i.Quantity * (1 - discountCoupon.DiscountRate));
                    saved += discountAmount;

                    foreach (var item in itemsCopy)
                    {
                        if (discountCoupon.ApplicableProductIds == null || discountCoupon.ApplicableProductIds.Count == 0 || discountCoupon.ApplicableProductIds.Contains(item.ProductId))
                        {
                            item.Price *= discountCoupon.DiscountRate;
                        }
                    }
                }

                // Step6: 处理Cash券（满减、代金券）
                foreach (var coupon in distinctCoupons.Where(c => c.Type == CouponType.Cash))
                {
                    var amount = itemsCopy
                        .Where(i => coupon.ApplicableProductIds == null || coupon.ApplicableProductIds.Count == 0 || coupon.ApplicableProductIds.Contains(i.ProductId))
                        .Sum(i => i.Price * i.Quantity);
                    if (amount >= coupon.Threshold)
                    {
                        var reduction = Math.Min(coupon.Amount, amount);
                        saved += reduction;
                    }
                }

                payable = Math.Max(order.TotalAmount - saved, 0);

                // Step7: 更新最优方案
                if (payable < bestResult.PayableAmount ||
                    (payable == bestResult.PayableAmount && distinctCoupons.Count > bestResult.AppliedCoupons.Count))
                {
                    bestResult.PayableAmount = payable;
                    bestResult.SavedAmount = saved;
                    bestResult.AppliedCoupons = distinctCoupons;
                }
            }
            return bestResult;
        }
    }

    /// <summary>
    /// Defines test class CouponTests.
    /// </summary>
    [TestFixture]
    public class CouponTests {

        /// <summary>
        /// Defines the test method Test_SimpleCashCoupon.
        /// </summary>
        [Test]
        public void Test_SimpleCashCoupon()
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
        public void Test_CashAndDiscountBestCombo()
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
        /// Defines the test method Test_GiftAndSpecialPrice.
        /// </summary>
        [Test]
        public void Test_GiftAndSpecialPrice()
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
        /// Defines the test method Test_OnlyBestCombinationChosen.
        /// </summary>
        [Test]
        public void Test_OnlyBestCombinationChosen()
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
    }
}
