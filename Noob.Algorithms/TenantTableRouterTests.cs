using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace Noob.Algorithms
{

    /// <summary>
    /// 平台工程级多租户用户表路由器，实现“大租户”优先哈希分片和普通租户哈希分片的混合分表方案。
    /// </summary>
    public class TenantUserTableRouter
    {
        /// <summary>
        /// 普通租户分表数量，建议为质数或2的幂，支持动态扩容。
        /// </summary>
        private readonly int _normalShardCount;

        /// <summary>
        /// 大租户哈希分片数量（大租户分片表数），可根据实际大客户数动态调整。
        /// </summary>
        private readonly int _bigShardCount;

        /// <summary>
        /// 大租户集合（建议可由缓存/配置中心/DB实时同步）。
        /// </summary>
        private readonly HashSet<long> _bigTenantIds;

        /// <summary>
        /// VIP租户集合（每个VIP租户独立表/库）。
        /// </summary>
        private readonly HashSet<long> _vipTenantIds;

        /// <summary>
        /// 构造函数，初始化分片数量与租户集合。
        /// </summary>
        /// <param name="normalShardCount">普通租户分表数量</param>
        /// <param name="bigShardCount">大租户分表数量</param>
        /// <param name="bigTenantIds">大租户ID集合</param>
        /// <param name="vipTenantIds">VIP租户ID集合</param>
        public TenantUserTableRouter(
            int normalShardCount,
            int bigShardCount,
            IEnumerable<long> bigTenantIds = null,
            IEnumerable<long> vipTenantIds = null)
        {
            if (normalShardCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(normalShardCount), "普通租户分片数量须大于0");
            if (bigShardCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(bigShardCount), "大租户分片数量须大于0");

            _normalShardCount = normalShardCount;
            _bigShardCount = bigShardCount;
            _bigTenantIds = new HashSet<long>(bigTenantIds ?? Array.Empty<long>());
            _vipTenantIds = new HashSet<long>(vipTenantIds ?? Array.Empty<long>());
        }

        /// <summary>
        /// 获取指定租户ID的用户表名（兼容大租户、VIP租户和普通租户分片）。
        /// </summary>
        /// <param name="tenantId">租户ID</param>
        /// <returns>用户表名（如user_vip_{id}、user_big_{idx}、user_shard_{idx}）</returns>
        public string GetUserTableName(long tenantId)
        {
            if (IsVipTenant(tenantId))
                return $"user_vip_{tenantId}"; // 超级大客户独立表
            if (IsBigTenant(tenantId))
            {
                int idx = ComputeMultiplicativeHash(tenantId, _bigShardCount);
                return $"user_big_{idx}";
            }
            int shardIdx = ComputeMultiplicativeHash(tenantId, _normalShardCount);
            return $"user_shard_{shardIdx}";
        }

        /// <summary>
        /// 判断是否为VIP租户（可独立表/库）。
        /// </summary>
        /// <param name="tenantId">租户ID</param>
        public bool IsVipTenant(long tenantId) => _vipTenantIds.Contains(tenantId);

        /// <summary>
        /// 判断是否为大租户（大客户，优先哈希分片）。
        /// </summary>
        /// <param name="tenantId">租户ID</param>
        public bool IsBigTenant(long tenantId) => _bigTenantIds.Contains(tenantId);

        /// <summary>
        /// 动态添加大租户（支持升级/扩容）。
        /// </summary>
        /// <param name="tenantId">要升级的大租户ID</param>
        public void AddBigTenant(long tenantId) => _bigTenantIds.Add(tenantId);

        /// <summary>
        /// 动态移除大租户（支持降级）。
        /// </summary>
        /// <param name="tenantId">要降级的租户ID</param>
        public void RemoveBigTenant(long tenantId) => _bigTenantIds.Remove(tenantId);

        /// <summary>
        /// 动态添加VIP租户（独立表/库）。
        /// </summary>
        /// <param name="tenantId">VIP租户ID</param>
        public void AddVipTenant(long tenantId) => _vipTenantIds.Add(tenantId);

        /// <summary>
        /// 动态移除VIP租户（支持降级）。
        /// </summary>
        /// <param name="tenantId">VIP租户ID</param>
        public void RemoveVipTenant(long tenantId) => _vipTenantIds.Remove(tenantId);

        /// <summary>
        /// 工程级乘法哈希（Knuth建议A），支持高效均匀分布，便于分表扩容。
        /// </summary>
        /// <param name="key">哈希key（如租户ID）</param>
        /// <param name="bucketCount">桶数量（分片数）</param>
        /// <returns>哈希下标</returns>
        protected virtual int ComputeMultiplicativeHash(long key, int bucketCount)
        {
            long normalizedKey = Math.Abs(key);
            double A = 0.6180339887;
            double frac = (normalizedKey * A) % 1;
            int hash = (int)(bucketCount * frac);
            return hash < 0 ? 0 : (hash >= bucketCount ? bucketCount - 1 : hash);
        }
    }


    /// <summary>
    /// TenantUserTableRouter 单元测试（NUnit + XML 注释 + Assert.That）。
    /// </summary>
    [TestFixture]
    public class TenantUserTableRouterTests
    {
        /// <summary>
        /// 验证VIP租户应被路由到独立表。
        /// </summary>
        [Test]
        public void GetUserTableName_VipTenant_RouteToVipTable()
        {
            var router = new TenantUserTableRouter(8, 4, null, new[] { 9001L, 9002L });
            Assert.That(router.GetUserTableName(9001L), Is.EqualTo("user_vip_9001"));
            Assert.That(router.GetUserTableName(9002L), Is.EqualTo("user_vip_9002"));
        }

        /// <summary>
        /// 验证大租户应被路由到大租户哈希分片表。
        /// </summary>
        [Test]
        public void GetUserTableName_BigTenant_RouteToBigTable()
        {
            var bigTenants = new HashSet<long> { 8001L, 8002L };
            var router = new TenantUserTableRouter(8, 3, bigTenants);
            int idx1 = GetMultiplicativeHash(8001L, 3);
            int idx2 = GetMultiplicativeHash(8002L, 3);

            Assert.That(router.GetUserTableName(8001L), Is.EqualTo($"user_big_{idx1}"));
            Assert.That(router.GetUserTableName(8002L), Is.EqualTo($"user_big_{idx2}"));
        }

        /// <summary>
        /// 验证普通租户应被路由到普通分片表。
        /// </summary>
        [Test]
        public void GetUserTableName_NormalTenant_RouteToNormalShard()
        {
            var router = new TenantUserTableRouter(5, 3);
            int idx = GetMultiplicativeHash(555L, 5);
            string expect = $"user_shard_{idx}";
            Assert.That(router.GetUserTableName(555L), Is.EqualTo(expect));
        }

        /// <summary>
        /// 验证VIP租户动态添加后被路由到独立表。
        /// </summary>
        [Test]
        public void AddVipTenant_AfterAdd_RouteToVipTable()
        {
            var router = new TenantUserTableRouter(4, 2);
            Assert.That(router.GetUserTableName(12345L), Does.StartWith("user_shard_"));
            router.AddVipTenant(12345L);
            Assert.That(router.GetUserTableName(12345L), Is.EqualTo("user_vip_12345"));
        }

        /// <summary>
        /// 验证VIP租户移除后被路由到普通分片。
        /// </summary>
        [Test]
        public void RemoveVipTenant_AfterRemove_RouteToNormalShard()
        {
            var router = new TenantUserTableRouter(6, 3, null, new[] { 456L });
            Assert.That(router.GetUserTableName(456L), Is.EqualTo("user_vip_456"));
            router.RemoveVipTenant(456L);
            Assert.That(router.GetUserTableName(456L), Does.StartWith("user_shard_"));
        }

        /// <summary>
        /// 验证大租户动态添加与移除，路由正确变化。
        /// </summary>
        [Test]
        public void AddRemoveBigTenant_RouteChange()
        {
            var router = new TenantUserTableRouter(7, 4);
            Assert.That(router.GetUserTableName(8888L), Does.StartWith("user_shard_"));
            router.AddBigTenant(8888L);
            Assert.That(router.GetUserTableName(8888L), Does.StartWith("user_big_"));
            router.RemoveBigTenant(8888L);
            Assert.That(router.GetUserTableName(8888L), Does.StartWith("user_shard_"));
        }

        /// <summary>
        /// 验证VIP判定与大租户判定功能。
        /// </summary>
        [Test]
        public void IsVipTenantAndIsBigTenant_Works()
        {
            var router = new TenantUserTableRouter(5, 3, new[] { 100L }, new[] { 200L });
            Assert.That(router.IsVipTenant(200L), Is.True);
            Assert.That(router.IsBigTenant(100L), Is.True);
            Assert.That(router.IsVipTenant(100L), Is.False);
            Assert.That(router.IsBigTenant(200L), Is.False);
        }

        /// <summary>
        /// 验证分片下标算法在不同分片数量下正常分布。
        /// </summary>
        [Test]
        public void GetUserTableName_DifferentTenants_ShardDistribution()
        {
            var router = new TenantUserTableRouter(6, 2);
            var set = new HashSet<string>();
            for (long i = 1; i <= 12; i++)
            {
                set.Add(router.GetUserTableName(i));
            }
            Assert.That(set.Count, Is.GreaterThan(2)); // 至少分散到多个表
        }

        /// <summary>
        /// 验证初始化时非法分片数抛出异常。
        /// </summary>
        [Test]
        public void InitWithInvalidShardCount_Throws()
        {
            Assert.That(() => new TenantUserTableRouter(0, 1), Throws.Exception);
            Assert.That(() => new TenantUserTableRouter(1, 0), Throws.Exception);
            Assert.That(() => new TenantUserTableRouter(-1, 3), Throws.Exception);
        }

        /// <summary>
        /// 验证同一租户多次路由结果一致。
        /// </summary>
        [Test]
        public void GetUserTableName_SameTenantId_Consistent()
        {
            var router = new TenantUserTableRouter(8, 4);
            var t1 = router.GetUserTableName(123456789L);
            var t2 = router.GetUserTableName(123456789L);
            Assert.That(t1, Is.EqualTo(t2));
        }

        /// <summary>
        /// 辅助方法：用主代码一致的乘法哈希算法计算下标。
        /// </summary>
        private static int GetMultiplicativeHash(long key, int bucketCount)
        {
            long normalizedKey = Math.Abs(key);
            double A = 0.6180339887;
            double frac = (normalizedKey * A) % 1;
            int hash = (int)(bucketCount * frac);
            return hash < 0 ? 0 : (hash >= bucketCount ? bucketCount - 1 : hash);
        }
    }


}
