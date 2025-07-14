using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Noob.Algorithms.Sorts
{

    /// <summary>
    /// 平台通用快速排序（QuickSort）家族算法工具
    /// 包含三路分区、随机化、尾递归、内省排序（Introsort）
    /// </summary>
    public static class QuickSorter
    {

        /// <summary>
        /// 基础快速排序（原理清晰，适合教学和小数据）
        /// </summary>
        public static void BasicQuickSort<T>(T[] arr, IComparer<T> comparer = null)
        {
            comparer ??= Comparer<T>.Default;
            BasicQuickSortInternal(arr, 0, arr.Length - 1, comparer);
        }

        /// <summary>
        /// 随机化三路快排：对大批量重复元素/极端输入有健壮性保障
        /// </summary>
        /// <typeparam name="T">元素类型</typeparam>
        /// <param name="arr">待排序数组</param>
        /// <param name="comparer">可选自定义比较器</param>
        public static void QuickSort<T>(T[] arr, IComparer<T> comparer = null)
        {
            if (arr == null) throw new ArgumentNullException(nameof(arr));
            comparer ??= Comparer<T>.Default;
            var rand = new Random();
            QuickSortInternal(arr, 0, arr.Length - 1, comparer,rand);
        }

        /// <summary>
        /// Basics the quick sort internal.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="arr">The arr.</param>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <param name="cmp">The CMP.</param>
        private static void BasicQuickSortInternal<T>(T[] arr, int left, int right, IComparer<T> cmp)
        {
            if (left >= right) return;
            int pivotIdx = Partition(arr, left, right, cmp);
            BasicQuickSortInternal(arr, left, pivotIdx - 1, cmp);
            BasicQuickSortInternal(arr, pivotIdx + 1, right, cmp);
        }

        /// <summary>
        /// Lomuto分区，返回基准元素最终位置    
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="arr">The arr.</param>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <param name="cmp">The CMP.</param>
        /// <returns>System.Int32.</returns>
        private static int Partition<T>(T[] arr, int left, int right, IComparer<T> cmp)
        {
            T pivot = arr[right];
            int i = left;
            for (int j = left; j < right; j++)
            {
                if (cmp.Compare(arr[j], pivot) < 0)
                {
                    Swap(arr, i, j);
                    i++;
                }
            }
            Swap(arr, i, right);
            return i;
        }



        /// <summary>
        /// 随机化三路快排 + 尾递归优化 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="arr">The arr.</param>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <param name="cmp">The CMP.</param>
        /// <param name="rand">The rand.</param>
        private static void QuickSortInternal<T>(T[] arr, int left, int right, IComparer<T> cmp, Random rand)
        {
            while (left < right)
            {
                // 随机化选pivot
                int pivotIdx = rand.Next(left, right + 1);
                Swap(arr, left, pivotIdx);
                T pivot = arr[left];
                int lt = left, i = left + 1, gt = right;
                // 三路分区
                while (i <= gt)
                {
                    int c = cmp.Compare(arr[i], pivot);
                    if (c < 0) Swap(arr, lt++, i++);
                    else if (c > 0) Swap(arr, i, gt--);
                    else i++;
                }
                // 尾递归优化：优先递归短区间
                if ((lt - left) < (right - gt))
                {
                    QuickSortInternal(arr, left, lt - 1, cmp, rand);
                    left = gt + 1;
                }
                else
                {
                    QuickSortInternal(arr, gt + 1, right, cmp, rand);
                    right = lt - 1;
                }
            }
        }

        /// <summary>
        /// 内省排序：当递归过深自动切堆排序，保障最坏 O(nlogn)
        /// </summary>
        /// <typeparam name="T">元素类型</typeparam>
        /// <param name="arr">待排序数组</param>
        /// <param name="comparer">可选自定义比较器</param>
        public static void IntroSort<T>(T[] arr, IComparer<T> comparer = null)
        {
            if (arr == null) throw new ArgumentNullException(nameof(arr));
            comparer ??= Comparer<T>.Default;
            int depthLimit = 2 * (int)Math.Log(arr.Length + 1, 2); // 理论最深递归层数
            var rand = new Random();
            IntroSortInternal(arr, 0, arr.Length - 1, comparer, rand, depthLimit);
        }

        /// <summary>
        /// 内省排序
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="arr">The arr.</param>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <param name="cmp">The CMP.</param>
        /// <param name="rand">The rand.</param>
        /// <param name="depthLimit">The depth limit.</param>
        private static void IntroSortInternal<T>(T[] arr, int left, int right, IComparer<T> cmp, Random rand, int depthLimit)
        {
            while (left < right)
            {
                if (depthLimit == 0)
                {
                    HeapSort(arr, left, right, cmp); // 切堆排
                    return;
                }
                depthLimit--;
                // 三路分区+随机化
                int pivotIdx = rand.Next(left, right + 1);
                Swap(arr, left, pivotIdx);
                T pivot = arr[left];
                int lt = left, i = left + 1, gt = right;
                while (i <= gt)
                {
                    int c = cmp.Compare(arr[i], pivot);
                    if (c < 0) Swap(arr, lt++, i++);
                    else if (c > 0) Swap(arr, i, gt--);
                    else i++;
                }
                // 尾递归优化
                if ((lt - left) < (right - gt))
                {
                    IntroSortInternal(arr, left, lt - 1, cmp, rand, depthLimit);
                    left = gt + 1;
                }
                else
                {
                    IntroSortInternal(arr, gt + 1, right, cmp, rand, depthLimit);
                    right = lt - 1;
                }
            }
        }


        /// <summary>
        /// 稳定快速排序：对任意类型支持稳定排序（相等元素原始顺序不变）
        /// 通过二次比较“元素值+原始下标”实现
        /// </summary>
        /// <typeparam name="T">元素类型</typeparam>
        /// <param name="arr">待排序数组</param>
        /// <param name="comparer">自定义比较器，默认升序</param>
        public static void StableQuickSort<T>(T[] arr, IComparer<T> comparer = null)
        {
            if (arr == null) throw new ArgumentNullException(nameof(arr));
            comparer ??= Comparer<T>.Default;
            var pairs = new StablePair<T>[arr.Length];
            for (int i = 0; i < arr.Length; i++)
                pairs[i] = new StablePair<T> { Value = arr[i], Index = i };

            QuickSortInternal(pairs, 0, pairs.Length - 1,
                Comparer<StablePair<T>>.Create((a, b) =>
                {
                    int cmp = comparer.Compare(a.Value, b.Value);
                    // 稳定性关键：值相等时按原始下标排序
                    return cmp != 0 ? cmp : a.Index.CompareTo(b.Index);
                }),
                new Random());

            for (int i = 0; i < arr.Length; i++)
                arr[i] = pairs[i].Value;
        }



        /// <summary>
        /// 原地堆排序（用于内省排序切换，支持区间）
        /// </summary>
        /// <param name="arr">待排序数组</param>
        /// <param name="left">排序区间左边界</param>
        /// <param name="right">排序区间右边界</param>
        /// <param name="cmp">可选自定义比较器</param>
        private static void HeapSort<T>(T[] arr, int left, int right, IComparer<T> cmp)
        {
            int n = right - left + 1;
            // Build max heap
            for (int i = n / 2 - 1; i >= 0; i--)
                Heapify(arr, n, i, left, cmp);
            // 排序
            for (int i = n - 1; i > 0; i--)
            {
                Swap(arr, left, left + i);
                Heapify(arr, i, 0, left, cmp);
            }
        }

        /// <summary>
        /// 区间堆化
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
        /// 交换元素
        /// </summary>
        private static void Swap<T>(T[] arr, int i, int j)
        {
            if (i == j) return;
            T tmp = arr[i];
            arr[i] = arr[j];
            arr[j] = tmp;
        }

        /// <summary>
        /// 稳定排序封装类型
        /// </summary>
        private class StablePair<T>
        {
            /// <summary>
            /// The value
            /// </summary>
            public T Value;

            /// <summary>
            /// The index
            /// </summary>
            public int Index;
        }


        /// <summary>
        /// QuickSorter平台快速排序（含三路、随机化、内省、稳定排序）单元测试
        /// </summary>
        [TestFixture]
        public class QuickSorterTests
        {
            /// <summary>
            /// 基础快排-整数升序
            /// </summary>
            [Test]
            public void BasicQuickSort_IntArray_ShouldSortAscending()
            {
                var arr = new[] { 7, 3, 1, 8, 2, 4 };
                QuickSorter.BasicQuickSort(arr);
                Assert.That(arr, Is.EqualTo(new[] { 1, 2, 3, 4, 7, 8 }));
            }


            /// <summary>
            /// 标准三路快排：整数数组升序排序
            /// </summary>
            [Test]
            public void QuickSort_IntArray_ShouldSortAscending()
            {
                var arr = new[] { 5, 2, 9, 4, 2, 8, 7 };
                QuickSorter.QuickSort(arr);
                Assert.That(arr, Is.EqualTo(new[] { 2, 2, 4, 5, 7, 8, 9 }));
            }

            /// <summary>
            /// 三路快排支持自定义降序
            /// </summary>
            [Test]
            public void QuickSort_CustomComparer_ShouldSortDescending()
            {
                var arr = new[] { 3, 1, 4, 2 };
                QuickSorter.QuickSort(arr, Comparer<int>.Create((a, b) => b.CompareTo(a)));
                Assert.That(arr, Is.EqualTo(new[] { 4, 3, 2, 1 }));
            }

            /// <summary>
            /// 内省排序（极端数据）不退化为O(n²)
            /// </summary>
            [Test]
            public void IntroSort_NearlyEqualOrReverse_ShouldSortCorrectly()
            {
                var arr1 = Enumerable.Repeat(1, 100).ToArray();
                QuickSorter.IntroSort(arr1);
                Assert.That(arr1, Is.EqualTo(Enumerable.Repeat(1, 100)));

                var arr2 = Enumerable.Range(1, 1000).Reverse().ToArray();
                QuickSorter.IntroSort(arr2);
                Assert.That(arr2, Is.EqualTo(Enumerable.Range(1, 1000)));
            }

            /// <summary>
            /// 稳定快排（同分不同名）顺序应保留原顺序
            /// </summary>
            [Test]
            public void StableQuickSort_SameKey_ShouldPreserveOriginalOrder()
            {
                var arr = new[]
                {
                    (score:90, name:"A"),
                    (score:85, name:"B"),
                    (score:90, name:"C"),
                    (score:90, name:"D"),
                };
                QuickSorter.StableQuickSort(arr, Comparer<(int score, string name)>.Create((a, b) => a.score.CompareTo(b.score)));
                Assert.That(arr[2].name, Is.EqualTo("C")); // A 应在 C 前

                Assert.That(arr[3].name, Is.EqualTo("D")); // C 应在 D 前

                Assert.That(arr.Select(x => x.score), Is.EqualTo(new[] { 85, 90, 90, 90 }));
            }

            /// <summary>
            /// 空数组/单元素健壮性
            /// </summary>
            [Test]
            public void QuickSort_EmptyAndSingle_ShouldNotThrow()
            {
                var empty = new int[0];
                QuickSorter.QuickSort(empty);
                Assert.That(empty, Is.Empty);

                var one = new[] { 99 };
                QuickSorter.QuickSort(one);
                Assert.That(one, Is.EqualTo(new[] { 99 }));
            }

            /// <summary>
            /// 所有元素相等，不应乱序
            /// </summary>
            [Test]
            public void QuickSort_AllEqual_ShouldRemainUnchanged()
            {
                var arr = Enumerable.Repeat(42, 10).ToArray();
                QuickSorter.QuickSort(arr);
                Assert.That(arr, Is.EqualTo(Enumerable.Repeat(42, 10)));
            }

            /// <summary>
            /// 泛型对象支持，排序后按属性
            /// </summary>
            private class Person { public int Age; public string Name; }
            [Test]
            public void QuickSort_CustomObjects_ShouldSortByAge()
            {
                var people = new[]
                {
                    new Person{ Age=30,Name="A" },
                    new Person{ Age=24,Name="B" },
                    new Person{ Age=40,Name="C" }
                };
                QuickSorter.QuickSort(people, Comparer<Person>.Create((a, b) => a.Age.CompareTo(b.Age)));
                Assert.That(people.Select(x => x.Age), Is.EqualTo(new[] { 24, 30, 40 }));
            }
        }

    }

}
