﻿// ***********************************************************************
// Assembly         : Noob.Algorithms
// Author           : noob
// Created          : 2023-10-25
//
// Last Modified By : noob
// Last Modified On : 2023-10-25
// ***********************************************************************
// <copyright file="Backoff.AwsDecorrelatedJitter.cs" company="Noob.Algorithms">
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
    public partial class Backoff // .AwsDecorrelatedJitter
    {
        /// <summary>
        /// Generates sleep durations in an jittered manner, making sure to mitigate any correlations.
        /// For example: 117ms, 236ms, 141ms, 424ms, ...
        /// Per the formula from https://aws.amazon.com/blogs/architecture/exponential-backoff-and-jitter/.
        /// </summary>
        /// <param name="minDelay">The minimum duration value to use for the wait before each retry.</param>
        /// <param name="maxDelay">The maximum duration value to use for the wait before each retry.</param>
        /// <param name="retryCount">The maximum number of retries to use, in addition to the original call.</param>
        /// <param name="seed">An optional <see cref="Random" /> seed to use.
        /// If not specified, will use a shared instance with a random seed, per Microsoft recommendation for maximum randomness.</param>
        /// <param name="fastFirst">Whether the first retry will be immediate or not.</param>
        /// <returns>IEnumerable&lt;TimeSpan&gt;.</returns>
        /// <exception cref="System.ArgumentOutOfRangeException">minDelay - should be >= 0ms</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">maxDelay - should be >= {minDelay}</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">retryCount - should be >= 0</exception>
        public static IEnumerable<TimeSpan> AwsDecorrelatedJitterBackoff(TimeSpan minDelay, TimeSpan maxDelay, int retryCount, int? seed = null, bool fastFirst = false)
        {
            if (minDelay < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(minDelay), minDelay, "should be >= 0ms");
            if (maxDelay < minDelay) throw new ArgumentOutOfRangeException(nameof(maxDelay), maxDelay, $"should be >= {minDelay}");
            if (retryCount < 0) throw new ArgumentOutOfRangeException(nameof(retryCount), retryCount, "should be >= 0");

            if (retryCount == 0)
                return Empty();

            return Enumerate(minDelay, maxDelay, retryCount, fastFirst, new ConcurrentRandom(seed));

            IEnumerable<TimeSpan> Enumerate(TimeSpan min, TimeSpan max, int retry, bool fast, ConcurrentRandom random)
            {
                int i = 0;
                if (fast)
                {
                    i++;
                    yield return TimeSpan.Zero;
                }

                // https://github.com/aws-samples/aws-arch-backoff-simulator/blob/master/src/backoff_simulator.py#L45
                // self.sleep = min(self.cap, random.uniform(self.base, self.sleep * 3))

                // Formula avoids hard clamping (which empirically results in a bad distribution)
                double ms = min.TotalMilliseconds;
                for (; i < retry; i++)
                {
                    double ceiling = Math.Min(max.TotalMilliseconds, ms * 3);
                    ms = random.Uniform(min.TotalMilliseconds, ceiling);

                    yield return TimeSpan.FromMilliseconds(ms);
                }
            }
        }
    }
}
