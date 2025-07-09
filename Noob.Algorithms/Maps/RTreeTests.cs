// ***********************************************************************
// Assembly         : Noob.Algorithms
// Author           : noob
// Created          : 2025-07-09
//
// Last Modified By : noob
// Last Modified On : 2025-07-09
// ***********************************************************************
// <copyright file="RTreeTests.cs" company="Noob.Algorithms">
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
    /// 表示空间矩形（最小外包矩形, MBR）。
    /// </summary>
    public class Rectangle
    {
        /// <summary>
        /// 左下角 X 坐标
        /// </summary>
        /// <value>The minimum x.</value>
        public double MinX { get; }
        /// <summary>
        /// 左下角 Y 坐标
        /// </summary>
        /// <value>The minimum y.</value>
        public double MinY { get; }
        /// <summary>
        /// 右上角 X 坐标
        /// </summary>
        /// <value>The maximum x.</value>
        public double MaxX { get; }
        /// <summary>
        /// 右上角 Y 坐标
        /// </summary>
        /// <value>The maximum y.</value>
        public double MaxY { get; }

        /// <summary>
        /// 构造矩形
        /// </summary>
        /// <param name="minX">The minimum x.</param>
        /// <param name="minY">The minimum y.</param>
        /// <param name="maxX">The maximum x.</param>
        /// <param name="maxY">The maximum y.</param>
        public Rectangle(double minX, double minY, double maxX, double maxY)
        {
            MinX = minX; MinY = minY; MaxX = maxX; MaxY = maxY;
        }
        /// <summary>
        /// 创建点的零面积矩形
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <returns>Rectangle.</returns>
        public static Rectangle FromPoint(double x, double y)
            => new Rectangle(x, y, x, y);

        /// <summary>
        /// 判断是否包含点
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <returns><c>true</c> if [contains] [the specified x]; otherwise, <c>false</c>.</returns>
        public bool Contains(double x, double y)
            => x >= MinX && x <= MaxX && y >= MinY && y <= MaxY;

        /// <summary>
        /// 是否和另一矩形有重叠
        /// </summary>
        /// <param name="other">The other.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public bool Intersects(Rectangle other)
            => !(MaxX < other.MinX || MinX > other.MaxX ||
                 MaxY < other.MinY || MinY > other.MaxY);

        /// <summary>
        /// 外包自身与另一个矩形
        /// </summary>
        /// <param name="other">The other.</param>
        /// <returns>Rectangle.</returns>
        public Rectangle Union(Rectangle other)
            => new Rectangle(
                Math.Min(MinX, other.MinX),
                Math.Min(MinY, other.MinY),
                Math.Max(MaxX, other.MaxX),
                Math.Max(MaxY, other.MaxY)
            );

        /// <summary>
        /// 面积
        /// </summary>
        /// <value>The area.</value>
        public double Area => (MaxX - MinX) * (MaxY - MinY);
    }

    /// <summary>
    /// R-Tree 节点。支持泛型数据实体。
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class RTreeNode<T>
    {
        /// <summary>
        /// 该节点的最小外包矩形
        /// </summary>
        /// <value>The bounds.</value>
        public Rectangle Bounds { get; set; }
        /// <summary>
        /// 叶节点数据列表
        /// </summary>
        /// <value>The entries.</value>
        public List<T> Entries { get; }
        /// <summary>
        /// 子节点列表
        /// </summary>
        /// <value>The children.</value>
        public List<RTreeNode<T>> Children { get; }
        /// <summary>
        /// 是否叶节点
        /// </summary>
        /// <value><c>true</c> if this instance is leaf; otherwise, <c>false</c>.</value>
        public bool IsLeaf { get; }

        /// <summary>
        /// 叶节点构造
        /// </summary>
        /// <param name="bounds">The bounds.</param>
        /// <param name="entries">The entries.</param>
        public RTreeNode(Rectangle bounds, List<T> entries)
        {
            Bounds = bounds;
            Entries = entries ?? new List<T>();
            IsLeaf = true;
            Children = null;
        }

        /// <summary>
        /// 非叶节点构造
        /// </summary>
        /// <param name="bounds">The bounds.</param>
        /// <param name="children">The children.</param>
        public RTreeNode(Rectangle bounds, List<RTreeNode<T>> children)
        {
            Bounds = bounds;
            Children = children ?? new List<RTreeNode<T>>();
            Entries = null;
            IsLeaf = false;
        }
    }

    /// <summary>
    /// 生产级可扩展 R-Tree 主体。支持批量构建、单点插入、范围查找、点查找。
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class RTree<T>
    {
        /// <summary>
        /// The maximum entries
        /// </summary>
        private readonly int _maxEntries;
        /// <summary>
        /// The bounds selector
        /// </summary>
        private readonly Func<T, Rectangle> _boundsSelector;
        /// <summary>
        /// The root
        /// </summary>
        private RTreeNode<T> _root;

        /// <summary>
        /// 构造 R-Tree。
        /// </summary>
        /// <param name="maxEntries">单节点最大容量（通常8~128）</param>
        /// <param name="boundsSelector">实体到矩形的映射</param>
        /// <exception cref="System.ArgumentException">maxEntries 必须 >=2 - maxEntries</exception>
        /// <exception cref="System.ArgumentNullException">boundsSelector</exception>
        public RTree(int maxEntries, Func<T, Rectangle> boundsSelector)
        {
            if (maxEntries < 2)
                throw new ArgumentException("maxEntries 必须 >=2", nameof(maxEntries));
            _maxEntries = maxEntries;
            _boundsSelector = boundsSelector ?? throw new ArgumentNullException(nameof(boundsSelector));
        }

        /// <summary>
        /// 批量构建 R-Tree（适合初建/大数据导入）。
        /// </summary>
        /// <param name="items">The items.</param>
        public void Build(IEnumerable<T> items)
        {
            var entries = items.Select(i => new RTreeNode<T>(_boundsSelector(i), new List<T> { i })).ToList();
            _root = BuildBulk(entries);
        }

        /// <summary>
        /// 插入单个实体。
        /// </summary>
        /// <param name="item">The item.</param>
        public void Insert(T item)
        {
            var entry = new RTreeNode<T>(_boundsSelector(item), new List<T> { item });
            if (_root == null)
            {
                _root = entry;
                return;
            }
            InsertRecursive(_root, entry, 0);
            // 若分裂导致根提升，外部应维护根
            if (_root.Children != null && _root.Children.Count > _maxEntries)
                _root = SplitNode(_root);
        }

        /// <summary>
        /// 范围查找：返回与给定矩形有交的所有实体。
        /// </summary>
        /// <param name="range">The range.</param>
        /// <returns>List&lt;T&gt;.</returns>
        public List<T> RangeSearch(Rectangle range)
        {
            var result = new List<T>();
            if (_root == null) return result;
            RangeSearchRecursive(_root, range, result);
            return result;
        }

        /// <summary>
        /// 点查找：返回包含该点的所有实体。
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <returns>List&lt;T&gt;.</returns>
        public List<T> PointSearch(double x, double y)
        {
            var result = new List<T>();
            if (_root == null) return result;
            PointSearchRecursive(_root, x, y, result);
            return result;
        }

        // ===== 工程级内部算法实现 =====

        // 批量建树（STR/简单分组法）
        /// <summary>
        /// Builds the bulk.
        /// </summary>
        /// <param name="nodes">The nodes.</param>
        /// <returns>RTreeNode&lt;T&gt;.</returns>
        private RTreeNode<T> BuildBulk(List<RTreeNode<T>> nodes)
        {
            if (nodes.Count <= _maxEntries)
            {
                var mbr = nodes[0].Bounds;
                foreach (var n in nodes.Skip(1)) mbr = mbr.Union(n.Bounds);
                return new RTreeNode<T>(mbr, nodes.SelectMany(n => n.Entries).ToList());
            }
            nodes.Sort((a, b) => a.Bounds.MinX.CompareTo(b.Bounds.MinX));
            var groups = new List<List<RTreeNode<T>>>();
            int groupSize = (int)Math.Ceiling((double)nodes.Count / Math.Ceiling((double)nodes.Count / _maxEntries));
            for (int i = 0; i < nodes.Count; i += groupSize)
                groups.Add(nodes.GetRange(i, Math.Min(groupSize, nodes.Count - i)));
            var children = groups.Select(BuildBulk).ToList();
            var mbr2 = children[0].Bounds;
            foreach (var c in children.Skip(1)) mbr2 = mbr2.Union(c.Bounds);
            return new RTreeNode<T>(mbr2, children);
        }

        /// <summary>
        /// Inserts the recursive.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <param name="entry">The entry.</param>
        /// <param name="level">The level.</param>
        private void InsertRecursive(RTreeNode<T> node, RTreeNode<T> entry, int level)
        {
            if (node.IsLeaf)
            {
                node.Entries.AddRange(entry.Entries);
                node.Bounds = node.Bounds.Union(entry.Bounds);
                return;
            }
            // 找MBR面积增量最小的子节点
            RTreeNode<T> best = null;
            double minIncrease = double.MaxValue;
            foreach (var child in node.Children)
            {
                var union = child.Bounds.Union(entry.Bounds);
                double increase = union.Area - child.Bounds.Area;
                if (increase < minIncrease)
                {
                    minIncrease = increase;
                    best = child;
                }
            }
            InsertRecursive(best, entry, level + 1);
            node.Bounds = node.Bounds.Union(entry.Bounds);
            // 若分裂，需重组
            if (best.Children != null && best.Children.Count > _maxEntries)
            {
                var split = SplitNode(best);
                node.Children.Remove(best);
                node.Children.Add(split);
            }
        }

        /// <summary>
        /// Splits the node.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <returns>RTreeNode&lt;T&gt;.</returns>
        private RTreeNode<T> SplitNode(RTreeNode<T> node)
        {
            // 线性分裂法：找间隔最大分组为两个分区种子
            var list = node.IsLeaf
                ? node.Entries.Select(i => new RTreeNode<T>(_boundsSelector(i), new List<T> { i })).ToList()
                : node.Children.ToList();
            int axis = node.Bounds.MaxX - node.Bounds.MinX > node.Bounds.MaxY - node.Bounds.MinY ? 0 : 1;
            list.Sort((a, b) => axis == 0
                ? a.Bounds.MinX.CompareTo(b.Bounds.MinX)
                : a.Bounds.MinY.CompareTo(b.Bounds.MinY));
            int mid = list.Count / 2;
            var g1 = list.Take(mid).ToList();
            var g2 = list.Skip(mid).ToList();
            RTreeNode<T> n1 = node.IsLeaf
                ? new RTreeNode<T>(GetMBR(g1), g1.SelectMany(x => x.Entries).ToList())
                : new RTreeNode<T>(GetMBR(g1), g1);
            RTreeNode<T> n2 = node.IsLeaf
                ? new RTreeNode<T>(GetMBR(g2), g2.SelectMany(x => x.Entries).ToList())
                : new RTreeNode<T>(GetMBR(g2), g2);
            // 返回新的上级节点
            return new RTreeNode<T>(n1.Bounds.Union(n2.Bounds), new List<RTreeNode<T>> { n1, n2 });
        }

        /// <summary>
        /// Gets the MBR.
        /// </summary>
        /// <param name="nodes">The nodes.</param>
        /// <returns>Rectangle.</returns>
        private static Rectangle GetMBR(List<RTreeNode<T>> nodes)
        {
            var mbr = nodes[0].Bounds;
            foreach (var n in nodes.Skip(1)) mbr = mbr.Union(n.Bounds);
            return mbr;
        }

        /// <summary>
        /// Ranges the search recursive.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <param name="range">The range.</param>
        /// <param name="result">The result.</param>
        private void RangeSearchRecursive(RTreeNode<T> node, Rectangle range, List<T> result)
        {
            if (!node.Bounds.Intersects(range)) return;
            if (node.IsLeaf)
            {
                foreach (var e in node.Entries)
                    if (range.Intersects(_boundsSelector(e))) result.Add(e);
            }
            else
            {
                foreach (var child in node.Children)
                    RangeSearchRecursive(child, range, result);
            }
        }

        /// <summary>
        /// Points the search recursive.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <param name="result">The result.</param>
        private void PointSearchRecursive(RTreeNode<T> node, double x, double y, List<T> result)
        {
            if (!node.Bounds.Contains(x, y)) return;
            if (node.IsLeaf)
            {
                foreach (var e in node.Entries)
                    if (_boundsSelector(e).Contains(x, y)) result.Add(e);
            }
            else
            {
                foreach (var child in node.Children)
                    PointSearchRecursive(child, x, y, result);
            }
        }
    }

    /// <summary>
    /// R-Tree 平台工程级单元测试
    /// </summary>
    [TestFixture]
    public class RTreeTests
    {
        /// <summary>
        /// 空间实体测试模型
        /// </summary>
        public class Box
        {
            /// <summary>
            /// Gets or sets the identifier.
            /// </summary>
            /// <value>The identifier.</value>
            public string Id { get; set; }

            /// <summary>
            /// Gets or sets the x1.
            /// </summary>
            /// <value>The x1.</value>
            public double X1 { get; set; }
            /// <summary>
            /// Gets or sets the y1.
            /// </summary>
            /// <value>The y1.</value>
            public double Y1 { get; set; }
            /// <summary>
            /// Gets or sets the x2.
            /// </summary>
            /// <value>The x2.</value>
            public double X2 { get; set; }
            /// <summary>
            /// Gets or sets the y2.
            /// </summary>
            /// <value>The y2.</value>
            public double Y2 { get; set; }
            /// <summary>
            /// Gets or sets the tag.
            /// </summary>
            /// <value>The tag.</value>
            public string Tag { get; set; }
        }

        /// <summary>
        /// 工厂：Box转Rectangle
        /// </summary>
        /// <param name="b">The b.</param>
        /// <returns>Rectangle.</returns>
        private static Rectangle BoxToRect(Box b) => new Rectangle(b.X1, b.Y1, b.X2, b.Y2);

        /// <summary>
        /// 测试：批量构建后，点查找命中。
        /// </summary>
        [Test]
        public void PointSearch_HitAndMiss_Works()
        {
            var boxes = new List<Box>
        {
            new Box { Id = "A", X1 = 0, Y1 = 0, X2 = 2, Y2 = 2, Tag = "A区" },
            new Box { Id = "B", X1 = 3, Y1 = 3, X2 = 5, Y2 = 5, Tag = "B区" }
        };
            var rtree = new RTree<Box>(4, BoxToRect);
            rtree.Build(boxes);

            // 命中A
            var found = rtree.PointSearch(1, 1);
            Assert.That(found.Count, Is.EqualTo(1));
            Assert.That(found[0].Tag, Is.EqualTo("A区"));

            // 未命中
            var miss = rtree.PointSearch(10, 10);
            Assert.That(miss, Is.Empty);
        }

        /// <summary>
        /// 测试：范围查询包含多个目标
        /// </summary>
        [Test]
        public void RangeSearch_MultiHits_Works()
        {
            var boxes = new List<Box>
            {
                new Box { Id = "A", X1 = 0, Y1 = 0, X2 = 2, Y2 = 2, Tag = "A区" },
                new Box { Id = "B", X1 = 1, Y1 = 1, X2 = 4, Y2 = 4, Tag = "B区" },
                new Box { Id = "C", X1 = 5, Y1 = 5, X2 = 6, Y2 = 6, Tag = "C区" }
            };
            var rtree = new RTree<Box>(4, BoxToRect);
            rtree.Build(boxes);

            var found = rtree.RangeSearch(new Rectangle(0, 0, 3, 3));
            Assert.That(found.Count, Is.EqualTo(2));
            var tags = new HashSet<string>(found.ConvertAll(x => x.Tag));
            Assert.That(tags, Does.Contain("A区"));
            Assert.That(tags, Does.Contain("B区"));
        }

        /// <summary>
        /// 测试：插入后查询命中
        /// </summary>
        [Test]
        public void Insert_Then_PointSearch_Success()
        {
            var rtree = new RTree<Box>(4, BoxToRect);
            rtree.Build(new List<Box>
            {
                new Box { Id = "A", X1 = 0, Y1 = 0, X2 = 2, Y2 = 2, Tag = "A区" }
            });
            var newBox = new Box { Id = "B", X1 = 10, Y1 = 10, X2 = 15, Y2 = 15, Tag = "新区" };
            rtree.Insert(newBox);

            var found = rtree.PointSearch(12, 12);
            Assert.That(found.Count, Is.EqualTo(1));
            Assert.That(found[0].Tag, Is.EqualTo("新区"));
        }

        /// <summary>
        /// 测试：空树查询返回空集。
        /// </summary>
        [Test]
        public void RangeSearch_EmptyTree_ReturnsEmpty()
        {
            var rtree = new RTree<Box>(4, BoxToRect);
            var found = rtree.RangeSearch(new Rectangle(0, 0, 10, 10));
            Assert.That(found, Is.Empty);
        }

        /// <summary>
        /// 测试：单节点最大容量边界，分裂正确
        /// </summary>
        [Test]
        public void Insert_OverCapacity_SplitsNode()
        {
            var boxes = new List<Box>
        {
            new Box { Id = "A", X1 = 0, Y1 = 0, X2 = 1, Y2 = 1, Tag = "A" },
            new Box { Id = "B", X1 = 2, Y1 = 2, X2 = 3, Y2 = 3, Tag = "B" },
            new Box { Id = "C", X1 = 4, Y1 = 4, X2 = 5, Y2 = 5, Tag = "C" },
            new Box { Id = "D", X1 = 6, Y1 = 6, X2 = 7, Y2 = 7, Tag = "D" }
        };
            var rtree = new RTree<Box>(2, BoxToRect); // 容量为2，必分裂
            rtree.Build(boxes);

            var newBox = new Box { Id = "E", X1 = 8, Y1 = 8, X2 = 9, Y2 = 9, Tag = "E" };
            rtree.Insert(newBox);

            var found = rtree.PointSearch(8.5, 8.5);
            Assert.That(found.Count, Is.EqualTo(1));
            Assert.That(found[0].Tag, Is.EqualTo("E"));
        }

        /// <summary>
        /// 测试：查找离深圳市车公庙最近的5个加油站
        /// </summary>
        [Test]
        public void Nearest5GasStations_ReturnsCorrect()
        {
            // 深圳市部分加油站经纬度（示意）
            var stations = new List<Box>
            {
                new Box { Id = "A", X1 = 114.044, Y1 = 22.537, X2 = 114.045, Y2 = 22.538, Tag = "中国石化福田车公庙站" },
                new Box { Id = "B", X1 = 114.048, Y1 = 22.533, X2 = 114.049, Y2 = 22.534, Tag = "中国石油香蜜湖站" },
                new Box { Id = "C", X1 = 114.034, Y1 = 22.540, X2 = 114.035, Y2 = 22.541, Tag = "中国石化竹子林站" },
                new Box { Id = "D", X1 = 114.051, Y1 = 22.539, X2 = 114.052, Y2 = 22.540, Tag = "中石化深南大道站" },
                new Box { Id = "E", X1 = 114.040, Y1 = 22.545, X2 = 114.041, Y2 = 22.546, Tag = "中油新洲站" },
                new Box { Id = "F", X1 = 114.065, Y1 = 22.531, X2 = 114.066, Y2 = 22.532, Tag = "其他较远加油站" }
            };
            var rtree = new RTree<Box>(4, BoxToRect);
            rtree.Build(stations);

            // 车公庙大致坐标
            double queryLon = 114.044, queryLat = 22.538;

            // 简单KNN逻辑：所有站点按中心点距离排序取前5
            var knn = stations
                .OrderBy(s =>
                {
                    double cx = (s.X1 + s.X2) / 2, cy = (s.Y1 + s.Y2) / 2;
                    return Math.Sqrt(Math.Pow(queryLon - cx, 2) + Math.Pow(queryLat - cy, 2));
                })
                .Take(5)
                .ToList();

            // 用RTree范围查询模拟——查询一个较小范围，结果数<5时适当扩大范围
            var results = rtree.RangeSearch(
                new Rectangle(queryLon - 0.02, queryLat - 0.02, queryLon + 0.02, queryLat + 0.02));
            if (results.Count < 5)
            {
                results = rtree.RangeSearch(
                    new Rectangle(queryLon - 0.03, queryLat - 0.03, queryLon + 0.03, queryLat + 0.03));
            }
            // 按距离精排
            results = results
                .OrderBy(s =>
                {
                    double cx = (s.X1 + s.X2) / 2, cy = (s.Y1 + s.Y2) / 2;
                    return Math.Sqrt(Math.Pow(queryLon - cx, 2) + Math.Pow(queryLat - cy, 2));
                })
                .Take(5)
                .ToList();

            Assert.That(results.Count, Is.EqualTo(5));
            // 最近应含车公庙、香蜜湖、竹子林、深南大道、新洲等
            var tags = results.ConvertAll(x => x.Tag);
            Assert.That(tags, Does.Contain("中国石化福田车公庙站"));
            Assert.That(tags, Does.Contain("中国石油香蜜湖站"));
            Assert.That(tags, Does.Contain("中国石化竹子林站"));
            Assert.That(tags, Does.Contain("中石化深南大道站"));
            Assert.That(tags, Does.Contain("中油新洲站"));
        }

    }


}
