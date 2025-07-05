using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Noob.Algorithms
{
    /// <summary>
    /// 分布式服务分片扩容自动化核心类。
    /// 支持平台级倍增扩容、分片迁移任务调度、节点元数据一致性同步等场景。
    /// </summary>
    public class DistributedShardExpansionService
    {
        /// <summary>
        /// 当前总分片数（需为2的幂次，扩容时倍增）。
        /// </summary>
        private int _currentShardCount;

        /// <summary>
        /// 分片到节点映射表，key为分片编号，value为物理节点ID。
        /// </summary>
        private Dictionary<int, string> _shardToNodeMap;

        /// <summary>
        /// 节点注册表，节点ID集合。
        /// </summary>
        private HashSet<string> _nodeSet;

        /// <summary>
        /// 构造函数，初始化分片映射和节点注册表。
        /// </summary>
        /// <param name="initialShardCount">初始分片数（建议为2的幂）</param>
        /// <param name="initialNodes">初始节点ID列表</param>
        public DistributedShardExpansionService(int initialShardCount, IEnumerable<string> initialNodes)
        {
            if (initialShardCount <= 0 || (initialShardCount & (initialShardCount - 1)) != 0)
                throw new ArgumentException("初始分片数必须为2的幂次且大于0", nameof(initialShardCount));
            _currentShardCount = initialShardCount;
            _nodeSet = new HashSet<string>(initialNodes ?? throw new ArgumentNullException(nameof(initialNodes)));
            _shardToNodeMap = new Dictionary<int, string>();
            AssignShardsToNodes();
        }

        /// <summary>
        /// 自动倍增扩容分片，并调度分片迁移任务。
        /// </summary>
        /// <param name="newNodes">扩容后新加入的节点ID列表</param>
        /// <returns>分片迁移计划任务列表</returns>
        public List<ShardMigrationTask> ExpandShards(IEnumerable<string> newNodes)
        {
            // 1. 合并新节点
            foreach (var node in newNodes)
                _nodeSet.Add(node);

            if (_nodeSet.Count == 0)
                throw new InvalidOperationException("没有可用节点分配分片");


            int newShardCount = _currentShardCount * 2;
            var oldToNewShardMap = new Dictionary<int, int>();
            var migrationTasks = new List<ShardMigrationTask>();

           
            // 2. 计算新分片分配（核心逻辑：2N扩容只需迁移一半分片）
            for (int shard = 0; shard < newShardCount; shard++)
            {
                // 原来的一半分片直接复用，另一半需要迁移
                int oldShard = shard % _currentShardCount;
                oldToNewShardMap[shard] = oldShard;

                string assignedNode = SelectNodeForShard(shard, _nodeSet);
                if (shard < _currentShardCount)
                {
                    // 旧分片：原地不动
                    _shardToNodeMap[shard] = assignedNode;
                }
                else
                {
                    // 新分片：数据需从对应旧分片迁移
                    _shardToNodeMap[shard] = assignedNode;
                    migrationTasks.Add(new ShardMigrationTask
                    {
                        OldShardId = oldShard,
                        NewShardId = shard,
                        SourceNodeId = _shardToNodeMap[oldShard],
                        TargetNodeId = assignedNode
                    });
                }
            }
            _currentShardCount = newShardCount;
            return migrationTasks;
        }

        /// <summary>
        /// 获取指定key应落在哪个分片与节点。
        /// </summary>
        /// <param name="key">业务key（如UserId/OrderId）</param>
        /// <returns>分片编号与节点ID</returns>
        public (int shardId, string nodeId) LocateShardAndNode(long key)
        {
            int shardId = (int)(key & (_currentShardCount - 1));
            string nodeId = _shardToNodeMap[shardId];
            return (shardId, nodeId);
        }

        /// <summary>
        /// 节点下线，分片自动重分配。
        /// </summary>
        /// <param name="nodeId">下线节点ID</param>
        public void RemoveNode(string nodeId)
        {
            if (!_nodeSet.Remove(nodeId))
                return;

            if (_nodeSet.Count == 0)
            {
                _shardToNodeMap.Clear();
                return;
            }
            // 将该节点上的分片均匀迁移到剩余节点
            foreach (var shard in _shardToNodeMap.Keys)
            {
                if (_shardToNodeMap[shard] == nodeId)
                {
                    string targetNode = SelectNodeForShard(shard, _nodeSet);
                    _shardToNodeMap[shard] = targetNode;
                }
            }
        }

        /// <summary>
        /// 当前所有分片的节点分配情况。
        /// </summary>
        /// <returns>字典（分片编号 -> 节点ID）</returns>
        public Dictionary<int, string> GetShardDistribution()
        {
            return new Dictionary<int, string>(_shardToNodeMap);
        }

        /// <summary>
        /// 均匀分配分片到节点。
        /// </summary>
        private void AssignShardsToNodes()
        {
            int i = 0;
            foreach (var shard in EnumerableRange(0, _currentShardCount))
            {
                string nodeId = SelectNodeForShard(shard, _nodeSet);
                _shardToNodeMap[shard] = nodeId;
                i++;
            }
        }

        /// <summary>
        /// 分片到节点分配策略（可按hash或轮询均匀分布）。
        /// </summary>
        /// <param name="shardId">分片编号</param>
        /// <param name="availableNodes">节点集合</param>
        /// <returns>选中的节点ID</returns>
        private static string SelectNodeForShard(int shardId, HashSet<string> availableNodes)
        {
            var nodes = new List<string>(availableNodes);
            if (nodes.Count == 0)
                throw new InvalidOperationException("没有可用节点分配分片");
            int idx = shardId % nodes.Count;
            return nodes[idx];
        }

        /// <summary>
        /// 辅助：生成整数区间序列。
        /// </summary>
        private static IEnumerable<int> EnumerableRange(int start, int count)
        {
            for (int i = 0; i < count; i++) yield return start + i;
        }
    }

    /// <summary>
    /// 分片迁移任务实体。
    /// </summary>
    public class ShardMigrationTask
    {
        /// <summary>旧分片编号</summary>
        public int OldShardId { get; set; }
        /// <summary>新分片编号</summary>
        public int NewShardId { get; set; }
        /// <summary>源节点ID</summary>
        public string SourceNodeId { get; set; }
        /// <summary>目标节点ID</summary>
        public string TargetNodeId { get; set; }
    }


    /// <summary>
    /// DistributedShardExpansionService 单元测试（平台工程级/NUnit+XML注释+Assert.That）
    /// </summary>
    [TestFixture]
    public class DistributedShardExpansionServiceTests
    {
        /// <summary>
        /// 验证初始化分片和节点映射是否正确。
        /// </summary>
        [Test]
        public void InitShardAndNodeAssignment_CorrectlyAssigned()
        {
            var nodes = new List<string> { "node1", "node2" };
            var svc = new DistributedShardExpansionService(4, nodes);

            var dist = svc.GetShardDistribution();
            Assert.That(dist.Count, Is.EqualTo(4));
            // 轮询分配，node1:0,2 node2:1,3
            Assert.That(dist[0], Is.EqualTo("node1"));
            Assert.That(dist[1], Is.EqualTo("node2"));
            Assert.That(dist[2], Is.EqualTo("node1"));
            Assert.That(dist[3], Is.EqualTo("node2"));
        }

        /// <summary>
        /// 验证倍增扩容后，分片数和迁移任务数量是否正确。
        /// </summary>
        [Test]
        public void ExpandShards_DoubleShardCount_OnlyHalfMigrate()
        {
            var svc = new DistributedShardExpansionService(4, new List<string> { "n1", "n2" });
            var migrationTasks = svc.ExpandShards(new List<string> { "n3" });
            // 分片数从4变8，4个迁移任务
            Assert.That(migrationTasks.Count, Is.EqualTo(4));
            var dist = svc.GetShardDistribution();
            Assert.That(dist.Count, Is.EqualTo(8));
            // 校验迁移任务新旧分片关系
            foreach (var task in migrationTasks)
            {
                Assert.That(task.NewShardId, Is.GreaterThanOrEqualTo(4));
                Assert.That(task.OldShardId, Is.EqualTo(task.NewShardId % 4));
            }
        }

        /// <summary>
        /// 验证定位分片和节点是否准确。
        /// </summary>
        [Test]
        public void LocateShardAndNode_CorrectShardAndNode()
        {
            var nodes = new List<string> { "A", "B", "C", "D" };
            var svc = new DistributedShardExpansionService(8, nodes);
            var (shard, node) = svc.LocateShardAndNode(12345L);
            // shard = 12345 & 7 = 1
            Assert.That(shard, Is.EqualTo(1));
            Assert.That(nodes, Does.Contain(node));
        }

        /// <summary>
        /// 验证节点下线后分片均匀重新分配。
        /// </summary>
        [Test]
        public void RemoveNode_ReassignShards_Correctly()
        {
            var nodes = new List<string> { "A", "B", "C" };
            var svc = new DistributedShardExpansionService(4, nodes);
            svc.RemoveNode("B");
            var dist = svc.GetShardDistribution();
            Assert.That(dist.Values, Does.Not.Contain("B"));
            // 分片数仍为4，分布在A和C
            foreach (var node in dist.Values)
                Assert.That(node == "A" || node == "C");
        }

        /// <summary>
        /// 验证没有可用节点时扩容抛异常。
        /// </summary>
        [Test]
        public void NoAvailableNode_Throws()
        {
            var svc = new DistributedShardExpansionService(2, new List<string> { "X" });
            svc.RemoveNode("X");
            var ex = Assert.Throws<InvalidOperationException>(() => svc.ExpandShards(new List<string>()));
            Assert.That(ex.Message, Does.Contain("没有可用节点分配分片"));
        }

        /// <summary>
        /// 非2的幂次初始化抛异常。
        /// </summary>
        [Test]
        public void InitWithNonPowerOfTwo_ThrowsArgumentException()
        {
            Assert.That(() => new DistributedShardExpansionService(3, new List<string> { "A" }),
                Throws.ArgumentException);
        }

        /// <summary>
        /// 验证扩容时新节点被分配到分片。
        /// </summary>
        [Test]
        public void ExpandShards_NewNodeAssigned()
        {
            var svc = new DistributedShardExpansionService(2, new List<string> { "A" });
            var migrationTasks = svc.ExpandShards(new List<string> { "B" });
            var dist = svc.GetShardDistribution();
            Assert.That(dist.Values, Does.Contain("B"));
            Assert.That(dist.Values, Does.Contain("A"));
        }

        /// <summary>
        /// 连续多次扩容，分片数每次倍增。
        /// </summary>
        [Test]
        public void MultipleExpandShards_DoubleShardCountEveryTime()
        {
            var svc = new DistributedShardExpansionService(2, new List<string> { "N1", "N2" });
            svc.ExpandShards(new List<string> { "N3" });
            var dist1 = svc.GetShardDistribution();
            Assert.That(dist1.Count, Is.EqualTo(4));
            svc.ExpandShards(new List<string> { "N4" });
            var dist2 = svc.GetShardDistribution();
            Assert.That(dist2.Count, Is.EqualTo(8));
        }
    }

}
