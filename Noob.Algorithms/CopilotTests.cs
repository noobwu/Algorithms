// ***********************************************************************
// Assembly         : Noob.Algorithms
// Author           : noob
// Created          : 2023-05-02
//
// Last Modified By : noob
// Last Modified On : 2023-05-02
// ***********************************************************************
// <copyright file="CopilotTests.cs" company="Noob.Algorithms">
//     Copyright (c) . All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// The Algorithms namespace.
/// </summary>
namespace Noob.Algorithms
{
    /// <summary>
    /// Class CopilotTests.
    /// </summary>
    [TestFixture]
    public class CopilotTests
    {
        /// <summary>
        /// 二分查询算法   
        /// </summary>
        /// <param name="array">The array.</param>
        /// <param name="target">The target.</param>
        /// <returns>System.Int32.</returns>
        [TestCaseSource(nameof(BinarySearchSource))]
        public int BinarySearch(int[] array, int target)
        {
            int left = 0;
            int right = array.Length - 1;
            while (left <= right)
            {
                int mid = left + (right - left) / 2;
                if (array[mid] == target)
                {
                    return mid;
                }
                else if (array[mid] < target)
                {
                    left = mid + 1;
                }
                else
                {
                    right = mid - 1;
                }
            }
            return -1;
        }


        /// <summary>
        /// Binaries the search source.
        /// </summary>
        /// <returns>IEnumerable.</returns>
        public static IEnumerable BinarySearchSource() { 
            yield return new TestCaseData(new int[] { 1, 2, 3, 4, 5 }, 3).Returns(2);
            yield return new TestCaseData(new int[] { 1, 2, 3, 4, 5 }, 5).Returns(4);
        }
    }
}
