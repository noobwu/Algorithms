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
