using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Noob.Algorithms
{
    /// <summary>
    /// 平台工程级文本相似度智能分派器（可扩展/可测试/自动适配）
    /// </summary>
    public class TextSimilarityDispatcher
    {
        /// <summary>
        /// The tokenizer
        /// </summary>
        private readonly ITextTokenizer _tokenizer;

        /// <summary>
        /// 文本长度阈值（短文本优先用余弦/MinHash，长文本用SimHash，默认30）
        /// </summary>
        public int ShortTextThreshold { get; }

        /// <summary>
        /// Gets the middle text threshold.
        /// </summary>
        /// <value>The middle text threshold.</value>
        public int MiddleTextThreshold { get; }

        /// <summary>
        /// The simhash
        /// </summary>
        private readonly SimHashFingerprint _simhash;

        /// <summary>
        /// The minhash
        /// </summary>
        private readonly MinHashCalculator _minhash;

        /// <summary>
        /// 构造函数，可注入分派阈值与各算法参数
        /// </summary>
        public TextSimilarityDispatcher(ITextTokenizer tokenizer, int shortTextThreshold = 30, int middleTextThreshold = 120, int simhashBits = 64, int minhashFunctions = 128)
        {
            _tokenizer = tokenizer ?? throw new ArgumentNullException(nameof(tokenizer));   

            ShortTextThreshold = shortTextThreshold;
            MiddleTextThreshold = middleTextThreshold;
            _simhash = new SimHashFingerprint(tokenizer, simhashBits);
            _minhash = new MinHashCalculator(tokenizer, minhashFunctions);
        }

        /// <summary>
        /// 智能判别文本对的相似度（算法自动分派）
        /// </summary>
        /// <param name="text1">文本1</param>
        /// <param name="text2">文本2</param>
        /// <returns>相似度（0~1），越接近1越相似</returns>
        public double ComputeSimilarity(string text1, string text2)
        {
            if (string.IsNullOrWhiteSpace(text1) || string.IsNullOrWhiteSpace(text2)) return 0;

            int len1 = text1?.Length ?? 0;
            int len2 = text2?.Length ?? 0;
            int avgLen = (len1 + len2) / 2;

            // 极短文本用余弦相似度（抗乱序最强）
            if (avgLen < ShortTextThreshold)
            {
                return CosineSimilarityCalculator.ComputeCosineSimilarity(_tokenizer, text1, text2);
            }
            // 中等长度用MinHash+Jaccard（对乱序、部分增删鲁棒）
            else if (avgLen < MiddleTextThreshold)
            {
                return _minhash.ComputeJaccardSimilarity(text1, text2);
            }
            // 长文本用SimHash（效率极高，适合大规模分布式查重）
            else
            {
                ulong h1 = _simhash.ComputeSimHash(text1);
                ulong h2 = _simhash.ComputeSimHash(text2);
                int hamming = _simhash.ComputeHammingDistance(h1, h2);
                // 汉明距离映射为相似度
                double sim = 1.0 - (double)hamming / _simhash.HashBitLength;
                return Math.Max(0.0, Math.Min(1.0, sim));
            }
        }

        /// <summary>
        /// 返回自动选择的底层算法（方便调试或日志分析）
        /// </summary>
        public string WhichAlgorithm(string text1, string text2)
        {
            int len1 = text1?.Length ?? 0;
            int len2 = text2?.Length ?? 0;
            int avgLen = (len1 + len2) / 2;
            if (avgLen < ShortTextThreshold) return "CosineSimilarity";
            if (avgLen < MiddleTextThreshold) return "MinHash+Jaccard";
            return "SimHash";
        }
    }

    /// <summary>
    /// 文本分词器接口，支持平台级多策略替换
    /// </summary>
    public interface ITextTokenizer
    {
        /// <summary>
        /// 将输入文本拆解为特征Token（如字、词、N-gram等）
        /// </summary>
        /// <param name="text">输入文本</param>
        /// <returns>分词结果Token列表</returns>
        List<string> Tokenize(string text);
    }

    /// <summary>
    /// 逐字分词器（适用于中文短文本场景）
    /// </summary>
    public class CharTokenizer : ITextTokenizer
    {
        /// <summary>
        /// 将输入文本拆解为特征Token（如字、词、N-gram等）
        /// </summary>
        /// <param name="text">输入文本</param>
        /// <returns>分词结果Token列表</returns>
        public List<string> Tokenize(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return new List<string>();
            return text.ToCharArray().Select(c => c.ToString()).ToList();
        }
    }

    /// <summary>
    /// 英文基础分词器（按空格和常见符号切分）
    /// </summary>
    public class BasicEnglishTokenizer : ITextTokenizer
    {
        /// <summary>
        /// 将输入文本拆解为特征Token（如字、词、N-gram等）
        /// </summary>
        /// <param name="text">输入文本</param>
        /// <returns>分词结果Token列表</returns>
        public List<string> Tokenize(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return new List<string>();
            return text
                .ToLowerInvariant()
                .Split(new[] { ' ', '\t', '\r', '\n', '.', ',', '!', '?', ';', ':', '-', '_', '/', '\\', '(', ')', '[', ']', '\'', '\"' }, StringSplitOptions.RemoveEmptyEntries)
                .ToList();
        }
    }

    /// <summary>
    /// N-Gram 分词器（适合中长文本、可设置窗口大小）
    /// </summary>
    public class NGramTokenizer : ITextTokenizer
    {
        /// <summary>
        /// The n
        /// </summary>
        private readonly int _n;

        /// <summary>
        /// Initializes a new instance of the <see cref="NGramTokenizer"/> class.
        /// </summary>
        /// <param name="n">The n.</param>
        /// <exception cref="System.ArgumentOutOfRangeException">n - N应大于0</exception>
        public NGramTokenizer(int n = 2)
        {
            if (n < 1) throw new ArgumentOutOfRangeException(nameof(n), "N应大于0");
            _n = n;
        }

        /// <summary>
        /// 将输入文本拆解为特征Token（如字、词、N-gram等）
        /// </summary>
        /// <param name="text">输入文本</param>
        /// <returns>分词结果Token列表</returns>
        public List<string> Tokenize(string text)
        {
            var result = new List<string>();
            if (string.IsNullOrWhiteSpace(text)) return result;
            var chars = text.ToCharArray();
            for (int i = 0; i <= chars.Length - _n; i++)
            {
                result.Add(new string(chars, i, _n));
            }
            return result;
        }
    }

    /// <summary>
    /// Jieba中文分词适配器（需要安装JiebaNet包）
    /// </summary>
    public class JiebaTokenizer : ITextTokenizer
    {
        public List<string> Tokenize(string text)
        {
            // 示例：集成JiebaNet分词库
            if (string.IsNullOrWhiteSpace(text)) return new List<string>();
            // return JiebaNet.Segmenter.JiebaSegmenter.Cut(text).ToList();
            throw new NotImplementedException("需集成实际Jieba分词逻辑");
        }
    }

    #region SimHash 实现

    /// <summary>
    /// SimHash 指纹算法（平台工程级，可扩展/可测试）
    /// </summary>
    public class SimHashFingerprint
    {
        /// <summary>
        /// The tokenizer
        /// </summary>
        private readonly ITextTokenizer _tokenizer;

        /// <summary>
        /// Gets the length of the hash bit.
        /// </summary>
        /// <value>The length of the hash bit.</value>
        public int HashBitLength { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SimHashFingerprint"/> class.
        /// </summary>
        /// <param name="tokenizer">The tokenizer.</param>
        /// <param name="hashBitLength">Length of the hash bit.</param>
        /// <exception cref="System.ArgumentOutOfRangeException">hashBitLength - SimHash 位数范围32~256</exception>
        public SimHashFingerprint(ITextTokenizer tokenizer, int hashBitLength = 64)
        {
            _tokenizer = tokenizer ?? throw new ArgumentNullException(nameof(tokenizer));

            if (hashBitLength < 32 || hashBitLength > 256)
                throw new ArgumentOutOfRangeException(nameof(hashBitLength), "SimHash 位数范围32~256");
            HashBitLength = hashBitLength;
        }

        /// <summary>
        /// 计算文本内容的SimHash指纹
        /// </summary>
        public ulong ComputeSimHash(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return 0UL;
            var tokens = _tokenizer.Tokenize(text);
            var weights = tokens.GroupBy(t => t).ToDictionary(g => g.Key, g => (double)g.Count());
            var vector = new double[HashBitLength];
            foreach (var kv in weights)
            {
                ulong wordHash = Fnv1aHash64(kv.Key);
                for (int i = 0; i < HashBitLength; i++)
                {
                    int bit = ((wordHash >> i) & 1) == 1 ? 1 : -1;
                    vector[i] += bit * kv.Value;
                }
            }
            ulong simhash = 0;
            for (int i = 0; i < HashBitLength; i++)
                if (vector[i] > 0)
                    simhash |= (1UL << i);
            return simhash;
        }

        /// <summary>
        /// 计算两个SimHash之间的汉明距离
        /// </summary>
        public int ComputeHammingDistance(ulong hash1, ulong hash2)
        {
            ulong x = hash1 ^ hash2;
            int count = 0;
            while (x != 0) { count++; x &= (x - 1); }
            return count;
        }

        /// <summary>
        /// Fnv1as the hash64.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns>System.UInt64.</returns>
        private static ulong Fnv1aHash64(string data)
        {
            const ulong fnvOffset = 14695981039346656037;
            const ulong fnvPrime = 1099511628211;
            ulong hash = fnvOffset;
            foreach (char c in data) { hash ^= c; hash *= fnvPrime; }
            return hash;
        }
    }

    #endregion

    #region MinHash+Jaccard 实现

    /// <summary>
    /// Class MinHashCalculator.
    /// </summary>
    public class MinHashCalculator
    {
        /// <summary>
        /// The tokenizer
        /// </summary>
        private readonly ITextTokenizer _tokenizer;

        /// <summary>
        /// Gets the hash function count.
        /// </summary>
        /// <value>The hash function count.</value>
        public int HashFunctionCount { get; }

        /// <summary>
        /// The hash functions
        /// </summary>
        private readonly List<Func<string, uint>> _hashFunctions;

        /// <summary>
        /// Initializes a new instance of the <see cref="MinHashCalculator"/> class.
        /// </summary>
        /// <param name="tokenizer">The tokenizer.</param>
        /// <param name="hashFunctionCount">The hash function count.</param>
        /// <exception cref="System.ArgumentOutOfRangeException">hashFunctionCount - 哈希函数数量建议16~512</exception>
        public MinHashCalculator(ITextTokenizer tokenizer, int hashFunctionCount = 128)
        {
            _tokenizer = tokenizer ?? throw new ArgumentNullException(nameof(tokenizer));

            if (hashFunctionCount < 16 || hashFunctionCount > 512)
                throw new ArgumentOutOfRangeException(nameof(hashFunctionCount), "哈希函数数量建议16~512");
            HashFunctionCount = hashFunctionCount;
            _hashFunctions = BuildHashFunctions(hashFunctionCount);
        }

        /// <summary>
        /// Computes the minimum hash.
        /// </summary>
        /// <param name="tokens">The tokens.</param>
        /// <returns>System.UInt32[].</returns>
        public uint[] ComputeMinHash(IEnumerable<string> tokens)
        {
            var minhash = Enumerable.Repeat(uint.MaxValue, HashFunctionCount).ToArray();
            foreach (var token in tokens.Distinct())
            {
                for (int i = 0; i < HashFunctionCount; i++)
                {
                    uint hash = _hashFunctions[i](token);
                    if (hash < minhash[i]) minhash[i] = hash;
                }
            }
            return minhash;
        }

        /// <summary>
        /// Computes the jaccard similarity.
        /// </summary>
        /// <param name="sig1">The sig1.</param>
        /// <param name="sig2">The sig2.</param>
        /// <returns>System.Double.</returns>
        /// <exception cref="System.ArgumentException">特征长度不一致</exception>
        public static double ComputeJaccardSimilarity(uint[] sig1, uint[] sig2)
        {
            if (sig1.Length != sig2.Length) throw new ArgumentException("特征长度不一致");
            int equal = 0;
            for (int i = 0; i < sig1.Length; i++)
                if (sig1[i] == sig2[i]) equal++;
            return (double)equal / sig1.Length;
        }

        /// <summary>
        /// Computes the jaccard similarity.
        /// </summary>
        /// <param name="text1">The text1.</param>
        /// <param name="text2">The text2.</param>
        /// <returns>System.Double.</returns>
        public double ComputeJaccardSimilarity(string text1, string text2)
        {
            var tokens1 = _tokenizer.Tokenize(text1);
            var tokens2 = _tokenizer.Tokenize(text2);
            var m1 = ComputeMinHash(tokens1);
            var m2 = ComputeMinHash(tokens2);
            return ComputeJaccardSimilarity(m1, m2);
        }


        /// <summary>
        /// Builds the hash functions.
        /// </summary>
        /// <param name="count">The count.</param>
        /// <returns>List&lt;Func&lt;System.String, System.UInt32&gt;&gt;.</returns>
        private static List<Func<string, uint>> BuildHashFunctions(int count)
        {
            var funcs = new List<Func<string, uint>>();
            var rand = new Random(2024);
            for (int i = 0; i < count; i++)
            {
                uint a = (uint)rand.Next(1, int.MaxValue);
                uint b = (uint)rand.Next(0, int.MaxValue);
                funcs.Add(s =>
                {
                    unchecked
                    {
                        uint hash = 2166136261;
                        foreach (char c in s)
                            hash = (hash ^ c) * 16777619;
                        return a * hash + b;
                    }
                });
            }
            return funcs;
        }
    }
    #endregion

    #region 余弦相似度实现

    /// <summary>
    /// Class CosineSimilarityCalculator.
    /// </summary>
    public static class CosineSimilarityCalculator
    {
        /// <summary>
        /// Computes the cosine similarity.
        /// </summary>
        /// <param name="tokenizer">The tokenizer.</param>
        /// <param name="text1">The text1.</param>
        /// <param name="text2">The text2.</param>
        /// <returns>System.Double.</returns>
        public static double ComputeCosineSimilarity(ITextTokenizer tokenizer, string text1, string text2)
        {
            var v1 = ToVector(tokenizer.Tokenize(text1));
            var v2 = ToVector(tokenizer.Tokenize(text2));
            return ComputeCosineSimilarity(v1, v2);
        }

        /// <summary>
        /// Computes the cosine similarity.
        /// </summary>
        /// <param name="v1">The v1.</param>
        /// <param name="v2">The v2.</param>
        /// <returns>System.Double.</returns>
        public static double ComputeCosineSimilarity(IDictionary<string, int> v1, IDictionary<string, int> v2)
        {
            double dot = 0, norm1 = 0, norm2 = 0;
            foreach (var key in v1.Keys.Union(v2.Keys))
            {
                int x = v1.ContainsKey(key) ? v1[key] : 0;
                int y = v2.ContainsKey(key) ? v2[key] : 0;
                dot += x * y;
                norm1 += x * x;
                norm2 += y * y;
            }
            if (norm1 == 0 || norm2 == 0) return 0;
            return dot / (Math.Sqrt(norm1) * Math.Sqrt(norm2));
        }

        /// <summary>
        /// Converts to vector.
        /// </summary>
        /// <param name="tokens">The tokens.</param>
        /// <returns>Dictionary&lt;System.String, System.Int32&gt;.</returns>
        private static Dictionary<string, int> ToVector(List<string> tokens)
        {
            var dict = new Dictionary<string, int>();
            foreach (var t in tokens)
            {
                if (!dict.ContainsKey(t)) dict[t] = 0;
                dict[t]++;
            }
            return dict;
        }

    }
    #endregion



    /// <summary>
    /// TextSimilarityDispatcher 智能分派文本相似度算法单元测试
    /// </summary>
    [TestFixture]
    public class TextSimilarityDispatcherTests
    {
        /// <summary>
        /// 极短文本自动使用余弦相似度
        /// </summary>
        [Test]
        public void ShortText_UsesCosineSimilarity()
        {
            var dispatcher = new TextSimilarityDispatcher(new CharTokenizer(),shortTextThreshold: 15, middleTextThreshold: 40);
            string t1 = "云原生平台";
            string t2 = "平台云原生";
            double sim = dispatcher.ComputeSimilarity(t1, t2);
            string algo = dispatcher.WhichAlgorithm(t1, t2);

            Assert.That(algo, Is.EqualTo("CosineSimilarity"));
            Assert.That(sim, Is.GreaterThan(0.95), $"短文本乱序应高度相似, 实际: {sim}");
        }

        /// <summary>
        /// 中等长度文本自动使用 MinHash+Jaccard，相似文本应有较高相似度
        /// </summary>
        [Test]
        public void MiddleText_UsesMinHashJaccard()
        {
            var dispatcher = new TextSimilarityDispatcher(new CharTokenizer(),shortTextThreshold: 10, middleTextThreshold: 60);
            string t1 = "分布式索引技术是大数据搜索的核心 支持大规模近似查重";
            string t2 = "大规模查重支持 分布式索引是大数据搜索的核心";
            double sim = dispatcher.ComputeSimilarity(t1, t2);
            string algo = dispatcher.WhichAlgorithm(t1, t2);

            Assert.That(algo, Is.EqualTo("MinHash+Jaccard"));
            Assert.That(sim, Is.GreaterThan(0.7), $"乱序中等文本应Jaccard较高, 实际: {sim}");
        }

        /// <summary>
        /// 长文本自动使用 SimHash，相似文本汉明距离映射相似度高
        /// </summary>
        [Test]
        public void LongText_UsesSimHash()
        {
            var dispatcher = new TextSimilarityDispatcher(new NGramTokenizer(),shortTextThreshold: 15, middleTextThreshold: 50);
            string t1 = "分布式索引技术是大数据搜索的核心。通过多级倒排索引与并行架构，平台能够在PB级别数据中实现秒级去重和查找。";
            string t2 = "平台通过多级倒排索引和并行架构，实现PB级数据的秒级去重和查找。这正是分布式索引技术成为大数据搜索核心的原因。";
            double sim = dispatcher.ComputeSimilarity(t1, t2);
            string algo = dispatcher.WhichAlgorithm(t1, t2);

            Assert.That(algo, Is.EqualTo("SimHash"));
            Assert.That(sim, Is.GreaterThan(0.7), $"相似长文本SimHash相似度应高, 实际: {sim}");
        }

        /// <summary>
        /// 完全不同文本自动选择算法后应判定极低相似度
        /// </summary>
        [TestCase("区块链安全与共识机制", "蛋炒饭的做法与家常菜秘籍")]
        [TestCase("分布式AI大模型训练平台", "体育赛事直播和足彩分析")]
        public void CompletelyDifferentText_SimilarityLow(string t1, string t2)
        {
            var dispatcher = new TextSimilarityDispatcher(new NGramTokenizer(),shortTextThreshold: 10, middleTextThreshold: 40);
            double sim = dispatcher.ComputeSimilarity(t1, t2);
            Assert.That(sim, Is.LessThan(0.3), $"完全不同文本应极低相似度, 实际: {sim}");
        }

        /// <summary>
        /// 算法分派逻辑边界判定
        /// </summary>
        [Test]
        public void Dispatcher_AlgorithmSwitchingBoundary()
        {
            var dispatcher = new TextSimilarityDispatcher(new CharTokenizer(), shortTextThreshold: 10, middleTextThreshold: 20);
            string shortT1 = "AI平台";
            string shortT2 = "平台AI";
            string midT1 = new string('A', 15);
            string midT2 = new string('A', 15);
            string longT1 = new string('A', 25);
            string longT2 = new string('A', 25);

            Assert.That(dispatcher.WhichAlgorithm(shortT1, shortT2), Is.EqualTo("CosineSimilarity"));
            Assert.That(dispatcher.WhichAlgorithm(midT1, midT2), Is.EqualTo("MinHash+Jaccard"));
            Assert.That(dispatcher.WhichAlgorithm(longT1, longT2), Is.EqualTo("SimHash"));
        }

        /// <summary>
        /// 边界条件和极端输入鲁棒性
        /// </summary>
        [Test]
        public void EdgeCase_EmptyOrNullInput()
        {
            var dispatcher = new TextSimilarityDispatcher(new CharTokenizer());
            Assert.That(dispatcher.ComputeSimilarity("", ""), Is.EqualTo(0));
            Assert.That(dispatcher.ComputeSimilarity(null, ""), Is.EqualTo(0));
            Assert.That(dispatcher.ComputeSimilarity("", null), Is.EqualTo(0));
            Assert.That(dispatcher.ComputeSimilarity(null, null), Is.EqualTo(0));
        }
    }

}
