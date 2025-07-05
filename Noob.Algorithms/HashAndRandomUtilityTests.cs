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
    using System.Security.Cryptography;
    using System.Text;

    /// <summary>
    /// 哈希算法与高质量随机数工具集（平台工程级组件）。
    /// </summary>
    public static class HashAndRandomUtility
    {
        /// <summary>
        /// 计算字符串的高质量SHA256哈希值（可用于数据查找、唯一标识、签名等场景）。
        /// </summary>
        /// <param name="input">需要哈希的字符串</param>
        /// <returns>哈希值的十六进制字符串</returns>
        /// <exception cref="ArgumentNullException">输入字符串为null时抛出</exception>
        public static string ComputeSha256Hash(string input)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            using (var sha256 = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(input);
                byte[] hashBytes = sha256.ComputeHash(bytes);
                // 转为十六进制字符串
                var sb = new StringBuilder(hashBytes.Length * 2);
                foreach (var b in hashBytes)
                    sb.AppendFormat("{0:x2}", b);
                return sb.ToString();
            }
        }

        /// <summary>
        /// 计算字符串的快速哈希（MurmurHash3-32位实现，适用于高性能查找、分布式一致性哈希等）。
        /// </summary>
        /// <param name="input">待哈希的字符串</param>
        /// <param name="seed">哈希种子（可选）</param>
        /// <returns>32位哈希值</returns>
        /// <exception cref="ArgumentNullException">输入字符串为null时抛出</exception>
        public static uint ComputeMurmurHash3(string input, uint seed = 0)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));
            // MurmurHash3算法简化版
            byte[] data = Encoding.UTF8.GetBytes(input);
            int len = data.Length;
            uint c1 = 0xcc9e2d51;
            uint c2 = 0x1b873593;
            uint h1 = seed;
            int roundedEnd = len & ~0x3;

            for (int i = 0; i < roundedEnd; i += 4)
            {
                uint k1 = (uint)(data[i] | (data[i + 1] << 8) | (data[i + 2] << 16) | (data[i + 3] << 24));
                k1 *= c1; k1 = Rotl32(k1, 15); k1 *= c2;

                h1 ^= k1;
                h1 = Rotl32(h1, 13);
                h1 = h1 * 5 + 0xe6546b64;
            }
            uint k2 = 0;
            switch (len & 3)
            {
                case 3: k2 ^= (uint)(data[roundedEnd + 2] << 16); goto case 2;
                case 2: k2 ^= (uint)(data[roundedEnd + 1] << 8); goto case 1;
                case 1:
                    k2 ^= data[roundedEnd];
                    k2 *= c1; k2 = Rotl32(k2, 15); k2 *= c2; h1 ^= k2;
                    break;
            }
            h1 ^= (uint)len;
            h1 = FMix(h1);
            return h1;
        }

        /// <summary>
        /// 获取线程安全的高质量随机数生成器（适用于安全密钥、Token、分布式唯一ID等）。
        /// </summary>
        /// <returns>可用于生成任意随机数的RandomNumberGenerator实例</returns>
        public static RandomNumberGenerator GetSecureRandomGenerator()
        {
            return RandomNumberGenerator.Create();
        }

        /// <summary>
        /// 生成指定范围的高质量伪随机整数。
        /// </summary>
        /// <param name="minValue">包含下限</param>
        /// <param name="maxValue">不包含上限</param>
        /// <returns>指定范围内的整数</returns>
        public static int NextSecureInt(int minValue, int maxValue)
        {
            if (minValue >= maxValue)
                throw new ArgumentException("minValue 必须小于 maxValue");
            using (var rng = RandomNumberGenerator.Create())
            {
                // 保证随机均匀分布
                var diff = (long)maxValue - minValue;
                var uint32Max = (long)uint.MaxValue + 1;
                var skip = (uint32Max - (uint32Max % diff));
                uint r;
                do
                {
                    var bytes = new byte[4];
                    rng.GetBytes(bytes);
                    r = BitConverter.ToUInt32(bytes, 0);
                } while (r >= skip);
                return (int)(minValue + (r % diff));
            }
        }

        /// <summary>
        /// 从一个列表中安全地随机采样若干元素（适合负载均衡、采样监控等）。
        /// </summary>
        /// <typeparam name="T">元素类型</typeparam>
        /// <param name="source">待采样的列表</param>
        /// <param name="count">采样数量</param>
        /// <returns>采样后的元素集合</returns>
        public static List<T> SecureRandomSample<T>(IList<T> source, int count)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (count < 0 || count > source.Count)
                throw new ArgumentOutOfRangeException(nameof(count));

            var indices = new HashSet<int>();
            var result = new List<T>(count);
            using (var rng = RandomNumberGenerator.Create())
            {
                while (indices.Count < count)
                {
                    int idx = NextSecureInt(0, source.Count);
                    if (indices.Add(idx))
                        result.Add(source[idx]);
                }
            }
            return result;
        }


        /// <summary>
        /// 内部辅助：左循环
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="r">The r.</param>
        /// <returns>System.UInt32.</returns>
        private static uint Rotl32(uint x, byte r)
        {
            return (x << r) | (x >> (32 - r));
        }

        /// <summary>
        /// MurmurHash3尾部混洗 
        /// </summary>
        /// <param name="h">The h.</param>
        /// <returns>System.UInt32.</returns>
        private static uint FMix(uint h)
        {
            h ^= h >> 16;
            h *= 0x85ebca6b;
            h ^= h >> 13;
            h *= 0xc2b2ae35;
            h ^= h >> 16;
            return h;
        }

        /// <summary>
        /// 计算整数key的乘法哈希值（生产环境推荐，适用于哈希表、分布式分片等场景）。
        /// 算法采用Knuth建议：A = (√5 - 1)/2 ≈ 0.6180339887，可保证较好均匀性。
        /// </summary>
        /// <param name="key">待哈希的整数key（可为负，建议已归一化）</param>
        /// <param name="bucketCount">哈希桶总数（建议为质数，必须大于0）</param>
        /// <returns>哈希桶编号（范围0~bucketCount-1）</returns>
        /// <exception cref="ArgumentOutOfRangeException">当bucketCount不大于0时抛出</exception>
        public static int ComputeMultiplicativeHash(int key, int bucketCount)
        {
            if (bucketCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(bucketCount), "桶数必须大于0");

            // 取绝对值，避免负数key影响分布
            long normalizedKey = Math.Abs((long)key);

            // 采用Knuth推荐A = (√5 - 1)/2 ≈ 0.6180339887，可保证较好均匀性
            double A = 0.6180339887;
            double frac = (normalizedKey * A) % 1; // 取小数部分
            int hash = (int)(bucketCount * frac);

            // 保证结果范围在[0, bucketCount-1]
            return hash < 0 ? 0 : (hash >= bucketCount ? bucketCount - 1 : hash);
        }
    }

    /// <summary>
    /// HashAndRandomUtility 工程级单元测试（NUnit + Xml 注释 + Assert.That）。
    /// </summary>
    [TestFixture]
    public class HashAndRandomUtilityTests
    {
        /// <summary>
        /// 验证 SHA256 哈希结果正确且一致。
        /// </summary>
        [Test]
        public void ComputeSha256Hash_InputIsConsistent_HashAlwaysEqual()
        {
            var input = "Google工程测试";
            var hash1 = HashAndRandomUtility.ComputeSha256Hash(input);
            var hash2 = HashAndRandomUtility.ComputeSha256Hash(input);
            Assert.That(hash1, Is.EqualTo(hash2));
            Assert.That(hash1.Length, Is.EqualTo(64));
        }

        /// <summary>
        /// SHA256 对不同输入结果应不同，区分度强。
        /// </summary>
        [Test]
        public void ComputeSha256Hash_DifferentInput_DifferentHash()
        {
            var h1 = HashAndRandomUtility.ComputeSha256Hash("A");
            var h2 = HashAndRandomUtility.ComputeSha256Hash("B");
            Assert.That(h1, Is.Not.EqualTo(h2));
        }

        /// <summary>
        /// MurmurHash3 哈希同输入一致，且能区分不同字符串。
        /// </summary>
        [Test]
        public void ComputeMurmurHash3_SameInput_AlwaysSameHash()
        {
            var input = "HashTest";
            var h1 = HashAndRandomUtility.ComputeMurmurHash3(input);
            var h2 = HashAndRandomUtility.ComputeMurmurHash3(input);
            Assert.That(h1, Is.EqualTo(h2));
        }

        /// <summary>
        /// MurmurHash3 不同输入哈希结果大概率不同。
        /// </summary>
        [Test]
        public void ComputeMurmurHash3_DifferentInput_DifferentHash()
        {
            var h1 = HashAndRandomUtility.ComputeMurmurHash3("Test1");
            var h2 = HashAndRandomUtility.ComputeMurmurHash3("Test2");
            Assert.That(h1, Is.Not.EqualTo(h2));
        }

        /// <summary>
        /// 获取安全随机数生成器实例，实例不为 null。
        /// </summary>
        [Test]
        public void GetSecureRandomGenerator_Instance_NotNull()
        {
            using (var rng = HashAndRandomUtility.GetSecureRandomGenerator())
            {
                Assert.That(rng, Is.Not.Null);
            }
        }

        /// <summary>
        /// 随机数生成结果应在指定区间，分布合理。
        /// </summary>
        [Test]
        public void NextSecureInt_GeneratesWithinRange()
        {
            int min = 10, max = 20;
            for (int i = 0; i < 50; i++)
            {
                int r = HashAndRandomUtility.NextSecureInt(min, max);
                Assert.That(r, Is.GreaterThanOrEqualTo(min));
                Assert.That(r, Is.LessThan(max));
            }
        }

        /// <summary>
        /// SecureRandomSample 能从集合中随机采样，采样数准确且元素唯一。
        /// </summary>
        [Test]
        public void SecureRandomSample_SampleCountCorrectAndUnique()
        {
            var list = new List<int>();
            for (int i = 0; i < 100; i++) list.Add(i);

            var sample = HashAndRandomUtility.SecureRandomSample(list, 10);
            Assert.That(sample.Count, Is.EqualTo(10));
            // 无重复
            Assert.That(new HashSet<int>(sample).Count, Is.EqualTo(10));
            // 每个元素都在原集合内
            foreach (var e in sample)
                Assert.That(list.Contains(e));
        }

        /// <summary>
        /// SecureRandomSample 抛出参数越界异常。
        /// </summary>
        [Test]
        public void SecureRandomSample_OutOfRange_Throws()
        {
            var list = new List<int>() { 1, 2, 3 };
            Assert.That(() => HashAndRandomUtility.SecureRandomSample(list, -1), Throws.Exception);
            Assert.That(() => HashAndRandomUtility.SecureRandomSample(list, 4), Throws.Exception);
        }

        /// <summary>
        /// ComputeSha256Hash 输入 null 抛出异常。
        /// </summary>
        [Test]
        public void ComputeSha256Hash_NullInput_ThrowsArgumentNullException()
        {
            Assert.That(() => HashAndRandomUtility.ComputeSha256Hash(null), Throws.ArgumentNullException);
        }

        /// <summary>
        /// ComputeMurmurHash3 输入 null 抛出异常。
        /// </summary>
        [Test]
        public void ComputeMurmurHash3_NullInput_ThrowsArgumentNullException()
        {
            Assert.That(() => HashAndRandomUtility.ComputeMurmurHash3(null), Throws.ArgumentNullException);
        }

        /// <summary>
        /// NextSecureInt 非法区间参数抛异常。
        /// </summary>
        [Test]
        public void NextSecureInt_InvalidRange_ThrowsArgumentException()
        {
            Assert.That(() => HashAndRandomUtility.NextSecureInt(10, 10), Throws.ArgumentException);
            Assert.That(() => HashAndRandomUtility.NextSecureInt(10, 5), Throws.ArgumentException);
        }

        /// <summary>
        /// 验证同key和同桶数多次哈希结果一致。
        /// </summary>
        [Test]
        public void ComputeMultiplicativeHash_SameKeyAndBucket_AlwaysSameResult()
        {
            int key = 123456789;
            int bucketCount = 101;
            int hash1 = HashAndRandomUtility.ComputeMultiplicativeHash(key, bucketCount);
            int hash2 = HashAndRandomUtility.ComputeMultiplicativeHash(key, bucketCount);
            Assert.That(hash1, Is.EqualTo(hash2));
            Assert.That(hash1, Is.GreaterThanOrEqualTo(0));
            Assert.That(hash1, Is.LessThan(bucketCount));
        }

        /// <summary>
        /// 验证不同key哈希结果分布大概率不同（碰撞概率低）。
        /// </summary>
        [Test]
        public void ComputeMultiplicativeHash_DifferentKey_DifferentHash()
        {
            int bucketCount = 97;
            int hash1 = HashAndRandomUtility.ComputeMultiplicativeHash(1001, bucketCount);
            int hash2 = HashAndRandomUtility.ComputeMultiplicativeHash(2002, bucketCount);
            Assert.That(hash1, Is.Not.EqualTo(hash2));
        }

        /// <summary>
        /// 验证负数key输入结果合法。
        /// </summary>
        [Test]
        public void ComputeMultiplicativeHash_NegativeKey_ResultInRange()
        {
            int hash = HashAndRandomUtility.ComputeMultiplicativeHash(-12345, 19);
            Assert.That(hash, Is.GreaterThanOrEqualTo(0));
            Assert.That(hash, Is.LessThan(19));
        }

        /// <summary>
        /// 验证桶数为1时，哈希结果始终为0。
        /// </summary>
        [Test]
        public void ComputeMultiplicativeHash_BucketCountOne_AlwaysZero()
        {
            int hash = HashAndRandomUtility.ComputeMultiplicativeHash(98765, 1);
            Assert.That(hash, Is.EqualTo(0));
        }

        /// <summary>
        /// 验证非法桶数抛出异常。
        /// </summary>
        [Test]
        public void ComputeMultiplicativeHash_InvalidBucketCount_Throws()
        {
            Assert.That(() => HashAndRandomUtility.ComputeMultiplicativeHash(123, 0), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => HashAndRandomUtility.ComputeMultiplicativeHash(123, -10), Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        /// <summary>
        /// 验证高密度key分布，哈希值均匀性基本达标（工程可用性测试）。
        /// </summary>
        [Test]
        public void ComputeMultiplicativeHash_HighDensityKey_UniformDistribution()
        {
            int bucketCount = 100;
            int[] counts = new int[bucketCount];
            int sampleSize = 10000;
            for (int i = 0; i < sampleSize; i++)
            {
                int hash = HashAndRandomUtility.ComputeMultiplicativeHash(i, bucketCount);
                counts[hash]++;
            }
            // 简单判定：每个桶数量在平均值的80%-120%之间
            double avg = (double)sampleSize / bucketCount;
            foreach (int c in counts)
            {
                Assert.That(c, Is.InRange(avg * 0.8, avg * 1.2));
            }
        }
    }

}
