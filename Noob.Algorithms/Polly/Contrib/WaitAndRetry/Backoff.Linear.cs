// ***********************************************************************
// Assembly         : Noob.Algorithms
// Author           : noob
// Created          : 2023-10-25
//
// Last Modified By : noob
// Last Modified On : 2023-10-25
// ***********************************************************************
// <copyright file="Backoff.Linear.cs" company="Noob.Algorithms">
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
    public partial class Backoff // .Linear
    {
        /// <summary>
        /// Generates sleep durations in an linear manner.
        /// The formula used is: Duration = <paramref name="initialDelay" /> x (1 + <paramref name="factor" /> x iteration).
        /// For example: 100ms, 200ms, 300ms, 400ms, ...
        /// </summary>
        /// <param name="initialDelay">The duration value for the first retry.</param>
        /// <param name="retryCount">The maximum number of retries to use, in addition to the original call.</param>
        /// <param name="factor">The linear factor to use for increasing the duration on subsequent calls.</param>
        /// <param name="fastFirst">Whether the first retry will be immediate or not.</param>
        /// <returns>IEnumerable&lt;TimeSpan&gt;.</returns>
        /// <exception cref="System.ArgumentOutOfRangeException">initialDelay - should be >= 0ms</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">retryCount - should be >= 0</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">factor - should be >= 0</exception>
        public static IEnumerable<TimeSpan> LinearBackoff(TimeSpan initialDelay, int retryCount, double factor = 1.0, bool fastFirst = false)
        {
            if (initialDelay < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(initialDelay), initialDelay, "should be >= 0ms");
            if (retryCount < 0) throw new ArgumentOutOfRangeException(nameof(retryCount), retryCount, "should be >= 0");
            if (factor < 0) throw new ArgumentOutOfRangeException(nameof(factor), factor, "should be >= 0");

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
                double ad = f * ms;

                for (; i < retry; i++, ms += ad)
                {
                    yield return TimeSpan.FromMilliseconds(ms);
                }
            }
        }
    }
}
