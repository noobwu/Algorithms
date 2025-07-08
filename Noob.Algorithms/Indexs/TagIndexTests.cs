using NUnit.Framework;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Noob.Algorithms.Indexs
{
    /// <summary>
    /// 标签化实体接口，要求所有被索引对象有全局唯一ID与标签集合。
    /// </summary>
    public interface ITaggableEntity
    {
        /// <summary>
        /// 实体全局唯一ID。
        /// </summary>
        string Id { get; }

        /// <summary>
        /// 当前实体所绑定的标签集合（去重，不区分顺序）。
        /// </summary>
        IReadOnlyCollection<string> Tags { get; }
    }

    /// <summary>
    /// 标签（Tag/Faceted）索引管理器，支持多标签多实体的高效筛选、聚合与增删查，线程安全。
    /// </summary>
    /// <typeparam name="T">被索引实体类型，需实现ITaggableEntity接口。</typeparam>
    public class TagIndexManager<T> where T : ITaggableEntity
    {
        /// <summary>
        /// 标签倒排表，标签名→实体ID集合（HashSet保证无重复）。
        /// </summary>
        private readonly ConcurrentDictionary<string, HashSet<string>> _tagToEntities =
            new ConcurrentDictionary<string, HashSet<string>>();

        /// <summary>
        /// 主数据表，实体ID→实体对象。
        /// </summary>
        private readonly ConcurrentDictionary<string, T> _entities =
            new ConcurrentDictionary<string, T>();

        /// <summary>
        /// 添加或更新实体及其标签索引。
        /// </summary>
        /// <param name="entity">被索引实体对象。</param>
        public void AddOrUpdateEntity(T entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            // 删除旧标签（如果实体已存在）
            if (_entities.TryGetValue(entity.Id, out var oldEntity))
            {
                var oldTags = oldEntity.Tags ?? Array.Empty<string>();
                foreach (var tag in oldTags)
                {
                    if (_tagToEntities.TryGetValue(tag, out var set))
                    {
                        lock (set)
                        {
                            set.Remove(entity.Id);
                            if (set.Count == 0) _tagToEntities.TryRemove(tag, out _);
                        }
                    }
                }
            }

            // 保存实体
            _entities[entity.Id] = entity;

            // 添加新标签
            foreach (var tag in entity.Tags ?? Array.Empty<string>())
            {
                var set = _tagToEntities.GetOrAdd(tag, _ => new HashSet<string>());
                lock (set) { set.Add(entity.Id); }
            }
        }

        /// <summary>
        /// 删除实体及其相关标签索引。
        /// </summary>
        /// <param name="entityId">被删除实体ID。</param>
        public void RemoveEntity(string entityId)
        {
            if (!_entities.TryRemove(entityId, out var entity)) return;

            foreach (var tag in entity.Tags ?? Array.Empty<string>())
            {
                if (_tagToEntities.TryGetValue(tag, out var set))
                {
                    lock (set)
                    {
                        set.Remove(entityId);
                        if (set.Count == 0)
                            _tagToEntities.TryRemove(tag, out _);
                    }
                }
            }
        }

        /// <summary>
        /// 按标签精确筛选，支持多标签交集（所有标签均需匹配）。
        /// </summary>
        /// <param name="tags">标签集合。可为空或任意数量。</param>
        /// <returns>匹配所有标签的实体ID集合。</returns>
        public IReadOnlyCollection<string> QueryByTags(params string[] tags)
        {
            if (tags == null || tags.Length == 0)
                return _entities.Keys.ToArray();

            HashSet<string> result = null;
            foreach (var tag in tags.Distinct())
            {
                if (_tagToEntities.TryGetValue(tag, out var set))
                {
                    if (result == null)
                        result = new HashSet<string>(set);
                    else
                        result.IntersectWith(set);
                }
                else
                {
                    return Array.Empty<string>();
                }
            }

            if (result == null)
                return Array.Empty<string>();

            return result.ToList();
        }

        /// <summary>
        /// 查询指定实体的所有标签。
        /// </summary>
        public IReadOnlyCollection<string> GetTagsForEntity(string entityId)
        {
            if (_entities.TryGetValue(entityId, out var entity))
                return entity.Tags.ToArray();
            return Array.Empty<string>();
        }

        /// <summary>
        /// 查询全平台所有标签及其聚合实体数量（标签云、统计等）。
        /// </summary>
        public IReadOnlyDictionary<string, int> GetAllTagsWithCounts()
        {
            return _tagToEntities.ToDictionary(
                kv => kv.Key,
                kv => kv.Value.Count
            );
        }
    }

    /// <summary>
    /// 示例文档实体，实现 ITaggableEntity。
    /// </summary>
    public class DocumentEntity : ITaggableEntity
    {
        /// <summary>
        /// 实体全局唯一ID。
        /// </summary>
        /// <value>The identifier.</value>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the title.
        /// </summary>
        /// <value>The title.</value>
        public string Title { get; set; }

        /// <summary>
        /// 当前实体所绑定的标签集合（去重，不区分顺序）。
        /// </summary>
        /// <value>The tags.</value>
        public IReadOnlyCollection<string> Tags { get; set; }
    }


    /// <summary>
    /// 针对 TagIndexManager 的单元测试套件，验证标签索引的增删查聚合等功能。
    /// </summary>
    [TestFixture]
    public class TagIndexManagerTests
    {
        /// <summary>
        /// The tag index
        /// </summary>
        private TagIndexManager<DocumentEntity> _tagIndex;

        /// <summary>
        /// 每次测试初始化新的标签索引实例。
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            _tagIndex = new TagIndexManager<DocumentEntity>();
        }

        /// <summary>
        /// 验证添加单文档及单标签/多标签索引功能。
        /// </summary>
        [Test]
        public void AddOrUpdateEntity_SingleAndMultiTag_Works()
        {
            _tagIndex.AddOrUpdateEntity(new DocumentEntity { Id = "d1", Title = "AI趋势", Tags = new[] { "AI" } });
            _tagIndex.AddOrUpdateEntity(new DocumentEntity { Id = "d2", Title = "大模型实战", Tags = new[] { "AI", "技术", "大模型" } });

            var aiDocs = _tagIndex.QueryByTags("AI");
            Assert.That(aiDocs, Is.EquivalentTo(new[] { "d1", "d2" }));

            var techDocs = _tagIndex.QueryByTags("技术");
            Assert.That(techDocs, Is.EquivalentTo(new[] { "d2" }));

            var multiTagDocs = _tagIndex.QueryByTags("AI", "技术");
            Assert.That(multiTagDocs, Is.EquivalentTo(new[] { "d2" }));
        }

        /// <summary>
        /// 验证删除实体后，索引同步移除、标签倒排表自动清理。
        /// </summary>
        [Test]
        public void RemoveEntity_UpdatesIndex_Correctly()
        {
            _tagIndex.AddOrUpdateEntity(new DocumentEntity { Id = "d1", Title = "AI", Tags = new[] { "AI" } });
            _tagIndex.AddOrUpdateEntity(new DocumentEntity { Id = "d2", Title = "ML", Tags = new[] { "机器学习", "AI" } });

            _tagIndex.RemoveEntity("d2");

            var aiDocs = _tagIndex.QueryByTags("AI");
            Assert.That(aiDocs, Is.EquivalentTo(new[] { "d1" }));

            var mlDocs = _tagIndex.QueryByTags("机器学习");
            Assert.That(mlDocs, Is.Empty);
        }

        /// <summary>
        /// 验证交集查询（多标签AND）及全量查询场景。
        /// </summary>
        [Test]
        public void QueryByTags_IntersectionAndAllEntities_Works()
        {
            _tagIndex.AddOrUpdateEntity(new DocumentEntity { Id = "d1", Tags = new[] { "A", "B" } });
            _tagIndex.AddOrUpdateEntity(new DocumentEntity { Id = "d2", Tags = new[] { "A", "C" } });
            _tagIndex.AddOrUpdateEntity(new DocumentEntity { Id = "d3", Tags = new[] { "B", "C" } });

            // 标签A&B交集，只有d1
            var ab = _tagIndex.QueryByTags("A", "B");
            Assert.That(ab, Is.EquivalentTo(new[] { "d1" }));

            // 标签A&C交集，只有d2
            var ac = _tagIndex.QueryByTags("A", "C");
            Assert.That(ac, Is.EquivalentTo(new[] { "d2" }));

            // 查询全部文档（空参数）
            var all = _tagIndex.QueryByTags();
            Assert.That(all, Is.EquivalentTo(new[] { "d1", "d2", "d3" }));

            // 不存在的标签组合应返回空
            var empty = _tagIndex.QueryByTags("A", "Z");
            Assert.That(empty, Is.Empty);
        }

        /// <summary>
        /// 验证标签聚合统计、标签云生成的正确性。
        /// </summary>
        [Test]
        public void GetAllTagsWithCounts_TagCloud_IsCorrect()
        {
            _tagIndex.AddOrUpdateEntity(new DocumentEntity { Id = "d1", Tags = new[] { "X", "Y" } });
            _tagIndex.AddOrUpdateEntity(new DocumentEntity { Id = "d2", Tags = new[] { "X", "Z" } });

            var cloud = _tagIndex.GetAllTagsWithCounts();
            Assert.That(cloud["X"], Is.EqualTo(2));
            Assert.That(cloud["Y"], Is.EqualTo(1));
            Assert.That(cloud["Z"], Is.EqualTo(1));
        }

        /// <summary>
        /// 验证边界情况：无标签实体、重复标签、标签变更、无效删除。
        /// </summary>
        [Test]
        public void EdgeCases_HandleGracefully()
        {
            // 添加无标签实体
            _tagIndex.AddOrUpdateEntity(new DocumentEntity { Id = "d1", Title = "EmptyTag", Tags = Array.Empty<string>() });
            Assert.That(_tagIndex.QueryByTags(), Is.EquivalentTo(new[] { "d1" }));

            // 重复标签应自动去重
            _tagIndex.AddOrUpdateEntity(new DocumentEntity { Id = "d2", Tags = new[] { "A", "A", "B" } });
            var ab = _tagIndex.QueryByTags("A", "B");
            Assert.That(ab, Is.EquivalentTo(new[] { "d2" }));

            // 标签变更：先添加再更改标签，应自动维护倒排表
            _tagIndex.AddOrUpdateEntity(new DocumentEntity { Id = "d2", Tags = new[] { "C" } });
            Assert.That(_tagIndex.QueryByTags("A"), Is.Empty);
            Assert.That(_tagIndex.QueryByTags("C"), Is.EquivalentTo(new[] { "d2" }));

            // 删除不存在ID不抛异常
            Assert.DoesNotThrow(() => _tagIndex.RemoveEntity("not_exist"));
        }

        /// <summary>
        /// 验证标签读取接口返回正确标签集合。
        /// </summary>
        [Test]
        public void GetTagsForEntity_ReturnsCorrectTags()
        {
            _tagIndex.AddOrUpdateEntity(new DocumentEntity { Id = "docX", Tags = new[] { "金融", "区块链" } });
            var tags = _tagIndex.GetTagsForEntity("docX");
            Assert.That(tags, Is.EquivalentTo(new[] { "金融", "区块链" }));

            var empty = _tagIndex.GetTagsForEntity("not_exist");
            Assert.That(empty, Is.Empty);
        }
    }
}
