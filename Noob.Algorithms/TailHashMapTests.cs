using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Noob.Algorithms
{
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// 高性能哈希映射表，采用“编号取尾数/取模定位”与线性探测冲突解决，支持动态扩容，适合大规模分布式分片或用户/文档编号快速查找。
    /// </summary>
    /// <typeparam name="TKey">Key类型（如long、string等可转long）</typeparam>
    /// <typeparam name="TValue">Value类型</typeparam>
    public class TailHashMap<TKey, TValue>
    {
        /// <summary>
        /// 存储桶数组（主存储结构）。
        /// </summary>
        private Entry[] _buckets;

        /// <summary>
        /// 负载因子（超出后自动扩容，建议不超过0.75）。
        /// </summary>
        private readonly double _loadFactor;

        /// <summary>
        /// 当前已用元素数量。
        /// </summary>
        private int _count;

        /// <summary>
        /// 默认初始桶数量（建议为2的幂或质数）。
        /// </summary>
        private const int DefaultCapacity = 10000007; // 约一千万，防止冲突

        /// <summary>
        /// 构造函数，指定初始桶数量与负载因子。
        /// </summary>
        /// <param name="capacity">桶数量（默认一千万零七，防冲突）</param>
        /// <param name="loadFactor">负载因子（0.1~1.0），推荐0.75</param>
        public TailHashMap(int capacity = DefaultCapacity, double loadFactor = 0.75)
        {
            if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
            if (loadFactor <= 0 || loadFactor >= 1) throw new ArgumentOutOfRangeException(nameof(loadFactor));
            _buckets = new Entry[capacity];
            _loadFactor = loadFactor;
            _count = 0;
        }

        /// <summary>
        /// 插入或更新元素（如冲突采用线性探测）。
        /// </summary>
        /// <param name="key">主键（如用户ID/文档编号）</param>
        /// <param name="value">值</param>
        public void Put(TKey key, TValue value)
        {
            EnsureCapacity();
            int hashIdx = GetTailIndex(key, _buckets.Length);
            int idx = hashIdx;

            while (_buckets[idx] != null)
            {
                if (_buckets[idx].IsActive && _buckets[idx].Key.Equals(key))
                {
                    _buckets[idx].Value = value; // 更新
                    return;
                }
                idx = (idx + 1) % _buckets.Length; // 线性探测
                if (idx == hashIdx) throw new InvalidOperationException("哈希表已满，不能插入新元素。");
            }
            _buckets[idx] = new Entry { Key = key, Value = value, IsActive = true };
            _count++;
        }

        /// <summary>
        /// 查找key对应的值，找不到抛KeyNotFoundException。
        /// </summary>
        public TValue Get(TKey key)
        {
            int hashIdx = GetTailIndex(key, _buckets.Length);
            int idx = hashIdx;
            while (_buckets[idx] != null)
            {
                if (_buckets[idx].IsActive && _buckets[idx].Key.Equals(key))
                    return _buckets[idx].Value;
                idx = (idx + 1) % _buckets.Length;
                if (idx == hashIdx) break;
            }
            throw new KeyNotFoundException($"Key {key} 不存在。");
        }

        /// <summary>
        /// 尝试查找key对应的值。
        /// </summary>
        public bool TryGetValue(TKey key, out TValue value)
        {
            int hashIdx = GetTailIndex(key, _buckets.Length);
            int idx = hashIdx;
            while (_buckets[idx] != null)
            {
                if (_buckets[idx].IsActive && _buckets[idx].Key.Equals(key))
                {
                    value = _buckets[idx].Value;
                    return true;
                }
                idx = (idx + 1) % _buckets.Length;
                if (idx == hashIdx) break;
            }
            value = default;
            return false;
        }

        /// <summary>
        /// 删除key对应元素（惰性删除）。
        /// </summary>
        public bool Remove(TKey key)
        {
            int hashIdx = GetTailIndex(key, _buckets.Length);
            int idx = hashIdx;
            while (_buckets[idx] != null)
            {
                if (_buckets[idx].IsActive && _buckets[idx].Key.Equals(key))
                {
                    _buckets[idx].IsActive = false;
                    _count--;
                    return true;
                }
                idx = (idx + 1) % _buckets.Length;
                if (idx == hashIdx) break;
            }
            return false;
        }

        /// <summary>
        /// 当前存储的元素数量。
        /// </summary>
        public int Count => _count;

        /// <summary>
        /// 尾数定位核心：只保留key编号的尾部几位（等价于对桶数取模）。
        /// </summary>
        /// <param name="key">主键</param>
        /// <param name="modulo">桶总数</param>
        /// <returns>下标</returns>
        protected virtual int GetTailIndex(TKey key, int modulo)
        {
            long n = KeyToLong(key);
            return (int)(Math.Abs(n) % modulo); // 只保留尾数（如后7位）
        }

        /// <summary>
        /// 扩容并rehash（负载超过阈值时自动调用）。
        /// </summary>
        private void EnsureCapacity()
        {
            if (_count < _buckets.Length * _loadFactor) return;
            int newCapacity = _buckets.Length * 2 + 1; // 增为奇数更防冲突
            var oldBuckets = _buckets;
            _buckets = new Entry[newCapacity];
            _count = 0;
            foreach (var entry in oldBuckets)
            {
                if (entry != null && entry.IsActive)
                    Put(entry.Key, entry.Value); // 重新分布
            }
        }

        /// <summary>
        /// 支持long、int、string等主键的自动转换（如string自动hashcode）。
        /// </summary>
        private long KeyToLong(TKey key)
        {
            if (key is long l) return l;
            if (key is int i) return i;
            if (key is string s) return s.GetHashCode();
            throw new ArgumentException("仅支持long、int、string等类型主键。");
        }

        /// <summary>
        /// 内部存储单元结构（含惰性删除标记）。
        /// </summary>
        private class Entry
        {
            /// <summary>
            /// The key
            /// </summary>
            public TKey Key;
            /// <summary>
            /// The value
            /// </summary>
            public TValue Value;
            /// <summary>
            /// The is active
            /// </summary>
            public bool IsActive;
        }
    }


    /// <summary>
    /// TailHashMap 单元测试（NUnit + XML注释 + Assert.That）
    /// </summary>
    [TestFixture]
    public class TailHashMapTests
    {
        /// <summary>
        /// 验证插入后可正常查找与计数。
        /// </summary>
        [Test]
        public void PutAndGet_ShouldWork_ForLongKey()
        {
            var map = new TailHashMap<long, string>(capacity: 17);
            map.Put(1234567L, "Alice");
            map.Put(7654321L, "Bob");

            Assert.That(map.Count, Is.EqualTo(2));
            Assert.That(map.Get(1234567L), Is.EqualTo("Alice"));
            Assert.That(map.Get(7654321L), Is.EqualTo("Bob"));
        }

        /// <summary>
        /// 验证插入后可覆盖原值。
        /// </summary>
        [Test]
        public void Put_OverwriteExistingValue_ShouldUpdate()
        {
            var map = new TailHashMap<long, string>();
            map.Put(100L, "First");
            map.Put(100L, "Second");

            Assert.That(map.Count, Is.EqualTo(1));
            Assert.That(map.Get(100L), Is.EqualTo("Second"));
        }

        /// <summary>
        /// 验证删除后元素不可查找，计数减一。
        /// </summary>
        [Test]
        public void Remove_ShouldDelete_AndDecreaseCount()
        {
            var map = new TailHashMap<long, string>();
            map.Put(99L, "Hello");
            bool removed = map.Remove(99L);

            Assert.That(removed, Is.True);
            Assert.That(map.Count, Is.EqualTo(0));
            Assert.That(() => map.Get(99L), Throws.TypeOf<KeyNotFoundException>());
        }

        /// <summary>
        /// 验证 TryGetValue 可区分命中和未命中。
        /// </summary>
        [Test]
        public void TryGetValue_ShouldReturnTrueOrFalse()
        {
            var map = new TailHashMap<int, string>();
            map.Put(7, "Seven");
            string value;
            bool found = map.TryGetValue(7, out value);

            Assert.That(found, Is.True);
            Assert.That(value, Is.EqualTo("Seven"));

            bool notFound = map.TryGetValue(888, out value);
            Assert.That(notFound, Is.False);
            Assert.That(value, Is.Null);
        }

        /// <summary>
        /// 验证对string主键的支持。
        /// </summary>
        [Test]
        public void PutAndGet_ShouldWork_ForStringKey()
        {
            var map = new TailHashMap<string, int>();
            map.Put("Alice", 1);
            map.Put("Bob", 2);

            Assert.That(map.Get("Alice"), Is.EqualTo(1));
            Assert.That(map.Get("Bob"), Is.EqualTo(2));
        }

        /// <summary>
        /// 验证扩容时数据不会丢失。
        /// </summary>
        [Test]
        public void AutoExpand_ShouldKeepAllElements()
        {
            var map = new TailHashMap<long, string>(capacity: 5, loadFactor: 0.6);
            for (int i = 0; i < 10; i++)
            {
                map.Put(i, $"user{i}");
            }
            Assert.That(map.Count, Is.EqualTo(10));
            for (int i = 0; i < 10; i++)
            {
                Assert.That(map.Get(i), Is.EqualTo($"user{i}"));
            }
        }

        /// <summary>
        /// 验证冲突时线性探测依然正确存取数据。
        /// </summary>
        [Test]
        public void PutWithCollision_LinearProbeShouldWork()
        {
            // 取模为5，故5、10、15会冲突
            var map = new TailHashMap<int, string>(capacity: 5);
            map.Put(5, "A");
            map.Put(10, "B");
            map.Put(15, "C");

            Assert.That(map.Get(5), Is.EqualTo("A"));
            Assert.That(map.Get(10), Is.EqualTo("B"));
            Assert.That(map.Get(15), Is.EqualTo("C"));
            Assert.That(map.Count, Is.EqualTo(3));
        }

        /// <summary>
        /// 验证插入不支持类型时抛异常。
        /// </summary>
        [Test]
        public void Put_UnsupportedKeyType_Throws()
        {
            var map = new TailHashMap<DateTime, string>();
            Assert.That(() => map.Put(DateTime.Now, "fail"), Throws.TypeOf<ArgumentException>());
        }

        /// <summary>
        /// 验证哈希表容量满时会自动扩容，不抛异常，数据可全部查回。
        /// </summary>
        [Test]
        public void Put_WhenFull_ShouldAutoExpand()
        {
            var map = new TailHashMap<int, string>(capacity: 2, loadFactor: 0.6);
            map.Put(1, "one");
            map.Put(2, "two");
            map.Put(3, "three"); // 应触发扩容
            map.Put(4, "four");
            Assert.That(map.Count, Is.EqualTo(4));
            Assert.That(map.Get(1), Is.EqualTo("one"));
            Assert.That(map.Get(2), Is.EqualTo("two"));
            Assert.That(map.Get(3), Is.EqualTo("three"));
            Assert.That(map.Get(4), Is.EqualTo("four"));
        }

        /// <summary>
        /// 验证惰性删除后的插入/查找依然正常。
        /// </summary>
        [Test]
        public void Remove_AndPut_AfterLazyDelete()
        {
            var map = new TailHashMap<int, string>(capacity: 5);
            map.Put(42, "A");
            map.Remove(42);
            map.Put(42, "B");
            Assert.That(map.Get(42), Is.EqualTo("B"));
        }
    }


}
