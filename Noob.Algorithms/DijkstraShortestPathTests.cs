using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;


namespace Noob.Algorithms
{

    /// <summary>
    /// 表示加权有向图中的边。
    /// </summary>
    public class DirectedEdge
    {
        /// <summary>起点</summary>
        public int From { get; }
        /// <summary>终点</summary>
        public int To { get; }
        /// <summary>权重（如距离/时间/费用）</summary>
        public double Weight { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DirectedEdge"/> class.
        /// </summary>
        /// <param name="from">From.</param>
        /// <param name="to">To.</param>
        /// <param name="weight">The weight.</param>
        public DirectedEdge(int from, int to, double weight)
        {
            From = from;
            To = to;
            Weight = weight;
        }
    }

    /// <summary>
    /// 图节点的邻接表表示（支持可扩展属性，如多类型权重/标签）。
    /// </summary>
    public class DirectedGraph
    {
        /// <summary>
        /// The adjacency
        /// </summary>
        private readonly Dictionary<int, List<DirectedEdge>> _adjacency = new();

        /// <summary>图中所有节点集合</summary>
        public IEnumerable<int> Nodes => _adjacency.Keys;

        /// <summary>向图添加一条有向边</summary>
        public void AddEdge(DirectedEdge edge)
        {
            if (!_adjacency.ContainsKey(edge.From))
                _adjacency[edge.From] = new List<DirectedEdge>();
            _adjacency[edge.From].Add(edge);

            // 确保终点在节点集合内（即使没有出边）
            if (!_adjacency.ContainsKey(edge.To))
                _adjacency[edge.To] = new List<DirectedEdge>();
        }

        /// <summary>获取某个节点的所有出边</summary>
        public IEnumerable<DirectedEdge> GetEdges(int node)
        {
            return _adjacency.ContainsKey(node) ? _adjacency[node] : Enumerable.Empty<DirectedEdge>();
        }
    }

    /// <summary>
    /// Dijkstra最短路径算法平台级实现（支持扩展与测试）。
    /// </summary>
    public class DijkstraShortestPath
    {
        /// <summary>
        /// The graph
        /// </summary>
        private readonly DirectedGraph _graph;
        /// <summary>
        /// The dist to
        /// </summary>
        private readonly Dictionary<int, double> _distTo;
        /// <summary>
        /// The edge to
        /// </summary>
        private readonly Dictionary<int, int?> _edgeTo;
        /// <summary>
        /// The visited
        /// </summary>
        private readonly HashSet<int> _visited;

        /// <summary>
        /// 构造函数，注入图对象。
        /// </summary>
        public DijkstraShortestPath(DirectedGraph graph)
        {
            _graph = graph ?? throw new ArgumentNullException(nameof(graph));
            _distTo = new Dictionary<int, double>();
            _edgeTo = new Dictionary<int, int?>();
            _visited = new HashSet<int>();
        }

        /// <summary>
        /// 计算从源点到所有节点的最短距离和路径。
        /// </summary>
        /// <param name="source">源点编号</param>
        public void Compute(int source)
        {
            _distTo.Clear();
            _edgeTo.Clear();
            _visited.Clear();

            var pq = new SimplePriorityQueue();
            foreach (var node in _graph.Nodes)
            {
                _distTo[node] = double.PositiveInfinity;
                _edgeTo[node] = null;
            }
            _distTo[source] = 0.0;
            pq.Enqueue(source, 0.0);

            while (pq.Count > 0)
            {
                int u = pq.Dequeue();
                if (_visited.Contains(u)) continue;
                _visited.Add(u);

                foreach (var edge in _graph.GetEdges(u))
                {
                    int v = edge.To;
                    double newDist = _distTo[u] + edge.Weight;
                    if (newDist < _distTo[v])
                    {
                        _distTo[v] = newDist;
                        _edgeTo[v] = u;
                        pq.Enqueue(v, newDist);
                    }
                }
            }
        }

        /// <summary>
        /// 获取从源点到指定终点的最短距离。
        /// </summary>
        public double GetDistance(int target)
        {
            return _distTo.TryGetValue(target, out double d) ? d : double.PositiveInfinity;
        }

        /// <summary>
        /// 获取从源点到指定终点的最短路径（节点序列）。
        /// </summary>
        public List<int> GetPath(int target)
        {
            if (!_distTo.ContainsKey(target) || double.IsInfinity(_distTo[target]))
                return null;
            var path = new Stack<int>();
            int? curr = target;
            while (curr != null)
            {
                path.Push(curr.Value);
                curr = _edgeTo[curr.Value];
            }
            return path.ToList();
        }

        /// <summary>
        /// 简单优先队列实现（小顶堆，支持可扩展替换为高性能版本）。
        /// </summary>
        private class SimplePriorityQueue
        {
            private readonly SortedSet<(double priority, int node, int idx)> _set = new();
            private int _insertIndex = 0;

            public int Count => _set.Count;

            public void Enqueue(int node, double priority)
            {
                _set.Add((priority, node, _insertIndex++));
            }
            public int Dequeue()
            {
                if (_set.Count == 0) throw new InvalidOperationException("队列为空");
                var first = _set.Min;
                _set.Remove(first);
                return first.node;
            }
        }
    }

    /// <summary>
    /// DijkstraShortestPath 平台级单元测试（NUnit + XML注释 + Assert.That）
    /// </summary>
    [TestFixture]
    public class DijkstraShortestPathTests
    {
        /// <summary>
        /// 基础功能：单一有向边可求出正确路径与距离。
        /// </summary>
        [Test]
        public void Compute_SingleEdge_ShouldFindCorrectDistanceAndPath()
        {
            var graph = new DirectedGraph();
            graph.AddEdge(new DirectedEdge(1, 2, 5.0));
            var dijkstra = new DijkstraShortestPath(graph);
            dijkstra.Compute(1);

            Assert.That(dijkstra.GetDistance(2), Is.EqualTo(5.0));
            var path = dijkstra.GetPath(2);
            Assert.That(path, Is.EqualTo(new List<int> { 1, 2 }));
        }

        /// <summary>
        /// 多节点多路径应选最短路径。
        /// </summary>
        [Test]
        public void Compute_MultiPaths_ShouldChooseShortest()
        {
            var graph = new DirectedGraph();
            graph.AddEdge(new DirectedEdge(1, 2, 2.0));
            graph.AddEdge(new DirectedEdge(1, 3, 4.0));
            graph.AddEdge(new DirectedEdge(2, 3, 1.0));
            var dijkstra = new DijkstraShortestPath(graph);
            dijkstra.Compute(1);

            Assert.That(dijkstra.GetDistance(3), Is.EqualTo(3.0));
            var path = dijkstra.GetPath(3);
            Assert.That(path, Is.EqualTo(new List<int> { 1, 2, 3 }));
        }

        /// <summary>
        /// 不可达节点应返回正无穷和空路径。
        /// </summary>
        [Test]
        public void Compute_UnreachableNode_ShouldReturnInfinityAndNullPath()
        {
            var graph = new DirectedGraph();
            graph.AddEdge(new DirectedEdge(1, 2, 1.0));
            graph.AddEdge(new DirectedEdge(2, 3, 1.0));
            // 节点4不连通
            graph.AddEdge(new DirectedEdge(4, 5, 1.0));
            var dijkstra = new DijkstraShortestPath(graph);
            dijkstra.Compute(1);

            Assert.That(dijkstra.GetDistance(4), Is.EqualTo(double.PositiveInfinity));
            var path = dijkstra.GetPath(4);
            Assert.That(path, Is.Null);
        }

        /// <summary>
        /// 支持回路/环路但能防止死循环，取最短距离。
        /// </summary>
        [Test]
        public void Compute_WithCycle_ShouldNotLoopForever()
        {
            var graph = new DirectedGraph();
            graph.AddEdge(new DirectedEdge(1, 2, 1.0));
            graph.AddEdge(new DirectedEdge(2, 3, 1.0));
            graph.AddEdge(new DirectedEdge(3, 1, 1.0)); // 环路
            var dijkstra = new DijkstraShortestPath(graph);
            dijkstra.Compute(1);

            Assert.That(dijkstra.GetDistance(3), Is.EqualTo(2.0));
            var path = dijkstra.GetPath(3);
            Assert.That(path, Is.EqualTo(new List<int> { 1, 2, 3 }));
        }

        /// <summary>
        /// 多路径、等权重时任选一条均视为正确。
        /// </summary>
        [Test]
        public void Compute_MultipleSameWeightPaths_AnyShortestPathValid()
        {
            var graph = new DirectedGraph();
            graph.AddEdge(new DirectedEdge(1, 2, 2.0));
            graph.AddEdge(new DirectedEdge(1, 3, 2.0));
            graph.AddEdge(new DirectedEdge(2, 4, 2.0));
            graph.AddEdge(new DirectedEdge(3, 4, 2.0));
            var dijkstra = new DijkstraShortestPath(graph);
            dijkstra.Compute(1);

            Assert.That(dijkstra.GetDistance(4), Is.EqualTo(4.0));
            var path = dijkstra.GetPath(4);
            // 可能为[1,2,4]或[1,3,4]，两者都可接受
            Assert.That(
                path,
                Is.AnyOf(new List<int> { 1, 2, 4 }, new List<int> { 1, 3, 4 })
            );
        }

        /// <summary>
        /// 起点到自身距离为0，路径只包含自身。
        /// </summary>
        [Test]
        public void Compute_SourceToSelf_ShouldReturnZeroAndSelf()
        {
            var graph = new DirectedGraph();
            graph.AddEdge(new DirectedEdge(7, 7, 0.0));
            var dijkstra = new DijkstraShortestPath(graph);
            dijkstra.Compute(7);

            Assert.That(dijkstra.GetDistance(7), Is.EqualTo(0.0));
            var path = dijkstra.GetPath(7);
            Assert.That(path, Is.EqualTo(new List<int> { 7 }));
        }

        /// <summary>
        /// 空图查询应返回正无穷和null。
        /// </summary>
        [Test]
        public void Compute_EmptyGraph_ShouldReturnInfinity()
        {
            var graph = new DirectedGraph();
            var dijkstra = new DijkstraShortestPath(graph);
            dijkstra.Compute(42); // 随意节点
            Assert.That(dijkstra.GetDistance(1), Is.EqualTo(double.PositiveInfinity));
            Assert.That(dijkstra.GetPath(1), Is.Null);
        }

        /// <summary>
        /// 图中有负权边时，不适合Dijkstra，应保持健壮（生产建议：检测并报错）。
        /// </summary>
        [Test]
        public void Compute_NegativeWeight_ShouldStillProduceAPathButWarn()
        {
            var graph = new DirectedGraph();
            graph.AddEdge(new DirectedEdge(1, 2, -3.0));
            graph.AddEdge(new DirectedEdge(2, 3, 1.0));
            var dijkstra = new DijkstraShortestPath(graph);
            // 允许测试，但实际生产应拒绝负权，或记录告警
            dijkstra.Compute(1);
            var dist = dijkstra.GetDistance(3);
            Assert.That(dist, Is.LessThanOrEqualTo(-2.0));
        }
    }


}
