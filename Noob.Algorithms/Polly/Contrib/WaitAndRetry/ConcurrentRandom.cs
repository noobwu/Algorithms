// ***********************************************************************
// Assembly         : Noob.Algorithms
// Author           : noob
// Created          : 2023-10-25
//
// Last Modified By : noob
// Last Modified On : 2023-10-25
// ***********************************************************************
// <copyright file="ConcurrentRandom.cs" company="Noob.Algorithms">
//     Copyright (c) . All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// The WaitAndRetry namespace.
/// </summary>
namespace Noob.Algorithms.Polly.Contrib.WaitAndRetry
{
    /// <summary>
    /// A random number generator with a Uniform distribution that is thread-safe (via locking).
    /// Can be instantiated with a custom <see cref="int" /> seed to make it emit deterministically.
    /// </summary>
    public sealed class ConcurrentRandom
    {
        // Singleton approach is per MS best-practices.
        // https://docs.microsoft.com/en-us/dotnet/api/system.random?view=netframework-4.7.2#the-systemrandom-class-and-thread-safety
        // https://stackoverflow.com/a/25448166/
        // Also note that in concurrency testing, using a 'new Random()' for every thread ended up
        // being highly correlated. On NetFx this is maybe due to the same seed somehow being used
        // in each instance, but either way the singleton approach mitigated the problem.

        // For more discussion of different approaches to randomization in concurrent scenarios: https://github.com/App-vNext/Polly/issues/530#issuecomment-439680613

        /// <summary>
        /// The s random
        /// </summary>
        private static readonly Random s_random = new Random();
        /// <summary>
        /// The random
        /// </summary>
        private readonly Random _random;

        /// <summary>
        /// Creates an instance of the <see cref="ConcurrentRandom" /> class.
        /// </summary>
        /// <param name="seed">An optional <see cref="Random" /> seed to use.
        /// If not specified, will use a shared instance with a random seed, per Microsoft recommendation for maximum randomness.</param>
        public ConcurrentRandom(int? seed = null)
        {
            _random = seed == null
                ? s_random // Do not use 'new Random()' here; in concurrent scenarios they could have the same seed
                : new Random(seed.Value);
        }

        /// <summary>
        /// Returns a random floating-point number that is greater than or equal to 0.0,
        /// and less than 1.0.
        /// This method uses locks in order to avoid issues with concurrent access.
        /// </summary>
        /// <returns>System.Double.</returns>
        public double NextDouble()
        {
            // It is safe to lock on _random since it's not exposed
            // to outside use so it cannot be contended.
            lock (_random)
            {
                return _random.NextDouble();
            }
        }

        /// <summary>
        /// Returns a random floating-point number that is greater than or equal to <paramref name="a" />,
        /// and less than <paramref name="b" />.
        /// </summary>
        /// <param name="a">The minimum value.</param>
        /// <param name="b">The maximum value.</param>
        /// <returns>System.Double.</returns>
        public double Uniform(double a, double b)
        {
            Debug.Assert(a <= b);

            if (a == b) return a;

            return a + (b - a) * NextDouble();
        }
    }
}
