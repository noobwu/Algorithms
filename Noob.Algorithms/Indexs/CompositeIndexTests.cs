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
    /// 组合索引实体接口，所有被索引对象需有主键和属性字典。
    /// </summary>
    public interface ICompositeIndexEntity
    {
        /// <summary>
        /// 全局唯一实体ID（主键）。
        /// </summary>
        string Id { get; }

        /// <summary>
        /// 返回实体所有用于索引的属性（字段名→值），可包含标签、属性等。
        /// </summary>
        IReadOnlyDictionary<string, object> GetIndexAttributes();
    }

    /// <summary>
    /// 组合索引键工具，按字段名顺序和值生成复合Key。
    /// </summary>
    public static class CompositeKeyUtil
    {
        /// <summary>
        /// 生成组合Key，如["City","Type"]和["Beijing","零售"]→"City=Beijing|Type=零售"
        /// </summary>
        public static string BuildCompositeKey(string[] fields, object[] values)
        {
            if (fields == null || values == null || fields.Length != values.Length)
                throw new ArgumentException("字段与值数量需一致");
            var pairs = new List<string>();
            for (int i = 0; i < fields.Length; i++)
                pairs.Add($"{fields[i]}={values[i] ?? ""}");
            return string.Join("|", pairs);
        }
    }

    /// <summary>
    /// 混合（组合）索引管理器，支持多字段联合索引及高效查询、增删，线程安全。
    /// </summary>
    /// <typeparam name="T">实体类型，需实现ICompositeIndexEntity。</typeparam>
    public class CompositeIndexManager<T> where T : ICompositeIndexEntity
    {
        /// <summary>
        /// 主数据存储表，实体ID → 实体对象。
        /// </summary>
        private readonly ConcurrentDictionary<string, T> _entities = new();

        /// <summary>
        /// 组合索引表，Key为字段名顺序拼接（如"City|Type"），Value为（组合Key→实体ID集合）。
        /// </summary>
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, HashSet<string>>> _compositeIndexes = new();

        /// <summary>
        /// 新增或更新实体，并自动维护所有组合索引。
        /// </summary>
        /// <param name="entity">待添加/更新实体。</param>
        /// <param name="compositeFieldSets">所有需维护的联合索引字段集。如[["City","Type"],["City","Status"]]。</param>
        public void AddOrUpdateEntity(T entity, IEnumerable<string[]> compositeFieldSets)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            var entityId = entity.Id;

            // 若已存在，先删除索引
            if (_entities.ContainsKey(entityId))
                RemoveEntity(entityId, compositeFieldSets);

            _entities[entityId] = entity;
            var attrs = entity.GetIndexAttributes();

            foreach (var fields in compositeFieldSets ?? Enumerable.Empty<string[]>())
            {
                var compositeKey = string.Join("|", fields);
                var values = fields.Select(f => attrs.TryGetValue(f, out var v) ? v : null).ToArray();
                var key = CompositeKeyUtil.BuildCompositeKey(fields, values);

                var index = _compositeIndexes.GetOrAdd(compositeKey, _ => new());
                var set = index.GetOrAdd(key, _ => new HashSet<string>());
                lock (set) { set.Add(entityId); }
            }
        }

        /// <summary>
        /// 删除实体及其相关组合索引。
        /// </summary>
        /// <param name="entityId">实体ID。</param>
        /// <param name="compositeFieldSets">联合索引字段组。</param>
        public void RemoveEntity(string entityId, IEnumerable<string[]> compositeFieldSets)
        {
            if (!_entities.TryRemove(entityId, out var entity)) return;
            var attrs = entity.GetIndexAttributes();

            foreach (var fields in compositeFieldSets ?? Enumerable.Empty<string[]>())
            {
                var compositeKey = string.Join("|", fields);
                var values = fields.Select(f => attrs.TryGetValue(f, out var v) ? v : null).ToArray();
                var key = CompositeKeyUtil.BuildCompositeKey(fields, values);

                if (_compositeIndexes.TryGetValue(compositeKey, out var index) &&
                    index.TryGetValue(key, out var set))
                {
                    lock (set)
                    {
                        set.Remove(entityId);
                        if (set.Count == 0)
                            index.TryRemove(key, out _);
                    }
                }
            }
        }

        /// <summary>
        /// 按联合字段及其值查询实体ID集合（全等匹配）。
        /// </summary>
        /// <param name="fields">联合字段组。如["City","Type"]。</param>
        /// <param name="values">对应值。如["Beijing","零售"]。</param>
        /// <returns>匹配的实体ID集合。</returns>
        public IReadOnlyCollection<string> QueryByCompositeFields(string[] fields, object[] values)
        {
            var compositeKey = string.Join("|", fields);
            var key = CompositeKeyUtil.BuildCompositeKey(fields, values);
            if (_compositeIndexes.TryGetValue(compositeKey, out var index) &&
                index.TryGetValue(key, out var set))
                return set.ToList();
            return Array.Empty<string>();
        }

        /// <summary>
        /// 获取实体对象。
        /// </summary>
        public T GetEntity(string id) => _entities.TryGetValue(id, out var e) ? e : default;
    }

    /// <summary>
    /// 针对 CompositeIndexManager 的单元测试套件，验证组合索引（多字段联合索引）增删查及边界行为。
    /// </summary>
    [TestFixture]
    public class CompositeIndexManagerTests
    {
        /// <summary>
        /// The manager
        /// </summary>
        private CompositeIndexManager<OrderEntity> _manager;

        /// <summary>
        /// The composite fields
        /// </summary>
        private string[][] _compositeFields;

        /// <summary>
        /// 每次测试初始化新实例和联合字段配置。
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            _manager = new CompositeIndexManager<OrderEntity>();
            _compositeFields = new[]
            {
            new[] { "City", "Type" },
            new[] { "Status", "Type" }
        };
        }

        /// <summary>
        /// 验证新增实体后，按多字段组合索引能正确命中。
        /// </summary>
        [Test]
        public void AddOrUpdateEntity_And_QueryByCompositeFields_Works()
        {
            _manager.AddOrUpdateEntity(new OrderEntity { Id = "o1", City = "北京", Type = "零售", Status = "已发货" }, _compositeFields);
            _manager.AddOrUpdateEntity(new OrderEntity { Id = "o2", City = "上海", Type = "零售", Status = "待付款" }, _compositeFields);

            // 查北京零售
            var bjRetail = _manager.QueryByCompositeFields(new[] { "City", "Type" }, new object[] { "北京", "零售" });
            Assert.That(bjRetail, Is.EquivalentTo(new[] { "o1" }));

            // 查上海零售
            var shRetail = _manager.QueryByCompositeFields(new[] { "City", "Type" }, new object[] { "上海", "零售" });
            Assert.That(shRetail, Is.EquivalentTo(new[] { "o2" }));

            // 查已发货+零售
            var deliveredRetail = _manager.QueryByCompositeFields(new[] { "Status", "Type" }, new object[] { "已发货", "零售" });
            Assert.That(deliveredRetail, Is.EquivalentTo(new[] { "o1" }));
        }

        /// <summary>
        /// 验证重复添加（覆盖更新）实体时，索引自动维护且无脏数据。
        /// </summary>
        [Test]
        public void AddOrUpdateEntity_Overwrite_UpdatesIndexCorrectly()
        {
            _manager.AddOrUpdateEntity(new OrderEntity { Id = "o1", City = "北京", Type = "零售", Status = "已发货" }, _compositeFields);
            // 更新城市和状态
            _manager.AddOrUpdateEntity(new OrderEntity { Id = "o1", City = "广州", Type = "零售", Status = "待付款" }, _compositeFields);

            // 老组合命中应无结果
            var bjRetail = _manager.QueryByCompositeFields(new[] { "City", "Type" }, new object[] { "北京", "零售" });
            Assert.That(bjRetail, Is.Empty);

            // 新组合命中
            var gzRetail = _manager.QueryByCompositeFields(new[] { "City", "Type" }, new object[] { "广州", "零售" });
            Assert.That(gzRetail, Is.EquivalentTo(new[] { "o1" }));

            // 新状态命中
            var unpaidRetail = _manager.QueryByCompositeFields(new[] { "Status", "Type" }, new object[] { "待付款", "零售" });
            Assert.That(unpaidRetail, Is.EquivalentTo(new[] { "o1" }));
        }

        /// <summary>
        /// 验证实体删除后所有相关组合索引都被清理。
        /// </summary>
        [Test]
        public void RemoveEntity_RemovesFromAllCompositeIndexes()
        {
            _manager.AddOrUpdateEntity(new OrderEntity { Id = "o1", City = "上海", Type = "批发", Status = "已发货" }, _compositeFields);
            _manager.RemoveEntity("o1", _compositeFields);

            var res1 = _manager.QueryByCompositeFields(new[] { "City", "Type" }, new object[] { "上海", "批发" });
            Assert.That(res1, Is.Empty);

            var res2 = _manager.QueryByCompositeFields(new[] { "Status", "Type" }, new object[] { "已发货", "批发" });
            Assert.That(res2, Is.Empty);

            // 删除不存在实体不抛异常
            Assert.DoesNotThrow(() => _manager.RemoveEntity("not_exist", _compositeFields));
        }

        /// <summary>
        /// 验证单字段索引场景（fields仅传一个字段）。
        /// </summary>
        [Test]
        public void SingleFieldCompositeIndex_Works()
        {
            var singleField = new[] { new[] { "Type" } };
            _manager.AddOrUpdateEntity(new OrderEntity { Id = "o1", City = "成都", Type = "预售", Status = "待付款" }, singleField);

            var res = _manager.QueryByCompositeFields(new[] { "Type" }, new object[] { "预售" });
            Assert.That(res, Is.EquivalentTo(new[] { "o1" }));
        }

        /// <summary>
        /// 验证空字段、空数据、异常等边界情况。
        /// </summary>
        [Test]
        public void EdgeCases_HandleCorrectly()
        {
            // 新增空实体应抛异常
            Assert.Throws<ArgumentNullException>(() => _manager.AddOrUpdateEntity(null, _compositeFields));

            // 查询不存在字段组合应返回空
            var emptyRes = _manager.QueryByCompositeFields(new[] { "City", "Type" }, new object[] { "不存在", "批发" });
            Assert.That(emptyRes, Is.Empty);

            // 查询已删除实体
            _manager.AddOrUpdateEntity(new OrderEntity { Id = "o2", City = "深圳", Type = "批发", Status = "已发货" }, _compositeFields);
            _manager.RemoveEntity("o2", _compositeFields);
            var res = _manager.QueryByCompositeFields(new[] { "City", "Type" }, new object[] { "深圳", "批发" });
            Assert.That(res, Is.Empty);

            // 查询实体对象
            var o3 = new OrderEntity { Id = "o3", City = "苏州", Type = "零售", Status = "已完成" };
            _manager.AddOrUpdateEntity(o3, _compositeFields);
            var entity = _manager.GetEntity("o3");
            Assert.That(entity, Is.EqualTo(o3));
        }
    }

    /// <summary>
    /// 测试用订单实体，实现 ICompositeIndexEntity。
    /// </summary>
    public class OrderEntity : ICompositeIndexEntity
    {
        /// <summary>
        /// 全局唯一实体ID（主键）。
        /// </summary>
        /// <value>The identifier.</value>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the city.
        /// </summary>
        /// <value>The city.</value>
        public string City { get; set; }

        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        /// <value>The type.</value>
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets the status.
        /// </summary>
        /// <value>The status.</value>
        public string Status { get; set; }

        /// <summary>
        /// Gets or sets the tags.
        /// </summary>
        /// <value>The tags.</value>
        public HashSet<string> Tags { get; set; }

        public IReadOnlyDictionary<string, object> GetIndexAttributes()
            => new Dictionary<string, object>
            {
            { "City", City },
            { "Type", Type },
            { "Status", Status },
            { "Tags", Tags != null ? string.Join(",", Tags) : "" }
            };
    }

}
