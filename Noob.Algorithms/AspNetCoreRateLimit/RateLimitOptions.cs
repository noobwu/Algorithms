// ***********************************************************************
// Assembly         : Noob.Algorithms
// Author           : noob
// Created          : 2023-11-25
//
// Last Modified By : noob
// Last Modified On : 2023-11-25
// ***********************************************************************
// <copyright file="RateLimitOptions.cs" company="Noob.Algorithms">
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
/// The AspNetCoreRateLimit namespace.
/// </summary>
namespace Noob.Algorithms.AspNetCoreRateLimit
{
    /// <summary>
    /// Class RateLimitOptions.
    /// </summary>
    public class RateLimitOptions
    {
        /// <summary>
        /// HTTP verb and path
        /// </summary>
        /// <value>The endpoint whitelist.</value>
        /// <example>
        /// get:/api/values
        /// *:/api/values
        /// *
        /// </example>
        public List<string> EndpointWhitelist { get; set; }

        /// <summary>
        /// Enabled the comparison logic to use Regex instead of wildcards.
        /// </summary>
        /// <value><c>true</c> if [enable regex rule matching]; otherwise, <c>false</c>.</value>
        public bool EnableRegexRuleMatching { get; set; }
    }
}
