using NUnit.Framework;
using NUnit.Framework.Constraints;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Noob.Algorithms.Indexs
{
    /// <summary>
    /// 文本分词器接口，支持不同语言/业务场景下的分词算法替换。
    /// </summary>
    public interface ITextTokenizer
    {
        /// <summary>
        /// 对指定文本进行分词，返回所有词条（Token）。
        /// </summary>
        /// <param name="text">待分词的文本内容。</param>
        /// <returns>分词结果，Token集合。</returns>
        IReadOnlyList<string> Tokenize(string text);
    }

    /// <summary>
    /// 简单英文/中英文通用分词器，按空格和标点切分。
    /// </summary>
    public class BasicTextTokenizer : ITextTokenizer
    {
        /// <summary>
        /// The token pattern
        /// </summary>
        private static readonly Regex TokenPattern = new Regex(@"\w+", RegexOptions.Compiled);

        /// <summary>
        /// 对指定文本进行分词，返回所有词条（Token）。
        /// </summary>
        /// <param name="text">待分词的文本内容。</param>
        /// <returns>分词结果，Token集合。</returns>
        public IReadOnlyList<string> Tokenize(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return new List<string>();
            var matches = TokenPattern.Matches(text.ToLowerInvariant());
            var tokens = new List<string>(matches.Count);
            foreach (Match match in matches)
                tokens.Add(match.Value);
            return tokens;
        }
    }

    /// <summary>
    /// 内存倒排全文索引，支持增、删、查，适用于中小规模数据和平台型二次开发。
    /// </summary>
    public class InMemoryFullTextIndex
    {
        /// <summary>
        /// 倒排表结构，词条（Token）指向包含该词条的文档ID集合。
        /// </summary>
        private readonly ConcurrentDictionary<string, HashSet<string>> _invertedIndex =
            new ConcurrentDictionary<string, HashSet<string>>();

        /// <summary>
        /// 文档原文存储，支持高亮、重建索引等扩展场景。
        /// </summary>
        private readonly ConcurrentDictionary<string, string> _documents =
            new ConcurrentDictionary<string, string>();

        /// <summary>
        /// The tokenizer
        /// </summary>
        private readonly ITextTokenizer _tokenizer;

        /// <summary>
        /// 构造函数，注入分词器以支持可扩展的分词逻辑。
        /// </summary>
        /// <param name="tokenizer">文本分词器实例。</param>
        public InMemoryFullTextIndex(ITextTokenizer tokenizer)
        {
            _tokenizer = tokenizer ?? throw new ArgumentNullException(nameof(tokenizer));
        }

        /// <summary>
        /// 向全文索引中添加/更新一个文档。
        /// </summary>
        /// <param name="docId">文档唯一标识符。</param>
        /// <param name="content">文档文本内容。</param>
        public void AddOrUpdateDocument(string docId, string content)
        {
            if (string.IsNullOrWhiteSpace(docId))
                throw new ArgumentException("文档ID不能为空", nameof(docId));

            if (content == null)
                content = string.Empty;

            // 若已存在，先删除旧索引
            if (_documents.ContainsKey(docId))
                RemoveDocument(docId);

            _documents[docId] = content;
            var tokens = _tokenizer.Tokenize(content).Distinct();
            foreach (var token in tokens)
            {
                var set = _invertedIndex.GetOrAdd(token, _ => new HashSet<string>());
                lock (set) { set.Add(docId); }
            }
        }

        /// <summary>
        /// 从全文索引中删除一个文档。
        /// </summary>
        /// <param name="docId">文档唯一标识符。</param>
        public void RemoveDocument(string docId)
        {
            if (!_documents.TryRemove(docId, out var oldContent))
                return;

            var tokens = _tokenizer.Tokenize(oldContent).Distinct();
            foreach (var token in tokens)
            {
                if (_invertedIndex.TryGetValue(token, out var set))
                {
                    lock (set)
                    {
                        set.Remove(docId);
                        if (set.Count == 0)
                            _invertedIndex.TryRemove(token, out _);
                    }
                }
            }
        }

        /// <summary>
        /// 根据关键字查询包含全部关键词的文档ID集合。
        /// </summary>
        /// <param name="keywords">查询关键词，支持多个词。</param>
        /// <returns>所有匹配文档ID。</returns>
        public IReadOnlyCollection<string> Search(params string[] keywords)
        {
            if (keywords == null || keywords.Length == 0)
                return Array.Empty<string>();

            HashSet<string> result = null;
            foreach (var word in keywords.Distinct())
            {
                if (_invertedIndex.TryGetValue(word, out var set))
                {
                    if (result == null)
                        result = new HashSet<string>(set);
                    else
                        result.IntersectWith(set);
                }
                else
                {
                    // 任一关键词无结果，则整体无命中
                    return Array.Empty<string>();
                }
            }

            if (result == null)
                return Array.Empty<string>();

            return result.ToList();
        }

        /// <summary>
        /// 获取文档原文（如需高亮等场景）。
        /// </summary>
        /// <param name="docId">文档ID。</param>
        /// <returns>原始内容，无则返回null。</returns>
        public string GetDocument(string docId)
        {
            _documents.TryGetValue(docId, out var content);
            return content;
        }
    }

    /// <summary>
    /// 针对 InMemoryFullTextIndex 的单元测试套件，验证全文索引的增删查各项功能。
    /// </summary>
    [TestFixture]
    public class InMemoryFullTextIndexTests
    {
        private InMemoryFullTextIndex _index;

        /// <summary>
        /// 每次测试前初始化新的全文索引和基础分词器。
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            _index = new InMemoryFullTextIndex(new BasicTextTokenizer());
        }

        /// <summary>
        /// 验证单文档添加和关键词检索基本功能。
        /// </summary>
        [Test]
        public void AddOrUpdateDocument_And_Search_Works_Correctly()
        {
            // 添加文档
            _index.AddOrUpdateDocument("doc1", "The quick brown fox jumps over the lazy dog");

            // 单关键词查询
            var result1 = _index.Search("fox");
            Assert.That(result1, Is.EquivalentTo(new[] { "doc1" }));

            // 多关键词查询
            var result2 = _index.Search("quick", "dog");
            Assert.That(result2, Is.EquivalentTo(new[] { "doc1" }));

            // 查询不存在关键词
            var result3 = _index.Search("cat");
            Assert.That(result3, Is.Empty);
        }

        /// <summary>
        /// 验证多文档添加、不同关键词命中多文档。
        /// </summary>
        [Test]
        public void MultipleDocuments_Supports_CorrectMatching()
        {
            _index.AddOrUpdateDocument("doc1", "apple orange banana");
            _index.AddOrUpdateDocument("doc2", "banana grape lemon");
            _index.AddOrUpdateDocument("doc3", "orange lemon grapefruit");

            // "orange"应命中doc1和doc3
            var result1 = _index.Search("orange");
            Assert.That(result1, Is.EquivalentTo(new[] { "doc1", "doc3" }));

            // "lemon"应命中doc2和doc3
            var result2 = _index.Search("lemon");
            Assert.That(result2, Is.EquivalentTo(new[] { "doc2", "doc3" }));

            // "banana"应命中doc1和doc2
            var result3 = _index.Search("banana");
            Assert.That(result3, Is.EquivalentTo(new[] { "doc1", "doc2" }));

            // "banana"和"lemon"组合应命中doc2
            var result4 = _index.Search("banana", "lemon");
            Assert.That(result4, Is.EquivalentTo(new[] { "doc2" }));

            // 多关键词都未命中则为空
            var result5 = _index.Search("grape", "apple", "grapefruit");
            Assert.That(result5, Is.Empty);
        }

        /// <summary>
        /// 验证添加、删除文档后，索引自动维护，不会返回已删除文档。
        /// </summary>
        [Test]
        public void RemoveDocument_DeletesFromIndex()
        {
            _index.AddOrUpdateDocument("doc1", "sun moon stars");
            _index.AddOrUpdateDocument("doc2", "sun rain cloud");
            _index.RemoveDocument("doc1");

            var result = _index.Search("sun");
            Assert.That(result, Is.EquivalentTo(new[] { "doc2" }));

            // 删除不存在文档不会抛异常
            Assert.DoesNotThrow(() => _index.RemoveDocument("not_exist_doc"));
        }

        /// <summary>
        /// 验证重复添加同一文档时，索引更新无冗余。
        /// </summary>
        [Test]
        public void AddOrUpdateDocument_Overwrite_WorksCorrectly()
        {
            _index.AddOrUpdateDocument("doc1", "red green blue");
            _index.AddOrUpdateDocument("doc1", "blue yellow");

            // 只应命中最新内容
            var result1 = _index.Search("green");
            Assert.That(result1, Is.Empty);
            var result2 = _index.Search("blue");
            Assert.That(result2, Is.EquivalentTo(new[] { "doc1" }));
            var result3 = _index.Search("yellow");
            Assert.That(result3, Is.EquivalentTo(new[] { "doc1" }));
        }

        /// <summary>
        /// 验证边界情况（空文档、空/全空关键词、重复Token）行为合理。
        /// </summary>
        [Test]
        public void EdgeCases_HandleCorrectly()
        {
            _index.AddOrUpdateDocument("doc1", "a a a b b c");

            // 空内容文档也可被添加和删除
            _index.AddOrUpdateDocument("doc2", "");
            var result1 = _index.Search("a");
            Assert.That(result1, Is.EquivalentTo(new[] { "doc1" }));

            // 空关键词查询应返回空
            Assert.That(_index.Search(), Is.Empty);

            // 重复Token无影响
            var result2 = _index.Search("a", "a", "b");
            Assert.That(result2, Is.EquivalentTo(new[] { "doc1" }));
        }

        /// <summary>
        /// 验证获取文档原文的接口功能。
        /// </summary>
        [Test]
        public void GetDocument_ReturnsCorrectContent()
        {
            _index.AddOrUpdateDocument("doc1", "China is a beautiful country.");
            var content = _index.GetDocument("doc1");
            Assert.That(content, Is.EqualTo("China is a beautiful country."));

            Assert.That(_index.GetDocument("not_exist"), Is.Null);
        }
    }

}
