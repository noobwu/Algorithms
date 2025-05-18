using NUnit.Framework;
using System.Collections.Concurrent;
namespace Noob.Algorithms
{
    /// <summary>
    /// 线程安全的令牌桶限流器
    /// - capacity：最大令牌数，决定最大瞬时“抗压”能力
    /// - refillRate：令牌补充速率（每秒）
    /// - initialTokens：初始化令牌数（可选），适合测试、动态风控、灰度、冷启动等场景
    ///   默认=capacity（满桶），但可设置为空桶或部分装
    /// </summary>
    public class TokenBucketLimiter
    {
        /// <summary>
        /// The capacity
        /// </summary>
        private readonly int _capacity;
        /// <summary>
        /// The refill rate
        /// </summary>
        private readonly int _refillRate;
        /// <summary>
        /// The tokens
        /// </summary>
        private int _tokens;
        /// <summary>
        /// The last refill time
        /// </summary>
        private DateTime _lastRefillTime;
        /// <summary>
        /// The lock
        /// </summary>
        private readonly object _lock = new();

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="capacity">桶最大容量（必须大于0）</param>
        /// <param name="refillRate">每秒补充令牌速率（必须大于0）</param>
        /// <param name="initialTokens">
        /// 初始令牌数（可选，默认=capacity），
        /// 适合测试冷启动、灰度预热、动态风控等
        /// </param>
        public TokenBucketLimiter(int capacity, int refillRate, int? initialTokens = null)
        {
            if (capacity <= 0) throw new ArgumentException("capacity must be positive");
            if (refillRate <= 0) throw new ArgumentException("refillRate must be positive");

            _capacity = capacity;
            _refillRate = refillRate;

            // 默认满桶，允许[0, capacity]自定义初始令牌
            _tokens = initialTokens.HasValue
                ? Math.Max(0, Math.Min(capacity, initialTokens.Value))
                : capacity;

            _lastRefillTime = DateTime.UtcNow;
        }

        /// <summary>
        /// 尝试获取n个令牌（默认1个），成功返回true，否则false
        /// </summary>
        public bool TryAcquire(int tokens = 1)
        {
            if (tokens <= 0) throw new ArgumentException("tokens must be positive");

            lock (_lock)
            {
                Refill();
                if (_tokens >= tokens)
                {
                    _tokens -= tokens;
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// 获取当前可用令牌数（会自动补充到最新状态）
        /// </summary>
        public double GetAvailableTokens()
        {
            lock (_lock)
            {
                Refill();
                return _tokens;
            }
        }

        /// <summary>
        /// 桶最大容量
        /// </summary>
        public int Capacity => _capacity;

        /// <summary>
        /// 令牌补充速率（每秒）
        /// </summary>
        public int RefillRate => _refillRate;

        /// <summary>
        /// 补充令牌到桶内（自动向上限靠拢）
        /// </summary>
        private void Refill()
        {
            var now = DateTime.UtcNow;
            var elapsed = (now - _lastRefillTime).TotalSeconds;
            if (elapsed > 0)
            {
                var added = elapsed * _refillRate;
                if (added > 0)
                {
                    _tokens = Math.Min(_capacity, (int)(_tokens + added));
                    _lastRefillTime = now;
                }
            }
        }
    }

    /// <summary>
    /// 线程安全的漏斗限流器（Leaky Bucket）
    /// - capacity: 漏斗队列最大长度（排队极限）
    /// - leakRate: 每秒最大处理速率（单位：次/秒）
    /// </summary>
    public class LeakyBucketLimiter : IDisposable
    {
        private readonly int _capacity;
        private readonly int _leakRate;
        private readonly ConcurrentQueue<DateTime> _queue = new();
        private CancellationTokenSource? _leakCts;
        private Task? _leakTask;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="capacity">漏斗队列最大容量，必须大于0</param>
        /// <param name="leakRate">每秒处理速率，必须大于0</param>
        public LeakyBucketLimiter(int capacity, int leakRate)
        {
            if (capacity <= 0) throw new ArgumentException("capacity must be positive");
            if (leakRate <= 0) throw new ArgumentException("leakRate must be positive");
            _capacity = capacity;
            _leakRate = leakRate;
        }

        /// <summary>
        /// 尝试排队（如满则拒绝）
        /// </summary>
        /// <returns>队列未满返回true，否则false</returns>
        public bool TryEnqueue()
        {
            if (_queue.Count >= _capacity)
                return false;
            _queue.Enqueue(DateTime.UtcNow);
            return true;
        }

        /// <summary>
        /// 当前排队数
        /// </summary>
        public int QueueCount => _queue.Count;

        /// <summary>
        /// 启动漏水处理（onLeak：每次出队触发）
        /// </summary>
        public void StartLeaking(Action onLeak)
        {
            if (_leakCts != null) return; // 已启动
            _leakCts = new CancellationTokenSource();
            _leakTask = Task.Run(async () =>
            {
                var intervalMs = 1000.0 / _leakRate;
                while (!_leakCts.IsCancellationRequested)
                {
                    // 每轮最多批量漏水：leakRate次
                    int batch = 0;
                    while (batch < _leakRate && _queue.TryDequeue(out _))
                    {
                        onLeak?.Invoke();
                        batch++;
                    }
                    await Task.Delay(TimeSpan.FromMilliseconds(intervalMs));
                }
            }, _leakCts.Token);
        }

        /// <summary>
        /// 停止漏水线程（可用于单元测试或安全关闭）
        /// </summary>
        public void StopLeaking()
        {
            _leakCts?.Cancel();
            try { _leakTask?.Wait(1000); } catch { }
            _leakCts = null;
            _leakTask = null;
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose() => StopLeaking();
    }

    /// <summary>
    /// 秒杀业务模拟服务（令牌桶+漏斗限流联合，线程安全，便于测试与扩展）
    /// </summary>
    public class SeckillService : IDisposable
    {
        private readonly TokenBucketLimiter _tokenLimiter;
        private readonly LeakyBucketLimiter _leakyLimiter;
        private int _stock; // 商品库存
        private int _successCount; // 实际成功数

        /// <summary>
        /// 秒杀成功回调，便于测试/业务统计（参数：剩余库存、累计成功数）
        /// </summary>
        public Action<int, int>? OnSeckillSuccess { get; set; }
        /// <summary>
        /// 秒杀失败回调（库存不足时），便于监控
        /// </summary>
        public Action? OnSeckillSoldOut { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SeckillService"/> class.
        /// </summary>
        /// <param name="initialStock">The initial stock.</param>
        /// <param name="tokenBucketCapacity">The token bucket capacity.</param>
        /// <param name="tokenRefillRate">The token refill rate.</param>
        /// <param name="leakyBucketCapacity">The leaky bucket capacity.</param>
        /// <param name="leakRate">The leak rate.</param>
        /// <exception cref="System.ArgumentException">初始库存不能为负数</exception>
        public SeckillService(
            int initialStock,
            int tokenBucketCapacity,
            int tokenRefillRate,
            int leakyBucketCapacity,
            int leakRate)
        {
            if (initialStock < 0) throw new ArgumentException("初始库存不能为负数");
            _stock = initialStock;
            _tokenLimiter = new TokenBucketLimiter(tokenBucketCapacity, tokenRefillRate);
            _leakyLimiter = new LeakyBucketLimiter(leakyBucketCapacity, leakRate);
            _leakyLimiter.StartLeaking(ProcessOrder);
        }

        /// <summary>
        /// 秒杀请求入口
        /// </summary>
        /// <returns>用户反馈</returns>
        public string TrySeckill()
        {
            // 1. 令牌桶限流
            if (!_tokenLimiter.TryAcquire())
                return "高峰限流中，未获得令牌！请稍后再试。";

            // 2. 漏斗限流
            if (!_leakyLimiter.TryEnqueue())
                return "排队过多，秒杀未成功！请稍后再试。";

            // 3. 排队成功，等待异步处理
            return "进入秒杀队列，等待处理结果！";
        }

        /// <summary>
        /// 实际订单处理（由漏斗异步触发）
        /// </summary>
        private void ProcessOrder()
        {
            if (Interlocked.CompareExchange(ref _stock, 0, 0) > 0)
            {
                int left = Interlocked.Decrement(ref _stock);
                int success = Interlocked.Increment(ref _successCount);

                // 用户自定义成功回调（如发券/下单/数据埋点）
                OnSeckillSuccess?.Invoke(left, success);

                // 控制台友好输出
                Console.WriteLine($"[{DateTime.Now:T}] 秒杀成功，剩余库存：{left}");
            }
            else
            {
                OnSeckillSoldOut?.Invoke();
                Console.WriteLine($"[{DateTime.Now:T}] 很抱歉，商品已售罄！");
            }
        }

        /// <summary>
        /// 查询当前剩余库存
        /// </summary>
        public int GetStock() => Interlocked.CompareExchange(ref _stock, 0, 0);

        /// <summary>
        /// 查询累计秒杀成功数
        /// </summary>
        public int GetSuccessCount() => Interlocked.CompareExchange(ref _successCount, 0, 0);

        /// <summary>
        /// 关闭漏斗线程（安全释放）
        /// </summary>
        public void Dispose()
        {
            _leakyLimiter?.Dispose();
        }
    }

    /// <summary>
    /// Defines test class SeckillLimiterTests.
    /// </summary>
    [TestFixture]
    public class SeckillLimiterTests
    {
        /// <summary>
        /// Defines the test method TokenBucketLimiter_Should_Allow_Requests_When_Tokens_Available.
        /// </summary>
        [Test]
        public void TokenBucketLimiter_Should_Allow_Requests_When_Tokens_Available()
        {
            // 令牌桶参数
            int capacity = 5;
            int refillRate = 1; // 每秒1个

            // 初始化为满桶
            var limiter = new TokenBucketLimiter(capacity, refillRate);

            // 连续5次获取应全部成功
            for (int i = 0; i < capacity; i++)
            {
                Assert.IsTrue(limiter.TryAcquire(), $"第{i + 1}次请求应该被允许");
            }

            // 第6次立即请求应该失败
            Assert.IsFalse(limiter.TryAcquire(), "超过桶容量的请求应被限流");

            // 等待1.1秒，令牌应补充1个
            Thread.Sleep(1100);

            Assert.IsTrue(limiter.TryAcquire(), "等待补充后，应允许新请求");
            Assert.AreEqual(0, limiter.GetAvailableTokens(), 0.01, "应只剩0个令牌");
        }

        /// <summary>
        /// Defines the test method LeakyBucketLimiter_Should_Enqueue_Until_Capacity.
        /// </summary>
        [Test]
        public void LeakyBucketLimiter_Should_Enqueue_Until_Capacity()
        {
            var limiter = new LeakyBucketLimiter(10, 5); // 最大队列10，每秒5单
            limiter.StartLeaking(() => Console.WriteLine("请求被处理！"));
            for (int i = 0; i < 20; i++)
            {
                bool accepted = limiter.TryEnqueue();
                Console.WriteLine($"第{i + 1}个请求：{(accepted ? "进入队列" : "被拒绝")}");
            }
            Thread.Sleep(3000);
            limiter.StopLeaking();
        }


        /// <summary>
        /// Defines the test method SeckillService_Should_Limit_Successful_Orders_By_Stock.
        /// </summary>
        [Test]
        public void SeckillService_Should_Limit_Successful_Orders_By_Stock()
        {
            var seckill = new SeckillService(
                 initialStock: 10,
                 tokenBucketCapacity: 20,
                 tokenRefillRate: 10,
                 leakyBucketCapacity: 15,
                 leakRate: 5
             );

            // 注入自定义事件（如埋点、统计、报警等）
            seckill.OnSeckillSuccess = (stockLeft, successCount) =>
            {
                // 记录数据库/消息队列/控制台等
                Console.WriteLine($"【统计】第{successCount}单成功，剩余库存：{stockLeft}");
            };

            seckill.OnSeckillSoldOut = () =>
            {
                // 报警/业务通知等
                Console.WriteLine("【预警】库存已售罄！");
            };

            // 并发模拟秒杀
            Parallel.For(0, 50, i =>
            {
                var result = seckill.TrySeckill();
                Console.WriteLine($"用户{i + 1:D2}: {result}");
            });

            Thread.Sleep(3000); // 给漏斗留足处理时间
            seckill.Dispose();

        }
    }

}

