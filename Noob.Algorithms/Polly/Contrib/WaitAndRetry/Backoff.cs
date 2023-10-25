// ***********************************************************************
// Assembly         : Noob.Algorithms
// Author           : noob
// Created          : 2023-10-25
//
// Last Modified By : noob
// Last Modified On : 2023-10-25
// ***********************************************************************
// <copyright file="Backoff.cs" company="Noob.Algorithms">
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
    /// Helper methods for creating backoff strategies.
    /// </summary>
    public static partial class Backoff
    {
        /// <summary>
        /// Empties this instance.
        /// </summary>
        /// <returns>IEnumerable&lt;TimeSpan&gt;.</returns>
        private static IEnumerable<TimeSpan> Empty()
        {
            yield break;
        }
    }
}
