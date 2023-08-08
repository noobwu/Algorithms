// ***********************************************************************
// Assembly         : Noob.Maths
// Author           : noob
// Created          : 2023-08-08
//
// Last Modified By : noob
// Last Modified On : 2023-08-08
// ***********************************************************************
// <copyright file="BinomialTests.cs" company="Noob.Maths">
//     Copyright (c) . All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************
using MathNet.Numerics.Distributions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// The Maths namespace.
/// </summary>
namespace Noob.Maths
{
    /// <summary>
    /// Defines test class BinomialTests.
    /// https://github.com/mathnet/mathnet-numerics/blob/master/src/Numerics.Tests/DistributionTests/Discrete/BinomialTests.cs
    /// </summary>
    [TestFixture]
    public class BinomialTests
    {
        /// <summary>
        /// Can create binomial.
        /// </summary>
        /// <param name="p">Success probability.</param>
        /// <param name="n">Number of trials.</param>
        [TestCase(0.0, 4)]
        [TestCase(0.3, 3)]
        [TestCase(1.0, 2)]
        public void CanCreateBinomial(double p, int n)
        {
            var bernoulli = new Binomial(p, n);
            Assert.AreEqual(p, bernoulli.P);
        }
        /// <summary>
        /// Binomial create fails with bad parameters.
        /// </summary>
        /// <param name="p">Success probability.</param>
        /// <param name="n">Number of trials.</param>
        [TestCase(double.NaN, 1)]
        [TestCase(-1.0, 1)]
        [TestCase(2.0, 1)]
        [TestCase(0.3, -2)]
        public void BinomialCreateFailsWithBadParameters(double p, int n)
        {
            Assert.That(() => new Binomial(p, n), Throws.ArgumentException);
        }

        /// <summary>
        /// Binomials the specified n.
        /// </summary>
        /// <param name="n">The n.</param>
        /// <param name="k">The k.</param>
        /// <param name="p">The p.</param>
        /// <returns>System.Double.</returns>
        public  double Binomial(int n, int k, double p)
        {
            var binomial = new Binomial(p, n);
            return binomial.Probability(k);
        }

        /// <summary>
        /// Defines the test method TestProbability.
        /// </summary>
        [Test]
        public void Binomial_Test()
        {
            // 测试 14 次抛掷硬币并正面朝上 8 次的概率
            double expectedProbability = 0.18328857421875;
            double actualProbability = Binomial(14,8,0.5);
            Assert.AreEqual(expectedProbability, actualProbability, 1e-10);

            /*
            // 测试 n 或 k 为 0 的情况
            Assert.AreEqual(1, Binomial(0, 0, 0.5));
            Assert.AreEqual(0, Binomial(10, 0, 0.5));
            Assert.AreEqual(0, Binomial(0, 10, 0.5));

            // 测试 n 等于 k 的情况
            Assert.AreEqual(1, Binomial(10, 10, 0.5));

            // 测试 n 小于 k 的情况
            Assert.AreEqual(0, Binomial(5, 10, 0.5));

            // 测试 p 为 0 或 1 的情况
            Assert.AreEqual(0, Binomial(10, 5, 0));
            Assert.AreEqual(1, Binomial(10, 10, 1));
            */
        }
    }
}
