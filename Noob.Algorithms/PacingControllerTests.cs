// ***********************************************************************
// Assembly         : Noob.Algorithms
// Author           : noob
// Created          : 2025-06-27
//
// Last Modified By : noob
// Last Modified On : 2025-06-27
// ***********************************************************************
// <copyright file="PacingControllerTests.cs" company="Noob.Algorithms">
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
    /// 广告预算动态pacing调度控制器，支持S型消耗曲线和平滑防抖
    /// </summary>
    public class PacingController : IPacingController
    {
        /// <summary>
        /// pacing模式：可选"sigmoid"或"polynomial"
        /// </summary>
        /// <value>The mode.</value>
        public string Mode { get; set; } = "sigmoid"; // or "polynomial"

        /// <summary>
        /// S型参考曲线，如 1-e^-(1-x)
        /// </summary>
        /// <param name="t">The t.</param>
        /// <returns>System.Double.</returns>
        private static double SCurve(double t)
        {
            return 1 - Math.Exp(-t);
        }

        /// <summary>
        /// 入口：根据预算消耗进度，平滑调整广告分数
        /// </summary>
        /// <param name="ad">The ad.</param>
        /// <param name="time">The time.</param>
        /// <returns>System.Double.</returns>
        public double AdjustScore(AdCandidate ad, DateTime time)
        {
            var totalMinutes = 24 * 60.0;
            var curMinutes = time.Hour * 60 + time.Minute;
            var timeProgress = curMinutes / totalMinutes;
            var targetProgress = SCurve(timeProgress);
            var realProgress = 1.0 - ad.Budget.Remain / (ad.Budget.Total + 1e-6);

            // 注意：反转方向
            var diff = targetProgress - realProgress;

            double pacingFactor;
            if (Mode == "sigmoid")
                pacingFactor = SigmoidSmooth(diff);
            else
                pacingFactor = PolynomialSmooth(diff);

            return ad.Score * pacingFactor;
        }

        /// <summary>
        /// Sigmoid平滑防抖，diff=0时为1.0，[0.9,1.1]
        /// </summary>
        private static double SigmoidSmooth(double diff)
        {
            double baseFactor = 1.0;
            double delta = 0.1; // 最大调整幅度±10%
            double k = 5.0;     // 灵敏度，工程可调
            double scaling = 1.0 / (1.0 + Math.Exp(-k * diff));
            double pacingFactor = baseFactor + (scaling - 0.5) * 2 * delta; // [0.9,1.1]
            pacingFactor = Math.Max(0.7, Math.Min(1.2, pacingFactor));
            return pacingFactor;
        }

        /// <summary>
        /// Polynomial（二次/三次型）平滑防抖
        /// </summary>
        private static double PolynomialSmooth(double diff)
        {
            double alpha = 0.8, beta = 0.7, gamma = 0.25; // gamma增强响应
            double pacingFactor = 1.0 + gamma * diff - alpha * Math.Pow(diff, 2) - beta * Math.Pow(diff, 3);
            pacingFactor = Math.Max(0.7, Math.Min(1.2, pacingFactor));
            return pacingFactor;
        }
    }

    /// <summary>
    /// 广告pacing控制器接口（便于A/B实验、替换不同算法）
    /// </summary>
    public interface IPacingController
    {
        /// <summary>
        /// Adjusts the score.
        /// </summary>
        /// <param name="ad">The ad.</param>
        /// <param name="time">The time.</param>
        /// <returns>System.Double.</returns>
        double AdjustScore(AdCandidate ad, DateTime time);
    }

    /// <summary>
    /// 广告预算结构体
    /// </summary>
    public class AdBudget
    {
        /// <summary>
        /// Gets or sets the total.
        /// </summary>
        /// <value>The total.</value>
        public double Total { get; set; }
        /// <summary>
        /// Gets or sets the remain.
        /// </summary>
        /// <value>The remain.</value>
        public double Remain { get; set; }
        // ...其它字段
    }

    /// <summary>
    /// 单个广告候选
    /// </summary>
    public class AdCandidate
    {
        /// <summary>
        /// Gets or sets the ad identifier.
        /// </summary>
        /// <value>The ad identifier.</value>
        public long AdId { get; set; }
        /// <summary>
        /// Gets or sets the budget.
        /// </summary>
        /// <value>The budget.</value>
        public AdBudget Budget { get; set; }
        /// <summary>
        /// Gets or sets the score.
        /// </summary>
        /// <value>The score.</value>
        public double Score { get; set; } // 来自ML排序/出价
                                          // ...其它特征...
    }

    /// <summary>
    /// Defines test class PacingControllerTests.
    /// </summary>
    [TestFixture]
    public class PacingControllerTests
    {
        /// <summary>
        /// Makes the ad.
        /// </summary>
        /// <param name="total">The total.</param>
        /// <param name="remain">The remain.</param>
        /// <param name="score">The score.</param>
        /// <returns>AdCandidate.</returns>
        private AdCandidate MakeAd(double total, double remain, double score = 1.0)
        {
            return new AdCandidate
            {
                AdId = 101,
                Budget = new AdBudget { Total = total, Remain = remain },
                Score = score
            };
        }

        /// <summary>
        /// Defines the test method Sigmoid_NormalProgress_ShouldNotAdjustMuch.
        /// </summary>
        [Test]
        public void Sigmoid_NormalProgress_ShouldNotAdjustMuch()
        {
            var controller = new PacingController { Mode = "sigmoid" };
            var ad = MakeAd(100, 50);
            var now = new DateTime(2024, 6, 1, 12, 0, 0);
            var adjScore = controller.AdjustScore(ad, now);
            Assert.That(adjScore, Is.InRange(0.9, 1.1), $"Actual: {adjScore}");
        }

        /// <summary>
        /// Defines the test method Sigmoid_BudgetLagging_ShouldBoostScore.
        /// </summary>
        [Test]
        public void Sigmoid_BudgetLagging_ShouldBoostScore()
        {
            var controller = new PacingController { Mode = "sigmoid" };
            var ad = MakeAd(100, 60);
            var now = new DateTime(2024, 6, 1, 18, 0, 0);
            var adjScore = controller.AdjustScore(ad, now);
            Assert.That(adjScore, Is.GreaterThan(1.0), $"Actual: {adjScore}");
        }

        /// <summary>
        /// Defines the test method Sigmoid_BudgetRushed_ShouldLowerScore.
        /// </summary>
        [Test]
        public void Sigmoid_BudgetRushed_ShouldLowerScore()
        {
            var controller = new PacingController { Mode = "sigmoid" };
            var ad = MakeAd(100, 10);
            var now = new DateTime(2024, 6, 1, 6, 0, 0);
            var adjScore = controller.AdjustScore(ad, now);
            Assert.That(adjScore, Is.LessThan(1.0), $"Actual: {adjScore}");
        }


        [Test]
        public void Polynomial_NormalProgress_ShouldNotAdjustMuch()
        {
            var controller = new PacingController { Mode = "polynomial" };
            var ad = MakeAd(100, 50);
            var now = new DateTime(2024, 6, 1, 12, 0, 0);
            var adjScore = controller.AdjustScore(ad, now);
            Assert.That(adjScore, Is.InRange(0.85, 1.1), $"Actual: {adjScore}");
        }

        [Test]
        public void Polynomial_BudgetLagging_ShouldBoostScore()
        {
            var controller = new PacingController { Mode = "polynomial" };
            var ad = MakeAd(100, 60);
            var now = new DateTime(2024, 6, 1, 18, 0, 0);
            var adjScore = controller.AdjustScore(ad, now);
            Assert.That(adjScore, Is.GreaterThan(1.0), $"Actual: {adjScore}");
        }

        [Test]
        public void Polynomial_BudgetRushed_ShouldLowerScore()
        {
            var controller = new PacingController { Mode = "polynomial" };
            var ad = MakeAd(100, 10);
            var now = new DateTime(2024, 6, 1, 6, 0, 0);
            var adjScore = controller.AdjustScore(ad, now);
            Assert.That(adjScore, Is.LessThan(1.0), $"Actual: {adjScore}");
        }

        [Test]
        public void PacingFactor_ClippedBetweenRange()
        {
            var controller = new PacingController { Mode = "sigmoid" };
            var ad = MakeAd(100, 100);
            var now = new DateTime(2024, 6, 1, 23, 59, 0);
            var adjScore = controller.AdjustScore(ad, now);
            Assert.That(adjScore, Is.InRange(0.7, 1.2));

            controller.Mode = "polynomial";
            adjScore = controller.AdjustScore(ad, now);
            Assert.That(adjScore, Is.InRange(0.7, 1.2));
        }


        /// <summary>
        /// Defines the test method ZeroBudget_ShouldNotThrow.
        /// </summary>
        [Test]
        public void ZeroBudget_ShouldNotThrow()
        {
            var controller = new PacingController();
            var ad = MakeAd(0, 0);
            var now = DateTime.Now;
            Assert.DoesNotThrow(() => controller.AdjustScore(ad, now));
        }
    }

}
