using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Noob.Algorithms.Graphs
{
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// 节点（如路口、地图点）
    /// </summary>
    public class Node
    {
        /// <summary>唯一节点ID</summary>
        public int Id { get; set; }

        /// <summary>节点关联的边（目标节点ID, 权重）</summary>
        public List<Edge> Neighbors { get; } = new List<Edge>();
    }

    /// <summary>
    /// 图边，表示从当前节点到邻居的连接关系
    /// </summary>
    public class Edge
    {
        /// <summary>目标节点ID</summary>
        public int TargetNodeId { get; set; }

        /// <summary>边权重（距离/耗时等）</summary>
        public double Weight { get; set; }
    }
    /// <summary>
    /// 扩展的空间节点，支持地理坐标、业务属性
    /// </summary>
    public class SpatialNode : Node
    {
        /// <summary>
        /// 经度
        /// </summary>
        public double Longitude { get; set; }
        /// <summary>
        /// 纬度
        /// </summary>
        public double Latitude { get; set; }

        /// <summary>
        /// 分类,如 "加油站", "地标", "路口"
        /// </summary>
        public string Category { get; set; }
    }

    /// <summary>
    /// 支持动态权重的道路边
    /// </summary>
    public class DynamicEdge : Edge
    {
        /// <summary>
        /// 是否畅通
        /// </summary>
        public bool IsOpen { get; set; } = true;
        /// <summary>
        /// 拥堵系数
        /// </summary>
        public double Congestion { get; set; } = 1.0;

        /// <summary>
        /// 节点备注
        /// </summary>
        public string Note { get; set; }
    }

    /// <summary>
    /// 地图数据管理，便于大规模批量查询与维护
    /// </summary>
    public class MapGraph
    {
        /// <summary>
        /// 所有节点
        /// </summary>
        public Dictionary<int, SpatialNode> Nodes { get; } = new();
        /// <summary>
        /// 添加节点
        /// </summary>
        /// <param name="node"></param>
        public void AddNode(SpatialNode node) => Nodes[node.Id] = node;

        /// <summary>
        /// 添加道路边
        /// </summary>
        /// <param name="fromId"></param>
        /// <param name="toId"></param>
        /// <param name="baseWeight"></param>
        /// <param name="congestion"></param>
        /// <param name="isOpen"></param>
        public void AddEdge(int fromId, int toId, double baseWeight, double congestion = 1.0, bool isOpen = true)
        {
            Nodes[fromId].Neighbors.Add(new DynamicEdge { TargetNodeId = toId, Weight = baseWeight, IsOpen = isOpen, Congestion = congestion });
        }
    }

    /// <summary>
    /// A*最短路径算法，支持任意图结构、可插拔启发式。
    /// </summary>
    public class AStarPathfinder
    {
        private readonly Func<Node, Node, double> _heuristic;

        /// <summary>
        /// 构造A*算法实例。
        /// </summary>
        /// <param name="heuristic">启发式函数（如欧氏距离、曼哈顿距离等）</param>
        public AStarPathfinder(Func<Node, Node, double> heuristic)
        {
            _heuristic = heuristic ?? throw new ArgumentNullException(nameof(heuristic));
        }

        /// <summary>
        /// 计算从起点到终点的最短路径。
        /// </summary>
        /// <param name="start">起点</param>
        /// <param name="goal">终点</param>
        /// <param name="graph">全量节点（ID→Node）</param>
        /// <returns>最短路径节点列表，无路径则返回空列表</returns>
        public List<Node> FindPath(Node start, Node goal, Dictionary<int, Node> graph)
        {
            var openSet = new PriorityQueue<Node, double>();
            var gScore = new Dictionary<int, double>();
            var cameFrom = new Dictionary<int, int>();

            openSet.Enqueue(start, _heuristic(start, goal));
            gScore[start.Id] = 0;

            var closedSet = new HashSet<int>();

            while (openSet.Count > 0)
            {
                Node current = openSet.Dequeue();
                if (current.Id == goal.Id)
                    return ReconstructPath(cameFrom, graph, goal.Id);

                closedSet.Add(current.Id);

                foreach (var edge in current.Neighbors)
                {
                    if (edge is DynamicEdge de && !de.IsOpen)
                        continue;

                    var neighbor = graph[edge.TargetNodeId];
                    if (closedSet.Contains(neighbor.Id))
                        continue;

                    double tentativeG = gScore[current.Id] + edge.Weight;
                    if (!gScore.TryGetValue(neighbor.Id, out var gN) || tentativeG < gN)
                    {
                        cameFrom[neighbor.Id] = current.Id;
                        gScore[neighbor.Id] = tentativeG;
                        double fScore = tentativeG + _heuristic(neighbor, goal);
                        openSet.Enqueue(neighbor, fScore);
                    }
                }
            }
            return new List<Node>();
        }

        /// <summary>
        /// 回溯构造最短路径节点列表。
        /// </summary>
        private List<Node> ReconstructPath(Dictionary<int, int> cameFrom, Dictionary<int, Node> graph, int goalId)
        {
            var path = new List<Node>();
            int currentId = goalId;
            while (cameFrom.TryGetValue(currentId, out var prevId))
            {
                path.Add(graph[currentId]);
                currentId = prevId;
            }
            path.Add(graph[currentId]);
            path.Reverse();
            return path;
        }

        /// <summary>
        /// 综合拥堵、断路等的动态权重计算
        /// </summary>
        public static double ComputeDynamicWeight(SpatialNode from, SpatialNode to, DynamicEdge edge)
        {
            if (!edge.IsOpen) return double.MaxValue;
            double dx = (from.Longitude - to.Longitude) * 111320 * Math.Cos((from.Latitude + to.Latitude) / 2 * Math.PI / 180);
            double dy = (from.Latitude - to.Latitude) * 110540;
            double dist = Math.Sqrt(dx * dx + dy * dy);
            return dist * edge.Congestion;
        }


        /// <summary>
        /// 从起点查找最近K个加油站的最短路径和距离
        /// </summary>
        /// <param name="start"></param>
        /// <param name="stations"></param>
        /// <param name="graph"></param>
        /// <param name="k"></param>
        /// <returns></returns>
        public static List<(SpatialNode Station, List<Node> Path, double Dist)> FindKNearestStations(
            SpatialNode start,
            IEnumerable<SpatialNode> stations,
            MapGraph graph,
            int k)
        {
            // 启发式可灵活更换
            var astar = new AStarPathfinder((a, b) => ComputeDynamicWeight((SpatialNode)a, (SpatialNode)b, new DynamicEdge()));
            var results = new List<(SpatialNode, List<Node>, double Dist)>();

            foreach (var station in stations)
            {
                var path = astar.FindPath(start, station, graph.Nodes.ToDictionary(x => x.Key, x => (Node)x.Value));
                double dist = 0;
                for (int i = 1; i < path.Count; i++)
                {
                    var prev = (SpatialNode)path[i - 1];
                    var next = (SpatialNode)path[i];
                    var edge = prev.Neighbors.OfType<DynamicEdge>().FirstOrDefault(e => e.TargetNodeId == next.Id);
                    dist += ComputeDynamicWeight(prev, next, edge);
                }
                if (path.Count > 1 && dist < 1e6)
                    results.Add((station, path, dist));
            }
            return results.OrderBy(r => r.Dist).Take(k).ToList();
        }

    }



    /// <summary>
    /// 平台级 AStarPathfinder 核心算法和扩展场景单元测试
    /// </summary>
    [TestFixture]
    public class AStarPathfinderTests
    {
        /// <summary>
        /// 简单欧氏距离启发式
        /// </summary>
        private static double Euclidean(Node a, Node b)
        {
            if (a is SpatialNode sa && b is SpatialNode sb)
            {
                double dx = (sa.Longitude - sb.Longitude) * 111320 * Math.Cos((sa.Latitude + sb.Latitude) / 2 * Math.PI / 180);
                double dy = (sa.Latitude - sb.Latitude) * 110540;
                return Math.Sqrt(dx * dx + dy * dy);
            }
            return 0;
        }

        /// <summary>
        /// 基础最短路径查找（小图）
        /// </summary>
        [Test]
        public void FindPath_SimpleChain_ShouldReturnShortestPath()
        {
            // Arrange
            var graph = new Dictionary<int, Node>();
            for (int i = 0; i < 5; i++) graph[i] = new Node { Id = i };
            graph[0].Neighbors.Add(new Edge { TargetNodeId = 1, Weight = 2 });
            graph[1].Neighbors.Add(new Edge { TargetNodeId = 2, Weight = 3 });
            graph[2].Neighbors.Add(new Edge { TargetNodeId = 3, Weight = 1 });
            graph[3].Neighbors.Add(new Edge { TargetNodeId = 4, Weight = 5 });
            var astar = new AStarPathfinder((a, b) => 0);

            // Act
            var path = astar.FindPath(graph[0], graph[4], graph);

            // Assert
            Assert.That(path.Select(n => n.Id), Is.EqualTo(new[] { 0, 1, 2, 3, 4 }));
        }

        /// <summary>
        /// 测试空间节点+动态权重+封路等复杂路况
        /// </summary>
        [Test]
        public void FindPath_SpatialNodeWithDynamicEdge_ShouldHandleCongestionAndBlock()
        {
            // Arrange
            var g = new MapGraph();
            g.AddNode(new SpatialNode { Id = 0, Longitude = 114.0, Latitude = 22.5, Category = "起点" });
            g.AddNode(new SpatialNode { Id = 1, Longitude = 114.01, Latitude = 22.5, Category = "路口" });
            g.AddNode(new SpatialNode { Id = 2, Longitude = 114.02, Latitude = 22.5, Category = "终点" });
            g.AddEdge(0, 1, 100, 1.0, true); // 正常路段
            g.AddEdge(1, 2, 80, 4.0, true);  // 拥堵路段
            g.AddEdge(0, 2, double.MaxValue, 1.0, false); // 封路（直达）

            var astar = new AStarPathfinder(Euclidean);

            // Act
            var path = astar.FindPath(g.Nodes[0], g.Nodes[2], g.Nodes.ToDictionary(x => x.Key, x => (Node)x.Value));
            var pathIds = path.Select(n => n.Id).ToArray();

            // Assert
            Assert.That(pathIds, Is.EqualTo(new[] { 0, 1, 2 }));
            // 验证未走封路路段
            var usedEdges = path.Zip(path.Skip(1), (from, to) =>
                ((SpatialNode)from).Neighbors.OfType<DynamicEdge>().FirstOrDefault(e => e.TargetNodeId == to.Id)).ToList();
            Assert.That(usedEdges.All(e => e != null && e.IsOpen), "所有路段应通畅");
            Assert.That(usedEdges.Any(e => e.Congestion > 2.0), "应经过拥堵路段");
        }

        /// <summary>
        /// 测试无路径场景（全部断路）
        /// </summary>
        [Test]
        public void FindPath_AllBlocked_ShouldReturnEmpty()
        {
            // Arrange
            var g = new MapGraph();
            g.AddNode(new SpatialNode { Id = 0, Longitude = 0, Latitude = 0 });
            g.AddNode(new SpatialNode { Id = 1, Longitude = 0, Latitude = 0 });
            g.AddEdge(0, 1, double.MaxValue, 1.0, false); // 封路
            var astar = new AStarPathfinder(Euclidean);

            // Act
            var path = astar.FindPath(g.Nodes[0], g.Nodes[1], g.Nodes.ToDictionary(x => x.Key, x => (Node)x.Value));

            // Assert
            Assert.That(path, Is.Empty);
        }

        /// <summary>
        /// 多目标KNN最近邻查找（批量目标）
        /// </summary>
        [Test]
        public void FindKNearestStations_BatchSearch_ShouldReturnTopKOrderedByDist()
        {
            // Arrange
            var g = new MapGraph();
            g.AddNode(new SpatialNode { Id = 0, Longitude = 114, Latitude = 22.5, Category = "起点" });
            for (int i = 1; i <= 5; i++)
                g.AddNode(new SpatialNode { Id = i, Longitude = 114 + i * 0.001, Latitude = 22.5, Category = "加油站" });
            for (int i = 1; i < 5; i++)
                g.AddEdge(i, i + 1, 100, 1.0, true);
            g.AddEdge(0, 1, 60, 1.0, true);
            g.AddEdge(0, 3, 500, 1.0, true); // 偏远加油站，距离远

            // Act
            var result = AStarPathfinder.FindKNearestStations(
                g.Nodes[0],
                g.Nodes.Values.Where(n => n.Category == "加油站"),
                g, 3);

            // Assert
            Assert.That(result.Count, Is.EqualTo(3));
            Assert.That(result[0].Station.Id, Is.EqualTo(1)); // 最近
            Assert.That(result[1].Dist, Is.GreaterThanOrEqualTo(result[0].Dist));
            Assert.That(result[2].Dist, Is.GreaterThanOrEqualTo(result[1].Dist));

            // 所有路径起点为0，终点为加油站（修复此处类型断言）
            Assert.That(result.All(r =>
                r.Path.First().Id == 0 &&
                r.Path.Last() is SpatialNode endNode &&
                endNode.Category == "加油站"
            ));
        }

        /// <summary>
        /// 大规模路网+批量目标+动态权重+断路压力测试
        /// </summary>
        [Test]
        public void FindKNearestStations_LargeScaleDynamicNetwork_ShouldAlwaysFindValidKOrLess()
        {
            // Arrange
            var g = new MapGraph();
            int nodeCount = 100;
            int stationCount = 10;
            for (int i = 0; i < nodeCount; i++)
            {
                g.AddNode(new SpatialNode { Id = i, Longitude = 113 + i * 0.0001, Latitude = 22.5 + i % 5 * 0.0002, Category = i % 10 == 0 ? "加油站" : "路口" });
            }
            // 连环路+部分高拥堵、断路
            for (int i = 0; i < nodeCount - 1; i++)
                g.AddEdge(i, i + 1, 100, i % 17 == 0 ? 5.0 : 1.0, i % 23 != 0);
            g.AddEdge(nodeCount - 1, 0, 120, 1.0, true); // 闭环

            var start = g.Nodes[1];
            var stations = g.Nodes.Values.Where(n => n.Category == "加油站");
            int k = 5;

            // Act
            var result = AStarPathfinder.FindKNearestStations(
                start,
                stations,
                g, k);

            // Assert
            Assert.That(result.Count, Is.LessThanOrEqualTo(k));
            Assert.That(result.All(r => 
                r.Path.First().Id == start.Id &&
                r.Path.Last() is SpatialNode endNode &&
                endNode.Category == "加油站"
             ));

            // 路径应避开断路
            foreach (var r in result)
            {
                var usedEdges = r.Path.Zip(r.Path.Skip(1), (from, to) =>
                    ((SpatialNode)from).Neighbors.OfType<DynamicEdge>().FirstOrDefault(e => e.TargetNodeId == to.Id)).ToList();
                Assert.That(usedEdges.All(e => e != null && e.IsOpen), "所有路径应通畅");
            }
        }
    }


}
