// ***********************************************************************
// Assembly         : Noob.Algorithms
// Author           : noob
// Created          : 2023-11-25
//
// Last Modified By : noob
// Last Modified On : 2023-11-25
// ***********************************************************************
// <copyright file="RateLimitRule.cs" company="Noob.Algorithms">
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
    /// Class RateLimitRule.
    /// </summary>
    public class RateLimitRule
    {
        /// <summary>
        /// HTTP verb and path
        /// </summary>
        /// <value>The endpoint.</value>
        /// <example>
        /// get:/api/values
        /// *:/api/values
        /// *
        /// </example>
        public string Endpoint { get; set; }
    }
}
