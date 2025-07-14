using NUnit.Framework;
using NUnit.Framework.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Noob.Algorithms.Sorts
{

    /// <summary>
    /// 平台通用选择排序（Selection Sort）算法工具
    /// </summary>
    public static class SelectionSorter
    {
        /// <summary>
        /// 对输入列表进行原地选择排序（升序，支持自定义比较器）
        /// </summary>
        /// <typeparam name="T">元素类型</typeparam>
        /// <param name="list">待排序的列表</param>
        /// <param name="comparer">可选比较器，默认升序</param>
        public static void SelectionSort<T>(IList<T> list, IComparer<T> comparer = null)
        {
            if (list == null) throw new ArgumentNullException(nameof(list));
            comparer ??= Comparer<T>.Default;
            int n = list.Count;
            if (n < 2) return;
            for (int i = 0; i < n - 1; i++)
            {
                int minIdx = i;
                for (int j = i + 1; j < n; j++)
                {
                    if (comparer.Compare(list[j], list[minIdx]) < 0)
                        minIdx = j;
                }
                if (minIdx != i)
                    Swap(list, i, minIdx);
            }
        }

        /// <summary>
        /// 元素交换辅助函数
        /// </summary>
        private static void Swap<T>(IList<T> list, int i, int j)
        {
            if (i == j) return;
            T tmp = list[i];
            list[i] = list[j];
            list[j] = tmp;
        }
    }

    /// <summary>
    /// SelectionSorter 平台选择排序单元测试
    /// </summary>
    [TestFixture]
    public class SelectionSorterTests
    {
        /// <summary>
        /// 基本升序排序
        /// </summary>
        [Test]
        public void SelectionSort_IntList_ShouldSortAscending()
        {
            var arr = new List<int> { 5, 2, 9, 4, 2, 8 };
            SelectionSorter.SelectionSort(arr);
            Assert.That(arr, Is.EqualTo(new[] { 2, 2, 4, 5, 8, 9 }));
        }

        /// <summary>
        /// 降序排序
        /// </summary>
        [Test]
        public void SelectionSort_CustomComparer_ShouldSortDescending()
        {
            var arr = new List<int> { 1, 3, 2, 4 };
            SelectionSorter.SelectionSort(arr, Comparer<int>.Create((a, b) => b.CompareTo(a)));
            Assert.That(arr, Is.EqualTo(new[] { 4, 3, 2, 1 }));
        }

        /// <summary>
        /// 所有元素相等时，结果不变（但不保证稳定性）
        /// </summary>
        [Test]
        public void SelectionSort_AllEqual_ShouldRemainUnchanged()
        {
            var arr = new List<int> { 7, 7, 7, 7 };
            SelectionSorter.SelectionSort(arr);
            Assert.That(arr, Is.EqualTo(new[] { 7, 7, 7, 7 }));
        }

        /// <summary>
        /// 空列表和单元素，排序后应无变化
        /// </summary>
        [Test]
        public void SelectionSort_EmptyAndSingle_ShouldNotThrow()
        {
            var empty = new List<int>();
            SelectionSorter.SelectionSort(empty);
            Assert.That(empty, Is.Empty);

            var one = new List<int> { 42 };
            SelectionSorter.SelectionSort(one);
            Assert.That(one, Is.EqualTo(new[] { 42 }));
        }

        /// <summary>
        /// 泛型对象排序，按属性升序
        /// </summary>
        private class Person { public int Age; public string Name; }
        [Test]
        public void SelectionSort_CustomObjects_ShouldSortByAge()
        {
            var people = new List<Person>
            {
                new Person{Age=30,Name="A"},
                new Person{Age=24,Name="B"},
                new Person{Age=40,Name="C"}
            };
            SelectionSorter.SelectionSort(people, Comparer<Person>.Create((a, b) => a.Age.CompareTo(b.Age)));
            Assert.That(people.Select(x => x.Age), Is.EqualTo(new[] { 24, 30, 40 }));
        }

        /// <summary>
        /// 已有序数组应不变
        /// </summary>
        [Test]
        public void SelectionSort_AlreadySorted_ShouldRemainUnchanged()
        {
            var arr = new List<int> { 1, 2, 3, 4, 5 };
            SelectionSorter.SelectionSort(arr);
            Assert.That(arr, Is.EqualTo(new[] { 1, 2, 3, 4, 5 }));
        }

        /// <summary>
        /// 逆序数组应完全反转为升序
        /// </summary>
        [Test]
        public void SelectionSort_Reversed_ShouldBeSorted()
        {
            var arr = new List<int> { 5, 4, 3, 2, 1 };
            SelectionSorter.SelectionSort(arr);
            Assert.That(arr, Is.EqualTo(new[] { 1, 2, 3, 4, 5 }));
        }
    }

}
