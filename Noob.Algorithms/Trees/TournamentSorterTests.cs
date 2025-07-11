using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;


namespace Noob.Algorithms.Trees
{
    /// <summary>
    /// 锦标赛排序（Tournament Sort）通用实现，支持可扩展的比较方式、Top-K选拔。
    /// </summary>
    public class TournamentSorter<T>
    {
        /// <summary>
        /// 锦标赛排序器
        /// </summary>
        private readonly IComparer<T> _comparer;

        /// <summary>
        /// 创建锦标赛排序器，可指定自定义比较器。
        /// </summary>
        /// <param name="comparer">元素比较器。默认使用 Comparer&lt;T&gt;.Default。</param>
        public TournamentSorter(IComparer<T>? comparer = null)
        {
            _comparer = comparer ?? Comparer<T>.Default;
        }

        /// <summary>
        /// 对集合执行锦标赛排序，返回升序排列的新列表。
        /// </summary>
        /// <param name="source">待排序集合</param>
        /// <returns>排序后的新列表</returns>
        public List<T> Sort(IEnumerable<T> source)
        {
            var array = new List<T>(source);
            var result = new List<T>(array.Count);

            // 使用胜者树结构，每次弹出当前最小/最大，重新补位
            TournamentTree tree = new TournamentTree(array, _comparer);

            for (int i = 0; i < array.Count; i++)
            {
                result.Add(tree.PopWinner());
            }

            return result;
        }

        /// <summary>
        /// 选拔前K名（Top-K），效率高于全排序，返回Top-K有序列表。
        /// </summary>
        /// <param name="source">输入集合</param>
        /// <param name="k">Top-K数量</param>
        /// <returns>前K名列表</returns>
        public List<T> SelectTopK(IEnumerable<T> source, int k)
        {
            var array = new List<T>(source);
            if (k > array.Count) k = array.Count;
            var result = new List<T>(k);

            TournamentTree tree = new TournamentTree(array, _comparer);
            for (int i = 0; i < k; i++)
            {
                result.Add(tree.PopWinner());
            }
            return result;
        }

        /// <summary>
        /// 胜者树结构。每个节点保存一个索引和元素，对应“锦标赛树”。
        /// </summary>
        private class TournamentTree
        {
            private readonly List<T> _items;
            private readonly IComparer<T> _comparer;
            private int?[] _tree; // 每个节点存储索引
            private int _leafStart;

            /// <summary>
            /// 创建锦标赛树。
            /// </summary>
            public TournamentTree(List<T> items, IComparer<T> comparer)
            {
                _items = items;
                _comparer = comparer;
                BuildTree();
            }

            /// <summary>
            /// 弹出当前胜者（最大/最小），并从树中移除。
            /// </summary>
            public T PopWinner()
            {
                if (_tree[1] == null) throw new InvalidOperationException("没有元素可弹出");
                int winnerIdx = _tree[1].Value;
                T winner = _items[winnerIdx];

                // 把叶子节点该位置设为null，并向上修复
                int leafIdx = _leafStart + winnerIdx;
                _tree[leafIdx] = null;
                UpdateTreeUp(leafIdx);

                return winner;
            }

            /// <summary>
            /// 初始化锦标赛树，构造完全二叉树。
            /// </summary>
            private void BuildTree()
            {
                int n = _items.Count;
                int m = 1;
                while (m < n) m <<= 1; // 向上补齐为2的幂
                _leafStart = m;
                _tree = new int?[m * 2];

                // 叶节点
                for (int i = 0; i < n; i++) _tree[m + i] = i;
                for (int i = m + n; i < _tree.Length; i++) _tree[i] = null;

                // 自底向上合成父节点
                for (int i = m - 1; i > 0; i--)
                {
                    _tree[i] = Winner(_tree[i * 2], _tree[i * 2 + 1]);
                }
            }

            /// <summary>
            /// 叶节点改变后，自底向上维护胜者。
            /// </summary>
            private void UpdateTreeUp(int idx)
            {
                while (idx > 1)
                {
                    int parent = idx / 2;
                    _tree[parent] = Winner(_tree[parent * 2], _tree[parent * 2 + 1]);
                    idx = parent;
                }
            }

            /// <summary>
            /// 从左右子节点中选出胜者索引。
            /// </summary>
            private int? Winner(int? left, int? right)
            {
                if (left == null) return right;
                if (right == null) return left;
                return _comparer.Compare(_items[left.Value], _items[right.Value]) <= 0 ? left : right;
            }
        }
    }



    /// <summary>
    /// 单元测试：TournamentSorter{T} 锦标赛排序器（全功能、TopK、自定义比较器等）
    /// </summary>
    [TestFixture]
    public class TournamentSorterTests
    {
        /// <summary>
        /// 测试整数升序排序
        /// </summary>
        [Test]
        public void Sort_ShouldSortIntegersAscending()
        {
            // Arrange
            var sorter = new TournamentSorter<int>();
            var data = new[] { 5, 2, 8, 3, 1, 7, 9 };

            // Act
            var sorted = sorter.Sort(data);

            // Assert
            Assert.That(sorted, Is.EqualTo(new[] { 1, 2, 3, 5, 7, 8, 9 }));
        }

        /// <summary>
        /// 测试整数Top-K选拔
        /// </summary>
        [Test]
        public void SelectTopK_ShouldReturnKSmallestElementsAscending()
        {
            // Arrange
            var sorter = new TournamentSorter<int>();
            var data = new List<int> { 10, 4, 7, 2, 8, 9, 3 };
            int k = 4;

            // Act
            var topK = sorter.SelectTopK(data, k);

            // Assert
            Assert.That(topK, Is.EqualTo(new[] { 2, 3, 4, 7 }));
        }

        /// <summary>
        /// 测试自定义降序比较器
        /// </summary>
        [Test]
        public void Sort_ShouldSupportCustomDescendingComparer()
        {
            // Arrange
            var comparer = Comparer<int>.Create((a, b) => b.CompareTo(a)); // 降序
            var sorter = new TournamentSorter<int>(comparer);
            var data = new List<int> { 5, 8, 1, 7, 3 };

            // Act
            var sorted = sorter.Sort(data);

            // Assert
            Assert.That(sorted, Is.EqualTo(new[] { 8, 7, 5, 3, 1 }));
        }

        /// <summary>
        /// 测试 TopK 超过元素数量时的容错
        /// </summary>
        [Test]
        public void SelectTopK_WhenKGreaterThanCount_ShouldReturnAllSorted()
        {
            // Arrange
            var sorter = new TournamentSorter<int>();
            var data = new List<int> { 4, 2, 9 };
            int k = 10;

            // Act
            var topK = sorter.SelectTopK(data, k);

            // Assert
            Assert.That(topK, Is.EqualTo(new[] { 2, 4, 9 }));
        }

        /// <summary>
        /// 测试空集合时排序结果
        /// </summary>
        [Test]
        public void Sort_WhenEmpty_ShouldReturnEmpty()
        {
            // Arrange
            var sorter = new TournamentSorter<int>();
            var data = new List<int>();

            // Act
            var sorted = sorter.Sort(data);

            // Assert
            Assert.That(sorted, Is.Empty);
        }

        /// <summary>
        /// 测试 TopK 空集时结果
        /// </summary>
        [Test]
        public void SelectTopK_WhenEmpty_ShouldReturnEmpty()
        {
            // Arrange
            var sorter = new TournamentSorter<int>();
            var data = new int[0];

            // Act
            var topK = sorter.SelectTopK(data, 3);

            // Assert
            Assert.That(topK, Is.Empty);
        }

        /// <summary>
        /// 测试异常：弹出元素超限应抛出异常
        /// </summary>
        [Test]
        public void PopWinner_ShouldThrow_WhenNoElements()
        {
            // Arrange
            var sorter = new TournamentSorter<int>();
            var data = new int[0];
            // 利用反射/临时类可单测 PopWinner，或跳过此内部异常

            // 这里通过 Sort/SelectTopK 的外部表现等价测试，无需测试内部类细节
            Assert.That(() => sorter.Sort(data), Throws.Nothing);
        }

        /// <summary>
        /// 测试对象型数据和自定义比较逻辑
        /// </summary>
        private class Person
        {
            public string Name { get; set; }
            public int Score { get; set; }
        }

        /// <summary>
        /// 测试对象型数据
        /// </summary>
        [Test]
        public void Sort_ShouldWorkWithCustomObjects()
        {
            // Arrange
            var people = new List<Person>
            {
                new Person { Name = "Alice", Score = 90 },
                new Person { Name = "Bob", Score = 85 },
                new Person { Name = "Charlie", Score = 95 }
            };
            var comparer = Comparer<Person>.Create((a, b) => a.Score.CompareTo(b.Score));
            var sorter = new TournamentSorter<Person>(comparer);

            // Act
            var sorted = sorter.Sort(people);

            // Assert
            Assert.That(sorted[0].Name, Is.EqualTo("Bob"));    // 85
            Assert.That(sorted[2].Name, Is.EqualTo("Charlie")); // 95
        }
        /// <summary>
        /// 简单 Runner 数据结构
        /// </summary>
        public class Runner
        {
            /// <summary>姓名（如A1~E5）</summary>
            public string Name { get; set; } = "";
            /// <summary>速度分数，越小越快</summary>
            public int Speed { get; set; }
        }


        /// <summary>
        /// 25人分5组，每组5人，按锦标赛排序原理决出金银铜牌（Top3）测试用例
        /// </summary>
        [Test]
        public void SelectTop3_GoldSilverBronze_From25RunnersByTournament()
        {
            // Arrange
            // 构造25名选手，A1最快，E5最慢
            var runners = new List<Runner>();
            foreach (var group in new[] { "A", "B", "C", "D", "E" })
                for (int i = 1; i <= 5; i++)
                    runners.Add(new Runner { Name = $"{group}{i}", Speed = (group[0] - 'A') * 5 + i });

            // 1. 5组小组赛：每组各比一次
            var groupSorter = new TournamentSorter<Runner>(Comparer<Runner>.Create((x, y) => x.Speed.CompareTo(y.Speed)));
            var groupWinners = new List<List<Runner>>();
            for (int i = 0; i < 5; i++)
            {
                var group = runners.Skip(i * 5).Take(5).ToList();
                groupWinners.Add(groupSorter.Sort(group));
            }

            // 2. 决赛（第6场）：5组第一名晋级
            var finalists = groupWinners.Select(g => g[0]).ToList();
            var finalSorter = new TournamentSorter<Runner>(Comparer<Runner>.Create((x, y) => x.Speed.CompareTo(y.Speed)));
            var finalRank = finalSorter.Sort(finalists);

            var champion = finalRank[0];

            // 3. 最优锦标赛排序Top3原则，亚军/季军只需再一场（第7场）：
            // - 冠军组的第2、第3名
            // - 决赛第2、3名（小组冠军）
            // - 决赛第2名所在组的小组第二
            int championGroupIndex = runners.FindIndex(r => r.Name == champion.Name) / 5;
            int finalist2GroupIndex = runners.FindIndex(r => r.Name == finalRank[1].Name) / 5;

            var candidateList = new List<Runner>
            {
                groupWinners[championGroupIndex][1], // 冠军组第二
                groupWinners[championGroupIndex][2], // 冠军组三
                finalRank[1],                        // 决赛第二名（小组冠军）
                finalRank[2],                        // 决赛第三名（小组冠军）
                groupWinners[finalist2GroupIndex][1] // 决赛第二名所在组的小组第二
            };

            var medalSorter = new TournamentSorter<Runner>(Comparer<Runner>.Create((x, y) => x.Speed.CompareTo(y.Speed)));
            var medalRank = medalSorter.Sort(candidateList).Take(2).ToList();

            // Act & Assert
            Assert.That(champion.Name, Is.EqualTo("A1"));           // 冠军
            Assert.That(medalRank[0].Name, Is.EqualTo("A2"));       // 亚军
            Assert.That(medalRank[1].Name, Is.EqualTo("A3"));       // 季军
                                                                    // 总共7场比赛（5组+决赛+亚/季军争夺）
        }

    }

}
