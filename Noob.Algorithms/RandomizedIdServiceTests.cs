using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Noob.Algorithms
{
    using NUnit.Framework;
    using System;
    using System.Security.Cryptography;
    using System.Text;

    /// <summary>
    /// 名字/词语等对象的哈希编号策略接口（可扩展多个算法策略）。
    /// </summary>
    public interface IObjectHasher
    {
        /// <summary>
        /// 计算输入对象的伪随机编号（返回long，保证全局唯一且分布均匀）。
        /// </summary>
        long GetObjectHash(string input);
    }

    /// <summary>
    /// 经典MurmurHash3算法实现（高性能、分布好，适合分表、索引）。
    /// </summary>
    public class MurmurHash3ObjectHasher : IObjectHasher
    {
        /// <summary>
        /// 计算输入的MurmurHash3-128，并折叠为long编号。
        /// </summary>
        public long GetObjectHash(string input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            byte[] data = Encoding.UTF8.GetBytes(input);
            byte[] hash = MurmurHash3.Hash128(data, 0x9747b28c);
            // 取前8字节转long（可用异或折叠）
            long code = BitConverter.ToInt64(hash, 0) ^ BitConverter.ToInt64(hash, 8);
            return code < 0 ? -code : code;
        }

        /// <summary>
        /// MurmurHash3-128实现（适合高并发哈希表/分表/唯一索引）。
        /// </summary>
        public static class MurmurHash3
        {
            /// <summary>
            /// Hash128s the specified data.
            /// </summary>
            /// <param name="data">The data.</param>
            /// <param name="seed">The seed.</param>
            /// <returns>System.Byte[].</returns>
            public static byte[] Hash128(byte[] data, uint seed = 0)
            {
                // 128位输出，见 https://github.com/darrenkopp/murmurhash-net
                const ulong c1 = 0x87c37b91114253d5L;
                const ulong c2 = 0x4cf5ad432745937fL;
                ulong h1 = seed;
                ulong h2 = seed;
                int length = data.Length;
                int pos = 0;

                while (length >= 16)
                {
                    ulong k1 = BitConverter.ToUInt64(data, pos);
                    ulong k2 = BitConverter.ToUInt64(data, pos + 8);

                    k1 *= c1; k1 = Rotl64(k1, 31); k1 *= c2; h1 ^= k1;
                    h1 = Rotl64(h1, 27); h1 += h2; h1 = h1 * 5 + 0x52dce729;

                    k2 *= c2; k2 = Rotl64(k2, 33); k2 *= c1; h2 ^= k2;
                    h2 = Rotl64(h2, 31); h2 += h1; h2 = h2 * 5 + 0x38495ab5;

                    pos += 16; length -= 16;
                }
                ulong k1f = 0, k2f = 0;
                switch (length)
                {
                    case 15: k2f ^= ((ulong)data[pos + 14]) << 48; goto case 14;
                    case 14: k2f ^= ((ulong)data[pos + 13]) << 40; goto case 13;
                    case 13: k2f ^= ((ulong)data[pos + 12]) << 32; goto case 12;
                    case 12: k2f ^= ((ulong)data[pos + 11]) << 24; goto case 11;
                    case 11: k2f ^= ((ulong)data[pos + 10]) << 16; goto case 10;
                    case 10: k2f ^= ((ulong)data[pos + 9]) << 8; goto case 9;
                    case 9: k2f ^= ((ulong)data[pos + 8]) << 0; goto case 8;
                    case 8: k1f ^= ((ulong)data[pos + 7]) << 56; goto case 7;
                    case 7: k1f ^= ((ulong)data[pos + 6]) << 48; goto case 6;
                    case 6: k1f ^= ((ulong)data[pos + 5]) << 40; goto case 5;
                    case 5: k1f ^= ((ulong)data[pos + 4]) << 32; goto case 4;
                    case 4: k1f ^= ((ulong)data[pos + 3]) << 24; goto case 3;
                    case 3: k1f ^= ((ulong)data[pos + 2]) << 16; goto case 2;
                    case 2: k1f ^= ((ulong)data[pos + 1]) << 8; goto case 1;
                    case 1: k1f ^= ((ulong)data[pos]) << 0; break;
                }
                if (length > 8)
                {
                    k2f *= c2; k2f = Rotl64(k2f, 33); k2f *= c1; h2 ^= k2f;
                }
                if (length > 0)
                {
                    k1f *= c1; k1f = Rotl64(k1f, 31); k1f *= c2; h1 ^= k1f;
                }
                h1 ^= (ulong)data.Length; h2 ^= (ulong)data.Length;
                h1 += h2; h2 += h1;
                h1 = FMix64(h1); h2 = FMix64(h2);
                h1 += h2; h2 += h1;

                byte[] result = new byte[16];
                Array.Copy(BitConverter.GetBytes(h1), 0, result, 0, 8);
                Array.Copy(BitConverter.GetBytes(h2), 0, result, 8, 8);
                return result;
            }

            /// <summary>
            /// Rotl64s the specified x.
            /// </summary>
            /// <param name="x">The x.</param>
            /// <param name="r">The r.</param>
            /// <returns>System.UInt64.</returns>
            private static ulong Rotl64(ulong x, byte r) => (x << r) | (x >> (64 - r));
            /// <summary>
            /// fs the mix64.
            /// </summary>
            /// <param name="k">The k.</param>
            /// <returns>System.UInt64.</returns>
            private static ulong FMix64(ulong k)
            {
                k ^= k >> 33;
                k *= 0xff51afd7ed558ccdL;
                k ^= k >> 33;
                k *= 0xc4ceb9fe1a85ec53L;
                k ^= k >> 33;
                return k;
            }
        }
    }

    /// <summary>
    /// SHA256哈希算法实现（更适合高安全需求，如加密密钥/区块链ID）。
    /// </summary>
    public class Sha256ObjectHasher : IObjectHasher
    {
        /// <summary>
        /// 计算SHA256哈希，并折叠为long编号。
        /// </summary>
        public long GetObjectHash(string input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            using var sha = SHA256.Create();
            byte[] data = Encoding.UTF8.GetBytes(input);
            byte[] hash = sha.ComputeHash(data);
            long code = BitConverter.ToInt64(hash, 0) ^ BitConverter.ToInt64(hash, 8) ^ BitConverter.ToInt64(hash, 16) ^ BitConverter.ToInt64(hash, 24);
            return code < 0 ? -code : code;
        }
    }

    /// <summary>
    /// 工程示例：将名字/词语/主键映射为唯一编号，支持注入不同哈希策略。
    /// </summary>
    public class RandomizedIdService
    {
        /// <summary>
        /// The hasher
        /// </summary>
        private readonly IObjectHasher _hasher;
        /// <summary>
        /// The bucket count
        /// </summary>
        private readonly int _bucketCount;

        /// <summary>
        /// 构造函数，注入哈希策略与编号空间。
        /// </summary>
        public RandomizedIdService(IObjectHasher hasher, int bucketCount = 10000019)
        {
            if (bucketCount <= 0) throw new ArgumentOutOfRangeException(nameof(bucketCount));
            _hasher = hasher ?? throw new ArgumentNullException(nameof(hasher));
            _bucketCount = bucketCount;
        }

        /// <summary>
        /// 获取对象的伪随机编号（主键空间内的下标）。
        /// </summary>
        /// <param name="input">名字、词语或业务主键</param>
        /// <returns>0~bucketCount-1内的编号</returns>
        public int GetRandomId(string input)
        {
            long code = _hasher.GetObjectHash(input);
            int idx = (int)(code % _bucketCount);
            return idx < 0 ? -idx : idx;
        }
    }



    /// <summary>
    /// RandomizedIdService 及哈希策略的单元测试（NUnit + XML注释 + Assert.That）。
    /// </summary>
    [TestFixture]
    public class RandomizedIdServiceTests
    {
        /// <summary>
        /// MurmurHash3 哈希策略可为同一输入输出相同编号（确定性）。
        /// </summary>
        [Test]
        public void MurmurHash3ObjectHasher_SameInput_SameHash()
        {
            var hasher = new MurmurHash3ObjectHasher();
            var id1 = hasher.GetObjectHash("Alice");
            var id2 = hasher.GetObjectHash("Alice");
            Assert.That(id1, Is.EqualTo(id2));
        }

        /// <summary>
        /// Sha256 哈希策略同理可保证确定性。
        /// </summary>
        [Test]
        public void Sha256ObjectHasher_SameInput_SameHash()
        {
            var hasher = new Sha256ObjectHasher();
            var id1 = hasher.GetObjectHash("Bob");
            var id2 = hasher.GetObjectHash("Bob");
            Assert.That(id1, Is.EqualTo(id2));
        }

        /// <summary>
        /// 不同输入应绝大多数情况下输出不同编号（低碰撞）。
        /// </summary>
        [Test]
        public void MurmurHash3ObjectHasher_DifferentInput_DifferentHash()
        {
            var hasher = new MurmurHash3ObjectHasher();
            var id1 = hasher.GetObjectHash("apple");
            var id2 = hasher.GetObjectHash("banana");
            Assert.That(id1, Is.Not.EqualTo(id2));
        }

        /// <summary>
        /// 哈希编号为非负数（便于分表、分区等场景）。
        /// </summary>
        [Test]
        public void MurmurHash3ObjectHasher_ResultIsNonNegative()
        {
            var hasher = new MurmurHash3ObjectHasher();
            var id = hasher.GetObjectHash("测试");
            Assert.That(id, Is.GreaterThanOrEqualTo(0));
        }

        /// <summary>
        /// RandomizedIdService 可为同一名字给出同样编号且不超分片空间。
        /// </summary>
        [Test]
        public void RandomizedIdService_SameInput_SameBucket()
        {
            var service = new RandomizedIdService(new MurmurHash3ObjectHasher(), bucketCount: 1021);
            int id1 = service.GetRandomId("Lucy");
            int id2 = service.GetRandomId("Lucy");
            Assert.That(id1, Is.EqualTo(id2));
            Assert.That(id1, Is.InRange(0, 1020));
        }

        /// <summary>
        /// RandomizedIdService 不同输入分布均匀，热点概率低。
        /// </summary>
        [Test]
        public void RandomizedIdService_DistributionIsUniform()
        {
            var service = new RandomizedIdService(new MurmurHash3ObjectHasher(), bucketCount: 101);
            int N = 5000;
            var buckets = new int[101];
            for (int i = 0; i < N; i++)
            {
                string name = "word" + i;
                int idx = service.GetRandomId(name);
                buckets[idx]++;
            }
            double avg = N / 101.0;
            double max = buckets.Max();
            double min = buckets.Min();
            // 检查没有桶为空，且最大/最小不相差太大（粗略均匀性）
            Assert.That(min, Is.GreaterThan(0));
            Assert.That(max / avg, Is.LessThan(2.0));
        }

        /// <summary>
        /// Null输入应抛异常，保障健壮性。
        /// </summary>
        [Test]
        public void MurmurHash3ObjectHasher_NullInput_Throws()
        {
            var hasher = new MurmurHash3ObjectHasher();
            Assert.That(() => hasher.GetObjectHash(null), Throws.TypeOf<ArgumentNullException>());
        }

        /// <summary>
        /// bucketCount非法时构造应抛异常。
        /// </summary>
        [Test]
        public void RandomizedIdService_InvalidBucketCount_Throws()
        {
            Assert.That(() => new RandomizedIdService(new MurmurHash3ObjectHasher(), 0), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => new RandomizedIdService(new Sha256ObjectHasher(), -1), Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        /// <summary>
        /// Sha256ObjectHasher 也能用于 RandomizedIdService 并给出编号。
        /// </summary>
        [Test]
        public void Sha256ObjectHasher_CanUseInRandomizedIdService()
        {
            var service = new RandomizedIdService(new Sha256ObjectHasher(), bucketCount: 1009);
            int idx = service.GetRandomId("bitcoin");
            Assert.That(idx, Is.InRange(0, 1008));
        }
    }


}
