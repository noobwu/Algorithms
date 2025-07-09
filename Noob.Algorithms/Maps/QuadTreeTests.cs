// ***********************************************************************
// Assembly         : Noob.Algorithms
// Author           : noob
// Created          : 2025-07-09
//
// Last Modified By : noob
// Last Modified On : 2025-07-09
// ***********************************************************************
// <copyright file="QuadTreeTests.cs" company="Noob.Algorithms">
//     Copyright (c) . All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;


namespace Noob.Algorithms.Maps
{

    /// <summary>
    /// 表示二维空间中的点。
    /// </summary>
    public struct Point2D
    {
        /// <summary>
        /// Gets the x.
        /// </summary>
        /// <value>The x.</value>
        public double X { get; }
        /// <summary>
        /// Gets the y.
        /// </summary>
        /// <value>The y.</value>
        public double Y { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Point2D"/> struct.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        public Point2D(double x, double y)
        {
            X = x;
            Y = y;
        }
    }

    /// <summary>
    /// 表示四叉树节点的空间区域（矩形）。
    /// </summary>
    public class QuadRectangle
    {
        /// <summary>
        /// Gets the minimum x.
        /// </summary>
        /// <value>The minimum x.</value>
        public double MinX { get; }
        /// <summary>
        /// Gets the minimum y.
        /// </summary>
        /// <value>The minimum y.</value>
        public double MinY { get; }
        /// <summary>
        /// Gets the maximum x.
        /// </summary>
        /// <value>The maximum x.</value>
        public double MaxX { get; }
        /// <summary>
        /// Gets the maximum y.
        /// </summary>
        /// <value>The maximum y.</value>
        public double MaxY { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="QuadRectangle"/> class.
        /// </summary>
        /// <param name="minX">The minimum x.</param>
        /// <param name="minY">The minimum y.</param>
        /// <param name="maxX">The maximum x.</param>
        /// <param name="maxY">The maximum y.</param>
        /// <exception cref="System.ArgumentException">最小值必须不大于最大值</exception>
        public QuadRectangle(double minX, double minY, double maxX, double maxY)
        {
            if (minX > maxX || minY > maxY)
                throw new ArgumentException("最小值必须不大于最大值");
            MinX = minX;
            MinY = minY;
            MaxX = maxX;
            MaxY = maxY;
        }

        /// <summary>
        /// 判断给定点是否落在矩形范围内。
        /// </summary>
        /// <param name="p">The p.</param>
        /// <returns><c>true</c> if [contains] [the specified p]; otherwise, <c>false</c>.</returns>
        public bool Contains(Point2D p)
            => p.X >= MinX && p.X <= MaxX && p.Y >= MinY && p.Y <= MaxY;

        /// <summary>
        /// 判断矩形是否和另一个有交集。
        /// </summary>
        /// <param name="other">The other.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public bool Intersects(QuadRectangle other)
            => !(MaxX < other.MinX || MinX > other.MaxX ||
                 MaxY < other.MinY || MinY > other.MaxY);
    }

    /// <summary>
    /// 四叉树节点。
    /// </summary>
    /// <typeparam name="T">存储的数据类型。</typeparam>
    public class QuadTreeNode<T>
    {
        /// <summary>
        /// 节点区域。
        /// </summary>
        /// <value>The bounds.</value>
        public QuadRectangle Bounds { get; }
        /// <summary>
        /// 本节点包含的数据。
        /// </summary>
        /// <value>The items.</value>
        public List<(Point2D pos, T data)> Items { get; }
        /// <summary>
        /// 子节点（四象限）。
        /// </summary>
        /// <value>The children.</value>
        public QuadTreeNode<T>[] Children { get; }

        /// <summary>
        /// Gets a value indicating whether this instance is leaf.
        /// </summary>
        /// <value><c>true</c> if this instance is leaf; otherwise, <c>false</c>.</value>
        public bool IsLeaf => Children.All(c => c == null);

        /// <summary>
        /// 构造一个空节点。
        /// </summary>
        /// <param name="bounds">The bounds.</param>
        public QuadTreeNode(QuadRectangle bounds)
        {
            Bounds = bounds;
            Items = new List<(Point2D, T)>();
            Children = new QuadTreeNode<T>[4]; // NE, NW, SW, SE
        }
    }

    /// <summary>
    /// 四叉树空间分桶索引（生产级泛型可扩展版）。
    /// </summary>
    /// <typeparam name="T">业务数据类型。</typeparam>
    public class QuadTree<T>
    {
        /// <summary>
        /// The bucket capacity
        /// </summary>
        private readonly int _bucketCapacity;
        /// <summary>
        /// The position selector
        /// </summary>
        private readonly Func<T, Point2D> _positionSelector;
        /// <summary>
        /// The root
        /// </summary>
        private QuadTreeNode<T> _root;

        /// <summary>
        /// 构造四叉树。
        /// </summary>
        /// <param name="bounds">空间覆盖区域。</param>
        /// <param name="bucketCapacity">叶节点最大容量，超过则分裂。</param>
        /// <param name="positionSelector">实体到二维坐标映射。</param>
        /// <exception cref="System.ArgumentException">容量必须大于0</exception>
        /// <exception cref="System.ArgumentNullException">positionSelector</exception>
        public QuadTree(QuadRectangle bounds, int bucketCapacity, Func<T, Point2D> positionSelector)
        {
            _bucketCapacity = bucketCapacity > 0 ? bucketCapacity : throw new ArgumentException("容量必须大于0");
            _positionSelector = positionSelector ?? throw new ArgumentNullException(nameof(positionSelector));
            _root = new QuadTreeNode<T>(bounds);
        }

        /// <summary>
        /// 批量构建树。
        /// </summary>
        /// <param name="items">The items.</param>
        public void Build(IEnumerable<T> items)
        {
            _root = new QuadTreeNode<T>(_root.Bounds);
            foreach (var item in items)
                Insert(item);
        }

        /// <summary>
        /// 插入单个实体。
        /// </summary>
        /// <param name="item">The item.</param>
        public void Insert(T item)
        {
            var pos = _positionSelector(item);
            InsertRecursive(_root, pos, item);
        }

        /// <summary>
        /// 查找覆盖指定点的桶及其数据。
        /// </summary>
        /// <param name="point">The point.</param>
        /// <returns>List&lt;T&gt;.</returns>
        public List<T> QueryPoint(Point2D point)
        {
            var node = FindNode(_root, point);
            return node?.Items.Select(i => i.data).ToList() ?? new List<T>();
        }

        /// <summary>
        /// 查找与指定矩形有交集的所有数据。
        /// </summary>
        /// <param name="range">The range.</param>
        /// <returns>List&lt;T&gt;.</returns>
        public List<T> QueryRange(QuadRectangle range)
        {
            var results = new List<T>();
            QueryRangeRecursive(_root, range, results);
            return results;
        }

        /// <summary>
        /// 查询指定点周边（含自身）的所有桶中所有数据。
        /// （典型用于近邻桶聚合检索）
        /// </summary>
        /// <param name="point">The point.</param>
        /// <returns>List&lt;T&gt;.</returns>
        public List<T> QueryAdjacentBuckets(Point2D point)
        {
            // 查找所有与本节点或周边格有交集的叶子桶
            var buckets = new List<QuadTreeNode<T>>();
            FindAdjacentLeafBuckets(_root, point, buckets);
            return buckets.SelectMany(b => b.Items.Select(i => i.data)).ToList();
        }

        // ---- 内部递归/分裂/查询逻辑 ----

        /// <summary>
        /// Inserts the recursive.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <param name="pos">The position.</param>
        /// <param name="data">The data.</param>
        /// <exception cref="System.ArgumentException">点超出当前树的空间范围</exception>
        private void InsertRecursive(QuadTreeNode<T> node, Point2D pos, T data)
        {
            if (!node.Bounds.Contains(pos))
                throw new ArgumentException("点超出当前树的空间范围");

            if (node.IsLeaf)
            {
                node.Items.Add((pos, data));
                if (node.Items.Count > _bucketCapacity)
                    Split(node);
            }
            else
            {
                int idx = GetChildIndex(node.Bounds, pos);
                InsertRecursive(node.Children[idx], pos, data);
            }
        }

        /// <summary>
        /// Finds the node.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <param name="p">The p.</param>
        /// <returns>QuadTreeNode&lt;T&gt;.</returns>
        private QuadTreeNode<T> FindNode(QuadTreeNode<T> node, Point2D p)
        {
            if (node == null || !node.Bounds.Contains(p)) return null;
            if (node.IsLeaf) return node;
            int idx = GetChildIndex(node.Bounds, p);
            return FindNode(node.Children[idx], p);
        }

        /// <summary>
        /// Queries the range recursive.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <param name="range">The range.</param>
        /// <param name="results">The results.</param>
        private void QueryRangeRecursive(QuadTreeNode<T> node, QuadRectangle range, List<T> results)
        {
            if (node == null || !node.Bounds.Intersects(range)) return;
            if (node.IsLeaf)
            {
                results.AddRange(node.Items.Where(i =>
                    range.Contains(i.pos)).Select(i => i.data));
            }
            else
            {
                foreach (var child in node.Children)
                    QueryRangeRecursive(child, range, results);
            }
        }

        /// <summary>
        /// Finds the adjacent leaf buckets.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <param name="p">The p.</param>
        /// <param name="result">The result.</param>
        private void FindAdjacentLeafBuckets(QuadTreeNode<T> node, Point2D p, List<QuadTreeNode<T>> result)
        {
            if (node == null) return;
            // 只要该桶覆盖点的邻域（中心±格宽/高），即收集
            var midX = (node.Bounds.MinX + node.Bounds.MaxX) / 2;
            var midY = (node.Bounds.MinY + node.Bounds.MaxY) / 2;
            var width = node.Bounds.MaxX - node.Bounds.MinX;
            var height = node.Bounds.MaxY - node.Bounds.MinY;
            if (Math.Abs(p.X - midX) <= width * 1.5 &&
                Math.Abs(p.Y - midY) <= height * 1.5 && node.IsLeaf)
            {
                result.Add(node);
            }
            else if (!node.IsLeaf)
            {
                foreach (var child in node.Children)
                    FindAdjacentLeafBuckets(child, p, result);
            }
        }

        /// <summary>
        /// Splits the specified node.
        /// </summary>
        /// <param name="node">The node.</param>
        private void Split(QuadTreeNode<T> node)
        {
            // 切分为4个子象限
            double minX = node.Bounds.MinX, minY = node.Bounds.MinY, maxX = node.Bounds.MaxX, maxY = node.Bounds.MaxY;
            double midX = (minX + maxX) / 2, midY = (minY + maxY) / 2;
            node.Children[0] = new QuadTreeNode<T>(new QuadRectangle(midX, midY, maxX, maxY)); // NE
            node.Children[1] = new QuadTreeNode<T>(new QuadRectangle(minX, midY, midX, maxY)); // NW
            node.Children[2] = new QuadTreeNode<T>(new QuadRectangle(minX, minY, midX, midY)); // SW
            node.Children[3] = new QuadTreeNode<T>(new QuadRectangle(midX, minY, maxX, midY)); // SE

            // 重新分配已有点
            foreach (var (pos, data) in node.Items)
            {
                int idx = GetChildIndex(node.Bounds, pos);
                node.Children[idx].Items.Add((pos, data));
            }
            node.Items.Clear();
        }

        /// <summary>
        /// Gets the index of the child.
        /// </summary>
        /// <param name="bounds">The bounds.</param>
        /// <param name="pos">The position.</param>
        /// <returns>System.Int32.</returns>
        private int GetChildIndex(QuadRectangle bounds, Point2D pos)
        {
            double midX = (bounds.MinX + bounds.MaxX) / 2;
            double midY = (bounds.MinY + bounds.MaxY) / 2;
            // 0: NE, 1: NW, 2: SW, 3: SE
            if (pos.X >= midX && pos.Y >= midY) return 0;
            if (pos.X < midX && pos.Y >= midY) return 1;
            if (pos.X < midX && pos.Y < midY) return 2;
            return 3;
        }
    }

    /// <summary>
    /// QuadTree 空间分桶索引平台级单元测试。
    /// </summary>
    [TestFixture]
    public class QuadTreeTests
    {
        /// <summary>
        /// 加油站业务模型。
        /// </summary>
        public class GasStation
        {
            /// <summary>
            /// Gets or sets the identifier.
            /// </summary>
            /// <value>The identifier.</value>
            public string Id { get; set; }

            /// <summary>
            /// Gets or sets the longitude.
            /// </summary>
            /// <value>The longitude.</value>
            public double Longitude { get; set; }

            /// <summary>
            /// Gets or sets the latitude.
            /// </summary>
            /// <value>The latitude.</value>
            public double Latitude { get; set; }

            /// <summary>
            /// Gets or sets the name.
            /// </summary>
            /// <value>The name.</value>
            public string Name { get; set; }
        }

        /// <summary>
        /// The area
        /// </summary>
        private QuadRectangle Area = new QuadRectangle(113.9, 22.5, 114.1, 22.6);

        /// <summary>
        /// 测试：插入单个点后点查命中。
        /// </summary>
        [Test]
        public void Insert_And_QueryPoint_Success()
        {
            var quadTree = new QuadTree<GasStation>(Area, 2, s => new Point2D(s.Longitude, s.Latitude));
            var station = new GasStation { Id = "A", Longitude = 114.00, Latitude = 22.55, Name = "福田站" };
            quadTree.Insert(station);

            var found = quadTree.QueryPoint(new Point2D(114.00, 22.55));

            Assert.That(found.Count, Is.EqualTo(1));
            Assert.That(found[0].Name, Is.EqualTo("福田站"));
        }

        /// <summary>
        /// 测试：批量构建与范围查找。
        /// </summary>
        [Test]
        public void Build_And_QueryRange_FindsExpected()
        {
            var stations = new List<GasStation>
            {
                new GasStation { Id = "A", Longitude = 114.00, Latitude = 22.55, Name = "福田站" },
                new GasStation { Id = "B", Longitude = 114.05, Latitude = 22.57, Name = "香蜜湖站" },
                new GasStation { Id = "C", Longitude = 113.95, Latitude = 22.53, Name = "南山站" }
            };
            var quadTree = new QuadTree<GasStation>(Area, 2, s => new Point2D(s.Longitude, s.Latitude));
            quadTree.Build(stations);

            var results = quadTree.QueryRange(new QuadRectangle(113.99, 22.54, 114.06, 22.58));
            var names = results.Select(s => s.Name).ToList();

            Assert.That(names, Does.Contain("福田站"));
            Assert.That(names, Does.Contain("香蜜湖站"));
            Assert.That(names, Does.Not.Contain("南山站"));
        }

        /// <summary>
        /// 测试：QueryAdjacentBuckets 能返回本格与邻域点。
        /// </summary>
        [Test]
        public void QueryAdjacentBuckets_ReturnsNearStations()
        {
            var stations = new List<GasStation>
            {
                new GasStation { Id = "A", Longitude = 114.00, Latitude = 22.55, Name = "A" },
                new GasStation { Id = "B", Longitude = 114.01, Latitude = 22.55, Name = "B" },
                new GasStation { Id = "C", Longitude = 114.02, Latitude = 22.56, Name = "C" },
                new GasStation { Id = "D", Longitude = 114.09, Latitude = 22.59, Name = "D" }
            };
            var quadTree = new QuadTree<GasStation>(Area, 1, s => new Point2D(s.Longitude, s.Latitude));
            quadTree.Build(stations);

            // 在 114.01, 22.55 处查询邻近格
            var result = quadTree.QueryAdjacentBuckets(new Point2D(114.01, 22.55));

            Assert.That(result.Count, Is.GreaterThanOrEqualTo(2));
            Assert.That(result.Select(s => s.Name), Does.Contain("A"));
            Assert.That(result.Select(s => s.Name), Does.Contain("B"));
        }

        /// <summary>
        /// 测试：空树查找结果为空。
        /// </summary>
        [Test]
        public void QueryOnEmptyTree_ReturnsEmpty()
        {
            var quadTree = new QuadTree<GasStation>(Area, 2, s => new Point2D(s.Longitude, s.Latitude));
            var found = quadTree.QueryPoint(new Point2D(114.0, 22.55));
            Assert.That(found, Is.Empty);

            var results = quadTree.QueryRange(new QuadRectangle(114, 22.5, 114.1, 22.6));
            Assert.That(results, Is.Empty);
        }

        /// <summary>
        /// 测试：插入超出区域的点抛出异常。
        /// </summary>
        [Test]
        public void Insert_OutsideArea_Throws()
        {
            var quadTree = new QuadTree<GasStation>(Area, 2, s => new Point2D(s.Longitude, s.Latitude));
            var station = new GasStation { Id = "X", Longitude = 115.0, Latitude = 23.0, Name = "外部站" };
            Assert.Throws<ArgumentException>(() => quadTree.Insert(station));
        }
    }


}
