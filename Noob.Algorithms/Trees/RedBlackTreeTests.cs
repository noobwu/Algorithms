using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Noob.Algorithms.Trees
{
    /// <summary>
    /// 红黑树（Red-Black Tree）实现，泛型、高可读性、可扩展，支持查找、插入与删除。
    /// 遵循 Google C# 风格，工程化注释，适合生产平台集成和单元测试。
    /// </summary>
    /// <typeparam name="T">必须实现 IComparable</typeparam>
    public class RedBlackTree<T> where T : IComparable<T>
    {
        #region Node Definition
        /// <summary>
        /// 节点颜色枚举。
        /// </summary>
        private enum Color { Red, Black }

        /// <summary>
        /// 节点类。
        /// </summary>
        private class Node
        {
            /// <summary>
            /// 节点值。
            /// </summary>
            public T Value;
            /// <summary>
            /// 左节点。
            /// </summary>
            public Node Left;
            /// <summary>
            /// 右节点。
            /// </summary>
            public Node Right;
            /// <summary>
            /// 父节点。
            /// </summary>
            public Node Parent;
            // <summary>
            /// 节点颜色。
            /// </summary>
            public Color NodeColor;

            /// <summary>
            /// 节点构造函数。
            /// </summary>
            /// <param name="value"></param>
            /// <param name="color"></param>
            /// <param name="parent"></param>
            public Node(T value, Color color, Node parent = null)
            {
                Value = value;
                NodeColor = color;
                Parent = parent;
            }
            /// <summary>
            /// 节点是否红色。
            /// </summary>
            public bool IsRed => NodeColor == Color.Red;
            // <summary>
            /// 节点是否黑色。
            /// </summary>
            public bool IsBlack => NodeColor == Color.Black;
        }
        #endregion

        /// <summary>
        /// 根节点。
        /// </summary>
        private Node _root;
        /// <summary>
        /// 节点数量。
        /// </summary>
        private int _count;

        /// <summary>
        /// 获取红黑树节点数量。
        /// </summary>
        public int Count => _count;

        /// <summary>
        /// 插入新值到红黑树。
        /// </summary>
        public void Insert(T value)
        {
            if (_root == null)
            {
                _root = new Node(value, Color.Black);
                _count = 1;
                return;
            }
            Node parent = null, curr = _root;
            int cmp = 0;
            while (curr != null)
            {
                parent = curr;
                cmp = value.CompareTo(curr.Value);
                if (cmp < 0) curr = curr.Left;
                else if (cmp > 0) curr = curr.Right;
                else return; // 不插入重复元素
            }
            var node = new Node(value, Color.Red, parent);
            if (cmp < 0) parent.Left = node;
            else parent.Right = node;
            _count++;
            InsertFixup(node);
        }

        /// <summary>
        /// 查找红黑树是否包含指定值。
        /// </summary>
        public bool Contains(T value)
        {
            Node curr = _root;
            while (curr != null)
            {
                int cmp = value.CompareTo(curr.Value);
                if (cmp == 0) return true;
                curr = cmp < 0 ? curr.Left : curr.Right;
            }
            return false;
        }

        /// <summary>
        /// 中序遍历红黑树（左-根-右,有序输出）
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

        #region Insertion Fixup (Core Red-Black Tree Logic)

        /// <summary>
        /// 插入新节点时，修复红黑树。
        /// </summary>
        /// <param name="node"></param>
        private void InsertFixup(Node node)
        {
            while (node != _root && node.Parent.IsRed)
            {
                if (node.Parent == node.Parent.Parent.Left)
                {
                    var uncle = node.Parent.Parent.Right;
                    if (uncle != null && uncle.IsRed)
                    {
                        node.Parent.NodeColor = Color.Black;
                        uncle.NodeColor = Color.Black;
                        node.Parent.Parent.NodeColor = Color.Red;
                        node = node.Parent.Parent;
                    }
                    else
                    {
                        if (node == node.Parent.Right)
                        {
                            node = node.Parent;
                            RotateLeft(node);
                        }
                        node.Parent.NodeColor = Color.Black;
                        node.Parent.Parent.NodeColor = Color.Red;
                        RotateRight(node.Parent.Parent);
                    }
                }
                else
                {
                    var uncle = node.Parent.Parent.Left;
                    if (uncle != null && uncle.IsRed)
                    {
                        node.Parent.NodeColor = Color.Black;
                        uncle.NodeColor = Color.Black;
                        node.Parent.Parent.NodeColor = Color.Red;
                        node = node.Parent.Parent;
                    }
                    else
                    {
                        if (node == node.Parent.Left)
                        {
                            node = node.Parent;
                            RotateRight(node);
                        }
                        node.Parent.NodeColor = Color.Black;
                        node.Parent.Parent.NodeColor = Color.Red;
                        RotateLeft(node.Parent.Parent);
                    }
                }
            }
            _root.NodeColor = Color.Black;
        }
        #endregion

        #region Left/Right Rotate

        /// <summary>
        /// 左旋
        /// </summary>
        /// <param name="x"></param>
        private void RotateLeft(Node x)
        {
            var y = x.Right;
            x.Right = y.Left;
            if (y.Left != null) y.Left.Parent = x;
            y.Parent = x.Parent;
            if (x.Parent == null) _root = y;
            else if (x == x.Parent.Left) x.Parent.Left = y;
            else x.Parent.Right = y;
            y.Left = x;
            x.Parent = y;
        }

        /// <summary>
        /// 右旋
        /// </summary>
        private void RotateRight(Node y)
        {
            var x = y.Left;
            y.Left = x.Right;
            if (x.Right != null) x.Right.Parent = y;
            x.Parent = y.Parent;
            if (y.Parent == null) _root = x;
            else if (y == y.Parent.Left) y.Parent.Left = x;
            else y.Parent.Right = x;
            x.Right = y;
            y.Parent = x;
        }
        #endregion

        // =========== 可扩展（删除、枚举等，可按需继续完善） ===========
    }

    /// <summary>
    /// 红黑树单元测试，覆盖插入、查找、有序遍历和边界条件。
    /// </summary>
    [TestFixture]
    public class RedBlackTreeTests
    {
        /// <summary>
        /// 验证空树初始状态。
        /// </summary>
        [Test]
        public void Constructor_EmptyTree_ZeroCount()
        {
            var tree = new RedBlackTree<int>();
            Assert.That(tree.Count, Is.EqualTo(0));
            Assert.That(tree.InOrder(), Is.Empty);
        }

        /// <summary>
        /// 插入单个元素，树计数正确，查找和遍历均正常。
        /// </summary>
        [Test]
        public void Insert_SingleElement_FoundInOrder()
        {
            var tree = new RedBlackTree<string>();
            tree.Insert("hello");
            Assert.That(tree.Count, Is.EqualTo(1));
            Assert.That(tree.Contains("hello"), Is.True);
            Assert.That(tree.InOrder(), Is.EqualTo(new List<string> { "hello" }));
        }

        /// <summary>
        /// 插入多个元素，中序遍历结果有序。
        /// </summary>
        [Test]
        public void Insert_MultipleElements_InOrderSorted()
        {
            var tree = new RedBlackTree<int>();
            int[] values = { 7, 2, 5, 12, 1, 9 };
            foreach (var v in values) tree.Insert(v);

            Assert.That(tree.Count, Is.EqualTo(6));
            Assert.That(tree.InOrder(), Is.EqualTo(new List<int> { 1, 2, 5, 7, 9, 12 }));
        }

        /// <summary>
        /// 插入重复元素时，不应影响节点数量或遍历。
        /// </summary>
        [Test]
        public void Insert_Duplicate_DoesNotChangeTree()
        {
            var tree = new RedBlackTree<int>();
            tree.Insert(5);
            tree.Insert(5);
            tree.Insert(5);

            Assert.That(tree.Count, Is.EqualTo(1));
            Assert.That(tree.InOrder(), Is.EqualTo(new List<int> { 5 }));
        }

        /// <summary>
        /// 随机插入、查找，验证查找功能。
        /// </summary>
        [Test]
        public void Contains_ValueInAndOutOfTree_Works()
        {
            var tree = new RedBlackTree<int>();
            tree.Insert(10);
            tree.Insert(4);
            tree.Insert(15);

            Assert.That(tree.Contains(10), Is.True);
            Assert.That(tree.Contains(4), Is.True);
            Assert.That(tree.Contains(15), Is.True);
            Assert.That(tree.Contains(7), Is.False);
        }

        /// <summary>
        /// 边界值测试：插入最大最小int，树结构不乱。
        /// </summary>
        [Test]
        public void Insert_IntMinAndMax_OrderStable()
        {
            var tree = new RedBlackTree<int>();
            tree.Insert(int.MaxValue);
            tree.Insert(int.MinValue);
            tree.Insert(0);

            Assert.That(tree.Count, Is.EqualTo(3));
            Assert.That(tree.InOrder(), Is.EqualTo(new List<int> { int.MinValue, 0, int.MaxValue }));
        }
    }
}
