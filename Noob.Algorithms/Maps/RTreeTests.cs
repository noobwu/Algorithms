using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Noob.Algorithms.Maps
{
    /// <summary>
    /// 表示二维矩形边界（最小外包矩形/MBR）。
    /// </summary>
    public class Rectangle
    {
        /// <summary>
        /// 左下角 X 坐标。
        /// </summary>
        public double MinX { get; }

        /// <summary>
        /// 左下角 Y 坐标。
        /// </summary>
        public double MinY { get; }

        /// <summary>
        /// 右上角 X 坐标。
        /// </summary>
        public double MaxX { get; }

        /// <summary>
        /// 右上角 Y 坐标。
        /// </summary>
        public double MaxY { get; }

        /// <summary>
        /// 创建一个矩形。
        /// </summary>
        public Rectangle(double minX, double minY, double maxX, double maxY)
        {
            MinX = minX;
            MinY = minY;
            MaxX = maxX;
            MaxY = maxY;
        }

        /// <summary>
        /// 是否包含给定点。
        /// </summary>
        public bool Contains(double x, double y)
            => x >= MinX && x <= MaxX && y >= MinY && y <= MaxY;

        /// <summary>
        /// 是否和另一矩形有重叠。
        /// </summary>
        public bool Intersects(Rectangle other)
            => !(MaxX < other.MinX || MinX > other.MaxX ||
                 MaxY < other.MinY || MinY > other.MaxY);

        /// <summary>
        /// 得到包络自身和另一个矩形的最小外包矩形。
        /// </summary>
        public Rectangle Union(Rectangle other)
            => new Rectangle(
                Math.Min(MinX, other.MinX),
                Math.Min(MinY, other.MinY),
                Math.Max(MaxX, other.MaxX),
                Math.Max(MaxY, other.MaxY)
            );

        /// <summary>
        /// 面积。
        /// </summary>
        public double Area => (MaxX - MinX) * (MaxY - MinY);

        /// <summary>
        /// 构造点的矩形（零面积）。
        /// </summary>
        public static Rectangle FromPoint(double x, double y)
            => new Rectangle(x, y, x, y);
    }

    /// <summary>
    /// R-Tree 节点类型。
    /// </summary>
    public class RTreeNode<T>
    {
        /// <summary>
        /// 当前节点的 MBR（最小外包矩形）。
        /// </summary>
        public Rectangle Bounds { get; set; }

        /// <summary>
        /// 子节点列表（非叶子时）。
        /// </summary>
        public List<RTreeNode<T>> Children { get; set; }

        /// <summary>
        /// 数据实体（叶子节点时）。
        /// </summary>
        public T Data { get; set; }

        /// <summary>
        /// 是否为叶节点。
        /// </summary>
        public bool IsLeaf => Children == null;

        /// <summary>
        /// 构造叶节点。
        /// </summary>
        public RTreeNode(Rectangle bounds, T data)
        {
            Bounds = bounds;
            Data = data;
            Children = null;
        }

        /// <summary>
        /// 构造非叶节点。
        /// </summary>
        public RTreeNode(Rectangle bounds, List<RTreeNode<T>> children)
        {
            Bounds = bounds;
            Children = children;
            Data = default;
        }
    }

    /// <summary>
    /// 通用 R-Tree 实现（生产级可扩展，支持泛型）
    /// </summary>
    public class RTree<T>
    {
        private readonly int _maxEntries;
        private readonly Func<T, Rectangle> _boundsSelector;
        private RTreeNode<T> _root;

        /// <summary>
        /// 构造 R-Tree。
        /// </summary>
        /// <param name="maxEntries">单节点最大元素数（超过后分裂）。</param>
        /// <param name="boundsSelector">实体到矩形边界的映射。</param>
        public RTree(int maxEntries, Func<T, Rectangle> boundsSelector)
        {
            if (maxEntries < 2) throw new ArgumentException($"{nameof(maxEntries)} 必须 >=2");

            _maxEntries = maxEntries;
            _boundsSelector = boundsSelector ?? throw new ArgumentNullException(nameof(boundsSelector));
        }

        /// <summary>
        /// 构建 R-Tree（批量插入，适合离线批量场景）。
        /// </summary>
        /// <param name="items">所有要索引的元素。</param>
        public void Build(IEnumerable<T> items)
        {
            var leaves = items
                .Select(item => new RTreeNode<T>(_boundsSelector(item), item))
                .ToList();

            if (leaves.Count == 0)
            {
                _root = null;  // 防止空数据非法树结构
                return;
            }

            _root = BuildNodes(leaves, 1);
        }

        /// <summary>
        /// 范围查询：返回所有与矩形 bounds 有交集的实体。
        /// </summary>
        public List<T> RangeSearch(Rectangle bounds)
        {
            var result = new List<T>();
            if (_root == null) return result; // 空树直接返回
            SearchRecursive(_root, bounds, result);
            return result;
        }

        /// <summary>
        /// 点查询：返回包含该点的所有实体。
        /// </summary>
        public List<T> PointSearch(double x, double y)
        {
            var result = new List<T>();
            if (_root == null) return result; // 空树直接返回
            PointRecursive(_root, x, y, result);
            return result;
        }

        /// <summary>
        /// 插入单个元素。
        /// </summary>
        public void Insert(T item)
        {
            var leaf = new RTreeNode<T>(_boundsSelector(item), item);
            if (_root == null)
            {
                _root = leaf;
                return;
            }
            InsertRecursive(_root, leaf, 1);
            // TODO: 若根节点分裂，提升新层
        }

        /// <summary>
        /// 内部递归与辅助方法
        /// </summary>
        /// <param name="nodes">The nodes.</param>
        /// <param name="level">The level.</param>
        /// <returns>RTreeNode&lt;T&gt;.</returns>
        private RTreeNode<T> BuildNodes(List<RTreeNode<T>> nodes, int level)
        {
            if (nodes.Count == 0)
                return null;

            if (nodes.Count <= _maxEntries)
            {
                // 叶节点
                Rectangle bounds = nodes[0].Bounds;
                foreach (var n in nodes.Skip(1)) bounds = bounds.Union(n.Bounds);
                return new RTreeNode<T>(bounds, nodes);
            }
            // 按X坐标排序后分组
            nodes.Sort((a, b) => a.Bounds.MinX.CompareTo(b.Bounds.MinX));
            var groups = new List<List<RTreeNode<T>>>();
            int groupSize = (int)Math.Ceiling((double)nodes.Count / _maxEntries);
            for (int i = 0; i < nodes.Count; i += groupSize)
                groups.Add(nodes.GetRange(i, Math.Min(groupSize, nodes.Count - i)));
            var children = groups.Select(g => BuildNodes(g, level + 1)).ToList();
            Rectangle mbr = children[0].Bounds;
            foreach (var c in children.Skip(1)) mbr = mbr.Union(c.Bounds);
            return new RTreeNode<T>(mbr, children);
        }

        /// <summary>
        /// Searches the recursive.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <param name="bounds">The bounds.</param>
        /// <param name="result">The result.</param>
        private void SearchRecursive(RTreeNode<T> node, Rectangle bounds, List<T> result)
        {
            if (node == null) return;
            if (!node.Bounds.Intersects(bounds)) return;
            if (node.IsLeaf)
            {
                if (bounds.Intersects(node.Bounds))
                    result.Add(node.Data);
                return;
            }
            foreach (var child in node.Children)
                SearchRecursive(child, bounds, result);
        }


        /// <summary>
        /// Points the recursive.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <param name="result">The result.</param>
        private void PointRecursive(RTreeNode<T> node, double x, double y, List<T> result)
        {
            if (node == null) return;
            if (!node.Bounds.Contains(x, y)) return;
            if (node.IsLeaf)
            {
                if (node.Bounds.Contains(x, y))
                    result.Add(node.Data);
                return;
            }
            foreach (var child in node.Children)
                PointRecursive(child, x, y, result);
        }

        /// <summary>
        /// Inserts the recursive.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <param name="leaf">The leaf.</param>
        /// <param name="level">The level.</param>
        private void InsertRecursive(RTreeNode<T> node, RTreeNode<T> leaf, int level)
        {
            // 仅支持简单插入，未分裂提升（工程可扩展此部分为Guttman分裂算法）
            if (node.IsLeaf)
            {
                if (node.Children == null) node.Children = new List<RTreeNode<T>>();
                node.Children.Add(leaf);
                node.Bounds = node.Bounds.Union(leaf.Bounds);
                return;
            }
            // 选取最小增量的子节点递归插入
            RTreeNode<T> best = null;
            double minEnlargement = double.MaxValue;
            foreach (var child in node.Children)
            {
                var enlarged = child.Bounds.Union(leaf.Bounds);
                double enlargement = enlarged.Area - child.Bounds.Area;
                if (enlargement < minEnlargement)
                {
                    minEnlargement = enlargement;
                    best = child;
                }
            }
            InsertRecursive(best, leaf, level + 1);
            node.Bounds = node.Bounds.Union(leaf.Bounds);
            // TODO: 支持分裂和根节点提升
        }
    }

    /// <summary>
    /// R-Tree 空间索引的核心单元测试集，适配生产平台。
    /// </summary>
    [TestFixture]
    public class RTreeTests
    {
        /// <summary>
        /// 加油站实体模型。
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
        /// 测试：单点查询应能命中该点对应的加油站。
        /// </summary>
        [Test]
        public void PointSearch_ReturnsCorrectStation()
        {
            var station = new GasStation { Id = "A", Longitude = 114.04, Latitude = 22.54, Name = "福田站" };
            var rtree = new RTree<GasStation>(4, s => Rectangle.FromPoint(s.Longitude, s.Latitude));
            rtree.Build(new[] { station });

            var found = rtree.PointSearch(114.04, 22.54);

            Assert.That(found, Is.Not.Null);
            Assert.That(found.Count, Is.EqualTo(1));
            Assert.That(found[0].Name, Is.EqualTo("福田站"));
        }

        /// <summary>
        /// 测试：范围查询命中多个加油站。
        /// </summary>
        [Test]
        public void RangeSearch_FindsStationsWithinBounds()
        {
            var stations = new List<GasStation>
        {
            new GasStation { Id = "A", Longitude = 114.04, Latitude = 22.54, Name = "福田站" },
            new GasStation { Id = "B", Longitude = 114.05, Latitude = 22.53, Name = "香蜜湖站" },
            new GasStation { Id = "C", Longitude = 114.06, Latitude = 22.60, Name = "龙华站" }
        };
            var rtree = new RTree<GasStation>(4, s => Rectangle.FromPoint(s.Longitude, s.Latitude));
            rtree.Build(stations);

            var results = rtree.RangeSearch(new Rectangle(114.03, 22.53, 114.055, 22.55));

            Assert.That(results.Count, Is.EqualTo(2));
            Assert.That(results, Has.Some.Matches<GasStation>(g => g.Name == "福田站"));
            Assert.That(results, Has.Some.Matches<GasStation>(g => g.Name == "香蜜湖站"));
        }

        /// <summary>
        /// 测试：空树查询返回空结果。
        /// </summary>
        [Test]
        public void RangeSearch_EmptyTree_ReturnsEmptyList()
        {
            var rtree = new RTree<GasStation>(4, s => Rectangle.FromPoint(s.Longitude, s.Latitude));
            rtree.Build(new GasStation[0]);

            var results = rtree.RangeSearch(new Rectangle(0, 0, 100, 100));

            Assert.That(results, Is.Not.Null);
            Assert.That(results, Is.Empty);
        }

        /// <summary>
        /// 测试：插入单个加油站后可被范围命中。
        /// </summary>
        [Test]
        public void Insert_AfterBuild_CanBeSearched()
        {
            var station1 = new GasStation { Id = "A", Longitude = 114.04, Latitude = 22.54, Name = "福田站" };
            var station2 = new GasStation { Id = "B", Longitude = 114.09, Latitude = 22.55, Name = "车公庙站" };
            var rtree = new RTree<GasStation>(4, s => Rectangle.FromPoint(s.Longitude, s.Latitude));
            rtree.Build(new[] { station1 });

            rtree.Insert(station2);

            var results = rtree.RangeSearch(new Rectangle(114.08, 22.54, 114.10, 22.56));

            Assert.That(results, Has.Exactly(1).Matches<GasStation>(g => g.Name == "车公庙站"));
        }

        /// <summary>
        /// 测试：边界点命中判定正确。
        /// </summary>
        [Test]
        public void RangeSearch_PointOnBoundary_IsIncluded()
        {
            var station = new GasStation { Id = "B", Longitude = 114.05, Latitude = 22.53, Name = "香蜜湖站" };
            var rtree = new RTree<GasStation>(4, s => Rectangle.FromPoint(s.Longitude, s.Latitude));
            rtree.Build(new[] { station });

            var results = rtree.RangeSearch(new Rectangle(114.05, 22.53, 114.06, 22.60));

            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results[0].Name, Is.EqualTo("香蜜湖站"));
        }

        /// <summary>
        /// 测试：多点构建，查询结果包含所有命中的站点，无误命中。
        /// </summary>
        [Test]
        public void BuildAndQuery_MultiplePoints_ResultCorrect()
        {
            var stations = new List<GasStation>
        {
            new GasStation { Id = "A", Longitude = 114.04, Latitude = 22.54, Name = "福田站" },
            new GasStation { Id = "B", Longitude = 114.06, Latitude = 22.58, Name = "南山站" },
            new GasStation { Id = "C", Longitude = 114.09, Latitude = 22.60, Name = "盐田站" }
        };
            var rtree = new RTree<GasStation>(2, s => Rectangle.FromPoint(s.Longitude, s.Latitude));
            rtree.Build(stations);

            var results = rtree.RangeSearch(new Rectangle(114.05, 22.56, 114.10, 22.62));

            Assert.That(results.Count, Is.EqualTo(2));
            Assert.That(results.Select(s => s.Name), Contains.Item("南山站"));
            Assert.That(results.Select(s => s.Name), Contains.Item("盐田站"));
        }
    }

}
