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
    /// 属性索引的实体接口，用于支持通用化和泛型扩展。
    /// </summary>
    public interface IAttributeEntity
    {
        /// <summary>
        /// 实体全局唯一ID（主键）。
        /// </summary>
        string Id { get; }
    }


    /// <summary>
    /// 属性（辅助）索引管理器，支持动态索引字段、联合索引、多值索引和高效查找。
    /// 泛型T需实现IAttributeEntity接口，确保主键唯一性。
    /// </summary>
    /// <typeparam name="T">被索引实体类型。</typeparam>
    public class AttributeIndexManager<T> where T : IAttributeEntity
    {
        /// <summary>
        /// 主数据存储表，key为实体ID。
        /// </summary>
        private readonly ConcurrentDictionary<string, T> _entities = new();

        /// <summary>
        /// 索引字典，key为字段名，value为“字段值→实体ID集合”的反向映射。
        /// </summary>
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<object, HashSet<string>>> _attributeIndexes = new();

        /// <summary>
        /// 联合索引字典，key为多个字段名拼接（如"city+gender"），value为“字段值组合→实体ID集合”。
        /// </summary>
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, HashSet<string>>> _compositeIndexes = new();

        /// <summary>
        /// 新增或更新实体，并自动维护所有索引。
        /// </summary>
        /// <param name="entity">实体对象。</param>
        /// <param name="indexedAttributes">需索引的字段名集合，需与属性名称一致。</param>
        /// <param name="compositeAttributes">联合索引字段组（如new[]{"city","gender"}）。可选。</param>
        public void AddOrUpdateEntity(
            T entity,
            IEnumerable<string> indexedAttributes,
            IEnumerable<string[]> compositeAttributes = null)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            var entityId = entity.Id;

            // 若存在则先删除所有索引
            if (_entities.ContainsKey(entityId))
                RemoveEntity(entityId, indexedAttributes, compositeAttributes);

            _entities[entityId] = entity;

            // 单字段索引
            foreach (var attr in indexedAttributes ?? Enumerable.Empty<string>())
            {
                var value = GetPropertyValue(entity, attr);
                if (value == null) continue;

                var index = _attributeIndexes.GetOrAdd(attr, _ => new());
                var set = index.GetOrAdd(value, _ => new HashSet<string>());
                lock (set) { set.Add(entityId); }
            }

            // 联合索引
            if (compositeAttributes != null)
            {
                foreach (var fields in compositeAttributes)
                {
                    var compositeKey = string.Join("+", fields);
                    var values = fields.Select(f => GetPropertyValue(entity, f)?.ToString() ?? "").ToArray();
                    var compValue = string.Join("|", values);

                    var index = _compositeIndexes.GetOrAdd(compositeKey, _ => new());
                    var set = index.GetOrAdd(compValue, _ => new HashSet<string>());
                    lock (set) { set.Add(entityId); }
                }
            }
        }

        /// <summary>
        /// 按主键删除实体，并同步删除相关索引。
        /// </summary>
        /// <param name="entityId">实体ID。</param>
        /// <param name="indexedAttributes">需维护的单字段索引集合。</param>
        /// <param name="compositeAttributes">联合索引字段组。可选。</param>
        public void RemoveEntity(
            string entityId,
            IEnumerable<string> indexedAttributes,
            IEnumerable<string[]> compositeAttributes = null)
        {
            if (!_entities.TryRemove(entityId, out var entity)) return;

            // 单字段索引
            foreach (var attr in indexedAttributes ?? Enumerable.Empty<string>())
            {
                var value = GetPropertyValue(entity, attr);
                if (value == null) continue;

                if (_attributeIndexes.TryGetValue(attr, out var index) && index.TryGetValue(value, out var set))
                {
                    lock (set)
                    {
                        set.Remove(entityId);
                        if (set.Count == 0)
                            index.TryRemove(value, out _);
                    }
                }
            }

            // 联合索引
            if (compositeAttributes != null)
            {
                foreach (var fields in compositeAttributes)
                {
                    var compositeKey = string.Join("+", fields);
                    var values = fields.Select(f => GetPropertyValue(entity, f)?.ToString() ?? "").ToArray();
                    var compValue = string.Join("|", values);

                    if (_compositeIndexes.TryGetValue(compositeKey, out var index) && index.TryGetValue(compValue, out var set))
                    {
                        lock (set)
                        {
                            set.Remove(entityId);
                            if (set.Count == 0)
                                index.TryRemove(compValue, out _);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 按单字段索引查找实体ID集合。
        /// </summary>
        /// <param name="attributeName">字段名。</param>
        /// <param name="attributeValue">字段值。</param>
        /// <returns>所有匹配实体ID集合。</returns>
        public IReadOnlyCollection<string> QueryByAttribute(string attributeName, object attributeValue)
        {
            if (_attributeIndexes.TryGetValue(attributeName, out var index)
                && attributeValue != null
                && index.TryGetValue(attributeValue, out var set))
                return set.ToList();
            return Array.Empty<string>();
        }

        /// <summary>
        /// 按联合索引查找实体ID集合。
        /// </summary>
        /// <param name="attributeNames">联合字段组，如new[]{"city","gender"}。</param>
        /// <param name="attributeValues">字段值组，顺序需一致。</param>
        /// <returns>所有匹配实体ID集合。</returns>
        public IReadOnlyCollection<string> QueryByCompositeAttributes(string[] attributeNames, object[] attributeValues)
        {
            var compositeKey = string.Join("+", attributeNames);
            var compValue = string.Join("|", attributeValues.Select(v => v?.ToString() ?? ""));
            if (_compositeIndexes.TryGetValue(compositeKey, out var index)
                && index.TryGetValue(compValue, out var set))
                return set.ToList();
            return Array.Empty<string>();
        }

        /// <summary>
        /// 获取实体的某个属性值（支持反射）。
        /// </summary>
        private static object GetPropertyValue(T entity, string propertyName)
        {
            var prop = typeof(T).GetProperty(propertyName);
            return prop?.GetValue(entity);
        }
    }

    /// <summary>
    /// 示例：用户实体，实现 IAttributeEntity，可扩展更多字段。
    /// </summary>
    public class UserEntity : IAttributeEntity
    {
        /// <summary>
        /// 实体全局唯一ID（主键）。
        /// </summary>
        /// <value>The identifier.</value>
        public string Id { get; set; }
        /// <summary>
        /// Gets or sets the username.
        /// </summary>
        /// <value>The username.</value>
        public string Username { get; set; }
        /// <summary>
        /// Gets or sets the gender.
        /// </summary>
        /// <value>The gender.</value>
        public string Gender { get; set; }
        /// <summary>
        /// Gets or sets the city.
        /// </summary>
        /// <value>The city.</value>
        public string City { get; set; }
        /// <summary>
        /// Gets or sets the registered at.
        /// </summary>
        /// <value>The registered at.</value>
        public DateTime RegisteredAt { get; set; }
    }

    /// <summary>
    /// 针对 AttributeIndexManager 的单元测试套件，验证属性索引增删查等各项功能。
    /// </summary>
    [TestFixture]
    public class AttributeIndexManagerTests
    {
        /// <summary>
        /// The index
        /// </summary>
        private AttributeIndexManager<UserEntity> _index;

        /// <summary>
        /// The indexed attrs
        /// </summary>
        private string[] _indexedAttrs;

        /// <summary>
        /// The composite attrs
        /// </summary>
        private string[][] _compositeAttrs;

        /// <summary>
        /// 测试前初始化索引管理器和索引字段配置。
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            _index = new AttributeIndexManager<UserEntity>();
            _indexedAttrs = new[] { "Username", "Gender", "City" };
            _compositeAttrs = new[] { new[] { "City", "Gender" } };
        }

        /// <summary>
        /// 验证单字段索引增删查功能。
        /// </summary>
        [Test]
        public void AddOrUpdateEntity_And_QueryByAttribute_Works()
        {
            _index.AddOrUpdateEntity(new UserEntity { Id = "u1", Username = "alice", Gender = "F", City = "Shanghai" }, _indexedAttrs);
            _index.AddOrUpdateEntity(new UserEntity { Id = "u2", Username = "bob", Gender = "M", City = "Beijing" }, _indexedAttrs);

            var usersShanghai = _index.QueryByAttribute("City", "Shanghai");
            Assert.That(usersShanghai, Is.EquivalentTo(new[] { "u1" }));

            var usersMale = _index.QueryByAttribute("Gender", "M");
            Assert.That(usersMale, Is.EquivalentTo(new[] { "u2" }));

            var usersNone = _index.QueryByAttribute("Username", "charlie");
            Assert.That(usersNone, Is.Empty);
        }

        /// <summary>
        /// 验证联合索引添加与组合条件查找功能。
        /// </summary>
        [Test]
        public void AddOrUpdateEntity_And_QueryByCompositeAttributes_Works()
        {
            _index.AddOrUpdateEntity(new UserEntity { Id = "u1", Username = "alice", Gender = "F", City = "Shanghai" }, _indexedAttrs, _compositeAttrs);
            _index.AddOrUpdateEntity(new UserEntity { Id = "u2", Username = "bob", Gender = "F", City = "Shanghai" }, _indexedAttrs, _compositeAttrs);
            _index.AddOrUpdateEntity(new UserEntity { Id = "u3", Username = "lucy", Gender = "M", City = "Shanghai" }, _indexedAttrs, _compositeAttrs);

            // City=Shanghai, Gender=F 命中u1和u2
            var femaleInShanghai = _index.QueryByCompositeAttributes(new[] { "City", "Gender" }, new object[] { "Shanghai", "F" });
            Assert.That(femaleInShanghai, Is.EquivalentTo(new[] { "u1", "u2" }));

            // City=Shanghai, Gender=M 命中u3
            var maleInShanghai = _index.QueryByCompositeAttributes(new[] { "City", "Gender" }, new object[] { "Shanghai", "M" });
            Assert.That(maleInShanghai, Is.EquivalentTo(new[] { "u3" }));

            // 不存在的组合应为空
            var notExist = _index.QueryByCompositeAttributes(new[] { "City", "Gender" }, new object[] { "Beijing", "F" });
            Assert.That(notExist, Is.Empty);
        }

        /// <summary>
        /// 验证实体覆盖更新及索引同步功能。
        /// </summary>
        [Test]
        public void AddOrUpdateEntity_Overwrite_UpdatesIndexesCorrectly()
        {
            _index.AddOrUpdateEntity(new UserEntity { Id = "u1", Username = "alice", Gender = "F", City = "Shanghai" }, _indexedAttrs, _compositeAttrs);
            // 更新City
            _index.AddOrUpdateEntity(new UserEntity { Id = "u1", Username = "alice", Gender = "F", City = "Beijing" }, _indexedAttrs, _compositeAttrs);

            var inShanghai = _index.QueryByAttribute("City", "Shanghai");
            Assert.That(inShanghai, Is.Empty);

            var inBeijing = _index.QueryByAttribute("City", "Beijing");
            Assert.That(inBeijing, Is.EquivalentTo(new[] { "u1" }));

            // 联合索引也同步更新
            var femaleInBeijing = _index.QueryByCompositeAttributes(new[] { "City", "Gender" }, new object[] { "Beijing", "F" });
            Assert.That(femaleInBeijing, Is.EquivalentTo(new[] { "u1" }));
        }

        /// <summary>
        /// 验证删除实体后索引同步清理，无冗余残留。
        /// </summary>
        [Test]
        public void RemoveEntity_RemovesFromAllIndexes()
        {
            _index.AddOrUpdateEntity(new UserEntity { Id = "u1", Username = "alice", Gender = "F", City = "Shanghai" }, _indexedAttrs, _compositeAttrs);
            _index.RemoveEntity("u1", _indexedAttrs, _compositeAttrs);

            var result = _index.QueryByAttribute("City", "Shanghai");
            Assert.That(result, Is.Empty);

            var composite = _index.QueryByCompositeAttributes(new[] { "City", "Gender" }, new object[] { "Shanghai", "F" });
            Assert.That(composite, Is.Empty);

            // 删除不存在的实体不应抛异常
            Assert.DoesNotThrow(() => _index.RemoveEntity("not_exist", _indexedAttrs, _compositeAttrs));
        }

        /// <summary>
        /// 验证边界情况：空字段、空实体、空索引配置时的健壮性。
        /// </summary>
        [Test]
        public void EdgeCases_HandleCorrectly()
        {
            // 空实体应抛异常
            Assert.Throws<ArgumentNullException>(() => _index.AddOrUpdateEntity(null, _indexedAttrs));

            // 查询空索引字段应返回空
            var result = _index.QueryByAttribute("City", null);
            Assert.That(result, Is.Empty);

            // 只索引部分字段
            _index.AddOrUpdateEntity(new UserEntity { Id = "u2", Username = "charlie", Gender = "M", City = "Guangzhou" }, new[] { "Username" });
            var byUsername = _index.QueryByAttribute("Username", "charlie");
            Assert.That(byUsername, Is.EquivalentTo(new[] { "u2" }));

            var byCity = _index.QueryByAttribute("City", "Guangzhou");
            Assert.That(byCity, Is.Empty);
        }
    }

}
