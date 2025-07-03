using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Noob.DataStructures
{
    /// <summary>
    /// 跳表节点（SkipListNode），包含值及多级前向指针
    /// </summary>
    /// <typeparam name="T">节点值类型，需实现 IComparable</typeparam>
    public class SkipListNode<T> where T : IComparable<T>
    {
        /// <summary>
        /// 节点值
        /// </summary>
        public T Value { get; set; }

        /// <summary>
        /// 各级前向指针（Forwards[i] 表示第 i 层的下一个节点）
        /// </summary>
        public SkipListNode<T>[] Forwards { get; set; }

        /// <summary>
        /// 创建新节点
        /// </summary>
        /// <param name="value">节点值</param>
        /// <param name="level">节点层数</param>
        public SkipListNode(T value, int level)
        {
            Value = value;
            Forwards = new SkipListNode<T>[level];
        }
    }

    /// <summary>
    /// 平台工程级跳表（SkipList）实现，支持高效插入、查找、删除、遍历
    /// </summary>
    /// <typeparam name="T">元素类型，要求实现 IComparable</typeparam>
    public class SkipList<T> : IEnumerable<T> where T : IComparable<T>
    {
        /// <summary>
        /// 跳表最大层数（默认16层，足以支持百万量级元素）
        /// </summary>
        public const int MaxLevel = 16;

        /// <summary>
        /// 跳表头节点（哑节点/哨兵）
        /// </summary>
        private readonly SkipListNode<T> _head;

        /// <summary>
        /// 当前跳表实际层数
        /// </summary>
        public int Level { get; private set; }

        /// <summary>
        /// 当前跳表元素总数
        /// </summary>
        public int Count { get; private set; }

        /// <summary>
        /// 随机数生成器（用于层数抽签）
        /// </summary>
        private readonly Random _random;

        /// <summary>
        /// 跳表构造函数
        /// </summary>
        public SkipList()
        {
            _head = new SkipListNode<T>(default, MaxLevel);
            Level = 1;
            Count = 0;
            _random = new Random();
        }

        /// <summary>
        /// 向跳表插入新元素（自动去重）
        /// </summary>
        /// <param name="value">要插入的值</param>
        /// <returns>插入成功 true，已存在 false</returns>
        public bool Insert(T value)
        {
            var update = new SkipListNode<T>[MaxLevel];
            var current = _head;

            // 自顶向下，定位每一层插入点
            for (int i = Level - 1; i >= 0; i--)
            {
                while (current.Forwards[i] != null && current.Forwards[i].Value.CompareTo(value) < 0)
                    current = current.Forwards[i];
                update[i] = current;
            }

            current = current.Forwards[0];
            // 已存在则不插入
            if (current != null && current.Value.CompareTo(value) == 0)
                return false;

            int nodeLevel = GenerateRandomLevel();
            if (nodeLevel > Level)
            {
                for (int i = Level; i < nodeLevel; i++)
                    update[i] = _head;
                Level = nodeLevel;
            }

            var newNode = new SkipListNode<T>(value, nodeLevel);
            for (int i = 0; i < nodeLevel; i++)
            {
                newNode.Forwards[i] = update[i].Forwards[i];
                update[i].Forwards[i] = newNode;
            }
            Count++;
            return true;
        }

        /// <summary>
        /// 判断跳表中是否存在指定元素
        /// </summary>
        /// <param name="value">要查找的值</param>
        /// <returns>存在 true，否则 false</returns>
        public bool Contains(T value)
        {
            var current = _head;
            for (int i = Level - 1; i >= 0; i--)
            {
                while (current.Forwards[i] != null && current.Forwards[i].Value.CompareTo(value) < 0)
                    current = current.Forwards[i];
            }
            current = current.Forwards[0];
            return current != null && current.Value.CompareTo(value) == 0;
        }

        /// <summary>
        /// 从跳表中删除指定元素
        /// </summary>
        /// <param name="value">要删除的值</param>
        /// <returns>删除成功 true，否则 false</returns>
        public bool Remove(T value)
        {
            var update = new SkipListNode<T>[MaxLevel];
            var current = _head;

            for (int i = Level - 1; i >= 0; i--)
            {
                while (current.Forwards[i] != null && current.Forwards[i].Value.CompareTo(value) < 0)
                    current = current.Forwards[i];
                update[i] = current;
            }

            current = current.Forwards[0];
            if (current == null || current.Value.CompareTo(value) != 0)
                return false;

            for (int i = 0; i < Level; i++)
            {
                if (update[i].Forwards[i] == current)
                    update[i].Forwards[i] = current.Forwards[i];
            }

            // 调整跳表实际层数
            while (Level > 1 && _head.Forwards[Level - 1] == null)
                Level--;
            Count--;
            return true;
        }

        /// <summary>
        /// 按升序遍历跳表中所有元素
        /// </summary>
        /// <returns>元素枚举器</returns>
        public IEnumerator<T> GetEnumerator()
        {
            var node = _head.Forwards[0];
            while (node != null)
            {
                yield return node.Value;
                node = node.Forwards[0];
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// 生成节点的随机层数（概率型抽签，p=0.5）
        /// </summary>
        /// <returns>层数，至少为1，最大不超过MaxLevel</returns>
        private int GenerateRandomLevel()
        {
            int level = 1;
            while (_random.NextDouble() < 0.5 && level < MaxLevel)
                level++;
            return level;
        }
    }


    /// <summary>
    /// SkipList 平台工程级单元测试，覆盖插入、查找、删除、有序性等主功能
    /// </summary>
    [TestFixture]
    public class SkipListTests
    {
        /// <summary>
        /// 测试跳表基本插入和查找功能
        /// </summary>
        [Test]
        public void Insert_And_Contains_Basic_Success()
        {
            var skipList = new SkipList<int>();
            Assert.That(skipList.Insert(10), Is.True);
            Assert.That(skipList.Insert(20), Is.True);
            Assert.That(skipList.Insert(15), Is.True);

            Assert.That(skipList.Contains(10), Is.True);
            Assert.That(skipList.Contains(15), Is.True);
            Assert.That(skipList.Contains(20), Is.True);
            Assert.That(skipList.Contains(99), Is.False);
        }

        /// <summary>
        /// 测试插入重复元素（应去重）
        /// </summary>
        [Test]
        public void Insert_Duplicate_Should_Fail()
        {
            var skipList = new SkipList<int>();
            Assert.That(skipList.Insert(30), Is.True);
            Assert.That(skipList.Insert(30), Is.False); // 再插入返回false
            Assert.That(skipList.Count, Is.EqualTo(1));
        }

        /// <summary>
        /// 测试跳表元素升序遍历
        /// </summary>
        [Test]
        public void Enumerate_Should_Return_Ordered()
        {
            var skipList = new SkipList<int>();
            var data = new[] { 10, 5, 15, 7, 20 };
            foreach (var x in data) skipList.Insert(x);

            var sorted = new List<int>(skipList);
            Assert.That(sorted, Is.Ordered.Ascending);
            Assert.That(sorted, Is.EqualTo(new[] { 5, 7, 10, 15, 20 }));
        }

        /// <summary>
        /// 测试删除功能和删除后的查找
        /// </summary>
        [Test]
        public void Remove_Should_Delete_And_Contains_Should_Fail()
        {
            var skipList = new SkipList<int>();
            skipList.Insert(100);
            skipList.Insert(200);

            Assert.That(skipList.Remove(100), Is.True);
            Assert.That(skipList.Contains(100), Is.False);
            Assert.That(skipList.Count, Is.EqualTo(1));
            Assert.That(skipList.Remove(100), Is.False); // 再删返回false
        }

        /// <summary>
        /// 测试边界场景：空表查找、删除
        /// </summary>
        [Test]
        public void Empty_SkipList_Should_Behave_Correctly()
        {
            var skipList = new SkipList<string>();
            Assert.That(skipList.Contains("hello"), Is.False);
            Assert.That(skipList.Remove("world"), Is.False);
            Assert.That(skipList.Count, Is.EqualTo(0));
        }

        /// <summary>
        /// 测试大量数据插入与查询
        /// </summary>
        [Test]
        public void Insert_Many_Items_Should_Work()
        {
            var skipList = new SkipList<int>();
            int n = 1000;
            for (int i = 1; i <= n; i++)
                Assert.That(skipList.Insert(i), Is.True);

            Assert.That(skipList.Count, Is.EqualTo(n));
            for (int i = 1; i <= n; i++)
                Assert.That(skipList.Contains(i), Is.True);
        }

        /// <summary>
        /// 测试正序和倒序插入都能保证有序遍历
        /// </summary>
        [Test]
        public void Forward_And_Backward_Insert_Should_Ordered()
        {
            var skipList = new SkipList<int>();
            for (int i = 10; i >= 1; i--)
                skipList.Insert(i);

            var sorted = new List<int>(skipList);
            Assert.That(sorted, Is.Ordered.Ascending);
        }
    }

}
