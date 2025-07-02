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
    public class SocialUser : IComparable<SocialUser>
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
    /// 社交网络关系存储结构，支持关注、粉丝、分页、排序等高频操作
    /// </summary>
    public class SocialGraph
    {
        /// <summary>
        /// 关注邻接表（每个用户的关注集合，按用户名有序）
        /// </summary>
        private readonly Dictionary<SocialUser, SortedSet<SocialUser>> _followingTable;

        /// <summary>
        /// 粉丝逆邻接表（每个用户的粉丝集合，按用户名有序）
        /// </summary>
        private readonly Dictionary<SocialUser, SortedSet<SocialUser>> _followerTable;

        /// <summary>
        /// 构造函数，初始化表结构
        /// </summary>
        public SocialGraph()
        {
            _followingTable = new Dictionary<SocialUser, SortedSet<SocialUser>>();
            _followerTable = new Dictionary<SocialUser, SortedSet<SocialUser>>();
        }

        /// <summary>
        /// 添加用户（如果已存在自动忽略）
        /// </summary>
        public void AddUser(SocialUser user)
        {
            if (!_followingTable.ContainsKey(user))
                _followingTable[user] = new SortedSet<SocialUser>();
            if (!_followerTable.ContainsKey(user))
                _followerTable[user] = new SortedSet<SocialUser>();
        }

        /// <summary>
        /// 用户A关注用户B
        /// </summary>
        public void Follow(SocialUser from, SocialUser to)
        {
            AddUser(from); AddUser(to);
            _followingTable[from].Add(to);
            _followerTable[to].Add(from);
        }

        /// <summary>
        /// 用户A取消关注用户B
        /// </summary>
        public void Unfollow(SocialUser from, SocialUser to)
        {
            if (_followingTable.ContainsKey(from)) _followingTable[from].Remove(to);
            if (_followerTable.ContainsKey(to)) _followerTable[to].Remove(from);
        }

        /// <summary>
        /// 判断A是否关注B
        /// </summary>
        public bool IsFollowing(SocialUser from, SocialUser to)
        {
            return _followingTable.ContainsKey(from) && _followingTable[from].Contains(to);
        }

        /// <summary>
        /// 判断A是否是B的粉丝
        /// </summary>
        public bool IsFollower(SocialUser follower, SocialUser user)
        {
            return _followerTable.ContainsKey(user) && _followerTable[user].Contains(follower);
        }

        /// <summary>
        /// 获取用户的关注列表（支持排序分页）
        /// </summary>
        public IEnumerable<SocialUser> GetFollowing(SocialUser user, int skip = 0, int take = int.MaxValue)
        {
            if (!_followingTable.ContainsKey(user)) yield break;
            foreach (var u in _followingTable[user])
            {
                if (skip > 0) { skip--; continue; }
                if (take-- <= 0) yield break;
                yield return u;
            }
        }

        /// <summary>
        /// 获取用户的粉丝列表（支持排序分页）
        /// </summary>
        public IEnumerable<SocialUser> GetFollowers(SocialUser user, int skip = 0, int take = int.MaxValue)
        {
            if (!_followerTable.ContainsKey(user)) yield break;
            foreach (var u in _followerTable[user])
            {
                if (skip > 0) { skip--; continue; }
                if (take-- <= 0) yield break;
                yield return u;
            }
        }

        /// <summary>
        /// 支持分片部署：通过哈希获取用户属于哪个分片（模拟分布式）
        /// </summary>
        public static int GetShardIndex(int userId, int totalShards)
            => Math.Abs(userId.GetHashCode()) % totalShards;
    }



    /// <summary>
    /// SocialGraph 业务级单元测试
    /// </summary>
    [TestFixture]
    public class SocialGraphTests
    {
        /// <summary>
        /// Defines the test method FollowAndUnfollow_BasicScenario_Works.
        /// </summary>
        [Test]
        public void FollowAndUnfollow_BasicScenario_Works()
        {
            var userA = new SocialUser { UserId = 1, UserName = "Alice" };
            var userB = new SocialUser { UserId = 2, UserName = "Bob" };
            var graph = new SocialGraph();

            graph.Follow(userA, userB);
            Assert.That(graph.IsFollowing(userA, userB), Is.True);
            Assert.That(graph.IsFollower(userA, userB), Is.True);

            graph.Unfollow(userA, userB);
            Assert.That(graph.IsFollowing(userA, userB), Is.False);
            Assert.That(graph.IsFollower(userA, userB), Is.False);
        }

        /// <summary>
        /// Defines the test method GetFollowersAndFollowing_Pagination_Sorted.
        /// </summary>
        [Test]
        public void GetFollowersAndFollowing_Pagination_Sorted()
        {
            var u1 = new SocialUser { UserId = 1, UserName = "Anna" };
            var u2 = new SocialUser { UserId = 2, UserName = "Bella" };
            var u3 = new SocialUser { UserId = 3, UserName = "Chris" };
            var graph = new SocialGraph();

            graph.Follow(u1, u2);
            graph.Follow(u1, u3);

            var followings = graph.GetFollowing(u1).ToList();
            Assert.That(followings[0].UserName, Is.EqualTo("Bella"));
            Assert.That(followings[1].UserName, Is.EqualTo("Chris"));

            var followers = graph.GetFollowers(u2).ToList();
            Assert.That(followers.Count, Is.EqualTo(1));
            Assert.That(followers[0].UserName, Is.EqualTo("Anna"));

            // 分页只取一个
            var paged = graph.GetFollowing(u1, skip: 1, take: 1).ToList();
            Assert.That(paged.Count, Is.EqualTo(1));
            Assert.That(paged[0].UserName, Is.EqualTo("Chris"));
        }

        /// <summary>
        /// Defines the test method Sharding_SimulatesDistributedDeployment.
        /// </summary>
        [Test]
        public void Sharding_SimulatesDistributedDeployment()
        {
            int totalShards = 2;
            var userId1 = 1001;
            var userId2 = 1002;
            int shard1 = SocialGraph.GetShardIndex(userId1, totalShards);
            int shard2 = SocialGraph.GetShardIndex(userId2, totalShards);
            Assert.That(shard1, Is.GreaterThanOrEqualTo(0).And.LessThan(totalShards));
            Assert.That(shard2, Is.GreaterThanOrEqualTo(0).And.LessThan(totalShards));
        }
    }

}
