// ***********************************************************************
// Assembly         : Noob.Algorithms
// Author           : noob
// Created          : 2023-10-21
// SourceLink       : https://github.com/App-vNext/Polly/blob/main/test/Polly.Core.Tests/Retry/RetryHelperTests.cs
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
using Noob.Algorithms.Polly.Contrib.WaitAndRetry;
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
        [TestCase]
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

        /// <summary>
        /// Linears the ok.
        /// </summary>
        [TestCase]
        public void Linear_Ok()
        {
            double state = 0;

            RetryHelper.GetRetryDelay(DelayBackoffType.Linear, false, 0, TimeSpan.Zero, null, ref state, _randomizer).Should().Be(TimeSpan.Zero);
            RetryHelper.GetRetryDelay(DelayBackoffType.Linear, false, 1, TimeSpan.Zero, null, ref state, _randomizer).Should().Be(TimeSpan.Zero);
            RetryHelper.GetRetryDelay(DelayBackoffType.Linear, false, 2, TimeSpan.Zero, null, ref state, _randomizer).Should().Be(TimeSpan.Zero);

            RetryHelper.GetRetryDelay(DelayBackoffType.Linear, false, 0, TimeSpan.FromSeconds(1), null, ref state, _randomizer).Should().Be(TimeSpan.FromSeconds(1));
            RetryHelper.GetRetryDelay(DelayBackoffType.Linear, false, 1, TimeSpan.FromSeconds(1), null, ref state, _randomizer).Should().Be(TimeSpan.FromSeconds(2));
            RetryHelper.GetRetryDelay(DelayBackoffType.Linear, false, 2, TimeSpan.FromSeconds(1), null, ref state, _randomizer).Should().Be(TimeSpan.FromSeconds(3));
        }

        /// <summary>
        /// Defines the test method Linear_Jitter_Ok.
        /// </summary>
        [TestCase]
        public void Linear_Jitter_Ok()
        {
            double state = 0;

            RetryHelper.GetRetryDelay(DelayBackoffType.Linear, true, 0, TimeSpan.Zero, null, ref state, _randomizer).Should().Be(TimeSpan.Zero);
            RetryHelper.GetRetryDelay(DelayBackoffType.Linear, true, 1, TimeSpan.Zero, null, ref state, _randomizer).Should().Be(TimeSpan.Zero);
            RetryHelper.GetRetryDelay(DelayBackoffType.Linear, true, 2, TimeSpan.Zero, null, ref state, _randomizer).Should().Be(TimeSpan.Zero);

            _randomizer = () => 0.0;
            RetryHelper
                .GetRetryDelay(DelayBackoffType.Linear, true, 2, TimeSpan.FromSeconds(1), null, ref state, _randomizer)
                .Should()
                .Be(TimeSpan.FromSeconds(2.25));

            _randomizer = () => 0.4;
            RetryHelper
                .GetRetryDelay(DelayBackoffType.Linear, true, 2, TimeSpan.FromSeconds(1), null, ref state, _randomizer)
                .Should()
                .Be(TimeSpan.FromSeconds(2.85));

            _randomizer = () => 0.5;
            RetryHelper
                .GetRetryDelay(DelayBackoffType.Linear, true, 2, TimeSpan.FromSeconds(1), null, ref state, _randomizer)
                .Should()
                .Be(TimeSpan.FromSeconds(3));

            _randomizer = () => 0.6;
            RetryHelper.GetRetryDelay(DelayBackoffType.Linear, true, 2, TimeSpan.FromSeconds(1), null, ref state, _randomizer)
                .Should()
                .Be(TimeSpan.FromSeconds(3.15));

            _randomizer = () => 1.0;
            RetryHelper
                .GetRetryDelay(DelayBackoffType.Linear, true, 2, TimeSpan.FromSeconds(1), null, ref state, _randomizer)
                .Should()
                .Be(TimeSpan.FromSeconds(3.75));
        }

        /// <summary>
        /// Exponentials the ok.
        /// </summary>
        [TestCase]
        public void Exponential_Ok()
        {
            double state = 0;

            RetryHelper.GetRetryDelay(DelayBackoffType.Exponential, false, 0, TimeSpan.Zero, null, ref state, _randomizer).Should().Be(TimeSpan.Zero);
            RetryHelper.GetRetryDelay(DelayBackoffType.Exponential, false, 1, TimeSpan.Zero, null, ref state, _randomizer).Should().Be(TimeSpan.Zero);
            RetryHelper.GetRetryDelay(DelayBackoffType.Exponential, false, 2, TimeSpan.Zero, null, ref state, _randomizer).Should().Be(TimeSpan.Zero);

            RetryHelper.GetRetryDelay(DelayBackoffType.Exponential, false, 0, TimeSpan.FromSeconds(1), null, ref state, _randomizer).Should().Be(TimeSpan.FromSeconds(1));
            RetryHelper.GetRetryDelay(DelayBackoffType.Exponential, false, 1, TimeSpan.FromSeconds(1), null, ref state, _randomizer).Should().Be(TimeSpan.FromSeconds(2));
            RetryHelper.GetRetryDelay(DelayBackoffType.Exponential, false, 2, TimeSpan.FromSeconds(1), null, ref state, _randomizer).Should().Be(TimeSpan.FromSeconds(4));
        }

        /// <summary>
        /// Defines the test method MaxDelay_Ok.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="jitter">if set to <c>true</c> [jitter].</param>
        [TestCase(DelayBackoffType.Linear, false)]
        [TestCase(DelayBackoffType.Exponential, false)]
        [TestCase(DelayBackoffType.Constant, false)]
        [TestCase(DelayBackoffType.Linear, true)]
        [TestCase(DelayBackoffType.Exponential, true)]
        [TestCase(DelayBackoffType.Constant, true)]
        [Theory]
        public void MaxDelay_Ok(DelayBackoffType type, bool jitter)
        {
            _randomizer = () => 0.5;
            var expected = TimeSpan.FromSeconds(1);
            double state = 0;

            RetryHelper.GetRetryDelay(type, jitter, 2, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(1), ref state, _randomizer).Should().Be(expected);
        }

        /// <summary>
        /// Maximums the delay delay less than maximum delay respected.
        /// </summary>
        [Test]
        public void MaxDelay_DelayLessThanMaxDelay_Respected()
        {
            double state = 0;
            var expected = TimeSpan.FromSeconds(1);

            RetryHelper.GetRetryDelay(DelayBackoffType.Constant, false, 2, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), ref state, _randomizer).Should().Be(expected);
        }

        /// <summary>
        /// Gets the retry delay overflow returns maximum time span.
        /// </summary>
        [Test]
        public void GetRetryDelay_Overflow_ReturnsMaxTimeSpan()
        {
            double state = 0;

            RetryHelper.GetRetryDelay(DelayBackoffType.Exponential, false, 1000, TimeSpan.FromDays(1), null, ref state, _randomizer).Should().Be(TimeSpan.MaxValue);
        }
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(10)]
        [TestCase(100)]
        [TestCase(1000)]
        [Theory]
        public void ExponentialWithJitter_Ok(int count)
        {
            var delay = TimeSpan.FromSeconds(7.8);
            var oldDelays = GetExponentialWithJitterBackoff(true, delay, count);
            var newDelays = GetExponentialWithJitterBackoff(false, delay, count);

            newDelays.Should().ContainInConsecutiveOrder(oldDelays);
            newDelays.Should().HaveCount(oldDelays.Count);
        }
        /// <summary>
        /// Defines the test method ExponentialWithJitter_EnsureRandomness.
        /// </summary>
        [Test]
        public void ExponentialWithJitter_EnsureRandomness()
        {
            var delay = TimeSpan.FromSeconds(7.8);
            var delays1 = GetExponentialWithJitterBackoff(false, delay, 100, RandomUtil.Instance.NextDouble);
            var delays2 = GetExponentialWithJitterBackoff(false, delay, 100, RandomUtil.Instance.NextDouble);

            delays1.SequenceEqual(delays2).Should().BeFalse();
        }

        /// <summary>
        /// Gets the exponential with jitter backoff.
        /// </summary>
        /// <param name="contrib">if set to <c>true</c> [contrib].</param>
        /// <param name="baseDelay">The base delay.</param>
        /// <param name="retryCount">The retry count.</param>
        /// <param name="randomizer">The randomizer.</param>
        /// <returns>IReadOnlyList&lt;TimeSpan&gt;.</returns>
        private static IReadOnlyList<TimeSpan> GetExponentialWithJitterBackoff(bool contrib, TimeSpan baseDelay, int retryCount, Func<double>? randomizer = null)
        {
            if (contrib)
            {
                return Backoff.DecorrelatedJitterBackoffV2(baseDelay, retryCount, 0, false).Take(retryCount).ToArray();
            }

            var random = randomizer ?? new RandomUtil(0).NextDouble;
            double state = 0;
            var result = new List<TimeSpan>();

            for (int i = 0; i < retryCount; i++)
            {
                result.Add(RetryHelper.GetRetryDelay(DelayBackoffType.Exponential, true, i, baseDelay, null, ref state, random));
            }

            return result;
        }
    }
}
