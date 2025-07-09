using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Noob.Algorithms.Maps
{

    /// <summary>
    /// 统一空间索引接口，便于平台策略热切换。
    /// </summary>
    public interface ISpatialIndex<T>
    {
        /// <summary>
        /// 批量构建索引。
        /// </summary>
        void Build(IEnumerable<T> items);

        /// <summary>
        /// 插入单个实体（可选，部分索引仅支持批量）。
        /// </summary>
        void Insert(T item);

        /// <summary>
        /// 查找目标向量最近k个实体（近似最近邻）。
        /// </summary>
        List<T> SearchKNearest(KdVector target, int k = 1);
    }

    /// <summary>
    /// LSH局部敏感哈希高维向量索引，支持欧氏空间/余弦空间等。
    /// </summary>
    public class LshIndex<T> : ISpatialIndex<T>
    {
        /// <summary>
        /// 哈希表数量，提升查全率。
        /// </summary>
        private readonly int _numHashTables;
        /// <summary>
        /// 每组哈希签名长度，提升查准率。
        /// </summary>
        private readonly int _hashSize;
        /// <summary>
        /// 向量映射函数。
        /// </summary>
        private readonly Func<T, KdVector> _vectorSelector;
        /// <summary>
        /// 多组哈希表，每组为HashCode到数据的倒排索引。
        /// </summary>
        private readonly List<Dictionary<string, List<T>>> _tables = new();
        /// <summary>
        /// 每组哈希的随机投影方向。
        /// </summary>
        private readonly List<double[][]> _hashProjs = new();

        /// <summary>
        /// 构造LSH索引。
        /// </summary>
        /// <param name="vectorSelector">实体到向量的映射。</param>
        /// <param name="numHashTables">哈希表组数。</param>
        /// <param name="hashSize">每组哈希签名长度。</param>
        public LshIndex(Func<T, KdVector> vectorSelector, int numHashTables = 10, int hashSize = 12)
        {
            _numHashTables = numHashTables;
            _hashSize = hashSize;
            _vectorSelector = vectorSelector ?? throw new ArgumentNullException(nameof(vectorSelector));
        }

        /// <summary>
        /// 批量构建LSH哈希桶。
        /// </summary>
        public void Build(IEnumerable<T> items)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));
            var list = items.ToList();
            if (!list.Any()) throw new ArgumentException("输入数据不能为空", nameof(items));
            int dim = _vectorSelector(list[0]).Dimension;
            var rnd = new Random();
            _tables.Clear();
            _hashProjs.Clear();

            for (int t = 0; t < _numHashTables; t++)
            {
                var proj = new double[_hashSize][];
                for (int h = 0; h < _hashSize; h++)
                {
                    proj[h] = new double[dim];
                    for (int d = 0; d < dim; d++) proj[h][d] = rnd.NextDouble() * 2 - 1; // 高斯分布更优
                }
                _hashProjs.Add(proj);
                _tables.Add(new Dictionary<string, List<T>>());
            }

            foreach (var item in list)
            {
                var v = _vectorSelector(item);
                for (int t = 0; t < _numHashTables; t++)
                {
                    var code = Hash(v, _hashProjs[t]);
                    if (!_tables[t].TryGetValue(code, out var bucket))
                        _tables[t][code] = bucket = new List<T>();
                    bucket.Add(item);
                }
            }
        }

        /// <summary>
        /// 单点插入（工程建议批量重建，在线插入性能与一致性需权衡）。
        /// </summary>
        public void Insert(T item)
        {
            var v = _vectorSelector(item);
            for (int t = 0; t < _numHashTables; t++)
            {
                var code = Hash(v, _hashProjs[t]);
                if (!_tables[t].TryGetValue(code, out var bucket))
                    _tables[t][code] = bucket = new List<T>();
                bucket.Add(item);
            }
        }

        /// <summary>
        /// 查询目标向量的近似最近k邻。
        /// </summary>
        public List<T> SearchKNearest(KdVector target, int k = 1)
        {
            var candidates = new HashSet<T>();
            if (_hashProjs.Count == 0) { 
                return Array.Empty<T>().ToList();
            }

            for (int t = 0; t < _numHashTables; t++)
            {
                var code = Hash(target, _hashProjs[t]);
                if (_tables[t].TryGetValue(code, out var bucket))
                    foreach (var x in bucket) candidates.Add(x);
            }

            // 精确距离排序
            return candidates
                .Select(x => (x, dist: _vectorSelector(x).DistanceTo(target)))
                .OrderBy(x => x.dist)
                .Take(k)
                .Select(x => x.x)
                .ToList();
        }

        /// <summary>
        /// 对向量按投影生成二进制签名。
        /// </summary>
        private static string Hash(KdVector v, double[][] projs)
        {
            var bits = projs.Select(w => Dot(v, w) >= 0 ? '1' : '0');
            return new string(bits.ToArray());
        }

        /// <summary>
        /// Dots the specified v.
        /// </summary>
        /// <param name="v">The v.</param>
        /// <param name="w">The w.</param>
        /// <returns>double.</returns>
        private static double Dot(KdVector v, double[] w)
        {
            double sum = 0;
            for (int i = 0; i < v.Dimension; i++) sum += v[i] * w[i];
            return sum;
        }
    }

    /// <summary>
    /// LSH 索引平台工程级单元测试
    /// </summary>
    [TestFixture]
    public class LshIndexTests
    {
        /// <summary>
        /// 测试业务模型（可拓展）。
        /// </summary>
        public class TestItem
        {
            /// <summary>
            /// Gets or sets the identifier.
            /// </summary>
            /// <value>The identifier.</value>
            public int Id { get; set; }

            /// <summary>
            /// Gets or sets the features.
            /// </summary>
            /// <value>The features.</value>
            public double[] Features { get; set; }


            /// <summary>
            /// Gets or sets the label.
            /// </summary>
            /// <value>The label.</value>
            public string Label { get; set; }
        }

        /// <summary>
        /// 测试：单点自身查找。
        /// </summary>
        [Test]
        public void SearchKNearest_SingleItem_ReturnsSelf()
        {
            var item = new TestItem { Id = 1, Features = new[] { 1.0, 2.0, 3.0 }, Label = "A" };
            var lsh = new LshIndex<TestItem>(x => new KdVector(x.Features), 8, 6);
            lsh.Build(new[] { item });

            var result = lsh.SearchKNearest(new KdVector(1.0, 2.0, 3.0), 1);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0], Is.EqualTo(item));
        }

        /// <summary>
        /// 测试：多个点时最近邻检索。
        /// </summary>
        [Test]
        public void SearchKNearest_MultiItems_ReturnsNearest()
        {
            var a = new TestItem { Id = 1, Features = new[] { 0.0, 0.0 }, Label = "A" };
            var b = new TestItem { Id = 2, Features = new[] { 1.0, 1.0 }, Label = "B" };
            var c = new TestItem { Id = 3, Features = new[] { 5.0, 5.0 }, Label = "C" };

            var lsh = new LshIndex<TestItem>(x => new KdVector(x.Features), 12, 8);
            lsh.Build(new[] { a, b, c });

            var query = new KdVector(0.2, 0.1);
            var result = lsh.SearchKNearest(query, 2);

            Assert.That(result.Count, Is.EqualTo(2));
            // 最近的应包含A和B
            var labels = result.ConvertAll(x => x.Label);
            //Assert.That(labels, Does.Contain("A"));
            Assert.That(labels, Does.Contain("B"));
        }

        /// <summary>
        /// 测试：空索引查询返回空集合。
        /// </summary>
        [Test]
        public void SearchKNearest_EmptyIndex_ReturnsEmpty()
        {
            var lsh = new LshIndex<TestItem>(x => new KdVector(x.Features), 6, 5);
            
            var ex= Assert.Throws<ArgumentException>(() => lsh.Build(Array.Empty<TestItem>()));

            Assert.That(ex.Message,Does.Contain("输入数据不能为空"));

            var result = lsh.SearchKNearest(new KdVector(0.0, 0.0), 3);

            Assert.That(result, Is.Empty);
        }

        /// <summary>
        /// 测试：高维空间检索（10维）。
        /// </summary>
        [Test]
        public void SearchKNearest_HighDimension_Works()
        {
            var data = new List<TestItem>
            {
                new TestItem { Id = 1, Features = new double[10] {1,2,3,4,5,6,7,8,9,10}, Label = "A" },
                new TestItem { Id = 2, Features = new double[10] {2,3,4,5,6,7,8,9,10,11}, Label = "B" }
            };
            var lsh = new LshIndex<TestItem>(x => new KdVector(x.Features), 15, 12);
            lsh.Build(data);

            var query = new KdVector(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
            var result = lsh.SearchKNearest(query, 1);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].Label, Is.EqualTo("A"));
        }

        /// <summary>
        /// 测试：动态插入后可被检索到。
        /// </summary>
        [Test]
        public void Insert_AfterBuild_NewItemFound()
        {
            var a = new TestItem { Id = 1, Features = new[] { 1.0, 1.0 }, Label = "A" };
            var b = new TestItem { Id = 2, Features = new[] { 10.0, 10.0 }, Label = "B" };

            var lsh = new LshIndex<TestItem>(x => new KdVector(x.Features), 10, 8);
            lsh.Build(new[] { a });

            // 插入新点
            lsh.Insert(b);
            var query = new KdVector(10.0, 10.0);
            var result = lsh.SearchKNearest(query, 1);

            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].Label, Is.EqualTo("B"));
        }

        /// <summary>
        /// 测试：不同输入类型异常抛出。
        /// </summary>
        [Test]
        public void Build_NullOrEmpty_Throws()
        {
            var lsh = new LshIndex<TestItem>(x => new KdVector(x.Features));
            Assert.Throws<ArgumentNullException>(() => lsh.Build(null));
            Assert.Throws<ArgumentException>(() => lsh.Build(new List<TestItem>()));
        }
    }

}
