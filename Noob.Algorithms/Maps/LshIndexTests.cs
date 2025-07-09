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
}
