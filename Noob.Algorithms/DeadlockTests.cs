using NUnit.Framework;
using NUnit.Framework.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Noob.Algorithms
{
    /// <summary>
    /// 线程安全的银行账户对象，支持存款、取款、余额查询、加锁等操作
    /// </summary>
    public class BankAccount
    {
        /// <summary>
        /// 账户名（唯一标识）
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// 账户余额（私有字段，仅受锁保护访问）
        /// </summary>
        private int _balance;

        /// <summary>
        /// 账户内部锁对象，用于保证所有操作的线程安全
        /// </summary>
        private readonly object _lock = new();

        /// <summary>
        /// 构造函数，初始化账户名和余额
        /// </summary>
        public BankAccount(string name, int initialBalance)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("账户名不能为空");
            if (initialBalance < 0)
                throw new ArgumentException("初始余额不能为负");
            Name = name;
            _balance = initialBalance;
        }

        /// <summary>
        /// 查询账户余额（线程安全）
        /// </summary>
        public int Balance
        {
            get { lock (_lock) { return _balance; } }
        }

        /// <summary>
        /// 线程安全存款
        /// </summary>
        /// <param name="amount">存款金额，必须大于0</param>
        public void Deposit(int amount)
        {
            if (amount <= 0) throw new ArgumentException("存款金额必须大于0");
            lock (_lock)
            {
                _balance += amount;
            }
        }

        /// <summary>
        /// 线程安全取款
        /// </summary>
        /// <param name="amount">取款金额，必须大于0且小于等于余额</param>
        public void Withdraw(int amount)
        {
            if (amount <= 0) throw new ArgumentException("取款金额必须大于0");
            lock (_lock)
            {
                if (_balance < amount)
                    throw new InvalidOperationException($"{Name} 余额不足，当前余额: {_balance}");
                _balance -= amount;
            }
        }

        /// <summary>
        /// 封装账户锁对象，便于业务层（如转账）统一顺序加锁
        /// </summary>
        public object GetLock() => _lock;

        /// <summary>
        /// （可选）线程安全转账（与另一个账户，原子操作，可防止并发下余额异常）
        /// </summary>
        public static void Transfer(BankAccount from, BankAccount to, int amount)
        {
            if (from == null || to == null) throw new ArgumentNullException("账户不能为空");
            if (ReferenceEquals(from, to)) throw new ArgumentException("不能给自己转账");
            if (amount <= 0) throw new ArgumentException("转账金额必须大于0");

            // 统一加锁顺序，彻底防死锁
            var accounts = string.CompareOrdinal(from.Name, to.Name) < 0
                ? new[] { from, to }
                : new[] { to, from };
            lock (accounts[0].GetLock())
            {
                lock (accounts[1].GetLock())
                {
                    from.Withdraw(amount);
                    to.Deposit(amount);
                }
            }
        }

  
    }

    /// <summary>
    /// Defines test class DeadlockTests.
    /// </summary>
    [TestFixture]
    public class DeadlockTests
    {
        /// <summary>
        /// Defines the test method ShouldDetectDeadlock.
        /// </summary>
        [Test]
        public void ShouldDetectDeadlock()
        {
            var accA = new BankAccount("Alice", 1000);
            var accB = new BankAccount("Bob", 1000);
            var demo = new DeadlockDemo();

            Assert.Throws<DeadlockDemo.DeadlockDetectedException>(
                () => demo.CauseDeadlock(accA, accB, 100, 200, 300, Console.WriteLine),
                "应检测到死锁并抛出 DeadlockDetectedException");
        }

        /// <summary>
        /// Defines the test method ShouldNotDetectDeadlock_WhenOrderedLock.
        /// </summary>
        [Test]
        public void ShouldNotDetectDeadlock_WhenOrderedLock()
        {
            var accA = new BankAccount("Alice", 1000);
            var accB = new BankAccount("Bob", 1000);
            bool completed = false;
            // 正确顺序加锁，避免死锁
            var t = new Thread(() =>
            {
                var accounts = string.CompareOrdinal(accA.Name, accB.Name) < 0
                    ? new[] { accA, accB }
                    : new[] { accB, accA };
                lock (accounts[0].GetLock())
                {
                    Thread.Sleep(10);
                    lock (accounts[1].GetLock())
                    {
                        accA.Withdraw(100);
                        accB.Deposit(100);
                        completed = true;
                    }
                }
            });
            t.Start();
            Assert.IsTrue(t.Join(500), "顺序加锁应无死锁");
            Assert.IsTrue(completed, "任务应能正常完成");
        }

        /// <summary>
        /// 死锁预防（强制加锁顺序）
        /// </summary>
        /// <param name="lockA">The lock a.</param>
        /// <param name="lockB">The lock b.</param>
        /// <param name="criticalSection">The critical section.</param>
        public static void LockInOrder(object lockA, object lockB, Action criticalSection)
        {
            object first = lockA.GetHashCode() < lockB.GetHashCode() ? lockA : lockB;
            object second = first == lockA ? lockB : lockA;

            lock (first)
            {
                lock (second)
                {
                    criticalSection();
                }
            }
        }

        /// <summary>
        /// Defines the test method ShouldAvoidDeadlock_WithOrderedLock.
        /// </summary>
        [Test]
        public void ShouldAvoidDeadlock_WithOrderedLock()
        {
            var accA = new BankAccount("Alice", 1000);
            var accB = new BankAccount("Bob", 1000);
            bool finished = false;

            var t = new Thread(() =>
            {
                LockInOrder(accA.GetLock(), accB.GetLock(), () =>
                {
                    accA.Withdraw(100);
                    accB.Deposit(100);
                    finished = true;
                });
            });
            t.Start();
            Assert.IsTrue(t.Join(500));
            Assert.IsTrue(finished, "顺序加锁应无死锁");
        }

        /// <summary>
        /// Tries the double lock.
        /// </summary>
        /// <param name="lockA">The lock a.</param>
        /// <param name="lockB">The lock b.</param>
        /// <param name="timeout">The timeout.</param>
        /// <param name="criticalSection">The critical section.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public static bool TryDoubleLock(object lockA, object lockB, TimeSpan timeout, Action criticalSection)
        {
            DateTime deadline = DateTime.Now + timeout;
            while (DateTime.Now < deadline)
            {
                if (Monitor.TryEnter(lockA, 100))
                {
                    try
                    {
                        if (Monitor.TryEnter(lockB, 100))
                        {
                            try
                            {
                                criticalSection();
                                return true;
                            }
                            finally { Monitor.Exit(lockB); }
                        }
                    }
                    finally { Monitor.Exit(lockA); }
                }
                Thread.Sleep(10);
            }
            return false; // 超时，可能死锁
        }

        /// <summary>
        /// Defines the test method ShouldAvoidDeadlock_WithTryDoubleLock.
        /// </summary>
        [Test]
        public void ShouldAvoidDeadlock_WithTryDoubleLock()
        {
            var accA = new BankAccount("Alice", 1000);
            var accB = new BankAccount("Bob", 1000);
            bool locked = TryDoubleLock(accA.GetLock(), accB.GetLock(), TimeSpan.FromMilliseconds(300), () =>
            {
                accA.Withdraw(100);
                accB.Deposit(100);
            });
            Assert.IsTrue(locked, "应通过TryEnter机制避免死锁");
        }

        /// <summary>
        /// Transfers the with defense.
        /// </summary>
        /// <param name="from">From.</param>
        /// <param name="to">To.</param>
        /// <param name="amount">The amount.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="maxRetries">The maximum retries.</param>
        public static void TransferWithDefense(
    BankAccount from, BankAccount to, int amount, ILogger logger, int maxRetries = 3)
        {
            for (int i = 0; i < maxRetries; i++)
            {
                bool success = TryDoubleLock(
                    from.GetLock(), to.GetLock(), TimeSpan.FromMilliseconds(200), () =>
                    {
                        from.Withdraw(amount);
                        to.Deposit(amount);
                    });

                if (success)
                {
                    logger.Info("转账成功：{From}->{To} 金额{Amount}", from.Name, to.Name, amount);
                    return;
                }
                logger.Warning("第{Retry}次重试未获取锁，警惕死锁", i + 1);
                Thread.Sleep(30);
            }
            logger.Error("连续重试未果，触发死锁报警或降级处理：{From}->{To} 金额{Amount}", from.Name, to.Name, amount);
            // 可接入报警、人工介入等
        }

    }

    /// <summary>
    /// 死锁制造与检测
    /// </summary>
    public class DeadlockDemo
    {
        /// <summary>
        /// 死锁检测异常类型，便于单元测试精确捕捉
        /// </summary>
        public class DeadlockDetectedException : Exception
        {
            public DeadlockDetectedException(string msg) : base(msg) { }
        }

        /// <summary>
        /// 制造典型死锁：A->B、B->A互等对方锁，并检测是否死锁
        /// </summary>
        /// <param name="accA">账户A</param>
        /// <param name="accB">账户B</param>
        /// <param name="amountAtoB">A转B金额</param>
        /// <param name="amountBtoA">B转A金额</param>
        /// <param name="timeoutMs">等待线程结束超时（ms），默认500ms</param>
        /// <param name="logAction">日志输出委托，可用于调试</param>
        public void CauseDeadlock(
            BankAccount accA,
            BankAccount accB,
            int amountAtoB,
            int amountBtoA,
            int timeoutMs = 500,
            Action<string>? logAction = null)
        {
            // 线程1尝试锁定A，再锁定B
            var t1 = new Thread(() =>
            {
                logAction?.Invoke("[T1] 尝试锁定A");
                lock (accA.GetLock())
                {
                    logAction?.Invoke("[T1] 已锁定A，等待锁定B");
                    Thread.Sleep(50);
                    lock (accB.GetLock())
                    {
                        logAction?.Invoke("[T1] 已锁定B，执行A->B转账");
                        accA.Withdraw(amountAtoB);
                        accB.Deposit(amountAtoB);
                    }
                }
            });

            // 线程2尝试锁定B，再锁定A
            var t2 = new Thread(() =>
            {
                logAction?.Invoke("[T2] 尝试锁定B");
                lock (accB.GetLock())
                {
                    logAction?.Invoke("[T2] 已锁定B，等待锁定A");
                    Thread.Sleep(50);
                    lock (accA.GetLock())
                    {
                        logAction?.Invoke("[T2] 已锁定A，执行B->A转账");
                        accB.Withdraw(amountBtoA);
                        accA.Deposit(amountBtoA);
                    }
                }
            });

            t1.Start();
            t2.Start();

            // 死锁检测
            bool t1Done = t1.Join(timeoutMs);
            bool t2Done = t2.Join(timeoutMs);

            if (!t1Done || !t2Done)
            {
                logAction?.Invoke("[检测] 检测到死锁！");
                throw new DeadlockDetectedException("检测到死锁，线程无法结束！");
            }
            else
            {
                logAction?.Invoke("[检测] 未检测到死锁，线程均已完成。");
            }
        }
    }
}
