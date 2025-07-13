using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Noob.Algorithms.Sorts
{
    /// <summary>
    /// 通用堆排序（HeapSort）与TopK、稳定堆平台工具
    /// </summary>
    public static class HeapSorter
    {
        /// <summary>
        /// 对输入列表进行原地堆排序，升序排列（支持自定义比较器）
        /// </summary>
        /// <typeparam name="T">元素类型</typeparam>
        /// <param name="list">待排序的列表</param>
        /// <param name="comparer">自定义比较器（可空，默认升序）</param>
        public static void HeapSort<T>(IList<T> list, IComparer<T> comparer = null)
        {
            if (list == null) throw new ArgumentNullException(nameof(list));
            comparer ??= Comparer<T>.Default;
            int n = list.Count;
            for (int i = n / 2 - 1; i >= 0; i--)
                Heapify(list, n, i, comparer);

            for (int i = n - 1; i > 0; i--)
            {
                Swap(list, 0, i);
                Heapify(list, i, 0, comparer);
            }
        }

        /// <summary>
        /// TopK：从列表中取前K大（或K小）元素，复杂度O(NlogK)，结果无序。
        /// 若K≥N，等价于全量堆排序。
        /// </summary>
        /// <typeparam name="T">元素类型</typeparam>
        /// <param name="list">输入序列</param>
        /// <param name="k">K值</param>
        /// <param name="comparer">自定义比较器，默认升序（返回最大K个）</param>
        /// <returns>前K大元素集合（无序），若K≤0则空</returns>
        public static List<T> TopK<T>(IEnumerable<T> list, int k, IComparer<T> comparer = null)
        {
            if (list == null) throw new ArgumentNullException(nameof(list));
            if (k <= 0) return new List<T>();
            comparer ??= Comparer<T>.Default;

            var minHeap = new PriorityQueue<T, T>(Comparer<T>.Create((a, b) => comparer.Compare(a, b)));
            foreach (var item in list)
            {
                if (minHeap.Count < k)
                {
                    minHeap.Enqueue(item, item);
                }
                else if (comparer.Compare(item, minHeap.Peek()) > 0)
                {
                    minHeap.Dequeue();
                    minHeap.Enqueue(item, item);
                }
            }
            var result = new List<T>(minHeap.UnorderedItems.Count);
            foreach (var pair in minHeap.UnorderedItems)
                result.Add(pair.Element);
            return result;
        }

        /// <summary>
        /// 稳定堆排序（Stable HeapSort）：同值元素原始顺序不变
        /// </summary>
        /// <typeparam name="T">元素类型</typeparam>
        /// <param name="list">待排序集合</param>
        /// <param name="comparer">可选自定义比较器</param>
        public static void StableHeapSort<T>(IList<T> list, IComparer<T> comparer = null)
        {
            if (list == null) throw new ArgumentNullException(nameof(list));
            comparer ??= Comparer<T>.Default;
            int n = list.Count;
            // 封装元素+原始下标，实现稳定排序
            var pairs = new List<StablePair<T>>(n);
            for (int i = 0; i < n; i++)
                pairs.Add(new StablePair<T> { Value = list[i], Index = i });

            // "稳定性"保证排序：同值时，index小的先出堆
            var stableComparer = Comparer<StablePair<T>>.Create((a, b) =>
            {
                int cmp = comparer.Compare(a.Value, b.Value);
                return cmp != 0 ? cmp : a.Index.CompareTo(b.Index);
            });

            HeapSort(pairs, stableComparer);
            for (int i = 0; i < n; i++)
                list[i] = pairs[i].Value;
        }

        /// <summary>
        /// 堆化操作（大顶堆/自定义比较器）
        /// </summary>
        /// <param name="list">列表</param>
        /// <param name="heapSize">当前堆大小</param>
        /// <param name="rootIndex">根节点索引</param>
        /// <param name="comparer">自定义比较器</param>
        private static void Heapify<T>(IList<T> list, int heapSize, int rootIndex, IComparer<T> comparer)
        {
            int largest = rootIndex;
            int left = 2 * rootIndex + 1, right = 2 * rootIndex + 2;
            if (left < heapSize && comparer.Compare(list[left], list[largest]) > 0)
                largest = left;
            if (right < heapSize && comparer.Compare(list[right], list[largest]) > 0)
                largest = right;
            if (largest != rootIndex)
            {
                Swap(list, rootIndex, largest);
                Heapify(list, heapSize, largest, comparer);
            }
        }

        /// <summary>
        /// 元素交换辅助
        /// </summary>
        /// <param name="list">列表</param>
        /// <param name="i">索引i</param>
        /// <param name="j">索引j</param>
        private static void Swap<T>(IList<T> list, int i, int j)
        {
            if (i == j) return;
            T t = list[i];
            list[i] = list[j];
            list[j] = t;
        }

        /// <summary>
        /// 稳定堆排序用封装类型
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
    }


    /// <summary>
    /// HeapSorter平台级堆排序、TopK与稳定堆的系统性单元测试
    /// </summary>
    [TestFixture]
    public class HeapSorterTests
    {
        /// <summary>
        /// 标准堆排序基础用例，验证升序排列
        /// </summary>
        [Test]
        public void HeapSort_IntList_ShouldSortAscending()
        {
            var arr = new List<int> { 5, 2, 9, 4, 2, 8 };
            HeapSorter.HeapSort(arr);
            Assert.That(arr, Is.EqualTo(new[] { 2, 2, 4, 5, 8, 9 }));
        }

        /// <summary>
        /// 降序自定义比较器
        /// </summary>
        [Test]
        public void HeapSort_IntList_CustomComparer_ShouldSortDescending()
        {
            var arr = new List<int> { 3, 7, 1, 6 };
            HeapSorter.HeapSort(arr, Comparer<int>.Create((a, b) => b.CompareTo(a)));
            Assert.That(arr, Is.EqualTo(new[] { 7, 6, 3, 1 }));
        }

        /// <summary>
        /// TopK返回前K大元素（无序）
        /// </summary>
        [Test]
        public void TopK_BasicScenario_ShouldReturnTopK()
        {
            var arr = new List<int> { 1, 5, 7, 2, 6, 8 };
            var result = HeapSorter.TopK(arr, 3);
            Assert.That(result.Count, Is.EqualTo(3));
            Assert.That(result, Is.SubsetOf(new[] { 8, 7, 6 }));
        }

        /// <summary>
        /// TopK返回前K小元素
        /// </summary>
        [Test]
        public void TopK_ReturnSmallestK_WhenUsingReverseComparer()
        {
            var arr = new List<int> { 1, 3, 2, 5, 8, 0 };
            var result = HeapSorter.TopK(arr, 2, Comparer<int>.Create((a, b) => b.CompareTo(a)));
            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result, Is.SubsetOf(new[] { 0, 1 }));
        }

        /// <summary>
        /// TopK边界测试：K大于输入长度时等价于全排序
        /// </summary>
        [Test]
        public void TopK_KExceedsLength_ShouldReturnAllElements()
        {
            var arr = new List<int> { 9, 3 };
            var result = HeapSorter.TopK(arr, 10);
            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result, Is.SubsetOf(arr));
        }

        /// <summary>
        /// TopK边界测试：K为0时返回空
        /// </summary>
        [Test]
        public void TopK_KIsZero_ShouldReturnEmpty()
        {
            var arr = new List<int> { 1, 2, 3 };
            var result = HeapSorter.TopK(arr, 0);
            Assert.That(result, Is.Empty);
        }

        /// <summary>
        /// 稳定堆排序保持相同元素原始顺序
        /// </summary>
        [Test]
        public void StableHeapSort_ShouldPreserveOriginalOrderForEqualElements()
        {
            var arr = new List<(int score, string name)>
            {
                (90, "A"),
                (90, "B"),
                (85, "C"),
                (90, "C")
            };
            HeapSorter.StableHeapSort(arr, Comparer<(int score, string name)>.Create((a, b) => a.score.CompareTo(b.score)));
            // 验证所有score=90的相对顺序
            Assert.That(arr[0].name, Is.EqualTo("C")); // "C"分数最低应排最前
            Assert.That(arr[1].name, Is.EqualTo("A"));
            Assert.That(arr[2].name, Is.EqualTo("B")); // "A"应在"B"前
            Assert.That(arr[3].name, Is.EqualTo("C"));
        }

        /// <summary>
        /// 空数组、单元素数组安全性
        /// </summary>
        [Test]
        public void HeapSort_EmptyOrSingle_ShouldNotThrow()
        {
            var empty = new List<int>();
            HeapSorter.HeapSort(empty);
            Assert.That(empty, Is.Empty);

            var one = new List<int> { 7 };
            HeapSorter.HeapSort(one);
            Assert.That(one, Is.EqualTo(new[] { 7 }));
        }

        /// <summary>
        /// TopK极端大K和空输入容错
        /// </summary>
        [Test]
        public void TopK_EmptyInputOrExtremeK_ShouldNotThrow()
        {
            Assert.That(HeapSorter.TopK(new List<int>(), 3), Is.Empty);
            Assert.That(HeapSorter.TopK(new List<int> { 1, 2 }, -1), Is.Empty);
        }
    }


}
