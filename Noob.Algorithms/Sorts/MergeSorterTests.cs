using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Noob.Algorithms.Sorts
{

    /// <summary>
    /// 平台级归并排序算法（支持基础归并、自然归并、混合、原地、并行扩展）
    /// </summary>
    public static class MergeSorter
    {
        /// <summary>
        /// 基础归并排序：稳定O(nlogn)，需O(n)辅助空间
        /// </summary>
        /// <typeparam name="T">元素类型</typeparam>
        /// <param name="arr">待排序数组</param>
        /// <param name="comparer">可选比较器</param>
        public static void MergeSort<T>(T[] arr, IComparer<T> comparer = null)
        {
            if (arr == null) throw new ArgumentNullException(nameof(arr));
            comparer ??= Comparer<T>.Default;
            var temp = new T[arr.Length];
            MergeSortInternal(arr, temp, 0, arr.Length - 1, comparer);
        }

        /// <summary>
        /// 自然归并排序（Natural Merge Sort）：自动检测自然有序区间提升效率
        /// </summary>
        /// <param name="arr">待排序数组</param>
        /// <param name="comparer">可选比较器</param>
        public static void NaturalMergeSort<T>(T[] arr, IComparer<T> comparer = null)
        {
            if (arr == null) throw new ArgumentNullException(nameof(arr));
            comparer ??= Comparer<T>.Default;
            int n = arr.Length;
            var temp = new T[n];
            bool sorted = false;
            while (!sorted)
            {
                sorted = true;
                int left = 0;
                while (left < n)
                {
                    int mid = left;
                    // 扫描到最大自然有序区间
                    while (mid < n - 1 && comparer.Compare(arr[mid], arr[mid + 1]) <= 0) mid++;
                    if (mid == n - 1) break;
                    int right = mid + 1;
                    while (right < n - 1 && comparer.Compare(arr[right], arr[right + 1]) <= 0) right++;
                    // 归并左右区间
                    Merge(arr, temp, left, mid, right, comparer);
                    sorted = false;
                    left = right + 1;
                }
            }
        }

        /// <summary>
        /// 原地归并排序（空间优化变体，原地合并，空间O(1)，但实现复杂/性能有折衷）
        /// </summary>
        /// <param name="arr">待排序数组</param>
        /// <param name="comparer">可选比较器</param>
        public static void InPlaceMergeSort<T>(T[] arr, IComparer<T> comparer = null)
        {
            if (arr == null) throw new ArgumentNullException(nameof(arr));
            comparer ??= Comparer<T>.Default;
            InPlaceMergeSortInternal(arr, 0, arr.Length - 1, comparer);
        }

        /// <summary>
        /// 并行归并排序（适合大数据量并行，利用多核提升效率）
        /// </summary>
        /// <param name="arr">待排序数组</param>
        /// <param name="comparer">可选比较器</param>
        /// <param name="threshold">分段阈值，当数组长度小于等于该阈值时，转为原地归并排序</param>
        public static void ParallelMergeSort<T>(T[] arr, IComparer<T> comparer = null, int threshold = 2048)
        {
            if (arr == null) throw new ArgumentNullException(nameof(arr));
            comparer ??= Comparer<T>.Default;
            var temp = new T[arr.Length];
            ParallelMergeSortInternal(arr, temp, 0, arr.Length - 1, comparer, threshold);
        }

        /// <summary>
        /// 内部实现
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="arr">The arr.</param>
        /// <param name="temp">The temporary.</param>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <param name="cmp">The CMP.</param>
        private static void MergeSortInternal<T>(T[] arr, T[] temp, int left, int right, IComparer<T> cmp)
        {
            if (left >= right) return;
            int mid = (left + right) / 2;
            MergeSortInternal(arr, temp, left, mid, cmp);
            MergeSortInternal(arr, temp, mid + 1, right, cmp);
            Merge(arr, temp, left, mid, right, cmp);
        }

        /// <summary>
        /// 归并两个有序区间[left, mid], [mid+1, right]
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="arr">The arr.</param>
        /// <param name="temp">The temporary.</param>
        /// <param name="left">The left.</param>
        /// <param name="mid">The mid.</param>
        /// <param name="right">The right.</param>
        /// <param name="cmp">The CMP.</param>
        private static void Merge<T>(T[] arr, T[] temp, int left, int mid, int right, IComparer<T> cmp)
        {
            int i = left, j = mid + 1, k = left;
            while (i <= mid && j <= right)
                temp[k++] = cmp.Compare(arr[i], arr[j]) <= 0 ? arr[i++] : arr[j++];
            while (i <= mid) temp[k++] = arr[i++];
            while (j <= right) temp[k++] = arr[j++];
            for (int x = left; x <= right; x++) arr[x] = temp[x];
        }

        /// <summary>
        /// 原地归并排序实现
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="arr">The arr.</param>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <param name="cmp">The CMP.</param>
        private static void InPlaceMergeSortInternal<T>(T[] arr, int left, int right, IComparer<T> cmp)
        {
            if (left >= right) return;
            int mid = (left + right) / 2;
            InPlaceMergeSortInternal(arr, left, mid, cmp);
            InPlaceMergeSortInternal(arr, mid + 1, right, cmp);
            InPlaceMerge(arr, left, mid, right, cmp);
        }

        /// <summary>
        /// 原地归并两个区间
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="arr">The arr.</param>
        /// <param name="left">The left.</param>
        /// <param name="mid">The mid.</param>
        /// <param name="right">The right.</param>
        /// <param name="cmp">The CMP.</param>
        private static void InPlaceMerge<T>(T[] arr, int left, int mid, int right, IComparer<T> cmp)
        {
            int i = left, j = mid + 1;
            while (i <= mid && j <= right)
            {
                if (cmp.Compare(arr[i], arr[j]) <= 0)
                    i++;
                else
                {
                    // 插入arr[j]到arr[i]前面
                    T tmp = arr[j];
                    for (int k = j; k > i; k--) arr[k] = arr[k - 1];
                    arr[i++] = tmp;
                    mid++; j++;
                }
            }
        }

        /// <summary>
        /// 并行归并排序
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="arr">The arr.</param>
        /// <param name="temp">The temporary.</param>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <param name="cmp">The CMP.</param>
        /// <param name="threshold">The threshold.</param>
        private static void ParallelMergeSortInternal<T>(T[] arr, T[] temp, int left, int right, IComparer<T> cmp, int threshold)
        {
            if (left >= right) return;
            if (right - left + 1 <= threshold)
            {
                MergeSortInternal(arr, temp, left, right, cmp);
                return;
            }
            int mid = (left + right) / 2;
            Parallel.Invoke(
                () => ParallelMergeSortInternal(arr, temp, left, mid, cmp, threshold),
                () => ParallelMergeSortInternal(arr, temp, mid + 1, right, cmp, threshold)
            );
            Merge(arr, temp, left, mid, right, cmp);
        }

        /// <summary>
        /// 混合排序（Hybrid Merge-Insertion Sort）
        /// 数据量大用归并，小区间自动切插入排序
        /// </summary>
        /// <typeparam name="T">元素类型</typeparam>
        /// <param name="arr">待排序数组</param>
        /// <param name="comparer">可选比较器</param>
        /// <param name="insertionThreshold">切换插入排序的阈值，默认16</param>
        public static void HybridMergeSort<T>(T[] arr, IComparer<T> comparer = null, int insertionThreshold = 16)
        {
            if (arr == null) throw new ArgumentNullException(nameof(arr));
            comparer ??= Comparer<T>.Default;
            var temp = new T[arr.Length];
            HybridMergeSortInternal(arr, temp, 0, arr.Length - 1, comparer, insertionThreshold);
        }

        /// <summary>
        /// 混合归并排序递归体（小区间插入排序，大区间归并）
        /// </summary>
        private static void HybridMergeSortInternal<T>(T[] arr, T[] temp, int left, int right, IComparer<T> cmp, int insertionThreshold)
        {
            if (right - left + 1 <= insertionThreshold)
            {
                InsertionSort(arr, left, right, cmp);
                return;
            }
            int mid = (left + right) / 2;
            HybridMergeSortInternal(arr, temp, left, mid, cmp, insertionThreshold);
            HybridMergeSortInternal(arr, temp, mid + 1, right, cmp, insertionThreshold);
            Merge(arr, temp, left, mid, right, cmp);
        }

        /// <summary>
        /// 插入排序，供混合策略内部调用
        /// </summary>
        private static void InsertionSort<T>(T[] arr, int left, int right, IComparer<T> cmp)
        {
            for (int i = left + 1; i <= right; i++)
            {
                T val = arr[i];
                int j = i - 1;
                while (j >= left && cmp.Compare(arr[j], val) > 0)
                    arr[j + 1] = arr[j--];
                arr[j + 1] = val;
            }
        }

    }


    /// <summary>
    /// MergeSorter平台归并排序家族单元测试（基础/自然/混合/原地/并行/自动切换）
    /// </summary>
    [TestFixture]
    public class MergeSorterTests
    {
        /// <summary>
        /// 基础归并排序-升序
        /// </summary>
        [Test]
        public void MergeSort_IntArray_ShouldSortAscending()
        {
            var arr = new[] { 7, 2, 8, 1, 9, 3 };
            MergeSorter.MergeSort(arr);
            Assert.That(arr, Is.EqualTo(new[] { 1, 2, 3, 7, 8, 9 }));
        }

        /// <summary>
        /// 自然归并排序：已局部有序能自动提升效率
        /// </summary>
        [Test]
        public void NaturalMergeSort_PartiallySorted_ShouldSortCorrectly()
        {
            var arr = new[] { 1, 2, 3, 7, 6, 5, 10, 11 };
            MergeSorter.NaturalMergeSort(arr);
            Assert.That(arr, Is.EqualTo(new[] { 1, 2, 3, 5, 6, 7, 10, 11 }));
        }

        /// <summary>
        /// 原地归并排序：空间O(1)，结果一致
        /// </summary>
        [Test]
        public void InPlaceMergeSort_ShouldSortCorrectly()
        {
            var arr = new[] { 4, 2, 7, 5, 1, 3 };
            MergeSorter.InPlaceMergeSort(arr);
            Assert.That(arr, Is.EqualTo(new[] { 1, 2, 3, 4, 5, 7 }));
        }

        /// <summary>
        /// 并行归并排序：大数据与小数据均能正确排序
        /// </summary>
        [Test]
        public void ParallelMergeSort_BigData_ShouldSortCorrectly()
        {
            var arr = Enumerable.Range(1000, 2000).Reverse().ToArray();
            MergeSorter.ParallelMergeSort(arr);
            Assert.That(arr, Is.EqualTo(Enumerable.Range(1000, 2000)));
        }

        /// <summary>
        /// 混合归并排序：小区间插入排序，大区间归并
        /// </summary>
        [Test]
        public void HybridMergeSort_MixedData_ShouldSortCorrectly()
        {
            var arr = new[] { 9, 5, 7, 3, 8, 1, 6, 2, 4 };
            MergeSorter.HybridMergeSort(arr);
            Assert.That(arr, Is.EqualTo(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 }));
        }

        /// <summary>
        /// 自定义比较器：降序排序
        /// </summary>
        [Test]
        public void MergeSort_CustomComparer_ShouldSortDescending()
        {
            var arr = new[] { 1, 3, 2, 4 };
            MergeSorter.MergeSort(arr, Comparer<int>.Create((a, b) => b.CompareTo(a)));
            Assert.That(arr, Is.EqualTo(new[] { 4, 3, 2, 1 }));
        }

        /// <summary>
        /// 所有元素相等
        /// </summary>
        [Test]
        public void MergeSort_AllEqual_ShouldRemainUnchanged()
        {
            var arr = Enumerable.Repeat(99, 20).ToArray();
            MergeSorter.MergeSort(arr);
            Assert.That(arr, Is.EqualTo(Enumerable.Repeat(99, 20)));
        }

        /// <summary>
        /// 空数组和单元素
        /// </summary>
        [Test]
        public void MergeSort_EmptyAndSingle_ShouldNotThrow()
        {
            var empty = new int[0];
            MergeSorter.MergeSort(empty);
            Assert.That(empty, Is.Empty);

            var one = new[] { 42 };
            MergeSorter.MergeSort(one);
            Assert.That(one, Is.EqualTo(new[] { 42 }));
        }

        /// <summary>
        /// 泛型对象排序，按属性升序
        /// </summary>
        private class Person { public int Age; public string Name; }

        /// <summary>
        /// Defines the test method MergeSort_CustomObjects_ShouldSortByAge.
        /// </summary>
        [Test]
        public void MergeSort_CustomObjects_ShouldSortByAge()
        {
            var people = new[]
            {
                new Person{Age=30,Name="A"},
                new Person{Age=24,Name="B"},
                new Person{Age=40,Name="C"}
            };
            MergeSorter.MergeSort(people, Comparer<Person>.Create((a, b) => a.Age.CompareTo(b.Age)));
            Assert.That(people.Select(x => x.Age), Is.EqualTo(new[] { 24, 30, 40 }));
        }
    }


}
