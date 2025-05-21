// ***********************************************************************
// Assembly         : Noob.Algorithms.ConsoleApp
// Author           : noob
// Created          : 2025-05-21
//
// Last Modified By : noob
// Last Modified On : 2025-05-21
// ***********************************************************************
// <copyright file="FactorialBenchmarks.cs" company="Noob.Algorithms.ConsoleApp">
//     Copyright (c) . All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************
using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Noob.Algorithms.ConsoleApp
{
    /// <summary>
    /// Class FactorialBenchmarks.
    /// </summary>
    public class FactorialBenchmarks
    {
        /// <summary>
        /// The n
        /// </summary>
        [Params(10, 20, 50, 100)] // 你可以调整更大或更小
        public int N;

        /// <summary>
        /// Recursives this instance.
        /// </summary>
        /// <returns>System.Int64.</returns>
        [Benchmark]
        public long Recursive() => RecursionSamples.FactorialRecursive(N);

        /// <summary>
        /// Tails the recursive.
        /// </summary>
        /// <returns>System.Int64.</returns>
        [Benchmark]
        public long TailRecursive() => RecursionSamples.FactorialTailRecursive(N);

        /// <summary>
        /// Iteratives this instance.
        /// </summary>
        /// <returns>System.Int64.</returns>
        [Benchmark]
        public long Iterative() => RecursionSamples.FactorialIterative(N);
    }
}
