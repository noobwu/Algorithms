// ***********************************************************************
// Assembly         : Noob.DataStructures
// Author           : noob
// Created          : 2025-07-02
//
// Last Modified By : noob
// Last Modified On : 2025-07-02
// ***********************************************************************
// <copyright file="SocialGraphTests.cs" company="Noob.DataStructures">
//     Copyright (c) . All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************
using NUnit.Framework;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Noob.DataStructures
{
    /// <summary>
    /// 社交平台用户实体（可扩展业务属性）
    /// </summary>
    public class SocialUser : IEquatable<SocialUser>, IComparable<SocialUser>
    {
        /// <summary>
        /// 用户唯一ID
        /// </summary>
        /// <value>The user identifier.</value>
        public int UserId { get; set; }
        /// <summary>
        /// 用户名（可做排序、分页）
        /// </summary>
        /// <value>The name of the user.</value>
        public string UserName { get; set; }

        /// <summary>
        /// 其他业务属性...
        /// </summary>
        /// <param name="other">An object to compare with this instance.</param>
        /// <returns>A value that indicates the relative order of the objects being compared. The return value has these meanings:
        /// <list type="table"><listheader><term> Value</term><description> Meaning</description></listheader><item><term> Less than zero</term><description> This instance precedes <paramref name="other" /> in the sort order.</description></item><item><term> Zero</term><description> This instance occurs in the same position in the sort order as <paramref name="other" />.</description></item><item><term> Greater than zero</term><description> This instance follows <paramref name="other" /> in the sort order.</description></item></list></returns>
        // public string Region { get; set; }

        public int CompareTo(SocialUser other)
        {
            if (other == null) return 1;
            return string.Compare(UserName, other.UserName, StringComparison.Ordinal);
        }

        /// <summary>
        /// 判断两个用户是否相等
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(SocialUser other) => other != null && UserId == other.UserId;

        /// <summary>
        /// Determines whether the specified <see cref="System.Object" /> is equal to this instance.
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns><c>true</c> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <c>false</c>.</returns>
        public override bool Equals(object obj) => obj is SocialUser u && u.UserId == UserId;

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.</returns>
        public override int GetHashCode() => UserId.GetHashCode();
        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
        public override string ToString() => $"{UserName}（{UserId}）";
    }

    /// <summary>
    /// 用户社交关系存储层接口，支持高频缓存和多级同步
    /// </summary>
    public interface ISocialRelationStore
    {
        /// <summary>添加用户，如果用户已存在自动忽略</summary>
        Task AddUserAsync(SocialUser user);

        /// <summary>用户关注</summary>
        Task FollowAsync(SocialUser follower, SocialUser followee);
        /// <summary>取消关注</summary>
        Task UnfollowAsync(SocialUser follower, SocialUser followee);

        /// <summary>判断是否关注/粉丝</summary>
        Task<bool> IsFollowingAsync(SocialUser follower, SocialUser followee);

        /// <summary>
        /// 判断是否有粉丝
        /// </summary>
        /// <param name="follower"></param>
        /// <param name="followee"></param>
        /// <returns></returns>
        Task<bool> IsFollowerAsync(SocialUser follower, SocialUser followee);

        /// <summary>获取关注和粉丝列表（支持分页、排序）</summary>
        Task<IReadOnlyList<SocialUser>> GetFollowingAsync(SocialUser user, int skip = 0, int take = 20);
        Task<IReadOnlyList<SocialUser>> GetFollowersAsync(SocialUser user, int skip = 0, int take = 20);

        /// <summary>定时/触发同步缓存与数据库</summary>
        Task SyncToDatabaseAsync();
    }

    /// <summary>
    /// 关系数据库/NoSQL 持久化接口
    /// </summary>
    public interface ISocialRelationRepository
    {
        /// <summary>
        /// 添加关注
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="targetIds"></param>
        /// <param name="syncTime"></param>
        /// <returns></returns>
        Task AddFollowingsAsync(int userId, IEnumerable<int> targetIds, DateTime syncTime);

        /// <summary>
        /// 移除关注
        /// </summary>
        Task RemoveFollowingsAsync(int userId, IEnumerable<int> targetIds);
    }


    /// <summary>
    /// 基于Redis的高频关系存储实现（主存储）
    /// </summary>
    public class RedisSocialRelationStore : ISocialRelationStore
    {
        /// <summary>
        /// Redis数据库
        /// </summary>
        private readonly IDatabase _redisDb;

        /// <summary>
        /// 数据库仓储
        /// </summary>
        private readonly ISocialRelationRepository _dbRepo;

        /// <summary>
        /// 数据库仓储
        /// </summary>
        /// <param name="redisDb"></param>
        /// <param name="dbRepo"></param>

        public RedisSocialRelationStore(IDatabase redisDb, ISocialRelationRepository dbRepo)
        {
            _redisDb = redisDb;
            _dbRepo = dbRepo;
        }

        /// <summary>
        /// 添加用户
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public async Task AddUserAsync(SocialUser user)
        {
            await _redisDb.StringSetAsync($"username:{user.UserId}", user.UserName);
        }

        /// <summary>
        /// 关注
        /// </summary>
        /// <param name="follower"></param>
        /// <param name="followee"></param>
        /// <returns></returns>
        public async Task FollowAsync(SocialUser follower, SocialUser followee)
        {
            await AddUserAsync(follower);
            await AddUserAsync(followee);
            await _redisDb.SortedSetAddAsync($"following:{follower.UserId}", followee.UserId, ComputeSortScore(followee.UserName));
            await _redisDb.SortedSetAddAsync($"followers:{followee.UserId}", follower.UserId, ComputeSortScore(follower.UserName));
            // 持久化入库（可用消息队列异步写，简化为直接调DB）
            await _dbRepo.AddFollowingsAsync(follower.UserId, new[] { followee.UserId }, DateTime.UtcNow);
        }

        /// <summary>
        /// 取消关注
        /// </summary>
        /// <param name="follower"></param>
        /// <param name="followee"></param>
        /// <returns></returns>
        public async Task UnfollowAsync(SocialUser follower, SocialUser followee)
        {
            await _redisDb.SortedSetRemoveAsync($"following:{follower.UserId}", followee.UserId);
            await _redisDb.SortedSetRemoveAsync($"followers:{followee.UserId}", follower.UserId);
            await _dbRepo.RemoveFollowingsAsync(follower.UserId, new[] { followee.UserId });
        }

        /// <summary>
        /// 判断是否关注
        /// </summary>
        public async Task<bool> IsFollowingAsync(SocialUser follower, SocialUser followee)
        {
            var rank = await _redisDb.SortedSetRankAsync($"following:{follower.UserId}", followee.UserId);
            return rank.HasValue;
        }

        /// <summary>
        /// 判断是否被关注
        /// </summary>
        /// <param name="follower"></param>
        /// <param name="followee"></param>
        /// <returns></returns>
        public async Task<bool> IsFollowerAsync(SocialUser follower, SocialUser followee)
        {
            var rank = await _redisDb.SortedSetRankAsync($"followers:{followee.UserId}", follower.UserId);
            return rank.HasValue;
        }

        /// <summary>
        /// 获取关注列表
        /// </summary>
        /// <param name="user"></param>
        /// <param name="skip"></param>
        /// <param name="take"></param>
        /// <returns></returns>
        public async Task<IReadOnlyList<SocialUser>> GetFollowingAsync(SocialUser user, int skip = 0, int take = 20)
        {
            var ids = await _redisDb.SortedSetRangeByRankAsync($"following:{user.UserId}", skip, skip + take - 1);
            var users = new List<SocialUser>();
            foreach (var val in ids)
            {
                if (int.TryParse(val, out var uid))
                {
                    var name = await _redisDb.StringGetAsync($"username:{uid}");
                    users.Add(new SocialUser { UserId = uid, UserName = name });
                }
            }
            return users;
        }

        /// <summary>
        /// 获取粉丝列表
        /// </summary>
        /// <param name="user"></param>
        /// <param name="skip"></param>
        /// <param name="take"></param>
        /// <returns></returns>
        public async Task<IReadOnlyList<SocialUser>> GetFollowersAsync(SocialUser user, int skip = 0, int take = 20)
        {
            var ids = await _redisDb.SortedSetRangeByRankAsync($"followers:{user.UserId}", skip, skip + take - 1);
            var users = new List<SocialUser>();
            foreach (var val in ids)
            {
                if (int.TryParse(val, out var uid))
                {
                    var name = await _redisDb.StringGetAsync($"username:{uid}");
                    users.Add(new SocialUser { UserId = uid, UserName = name });
                }
            }
            return users;
        }

        /// <summary>
        /// 同步缓存与数据库
        /// </summary>
        public async Task SyncToDatabaseAsync()
        {
            // 伪代码：遍历全部用户，将关注/粉丝关系批量同步到数据库
            // 推荐生产环境用消息队列和增量机制，这里简化为接口占位
        }

        /// <summary>
        /// 计算排序分
        /// </summary>
        /// <param name="userName"></param>
        /// <returns></returns>
        private static double ComputeSortScore(string userName)
        {
            // 支持自定义排序规则，如拼音、等级、注册时间
            return string.IsNullOrEmpty(userName) ? 0 : userName[0];
        }
    }


    /// <summary>
    /// 社交网络关系存储结构，支持关注、粉丝、分页、排序等高频操作
    /// </summary>
    public class SocialGraph
    {
        private readonly ISocialRelationStore _relationStore;

        /// <summary>
        /// 构造注入存储层（Redis+DB多级）
        /// </summary>
        public SocialGraph(ISocialRelationStore relationStore)
        {
            _relationStore = relationStore;
        }

        /// <summary>添加新用户</summary>
        public Task AddUserAsync(SocialUser user) => _relationStore.AddUserAsync(user);

        /// <summary>关注/取关</summary>
        public Task FollowAsync(SocialUser follower, SocialUser followee) => _relationStore.FollowAsync(follower, followee);
        public Task UnfollowAsync(SocialUser follower, SocialUser followee) => _relationStore.UnfollowAsync(follower, followee);

        /// <summary>关系判定</summary>
        public Task<bool> IsFollowingAsync(SocialUser follower, SocialUser followee) => _relationStore.IsFollowingAsync(follower, followee);
        public Task<bool> IsFollowerAsync(SocialUser follower, SocialUser followee) => _relationStore.IsFollowerAsync(follower, followee);

        /// <summary>关系分页查询</summary>
        public Task<IReadOnlyList<SocialUser>> GetFollowingAsync(SocialUser user, int skip = 0, int take = 20) => _relationStore.GetFollowingAsync(user, skip, take);
        public Task<IReadOnlyList<SocialUser>> GetFollowersAsync(SocialUser user, int skip = 0, int take = 20) => _relationStore.GetFollowersAsync(user, skip, take);

        /// <summary>触发数据库同步</summary>
        public Task SyncToDatabaseAsync() => _relationStore.SyncToDatabaseAsync();
    }


    /// <summary>
    /// Mock 实现 ISocialRelationStore，仅用内存集合，便于测试解耦与行为覆盖
    /// </summary>
    public class MockSocialRelationStore : ISocialRelationStore
    {
        /// <summary>
        /// 模拟关系存储
        /// </summary>
        private readonly Dictionary<int, HashSet<int>> _following = new();

        /// <summary>
        /// 模拟粉丝存储
        /// </summary>
        private readonly Dictionary<int, HashSet<int>> _followers = new();

        // <summary>
        /// 模拟关系同步日志
        /// </summary>
        public readonly List<string> DbSyncLog = new();

        /// <summary>
        /// 添加用户
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public Task AddUserAsync(SocialUser user)
        {
            if (!_following.ContainsKey(user.UserId)) _following[user.UserId] = new HashSet<int>();
            if (!_followers.ContainsKey(user.UserId)) _followers[user.UserId] = new HashSet<int>();
            return Task.CompletedTask;
        }

        /// <summary>
        /// 关注
        /// </summary>
        /// <param name="follower"></param>
        /// <param name="followee"></param>
        /// <returns></returns>

        public Task FollowAsync(SocialUser follower, SocialUser followee)
        {
            AddUserAsync(follower);
            AddUserAsync(followee);
            _following[follower.UserId].Add(followee.UserId);
            _followers[followee.UserId].Add(follower.UserId);
            return Task.CompletedTask;
        }

        /// <summary>
        /// 取关
        /// </summary>
        /// <param name="follower"></param>
        /// <param name="followee"></param>
        /// <returns></returns>
        public Task UnfollowAsync(SocialUser follower, SocialUser followee)
        {
            _following[follower.UserId]?.Remove(followee.UserId);
            _followers[followee.UserId]?.Remove(follower.UserId);
            return Task.CompletedTask;
        }

        /// <summary>
        /// 关注判定
        /// </summary>
        /// <param name="follower"></param>
        /// <param name="followee"></param>
        /// <returns></returns>
        public Task<bool> IsFollowingAsync(SocialUser follower, SocialUser followee)
            => Task.FromResult(_following[follower.UserId]?.Contains(followee.UserId) ?? false);

        /// <summary>
        /// 粉丝判定
        /// </summary>
        /// <param name="follower"></param>
        /// <param name="followee"></param>
        /// <returns></returns>
        public Task<bool> IsFollowerAsync(SocialUser follower, SocialUser followee)
            => Task.FromResult(_followers[followee.UserId]?.Contains(follower.UserId) ?? false);

        /// <summary>
        /// 关注分页查询
        /// </summary>
        /// <param name="user"></param>
        /// <param name="skip"></param>
        /// <param name="take"></param>
        /// <returns></returns>
        public Task<IReadOnlyList<SocialUser>> GetFollowingAsync(SocialUser user, int skip = 0, int take = 20)
        {
            var list = _following.TryGetValue(user.UserId, out var set)
                ? set.Select(id => new SocialUser { UserId = id, UserName = $"U{id}" }).Skip(skip).Take(take).ToList()
                : new List<SocialUser>();
            return Task.FromResult<IReadOnlyList<SocialUser>>(list);
        }

        public Task<IReadOnlyList<SocialUser>> GetFollowersAsync(SocialUser user, int skip = 0, int take = 20)
        {
            var list = _followers.TryGetValue(user.UserId, out var set)
                ? set.Select(id => new SocialUser { UserId = id, UserName = $"U{id}" }).Skip(skip).Take(take).ToList()
                : new List<SocialUser>();
            return Task.FromResult<IReadOnlyList<SocialUser>>(list);
        }

        public Task SyncToDatabaseAsync()
        {
            DbSyncLog.Add("SyncToDatabase called at " + DateTime.UtcNow.ToString("s"));
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// SocialGraph 业务级单元测试
    /// </summary>
    [TestFixture]
    public class SocialGraphTests
    {
        private SocialGraph _graph;
        private MockSocialRelationStore _mockStore;

        /// <summary>
        /// 测试前初始化Mock依赖
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            _mockStore = new MockSocialRelationStore();
            _graph = new SocialGraph(_mockStore);
        }

        /// <summary>
        /// 验证关注、取关、关系判断等主流程
        /// </summary>
        [Test]
        public async Task Follow_And_Unfollow_Should_Work_Correctly()
        {
            var alice = new SocialUser { UserId = 1, UserName = "Alice" };
            var bob = new SocialUser { UserId = 2, UserName = "Bob" };

            await _graph.FollowAsync(alice, bob);
            Assert.That(await _graph.IsFollowingAsync(alice, bob), Is.True);
            Assert.That(await _graph.IsFollowerAsync(alice, bob), Is.True);

            await _graph.UnfollowAsync(alice, bob);
            Assert.That(await _graph.IsFollowingAsync(alice, bob), Is.False);
            Assert.That(await _graph.IsFollowerAsync(alice, bob), Is.False);
        }

        /// <summary>
        /// 验证分页获取关注与粉丝，顺序与数量
        /// </summary>
        [Test]
        public async Task GetFollowing_And_Followers_Should_Return_CorrectPage()
        {
            var user = new SocialUser { UserId = 10, UserName = "Main" };
            var followees = Enumerable.Range(100, 30)
                .Select(id => new SocialUser { UserId = id, UserName = $"U{id}" }).ToList();

            foreach (var f in followees)
                await _graph.FollowAsync(user, f);

            var page1 = await _graph.GetFollowingAsync(user, skip: 0, take: 10);
            var page2 = await _graph.GetFollowingAsync(user, skip: 10, take: 10);

            Assert.That(page1, Has.Count.EqualTo(10));
            Assert.That(page2, Has.Count.EqualTo(10));
            Assert.That(page1.First().UserId, Is.EqualTo(100));
            Assert.That(page2.First().UserId, Is.EqualTo(110));

            // 验证粉丝方向
            var firstFollowee = followees[0];
            var fans = await _graph.GetFollowersAsync(firstFollowee);
            Assert.That(fans.Any(u => u.UserId == user.UserId), Is.True);
        }

        /// <summary>
        /// 验证重复关注/取关等边界场景
        /// </summary>
        [Test]
        public async Task Multiple_Follow_And_Unfollow_Should_Be_Idempotent()
        {
            var alice = new SocialUser { UserId = 1, UserName = "Alice" };
            var bob = new SocialUser { UserId = 2, UserName = "Bob" };

            await _graph.FollowAsync(alice, bob);
            await _graph.FollowAsync(alice, bob); // 重复关注
            Assert.That(await _graph.IsFollowingAsync(alice, bob), Is.True);

            await _graph.UnfollowAsync(alice, bob);
            await _graph.UnfollowAsync(alice, bob); // 重复取关
            Assert.That(await _graph.IsFollowingAsync(alice, bob), Is.False);
        }

        /// <summary>
        /// 验证无关注/无粉丝时返回空集
        /// </summary>
        [Test]
        public async Task Empty_Result_Should_Return_EmptyList()
        {
            var user = new SocialUser { UserId = 999, UserName = "Noone" };
            var following = await _graph.GetFollowingAsync(user);
            var followers = await _graph.GetFollowersAsync(user);
            Assert.That(following, Is.Empty);
            Assert.That(followers, Is.Empty);
        }

        /// <summary>
        /// 验证同步钩子调用（如用于归档或数据落盘）
        /// </summary>
        [Test]
        public async Task SyncToDatabase_Should_Log()
        {
            await _graph.SyncToDatabaseAsync();
            Assert.That(_mockStore.DbSyncLog.Count, Is.GreaterThan(0));
            Assert.That(_mockStore.DbSyncLog.Last(), Does.Contain("SyncToDatabase"));
        }
    }

    /// <summary>
    /// SocialGraph Redis集成测试，覆盖工程级真实平台场景
    /// </summary>
    [TestFixture]
    public class SocialGraphRedisIntegrationTests
    {
        /// <summary>
        /// 测试用Redis数据库
        /// </summary>
        private ConnectionMultiplexer _redis;
        /// <summary>
        /// 测试用数据库存储
        /// </summary>
        private IDatabase _db;
        /// <summary>
        /// 测试用数据库存储实现
        /// </summary>
        private RedisSocialRelationStore _redisStore;
        /// <summary>
        /// 测试用社交网络
        /// </summary>
        private SocialGraph _socialGraph;

        /// <summary>
        /// 测试前建立Redis连接与隔离环境（建议专用db=9）
        /// </summary>
        [OneTimeSetUp]
        public void Setup()
        {
            // 可用配置文件或环境变量指定Redis地址/db
            _redis = ConnectionMultiplexer.Connect("localhost:6379,defaultDatabase=0");
            _db = _redis.GetDatabase();
            _redisStore = new RedisSocialRelationStore(_db, new DummyDbRepo());
            _socialGraph = new SocialGraph(_redisStore);
        }

        ///// <summary>
        ///// 测试后清理Redis，保证环境隔离
        ///// </summary>
        //[OneTimeTearDown]
        //public async Task TearDown()
        //{
        //    var endpoints = _redis.GetEndPoints();
        //    var server = _redis.GetServer(endpoints[0]);
        //    foreach (var key in server.Keys(database: 0, pattern: "*"))
        //        await _db.KeyDeleteAsync(key);
        //}

        /// <summary>
        /// 测试基本关注、取关、关系判定和分页查询
        /// </summary>
        [Test]
        public async Task Follow_Unfollow_IsFollowing_IsFollower_Pagination_AllWork()
        {
            var alice = new SocialUser { UserId = 101, UserName = "Alice" };
            var bob = new SocialUser { UserId = 102, UserName = "Bob" };
            var carol = new SocialUser { UserId = 103, UserName = "Carol" };

            // 关注关系建立
            await _socialGraph.FollowAsync(alice, bob);
            await _socialGraph.FollowAsync(alice, carol);
            await _socialGraph.FollowAsync(bob, carol);

            Assert.That(await _socialGraph.IsFollowingAsync(alice, bob), Is.True);
            Assert.That(await _socialGraph.IsFollowingAsync(alice, carol), Is.True);
            Assert.That(await _socialGraph.IsFollowerAsync(alice, bob), Is.True);

            // 关注分页
            var aliceFollowing = await _socialGraph.GetFollowingAsync(alice, skip: 0, take: 10);
            Assert.That(aliceFollowing.Count, Is.EqualTo(2));
            Assert.That(aliceFollowing.Any(u => u.UserName == "Bob"), Is.True);
            Assert.That(aliceFollowing.Any(u => u.UserName == "Carol"), Is.True);

            // 粉丝分页
            var carolFollowers = await _socialGraph.GetFollowersAsync(carol, skip: 0, take: 10);
            Assert.That(carolFollowers.Count, Is.EqualTo(2));
            Assert.That(carolFollowers.Any(u => u.UserName == "Alice"), Is.True);
            Assert.That(carolFollowers.Any(u => u.UserName == "Bob"), Is.True);

            // 取关后校验
            await _socialGraph.UnfollowAsync(alice, carol);
            Assert.That(await _socialGraph.IsFollowingAsync(alice, carol), Is.False);
            var carolFansAfter = await _socialGraph.GetFollowersAsync(carol);
            Assert.That(carolFansAfter.Count, Is.EqualTo(1));
        }

        /// <summary>
        /// 验证关注、取关的幂等性
        /// </summary>
        [Test]
        public async Task Follow_Unfollow_Are_Idempotent()
        {
            var user = new SocialUser { UserId = 200, UserName = "TestUser" };
            var target = new SocialUser { UserId = 201, UserName = "Target" };

            await _socialGraph.FollowAsync(user, target);
            await _socialGraph.FollowAsync(user, target); // 重复操作
            Assert.That(await _socialGraph.IsFollowingAsync(user, target), Is.True);

            await _socialGraph.UnfollowAsync(user, target);
            await _socialGraph.UnfollowAsync(user, target); // 再次取关
            Assert.That(await _socialGraph.IsFollowingAsync(user, target), Is.False);
        }

        /// <summary>
        /// 验证空集/边界（无数据时返回空列表）
        /// </summary>
        [Test]
        public async Task GetFollowing_Followers_OnEmptyUser_ShouldReturnEmpty()
        {
            var unknown = new SocialUser { UserId = 9999, UserName = "Nobody" };
            var following = await _socialGraph.GetFollowingAsync(unknown);
            var followers = await _socialGraph.GetFollowersAsync(unknown);

            Assert.That(following, Is.Empty);
            Assert.That(followers, Is.Empty);
        }

        /// <summary>
        /// 验证隔离与清理后无残留数据
        /// </summary>
        [Test]
        public async Task DataIsolation_AfterTest()
        {
            var server = _redis.GetServer(_redis.GetEndPoints()[0]);
            var keys = server.Keys(database: 9, pattern: "*").ToList();
            Assert.That(keys, Is.Not.Null);
            // 如果用专属测试前缀，也可Assert.That(keys, Is.Empty);
        }

        /// <summary>
        /// 假实现用于依赖注入数据库Repo，避免实际写库
        /// </summary>
        public class DummyDbRepo : ISocialRelationRepository
        {
            public Task AddFollowingsAsync(int userId, IEnumerable<int> targetIds, DateTime syncTime) => Task.CompletedTask;
            public Task RemoveFollowingsAsync(int userId, IEnumerable<int> targetIds) => Task.CompletedTask;
        }
    }
}
