// ***********************************************************************
// Assembly         : Noob.DataStructures
// Author           : noob
// Created          : 2023-01-21
//
// Last Modified By : noob
// Last Modified On : 2023-01-21
// ***********************************************************************
// <copyright file="MaxSubTests.cs" company="Noob.DataStructures">
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
/// The DataStructures namespace.
/// </summary>
namespace Noob.DataStructures
{
    /// <summary>
    /// Class MaxSubTests.
    /// </summary>
    [TestFixture]
    public class MaxSubTests
    {
        /// <summary>
        /// 三重循环(总和最大区间)
        /// https://www.youtube.com/watch?v=ohHWQf1HDfU
        /// </summary>
        /// <param name="arr">The arr.</param>
        /// <param name="n">The n.</param>
        [TestCaseSource(nameof(MaxSumSubarraySource))]
        public void MaxSumSubarray1(double[] arr)
        {
            Assert.That(arr != null && arr.Length > 0); //时间复杂度 O(n^3)
            int n = arr.Length;
            double maxSum = double.MinValue;
            for (int subArraySize = 1; subArraySize <= n; ++subArraySize)  //O(n)
            {
                for (int startIndex = 0; startIndex < n; ++startIndex) //O(n)
                {
                    if (startIndex + subArraySize > n)
                    {
                        break;//子数组超过了数组的界限
                    }
                    double sum =0;
                    for (int i = startIndex; i < (startIndex + subArraySize); i++) //O(n)
                    {
                        sum = arr[i];
                    }
                    maxSum = Math.Max(maxSum, sum);
                }
            }
            Console.WriteLine($"三重循环(总和最大区间)，maxSum:{maxSum},arr:{string.Join(",",arr)}");
        }
        /// <summary>
        /// 两重重循环(总和最大区间)
        /// https://www.youtube.com/watch?v=ohHWQf1HDfU
        /// </summary>
        /// <param name="arr">The arr.</param>
        /// <param name="n">The n.</param>
        [TestCaseSource(nameof(MaxSumSubarraySource))]
        public void MaxSumSubarray2(double[] arr)
        {
            Assert.That(arr != null && arr.Length > 0); //时间复杂度 O(n^2)
            int n = arr.Length;
            double maxSum = double.MinValue;
            for (int startIndex = 0; startIndex < n; ++startIndex) //O(n)
            {
                double sum = 0;
                for (int subArraySize = 1; subArraySize <= n; ++subArraySize)  //O(n)
                {
                    if (startIndex + subArraySize > n)
                    {
                        break;//子数组超过了数组的界限
                    }
                    sum += arr[startIndex + subArraySize - 1];
                    maxSum = Math.Max(maxSum, sum);
                }
            }
            Console.WriteLine($"两重重循环(总和最大区间),maxSum:{maxSum}，arr:{string.Join(",", arr)}");
        }

        /// <summary>
        /// 分治(总和最大区间)
        /// https://www.youtube.com/watch?v=ohHWQf1HDfU
        /// </summary>
        /// <param name="arr">The arr.</param>
        /// <param name="n">The n.</param>
        [TestCaseSource(nameof(MaxSumSubarraySource))]
        public void MaxSumSubarray3(double[] arr)
        {
            Assert.That(arr != null && arr.Length > 0); 
            int n = arr.Length;
            double maxSum = MaxSumSubarrayByDivide(arr,n);
            Console.WriteLine($"分治(总和最大区间),maxSum:{maxSum}，arr:{string.Join(",", arr)}");
        }

        /// <summary>
        /// Maximums the sum subarray by divide.
        /// </summary>
        /// <param name="arr">The arr.</param>
        /// <param name="n">The n.</param>
        /// <returns>double.</returns>
        /// <exception cref="ArgumentException">nameof(arr)</exception>
        /// <exception cref="ArgumentException">nameof(n)</exception>
        private double MaxSumSubarrayByDivide(double[] arr, int n) {
            if (arr == null || arr.Length == 0) {
                throw new  ArgumentException(nameof(arr));
            }
            if (n < 1 || n > arr.Length) {
                throw new ArgumentException(nameof(n));
            }
            if (n == 1)
            {
                return arr[0];
            }
            int m = n / 2;
            double leftMaxSum = MaxSumSubarrayByDivide(arr, m);
            double rightMaxSum = MaxSumSubarrayByDivide(arr, n-m);
            double leftSum = double.MinValue,rightSum=double.MinValue,sum=0d;
            for (int i = m; i < n; i++)
            {
                sum += arr[i];
                rightSum=Math.Max(rightSum, sum);
            }
            sum = 0d;
            for (int i = (m-1); i >=0; i--)
            {
                sum += arr[i];
                leftSum = Math.Max(leftSum, sum);
            }
            double maxSum = Math.Max(leftMaxSum,rightMaxSum);
            maxSum = Math.Max(maxSum,leftSum+rightSum);
            return maxSum;
        }

        /// <summary>
        /// Kadane算法(总和最大区间)
        /// https://zh.wikipedia.org/wiki/%E6%9C%80%E5%A4%A7%E5%AD%90%E6%95%B0%E5%88%97%E9%97%AE%E9%A2%98
        /// https://www.youtube.com/watch?v=ohHWQf1HDfU
        /// </summary>
        /// <param name="arr">The arr.</param>
        /// <param name="n">The n.</param>
        [TestCaseSource(nameof(MaxSumSubarraySource))]
        public void MaxSumSubarray4(double[] arr)
        {
            Assert.That(arr != null && arr.Length > 0);
            int n = arr.Length;
            double maxSum =double.MinValue,sum=0d;
            for (int i = 0; i < n; i++)
            {
                if (sum + arr[i] > 0)
                {
                    sum += arr[i];
                }
                else {
                    sum = 0;
                }
                maxSum=Math.Max(maxSum,sum);
            }
            Console.WriteLine($"Kadane算法(总和最大区间),maxSum:{maxSum}，arr:{string.Join(",", arr)}");
        }

        /// <summary>
        /// Maximums the sum subarray source.
        /// </summary>
        /// <returns>IEnumerable.</returns>
        public static IEnumerable MaxSumSubarraySource()
        {
            yield return new TestCaseData(new double[] { 1.5, -12.3, 3.2, -5.5, 23.2, 3.2, -1.4, -12.2, 34.2, 5.4, -7.8, 1.1, -4.9 });
        }
    }
}
