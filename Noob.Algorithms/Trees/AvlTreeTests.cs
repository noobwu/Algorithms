using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Noob.Algorithms.Trees
{
    /// <summary>
    /// AVL树（自平衡二叉查找树）实现。插入、查找和删除时间复杂度均为O(log n)。
    /// 适用于需要严格平衡和高效查找的有序集合场景。
    /// </summary>
    /// <typeparam name="T">元素类型，需实现IComparable&lt;T&gt;。</typeparam>
    public class AvlTree<T> where T : IComparable<T>
    {
        /// <summary>
        /// AVL树节点。
        /// </summary>
        private class Node
        {
            public T Value;
            public Node Left, Right, Parent;
            public int Height;

            public Node(T value, Node parent = null)
            {
                Value = value;
                Parent = parent;
                Height = 1;
            }
        }

        private Node _root;
        private int _count;

        /// <summary>
        /// AVL树中元素数量。
        /// </summary>
        public int Count => _count;

        /// <summary>
        /// 插入元素到AVL树。
        /// </summary>
        /// <param name="value">要插入的元素。</param>
        public void Insert(T value)
        {
            _root = Insert(_root, value, null);
        }

        private Node Insert(Node node, T value, Node parent)
        {
            if (node == null)
            {
                _count++;
                return new Node(value, parent);
            }
            int cmp = value.CompareTo(node.Value);
            if (cmp < 0)
                node.Left = Insert(node.Left, value, node);
            else if (cmp > 0)
                node.Right = Insert(node.Right, value, node);
            else
                return node; // 不插入重复

            UpdateHeight(node);
            return Balance(node);
        }

        /// <summary>
        /// 判断AVL树中是否包含某元素。
        /// </summary>
        public bool Contains(T value)
        {
            var node = _root;
            while (node != null)
            {
                int cmp = value.CompareTo(node.Value);
                if (cmp == 0) return true;
                node = cmp < 0 ? node.Left : node.Right;
            }
            return false;
        }

        /// <summary>
        /// 返回中序遍历结果（有序集合）。
        /// </summary>
        public List<T> InOrder()
        {
            var result = new List<T>();
            void Traverse(Node n)
            {
                if (n == null) return;
                Traverse(n.Left);
                result.Add(n.Value);
                Traverse(n.Right);
            }
            Traverse(_root);
            return result;
        }

        #region 平衡性维护与旋转

        /// <summary>
        /// 获取节点高度。
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        private int Height(Node n) => n?.Height ?? 0;

        /// <summary>
        /// 更新节点高度。
        /// </summary>
        private void UpdateHeight(Node n)
        {
            n.Height = 1 + Math.Max(Height(n.Left), Height(n.Right));
        }

        /// <summary>
        /// 获取节点的平衡因子。
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        private int BalanceFactor(Node n) => Height(n.Left) - Height(n.Right);

        /// <summary>
        /// AVL树插入/删除后平衡修正。
        /// </summary>
        private Node Balance(Node node)
        {
            int bf = BalanceFactor(node);
            if (bf > 1)
            {
                if (BalanceFactor(node.Left) < 0)
                    node.Left = RotateLeft(node.Left);
                return RotateRight(node);
            }
            if (bf < -1)
            {
                if (BalanceFactor(node.Right) > 0)
                    node.Right = RotateRight(node.Right);
                return RotateLeft(node);
            }
            return node;
        }

        /// <summary>
        /// 左旋操作。
        /// </summary>
        private Node RotateLeft(Node x)
        {
            var y = x.Right;
            x.Right = y.Left;
            if (y.Left != null) y.Left.Parent = x;
            y.Left = x;
            y.Parent = x.Parent;
            x.Parent = y;
            UpdateHeight(x);
            UpdateHeight(y);
            return y;
        }

        /// <summary>
        /// 右旋操作。
        /// </summary>
        private Node RotateRight(Node y)
        {
            var x = y.Left;
            y.Left = x.Right;
            if (x.Right != null) x.Right.Parent = y;
            x.Right = y;
            x.Parent = y.Parent;
            y.Parent = x;
            UpdateHeight(y);
            UpdateHeight(x);
            return x;
        }
        #endregion

        // =========== 可扩展（删除、遍历等接口可补充） ===========

        /// <summary>
        /// 返回树的高度。
        /// </summary>
        public int GetHeight() => Height(_root);

        // TODO: public void Remove(T value) { ... }
        // TODO: public IEnumerable<T> PreOrder() { ... }
        // TODO: public IEnumerable<T> PostOrder() { ... }
    }

    /// <summary>
    /// AVL树单元测试，覆盖插入、查找、有序遍历与平衡性。
    /// </summary>
    [TestFixture]
    public class AvlTreeTests
    {
        /// <summary>
        /// 新建空树时节点数应为0，中序遍历为空。
        /// </summary>
        [Test]
        public void Constructor_EmptyTree_ZeroCount()
        {
            var tree = new AvlTree<int>();
            Assert.That(tree.Count, Is.EqualTo(0));
            Assert.That(tree.InOrder(), Is.Empty);
        }

        /// <summary>
        /// 插入单个元素后能查找到该元素，计数正确。
        /// </summary>
        [Test]
        public void Insert_SingleElement_FoundAndCount()
        {
            var tree = new AvlTree<string>();
            tree.Insert("abc");
            Assert.That(tree.Count, Is.EqualTo(1));
            Assert.That(tree.Contains("abc"), Is.True);
            Assert.That(tree.InOrder(), Is.EqualTo(new List<string> { "abc" }));
        }

        /// <summary>
        /// 插入多个乱序元素，中序遍历输出有序。
        /// </summary>
        [Test]
        public void Insert_MultipleElements_InOrderSorted()
        {
            var tree = new AvlTree<int>();
            int[] vals = { 7, 3, 11, 1, 5, 9, 13 };
            foreach (var v in vals) tree.Insert(v);

            Assert.That(tree.Count, Is.EqualTo(7));
            Assert.That(tree.InOrder(), Is.EqualTo(new List<int> { 1, 3, 5, 7, 9, 11, 13 }));
        }

        /// <summary>
        /// 多次插入重复元素不影响节点数与有序性。
        /// </summary>
        [Test]
        public void Insert_Duplicate_NoChange()
        {
            var tree = new AvlTree<int>();
            tree.Insert(5);
            tree.Insert(5);
            tree.Insert(5);
            Assert.That(tree.Count, Is.EqualTo(1));
            Assert.That(tree.InOrder(), Is.EqualTo(new List<int> { 5 }));
        }

        /// <summary>
        /// 查找存在和不存在的元素均能得到正确结果。
        /// </summary>
        [Test]
        public void Contains_ExistingAndMissing_Works()
        {
            var tree = new AvlTree<int>();
            tree.Insert(6);
            tree.Insert(2);
            tree.Insert(8);

            Assert.That(tree.Contains(6), Is.True);
            Assert.That(tree.Contains(2), Is.True);
            Assert.That(tree.Contains(8), Is.True);
            Assert.That(tree.Contains(0), Is.False);
        }

        /// <summary>
        /// 插入极大极小值，保证树有序且计数正确。
        /// </summary>
        [Test]
        public void Insert_IntMinMax_OrderAndCount()
        {
            var tree = new AvlTree<int>();
            tree.Insert(int.MinValue);
            tree.Insert(0);
            tree.Insert(int.MaxValue);

            Assert.That(tree.Count, Is.EqualTo(3));
            Assert.That(tree.InOrder(), Is.EqualTo(new List<int> { int.MinValue, 0, int.MaxValue }));
        }

        /// <summary>
        /// 插入升序或降序数据，AVL树高度始终O(log n)。
        /// </summary>
        [Test]
        public void Insert_SortedData_HeightLogN()
        {
            var tree = new AvlTree<int>();
            for (int i = 0; i < 1000; ++i)
                tree.Insert(i);

            int h = tree.GetHeight();
            // AVL树高度上界约1.44*log2(n+2)
            Assert.That(h, Is.LessThanOrEqualTo((int)(1.45 * System.Math.Log(1002, 2))));
        }
    }
}
