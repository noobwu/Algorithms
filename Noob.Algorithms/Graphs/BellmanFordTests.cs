using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Noob.Algorithms.Graphs
{
    /// <summary>
    /// Bellman-Ford/多目标/全源/SPFA优选路径算法平台级实现
    /// </summary>
    public static class BellmanFordPathfinder
    {
        /// <summary>
        /// Bellman-Ford 单源最短路径（支持负权边/检测负权环/支持动态权重委托）
        /// </summary>
        public static (Dictionary<int, double> dist, Dictionary<int, int> prev, bool hasNegativeCycle)
            FindShortestPaths(
                GraphNode start,
                Dictionary<int, GraphNode> graph,
                Func<GraphEdge, double> getWeight = null)
        {
            getWeight ??= e => e.Weight;
            var dist = new Dictionary<int, double>();
            var prev = new Dictionary<int, int>();
            int n = graph.Count;
            foreach (var node in graph.Values)
                dist[node.Id] = double.PositiveInfinity;
            dist[start.Id] = 0;

            // n-1 轮松弛
            for (int k = 1; k < n; k++)
            {
                bool updated = false;
                foreach (var u in graph.Values)
                    foreach (var edge in u.Neighbors)
                    {
                        var v = edge.TargetNodeId;
                        double w = getWeight(edge);
                        if (dist[u.Id] + w < dist[v])
                        {
                            dist[v] = dist[u.Id] + w;
                            prev[v] = u.Id;
                            updated = true;
                        }
                    }
                if (!updated) break;
            }

            // 检查负权环
            bool hasNegativeCycle = false;
            foreach (var u in graph.Values)
                foreach (var edge in u.Neighbors)
                {
                    var v = edge.TargetNodeId;
                    if (dist[u.Id] + getWeight(edge) < dist[v])
                    {
                        hasNegativeCycle = true;
                        break;
                    }
                }
            return (dist, prev, hasNegativeCycle);
        }

        /// <summary>
        /// 全源最短路径（所有点作为起点，分别Bellman-Ford，适合业务OD批量分析）
        /// </summary>
        /// <returns>二维距离字典：outer为起点id，inner为终点id到距离</returns>
        public static Dictionary<int, Dictionary<int, double>> AllPairsShortestPaths(
            Dictionary<int, GraphNode> graph,
            Func<GraphEdge, double> getWeight = null)
        {
            var result = new Dictionary<int, Dictionary<int, double>>();
            foreach (var node in graph.Values)
            {
                var (dist, _, _) = FindShortestPaths(node, graph, getWeight);
                result[node.Id] = dist;
            }
            return result;
        }

        /// <summary>
        /// 多目标最短路KNN（如“最近K个加油站”，支持负权，断路/动态因子）
        /// </summary>
        /// <param name="start">起点</param>
        /// <param name="targets">目标节点集合</param>
        /// <param name="graph">全量节点</param>
        /// <param name="k">返回最近K个</param>
        /// <param name="getWeight">权重委托</param>
        /// <returns>Top K 目标、路径、距离</returns>
        public static List<(AttributeNode Target, List<GraphNode> Path, double Dist)> FindKNearestTargets(
            AttributeNode start,
            IEnumerable<AttributeNode> targets,
            Dictionary<int, GraphNode> graph,
            int k,
            Func<GraphEdge, double> getWeight = null)
        {
            var (dist, prev, hasNeg) = FindShortestPaths(start, graph, getWeight);
            if (hasNeg)
                throw new InvalidOperationException("存在负权环，最短路径不可用！");
            var result = targets
                .Select(t => (Target: t, Dist: dist.ContainsKey(t.Id) ? dist[t.Id] : double.PositiveInfinity))
                .Where(x => x.Dist < double.PositiveInfinity)
                .OrderBy(x => x.Dist)
                .Take(k)
                .Select(x => (x.Target, ReconstructPath(x.Target.Id, prev, graph), x.Dist))
                .ToList();
            return result;
        }

        /// <summary>
        /// 路径恢复，适配环/无通路/动态结构
        /// </summary>
        public static List<GraphNode> ReconstructPath(
            int endId,
            Dictionary<int, int> prev,
            Dictionary<int, GraphNode> graph)
        {
            var path = new List<GraphNode>();
            int current = endId;
            var visited = new HashSet<int>();
            while (prev.ContainsKey(current))
            {
                path.Add(graph[current]);
                if (!visited.Add(current)) break;
                current = prev[current];
            }
            if (graph.ContainsKey(current)) path.Add(graph[current]);
            path.Reverse();
            return path.Count > 1 ? path : new List<GraphNode>();
        }

        /// <summary>
        /// SPFA（Shortest Path Faster Algorithm）队列优化Bellman-Ford，适合大规模稀疏图
        /// </summary>
        /// <param name="start">起点节点</param>
        /// <param name="graph">全量节点</param>
        /// <param name="getWeight">权重委托</param>
        /// <returns>
        /// (最短距离表dist, 前驱表prev, 是否有负权回路)
        /// </returns>
        public static (Dictionary<int, double> dist, Dictionary<int, int> prev, bool hasNegativeCycle)
            FindShortestPathsSpfa(
                GraphNode start,
                Dictionary<int, GraphNode> graph,
                Func<GraphEdge, double> getWeight = null)
        {
            getWeight ??= e => e.Weight;
            var dist = new Dictionary<int, double>();
            var prev = new Dictionary<int, int>();
            var inQueue = new Dictionary<int, bool>();
            var count = new Dictionary<int, int>();
            var queue = new Queue<GraphNode>();
            foreach (var node in graph.Values)
            {
                dist[node.Id] = double.PositiveInfinity;
                inQueue[node.Id] = false;
                count[node.Id] = 0;
            }
            dist[start.Id] = 0;
            queue.Enqueue(start);
            inQueue[start.Id] = true;
            bool hasNegativeCycle = false;

            while (queue.Count > 0)
            {
                var u = queue.Dequeue();
                inQueue[u.Id] = false;
                foreach (var edge in u.Neighbors)
                {
                    var v = edge.TargetNodeId;
                    double w = getWeight(edge);
                    if (dist[u.Id] + w < dist[v])
                    {
                        dist[v] = dist[u.Id] + w;
                        prev[v] = u.Id;
                        if (!inQueue[v])
                        {
                            queue.Enqueue(graph[v]);
                            inQueue[v] = true;
                            count[v]++;
                            if (count[v] > graph.Count)
                            {
                                hasNegativeCycle = true;
                                break;
                            }
                        }
                    }
                }
                if (hasNegativeCycle) break;
            }
            return (dist, prev, hasNegativeCycle);
        }
    }

    /// <summary>
    /// BellmanFordPathfinder 平台级全场景单元测试
    /// </summary>
    [TestFixture]
    public class BellmanFordPathfinderTests
    {
        /// <summary>
        /// 测试用带属性节点
        /// </summary>
        public class TestNode : AttributeNode { }
        /// <summary>
        /// 测试用带属性边
        /// </summary>
        public class TestEdge : AttributeEdge { }

        /// <summary>
        /// 单源最短路基础链测试，验证路径和距离正确
        /// </summary>
        [Test]
        public void FindShortestPaths_SimpleChain_ShouldReturnCorrectDistances()
        {
            var nodes = new Dictionary<int, GraphNode>
            {
                [0] = new TestNode { Id = 0, Category = "起点" },
                [1] = new TestNode { Id = 1 },
                [2] = new TestNode { Id = 2, Category = "终点" }
            };
            nodes[0].Neighbors.Add(new TestEdge { TargetNodeId = 1, Weight = 2 });
            nodes[1].Neighbors.Add(new TestEdge { TargetNodeId = 2, Weight = 5 });

            var (dist, prev, neg) = BellmanFordPathfinder.FindShortestPaths(nodes[0], nodes);
            Assert.That(neg, Is.False);
            Assert.That(dist[2], Is.EqualTo(7));
            var path = BellmanFordPathfinder.ReconstructPath(2, prev, nodes);
            Assert.That(path.Select(n => n.Id), Is.EqualTo(new[] { 0, 1, 2 }));
        }

        /// <summary>
        /// 负权边测试，正确检测负环且路径不可用
        /// </summary>
        [Test]
        public void FindShortestPaths_NegativeCycle_ShouldDetectNegativeCycle()
        {
            var nodes = new Dictionary<int, GraphNode>
            {
                [0] = new TestNode { Id = 0 },
                [1] = new TestNode { Id = 1 },
                [2] = new TestNode { Id = 2 }
            };
            nodes[0].Neighbors.Add(new TestEdge { TargetNodeId = 1, Weight = 1 });
            nodes[1].Neighbors.Add(new TestEdge { TargetNodeId = 2, Weight = -2 });
            nodes[2].Neighbors.Add(new TestEdge { TargetNodeId = 0, Weight = -2 });

            var (dist, prev, neg) = BellmanFordPathfinder.FindShortestPaths(nodes[0], nodes);
            Assert.That(neg, Is.True);
        }

        /// <summary>
        /// SPFA优化在大环无负环图中结果等价
        /// </summary>
        [Test]
        public void FindShortestPathsSpfa_LargeRingGraph_ShouldReturnSameAsBellmanFord()
        {
            int N = 12;
            var nodes = new Dictionary<int, GraphNode>();
            for (int i = 0; i < N; i++)
                nodes[i] = new TestNode { Id = i };
            for (int i = 0; i < N; i++)
                nodes[i].Neighbors.Add(new TestEdge { TargetNodeId = (i + 1) % N, Weight = 3 });

            var (dist1, prev1, neg1) = BellmanFordPathfinder.FindShortestPaths(nodes[0], nodes);
            var (dist2, prev2, neg2) = BellmanFordPathfinder.FindShortestPathsSpfa(nodes[0], nodes);

            Assert.That(neg1, Is.False);
            Assert.That(neg2, Is.False);
            for (int i = 0; i < N; i++)
                Assert.That(dist1[i], Is.EqualTo(dist2[i]));
        }

        /// <summary>
        /// 全源最短路：每对点的距离应对称一致
        /// </summary>
        [Test]
        public void AllPairsShortestPaths_SmallGraph_ShouldReturnValidOD()
        {
            var nodes = new Dictionary<int, GraphNode>();
            for (int i = 0; i < 4; i++) nodes[i] = new TestNode { Id = i };
            nodes[0].Neighbors.Add(new TestEdge { TargetNodeId = 1, Weight = 2 });
            nodes[1].Neighbors.Add(new TestEdge { TargetNodeId = 2, Weight = 4 });
            nodes[2].Neighbors.Add(new TestEdge { TargetNodeId = 3, Weight = 1 });
            nodes[3].Neighbors.Add(new TestEdge { TargetNodeId = 0, Weight = 3 });

            var allPairs = BellmanFordPathfinder.AllPairsShortestPaths(nodes);
            Assert.That(allPairs[0][3], Is.EqualTo(7)); // 0->1->2->3
            Assert.That(allPairs[3][2], Is.EqualTo(9)); // 3->0->1->2 (实际)
        }

        /// <summary>
        /// 多目标KNN查找：加权动态、断路、只返回最近K个
        /// </summary>
        [Test]
        public void FindKNearestTargets_KnnScenario_ShouldReturnTopKTargets()
        {
            var nodes = new Dictionary<int, GraphNode>();
            var targets = new List<TestNode>();
            nodes[0] = new TestNode { Id = 0, Category = "起点" };
            for (int i = 1; i <= 5; i++)
            {
                var n = new TestNode { Id = i, Category = "加油站" };
                nodes[i] = n;
                targets.Add(n);
            }
            // 起点到所有目标，且部分目标设为断路
            for (int i = 1; i <= 5; i++)
                nodes[0].Neighbors.Add(new TestEdge { TargetNodeId = i, Weight = i, IsOpen = (i != 3) });

            // 距离K最近，且断路目标不计入
            var result = BellmanFordPathfinder.FindKNearestTargets(
                (TestNode)nodes[0], targets, nodes, 2,
                e => (e as TestEdge)?.IsOpen == false ? double.PositiveInfinity : e.Weight);

            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result[0].Dist, Is.EqualTo(1));
            Assert.That(result[1].Dist, Is.EqualTo(2));
            Assert.That(result.All(x => x.Target.Category == "加油站"));
            Assert.That(result.All(x => x.Path.First().Id == 0));
        }
    }

}
