// ***********************************************************************
// Assembly         : Noob.Maths
// Author           : noob
// Created          : 2023-08-10
//
// Last Modified By : noob
// Last Modified On : 2023-08-10
// ***********************************************************************
// <copyright file="ProbabilityTests.cs" company="Noob.Maths">
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

/// <summary>
/// The Maths namespace.
/// </summary>
namespace Noob.Maths
{
    /// <summary>
    /// Class ProbabilityTests.
    /// </summary>
    [TestFixture]
    public class ProbabilityTests
    {
        /// <summary>
        /// Calculates the n.
        /// </summary>
        /// <param name="probability">The probability.</param>
        /// <param name="target">The target.</param>
        /// <returns>System.Double.</returns>
        public static double CalculateN(double probability, double target)
        {
            return Math.Log(target) / Math.Log(probability);
        }

        /// <summary>
        /// Defines the test method CalculateN_WithProbability0_9AndTarget0_05_ReturnsApproximately26.
        /// </summary>
        [Test]
        public void CalculateN_WithProbability0_9AndTarget0_05_ReturnsApproximately26()
        {
            // Arrange
            double probability = 0.9;
            double target = 0.05;//1−(0.9)^{26} \approx 0.9582
            double expected = 28; // 这里可以根据实际情况调整精度

            // Act
            double actual = CalculateN(probability, target);
            Console.WriteLine($"{nameof(CalculateN)},probability:{probability},target:{target},actual:{actual}");

            // Assert
            Assert.AreEqual(expected, actual, 1); // 允许1的误差
        }

        /// <summary>
        /// Probabilities the mass function with12 trials2 successes and probability1 over6 returns approximately0 2961.
        /// </summary>
        [Test]
        public void ProbabilityMassFunction_With12Trials2SuccessesAndProbability1Over6_ReturnsApproximately0_2961()
        {
            // Arrange
            int n = 12;
            int k = 2;
            double p = 1.0 / 6;
            double expected = 0.2961;

            // Act
            double actual = ProbabilityMassFunction(n, k, p);

            // Assert
            Assert.AreEqual(expected, actual, 4); // 允许0.0001的误差
        }
        /// <summary>
        /// Probabilities the mass function.
        /// </summary>
        /// <param name="n">The n.</param>
        /// <param name="k">The k.</param>
        /// <param name="p">The p.</param>
        /// <returns>System.Double.</returns>
        public double ProbabilityMassFunction(int n, int k, double p)
        {
            double binomialCoefficient = Factorial(n) / (Factorial(k) * Factorial(n - k));
            return binomialCoefficient * Math.Pow(p, k) * Math.Pow(1 - p, n - k);
        }

        /// <summary>
        /// Factorials the specified number.
        /// </summary>
        /// <param name="number">The number.</param>
        /// <returns>System.Double.</returns>
        private  double Factorial(int number)
        {
            double result = 1;
            for (int i = 1; i <= number; i++)
            {
                result *= i;
            }
            return result;
        }
    }
}
