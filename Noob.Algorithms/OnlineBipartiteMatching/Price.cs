// ***********************************************************************
// Assembly         : Noob.Algorithms
// Author           : noob
// Created          : 2025-07-01
//
// Last Modified By : noob
// Last Modified On : 2025-07-01
// ***********************************************************************
// <copyright file="Price.cs" company="Noob.Algorithms">
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

namespace Noob.Algorithms.OnlineBipartiteMatching
{

    /// <summary>
    /// 分段阶梯定价计算参数
    /// </summary>
    public class SegmentedPriceRequest
    {
        /// <summary>
        /// 分段区间列表（需按StartKm升序，EndKm支持double.MaxValue）
        /// </summary>
        /// <value>The segments.</value>
        public List<PriceSegment> Segments { get; set; } = new();

        /// <summary>
        /// 计费距离（公里）
        /// </summary>
        /// <value>The distance km.</value>
        public double DistanceKm { get; set; }

        /// <summary>
        /// 打赏/小费（元，默认0）
        /// </summary>
        /// <value>The base tip.</value>
        public double BaseTip { get; set; } = 0;

        /// <summary>
        /// 起步价（最低收费，默认0）
        /// </summary>
        /// <value>The minimum price.</value>
        public double MinPrice { get; set; } = 0;

        /// <summary>
        /// 封顶价（最高收费，默认double.MaxValue）
        /// </summary>
        /// <value>The maximum price.</value>
        public double MaxPrice { get; set; } = double.MaxValue;

        /// <summary>
        /// 保留小数位数（默认2）
        /// </summary>
        /// <value>The decimal places.</value>
        public int DecimalPlaces { get; set; } = 2;

        /// <summary>
        /// 币种代码（可选，如"RMB","USD"，未来扩展）
        /// </summary>
        /// <value>The currency code.</value>
        public string? CurrencyCode { get; set; }
    }

    /// <summary>
    /// 距离分段阶梯计价区间配置。
    /// </summary>
    public class PriceSegment
    {
        /// <summary>
        /// 区间起点（含）
        /// </summary>
        /// <value>The start km.</value>
        public double StartKm { get; set; }
        /// <summary>
        /// 区间终点（不含）
        /// </summary>
        /// <value>The end km.</value>
        public double EndKm { get; set; }
        /// <summary>
        /// 区间内单价（元/公里）
        /// </summary>
        /// <value>The unit price.</value>
        public double UnitPrice { get; set; }
    }

    /// <summary>
    /// 分段阶梯定价计算器。
    /// </summary>
    public static class PriceCalculator
    {
        /// <summary>
        /// 分段阶梯计价核心算法，支持可扩展参数对象，便于复杂业务扩展与测试
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Double.</returns>
        /// <exception cref="System.ArgumentNullException">request</exception>
        /// <exception cref="System.ArgumentException">分段定价区间不能为空 - Segments</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">DistanceKm - 距离不能为负</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">BaseTip - 打赏/小费不能为负</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">MinPrice - 起步价不能为负</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">MaxPrice - 封顶价必须为正</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">DecimalPlaces - 小数位数不能为负</exception>
        public static double CalculateSegmentedPrice(SegmentedPriceRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            if (request.Segments == null || request.Segments.Count == 0)
                throw new ArgumentException("分段定价区间不能为空", nameof(request.Segments));
            if (request.DistanceKm < 0)
                throw new ArgumentOutOfRangeException(nameof(request.DistanceKm), "距离不能为负");
            if (request.BaseTip < 0)
                throw new ArgumentOutOfRangeException(nameof(request.BaseTip), "打赏/小费不能为负");
            if (request.MinPrice < 0)
                throw new ArgumentOutOfRangeException(nameof(request.MinPrice), "起步价不能为负");
            if (request.MaxPrice <= 0)
                throw new ArgumentOutOfRangeException(nameof(request.MaxPrice), "封顶价必须为正");
            if (request.DecimalPlaces < 0)
                throw new ArgumentOutOfRangeException(nameof(request.DecimalPlaces), "小数位数不能为负");

            double total = 0;
            double remaining = request.DistanceKm;

            foreach (var segment in request.Segments)
            {
                if (remaining <= 0) break;
                double segmentLen = Math.Min(segment.EndKm - segment.StartKm, remaining);
                if (segmentLen > 0)
                {
                    total += segmentLen * segment.UnitPrice;
                    remaining -= segmentLen;
                }
            }

            total += request.BaseTip;
            if (total < request.MinPrice) total = request.MinPrice;
            if (total > request.MaxPrice) total = request.MaxPrice;

            return Math.Round(total, request.DecimalPlaces, MidpointRounding.AwayFromZero);
        }

    }

    /// <summary>
    /// Defines test class PriceCalculatorTests.
    /// </summary>
    [TestFixture]
    public class PriceCalculatorTests
    {
        /// <summary>
        /// Defines the test method SegmentedPrice_Basic_CorrectCalculation.
        /// </summary>
        [Test]
        public void SegmentedPrice_Basic_CorrectCalculation()
        {
            var request = new SegmentedPriceRequest
            {
                Segments = new List<PriceSegment>
                {
                    new PriceSegment { StartKm = 0, EndKm = 3, UnitPrice = 2.8 },
                    new PriceSegment { StartKm = 3, EndKm = 15, UnitPrice = 2.2 },
                    new PriceSegment { StartKm = 15, EndKm = double.MaxValue, UnitPrice = 2.0 }
                },
                DistanceKm = 18,
                BaseTip = 1,
                MinPrice = 8,
                MaxPrice = 120,
                DecimalPlaces = 2,
                CurrencyCode = "CNY"
            };
            var result = PriceCalculator.CalculateSegmentedPrice(request);
            // (3*2.8)+(12*2.2)+(3*2.0)+1=8.4+26.4+6.0+1=41.8
            Assert.That(result, Is.EqualTo(41.8).Within(1e-8));
        }

        /// <summary>
        /// Defines the test method SegmentedPrice_DistanceLessThanFirstSegment_MinPriceEnforced.
        /// </summary>
        [Test]
        public void SegmentedPrice_DistanceLessThanFirstSegment_MinPriceEnforced()
        {
            var request = new SegmentedPriceRequest
            {
                Segments = new List<PriceSegment>
                {
                    new PriceSegment { StartKm = 0, EndKm = 3, UnitPrice = 2.8 },
                    new PriceSegment { StartKm = 3, EndKm = double.MaxValue, UnitPrice = 2.2 }
                },
                DistanceKm = 1.5,
                MinPrice = 10,
                DecimalPlaces = 2
            };
            var result = PriceCalculator.CalculateSegmentedPrice(request);
            // 1.5*2.8=4.2 < minPrice=10
            Assert.That(result, Is.EqualTo(10).Within(1e-8));
        }

        /// <summary>
        /// Defines the test method SegmentedPrice_AboveCap_MaxPriceEnforced.
        /// </summary>
        [Test]
        public void SegmentedPrice_AboveCap_MaxPriceEnforced()
        {
            var request = new SegmentedPriceRequest
            {
                Segments = new List<PriceSegment>
                {
                    new PriceSegment { StartKm = 0, EndKm = 10, UnitPrice = 2 },
                    new PriceSegment { StartKm = 10, EndKm = double.MaxValue, UnitPrice = 3 }
                },
                DistanceKm = 50,
                BaseTip = 2,
                MaxPrice = 100,
                DecimalPlaces = 2
            };
            var result = PriceCalculator.CalculateSegmentedPrice(request);
            // (10*2)+(40*3)+2=20+120+2=142 > maxPrice=100
            Assert.That(result, Is.EqualTo(100).Within(1e-8));
        }

        /// <summary>
        /// Defines the test method SegmentedPrice_KeepDecimalPlaces.
        /// </summary>
        [Test]
        public void SegmentedPrice_KeepDecimalPlaces()
        {
            var request = new SegmentedPriceRequest
            {
                Segments = new List<PriceSegment>
                {
                    new PriceSegment { StartKm = 0, EndKm = double.MaxValue, UnitPrice = 1.2345 }
                },
                DistanceKm = 2,
                DecimalPlaces = 3
            };
            var result = PriceCalculator.CalculateSegmentedPrice(request);
            // 2*1.2345=2.469, 保留3位小数
            Assert.That(result, Is.EqualTo(2.469).Within(1e-8));
        }

        /// <summary>
        /// Defines the test method SegmentedPrice_NegativeDistance_ThrowsException.
        /// </summary>
        [Test]
        public void SegmentedPrice_NegativeDistance_ThrowsException()
        {
            var request = new SegmentedPriceRequest
            {
                Segments = new List<PriceSegment>
                {
                    new PriceSegment { StartKm = 0, EndKm = 5, UnitPrice = 1 }
                },
                DistanceKm = -1
            };
            Assert.That(() => PriceCalculator.CalculateSegmentedPrice(request),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        /// <summary>
        /// Defines the test method SegmentedPrice_NullSegments_ThrowsException.
        /// </summary>
        [Test]
        public void SegmentedPrice_NullSegments_ThrowsException()
        {
            var request = new SegmentedPriceRequest
            {
                Segments = null,
                DistanceKm = 1
            };
            Assert.That(() => PriceCalculator.CalculateSegmentedPrice(request),
                Throws.ArgumentException);
        }

        /// <summary>
        /// Defines the test method SegmentedPrice_ZeroSegments_ThrowsException.
        /// </summary>
        [Test]
        public void SegmentedPrice_ZeroSegments_ThrowsException()
        {
            var request = new SegmentedPriceRequest
            {
                Segments = new List<PriceSegment>(),
                DistanceKm = 1
            };
            Assert.That(() => PriceCalculator.CalculateSegmentedPrice(request),
                Throws.ArgumentException);
        }

        /// <summary>
        /// Defines the test method SegmentedPrice_NegativeTip_ThrowsException.
        /// </summary>
        [Test]
        public void SegmentedPrice_NegativeTip_ThrowsException()
        {
            var request = new SegmentedPriceRequest
            {
                Segments = new List<PriceSegment>
                {
                    new PriceSegment { StartKm = 0, EndKm = 5, UnitPrice = 2 }
                },
                DistanceKm = 2,
                BaseTip = -1
            };
            Assert.That(() => PriceCalculator.CalculateSegmentedPrice(request),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }
    }
}
