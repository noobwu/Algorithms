using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Noob.Algorithms.Trees
{

    /// <summary>
    /// B+树节点类型（内部节点/叶子节点）
    /// </summary>
    public enum BPlusTreeNodeType
    {
        /// <summary>
        /// 内部节点
        /// </summary>
        Internal,
        // <summary>
        /// 叶子节点
        /// </summary>
        Leaf
    }

    /// <summary>
    /// B+树持久化快照结构（去除父指针、NextLeaf引用，仅用于序列化）
    /// </summary>
    public class BPlusTreeSnapshot<K, V>
    {
        public int Order { get; set; }
        /// <summary>
        ///  根节点
        /// </summary>
        public SnapshotNode<K, V> Root { get; set; }
    }

    /// <summary>
    /// B+树快照节点
    /// </summary>
    /// <typeparam name="K"></typeparam>
    /// <typeparam name="V"></typeparam>
    public class SnapshotNode<K, V>
    {
        /// <summary>
        /// 是否为叶子节点
        /// </summary>
        public bool IsLeaf { get; set; }
        /// <summary>
        /// 关键字列表
        /// </summary>
        public List<K> Keys { get; set; }

        /// <summary>
        /// 值列表
        /// </summary>
        public List<V> Values { get; set; } // 叶子节点
        /// <summary>
        /// 叶子节点
        /// </summary>
        public List<SnapshotNode<K, V>> Children { get; set; } // 内部节点
    }
    /// <summary>
    /// B+树节点，泛型K为Key类型，V为Value类型
    /// </summary>
    public class BPlusTreeNode<K, V> where K : IComparable<K>
    {
        /// <summary>节点类型（内部/叶子）</summary>
        public BPlusTreeNodeType NodeType { get; set; }

        /// <summary>关键字列表（升序排列）</summary>
        public List<K> Keys { get; set; } = new List<K>();

        /// <summary>子节点指针（仅内部节点有效）</summary>
        public List<BPlusTreeNode<K, V>> Children { get; set; }

        /// <summary>值列表（仅叶子节点有效）</summary>
        public List<V> Values { get; set; }

        /// <summary>叶子节点指针：下一个叶子</summary>
        public BPlusTreeNode<K, V> NextLeaf { get; set; }

        /// <summary>父节点指针</summary>
        public BPlusTreeNode<K, V> Parent { get; set; }

        /// <summary>初始化节点</summary>
        public BPlusTreeNode(BPlusTreeNodeType nodeType)
        {
            NodeType = nodeType;
            if (nodeType == BPlusTreeNodeType.Internal)
                Children = new List<BPlusTreeNode<K, V>>();
            else
                Values = new List<V>();
        }
    }

    /// <summary>
    /// B+树实现类（支持查找、插入、范围查询等核心功能）
    /// </summary>
    public class BPlusTree<K, V> where K : IComparable<K>
    {
        /// <summary>根节点</summary>
        public BPlusTreeNode<K, V> Root { get; private set; }

        /// <summary>树的阶数（每个节点的最大孩子数）</summary>
        public int Order { get; }

        /// <summary>叶子节点最小元素数（ceil(Order/2)）</summary>
        private int MinKeys => (int)Math.Ceiling(Order / 2.0) - 1;

        /// <summary>内部节点最小孩子数</summary>
        private int MinChildren => (int)Math.Ceiling(Order / 2.0);

        /// <summary>构造B+树，指定阶数（通常64~256为宜）</summary>
        public BPlusTree(int order = 64)
        {
            if (order < 3)
                throw new ArgumentException("B+树阶数必须>=3");
            Order = order;
            Root = new BPlusTreeNode<K, V>(BPlusTreeNodeType.Leaf);
        }

        /// <summary>
        /// 查找指定Key的值，找不到抛出异常
        /// </summary>
        public V Search(K key)
        {
            var node = FindLeafNode(key);
            int idx = node.Keys.BinarySearch(key);
            if (idx >= 0) return node.Values[idx];
            throw new KeyNotFoundException("Key not found: " + key);
        }

        /// <summary>
        /// 尝试查找指定Key的值，找不到返回false
        /// </summary>
        public bool TrySearch(K key, out V value)
        {
            var node = FindLeafNode(key);
            int idx = node.Keys.BinarySearch(key);
            if (idx >= 0)
            {
                value = node.Values[idx];
                return true;
            }
            value = default;
            return false;
        }

        /// <summary>
        /// 插入（或更新）一个键值对。已存在则覆盖
        /// </summary>
        public void Insert(K key, V value)
        {
            var leaf = FindLeafNode(key);
            int idx = leaf.Keys.BinarySearch(key);
            if (idx >= 0)
            {
                leaf.Values[idx] = value; // 覆盖
                return;
            }
            idx = ~idx;
            leaf.Keys.Insert(idx, key);
            leaf.Values.Insert(idx, value);

            if (leaf.Keys.Count >= Order)
                SplitLeaf(leaf);
        }

        /// <summary>
        /// 范围查询：返回所有Key在区间[begin, end]内的键值对
        /// </summary>
        public List<KeyValuePair<K, V>> RangeQuery(K begin, K end)
        {
            var result = new List<KeyValuePair<K, V>>();
            var node = FindLeafNode(begin);
            while (node != null)
            {
                for (int i = 0; i < node.Keys.Count; i++)
                {
                    var key = node.Keys[i];
                    if (key.CompareTo(begin) < 0) continue;
                    if (key.CompareTo(end) > 0) return result;
                    result.Add(new KeyValuePair<K, V>(key, node.Values[i]));
                }
                node = node.NextLeaf;
            }
            return result;
        }

        /// <summary>
        /// 查找包含Key的叶子节点
        /// </summary>
        private BPlusTreeNode<K, V> FindLeafNode(K key)
        {
            var node = Root;
            while (node.NodeType == BPlusTreeNodeType.Internal)
            {
                int idx = node.Keys.BinarySearch(key);
                idx = idx >= 0 ? idx + 1 : ~idx;
                node = node.Children[idx];
            }
            return node;
        }

        /// <summary>
        /// 分裂叶子节点（插入后节点超限）
        /// </summary>
        private void SplitLeaf(BPlusTreeNode<K, V> leaf)
        {
            int mid = leaf.Keys.Count / 2;
            var newLeaf = new BPlusTreeNode<K, V>(BPlusTreeNodeType.Leaf)
            {
                Parent = leaf.Parent
            };
            newLeaf.Keys.AddRange(leaf.Keys.GetRange(mid, leaf.Keys.Count - mid));
            newLeaf.Values.AddRange(leaf.Values.GetRange(mid, leaf.Values.Count - mid));
            leaf.Keys.RemoveRange(mid, leaf.Keys.Count - mid);
            leaf.Values.RemoveRange(mid, leaf.Values.Count - mid);

            // 链表指针维护
            newLeaf.NextLeaf = leaf.NextLeaf;
            leaf.NextLeaf = newLeaf;

            InsertIntoParent(leaf, newLeaf.Keys[0], newLeaf);
        }

        /// <summary>
        /// 分裂内部节点（递归向上传递分裂）
        /// </summary>
        private void SplitInternal(BPlusTreeNode<K, V> node)
        {
            int mid = node.Keys.Count / 2;
            K upKey = node.Keys[mid];

            var newNode = new BPlusTreeNode<K, V>(BPlusTreeNodeType.Internal)
            {
                Parent = node.Parent
            };

            // 新节点key和孩子（右半部分，key不含mid）
            newNode.Keys.AddRange(node.Keys.GetRange(mid + 1, node.Keys.Count - (mid + 1)));
            newNode.Children.AddRange(node.Children.GetRange(mid + 1, node.Children.Count - (mid + 1)));

            // 原节点key和孩子（左半部分，key不含mid右侧）
            node.Keys.RemoveRange(mid, node.Keys.Count - mid); // mid之后都删
            node.Children.RemoveRange(mid + 1, node.Children.Count - (mid + 1)); // mid+1之后都删

            // 子节点父指针更新
            foreach (var child in newNode.Children)
                child.Parent = newNode;

            // 插入到父节点（upKey是被提升的key，右侧新节点）
            InsertIntoParent(node, upKey, newNode);
        }

        /// <summary>
        /// 插入新节点到父节点，递归处理根节点分裂
        /// </summary>
        private void InsertIntoParent(BPlusTreeNode<K, V> left, K key, BPlusTreeNode<K, V> right)
        {
            if (left.Parent == null)
            {
                // 创建新根
                var newRoot = new BPlusTreeNode<K, V>(BPlusTreeNodeType.Internal);
                newRoot.Keys.Add(key);
                newRoot.Children.Add(left);
                newRoot.Children.Add(right);
                left.Parent = right.Parent = newRoot;
                Root = newRoot;
                return;
            }

            var parent = left.Parent;
            int idx = parent.Children.IndexOf(left);
            parent.Keys.Insert(idx, key);
            parent.Children.Insert(idx + 1, right);
            right.Parent = parent;

            if (parent.Keys.Count >= Order)
                SplitInternal(parent);
        }

        /// <summary>
        /// 删除指定Key，若不存在则无操作
        /// </summary>
        public void Delete(K key)
        {
            var leaf = FindLeafNode(key);
            int idx = leaf.Keys.BinarySearch(key);
            if (idx < 0) return; // 不存在
            leaf.Keys.RemoveAt(idx);
            leaf.Values.RemoveAt(idx);

            // 检查是否需要合并或借用
            if (leaf == Root)
            {
                if (leaf.Keys.Count == 0)
                    Root = new BPlusTreeNode<K, V>(BPlusTreeNodeType.Leaf);
                return;
            }
            if (leaf.Keys.Count < MinKeys)
                RebalanceAfterDelete(leaf);
        }

        /// <summary>
        /// 删除后节点重平衡（向兄弟借用或合并）
        /// </summary>
        private void RebalanceAfterDelete(BPlusTreeNode<K, V> node)
        {
            var parent = node.Parent;
            if (parent == null) return; // 根节点，直接忽略
            int idx = parent.Children.IndexOf(node);

            // 尝试向左兄弟借
            if (idx > 0 && parent.Children[idx - 1].Keys.Count > MinKeys)
            {
                var left = parent.Children[idx - 1];
                node.Keys.Insert(0, left.Keys[^1]);
                node.Values.Insert(0, left.Values[^1]);
                left.Keys.RemoveAt(left.Keys.Count - 1);
                left.Values.RemoveAt(left.Values.Count - 1);
                parent.Keys[idx - 1] = node.Keys[0];
                return;
            }
            // 尝试向右兄弟借
            if (idx < parent.Children.Count - 1 && parent.Children[idx + 1].Keys.Count > MinKeys)
            {
                var right = parent.Children[idx + 1];
                node.Keys.Add(right.Keys[0]);
                node.Values.Add(right.Values[0]);
                right.Keys.RemoveAt(0);
                right.Values.RemoveAt(0);
                parent.Keys[idx] = right.Keys[0];
                return;
            }
            // 合并
            if (idx > 0)
            {
                var left = parent.Children[idx - 1];
                left.Keys.AddRange(node.Keys);
                left.Values.AddRange(node.Values);
                left.NextLeaf = node.NextLeaf;
                parent.Keys.RemoveAt(idx - 1);
                parent.Children.RemoveAt(idx);
                if (parent == Root && parent.Keys.Count == 0)
                    Root = left;
                else if (parent.Keys.Count < MinKeys)
                    RebalanceAfterDelete(parent);
            }
            else
            {
                var right = parent.Children[idx + 1];
                node.Keys.AddRange(right.Keys);
                node.Values.AddRange(right.Values);
                node.NextLeaf = right.NextLeaf;
                parent.Keys.RemoveAt(idx);
                parent.Children.RemoveAt(idx + 1);
                if (parent == Root && parent.Keys.Count == 0)
                    Root = node;
                else if (parent.Keys.Count < MinKeys)
                    RebalanceAfterDelete(parent);
            }
        }

        /// <summary>
        /// 批量构建B+树（适合一次性大量数据/初始化）
        /// </summary>
        /// <param name="items">已排序的键值对集合</param>
        public void BulkLoad(IEnumerable<KeyValuePair<K, V>> items)
        {
            var sortedItems = new List<KeyValuePair<K, V>>(items);
            sortedItems.Sort((a, b) => a.Key.CompareTo(b.Key));

            List<BPlusTreeNode<K, V>> leaves = new List<BPlusTreeNode<K, V>>();
            for (int i = 0; i < sortedItems.Count; i += Order - 1)
            {
                var leaf = new BPlusTreeNode<K, V>(BPlusTreeNodeType.Leaf);
                int cnt = Math.Min(Order - 1, sortedItems.Count - i);
                for (int j = 0; j < cnt; j++)
                {
                    leaf.Keys.Add(sortedItems[i + j].Key);
                    leaf.Values.Add(sortedItems[i + j].Value);
                }
                if (leaves.Count > 0)
                    leaves[^1].NextLeaf = leaf;
                leaves.Add(leaf);
            }

            // 构建父层
            while (leaves.Count > 1)
            {
                List<BPlusTreeNode<K, V>> parents = new List<BPlusTreeNode<K, V>>();
                for (int i = 0; i < leaves.Count; i += Order)
                {
                    var parent = new BPlusTreeNode<K, V>(BPlusTreeNodeType.Internal);
                    int cnt = Math.Min(Order, leaves.Count - i);
                    for (int j = 0; j < cnt; j++)
                    {
                        parent.Children.Add(leaves[i + j]);
                        leaves[i + j].Parent = parent;
                        if (j > 0)
                            parent.Keys.Add(leaves[i + j].Keys[0]);
                    }
                    parents.Add(parent);
                }
                leaves = parents;
            }
            Root = leaves[0];
        }

        /// <summary>
        /// 导出树快照为序列化数据（推荐持久化方式）
        /// </summary>
        public void SaveToFile(string filePath)
        {
            var snapshot = new BPlusTreeSnapshot<K, V>
            {
                Order = this.Order,
                Root = ToSnapshotNode(this.Root)
            };
            var json = JsonSerializer.Serialize(snapshot, new JsonSerializerOptions { WriteIndented = false });
            File.WriteAllText(filePath, json);
        }

        /// <summary>
        /// 将当前节点递归转为快照节点（用于序列化）
        /// </summary>
        private static SnapshotNode<K, V> ToSnapshotNode(BPlusTreeNode<K, V> node)
        {
            var snap = new SnapshotNode<K, V>
            {
                IsLeaf = node.NodeType == BPlusTreeNodeType.Leaf,
                Keys = new List<K>(node.Keys)
            };
            if (snap.IsLeaf)
                snap.Values = new List<V>(node.Values);
            else
            {
                snap.Children = new List<SnapshotNode<K, V>>();
                foreach (var child in node.Children)
                    snap.Children.Add(ToSnapshotNode(child));
            }
            return snap;
        }

        /// <summary>
        /// 加载快照文件重建B+树（生产推荐）
        /// </summary>
        public static BPlusTree<K, V> LoadFromFile(string filePath)
        {
            var json = File.ReadAllText(filePath);
            var snapshot = JsonSerializer.Deserialize<BPlusTreeSnapshot<K, V>>(json);
            var tree = new BPlusTree<K, V>(snapshot.Order)
            {
                Root = FromSnapshotNode(snapshot.Root, null)
            };
            // 加载后需要补充叶子链表（可选）
            tree.RebuildLeafLinks();
            return tree;
        }

        /// <summary>
        /// 递归还原快照节点为树节点
        /// </summary>
        private static BPlusTreeNode<K, V> FromSnapshotNode(SnapshotNode<K, V> snap, BPlusTreeNode<K, V> parent)
        {
            var node = new BPlusTreeNode<K, V>(snap.IsLeaf ? BPlusTreeNodeType.Leaf : BPlusTreeNodeType.Internal)
            {
                Parent = parent,
                Keys = new List<K>(snap.Keys)
            };
            if (snap.IsLeaf)
                node.Values = new List<V>(snap.Values);
            else
            {
                node.Children = new List<BPlusTreeNode<K, V>>();
                foreach (var c in snap.Children)
                    node.Children.Add(FromSnapshotNode(c, node));
            }
            return node;
        }


        /// <summary>
        /// 重新连接所有叶子节点的NextLeaf指针（反序列化后需要）
        /// </summary>
        public void RebuildLeafLinks()
        {
            BPlusTreeNode<K, V> prev = null;
            void Traverse(BPlusTreeNode<K, V> node)
            {
                if (node == null) return;
                if (node.NodeType == BPlusTreeNodeType.Leaf)
                {
                    if (prev != null)
                        prev.NextLeaf = node;
                    prev = node;
                }
                else
                {
                    foreach (var child in node.Children)
                        Traverse(child);
                }
            }
            Traverse(Root);
        }

    }



    /// <summary>
    /// BPlusTree测试用例，涵盖查找、插入、删除、范围、批量构建、持久化等
    /// </summary>
    [TestFixture]
    public class BPlusTreeTests
    {
        /// <summary>
        /// 测试简单插入和查找功能
        /// </summary>
        [Test]
        public void Insert_And_Search_Works()
        {
            var tree = new BPlusTree<int, string>(order: 4);

            tree.Insert(1, "A");
            tree.Insert(2, "B");
            tree.Insert(3, "C");

            Assert.That(tree.Search(1), Is.EqualTo("A"));
            Assert.That(tree.Search(2), Is.EqualTo("B"));
            Assert.That(tree.Search(3), Is.EqualTo("C"));

            Assert.That(() => tree.Search(100), Throws.TypeOf<KeyNotFoundException>());
        }

        /// <summary>
        /// 插入重复Key时应覆盖值
        /// </summary>
        [Test]
        public void Insert_DuplicateKey_ShouldUpdateValue()
        {
            var tree = new BPlusTree<int, string>(4);

            tree.Insert(5, "X");
            tree.Insert(5, "Y");

            Assert.That(tree.Search(5), Is.EqualTo("Y"));
        }

        /// <summary>
        /// 范围查询能返回正确区间
        /// </summary>
        [Test]
        public void RangeQuery_Returns_Correct_Results()
        {
            var tree = new BPlusTree<int, string>(4);
            for (int i = 1; i <= 10; i++)
                tree.Insert(i, $"S{i}");

            var res = tree.RangeQuery(4, 7);
            var expected = new List<KeyValuePair<int, string>> {
            new(4, "S4"), new(5, "S5"), new(6, "S6"), new(7, "S7")
        };

            Assert.That(res, Is.EqualTo(expected));
        }

        /// <summary>
        /// 删除节点并保证结构平衡
        /// </summary>
        [Test]
        public void Delete_RemovesKey_And_Rebalances()
        {
            var tree = new BPlusTree<int, string>(4);
            for (int i = 1; i <= 6; i++)
                tree.Insert(i, $"V{i}");

            tree.Delete(3);
            tree.Delete(1);
            tree.Delete(6);

            Assert.That(tree.TrySearch(3, out _), Is.False);
            Assert.That(tree.TrySearch(1, out _), Is.False);
            Assert.That(tree.TrySearch(6, out _), Is.False);

            Assert.That(tree.Search(2), Is.EqualTo("V2"));
            Assert.That(tree.Search(4), Is.EqualTo("V4"));
        }

        /// <summary>
        /// 支持批量构建功能
        /// </summary>
        [Test]
        public void BulkLoad_Works_Correctly()
        {
            var tree = new BPlusTree<int, string>(4);
            var items = new List<KeyValuePair<int, string>>();
            for (int i = 10; i <= 30; i += 2)
                items.Add(new(i, $"E{i}"));

            tree.BulkLoad(items);

            Assert.That(tree.Search(10), Is.EqualTo("E10"));
            Assert.That(tree.Search(30), Is.EqualTo("E30"));
            Assert.That(tree.TrySearch(12, out var v) && v == "E12", Is.True);
            Assert.That(tree.TrySearch(13, out _), Is.False);
        }

        /// <summary>
        /// 持久化保存与加载一致性测试
        /// </summary>
        [Test]
        public void SaveToFile_And_LoadFromFile_RestoreState()
        {
            var filePath = Path.GetTempFileName();
            //filePath = $"{AppDomain.CurrentDomain.BaseDirectory}\\{Guid.NewGuid().ToString("N")}.json";
            try
            {
                var tree1 = new BPlusTree<int, string>(4);
                for (int i = 1; i <= 20; i++)
                    tree1.Insert(i, $"T{i}");
                tree1.SaveToFile(filePath);

                var tree2 = BPlusTree<int, string>.LoadFromFile(filePath);

                for (int i = 1; i <= 20; i++)
                    Assert.That(tree2.Search(i), Is.EqualTo($"T{i}"));
                Assert.That(tree2.TrySearch(99, out _), Is.False);
            }
            finally
            {
                if (File.Exists(filePath)) File.Delete(filePath);
            }
        }

        /// <summary>
        /// 叶子节点链表重建正确性测试
        /// </summary>
        [Test]
        public void Leaf_NextLeaf_Link_Correct_After_Restore()
        {
            var tree = new BPlusTree<int, string>(4);
            for (int i = 1; i <= 12; i++)
                tree.Insert(i, $"N{i}");
            var filePath = Path.GetTempFileName();
            tree.SaveToFile(filePath);
            var loaded = BPlusTree<int, string>.LoadFromFile(filePath);

            // 检查叶链表按顺序遍历所有key
            var allKeys = new List<int>();
            var node = loaded.Root;
            while (node.NodeType != BPlusTreeNodeType.Leaf)
                node = node.Children[0];

            while (node != null)
            {
                allKeys.AddRange(node.Keys);
                node = node.NextLeaf;
            }
            Assert.That(allKeys, Is.EqualTo(new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 }));
            File.Delete(filePath);
        }
    }


}
