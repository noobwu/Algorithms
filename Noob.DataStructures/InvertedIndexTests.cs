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
                        lock (list)
                        {
                            // 保证唯一文档（可选：支持文档更新）
                            var exist = list.FindIndex(p => p.DocumentId == documentId);
                            if (exist >= 0)
                                list[exist] = new Posting(documentId, positions); // 覆盖
                            else
                                list.Add(new Posting(documentId, positions));
                            // Posting列表按文档ID排序，便于后续高效交集
                            list.Sort((a, b) => string.CompareOrdinal(a.DocumentId, b.DocumentId));
                            return list;
                        }
                    });
            }
        }

        /// <summary>
        /// 高性能近邻短语检索（滑动窗口+二分查找）。
        /// </summary>
        /// <param name="phraseTerms">短语词项数组，按顺序排列</param>
        /// <param name="maxDistance">词项间最大距离（≥1），1表示紧邻</param>
        public HashSet<string> SearchDocumentsByNearPhrase(IList<string> phraseTerms, int maxDistance)
        {
            if (phraseTerms == null || phraseTerms.Count == 0)
                throw new ArgumentNullException(nameof(phraseTerms));
            if (maxDistance < 1)
                throw new ArgumentException("最大距离需为正整数", nameof(maxDistance));
            if (phraseTerms.Count == 1)
                return SearchDocumentsByTerms(phraseTerms);

            // Posting列表交集，先过滤可能文档
            var candidateDocIds = SearchDocumentsByTerms(phraseTerms);
            if (candidateDocIds.Count == 0)
                return new HashSet<string>();

            var results = new HashSet<string>();
            foreach (var docId in candidateDocIds)
            {
                // 找到每个词项在该文档中的所有有序位置
                var positionsList = phraseTerms.Select(term =>
                {
                    var postings = _termToPostings.TryGetValue(term, out var list)
                        ? list : null;
                    return postings?.FirstOrDefault(p => p.DocumentId == docId)?.Positions ?? new SortedSet<int>();
                }).ToList();

                // 优化：滑动窗口+递归消除O(N^k)爆炸
                if (MatchNearPhrase(positionsList, maxDistance))
                {
                    results.Add(docId);
                }
            }
            return results;
        }


        /// <summary>
        /// 多词AND检索（Posting列表归并交集）。
        /// </summary>
        public HashSet<string> SearchDocumentsByTerms(ICollection<string> terms)
        {
            if (terms == null || terms.Count == 0)
                throw new ArgumentNullException(nameof(terms));
            List<List<Posting>> postingsLists = new List<List<Posting>>(terms.Count);

            foreach (var term in terms)
            {
                if (_termToPostings.TryGetValue(term, out var postings))
                    postingsLists.Add(postings);
                else
                    return new HashSet<string>();
            }

            // 先按Posting数量升序排序，优化归并
            postingsLists.Sort((a, b) => a.Count.CompareTo(b.Count));
            // Posting列表交集（按文档ID有序多路归并）
            var result = postingsLists[0].Select(p => p.DocumentId).ToHashSet();
            foreach (var postings in postingsLists.Skip(1))
            {
                var set = postings.Select(p => p.DocumentId).ToHashSet();
                result.IntersectWith(set);
                if (result.Count == 0) break;
            }
            return result;
        }

        /// <summary>
        /// 利用有序结构优化近邻短语匹配（滑动窗口）。
        /// </summary>
        /// <param name="positionsList">各词项在文档中的出现位置（已按词项顺序排列）</param>
        /// <param name="maxDistance">相邻词项最大允许距离</param>
        /// <returns>只要存在一组满足条件即返回true</returns>
        private bool MatchNearPhrase(List<SortedSet<int>> positionsList, int maxDistance)
        {
            // 使用队列滑动窗口遍历所有首词位置
            foreach (var start in positionsList[0])
            {
                int prev = start;
                bool match = true;
                for (int i = 1; i < positionsList.Count; i++)
                {
                    // 二分/跳跃找下一个词项最小满足条件的位置
                    bool found = false;
                    foreach (var pos in positionsList[i].GetViewBetween(prev + 1, prev + maxDistance))
                    {
                        prev = pos;`
                        found = true;
                        break;
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
        public SortedSet<int> Positions { get; }

        /// <summary>
        /// Posting 构造函数。
        /// </summary>
        /// <param name="documentId">文档ID</param>
        /// <param name="positions">出现位置</param>
        public Posting(string documentId, IEnumerable<int> positions)
        {
            DocumentId = documentId ?? throw new ArgumentNullException(nameof(documentId));

            // SortedSet 保证有序、唯一
            Positions = positions != null ? [.. positions] : new SortedSet<int>();
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
