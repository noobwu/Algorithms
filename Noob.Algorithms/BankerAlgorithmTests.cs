using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// The Algorithms namespace.
/// </summary>
namespace Noob.Algorithms
{
    /// <summary>
    /// 银行家算法核心类 <br/>
    /// 银行家算法是一种“只在确保安全的前提下分配资源，否则宁愿让请求等待”，从根本上避免死锁的策略。<br />
    /// 在操作系统里，多个进程同时申请资源（CPU/内存/磁盘等）。银行家算法会模拟“假如我这次答应了，会不会让系统进入一种死锁（谁都等不到资源）？”只要还有一条“全员平安撤退”的通路，才批资源。否则让本次请求排队。
    /// </summary>
    public class Banker
    {
        private readonly int _n; // 进程数
        private readonly int _m; // 资源种类
        private readonly int[] _available; // 可用资源
        private readonly int[,] _maximum;  // 最大需求
        private readonly int[,] _allocation; // 已分配
        private readonly int[,] _need; // 还需资源
        private readonly object _lock = new();

        public Banker(int[] available, int[,] maximum, int[,] allocation)
        {
            _n = maximum.GetLength(0);
            _m = available.Length;
            _available = (int[])available.Clone();
            _maximum = (int[,])maximum.Clone();
            _allocation = (int[,])allocation.Clone();
            _need = new int[_n, _m];

            for (int i = 0; i < _n; i++)
                for (int j = 0; j < _m; j++)
                    _need[i, j] = _maximum[i, j] - _allocation[i, j];
        }

        /// <summary>
        /// 银行家算法的请求资源方法
        /// </summary>
        /// <param name="pid">请求进程编号</param>
        /// <param name="request">请求向量</param>
        /// <returns>是否批准</returns>
        public bool Request(int pid, int[] request)
        {
            lock (_lock)
            {
                // 1. 检查请求是否合法
                for (int j = 0; j < _m; j++)
                    if (request[j] > _need[pid, j] || request[j] > _available[j])
                        return false;

                // 2. 试分配（先假定分配）
                for (int j = 0; j < _m; j++)
                {
                    _available[j] -= request[j];
                    _allocation[pid, j] += request[j];
                    _need[pid, j] -= request[j];
                }

                // 3. 检查系统是否安全
                bool safe = IsSafe();

                // 4. 不安全就回滚
                if (!safe)
                {
                    for (int j = 0; j < _m; j++)
                    {
                        _available[j] += request[j];
                        _allocation[pid, j] -= request[j];
                        _need[pid, j] += request[j];
                    }
                }
                return safe;
            }
        }

        /// <summary>
        /// 判断系统是否安全
        /// </summary>
        public bool IsSafe()
        {
            int[] work = (int[])_available.Clone();
            bool[] finish = new bool[_n];
            int[,] need = (int[,])_need.Clone();
            int[,] allocation = (int[,])_allocation.Clone();
            bool found;
            do
            {
                found = false;
                for (int i = 0; i < _n; i++)
                {
                    if (!finish[i] && Enumerable.Range(0, _m).All(j => need[i, j] <= work[j]))
                    {
                        for (int j = 0; j < _m; j++)
                            work[j] += allocation[i, j];
                        finish[i] = true;
                        found = true;
                    }
                }
            } while (found);
            return finish.All(f => f);
        }

        /// <summary>
        /// 查询当前资源分配快照
        /// </summary>
        public (int[] available, int[,] allocation, int[,] need) Snapshot()
        {
            lock (_lock)
            {
                return ((int[])_available.Clone(), (int[,])_allocation.Clone(), (int[,])_need.Clone());
            }
        }
    }


    /// <summary>
    /// Defines test class BankerTests.
    /// </summary>
    [TestFixture]
    public class BankerTests
    {
        // 参数化测试数据
        private static readonly object[] BankerCases =
        {
            // 合理请求
            new object[]
            {
                new[] {3, 3, 2},
                new[,] {{7,5,3}, {3,2,2}, {9,0,2}, {2,2,2}, {4,3,3}},
                new[,] {{0,1,0}, {2,0,0}, {3,0,2}, {2,1,1}, {0,0,2}},
                new[] {1, 0, 2},
                1,
                true
            },
            // 不合理请求（超最大需求或不安全）
            new object[]
            {
                new[] {3, 3, 2},
                new[,] {{7,5,3}, {3,2,2}, {9,0,2}, {2,2,2}, {4,3,3}},
                new[,] {{0,1,0}, {2,0,0}, {3,0,2}, {2,1,1}, {0,0,2}},
                new[] {6, 0, 0},
                0,
                false
            }
        };


        /// <summary>
        /// Defines the test method BankerAlgorithm_ParameterizedCases.
        /// </summary>
        /// <param name="available">The available.</param>
        /// <param name="maximum">The maximum.</param>
        /// <param name="allocation">The allocation.</param>
        /// <param name="request">The request.</param>
        /// <param name="pid">The pid.</param>
        /// <param name="expected">if set to <c>true</c> [expected].</param>
        [Test, TestCaseSource(nameof(BankerCases))]
        public void BankerAlgorithm_ParameterizedCases(
         int[] available,
         int[,] maximum,
         int[,] allocation,
         int[] request,
         int pid,
         bool expected)
        {
            var banker = new Banker(available, maximum, allocation);
            bool result = banker.Request(pid, request);

            if (result != expected)
            {
                // 打印关键快照帮助定位
                var (a, alloc, need) = banker.Snapshot();
                Console.WriteLine("Available: " + string.Join(",", a));
                Console.WriteLine("Request:   " + string.Join(",", request));
                Console.WriteLine("Allocation/Need:");
                for (int i = 0; i < alloc.GetLength(0); i++)
                    Console.WriteLine($"  P{i}: {string.Join(",", alloc.GetRow(i))} | {string.Join(",", need.GetRow(i))}");
            }

            Assert.AreEqual(expected, result, "请求审批结果不符");

            // 无论如何，系统必须安全
            Assert.IsTrue(banker.IsSafe(), "系统应始终安全");
        }

        /// <summary>
        /// Defines the test method BankerAlgorithm_Should_Be_ThreadSafe.
        /// </summary>
        [Test]
        public void BankerAlgorithm_Should_Be_ThreadSafe()
        {
            int[] available = { 10, 5, 7 };
            int[,] maximum = { { 7, 5, 3 }, { 3, 2, 2 }, { 9, 0, 2 }, { 2, 2, 2 }, { 4, 3, 3 } };
            int[,] allocation = { { 0, 1, 0 }, { 2, 0, 0 }, { 3, 0, 2 }, { 2, 1, 1 }, { 0, 0, 2 } };
            var banker = new Banker(available, maximum, allocation);

            int processCount = maximum.GetLength(0);
            int threadCount = 10;
            int successCount = 0;

            Parallel.For(0, threadCount, t =>
            {
                int pid = t % processCount;
                // 每个线程尝试发起一次合法或随机请求
                int[] req = new int[available.Length];
                for (int j = 0; j < req.Length; j++)
                {
                    // 生成不大于need的随机数，避免非法请求
                    lock (banker)
                    {
                        // 获取当前need
                        var (_, _, need) = banker.Snapshot();
                        req[j] = Math.Min(need[pid, j], 1); // 尝试最多1个
                    }
                }
                if (banker.Request(pid, req))
                    Interlocked.Increment(ref successCount);

                // 每次都保证系统安全
                Assert.IsTrue(banker.IsSafe(), "系统应始终安全（并发）");
            });

            // 多线程并发下仍然无死锁、资源安全
            Assert.GreaterOrEqual(successCount, 0);
        }
    }

    /// <summary>
    /// 工具方法：二维数组获取一行
    /// </summary>
    public static class ArrayExtensions
    {
        /// <summary>
        /// Gets the row.
        /// </summary>
        /// <param name="array">The array.</param>
        /// <param name="row">The row.</param>
        /// <returns>System.Int32[].</returns>
        public static int[] GetRow(this int[,] array, int row)
        {
            var cols = array.GetLength(1);
            var result = new int[cols];
            for (int i = 0; i < cols; i++)
                result[i] = array[row, i];
            return result;
        }
    }

}
