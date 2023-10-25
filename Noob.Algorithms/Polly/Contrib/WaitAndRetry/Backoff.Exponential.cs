// ***********************************************************************
// Assembly         : Noob.Algorithms
// Author           : noob
// Created          : 2023-10-25
//
// Last Modified By : noob
// Last Modified On : 2023-10-25
// ***********************************************************************
// <copyright file="Backoff.Exponential.cs" company="Noob.Algorithms">
//     Copyright (c) . All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// The WaitAndRetry namespace.
/// </summary>
namespace Noob.Algorithms.Polly.Contrib.WaitAndRetry
{
    /// <summary>
    /// Class Backoff.
    /// </summary>
    public partial class Backoff // .Exponential
    {
        /// <summary>
        /// Generates sleep durations in an exponential manner.
        /// The formula used is: Duration = <paramref name="initialDelay" /> x 2^iteration.
        /// For example: 100ms, 200ms, 400ms, 800ms, ...
        /// </summary>
        /// <param name="initialDelay">The duration value for the wait before the first retry.</param>
        /// <param name="retryCount">The maximum number of retries to use, in addition to the original call.</param>
        /// <param name="factor">The exponent to multiply each subsequent duration by.</param>
        /// <param name="fastFirst">Whether the first retry will be immediate or not.</param>
        /// <returns>IEnumerable&lt;TimeSpan&gt;.</returns>
        /// <exception cref="System.ArgumentOutOfRangeException">initialDelay - should be >= 0ms</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">retryCount - should be >= 0</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">factor - should be >= 1.0</exception>
        public static IEnumerable<TimeSpan> ExponentialBackoff(TimeSpan initialDelay, int retryCount, double factor = 2.0, bool fastFirst = false)
        {
            if (initialDelay < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(initialDelay), initialDelay, "should be >= 0ms");
            if (retryCount < 0) throw new ArgumentOutOfRangeException(nameof(retryCount), retryCount, "should be >= 0");
            if (factor < 1.0) throw new ArgumentOutOfRangeException(nameof(factor), factor, "should be >= 1.0");

            if (retryCount == 0)
                return Empty();

            return Enumerate(initialDelay, retryCount, fastFirst, factor);

            IEnumerable<TimeSpan> Enumerate(TimeSpan initial, int retry, bool fast, double f)
            {
                int i = 0;
                if (fast)
                {
                    i++;
                    yield return TimeSpan.Zero;
                }

                double ms = initial.TotalMilliseconds;
                for (; i < retry; i++, ms *= f)
                {
                    yield return TimeSpan.FromMilliseconds(ms);
                }
            }
        }
    }
}
