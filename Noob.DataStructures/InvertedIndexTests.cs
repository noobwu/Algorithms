using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Noob.DataStructures
{
   

    /// <summary>
    /// 生产平台级倒排索引，支持高效的短语及近邻短语检索。
    /// </summary>
    public class InvertedIndex
    {
        /// <summary>
        /// 词项（Term）到 Posting 列表的并发安全映射（倒排表）。
        /// 适配多线程/分布式环境，推荐使用 ConcurrentDictionary。
        /// </summary>
        private readonly ConcurrentDictionary<string, List<Posting>> _termToPostings =
            new ConcurrentDictionary<string, List<Posting>>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// 向倒排索引批量添加一个文档及其所有词项与位置信息。
        /// </summary>
        /// <param name="documentId">文档唯一标识</param>
        /// <param name="termsWithPositions">词项及其在文档中出现的位置集合</param>
        /// <exception cref="ArgumentNullException">参数为null时抛出</exception>
        /// <exception cref="ArgumentException">文档ID非法时抛出</exception>
        public void AddDocument(string documentId, IDictionary<string, List<int>> termsWithPositions)
        {
            if (string.IsNullOrWhiteSpace(documentId))
                throw new ArgumentException("文档ID不能为空", nameof(documentId));
            if (termsWithPositions == null)
                throw new ArgumentNullException(nameof(termsWithPositions));

            foreach (var kv in termsWithPositions)
            {
                var term = kv.Key;
                var positions = kv.Value ?? new List<int>();
                _termToPostings.AddOrUpdate(term,
                    _ => new List<Posting> { new Posting(documentId, positions) },
                    (_, list) =>
                    {
                        lock (list) // 保证并发下插入安全
                        {
                            list.Add(new Posting(documentId, positions));
                            return list;
                        }
                    });
            }
        }

        /// <summary>
        /// 平台正式环境：近邻短语检索。
        /// 查询所有包含 phraseTerms，且各词项间距离不超过 maxDistance 的文档ID。
        /// </summary>
        /// <param name="phraseTerms">短语词项数组，按顺序排列</param>
        /// <param name="maxDistance">词项间最大距离（≥1），1表示紧邻</param>
        /// <returns>命中的文档ID集合</returns>
        /// <exception cref="ArgumentNullException">参数为null时抛出</exception>
        /// <exception cref="ArgumentException">参数非法时抛出</exception>
        public HashSet<string> SearchDocumentsByNearPhrase(IList<string> phraseTerms, int maxDistance)
        {
            if (phraseTerms == null || phraseTerms.Count == 0)
                throw new ArgumentNullException(nameof(phraseTerms), "短语词项不能为空");
            if (maxDistance < 1)
                throw new ArgumentException("最大距离需为正整数", nameof(maxDistance));
            if (phraseTerms.Count == 1)
                return SearchDocumentsByTerms(phraseTerms);

            // 步骤1：找出包含所有词项的文档ID交集
            var candidateDocIds = SearchDocumentsByTerms(phraseTerms);
            if (candidateDocIds.Count == 0)
                return new HashSet<string>();

            var results = new HashSet<string>();

            // 步骤2：对每个候选文档，逐一验证是否满足“近邻短语”关系
            foreach (var docId in candidateDocIds)
            {
                // 取该文档中各词项出现的所有位置（已保证文档中都包含每个词）
                var positionsList = new List<List<int>>(phraseTerms.Count);
                foreach (var term in phraseTerms)
                {
                    // 为平台高性能，这里建议预排序或二分定位positions
                    var postings = _termToPostings.TryGetValue(term, out var postingList)
                        ? postingList
                        : null;
                    var p = postings?.FirstOrDefault(x => x.DocumentId == docId);
                    positionsList.Add(p?.Positions ?? new List<int>());
                }
                if (positionsList.Any(l => l.Count == 0))
                    continue;

                // 检查近邻短语逻辑
                if (MatchNearPhrase(positionsList, maxDistance))
                {
                    results.Add(docId);
                }
            }
            return results;
        }

        /// <summary>
        /// 检查是否存在一组递增位置满足“近邻短语”关系。
        /// </summary>
        /// <param name="positionsList">各词项在文档中的出现位置（已按词项顺序排列）</param>
        /// <param name="maxDistance">相邻词项最大允许距离</param>
        /// <returns>只要存在一组满足条件即返回true</returns>
        private bool MatchNearPhrase(List<List<int>> positionsList, int maxDistance)
        {
            // BFS/滑动窗口算法：枚举第一个词的位置，逐级寻找后续词项合规位置
            foreach (var start in positionsList[0])
            {
                int prev = start;
                bool match = true;
                for (int i = 1; i < positionsList.Count; i++)
                {
                    // 找到下一个词中第一个比prev大、且距离≤maxDistance的位置
                    bool found = false;
                    foreach (var pos in positionsList[i])
                    {
                        if (pos > prev && pos - prev <= maxDistance)
                        {
                            prev = pos;
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        match = false;
                        break;
                    }
                }
                if (match) return true;
            }
            return false;
        }

        /// <summary>
        /// 多关键词 AND 查询：返回包含所有关键词的文档ID集合。
        /// </summary>
        /// <param name="terms">词项集合</param>
        /// <returns>文档ID集合</returns>
        /// <exception cref="ArgumentNullException">参数为null时抛出</exception>
        public HashSet<string> SearchDocumentsByTerms(ICollection<string> terms)
        {
            if (terms == null || terms.Count == 0)
                throw new ArgumentNullException(nameof(terms));
            List<HashSet<string>> docSets = new List<HashSet<string>>(terms.Count);

            foreach (var term in terms)
            {
                if (_termToPostings.TryGetValue(term, out var postings))
                    docSets.Add(postings.Select(p => p.DocumentId).ToHashSet());
                else
                    return new HashSet<string>();
            }

            // 求交集
            var result = docSets[0];
            for (int i = 1; i < docSets.Count; i++)
                result.IntersectWith(docSets[i]);
            return result;
        }

        /// <summary>
        /// 获取指定词项的 Posting 列表。
        /// </summary>
        /// <param name="term">词项</param>
        /// <returns>Posting 列表，若词项不存在则为空列表</returns>
        public IReadOnlyList<Posting> GetPostings(string term)
        {
            if (string.IsNullOrWhiteSpace(term))
                throw new ArgumentException("词项不能为空", nameof(term));
            if (_termToPostings.TryGetValue(term, out var postings))
                return postings;
            return Array.Empty<Posting>();
        }
    }

    /// <summary>
    /// 词项 Posting，记录文档内词项所有出现位置。
    /// </summary>
    public class Posting
    {
        /// <summary>
        /// 文档唯一标识。
        /// </summary>
        public string DocumentId { get; }

        /// <summary>
        /// 该词项在文档中出现的位置（可多次出现）。
        /// </summary>
        public List<int> Positions { get; }

        /// <summary>
        /// Posting 构造函数。
        /// </summary>
        /// <param name="documentId">文档ID</param>
        /// <param name="positions">出现位置</param>
        public Posting(string documentId, IEnumerable<int> positions)
        {
            DocumentId = documentId ?? throw new ArgumentNullException(nameof(documentId));
            Positions = positions?.OrderBy(p => p).ToList() ?? new List<int>();
        }
    }


    /// <summary>
    /// 针对 InvertedIndex 生产环境近邻短语算法的工程级单元测试（NUnit + Xml注释 + Assert.That）。
    /// </summary>
    [TestFixture]
    public class InvertedIndexTests
    {
        private InvertedIndex _index;

        /// <summary>
        /// 每个测试用例前初始化倒排索引及基础数据，模拟生产平台文档语料。
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            _index = new InvertedIndex();

            // Doc1: "人工智能 引领 未来"
            _index.AddDocument("Doc1", new Dictionary<string, List<int>>
            {
                { "人工智能", new List<int> { 0 } },
                { "引领", new List<int> { 1 } },
                { "未来", new List<int> { 2 } }
            });

            // Doc2: "未来 XXX 算法"
            _index.AddDocument("Doc2", new Dictionary<string, List<int>>
            {
                { "未来", new List<int> { 0 } },
                { "XXX", new List<int> { 1 } },
                { "算法", new List<int> { 2 } }
            });

            // Doc3: "人工智能 未来 算法"
            _index.AddDocument("Doc3", new Dictionary<string, List<int>>
            {
                { "人工智能", new List<int> { 0 } },
                { "未来", new List<int> { 1 } },
                { "算法", new List<int> { 2 } }
            });

            // Doc4: "未来 算法 人工智能 算法"
            _index.AddDocument("Doc4", new Dictionary<string, List<int>>
            {
                { "未来", new List<int> { 0 } },
                { "算法", new List<int> { 1, 3 } },
                { "人工智能", new List<int> { 2 } }
            });
        }

        /// <summary>
        /// 验证紧邻短语（maxDistance=1）时，只命中完全连续文档。
        /// </summary>
        [Test]
        public void SearchDocumentsByNearPhrase_ExactPhraseOnly_FindsExpectedDocuments()
        {
            // "人工智能 引领" 只在Doc1连续出现
            var result = _index.SearchDocumentsByNearPhrase(new[] { "人工智能", "引领" }, 1);
            Assert.That(result, Is.EquivalentTo(new[] { "Doc1" }));

            // "人工智能 未来" 只在Doc3连续出现
            result = _index.SearchDocumentsByNearPhrase(new[] { "人工智能", "未来" }, 1);
            Assert.That(result, Is.EquivalentTo(new[] { "Doc3" }));

            result = _index.SearchDocumentsByNearPhrase(new[] { "未来", "算法" }, 1);
            Assert.That(result, Is.EquivalentTo(new[] { "Doc3", "Doc4" }));
        }

        /// <summary>
        /// 验证近邻短语（maxDistance=2）允许插入词，能命中更多文档。
        /// </summary>
        [Test]
        public void SearchDocumentsByNearPhrase_AllowDistance_FindsMultipleDocuments()
        {
            // "未来 算法"，maxDistance=2，Doc2（0->2）和Doc3（1->2）和Doc4（0->1）都应命中
            var result = _index.SearchDocumentsByNearPhrase(new[] { "未来", "算法" }, 2);
            Assert.That(result, Is.EquivalentTo(new[] { "Doc2", "Doc3", "Doc4" }));
        }

        /// <summary>
        /// 验证近邻短语算法支持单词多次出现时的正确处理。
        /// </summary>
        [Test]
        public void SearchDocumentsByNearPhrase_MultipleOccurrences_FindsCorrectDocuments()
        {
            // "算法 人工智能 算法" in Doc4: 算法@1->人工智能@2->算法@3
            var result = _index.SearchDocumentsByNearPhrase(new[] { "算法", "人工智能", "算法" }, 1);
            Assert.That(result, Is.EquivalentTo(new[] { "Doc4" }));
        }

        /// <summary>
        /// 验证非命中情况，算法应返回空集。
        /// </summary>
        [Test]
        public void SearchDocumentsByNearPhrase_NoMatch_ReturnsEmptySet()
        {
            // "引领 算法"未在任意文档中以允许距离连续出现
            var result = _index.SearchDocumentsByNearPhrase(new[] { "引领", "算法" }, 1);
            Assert.That(result, Is.Empty);
        }

        /// <summary>
        /// 验证非法参数时抛出明确异常。
        /// </summary>
        [Test]
        public void SearchDocumentsByNearPhrase_InvalidArgs_ThrowsArgumentException()
        {
            Assert.That(() => _index.SearchDocumentsByNearPhrase(null, 1), Throws.ArgumentNullException);
            Assert.That(() => _index.SearchDocumentsByNearPhrase(new string[0], 1), Throws.ArgumentNullException);
            Assert.That(() => _index.SearchDocumentsByNearPhrase(new[] { "未来", "算法" }, 0), Throws.ArgumentException);
        }

        /// <summary>
        /// 验证高并发添加文档时索引稳定性。
        /// </summary>
        [Test]
        public void AddDocument_ConcurrentAdditions_DoesNotThrowOrLoseData()
        {
            var terms = new Dictionary<string, List<int>>
            {
                { "大数据", new List<int> { 0 } },
                { "平台", new List<int> { 1 } }
            };

            // 模拟高并发批量写入
            Parallel.For(0, 100, i =>
            {
                _index.AddDocument("Doc" + (1000 + i), terms);
            });

            // 验证添加的数据全部可检索
            var result = _index.SearchDocumentsByNearPhrase(new[] { "大数据", "平台" }, 1);
            Assert.That(result.Count, Is.EqualTo(100));
        }
    }

}
