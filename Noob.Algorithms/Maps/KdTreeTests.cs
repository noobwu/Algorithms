using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;


namespace Noob.Algorithms.Maps
{

    /// <summary>
    /// 封装距离和节点，用于最近邻排序堆。
    /// </summary>
    public class DistanceNode<T> : IComparable<DistanceNode<T>>
    {
        /// <summary>
        /// Gets the distance.
        /// </summary>
        /// <value>The distance.</value>
        public double Distance { get; }

        /// <summary>
        /// Gets the node.
        /// </summary>
        /// <value>The node.</value>
        public KdTreeNode<T> Node { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DistanceNode{T}"/> class.
        /// </summary>
        /// <param name="distance">The distance.</param>
        /// <param name="node">The node.</param>
        public DistanceNode(double distance, KdTreeNode<T> node)
        {
            Distance = distance;
            Node = node;
        }

        /// <summary>
        /// Compares the current instance with another object of the same type and returns an integer that indicates whether the current instance precedes, follows, or occurs in the same position in the sort order as the other object.
        /// </summary>
        /// <param name="other">An object to compare with this instance.</param>
        /// <returns>A value that indicates the relative order of the objects being compared. The return value has these meanings:
        /// <list type="table"><listheader><term> Value</term><description> Meaning</description></listheader><item><term> Less than zero</term><description> This instance precedes <paramref name="other" /> in the sort order.</description></item><item><term> Zero</term><description> This instance occurs in the same position in the sort order as <paramref name="other" />.</description></item><item><term> Greater than zero</term><description> This instance follows <paramref name="other" /> in the sort order.</description></item></list></returns>
        public int CompareTo(DistanceNode<T> other)
        {
            int cmp = Distance.CompareTo(other.Distance);
            if (cmp == 0) cmp = Node.GetHashCode().CompareTo(other.Node.GetHashCode());
            return cmp;
        }
    }

    /// <summary>
    /// 表示一个通用的 K 维向量。
    /// </summary>
    public class KdVector
    {
        /// <summary>
        /// 维度数据。
        /// </summary>
        public double[] Coordinates { get; }

        /// <summary>
        /// 构造函数，传入维度数据。
        /// </summary>
        /// <param name="coordinates">向量各维的数值。</param>
        public KdVector(params double[] coordinates)
        {
            Coordinates = coordinates ?? throw new ArgumentNullException(nameof(coordinates));
        }

        /// <summary>
        /// 获取向量指定维度的值。
        /// </summary>
        public double this[int index] => Coordinates[index];

        /// <summary>
        /// 计算欧氏距离。
        /// </summary>
        public double DistanceTo(KdVector other)
        {
            if (other.Coordinates.Length != Coordinates.Length)
                throw new ArgumentException("维度不匹配");
            double sum = 0;
            for (int i = 0; i < Coordinates.Length; i++)
            {
                var diff = Coordinates[i] - other.Coordinates[i];
                sum += diff * diff;
            }
            return Math.Sqrt(sum);
        }
    }

    /// <summary>
    /// KD-Tree 节点。
    /// </summary>
    /// <typeparam name="T">关联数据类型。</typeparam>
    public class KdTreeNode<T>
    {
        /// <summary>
        /// 当前节点的向量。
        /// </summary>
        public KdVector Vector { get; }

        /// <summary>
        /// 存储的实体对象。
        /// </summary>
        public T Data { get; }

        /// <summary>
        /// 左子树。
        /// </summary>
        public KdTreeNode<T> Left { get; set; }

        /// <summary>
        /// 右子树。
        /// </summary>
        public KdTreeNode<T> Right { get; set; }

        /// <summary>
        /// 分割轴。
        /// </summary>
        public int Axis { get; set; }

        /// <summary>
        /// 构造 KD-Tree 节点。
        /// </summary>
        public KdTreeNode(KdVector vector, T data, int axis)
        {
            Vector = vector ?? throw new ArgumentNullException(nameof(vector));
            Data = data;
            Axis = axis;
        }
    }

    /// <summary>
    /// 通用 KD-Tree 实现。
    /// </summary>
    /// <typeparam name="T">关联的数据类型。</typeparam>
    public class KdTree<T>
    {
        private readonly int _dimensions;
        private readonly Func<T, KdVector> _vectorSelector;
        private KdTreeNode<T> _root;

        /// <summary>
        /// 构造 KD-Tree。
        /// </summary>
        /// <param name="dimensions">空间维度。</param>
        /// <param name="vectorSelector">实体到向量的映射函数。</param>
        public KdTree(int dimensions, Func<T, KdVector> vectorSelector)
        {
            if (dimensions < 1) throw new ArgumentException("维度必须为正整数", nameof(dimensions));
            _dimensions = dimensions;
            _vectorSelector = vectorSelector ?? throw new ArgumentNullException(nameof(vectorSelector));
        }

        /// <summary>
        /// 批量构建树。
        /// </summary>
        /// <param name="items">初始元素集合。</param>
        public void Build(IEnumerable<T> items)
        {
            var nodes = items.Select(item => new KdTreeNode<T>(_vectorSelector(item), item, 0)).ToList();
            _root = BuildTree(nodes, 0);
        }

        /// <summary>
        /// 插入单个元素（递归插入）。
        /// </summary>
        public void Insert(T item)
        {
            var node = new KdTreeNode<T>(_vectorSelector(item), item, 0);
            _root = Insert(_root, node, 0);
        }

        /// <summary>
        /// 查询最近邻。
        /// </summary>
        /// <param name="target">目标向量。</param>
        /// <param name="k">返回最近的k个结果。</param>
        public List<T> SearchKNearest(KdVector target, int k = 1)
        {
            var heap = new SortedSet<DistanceNode<T>>();
            void Search(KdTreeNode<T> current, int depth)
            {
                if (current == null) return;
                double d = current.Vector.DistanceTo(target);
                var dn = new DistanceNode<T>(d, current);
                if (heap.Count < k)
                {
                    heap.Add(dn);
                }
                else if (d < heap.Max.Distance)
                {
                    heap.Remove(heap.Max);
                    heap.Add(dn);
                }
                int axis = depth % _dimensions;
                bool goLeft = target[axis] < current.Vector[axis];
                var first = goLeft ? current.Left : current.Right;
                var second = goLeft ? current.Right : current.Left;
                Search(first, depth + 1);
                if (heap.Count < k || Math.Abs(target[axis] - current.Vector[axis]) < heap.Max.Distance)
                    Search(second, depth + 1);
            }
            Search(_root, 0);
            return heap.OrderBy(x => x.Distance).Select(x => x.Node.Data).ToList();
        }

        /// <summary>
        /// 内部递归构建树。
        /// </summary>
        private KdTreeNode<T> BuildTree(List<KdTreeNode<T>> nodes, int depth)
        {
            if (!nodes.Any()) return null;
            int axis = depth % _dimensions;
            nodes.Sort((a, b) => a.Vector[axis].CompareTo(b.Vector[axis]));
            int median = nodes.Count / 2;
            var root = nodes[median];
            root.Axis = axis;
            root.Left = BuildTree(nodes.Take(median).ToList(), depth + 1);
            root.Right = BuildTree(nodes.Skip(median + 1).ToList(), depth + 1);
            return root;
        }

        /// <summary>
        /// 内部递归插入。
        /// </summary>
        private KdTreeNode<T> Insert(KdTreeNode<T> current, KdTreeNode<T> node, int depth)
        {
            if (current == null) { node.Axis = depth % _dimensions; return node; }
            int axis = depth % _dimensions;
            if (node.Vector[axis] < current.Vector[axis])
                current.Left = Insert(current.Left, node, depth + 1);
            else
                current.Right = Insert(current.Right, node, depth + 1);
            return current;
        }
    }


    /// <summary>
    /// KD-Tree 单元测试集（平台工程级标准）。
    /// </summary>
    [TestFixture]
    public class KdTreeTests
    {
        /// <summary>
        /// 加油站业务模型，作为测试实体。
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
        /// 测试：单点最近邻查询应返回自身。
        /// </summary>
        [Test]
        public void NearestNeighbor_SinglePoint_ReturnsSelf()
        {
            var kdTree = new KdTree<GasStation>(
                2,
                gs => new KdVector(gs.Longitude, gs.Latitude)
            );
            var station = new GasStation { Id = "A", Longitude = 100, Latitude = 40, Name = "Solo" };
            kdTree.Build(new[] { station });

            var nearest = kdTree.SearchKNearest(new KdVector(100, 40), 1);

            Assert.That(nearest, Is.Not.Null);
            Assert.That(nearest.Count, Is.EqualTo(1));
            Assert.That(nearest[0], Is.SameAs(station));
        }

        /// <summary>
        /// 测试：2个点时K=1最近邻结果准确。
        /// </summary>
        [Test]
        public void NearestNeighbor_TwoPoints_K1_Expected()
        {
            var a = new GasStation { Id = "A", Longitude = 0, Latitude = 0, Name = "A" };
            var b = new GasStation { Id = "B", Longitude = 10, Latitude = 0, Name = "B" };
            var kdTree = new KdTree<GasStation>(
                2, gs => new KdVector(gs.Longitude, gs.Latitude));
            kdTree.Build(new[] { a, b });

            var nearest = kdTree.SearchKNearest(new KdVector(2, 0), 1);

            Assert.That(nearest.Count, Is.EqualTo(1));
            Assert.That(nearest[0], Is.SameAs(a));
        }

        /// <summary>
        /// 测试：多点场景K最近邻结果按距离升序返回。
        /// </summary>
        [Test]
        public void NearestNeighbor_MultiPoints_K3_Sorted()
        {
            var kdTree = new KdTree<GasStation>(
                2, gs => new KdVector(gs.Longitude, gs.Latitude));
            var stations = new[]
            {
                new GasStation { Id = "A", Longitude = 0, Latitude = 0 },
                new GasStation { Id = "B", Longitude = 1, Latitude = 1 },
                new GasStation { Id = "C", Longitude = 2, Latitude = 2 },
                new GasStation { Id = "D", Longitude = 10, Latitude = 10 }
            };
            kdTree.Build(stations);

            var nearest = kdTree.SearchKNearest(new KdVector(0, 0), 3);

            Assert.That(nearest.Count, Is.EqualTo(3));
            Assert.That(nearest[0].Id, Is.EqualTo("A"));
            Assert.That(nearest[1].Id, Is.EqualTo("B"));
            Assert.That(nearest[2].Id, Is.EqualTo("C"));
        }

        /// <summary>
        /// 测试：K超过数据量时不报错，返回所有元素。
        /// </summary>
        [Test]
        public void NearestNeighbor_KGreaterThanCount_ReturnsAll()
        {
            var kdTree = new KdTree<GasStation>(
                2, gs => new KdVector(gs.Longitude, gs.Latitude)
             );
            var stations = new[]
            {
                new GasStation { Id = "A", Longitude = 0, Latitude = 0 },
                new GasStation { Id = "B", Longitude = 10, Latitude = 10 }
            };
            kdTree.Build(stations);

            var nearest = kdTree.SearchKNearest(new KdVector(5, 5), 5);

            Assert.That(nearest.Count, Is.EqualTo(2));
        }

        /// <summary>
        /// 测试：查询高维空间最近邻结果（5维）。
        /// </summary>
        [Test]
        public void NearestNeighbor_HighDimension_Works()
        {
            var kdTree = new KdTree<double[]>(
                 5, vec => new KdVector(vec)
             );
            var data = new List<double[]>
            {
                new double[] { 1, 2, 3, 4, 5 },
                new double[] { 2, 3, 4, 5, 6 },
                new double[] { 10, 9, 8, 7, 6 }
            };
            kdTree.Build(data);

            var nearest = kdTree.SearchKNearest(new KdVector(1.5, 2.5, 3.5, 4.5, 5.5), 2);

            Assert.That(nearest.Count, Is.EqualTo(2));
            Assert.That(nearest.Any(arr => arr.SequenceEqual(data[0])));
            Assert.That(nearest.Any(arr => arr.SequenceEqual(data[1])));
        }

        /// <summary>
        /// 测试：插入节点后，最近邻查询能找到新点。
        /// </summary>
        [Test]
        public void Insert_AfterBuild_NewPointFound()
        {
            var kdTree = new KdTree<GasStation>(
                2, gs => new KdVector(gs.Longitude, gs.Latitude)
            );
            var a = new GasStation { Id = "A", Longitude = 0, Latitude = 0 };
            kdTree.Build(new[] { a });

            var b = new GasStation { Id = "B", Longitude = 10, Latitude = 10 };
            kdTree.Insert(b);

            var nearest = kdTree.SearchKNearest(new KdVector(9, 9), 1);

            Assert.That(nearest.Count, Is.EqualTo(1));
            Assert.That(nearest[0], Is.SameAs(b));
        }

        /// <summary>
        /// 测试：空树返回空列表不抛异常。
        /// </summary>
        [Test]
        public void Search_EmptyTree_ReturnsEmpty()
        {
            var kdTree = new KdTree<GasStation>(
                2, gs => new KdVector(gs.Longitude, gs.Latitude));
            var nearest = kdTree.SearchKNearest(new KdVector(0, 0), 3);

            Assert.That(nearest, Is.Not.Null);
            Assert.That(nearest, Is.Empty);
        }


        /// <summary>
        /// 测试：在“深圳市福田区车公庙”附近，查找最近5个加油站名称是否准确
        /// </summary>
        [Test]
        public void MapScenario_FindFiveNearestGasStations()
        {
            // 构造模拟深圳市福田区部分加油站（数据参考真实地图，适度简化）
            var stations = new List<GasStation>
            {
                new GasStation { Id = "1", Longitude = 114.042158, Latitude = 22.540809, Name = "中国石化福田加油站" },
                new GasStation { Id = "2", Longitude = 114.045022, Latitude = 22.534891, Name = "中国石化竹子林加油站" },
                new GasStation { Id = "3", Longitude = 114.037239, Latitude = 22.539685, Name = "中国石化深南加油站" },
                new GasStation { Id = "4", Longitude = 114.053763, Latitude = 22.542327, Name = "中国石油香蜜湖加油站" },
                new GasStation { Id = "5", Longitude = 114.029869, Latitude = 22.546317, Name = "中国石化华富加油站" },
                new GasStation { Id = "6", Longitude = 114.023361, Latitude = 22.535095, Name = "中国石化新洲加油站" },
                new GasStation { Id = "7", Longitude = 114.056189, Latitude = 22.525832, Name = "中国石化益田加油站" }
            };

            // 车公庙地铁站附近经纬度（约）
            var userPos = new KdVector(114.0416, 22.5375);

            var kdTree = new KdTree<GasStation>(
                2,
                gs => new KdVector(gs.Longitude, gs.Latitude)
            );
            kdTree.Build(stations);

            // 查询最近5家加油站
            var nearest = kdTree.SearchKNearest(userPos, 5);

            Assert.That(nearest.Count, Is.EqualTo(5));

            // 断言最近5家站点名称（结果应为距离最近的5家，排序依实现细节/距离，允许与预期集等价）
            var expectedNames = new[]
            {
                "中国石化福田加油站",
                "中国石化深南加油站",
                "中国石化竹子林加油站",
                "中国石油香蜜湖加油站",
                "中国石化华富加油站"
            };

            CollectionAssert.AreEquivalent(expectedNames, nearest.Select(s => s.Name).ToArray());

            // 距离递增断言
            var dists = nearest.Select(s => Math.Sqrt(
                Math.Pow(s.Longitude - userPos[0], 2) +
                Math.Pow(s.Latitude - userPos[1], 2))).ToArray();
            for (int i = 1; i < dists.Length; i++)
            {
                Assert.That(dists[i], Is.GreaterThanOrEqualTo(dists[i - 1]));
            }
        }

    }

}
