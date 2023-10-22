// ***********************************************************************
// Assembly         : Noob.Algorithms
// Author           : noob
// Created          : 2023-10-22
//
// Last Modified By : noob
// Last Modified On : 2023-10-22
// ***********************************************************************
// <copyright file="RandomUtil.cs" company="Noob.Algorithms">
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
/// The Utils namespace.
/// </summary>
namespace Noob.Algorithms.Polly.Utils
{
#pragma warning disable CA1001 // Types that own disposable fields should be disposable
#pragma warning disable CA5394 // Do not use insecure randomness
#pragma warning disable S2931 // Classes with "IDisposable" members should implement "IDisposable"

    /// <summary>
    /// Class RandomUtil. This class cannot be inherited.
    /// </summary>
    public sealed class RandomUtil
    {
        /// <summary>
        /// The random
        /// </summary>
        private readonly ThreadLocal<Random> _random;

        /// <summary>
        /// The instance
        /// </summary>
        public static readonly RandomUtil Instance = new(null);

        /// <summary>
        /// Initializes a new instance of the <see cref="RandomUtil"/> class.
        /// </summary>
        /// <param name="seed">The seed.</param>
        public RandomUtil(int? seed) => _random = new ThreadLocal<Random>(() => seed == null ? new Random() : new Random(seed.Value));

        /// <summary>
        /// Next the double.
        /// </summary>
        /// <returns>System.Double.</returns>
        public double NextDouble() => _random.Value!.NextDouble();
    }
}
