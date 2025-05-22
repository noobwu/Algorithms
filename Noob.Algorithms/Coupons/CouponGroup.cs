// ***********************************************************************
// Assembly         : Noob.Algorithms
// Author           : noob
// Created          : 2025-05-21
//
// Last Modified By : noob
// Last Modified On : 2025-05-21
// ***********************************************************************
// <copyright file="CouponGroup.cs" company="Noob.Algorithms">
//     Copyright (c) . All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Noob.Algorithms.Coupons
{
    /// <summary>
    /// Class CouponGroup.
    /// </summary>
    public class CouponGroup
    {
        /// <summary>
        /// Gets or sets the group key.
        /// </summary>
        /// <value>The group key.</value>
        public string GroupKey { get; set; }
        /// <summary>
        /// Gets or sets the coupons.
        /// </summary>
        /// <value>The coupons.</value>
        public List<Coupon> Coupons { get; set; } = new();
        /// <summary>
        /// Gets or sets the items.
        /// </summary>
        /// <value>The items.</value>
        public List<OrderItem> Items { get; set; } = new();
    }

}
