using NUnit.Framework;
using JiksLib.Collections;
using System;
using System.Collections.Generic;

namespace JiksLib.Test.Collections
{
    [TestFixture]
    public class CacheTests
    {
        #region Make - 参数校验测试

        [Test]
        public void Make_NullFactory_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.That(
                () => Cache.Make<int, int>(
                    Cache.CacheAlgorithm.FIFO, null!, 10),
                Throws.ArgumentNullException);
        }

        [Test]
        public void Make_InvalidCapacityForFifo_ThrowsArgumentOutOfRangeException()
        {
            // Arrange & Act & Assert
            Assert.That(
                () => Cache.Make<int, int>(
                    Cache.CacheAlgorithm.FIFO, _ => 0, 0),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void Make_InvalidCapacityForLru_ThrowsArgumentOutOfRangeException()
        {
            // Arrange & Act & Assert
            Assert.That(
                () => Cache.Make<int, int>(
                    Cache.CacheAlgorithm.LRU, _ => 0, -1),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void Make_InvalidCapacityForLfu_ThrowsArgumentOutOfRangeException()
        {
            // Arrange & Act & Assert
            Assert.That(
                () => Cache.Make<int, int>(
                    Cache.CacheAlgorithm.LFU, _ => 0, 0),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void Make_NeverEvictWithZeroCapacity_IsFine()
        {
            // Arrange & Act
            var cache = Cache.Make<int, int>(
                Cache.CacheAlgorithm.NeverEvict, key => key * 2, 0);

            // Assert
            Assert.That(cache, Is.Not.Null);
            Assert.That(cache.Access(5), Is.EqualTo(10));
        }

        [Test]
        public void MakeAsync_NullFactory_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.That(
                () => Cache.MakeAsync<int, int>(
                    Cache.CacheAlgorithm.LRU, null!, 10),
                Throws.ArgumentNullException);
        }

        [Test]
        public void MakeAsync_NeverEvictWithZeroCapacity_IsFine()
        {
            // Arrange & Act
            var factoryCalled = false;
            var cache = Cache.MakeAsync<int, int>(
                Cache.CacheAlgorithm.NeverEvict,
                (key, onReceive, _) =>
                {
                    factoryCalled = true;
                    onReceive(key * 3);
                },
                0);

            // Act
            var result = 0;
            cache.Access(5, v => result = v, _ => { });

            // Assert
            Assert.That(factoryCalled, Is.True);
            Assert.That(result, Is.EqualTo(15));
        }

        #endregion

        #region FIFO 测试

        [Test]
        public void Fifo_Access_ReturnsValueFromFactory()
        {
            // Arrange
            var cache = Cache.Make<int, string>(
                Cache.CacheAlgorithm.FIFO, key => $"value{key}", 3);

            // Act
            var result = cache.Access(1);

            // Assert
            Assert.That(result, Is.EqualTo("value1"));
        }

        [Test]
        public void Fifo_AccessSameKey_ReturnsCachedValueWithoutCallingFactory()
        {
            // Arrange
            var callCount = 0;
            var cache = Cache.Make<int, string>(
                Cache.CacheAlgorithm.FIFO, key =>
                {
                    callCount++;
                    return $"value{key}";
                }, 3);

            // Act
            cache.Access(1);
            var result = cache.Access(1);

            // Assert
            Assert.That(result, Is.EqualTo("value1"));
            Assert.That(callCount, Is.EqualTo(1));
        }

        [Test]
        public void Fifo_ExceedCapacity_EvictsOldest()
        {
            // Arrange
            var evictedKeys = new List<int>();
            var cache = Cache.Make<int, string>(
                Cache.CacheAlgorithm.FIFO, key => $"value{key}", 3);
            cache.OnEvicted += (key, _) => evictedKeys.Add(key);

            // Act
            cache.Access(1); // 1
            cache.Access(2); // 1, 2
            cache.Access(3); // 1, 2, 3 (满)
            cache.Access(4); // 淘汰 1 → 2, 3, 4

            // Assert
            Assert.That(evictedKeys, Is.EqualTo(new[] { 1 }));
            Assert.That(cache.Access(2), Is.EqualTo("value2"));
            Assert.That(cache.Access(3), Is.EqualTo("value3"));
            Assert.That(cache.Access(4), Is.EqualTo("value4"));
            // 1 已被淘汰，重新 Access 会调用 factory
            var factoryCallCount = 0;
            var cache2 = Cache.Make<int, string>(
                Cache.CacheAlgorithm.FIFO, key =>
                {
                    factoryCallCount++;
                    return $"new{key}";
                }, 3);
            cache2.Access(1);
            cache2.Access(2);
            cache2.Access(3);
            cache2.Access(4); // 淘汰 1
            var result = cache2.Access(1); // 重新创建

            Assert.That(factoryCallCount, Is.EqualTo(5));
            Assert.That(result, Is.EqualTo("new1"));
        }

        [Test]
        public void Fifo_ReadDoesNotAffectEvictionOrder()
        {
            // Arrange
            var evictedKeys = new List<int>();
            var cache = Cache.Make<int, string>(
                Cache.CacheAlgorithm.FIFO, key => $"value{key}", 2);
            cache.OnEvicted += (key, _) => evictedKeys.Add(key);

            // Act: 多次读取 key=1，但它仍然是最早插入的
            cache.Access(1);
            cache.Access(1); // 重复读取
            cache.Access(2);
            cache.Access(3); // 淘汰的应该是 1（最早插入），而不是 2

            // Assert
            Assert.That(evictedKeys, Is.EqualTo(new[] { 1 }));
        }

        [Test]
        public void Fifo_CapacityOne_AlwaysEvictsPrevious()
        {
            // Arrange
            var evictedKeys = new List<int>();
            var cache = Cache.Make<int, string>(
                Cache.CacheAlgorithm.FIFO, key => $"value{key}", 1);
            cache.OnEvicted += (key, _) => evictedKeys.Add(key);

            // Act
            cache.Access(1);
            cache.Access(2);
            cache.Access(3);

            // Assert
            Assert.That(evictedKeys, Is.EqualTo(new[] { 1, 2 }));
            Assert.That(cache.Access(3), Is.EqualTo("value3"));
        }

        #endregion

        #region LRU 测试

        [Test]
        public void Lru_Access_ReturnsValueFromFactory()
        {
            // Arrange
            var cache = Cache.Make<int, string>(
                Cache.CacheAlgorithm.LRU, key => $"value{key}", 3);

            // Act
            var result = cache.Access(1);

            // Assert
            Assert.That(result, Is.EqualTo("value1"));
        }

        [Test]
        public void Lru_ExceedCapacity_EvictsLeastRecentlyUsed()
        {
            // Arrange
            var evictedKeys = new List<int>();
            var cache = Cache.Make<int, string>(
                Cache.CacheAlgorithm.LRU, key => $"value{key}", 3);
            cache.OnEvicted += (key, _) => evictedKeys.Add(key);

            // Act
            cache.Access(1); // 1
            cache.Access(2); // 1, 2
            cache.Access(3); // 1, 2, 3 (满)
            cache.Access(1); // 读取 1 → 顺序变为 2, 3, 1（1 最近使用）
            cache.Access(4); // 淘汰 2（最久未使用）→ 3, 1, 4

            // Assert
            Assert.That(evictedKeys, Is.EqualTo(new[] { 2 }));
        }

        [Test]
        public void Lru_ReadUpdatesAccessOrder()
        {
            // Arrange
            var evictedKeys = new List<int>();
            var cache = Cache.Make<int, string>(
                Cache.CacheAlgorithm.LRU, key => $"value{key}", 2);
            cache.OnEvicted += (key, _) => evictedKeys.Add(key);

            // Act
            cache.Access(1); // 1
            cache.Access(2); // 1, 2 (满)
            cache.Access(1); // 读取 1 → 2, 1（1 变为最近使用）
            cache.Access(3); // 淘汰 2 → 1, 3

            // Assert
            Assert.That(evictedKeys, Is.EqualTo(new[] { 2 }));
        }

        [Test]
        public void Lru_CapacityOne_AlwaysEvictsPrevious()
        {
            // Arrange
            var evictedKeys = new List<int>();
            var cache = Cache.Make<int, string>(
                Cache.CacheAlgorithm.LRU, key => $"value{key}", 1);
            cache.OnEvicted += (key, _) => evictedKeys.Add(key);

            // Act
            cache.Access(1);
            cache.Access(2);
            cache.Access(3);

            // Assert
            Assert.That(evictedKeys, Is.EqualTo(new[] { 1, 2 }));
        }

        #endregion

        #region LFU 测试

        [Test]
        public void Lfu_Access_ReturnsValueFromFactory()
        {
            // Arrange
            var cache = Cache.Make<int, string>(
                Cache.CacheAlgorithm.LFU, key => $"value{key}", 3);

            // Act
            var result = cache.Access(1);

            // Assert
            Assert.That(result, Is.EqualTo("value1"));
        }

        [Test]
        public void Lfu_ExceedCapacity_EvictsLeastFrequentlyUsed()
        {
            // Arrange
            var evictedKeys = new List<int>();
            var cache = Cache.Make<int, string>(
                Cache.CacheAlgorithm.LFU, key => $"value{key}", 3);
            cache.OnEvicted += (key, _) => evictedKeys.Add(key);

            // Act
            cache.Access(1);
            cache.Access(2);
            cache.Access(3); // freq: 1=1, 2=1, 3=1
            cache.Access(1); // freq: 1=2, 2=1, 3=1
            cache.Access(3); // freq: 1=2, 2=1, 3=2
            cache.Access(4); // freq 最低的是 2（频率=1）→ 淘汰 2

            // Assert
            Assert.That(evictedKeys, Is.EqualTo(new[] { 2 }));
        }

        [Test]
        public void Lfu_SameFrequencyEvictsOldest()
        {
            // Arrange
            var evictedKeys = new List<int>();
            var cache = Cache.Make<int, string>(
                Cache.CacheAlgorithm.LFU, key => $"value{key}", 2);
            cache.OnEvicted += (key, _) => evictedKeys.Add(key);

            // Act: 1 和 2 频率相同，1 更早进入
            cache.Access(1);
            cache.Access(2); // 满，freq 都是 1
            cache.Access(3); // 淘汰同频率中最早进入的 1

            // Assert
            Assert.That(evictedKeys, Is.EqualTo(new[] { 1 }));
        }

        [Test]
        public void Lfu_FrequentAccessPreventsEviction()
        {
            // Arrange
            var evictedKeys = new List<int>();
            var cache = Cache.Make<int, string>(
                Cache.CacheAlgorithm.LFU, key => $"value{key}", 3);
            cache.OnEvicted += (key, _) => evictedKeys.Add(key);

            // Act
            cache.Access(1);
            cache.Access(2);
            cache.Access(3); // freq 全是 1
            cache.Access(2); // 2→2
            cache.Access(2); // 2→3
            cache.Access(3); // 3→2
            cache.Access(4); // minFreq=1，1 和 4（新）中 1 更早 → 淘汰 1

            // Assert
            Assert.That(evictedKeys, Is.EqualTo(new[] { 1 }));
            Assert.That(cache.Access(2), Is.EqualTo("value2"));
            Assert.That(cache.Access(3), Is.EqualTo("value3"));
            Assert.That(cache.Access(4), Is.EqualTo("value4"));
        }

        [Test]
        public void Lfu_CapacityOne_AlwaysEvictsPrevious()
        {
            // Arrange
            var evictedKeys = new List<int>();
            var cache = Cache.Make<int, string>(
                Cache.CacheAlgorithm.LFU, key => $"value{key}", 1);
            cache.OnEvicted += (key, _) => evictedKeys.Add(key);

            // Act
            cache.Access(1);
            cache.Access(2);
            cache.Access(3);

            // Assert
            Assert.That(evictedKeys, Is.EqualTo(new[] { 1, 2 }));
        }

        #endregion

        #region NeverEvict 测试

        [Test]
        public void NeverEvict_Access_ReturnsCachedValue()
        {
            // Arrange
            var callCount = 0;
            var cache = Cache.Make<int, string>(
                Cache.CacheAlgorithm.NeverEvict, key =>
                {
                    callCount++;
                    return $"value{key}";
                }, 100);

            // Act
            cache.Access(1);
            cache.Access(2);
            cache.Access(1);

            // Assert
            Assert.That(callCount, Is.EqualTo(2));
        }

        [Test]
        public void NeverEvict_NeverRaisesOnEvicted()
        {
            // Arrange
            var evicted = false;
            var cache = Cache.Make<int, string>(
                Cache.CacheAlgorithm.NeverEvict, key => $"value{key}", 2);
            cache.OnEvicted += (_, _) => evicted = true;

            // Act: 添加超过"容量"的值
            cache.Access(1);
            cache.Access(2);
            cache.Access(3);
            cache.Access(4);

            // Assert
            Assert.That(evicted, Is.False);
            Assert.That(cache.Access(1), Is.EqualTo("value1"));
            Assert.That(cache.Access(4), Is.EqualTo("value4"));
        }

        #endregion

        #region OnEvicted 事件测试

        [Test]
        public void OnEvicted_ProvidesCorrectKeyAndValue()
        {
            // Arrange
            int evictedKey = 0;
            string? evictedValue = null;
            var cache = Cache.Make<int, string>(
                Cache.CacheAlgorithm.FIFO, key => $"value{key}", 2);
            cache.OnEvicted += (key, value) =>
            {
                evictedKey = key;
                evictedValue = value;
            };

            // Act
            cache.Access(1);
            cache.Access(2);
            cache.Access(3); // 淘汰 1

            // Assert
            Assert.That(evictedKey, Is.EqualTo(1));
            Assert.That(evictedValue, Is.EqualTo("value1"));
        }

        [Test]
        public void OnEvicted_NotCalledWhenNoEviction()
        {
            // Arrange
            var evicted = false;
            var cache = Cache.Make<int, string>(
                Cache.CacheAlgorithm.LRU, key => $"value{key}", 5);
            cache.OnEvicted += (_, _) => evicted = true;

            // Act
            cache.Access(1);
            cache.Access(2);
            cache.Access(3);

            // Assert
            Assert.That(evicted, Is.False);
        }

        #endregion

        #region 异步缓存测试

        [Test]
        public void AsyncCache_Access_CallsOnReceiveWithValue()
        {
            // Arrange
            var cache = Cache.MakeAsync<int, string>(
                Cache.CacheAlgorithm.FIFO,
                (key, onReceive, _) => onReceive($"async{key}"),
                3);
            string? result = null;

            // Act
            cache.Access(5, v => result = v, _ => { });

            // Assert
            Assert.That(result, Is.EqualTo("async5"));
        }

        [Test]
        public void AsyncCache_AccessSameKey_ReturnsCachedValueImmediately()
        {
            // Arrange
            var factoryCallCount = 0;
            var cache = Cache.MakeAsync<int, string>(
                Cache.CacheAlgorithm.LRU,
                (key, onReceive, _) =>
                {
                    factoryCallCount++;
                    onReceive($"async{key}");
                },
                3);
            var results = new List<string>();

            // Act
            cache.Access(1, v => results.Add(v), _ => { });
            cache.Access(1, v => results.Add(v), _ => { }); // 应命中缓存

            // Assert
            Assert.That(factoryCallCount, Is.EqualTo(1));
            Assert.That(results, Has.Count.EqualTo(2));
            Assert.That(results[0], Is.EqualTo("async1"));
            Assert.That(results[1], Is.EqualTo("async1"));
        }

        [Test]
        public void AsyncCache_ExceedCapacity_EvictsAndCallsOnEvicted()
        {
            // Arrange
            var evictedKeys = new List<int>();
            var cache = Cache.MakeAsync<int, string>(
                Cache.CacheAlgorithm.FIFO,
                (key, onReceive, _) => onReceive($"async{key}"),
                2);
            cache.OnEvicted += (key, _) => evictedKeys.Add(key);

            // Act
            var results = new List<string>();
            cache.Access(1, v => results.Add(v), _ => { });
            cache.Access(2, v => results.Add(v), _ => { });
            cache.Access(3, v => results.Add(v), _ => { }); // 淘汰 1

            // Assert
            Assert.That(evictedKeys, Is.EqualTo(new[] { 1 }));
            Assert.That(results, Has.Count.EqualTo(3));
        }

        [Test]
        public void AsyncCache_OnException_CalledOnError()
        {
            // Arrange
            var cache = Cache.MakeAsync<int, string>(
                Cache.CacheAlgorithm.FIFO,
                (key, _, onException) => onException(new InvalidOperationException("test error")),
                3);
            Exception? caught = null;

            // Act
            cache.Access(1, _ => { }, ex => caught = ex);

            // Assert
            Assert.That(caught, Is.Not.Null);
            Assert.That(caught, Is.InstanceOf<InvalidOperationException>());
            Assert.That(caught.Message, Is.EqualTo("test error"));
        }

        [Test]
        public void AsyncCache_Lru_ReadUpdatesAccessOrder()
        {
            // Arrange
            var evictedKeys = new List<int>();
            var cache = Cache.MakeAsync<int, string>(
                Cache.CacheAlgorithm.LRU,
                (key, onReceive, _) => onReceive($"async{key}"),
                2);
            cache.OnEvicted += (key, _) => evictedKeys.Add(key);

            // Act
            var results = new List<string>();
            cache.Access(1, v => results.Add(v), _ => { });
            cache.Access(2, v => results.Add(v), _ => { });
            cache.Access(1, v => results.Add(v), _ => { }); // 刷新 1 的访问时间
            cache.Access(3, v => results.Add(v), _ => { }); // 应淘汰 2（未被刷新）

            // Assert
            Assert.That(evictedKeys, Is.EqualTo(new[] { 2 }));
        }

        #endregion

        #region 综合测试

        [Test]
        public void AllAlgorithms_CanCacheReferenceTypes()
        {
            // Arrange
            foreach (var algo in new[]
            {
                Cache.CacheAlgorithm.FIFO,
                Cache.CacheAlgorithm.LRU,
                Cache.CacheAlgorithm.LFU,
                Cache.CacheAlgorithm.NeverEvict
            })
            {
                var cache = Cache.Make<string, List<int>>(
                    algo, _ => new List<int>(), 3);

                // Act
                var list = cache.Access("test");

                // Assert
                Assert.That(list, Is.Not.Null);
                Assert.That(list, Is.Empty);
            }
        }

        [Test]
        public void AllAlgorithms_WithStringKeys_WorkCorrectly()
        {
            // Arrange
            var cache = Cache.Make<string, int>(
                Cache.CacheAlgorithm.LRU, k => k.Length, 2);

            // Act
            var r1 = cache.Access("hello");
            var r2 = cache.Access("world");
            var r3 = cache.Access("hello"); // 命中缓存
            cache.Access("a"); // 淘汰 "world"
            var r4 = cache.Access("world"); // 未命中，重新创建

            // Assert
            Assert.That(r1, Is.EqualTo(5));
            Assert.That(r2, Is.EqualTo(5));
            Assert.That(r3, Is.EqualTo(5));
            Assert.That(r4, Is.EqualTo(5));
        }

        #endregion
    }
}
