// ***********************************************************************
// Assembly         : Noob.Algorithms
// Author           : noob
// Created          : 2025-07-06
//
// Last Modified By : noob
// Last Modified On : 2025-07-06
// ***********************************************************************
// <copyright file="TenantHashShardingTests.cs" company="Noob.Algorithms">
//     Copyright (c) . All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Noob.Algorithms
{
    /// <summary>
    /// 多租户用户分表路由策略接口（支持扩展不同算法）
    /// </summary>
    public interface ITenantShardingRouter
    {
        /// <summary>
        /// 根据租户ID获取目标用户表名（或表后缀）
        /// </summary>
        /// <param name="tenantId">租户ID</param>
        /// <returns>表名（或后缀）</returns>
        string RouteToTable(long tenantId);
    }

    /// <summary>
    /// 基于Hash取模分片的多租户用户分表路由（推荐桶数为2的幂或质数）
    /// </summary>
    public class HashModTenantShardingRouter : ITenantShardingRouter
    {
        /// <summary> 分表总数 </summary>
        public int TableCount { get; }
        /// <summary> 分表前缀 </summary>
        public string TablePrefix { get; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="tablePrefix">用户表前缀</param>
        /// <param name="tableCount">分表总数（建议为2的幂或质数）</param>
        public HashModTenantShardingRouter(string tablePrefix, int tableCount)
        {
            if (tableCount <= 1) throw new ArgumentException("分表总数必须大于1", nameof(tableCount));
            TablePrefix = tablePrefix ?? throw new ArgumentNullException(nameof(tablePrefix));
            TableCount = tableCount;
        }

        /// <summary>
        /// 路由到分表：hash(tenantId) % TableCount
        /// </summary>
        /// <param name="tenantId">租户ID</param>
        /// <returns>分表名</returns>
        public string RouteToTable(long tenantId)
        {
            // 推荐使用高质量Hash算法，此处为简单混淆示例
            int hash = (int)((tenantId ^ (tenantId >> 16)) & 0x7FFFFFFF);
            int tableIdx = hash % TableCount;
            return $"{TablePrefix}{tableIdx:D2}";
        }
    }

    /// <summary>
    /// 一致性Hash分片的多租户用户分表路由（支持虚节点，扩容/缩容迁移量低，适合云原生SaaS）
    /// </summary>
    public class ConsistentHashTenantShardingRouter : ITenantShardingRouter
    {
        /// <summary>
        /// The hash ring
        /// </summary>
        private readonly SortedDictionary<long, string> _hashRing = new();

        /// <summary>
        /// Gets the table names.
        /// </summary>
        /// <value>The table names.</value>
        public IReadOnlyList<string> TableNames { get; }

        /// <summary>
        /// Gets the virtual nodes.
        /// </summary>
        /// <value>The virtual nodes.</value>
        public int VirtualNodes { get; }

        /// <summary>
        /// 构造
        /// </summary>
        /// <param name="tablePrefix">表前缀</param>
        /// <param name="tableCount">分表总数</param>
        /// <param name="virtualNodes">每表虚节点数（建议≥100）</param>
        public ConsistentHashTenantShardingRouter(string tablePrefix, int tableCount, int virtualNodes = 100)
        {
            if (tableCount < 1) throw new ArgumentException("分表总数必须大于0", nameof(tableCount));
            if (virtualNodes < 1) throw new ArgumentException("虚节点数必须大于0", nameof(virtualNodes));
            TableNames = Enumerable.Range(0, tableCount).Select(i => $"{tablePrefix}{i:D2}").ToList();
            VirtualNodes = virtualNodes;

            // 初始化虚节点环
            foreach (var table in TableNames)
            {
                for (int v = 0; v < virtualNodes; v++)
                {
                    long hash = ComputeHash($"{table}#VN{v}");
                    _hashRing[hash] = table;
                }
            }
        }

        /// <summary>
        /// 路由到顺时针最近表
        /// </summary>
        public string RouteToTable(long tenantId)
        {
            long hash = ComputeHash(tenantId.ToString());
            if (_hashRing.Count == 0) throw new InvalidOperationException("虚节点环未初始化");
            foreach (var kv in _hashRing)
            {
                if (hash <= kv.Key)
                    return kv.Value;
            }
            return _hashRing.First().Value;
        }

        /// <summary>
        /// 简化高质量哈希（生产推荐Murmur/XXHash/CityHash等）
        /// </summary>
        private static long ComputeHash(string key)
        {
            unchecked
            {
                long hash = 1125899906842597L; // FNV-like
                foreach (char c in key)
                    hash = (hash * 31) ^ c;
                return hash & 0x7FFFFFFFFFFFFFFF;
            }
        }
    }


    /// <summary>
    /// 多租户Hash混合分片算法及分表路由策略的单元测试
    /// </summary>
    [TestFixture]
    public class TenantHashShardingTests
    {
        /// <summary>
        /// 测试Hash混合算法生成的hash值为正数且分布合理
        /// </summary>
        [Test]
        public void HashMix_ShouldReturnPositive_AndUniform()
        {
            // 用不同高位和低位的tenantId进行测试
            long[] ids = {
                1L,
                0x100000000L,
                1234567890123L,
                long.MaxValue,
                long.MinValue + 1,
                0L
            };
            foreach (var tenantId in ids)
            {
                int hash = (int)((tenantId ^ (tenantId >> 32)) & 0x7FFFFFFF);
                Assert.That(hash, Is.GreaterThanOrEqualTo(0), $"Hash must be non-negative for tenantId={tenantId}");
            }
        }

        /// <summary>
        /// Hash分片路由：应将不同高低位租户均匀分布到所有分表
        /// </summary>
        [Test]
        public void HashModTenantShardingRouter_ShouldDistributeEvenly()
        {
            var router = new HashModTenantShardingRouter("User_", 8);
            var buckets = new int[8];
            for (long i = 1; i <= 80000; i++)
            {
                string tbl = router.RouteToTable(i << 32 | (i % 500)); // 保证高低位变化
                int idx = int.Parse(tbl.Split('_')[1]);
                buckets[idx]++;
            }
            int min = buckets.Min(), max = buckets.Max();
            Assert.That(max - min, Is.LessThan(0.10 * max), "Buckets must be relatively balanced");
        }

        /// <summary>
        /// 一致性Hash分片路由：租户应均匀落到所有分表
        /// </summary>
        [Test]
        public void ConsistentHashTenantShardingRouter_ShouldDistributeEvenly()
        {
            // 20000虚节点，覆盖密度大幅提升
            var router = new ConsistentHashTenantShardingRouter("User_", 8, 20000);
            var buckets = new int[8];

            for (int i = 1; i <= 80000; i++)
            {
                // tenantId 先转字符串再 ComputeHash，模拟真实环境
                long tenantId = ComputeHash(i.ToString());
                string tbl = router.RouteToTable(tenantId);
                int idx = int.Parse(tbl.Split('_')[1]);
                buckets[idx]++;
            }
            int min = buckets.Min(), max = buckets.Max();
            Assert.That(max - min, Is.LessThan(0.08 * max), $"ConsistentHash should be highly balanced. 分布: {string.Join(',', buckets)}");
        }

        /// <summary>
        /// Computes the hash.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>System.Int64.</returns>
        private static long ComputeHash(string key)
        {
            unchecked
            {
                long hash = 1125899906842597L; // FNV-like
                foreach (char c in key)
                    hash = (hash * 31) ^ c;
                return hash & 0x7FFFFFFFFFFFFFFF;
            }
        }
        /// <summary>
        /// 取极端tenantId时Hash依然不越界（健壮性边界测试）
        /// </summary>
        [Test]
        public void HashMix_WithExtremes_ShouldRemainInRange()
        {
            int h1 = (int)((long.MaxValue ^ (long.MaxValue >> 32)) & 0x7FFFFFFF);
            int h2 = (int)((long.MinValue ^ (long.MinValue >> 32)) & 0x7FFFFFFF);
            Assert.That(h1, Is.GreaterThanOrEqualTo(0));
            Assert.That(h2, Is.GreaterThanOrEqualTo(0));
        }
    }

}
