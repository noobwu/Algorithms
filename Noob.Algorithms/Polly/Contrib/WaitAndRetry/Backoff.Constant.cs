// ***********************************************************************
// Assembly         : Noob.Algorithms
// Author           : noob
// Created          : 2023-10-25
//
// Last Modified By : noob
// Last Modified On : 2023-10-25
// ***********************************************************************
// <copyright file="Backoff.Constant.cs" company="Noob.Algorithms">
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
    public partial class Backoff // .Constant
    {
        /// <summary>
        /// Generates sleep durations as a constant value.
        /// The formula used is: Duration = <paramref name="delay" />.
        /// For example: 200ms, 200ms, 200ms, ...
        /// </summary>
        /// <param name="delay">The constant wait duration before each retry.</param>
        /// <param name="retryCount">The maximum number of retries to use, in addition to the original call.</param>
        /// <param name="fastFirst">Whether the first retry will be immediate or not.</param>
        /// <returns>IEnumerable&lt;TimeSpan&gt;.</returns>
        /// <exception cref="System.ArgumentOutOfRangeException">delay - should be >= 0ms</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">retryCount - should be >= 0</exception>
        public static IEnumerable<TimeSpan> ConstantBackoff(TimeSpan delay, int retryCount, bool fastFirst = false)
        {
            if (delay < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(delay), delay, "should be >= 0ms");
            if (retryCount < 0) throw new ArgumentOutOfRangeException(nameof(retryCount), retryCount, "should be >= 0");

            if (retryCount == 0)
                return Empty();

            return Enumerate(delay, retryCount, fastFirst);

            IEnumerable<TimeSpan> Enumerate(TimeSpan timeSpan, int retry, bool fast)
            {
                int i = 0;
                if (fast)
                {
                    i++;
                    yield return TimeSpan.Zero;
                }

                for (; i < retry; i++)
                {
                    yield return timeSpan;
                }
            }
        }
    }
}
