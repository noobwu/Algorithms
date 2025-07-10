using NUnit.Framework;
using System;
using System.Collections.Generic;


namespace Noob.Algorithms.Trees
{
    /// <summary>
    /// 表示一个二叉查找树（Binary Search Tree, BST）节点。
    /// </summary>
    /// <typeparam name="T">节点存储的数据类型，要求可比较。</typeparam>
    public class BinaryTreeNode<T> where T : IComparable<T>
    {
        /// <summary>节点数据</summary>
        public T Value { get; set; }

        /// <summary>左子节点</summary>
        public BinaryTreeNode<T> Left { get; set; }

        /// <summary>右子节点</summary>
        public BinaryTreeNode<T> Right { get; set; }

        /// <summary>父节点（可选，可用于遍历优化）</summary>
        public BinaryTreeNode<T> Parent { get; set; }

        /// <summary>
        /// 创建节点
        /// </summary>
        public BinaryTreeNode(T value)
        {
            Value = value;
        }
    }

    /// <summary>
    /// 平台工程级二叉查找树（Binary Search Tree, BST）实现。
    /// </summary>
    /// <typeparam name="T">节点存储的数据类型，要求可比较。</typeparam>
    public class BinarySearchTree<T> where T : IComparable<T>
    {
        /// <summary>树的根节点</summary>
        public BinaryTreeNode<T> Root { get; private set; }

        /// <summary>
        /// 插入一个元素到BST中
        /// </summary>
        public void Insert(T value)
        {
            Root = Insert(Root, value, null);
        }

        /// <summary>
        /// 插入一个元素到BST中
        /// </summary>
        /// <param name="node"></param>
        /// <param name="value"></param>
        /// <param name="parent"></param>
        /// <returns></returns>
        private BinaryTreeNode<T> Insert(BinaryTreeNode<T> node, T value, BinaryTreeNode<T> parent)
        {
            if (node == null)
            {
                var newNode = new BinaryTreeNode<T>(value) { Parent = parent };
                return newNode;
            }

            int cmp = value.CompareTo(node.Value);
            if (cmp < 0)
                node.Left = Insert(node.Left, value, node);
            else if (cmp > 0)
                node.Right = Insert(node.Right, value, node);
            // 相等元素不插入（可自定义为允许重复）
            return node;
        }

        /// <summary>
        /// 判断BST中是否包含某个元素
        /// </summary>
        public bool Contains(T value)
        {
            return FindNode(value) != null;
        }

        /// <summary>
        /// 查找某个元素并返回节点
        /// </summary>
        public BinaryTreeNode<T> FindNode(T value)
        {
            var node = Root;
            while (node != null)
            {
                int cmp = value.CompareTo(node.Value);
                if (cmp == 0) return node;
                node = cmp < 0 ? node.Left : node.Right;
            }
            return null;
        }

        /// <summary>
        /// 删除一个元素（如存在则删除，仅删第一个匹配）
        /// </summary>
        public void Remove(T value)
        {
            Root = Remove(Root, value);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="node"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private BinaryTreeNode<T> Remove(BinaryTreeNode<T> node, T value)
        {
            if (node == null) return null;
            int cmp = value.CompareTo(node.Value);
            if (cmp < 0)
                node.Left = Remove(node.Left, value);
            else if (cmp > 0)
                node.Right = Remove(node.Right, value);
            else
            {
                // 单子节点或无子节点
                if (node.Left == null) return node.Right;
                if (node.Right == null) return node.Left;
                // 双子节点，替换为右子树最小节点
                var minNode = FindMin(node.Right);
                node.Value = minNode.Value;
                node.Right = Remove(node.Right, minNode.Value);
            }
            return node;
        }

        /// <summary>
        /// 返回最小节点
        /// </summary>
        public BinaryTreeNode<T> FindMin(BinaryTreeNode<T> node = null)
        {
            node ??= Root;
            if (node == null) return null;
            while (node.Left != null) node = node.Left;
            return node;
        }

        /// <summary>
        /// 返回最大节点
        /// </summary>
        public BinaryTreeNode<T> FindMax(BinaryTreeNode<T> node = null)
        {
            node ??= Root;
            if (node == null) return null;
            while (node.Right != null) node = node.Right;
            return node;
        }

        /// <summary>
        /// 中序遍历（返回有序序列）
        /// </summary>
        public List<T> InOrder()
        {
            var result = new List<T>();
            InOrder(Root, result);
            return result;
        }

        /// <summary>
        /// 中序遍历
        /// </summary>
        /// <param name="node"></param>
        /// <param name="result"></param>
        private void InOrder(BinaryTreeNode<T> node, List<T> result)
        {
            if (node == null) return;
            InOrder(node.Left, result);
            result.Add(node.Value);
            InOrder(node.Right, result);
        }

        /// <summary>
        /// 前序遍历
        /// </summary>
        public List<T> PreOrder()
        {
            var result = new List<T>();
            PreOrder(Root, result);
            return result;
        }

        /// <summary>
        /// 前序遍历
        /// </summary>
        private void PreOrder(BinaryTreeNode<T> node, List<T> result)
        {
            if (node == null) return;
            result.Add(node.Value);
            PreOrder(node.Left, result);
            PreOrder(node.Right, result);
        }

        /// <summary>
        /// 后序遍历
        /// </summary>
        public List<T> PostOrder()
        {
            var result = new List<T>();
            PostOrder(Root, result);
            return result;
        }

        /// <summary>
        /// 后序遍历
        /// </summary>
        private void PostOrder(BinaryTreeNode<T> node, List<T> result)
        {
            if (node == null) return;
            PostOrder(node.Left, result);
            PostOrder(node.Right, result);
            result.Add(node.Value);
        }

        /// <summary>
        /// 层序遍历（广度优先）
        /// </summary>
        public List<T> LevelOrder()
        {
            var result = new List<T>();
            if (Root == null) return result;
            var queue = new Queue<BinaryTreeNode<T>>();
            queue.Enqueue(Root);
            while (queue.Count > 0)
            {
                var node = queue.Dequeue();
                result.Add(node.Value);
                if (node.Left != null) queue.Enqueue(node.Left);
                if (node.Right != null) queue.Enqueue(node.Right);
            }
            return result;
        }

        /// <summary>
        /// 返回树的高度
        /// </summary>
        /// <returns></returns>
        public int Height()
        {
            return Height(Root);
        }

        /// <summary>
        /// 返回树的高度
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private int Height(BinaryTreeNode<T> node)
        {
            if (node == null) return 0;
            return 1 + Math.Max(Height(node.Left), Height(node.Right));
        }
      
    }

    /// <summary>
    /// BinarySearchTree 平台工程级单元测试
    /// </summary>
    [TestFixture]
    public class BinarySearchTreeTests
    {
        /// <summary>
        /// 测试：基本插入与查找
        /// </summary>
        [Test]
        public void Insert_And_Contains_Works()
        {
            var bst = new BinarySearchTree<int>();
            bst.Insert(5);
            bst.Insert(3);
            bst.Insert(7);

            Assert.That(bst.Contains(3), Is.True);
            Assert.That(bst.Contains(5), Is.True);
            Assert.That(bst.Contains(7), Is.True);
            Assert.That(bst.Contains(9), Is.False);
        }

        /// <summary>
        /// 测试：中序遍历输出为升序
        /// </summary>
        [Test]
        public void InOrder_SortedSequence()
        {
            var bst = new BinarySearchTree<int>();
            var values = new[] { 3, 1, 4, 2, 5 };
            foreach (var v in values) bst.Insert(v);

            var inOrder = bst.InOrder();
            Assert.That(inOrder, Is.Ordered.Ascending);
            Assert.That(inOrder, Is.EquivalentTo(values));
        }

        /// <summary>
        /// 测试：删除叶子节点和单子节点
        /// </summary>
        [Test]
        public void Remove_Leaf_And_SingleChild_Node()
        {
            var bst = new BinarySearchTree<int>();
            // 构建  5
            //     /   \
            //    3     7
            //   / \     \
            //  2   4     9
            bst.Insert(5); bst.Insert(3); bst.Insert(7);
            bst.Insert(2); bst.Insert(4); bst.Insert(9);

            // 删叶子节点
            bst.Remove(4);
            Assert.That(bst.Contains(4), Is.False);

            // 删单子节点
            bst.Remove(9);
            Assert.That(bst.Contains(9), Is.False);
        }

        /// <summary>
        /// 测试：删除有双子节点的节点
        /// </summary>
        [Test]
        public void Remove_Node_With_TwoChildren()
        {
            var bst = new BinarySearchTree<int>();
            // 5, 3, 7, 2, 4, 6, 8
            var values = new[] { 5, 3, 7, 2, 4, 6, 8 };
            foreach (var v in values) bst.Insert(v);

            // 删5，根有双子节点
            bst.Remove(5);
            Assert.That(bst.Contains(5), Is.False);

            // 中序遍历仍升序
            var inOrder = bst.InOrder();
            Assert.That(inOrder, Is.Ordered.Ascending);
            Assert.That(inOrder.Count, Is.EqualTo(6));
        }

        /// <summary>
        /// 测试：查找最小与最大节点
        /// </summary>
        [Test]
        public void FindMinMax_Works()
        {
            var bst = new BinarySearchTree<int>();
            foreach (var v in new[] { 10, 6, 22, 3, 8, 25 }) bst.Insert(v);

            var min = bst.FindMin();
            var max = bst.FindMax();
            Assert.That(min.Value, Is.EqualTo(3));
            Assert.That(max.Value, Is.EqualTo(25));
        }

        /// <summary>
        /// 测试：层序遍历与前序、后序
        /// </summary>
        [Test]
        public void Traversal_Works()
        {
            var bst = new BinarySearchTree<int>();
            foreach (var v in new[] { 5, 3, 7, 2, 4, 6, 8 }) bst.Insert(v);

            var levelOrder = bst.LevelOrder();
            Assert.That(levelOrder[0], Is.EqualTo(5)); // 根节点
            Assert.That(levelOrder, Has.Count.EqualTo(7));

            var preOrder = bst.PreOrder();
            Assert.That(preOrder[0], Is.EqualTo(5));

            var postOrder = bst.PostOrder();
            Assert.That(postOrder[postOrder.Count - 1], Is.EqualTo(5));
        }

        /// <summary>
        /// 测试：空树查询返回空集
        /// </summary>
        [Test]
        public void EmptyTree_Traversals_ReturnsEmpty()
        {
            var bst = new BinarySearchTree<int>();
            Assert.That(bst.InOrder(), Is.Empty);
            Assert.That(bst.LevelOrder(), Is.Empty);
            Assert.That(bst.PreOrder(), Is.Empty);
            Assert.That(bst.PostOrder(), Is.Empty);
            Assert.That(bst.FindMin(), Is.Null);
            Assert.That(bst.FindMax(), Is.Null);
        }

        /// <summary>
        /// 测试：极端情况下高度为节点数（单边插入）
        /// </summary>
        [Test]
        public void Height_SkewedTree()
        {
            var bst = new BinarySearchTree<int>();
            for (int i = 1; i <= 5; i++) bst.Insert(i); // 单边链表
            Assert.That(bst.Height(), Is.EqualTo(5));
        }
        

        /// <summary>
        /// 测试：插入重复元素不会重复存储
        /// </summary>
        [Test]
        public void Insert_DuplicateElement_NotInserted()
        {
            var bst = new BinarySearchTree<int>();
            bst.Insert(1); bst.Insert(2); bst.Insert(2); bst.Insert(1);

            var inOrder = bst.InOrder();
            Assert.That(inOrder, Is.EquivalentTo(new[] { 1, 2 }));
        }
    }

}
