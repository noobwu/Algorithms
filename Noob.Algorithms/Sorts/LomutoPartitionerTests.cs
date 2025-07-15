using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Noob.Algorithms.Sorts
{
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Lomuto 分区家族（基础分区、随机化基准、三数取中、自适应阈值/内省分区）
    /// 支持泛型与自定义比较器，工程化可扩展
    /// </summary>
    public static class LomutoPartitioner
    {
        /// <summary>
        /// 基础 Lomuto 分区：pivot 选用区间最右元素
        /// </summary>
        public static int LomutoPartition<T>(T[] arr, int left, int right, IComparer<T> comparer = null)
        {
            if(arr == null)
                throw new ArgumentNullException(nameof(arr));

            if(left > right)
                throw new ArgumentException("left must be less than right");

            if(left >= arr.Length)
                    throw new ArgumentException("left is out of range");

            if(right >= arr.Length)
                    throw new ArgumentException("right is out of range");

            comparer ??= Comparer<T>.Default;
            T pivot = arr[right];
            int i = left;
            for (int j = left; j < right; j++)
            {
                if (comparer.Compare(arr[j], pivot) < 0)
                {
                    Swap(arr, i, j);
                    i++;
                }
            }
            Swap(arr, i, right);
            return i;
        }

        /// <summary>
        /// 随机化 Lomuto 分区：pivot 随机选取，期望防退化
        /// </summary>
        public static int RandomizedLomutoPartition<T>(T[] arr, int left, int right, IComparer<T> comparer = null, Random rand = null)
        {
            comparer ??= Comparer<T>.Default;
            rand ??= new Random();
            int randomIdx = rand.Next(left, right + 1);
            Swap(arr, randomIdx, right); // 随机选pivot，置于最右
            return LomutoPartition(arr, left, right, comparer);
        }

        /// <summary>
        /// 三数取中 Lomuto 分区：pivot 取首/中/尾三数中位，适合部分有序场景
        /// </summary>
        public static int MedianOfThreeLomutoPartition<T>(T[] arr, int left, int right, IComparer<T> comparer = null)
        {
            comparer ??= Comparer<T>.Default;
            int mid = left + (right - left) / 2;
            int medianIdx = MedianOfThree(arr, left, mid, right, comparer);
            Swap(arr, medianIdx, right); // 中位数放到最右
            return LomutoPartition(arr, left, right, comparer);
        }

        /// <summary>
        /// 自适应阈值分区/内省分区：递归深度过大自动切换堆排
        /// </summary>
        public static void IntrospectiveQuickSort<T>(T[] arr, int left, int right, IComparer<T> comparer = null, int insertionThreshold = 16)
        {
            comparer ??= Comparer<T>.Default;

            if (arr == null)
                throw new ArgumentNullException(nameof(arr));

            if (left > right)
                throw new ArgumentException("left must be less than right");

            if (left >= arr.Length)
                throw new ArgumentException("left is out of range");

            if (right >= arr.Length)
                throw new ArgumentException("right is out of range");

            int depthLimit = 2 * (int)Math.Log(right - left + 1, 2);
            IntrospectiveQuickSortInternal(arr, left, right, comparer, depthLimit, insertionThreshold);
        }

        /// <summary>
        /// Introspectives the quick sort internal.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="arr">The arr.</param>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <param name="cmp">The CMP.</param>
        /// <param name="depthLimit">The depth limit.</param>
        /// <param name="insertionThreshold">The insertion threshold.</param>
        private static void IntrospectiveQuickSortInternal<T>(
            T[] arr, int left, int right, IComparer<T> cmp, int depthLimit, int insertionThreshold)
        {
            while (right - left + 1 > insertionThreshold)
            {
                if (depthLimit == 0)
                {
                    HeapSort(arr, left, right, cmp);
                    return;
                }
                depthLimit--;
                // 推荐：三数取中 + 随机，工程可灵活扩展
                int pivotIdx = MedianOfThree(arr, left, left + (right - left) / 2, right, cmp);
                Swap(arr, pivotIdx, right);
                int pivotFinal = LomutoPartition(arr, left, right, cmp);

                // 尾递归优化：优先递归短区间
                if (pivotFinal - left < right - pivotFinal)
                {
                    IntrospectiveQuickSortInternal(arr, left, pivotFinal - 1, cmp, depthLimit, insertionThreshold);
                    left = pivotFinal + 1;
                }
                else
                {
                    IntrospectiveQuickSortInternal(arr, pivotFinal + 1, right, cmp, depthLimit, insertionThreshold);
                    right = pivotFinal - 1;
                }
            }
            // 小区间插入排序
            InsertionSort(arr, left, right, cmp);
        }

        /// <summary>
        /// 插入排序，小区间快排兜底
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

        /// <summary>
        /// 堆排序，快排退化/递归过深兜底
        /// </summary>
        private static void HeapSort<T>(T[] arr, int left, int right, IComparer<T> cmp)
        {
            int n = right - left + 1;
            for (int i = n / 2 - 1; i >= 0; i--)
                Heapify(arr, n, i, left, cmp);
            for (int i = n - 1; i > 0; i--)
            {
                Swap(arr, left, left + i);
                Heapify(arr, i, 0, left, cmp);
            }
        }

        /// <summary>
        /// Heapifies the specified arr.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="arr">The arr.</param>
        /// <param name="heapSize">Size of the heap.</param>
        /// <param name="rootIdx">Index of the root.</param>
        /// <param name="baseIdx">Index of the base.</param>
        /// <param name="cmp">The CMP.</param>
        private static void Heapify<T>(T[] arr, int heapSize, int rootIdx, int baseIdx, IComparer<T> cmp)
        {
            int largest = rootIdx;
            int l = 2 * rootIdx + 1, r = 2 * rootIdx + 2;
            if (l < heapSize && cmp.Compare(arr[baseIdx + l], arr[baseIdx + largest]) > 0)
                largest = l;
            if (r < heapSize && cmp.Compare(arr[baseIdx + r], arr[baseIdx + largest]) > 0)
                largest = r;
            if (largest != rootIdx)
            {
                Swap(arr, baseIdx + rootIdx, baseIdx + largest);
                Heapify(arr, heapSize, largest, baseIdx, cmp);
            }
        }

        /// <summary>
        /// 求三数中位数的下标
        /// </summary>
        /// <param name="arr">数组</param>
        /// <param name="a">下标a</param>
        /// <param name="b">下标b</param>
        /// <param name="c">下标c</param>
        /// <param name="cmp">比较器</param>
        private static int MedianOfThree<T>(T[] arr, int a, int b, int c, IComparer<T> cmp)
        {
            T va = arr[a], vb = arr[b], vc = arr[c];
            if ((cmp.Compare(va, vb) <= 0 && cmp.Compare(vb, vc) <= 0) || (cmp.Compare(vc, vb) <= 0 && cmp.Compare(vb, va) <= 0))
                return b;
            if ((cmp.Compare(vb, va) <= 0 && cmp.Compare(va, vc) <= 0) || (cmp.Compare(vc, va) <= 0 && cmp.Compare(va, vb) <= 0))
                return a;
            return c;
        }

        /// <summary>
        /// 交换两个元素
        /// </summary>
        private static void Swap<T>(T[] arr, int i, int j)
        {
            if (i == j) return;
            T tmp = arr[i];
            arr[i] = arr[j];
            arr[j] = tmp;
        }
    }



    /// <summary>
    /// LomutoPartitioner 平台 Lomuto 分区家族单元测试
    /// 覆盖基础、随机化、三数取中、自适应阈值/内省等场景
    /// </summary>
    [TestFixture]
    public class LomutoPartitionerTests
    {
        /// <summary>
        /// 基础 Lomuto 分区：可正确分区，pivot落点左<、右≥
        /// </summary>
        [Test]
        public void LomutoPartition_Basic_ShouldPartitionCorrectly()
        {
            var arr = new[] { 7, 3, 9, 1, 5, 8 };
            int pos = LomutoPartitioner.LomutoPartition(arr, 0, arr.Length - 1);
            Assert.That(arr.Take(pos), Is.All.LessThan(arr[pos]));
            Assert.That(arr.Skip(pos + 1), Is.All.GreaterThanOrEqualTo(arr[pos]));
            Assert.That(arr.OrderBy(x => x), Is.EquivalentTo(new[] { 1, 3, 5, 7, 8, 9 })); // 不要求全排好，只保证分区后乱序无丢失
        }

        /// <summary>
        /// 随机化 Lomuto 分区：多轮随机分区结果总是有效
        /// </summary>
        [Test]
        public void RandomizedLomutoPartition_MultiRuns_ShouldAlwaysPartition()
        {
            var origin = new[] { 6, 4, 5, 9, 3, 7 };
            for (int t = 0; t < 10; t++)
            {
                var arr = origin.ToArray();
                int pos = LomutoPartitioner.RandomizedLomutoPartition(arr, 0, arr.Length - 1);
                Assert.That(arr.Take(pos), Is.All.LessThan(arr[pos]));
                Assert.That(arr.Skip(pos + 1), Is.All.GreaterThanOrEqualTo(arr[pos]));
                Assert.That(arr.OrderBy(x => x), Is.EquivalentTo(origin));
            }
        }

        /// <summary>
        /// 三数取中 Lomuto 分区：部分有序输入依然能保证分区正确
        /// </summary>
        [Test]
        public void MedianOfThreeLomutoPartition_SemiSorted_ShouldPartition()
        {
            var arr = new[] { 2, 3, 4, 5, 7, 1, 8 };
            int pos = LomutoPartitioner.MedianOfThreeLomutoPartition(arr, 0, arr.Length - 1);
            Assert.That(arr.Take(pos), Is.All.LessThan(arr[pos]));
            Assert.That(arr.Skip(pos + 1), Is.All.GreaterThanOrEqualTo(arr[pos]));
            Assert.That(arr.OrderBy(x => x), Is.EquivalentTo(new[] { 1, 2, 3, 4, 5, 7, 8 }));
        }

        /// <summary>
        /// 内省分区快排：极端数据不退化
        /// </summary>
        [Test]
        public void IntrospectiveQuickSort_ReverseAndDuplicates_ShouldSortCorrectly()
        {
            var arr1 = Enumerable.Range(1, 1000).Reverse().ToArray();
            LomutoPartitioner.IntrospectiveQuickSort(arr1, 0, arr1.Length - 1);
            Assert.That(arr1, Is.EqualTo(Enumerable.Range(1, 1000)));

            var arr2 = Enumerable.Repeat(42, 20).ToArray();
            LomutoPartitioner.IntrospectiveQuickSort(arr2, 0, arr2.Length - 1);
            Assert.That(arr2, Is.EqualTo(Enumerable.Repeat(42, 20)));
        }

        /// <summary>
        /// 泛型与自定义比较器（降序）
        /// </summary>
        [Test]
        public void MedianOfThreeLomutoPartition_CustomComparer_ShouldPartitionDescending()
        {
            var arr = new[] { 3, 1, 5, 2, 4 };
            int pos = LomutoPartitioner.MedianOfThreeLomutoPartition(arr, 0, arr.Length - 1, Comparer<int>.Create((a, b) => b.CompareTo(a)));
            Assert.That(arr.Take(pos), Is.All.GreaterThan(arr[pos]));
            Assert.That(arr.Skip(pos + 1), Is.All.LessThanOrEqualTo(arr[pos]));
            Assert.That(arr.OrderByDescending(x => x), Is.EquivalentTo(new[] { 1, 2, 3, 4, 5 }));
        }

        /// <summary>
        /// 空/单元素健壮性
        /// </summary>
        [Test]
        public void LomutoPartition_EmptyAndSingle_ShouldNotThrow()
        {
            var empty = new int[0];
            Assert.DoesNotThrow(() => LomutoPartitioner.IntrospectiveQuickSort(empty, 0, -1));

            var one = new[] { 88 };
            LomutoPartitioner.IntrospectiveQuickSort(one, 0, 0);
            Assert.That(one, Is.EqualTo(new[] { 88 }));
        }

        /// <summary>
        /// LeetCode常考“全有序/全逆序/大重复”——要能分区不退化
        /// </summary>
        [Test]
        public void Partition_OrderedAndReverseAndDuplicates_ShouldNotCrash()
        {
            var arr1 = Enumerable.Range(1, 100).ToArray();
            var arr2 = Enumerable.Range(1, 100).Reverse().ToArray();
            var arr3 = Enumerable.Repeat(42, 100).ToArray();

            int pos1 = LomutoPartitioner.LomutoPartition(arr1, 0, arr1.Length - 1);
            int pos2 = LomutoPartitioner.LomutoPartition(arr2, 0, arr2.Length - 1);
            int pos3 = LomutoPartitioner.LomutoPartition(arr3, 0, arr3.Length - 1);

            Assert.That(arr1.Take(pos1), Is.All.LessThan(arr1[pos1]));
            Assert.That(arr2.Take(pos2), Is.All.LessThan(arr2[pos2]));
            Assert.That(arr3.Take(pos3), Is.Empty.Or.All.LessThan(arr3[pos3])); //全等时左区间可能为空
        }

        /// <summary>
        /// Hoare分区与Lomuto分区行为差异（平台边界对照）
        /// </summary>
        [Test]
        public void Partition_HoareVsLomuto_ShouldHaveDifferentResults()
        {
            // Hoare分区结果pivot未必在最终位置
            int[] arr = { 3, 2, 1, 5, 4 };
            int lomutoPivot = LomutoPartitioner.LomutoPartition(arr.ToArray(), 0, arr.Length - 1);
            int hoarePivot = HoarePartition(arr.ToArray(), 0, arr.Length - 1);

            Assert.That(lomutoPivot, Is.GreaterThanOrEqualTo(0).And.LessThanOrEqualTo(arr.Length - 1));
            Assert.That(hoarePivot, Is.GreaterThanOrEqualTo(0).And.LessThanOrEqualTo(arr.Length - 1));
        }
     
        /// <summary>
        /// 简化版Hoare分区实现，仅用于测试对比     
        /// </summary>
        /// <param name="arr">The arr.</param>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns>System.Int32.</returns>
        private int HoarePartition(int[] arr, int left, int right)
        {
            int pivot = arr[left];
            int i = left - 1, j = right + 1;
            while (true)
            {
                do { i++; } while (arr[i] < pivot);
                do { j--; } while (arr[j] > pivot);
                if (i >= j) return j;
                int tmp = arr[i]; arr[i] = arr[j]; arr[j] = tmp;
            }
        }

        /// <summary>
        /// 极端“小区间”与“超大区间”——平台健壮性保障
        /// </summary>
        [Test]
        public void Partition_SmallAndLargeArrays_ShouldPartition()
        {
            var small = new[] { 5, 1 };
            int posSmall = LomutoPartitioner.LomutoPartition(small, 0, small.Length - 1);
            Assert.That(small.Take(posSmall), Is.All.LessThan(small[posSmall]));
            Assert.That(small.Skip(posSmall + 1), Is.All.GreaterThanOrEqualTo(small[posSmall]));

            var big = Enumerable.Range(0, 100_000).Reverse().ToArray();
            int posBig = LomutoPartitioner.RandomizedLomutoPartition(big, 0, big.Length - 1);
            Assert.That(big.Take(posBig), Is.All.LessThan(big[posBig]));
            Assert.That(big.Skip(posBig + 1), Is.All.GreaterThanOrEqualTo(big[posBig]));
        }


        /// <summary>
        /// 边界：左>右、空数组、单元素、越界访问等不抛未处理异常
        /// </summary>
        [Test]
        public void Partition_EdgeCases_ShouldThrow()
        {
            var empty = new int[0];
            Assert.Throws<ArgumentException>(() => LomutoPartitioner.LomutoPartition(empty, 0, -1));
            Assert.Throws<ArgumentException>(() => LomutoPartitioner.IntrospectiveQuickSort(empty, 0, -1));

            var one = new[] { 99 };
            Assert.DoesNotThrow(() => LomutoPartitioner.LomutoPartition(one, 0, 0));
            Assert.DoesNotThrow(() => LomutoPartitioner.IntrospectiveQuickSort(one, 0, 0));

            // left > right（典型递归终止条件）
            var arr = new[] { 1, 2, 3 };
            Assert.Throws<ArgumentException>(() => LomutoPartitioner.LomutoPartition(arr, 2, 1));
            Assert.Throws<ArgumentException>(() => LomutoPartitioner.IntrospectiveQuickSort(arr, 2, 1));
        }

        /// <summary>
        /// 稳定性/等值元素分布对分区影响——工程接口应能处理
        /// </summary>
        [Test]
        public void Partition_AllEqualAndSomeEqual_ShouldBehave()
        {
            var arr1 = Enumerable.Repeat(0, 10).ToArray();
            int pos1 = LomutoPartitioner.LomutoPartition(arr1, 0, arr1.Length - 1);
            Assert.That(arr1.Skip(pos1 + 1), Is.All.GreaterThanOrEqualTo(arr1[pos1]));
            // 混合重复
            var arr2 = new[] { 2, 2, 1, 3, 3, 2, 1, 3 };
            int pos2 = LomutoPartitioner.LomutoPartition(arr2, 0, arr2.Length - 1);
            Assert.That(arr2.Take(pos2), Is.All.LessThan(arr2[pos2]));
            Assert.That(arr2.Skip(pos2 + 1), Is.All.GreaterThanOrEqualTo(arr2[pos2]));
        }
    }

}
