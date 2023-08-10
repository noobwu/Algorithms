using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Noob.Maths
{
    /// <summary>
    /// Defines test class PoissonDistributionTests.
    /// </summary>
    public class PoissonDistributionTests
    {
        /// <summary>
        /// Defines the test method ProbabilityMassFunction_ValidInputs_ReturnsExpectedResult.
        /// </summary>
        /// <param name="k">The k.</param>
        /// <param name="lambda">The lambda.</param>
        /// <param name="expected">The expected.</param>
        [Theory]
        [TestCase(3, 5, 0.1403738958142805)]
        [TestCase(0, 2, 0.1353352832366127)]
        [TestCase(5, 5, 0.1754673697678507)]
        public void ProbabilityMassFunction_ValidInputs_ReturnsExpectedResult(int k, double lambda, double expected)
        {
            // Act
            double actual = ProbabilityMassFunction(k, lambda);

            // Assert
            Assert.AreEqual(expected, actual, 5);
        }

        /// <summary>
        /// Defines the test method ProbabilityMassFunction_InvalidInputs_ThrowsArgumentException.
        /// </summary>
        /// <param name="k">The k.</param>
        /// <param name="lambda">The lambda.</param>
        [Theory]
        [TestCase(-1, 5)]
        [TestCase(3, -5)]
        public void ProbabilityMassFunction_InvalidInputs_ThrowsArgumentException(int k, double lambda)
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => ProbabilityMassFunction(k, lambda));
        }
        /// <summary>
        /// Probabilities the mass function.
        /// </summary>
        /// <param name="k">The k.</param>
        /// <param name="lambda">The lambda.</param>
        /// <returns>System.Double.</returns>
        /// <exception cref="System.ArgumentException">k must be non-negative and lambda must be positive.</exception>
        public static double ProbabilityMassFunction(int k, double lambda)
        {
            if (k < 0 || lambda <= 0)
                throw new ArgumentException("k must be non-negative and lambda must be positive.");

            return Math.Exp(-lambda) * Math.Pow(lambda, k) / Factorial(k);
        }

        /// <summary>
        /// Factorials the specified n.
        /// </summary>
        /// <param name="n">The n.</param>
        /// <returns>System.Double.</returns>
        private static double Factorial(int n)
        {
            double result = 1;
            for (int i = 1; i <= n; i++)
            {
                result *= i;
            }
            return result;
        }
    }
}
