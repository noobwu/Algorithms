using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Noob.Algorithms.Graphs
{

    /// <summary>
    /// 图节点，支持任意业务属性扩展
    /// </summary>
    public class GraphNode
    {
        /// <summary>节点唯一ID</summary>
        public int Id { get; set; }

        /// <summary>邻接边集合</summary>
        public List<GraphEdge> Neighbors { get; } = new List<GraphEdge>();
    }

    /// <summary>
    /// 图边，支持权重（距离/耗时/费用等）
    /// </summary>
    public class GraphEdge
    {
        /// <summary>目标节点ID</summary>
        public int TargetNodeId { get; set; }

        /// <summary>边权重（必须非负）</summary>
        public double Weight { get; set; }
    }

    /// <summary>
    /// 支持复杂业务属性的节点（如坐标、类型、动态属性）
    /// </summary>
    public class AttributeNode : GraphNode
    {
        /// <summary>节点类型，如“加油站”、“路口”</summary>
        public string Category { get; set; }
        /// <summary>地理坐标（可扩展为三维）</summary>
        public double Longitude { get; set; }
        public double Latitude { get; set; }
        // 更多业务属性可扩展
    }

    /// <summary>
    /// 支持动态权重/属性的边
    /// </summary>
    public class AttributeEdge : GraphEdge
    {
        /// <summary>实时权重调整（如拥堵/封路）</summary>
        public bool IsOpen { get; set; } = true;

        /// <summary>拥堵度</summary>
        public double Congestion { get; set; } = 1.0;
        // 可扩展更多属性
    }

    /// <summary>
    /// Dijkstra最短路径算法（平台可集成/易扩展）
    /// </summary>
    public class DijkstraPathfinder
    {
        /// <summary>
        /// 单源到所有节点的最短路径（基础）
        /// </summary>
        public static (Dictionary<int, double> dist, Dictionary<int, int> prev) FindShortestPaths(
            GraphNode start,
            Dictionary<int, GraphNode> graph,
            Func<GraphEdge, double> getWeight = null)
        {
            getWeight ??= e => e.Weight;
            var dist = new Dictionary<int, double>();
            var prev = new Dictionary<int, int>();
            var visited = new HashSet<int>();
            var pq = new PriorityQueue<GraphNode, double>();

            foreach (var node in graph.Values)
                dist[node.Id] = double.PositiveInfinity;
            dist[start.Id] = 0;
            pq.Enqueue(start, 0);

            while (pq.Count > 0)
            {
                var current = pq.Dequeue();
                if (!visited.Add(current.Id)) continue;

                foreach (var edge in current.Neighbors)
                {
                    var neighbor = graph[edge.TargetNodeId];
                    if (!visited.Contains(neighbor.Id))
                    {
                        double alt = dist[current.Id] + getWeight(edge);
                        if (alt < dist[neighbor.Id])
                        {
                            dist[neighbor.Id] = alt;
                            prev[neighbor.Id] = current.Id;
                            pq.Enqueue(neighbor, alt);
                        }
                    }
                }
            }
            return (dist, prev);
        }

        /// <summary>
        /// 全源最短路径：所有节点作为起点，分别计算到其它所有节点的最短路径。
        /// </summary>
        /// <returns>二维字典，outer为起点id，inner为终点id到距离</returns>
        public static Dictionary<int, Dictionary<int, double>> AllPairsShortestPaths(
            Dictionary<int, GraphNode> graph,
            Func<GraphEdge, double> getWeight = null)
        {
            var result = new Dictionary<int, Dictionary<int, double>>();
            foreach (var node in graph.Values)
            {
                var (dist, _) = FindShortestPaths(node, graph, getWeight);
                result[node.Id] = dist;
            }
            return result;
        }

        /// <summary>
        /// 多目标KNN最短路径：从起点出发，找最近的K个指定类别目标节点
        /// </summary>
        /// <param name="start">起点</param>
        /// <param name="targets">目标节点集合（如所有加油站）</param>
        /// <param name="graph">全量节点</param>
        /// <param name="k">取最近K个</param>
        /// <param name="getWeight">边权重获取方法</param>
        /// <returns>Top K 最近目标、最短路径、距离</returns>
        public static List<(AttributeNode Target, List<GraphNode> Path, double Dist)> FindKNearestTargets(
            AttributeNode start,
            IEnumerable<AttributeNode> targets,
            Dictionary<int, GraphNode> graph,
            int k,
            Func<GraphEdge, double> getWeight = null)
        {
            getWeight ??= e => e.Weight;
            var (dist, prev) = FindShortestPaths(start, graph, getWeight);
            var candidateList = targets
                .Select(t => (Target: t, Dist: dist.ContainsKey(t.Id) ? dist[t.Id] : double.PositiveInfinity))
                .Where(x => x.Dist < double.PositiveInfinity)
                .OrderBy(x => x.Dist)
                .Take(k)
                .ToList();

            var results = new List<(AttributeNode, List<GraphNode>, double)>();
            foreach (var (target, distance) in candidateList)
            {
                var path = ReconstructPath(target.Id, prev, graph);
                results.Add((target, path, distance));
            }
            return results;
        }

        /// <summary>
        /// 路径恢复，适配环/孤立/无通路场景
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
                if (!visited.Add(current)) break;// 防止环
                current = prev[current];
            }
            if (graph.ContainsKey(current)) path.Add(graph[current]);
            path.Reverse();
            return path.Count > 1 ? path : new List<GraphNode>();
        }
    }

    /// <summary>
    /// DijkstraPathfinder 平台化全场景单元测试
    /// </summary>
    [TestFixture]
    public class DijkstraPathfinderTests
    {
        /// <summary>
        /// 测试用带属性节点（支持空间与类型）
        /// </summary>
        public class TestNode : AttributeNode { }

        /// <summary>
        /// 测试用带属性边（动态权重/禁行/拥堵）
        /// </summary>
        public class TestEdge : AttributeEdge { }

        /// <summary>
        /// 简单三节点无障碍最短路，校验基础路径正确性
        /// </summary>
        [Test]
        public void FindShortestPaths_SimpleChain_ShouldReturnCorrectDistances()
        {
            // Arrange
            var nodes = new Dictionary<int, GraphNode>
            {
                [0] = new TestNode { Id = 0, Category = "起点" },
                [1] = new TestNode { Id = 1, Category = "路口" },
                [2] = new TestNode { Id = 2, Category = "终点" }
            };
            nodes[0].Neighbors.Add(new TestEdge { TargetNodeId = 1, Weight = 5 });
            nodes[1].Neighbors.Add(new TestEdge { TargetNodeId = 2, Weight = 2 });

            // Act
            var (dist, prev) = DijkstraPathfinder.FindShortestPaths(nodes[0], nodes);

            // Assert
            Assert.That(dist[2], Is.EqualTo(7));
            var path = DijkstraPathfinder.ReconstructPath(2, prev, nodes);
            Assert.That(path.Select(n => n.Id), Is.EqualTo(new[] { 0, 1, 2 }));
        }

        /// <summary>
        /// 全源最短路径：10节点环形图，每个点到其余点距离可验证
        /// </summary>
        [Test]
        public void AllPairsShortestPaths_RingGraph_ShouldReturnAllPairDistances()
        {
            // Arrange
            int N = 10;
            var nodes = new Dictionary<int, GraphNode>();
            for (int i = 0; i < N; i++)
                nodes[i] = new TestNode { Id = i, Category = "路口" };
            for (int i = 0; i < N; i++)
            {
                nodes[i].Neighbors.Add(new TestEdge { TargetNodeId = (i + 1) % N, Weight = 1 });
                nodes[i].Neighbors.Add(new TestEdge { TargetNodeId = (i + N - 1) % N, Weight = 1 });
            }

            // Act
            var allPairDist = DijkstraPathfinder.AllPairsShortestPaths(nodes);

            // Assert
            for (int i = 0; i < N; i++)
                for (int j = 0; j < N; j++)
                {
                    int exp = Math.Min((j - i + N) % N, (i - j + N) % N);
                    Assert.That(allPairDist[i][j], Is.EqualTo(exp));
                }
        }

        /// <summary>
        /// 多目标KNN：查找最近3个加油站，含拥堵与断路处理
        /// </summary>
        [Test]
        public void FindKNearestTargets_KnnScenario_ShouldReturnValidTopK()
        {
            // Arrange
            var nodes = new Dictionary<int, GraphNode>();
            var stations = new List<TestNode>();
            nodes[0] = new TestNode { Id = 0, Category = "起点", Longitude = 0, Latitude = 0 };
            // 构建5个加油站和5个普通路口
            for (int i = 1; i <= 5; i++)
            {
                var st = new TestNode { Id = i, Category = "加油站", Longitude = i, Latitude = 0 };
                nodes[i] = st;
                stations.Add(st);
            }
            for (int i = 6; i < 10; i++)
                nodes[i] = new TestNode { Id = i, Category = "路口", Longitude = i, Latitude = 0 };

            // 起点连接到所有加油站及部分路口
            for (int i = 1; i <= 5; i++)
                nodes[0].Neighbors.Add(new TestEdge { TargetNodeId = i, Weight = i }); // 1~5 距离
                                                                                       // 加油站之间增加高拥堵边（仅部分有效）
            nodes[1].Neighbors.Add(new TestEdge { TargetNodeId = 2, Weight = 1, Congestion = 3.0, IsOpen = true });
            // 禁行边
            nodes[1].Neighbors.Add(new TestEdge { TargetNodeId = 3, Weight = 1, IsOpen = false });

            // Act
            var result = DijkstraPathfinder.FindKNearestTargets(
                (TestNode)nodes[0], stations, nodes, 3,
                e => (e as TestEdge)?.IsOpen == false ? double.MaxValue : e.Weight * ((e as TestEdge)?.Congestion ?? 1.0)
            );

            // Assert
            Assert.That(result.Count, Is.EqualTo(3));
            // 距离递增，且均为加油站
            Assert.That(result[0].Dist, Is.LessThan(result[1].Dist));
            Assert.That(result[2].Dist, Is.GreaterThanOrEqualTo(result[1].Dist));
            Assert.That(result.All(r => r.Target.Category == "加油站"));
            // 路径合法，未包含禁行边
            foreach (var r in result)
            {
                var steps = r.Path.Zip(r.Path.Skip(1), (from, to) =>
                    ((TestNode)from).Neighbors.OfType<TestEdge>().FirstOrDefault(e => e.TargetNodeId == to.Id));
                Assert.That(steps.All(e => e != null && e.IsOpen), "所有路径均应避开断路");
            }
        }

        /// <summary>
        /// 验证不可达（断路），路径应为空，距离为无穷
        /// </summary>
        [Test]
        public void FindShortestPaths_AllBlocked_ShouldReturnInfinityAndEmptyPath()
        {
            // Arrange
            var nodes = new Dictionary<int, GraphNode>
            {
                [0] = new TestNode { Id = 0, Category = "起点" },
                [1] = new TestNode { Id = 1, Category = "加油站" }
            };
            nodes[0].Neighbors.Add(new TestEdge { TargetNodeId = 1, Weight = 1, IsOpen = false }); // 封路

            // Act
            var (dist, prev) = DijkstraPathfinder.FindShortestPaths(
                nodes[0], nodes,
                e => (e as TestEdge)?.IsOpen == false ? double.PositiveInfinity : e.Weight
            );
            var path = DijkstraPathfinder.ReconstructPath(1, prev, nodes);

            // Assert
            Assert.That(dist[1], Is.EqualTo(double.PositiveInfinity));
            Assert.That(path, Is.Empty);
        }
    }

}
