// ***********************************************************************
// Assembly         : Noob.Algorithms
// Author           : noob
// Created          : 2023-10-21
//
// Last Modified By : noob
// Last Modified On : 2023-10-22
// ***********************************************************************
// <copyright file="RetryHelperTests.cs" company="Noob.Algorithms">
//     Copyright (c) . All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************
using FluentAssertions;
using Noob.Algorithms.Polly.Utils;
using NUnit.Framework;

/// <summary>
/// The Polly namespace.
/// </summary>
namespace Noob.Algorithms.Polly
{
    /// <summary>
    /// Class RetryHelperTests.
    /// </summary>
    [TestFixture]
    public class RetryHelperTests
    {
        /// <summary>
        /// The randomizer
        /// </summary>
        private Func<double> _randomizer = new RandomUtil(0).NextDouble;

        /// <summary>
        /// Defines the test method IsValidDelay_Ok.
        /// </summary>
        [TestCase]
        public void IsValidDelay_Ok()
        {
            RetryHelper.IsValidDelay(TimeSpan.Zero).Should().BeTrue();
            RetryHelper.IsValidDelay(TimeSpan.FromSeconds(1)).Should().BeTrue();
            RetryHelper.IsValidDelay(TimeSpan.MaxValue).Should().BeTrue();
            RetryHelper.IsValidDelay(TimeSpan.MinValue).Should().BeFalse();
            RetryHelper.IsValidDelay(TimeSpan.FromMilliseconds(-1)).Should().BeFalse();
        }

        /// <summary>
        /// Defines the test method UnsupportedRetryBackoffType_Throws.
        /// </summary>
        /// <param name="jitter">if set to <c>true</c> [jitter].</param>
        [TestCase(true)]
        [TestCase(false)]
        public void UnsupportedRetryBackoffType_Throws(bool jitter)
        {
            DelayBackoffType type = (DelayBackoffType)99;

            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                double state = 0;
                 RetryHelper.GetRetryDelay(type, jitter, 0, TimeSpan.FromSeconds(1), null, ref state, _randomizer);
            });
        }

        /// <summary>
        /// Constants the ok.
        /// </summary>
        [TestCase]
        public void Constant_Ok()
        {
            double state = 0;

            RetryHelper.GetRetryDelay(DelayBackoffType.Constant, false, 0, TimeSpan.Zero, null, ref state, _randomizer).Should().Be(TimeSpan.Zero);
            RetryHelper.GetRetryDelay(DelayBackoffType.Constant, false, 1, TimeSpan.Zero, null, ref state, _randomizer).Should().Be(TimeSpan.Zero);
            RetryHelper.GetRetryDelay(DelayBackoffType.Constant, false, 2, TimeSpan.Zero, null, ref state, _randomizer).Should().Be(TimeSpan.Zero);

            RetryHelper.GetRetryDelay(DelayBackoffType.Constant, false, 0, TimeSpan.FromSeconds(1), null, ref state, _randomizer).Should().Be(TimeSpan.FromSeconds(1));
            RetryHelper.GetRetryDelay(DelayBackoffType.Constant, false, 1, TimeSpan.FromSeconds(1), null, ref state, _randomizer).Should().Be(TimeSpan.FromSeconds(1));
            RetryHelper.GetRetryDelay(DelayBackoffType.Constant, false, 2, TimeSpan.FromSeconds(1), null, ref state, _randomizer).Should().Be(TimeSpan.FromSeconds(1));
        }

        /// <summary>
        /// Constants the jitter ok.
        /// </summary>
        public void Constant_Jitter_Ok()
        {
            double state = 0;

            RetryHelper.GetRetryDelay(DelayBackoffType.Constant, true, 0, TimeSpan.Zero, null, ref state, _randomizer).Should().Be(TimeSpan.Zero);
            RetryHelper.GetRetryDelay(DelayBackoffType.Constant, true, 1, TimeSpan.Zero, null, ref state, _randomizer).Should().Be(TimeSpan.Zero);
            RetryHelper.GetRetryDelay(DelayBackoffType.Constant, true, 2, TimeSpan.Zero, null, ref state, _randomizer).Should().Be(TimeSpan.Zero);

            _randomizer = () => 0.0;
            RetryHelper
                .GetRetryDelay(DelayBackoffType.Constant, true, 0, TimeSpan.FromSeconds(1), null, ref state, _randomizer)
                .Should()
                .Be(TimeSpan.FromSeconds(0.75));

            _randomizer = () => 0.4;
            RetryHelper
                .GetRetryDelay(DelayBackoffType.Constant, true, 0, TimeSpan.FromSeconds(1), null, ref state, _randomizer)
                .Should()
                .Be(TimeSpan.FromSeconds(0.95));

            _randomizer = () => 0.6;
            RetryHelper.GetRetryDelay(DelayBackoffType.Constant, true, 1, TimeSpan.FromSeconds(1), null, ref state, _randomizer)
                .Should()
                .Be(TimeSpan.FromSeconds(1.05));

            _randomizer = () => 1.0;
            RetryHelper
                .GetRetryDelay(DelayBackoffType.Constant, true, 0, TimeSpan.FromSeconds(1), null, ref state, _randomizer)
                .Should()
                .Be(TimeSpan.FromSeconds(1.25));
        }
    }
}
