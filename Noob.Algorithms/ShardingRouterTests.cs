using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Noob.Algorithms
{
    /// <summary>
    /// 表示一个可哈希的分片（如数据库分表、分库、服务节点）。
    /// </summary>
    public interface IShardNode
    {
        /// <summary>
        /// 分片唯一名称（如 UserTable_01 或 ShardDbA）。
        /// </summary>
        string ShardName { get; }

        /// <summary>
        /// 节点权重，影响虚节点数量（默认1）。
        /// </summary>
        int Weight { get; }
    }

    /// <summary>
    /// 分表物理节点实体。
    /// </summary>
    public class ShardNode : IShardNode
    {
        /// <summary>
        /// 分片唯一名称（如 UserTable_01 或 ShardDbA）。
        /// </summary>
        /// <value>The name of the shard.</value>
        public string ShardName { get; }

        /// <summary>
        /// 节点权重，影响虚节点数量（默认1）。
        /// </summary>
        /// <value>The weight.</value>
        public int Weight { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ShardNode"/> class.
        /// </summary>
        /// <param name="shardName">Name of the shard.</param>
        /// <param name="weight">The weight.</param>
        /// <exception cref="System.ArgumentNullException">shardName</exception>
        public ShardNode(string shardName, int weight = 1)
        {
            ShardName = shardName ?? throw new ArgumentNullException(nameof(shardName));
            Weight = weight > 0 ? weight : 1;
        }
    }

    /// <summary>
    /// 一致性哈希算法接口（便于扩展）。
    /// </summary>
    public interface IConsistentHashAlgorithm
    {
        /// <summary>
        /// 计算字符串Key的哈希值。
        /// </summary>
        /// <param name="key">待哈希的Key</param>
        /// <returns>哈希环上的位置</returns>
        uint Hash(string key);
    }

    /// <summary>
    /// MurmurHash3 32位高性能哈希算法实现。
    /// </summary>
    public class MurmurHash3Algorithm : IConsistentHashAlgorithm
    {
        /// <summary>
        /// 计算字符串Key的哈希值。
        /// </summary>
        /// <param name="key">待哈希的Key</param>
        /// <returns>哈希环上的位置</returns>
        /// <exception cref="System.ArgumentNullException">key</exception>
        public uint Hash(string key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            var bytes = Encoding.UTF8.GetBytes(key);
            uint seed = 0x9747b28c;
            uint h1 = seed;
            uint k1;
            for (int i = 0; i < bytes.Length; i += 4)
            {
                k1 = 0;
                for (int j = 0; j < 4 && i + j < bytes.Length; ++j)
                    k1 |= ((uint)bytes[i + j]) << (j * 8);
                k1 *= 0xcc9e2d51;
                k1 = (k1 << 15) | (k1 >> 17);
                k1 *= 0x1b873593;
                h1 ^= k1;
                h1 = (h1 << 13) | (h1 >> 19);
                h1 = h1 * 5 + 0xe6546b64;
            }
            h1 ^= (uint)bytes.Length;
            h1 ^= h1 >> 16;
            h1 *= 0x85ebca6b;
            h1 ^= h1 >> 13;
            h1 *= 0xc2b2ae35;
            h1 ^= h1 >> 16;
            return h1;
        }
    }

    /// <summary>
    /// 生产级支持虚节点一致性哈希分片的分表路由器（.NET Core平台工程用）。
    /// </summary>
    /// <typeparam name="TNode">实现IShardNode的分表节点类型</typeparam>
    public class ConsistentHashShardingRouter<TNode> where TNode : IShardNode
    {
        /// <summary>
        /// The hash ring
        /// </summary>
        private readonly SortedDictionary<uint, TNode> _hashRing = new();

        /// <summary>
        /// The node map
        /// </summary>
        private readonly Dictionary<string, TNode> _nodeMap = new();

        /// <summary>
        /// The hash algorithm
        /// </summary>
        private readonly IConsistentHashAlgorithm _hashAlgorithm;

        /// <summary>
        /// The virtual nodes per weight
        /// </summary>
        private readonly int _virtualNodesPerWeight;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="hashAlgorithm">可插拔哈希算法（默认MurmurHash3）</param>
        /// <param name="virtualNodesPerWeight">每权重虚节点数量，推荐100~200</param>
        public ConsistentHashShardingRouter(IConsistentHashAlgorithm hashAlgorithm = null, int virtualNodesPerWeight = 128)
        {
            _hashAlgorithm = hashAlgorithm ?? new MurmurHash3Algorithm();
            _virtualNodesPerWeight = Math.Max(1, virtualNodesPerWeight);
        }

        /// <summary>
        /// 添加分表节点（自动生成虚节点，支持动态扩容）。
        /// </summary>
        /// <param name="node">分表节点</param>
        public void AddNode(TNode node)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            if (_nodeMap.ContainsKey(node.ShardName))
                throw new InvalidOperationException($"Shard '{node.ShardName}' already exists.");
            _nodeMap[node.ShardName] = node;

            int virtualCount = node.Weight * _virtualNodesPerWeight;
            for (int i = 0; i < virtualCount; i++)
            {
                string vnodeKey = $"{node.ShardName}#VN{i}";
                uint hash = _hashAlgorithm.Hash(vnodeKey);
                _hashRing[hash] = node;
            }
        }

        /// <summary>
        /// 移除分表节点及其虚节点。
        /// </summary>
        /// <param name="shardName">节点名称</param>
        public void RemoveNode(string shardName)
        {
            if (!_nodeMap.TryGetValue(shardName, out var node))
                return;
            int virtualCount = node.Weight * _virtualNodesPerWeight;
            for (int i = 0; i < virtualCount; i++)
            {
                string vnodeKey = $"{node.ShardName}#VN{i}";
                uint hash = _hashAlgorithm.Hash(vnodeKey);
                _hashRing.Remove(hash);
            }
            _nodeMap.Remove(shardName);
        }

        /// <summary>
        /// 路由Key到分表节点（如用于动态表名/库名/连接串切换）。
        /// </summary>
        /// <param name="shardingKey">业务分片Key</param>
        /// <returns>命中的分表节点</returns>
        public TNode Route(string shardingKey)
        {
            if (_hashRing.Count == 0) throw new InvalidOperationException("No shards available in the hash ring.");
            uint hash = _hashAlgorithm.Hash(shardingKey);
            foreach (var kvp in _hashRing)
            {
                if (kvp.Key >= hash)
                    return kvp.Value;
            }
            return _hashRing.First().Value; // 环首
        }

        /// <summary>
        /// 获取全部分表节点信息。
        /// </summary>
        public IEnumerable<TNode> AllNodes => _nodeMap.Values;

        /// <summary>
        /// 获取虚节点总数（监控/调优用）。
        /// </summary>
        public int VirtualNodeCount => _hashRing.Count;
    }


    /// <summary>
    /// ConsistentHashShardingRouter + ORM动态分表业务场景单元测试
    /// </summary>
    [TestFixture]
    public class ConsistentHashShardingRouterOrmTests
    {
        /// <summary>
        /// 模拟ORM分表节点，支持动态表名/连接串。
        /// </summary>
        class OrmShardNode : ShardNode
        {
            /// <summary>
            /// Gets the connection string.
            /// </summary>
            /// <value>The connection string.</value>
            public string ConnectionString { get; }

            /// <summary>
            /// Initializes a new instance of the <see cref="OrmShardNode"/> class.
            /// </summary>
            /// <param name="shardName">Name of the shard.</param>
            /// <param name="connectionString">The connection string.</param>
            /// <param name="weight">The weight.</param>
            public OrmShardNode(string shardName, string connectionString, int weight = 1)
                : base(shardName, weight)
            {
                ConnectionString = connectionString;
            }

            /// <summary>
            /// Determines whether the specified <see cref="System.Object" /> is equal to this instance.
            /// </summary>
            /// <param name="obj">The object to compare with the current object.</param>
            /// <returns><c>true</c> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <c>false</c>.</returns>
            public override bool Equals(object obj) =>
                obj is OrmShardNode other && ShardName == other.ShardName && ConnectionString == other.ConnectionString;

            /// <summary>
            /// Returns a hash code for this instance.
            /// </summary>
            /// <returns>A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.</returns>
            public override int GetHashCode() => (ShardName, ConnectionString).GetHashCode();
        }

        /// <summary>
        /// 路由多个userId，分布在不同分表，表名可供ORM动态选择。
        /// </summary>
        [Test]
        public void Route_UserIds_ShouldDistributeToDifferentTables()
        {
            var router = new ConsistentHashShardingRouter<OrmShardNode>();
            var nodes = new List<OrmShardNode>
            {
                new OrmShardNode("UserTable_A", "Server=DbA;"),
                new OrmShardNode("UserTable_B", "Server=DbB;"),
                new OrmShardNode("UserTable_C", "Server=DbC;")
            };
            nodes.ForEach(router.AddNode);

            var tables = new HashSet<string>();
            for (int i = 1; i <= 20; i++)
            {
                var userId = "user:" + (1000 + i * 31); // 保证hash分布
                var node = router.Route(userId);
                tables.Add(node.ShardName);
                Console.WriteLine($"UserId: {userId} -> {node.ShardName}");
                Assert.That(node.ShardName, Does.StartWith("UserTable_"));
                Assert.That(node.ConnectionString, Does.Contain("Server=Db"));
            }
            // 至少应命中2~3个表，分布均衡
            Assert.That(tables.Count, Is.InRange(2, 3));
        }

        /// <summary>
        /// ORM集成：Route结果可直接用于动态表名/库名/连接串切换。
        /// </summary>
        [Test]
        public void Route_OrmIntegration_ReturnsCorrectConnection()
        {
            var router = new ConsistentHashShardingRouter<OrmShardNode>();
            router.AddNode(new OrmShardNode("OrderTable_01", "Server=db01;Database=orderdb01;"));
            router.AddNode(new OrmShardNode("OrderTable_02", "Server=db02;Database=orderdb02;"));

            var orderId = "order:8888888";
            var node = router.Route(orderId);

            Assert.That(node.ConnectionString, Does.Contain("Database=orderdb"));
            Assert.That(node.ShardName, Does.StartWith("OrderTable_"));
            Console.WriteLine($"OrderId: {orderId} -> Shard: {node.ShardName}, Connection: {node.ConnectionString}");
            // ORM: context.UseTable(node.ShardName) or context.ChangeDatabase(node.ConnectionString)
        }

        /// <summary>
        /// 动态增删分表，绝大多数老key路由结果稳定，仅少部分key迁移。
        /// </summary>
        [Test]
        public void Route_DynamicAddRemoveNode_MostKeyStable()
        {
            var router = new ConsistentHashShardingRouter<OrmShardNode>();
            var n1 = new OrmShardNode("T1", "Db1");
            var n2 = new OrmShardNode("T2", "Db2");
            router.AddNode(n1);
            router.AddNode(n2);

            var keys = new List<string>();
            for (int i = 0; i < 200; i++) keys.Add("uid:" + i);
            var before = new Dictionary<string, string>();
            foreach (var key in keys) before[key] = router.Route(key).ShardName;

            // 扩容
            var n3 = new OrmShardNode("T3", "Db3");
            router.AddNode(n3);

            int unchanged = 0, changed = 0;
            foreach (var key in keys)
            {
                var after = router.Route(key).ShardName;
                if (after == before[key]) unchanged++; else changed++;
            }
            Assert.That(changed, Is.LessThan(80));
            Assert.That(unchanged, Is.GreaterThan(120));
        }

        /// <summary>
        /// 虚节点均匀性保障：大量数据平均分布于所有分表。
        /// </summary>
        [Test]
        public void Route_VirtualNodeDistribution_ShouldBeBalanced()
        {
            var router = new ConsistentHashShardingRouter<OrmShardNode>(virtualNodesPerWeight: 200);
            var nodes = new List<OrmShardNode>
        {
            new OrmShardNode("ShardA", "ConnA"),
            new OrmShardNode("ShardB", "ConnB"),
            new OrmShardNode("ShardC", "ConnC")
        };
            nodes.ForEach(router.AddNode);

            var counter = new Dictionary<string, int> { { "ShardA", 0 }, { "ShardB", 0 }, { "ShardC", 0 } };
            for (int i = 0; i < 6000; i++)
            {
                var node = router.Route("user:" + i);
                counter[node.ShardName]++;
            }
            // 理论每分表约2000，20%波动内
            foreach (var cnt in counter.Values)
                Assert.That(cnt, Is.InRange(1600, 2400));
        }

        /// <summary>
        /// 没有分表节点时路由应抛出异常。
        /// </summary>
        [Test]
        public void Route_EmptyRing_Throws()
        {
            var router = new ConsistentHashShardingRouter<OrmShardNode>();
            Assert.That(() => router.Route("testkey"), Throws.InvalidOperationException);
        }
    }
}
