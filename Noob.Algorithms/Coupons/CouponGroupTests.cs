// ***********************************************************************
// Assembly         : Noob.Algorithms
// Author           : noob
// Created          : 2025-05-22
//
// Last Modified By : noob
// Last Modified On : 2025-05-22
// ***********************************************************************
// <copyright file="CouponGroupTests.cs" company="Noob.Algorithms">
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

namespace Noob.Algorithms.Coupons
{
    /// <summary>
    /// Defines test class CouponTests.
    /// </summary>
    [TestFixture]
    public partial class CouponTests
    {
        /// <summary>
        /// Defines the test method GroupPrune_SingleGroup_SelectsBestCoupon.
        /// </summary>
        [Test]
        public void GroupPrune_SingleGroup_SelectsBestCoupon()
        {
            var order = new Order
            {
                Items = new List<OrderItem>
                {
                    new OrderItem { ProductId = 1, Price = 100, Quantity = 1 }
                }
            };
            var coupons = new List<Coupon>
            {
                new Coupon { CouponId = 10, Type = CouponType.Cash, Amount = 30, Threshold = 90, ApplicableProductIds = new List<int> { 1 } },
                new Coupon { CouponId = 11, Type = CouponType.Cash, Amount = 15, Threshold = 80, ApplicableProductIds = new List<int> { 1 } }
            };

            var result = CouponOptimizer.SelectBestCouponsGroupPrune(order, coupons);

            Assert.That(result.AppliedCoupons.Count, Is.EqualTo(1));
            Assert.That(result.AppliedCoupons[0].CouponId, Is.EqualTo(10));
            Assert.That(result.PayableAmount, Is.EqualTo(70));
        }


        /// <summary>
        /// Defines the test method GroupPrune_MultiGroups_SelectsBestPerGroup.
        /// </summary>
        [Test]
        public void GroupPrune_MultiGroups_SelectsBestPerGroup()
        {
            var order = new Order
            {
                Items = new List<OrderItem>
                {
                    new OrderItem { ProductId = 1, Price = 150, Quantity = 1 },
                    new OrderItem { ProductId = 2, Price = 100, Quantity = 1 }
                }
            };
            var coupons = new List<Coupon>
            {
                new Coupon { CouponId = 21, Type = CouponType.Cash, Amount = 40, Threshold = 120, ApplicableProductIds = new List<int> { 1 } },
                new Coupon { CouponId = 22, Type = CouponType.Cash, Amount = 20, Threshold = 90, ApplicableProductIds = new List<int> { 2 } }
            };

            var result = CouponOptimizer.SelectBestCouponsGroupPrune(order, coupons);

            Assert.That(result.AppliedCoupons.Any(c => c.CouponId == 21));
            Assert.That(result.AppliedCoupons.Any(c => c.CouponId == 22));
            Assert.That(result.PayableAmount, Is.EqualTo(150 + 100 - 40 - 20));
        }
    }
}
