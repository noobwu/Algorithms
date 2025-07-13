// ***********************************************************************
// Assembly         : Noob.Algorithms
// Author           : noob
// Created          : 2025-07-13
//
// Last Modified By : noob
// Last Modified On : 2025-07-13
// ***********************************************************************
// <copyright file="BubbleSorterTests.cs" company="Noob.Algorithms">
//     Copyright (c) . All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace Noob.Algorithms.Sorts
{
    /// <summary>
    /// 通用冒泡排序家族算法工具（Bubble/Cocktail/OddEven）
    /// </summary>
    public static class BubbleSorter
    {
        /// <summary>
        /// 标准冒泡排序（Bubble Sort），原地升序排序，支持自定义比较器
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list">The list.</param>
        /// <param name="comparer">The comparer.</param>
        /// <exception cref="System.ArgumentNullException">list</exception>
        public static void BubbleSort<T>(IList<T> list, IComparer<T> comparer = null)
        {
            if (list == null) throw new ArgumentNullException(nameof(list));
            comparer ??= Comparer<T>.Default;
            int n = list.Count;
            if (n < 2) return;
            bool swapped;
            for (int i = 0; i < n - 1; i++)
            {
                swapped = false;
                for (int j = 0; j < n - i - 1; j++)
                {
                    if (comparer.Compare(list[j], list[j + 1]) > 0)
                    {
                        Swap(list, j, j + 1);
                        swapped = true;
                    }
                }
                if (!swapped) break;
            }
        }

        /// <summary>
        /// 鸡尾酒排序（Cocktail Sort/Bidirectional Bubble Sort），双向遍历提升有序数组效率
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list">The list.</param>
        /// <param name="comparer">The comparer.</param>
        /// <exception cref="System.ArgumentNullException">list</exception>
        public static void CocktailSort<T>(IList<T> list, IComparer<T> comparer = null)
        {
            if (list == null) throw new ArgumentNullException(nameof(list));
            comparer ??= Comparer<T>.Default;
            int n = list.Count;
            if (n < 2) return;
            bool swapped = true;
            int start = 0, end = n - 1;
            while (swapped)
            {
                swapped = false;
                for (int i = start; i < end; i++)
                {
                    if (comparer.Compare(list[i], list[i + 1]) > 0)
                    {
                        Swap(list, i, i + 1);
                        swapped = true;
                    }
                }
                if (!swapped) break;
                swapped = false;
                end--;
                for (int i = end - 1; i >= start; i--)
                {
                    if (comparer.Compare(list[i], list[i + 1]) > 0)
                    {
                        Swap(list, i, i + 1);
                        swapped = true;
                    }
                }
                start++;
            }
        }

        /// <summary>
        /// 奇偶冒泡排序（Odd-Even Sort），适用于并行计算场景
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list">The list.</param>
        /// <param name="comparer">The comparer.</param>
        /// <exception cref="System.ArgumentNullException">list</exception>
        public static void OddEvenSort<T>(IList<T> list, IComparer<T> comparer = null)
        {
            if (list == null) throw new ArgumentNullException(nameof(list));
            comparer ??= Comparer<T>.Default;
            int n = list.Count;
            if (n < 2) return;
            bool sorted = false;
            while (!sorted)
            {
                sorted = true;
                // 奇数位比较
                for (int i = 1; i < n - 1; i += 2)
                {
                    if (comparer.Compare(list[i], list[i + 1]) > 0)
                    {
                        Swap(list, i, i + 1);
                        sorted = false;
                    }
                }
                // 偶数位比较
                for (int i = 0; i < n - 1; i += 2)
                {
                    if (comparer.Compare(list[i], list[i + 1]) > 0)
                    {
                        Swap(list, i, i + 1);
                        sorted = false;
                    }
                }
            }
        }

        /// <summary>
        /// 元素交换辅助函数
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list">The list.</param>
        /// <param name="i">The i.</param>
        /// <param name="j">The j.</param>
        private static void Swap<T>(IList<T> list, int i, int j)
        {
            if (i == j) return;
            T tmp = list[i];
            list[i] = list[j];
            list[j] = tmp;
        }
    }



    /// <summary>
    /// 冒泡排序家族（Bubble/Cocktail/OddEven）单元测试
    /// </summary>
    [TestFixture]
    public class BubbleSorterTests
    {
        /// <summary>
        /// 标准冒泡排序：基础升序用例
        /// </summary>
        [Test]
        public void BubbleSort_IntList_ShouldSortAscending()
        {
            var arr = new List<int> { 5, 2, 9, 4, 2, 8 };
            BubbleSorter.BubbleSort(arr);
            Assert.That(arr, Is.EqualTo(new[] { 2, 2, 4, 5, 8, 9 }));
        }

        /// <summary>
        /// 鸡尾酒排序：升序结果一致性
        /// </summary>
        [Test]
        public void CocktailSort_IntList_ShouldSortAscending()
        {
            var arr = new List<int> { 8, 7, 4, 9, 3, 1 };
            BubbleSorter.CocktailSort(arr);
            Assert.That(arr, Is.EqualTo(new[] { 1, 3, 4, 7, 8, 9 }));
        }

        /// <summary>
        /// 奇偶冒泡排序：升序结果一致性
        /// </summary>
        [Test]
        public void OddEvenSort_IntList_ShouldSortAscending()
        {
            var arr = new List<int> { 7, 2, 6, 1, 5 };
            BubbleSorter.OddEvenSort(arr);
            Assert.That(arr, Is.EqualTo(new[] { 1, 2, 5, 6, 7 }));
        }

        /// <summary>
        /// 逆序数组（降序排序）
        /// </summary>
        [Test]
        public void BubbleSort_CustomComparer_ShouldSortDescending()
        {
            var arr = new List<int> { 1, 2, 3, 4 };
            BubbleSorter.BubbleSort(arr, Comparer<int>.Create((a, b) => b.CompareTo(a)));
            Assert.That(arr, Is.EqualTo(new[] { 4, 3, 2, 1 }));
        }

        /// <summary>
        /// 所有元素相等
        /// </summary>
        [Test]
        public void BubbleSort_AllEqual_ShouldRemainUnchanged()
        {
            var arr = new List<int> { 5, 5, 5, 5 };
            BubbleSorter.BubbleSort(arr);
            Assert.That(arr, Is.EqualTo(new[] { 5, 5, 5, 5 }));
        }

        /// <summary>
        /// 空数组和单元素
        /// </summary>
        [Test]
        public void BubbleSort_EmptyAndSingle_ShouldNotThrow()
        {
            var empty = new List<int>();
            BubbleSorter.BubbleSort(empty);
            Assert.That(empty, Is.Empty);

            var one = new List<int> { 42 };
            BubbleSorter.BubbleSort(one);
            Assert.That(one, Is.EqualTo(new[] { 42 }));
        }

        /// <summary>
        /// 鸡尾酒排序与标准冒泡结果一致
        /// </summary>
        [Test]
        public void CocktailSort_EqualsBubbleSort()
        {
            var arr1 = new List<int> { 10, 8, 6, 4, 2 };
            var arr2 = arr1.ToList();
            BubbleSorter.BubbleSort(arr1);
            BubbleSorter.CocktailSort(arr2);
            Assert.That(arr1, Is.EqualTo(arr2));
        }

        /// <summary>
        /// 奇偶冒泡排序与标准冒泡结果一致
        /// </summary>
        [Test]
        public void OddEvenSort_EqualsBubbleSort()
        {
            var arr1 = new List<int> { 11, 3, 7, 2, 5 };
            var arr2 = arr1.ToList();
            BubbleSorter.BubbleSort(arr1);
            BubbleSorter.OddEvenSort(arr2);
            Assert.That(arr1, Is.EqualTo(arr2));
        }

        /// <summary>
        /// 自定义对象排序，确保可扩展性和可用性
        /// </summary>
        private class Person { public int Age; public string Name; }
        /// <summary>
        /// Defines the test method BubbleSort_CustomObjects_ShouldSortByAge.
        /// </summary>
        [Test]
        public void BubbleSort_CustomObjects_ShouldSortByAge()
        {
            var people = new List<Person>
            {
                new Person{Age=30,Name="Alice"},
                new Person{Age=24,Name="Bob"},
                new Person{Age=40,Name="Eve"}
            };
            BubbleSorter.BubbleSort(people, Comparer<Person>.Create((a, b) => a.Age.CompareTo(b.Age)));
            Assert.That(people.Select(x => x.Age), Is.EqualTo(new[] { 24, 30, 40 }));
        }
    }


}
