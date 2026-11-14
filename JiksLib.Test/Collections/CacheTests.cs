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
                    Cache.Algorithm.FIFO, null!, 10),
                Throws.ArgumentNullException);
        }

        [Test]
        public void Make_InvalidCapacityForFifo_ThrowsArgumentOutOfRangeException()
        {
            // Arrange & Act & Assert
            Assert.That(
                () => Cache.Make<int, int>(
                    Cache.Algorithm.FIFO, _ => 0, 0),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void Make_InvalidCapacityForLru_ThrowsArgumentOutOfRangeException()
        {
            // Arrange & Act & Assert
            Assert.That(
                () => Cache.Make<int, int>(
                    Cache.Algorithm.LRU, _ => 0, -1),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void Make_InvalidCapacityForLfu_ThrowsArgumentOutOfRangeException()
        {
            // Arrange & Act & Assert
            Assert.That(
                () => Cache.Make<int, int>(
                    Cache.Algorithm.LFU, _ => 0, 0),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void Make_NeverEvictWithZeroCapacity_IsFine()
        {
            // Arrange & Act
            var cache = Cache.Make<int, int>(
                Cache.Algorithm.NeverEvict, key => key * 2, 0);

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
                    Cache.Algorithm.LRU, null!, 10),
                Throws.ArgumentNullException);
        }

        [Test]
        public void MakeAsync_NeverEvictWithZeroCapacity_IsFine()
        {
            // Arrange & Act
            var factoryCalled = false;
            var cache = Cache.MakeAsync<int, int>(
                Cache.Algorithm.NeverEvict,
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
                Cache.Algorithm.FIFO, key => $"value{key}", 3);

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
                Cache.Algorithm.FIFO, key =>
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
                Cache.Algorithm.FIFO, key => $"value{key}", 3);
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
                Cache.Algorithm.FIFO, key =>
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
                Cache.Algorithm.FIFO, key => $"value{key}", 2);
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
                Cache.Algorithm.FIFO, key => $"value{key}", 1);
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
                Cache.Algorithm.LRU, key => $"value{key}", 3);

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
                Cache.Algorithm.LRU, key => $"value{key}", 3);
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
                Cache.Algorithm.LRU, key => $"value{key}", 2);
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
                Cache.Algorithm.LRU, key => $"value{key}", 1);
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
                Cache.Algorithm.LFU, key => $"value{key}", 3);

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
                Cache.Algorithm.LFU, key => $"value{key}", 3);
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
                Cache.Algorithm.LFU, key => $"value{key}", 2);
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
                Cache.Algorithm.LFU, key => $"value{key}", 3);
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
                Cache.Algorithm.LFU, key => $"value{key}", 1);
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
                Cache.Algorithm.NeverEvict, key =>
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
                Cache.Algorithm.NeverEvict, key => $"value{key}", 2);
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
                Cache.Algorithm.FIFO, key => $"value{key}", 2);
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
                Cache.Algorithm.LRU, key => $"value{key}", 5);
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
                Cache.Algorithm.FIFO,
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
                Cache.Algorithm.LRU,
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
                Cache.Algorithm.FIFO,
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
                Cache.Algorithm.FIFO,
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
                Cache.Algorithm.LRU,
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

        #region Evict / GetCached 测试

        [Test]
        public void Evict_AllAlgorithms_RemovesValueAndRaisesOnEvicted()
        {
            foreach (var algorithm in new[]
            {
                Cache.Algorithm.FIFO,
                Cache.Algorithm.LRU,
                Cache.Algorithm.LFU,
                Cache.Algorithm.NeverEvict
            })
            {
                // Arrange
                var evicted = new List<int>();
                var cache = Cache.Make<int, string>(
                    algorithm, key => "v" + key, 3);
                cache.OnEvicted += (key, _) => evicted.Add(key);
                cache.Access(1);
                cache.Access(2);

                // Act
                cache.Evict(1);

                // Assert
                Assert.That(evicted, Is.EqualTo(new[] { 1 }), algorithm.ToString());
                Assert.That(
                    cache.GetCached(1, out _, dontModifyCacheState: true),
                    Is.False,
                    algorithm.ToString());
                Assert.That(
                    cache.GetCached(2, out var value, dontModifyCacheState: true),
                    Is.True,
                    algorithm.ToString());
                Assert.That(value, Is.EqualTo("v2"));
            }
        }

        [Test]
        public void Evict_UnknownKey_DoesNotRaiseEvent()
        {
            // Arrange
            var evicted = new List<int>();
            var cache = Cache.Make<int, string>(
                Cache.Algorithm.LRU, key => "v" + key, 2);
            cache.OnEvicted += (key, _) => evicted.Add(key);
            cache.Access(1);

            // Act
            cache.Evict(2);

            // Assert
            Assert.That(evicted, Is.Empty);
            Assert.That(cache.GetCached(1, out _, true), Is.True);
        }

        [Test]
        public void GetCached_DoesNotCallFactory()
        {
            // Arrange
            var factoryCalls = 0;
            var cache = Cache.Make<int, string>(
                Cache.Algorithm.LFU,
                key =>
                {
                    factoryCalls++;
                    return "v" + key;
                },
                2);

            // Act & Assert
            Assert.That(cache.GetCached(1, out _, true), Is.False);
            Assert.That(factoryCalls, Is.EqualTo(0));

            cache.Access(1);
            Assert.That(cache.GetCached(1, out var value, true), Is.True);
            Assert.That(value, Is.EqualTo("v1"));
            Assert.That(factoryCalls, Is.EqualTo(1));
        }

        [Test]
        public void GetCached_DontModifyCacheState_DoesNotRefreshLru()
        {
            // Arrange
            var evicted = new List<int>();
            var cache = Cache.Make<int, string>(
                Cache.Algorithm.LRU, key => "v" + key, 2);
            cache.OnEvicted += (key, _) => evicted.Add(key);
            cache.Access(1);
            cache.Access(2);

            // Act
            cache.GetCached(1, out _, dontModifyCacheState: true);
            cache.Access(3);

            // Assert - 1 仍然是最久未使用的，应被淘汰
            Assert.That(evicted, Is.EqualTo(new[] { 1 }));
        }

        [Test]
        public void GetCached_Default_UpdatesLruState()
        {
            // Arrange
            var evicted = new List<int>();
            var cache = Cache.Make<int, string>(
                Cache.Algorithm.LRU, key => "v" + key, 2);
            cache.OnEvicted += (key, _) => evicted.Add(key);
            cache.Access(1);
            cache.Access(2);

            // Act
            cache.GetCached(1, out _, dontModifyCacheState: false);
            cache.Access(3);

            // Assert - 1 被刷新为最近使用，2 应被淘汰
            Assert.That(evicted, Is.EqualTo(new[] { 2 }));
        }

        [Test]
        public void Fifo_Evict_PreservesRemainingEvictionOrder()
        {
            // Arrange
            var evicted = new List<int>();
            var cache = Cache.Make<int, string>(
                Cache.Algorithm.FIFO, key => "v" + key, 3);
            cache.OnEvicted += (key, _) => evicted.Add(key);
            cache.Access(1);
            cache.Access(2);
            cache.Access(3);

            // Act
            cache.Evict(2);
            cache.Access(4);
            cache.Access(5);

            // Assert - 手动移除 2，之后仍按 1、3 的顺序淘汰
            Assert.That(evicted, Is.EqualTo(new[] { 2, 1 }));
        }

        [Test]
        public void Lfu_Evict_RemovesFrequencyBucketCorrectly()
        {
            // Arrange
            var evicted = new List<int>();
            var cache = Cache.Make<int, string>(
                Cache.Algorithm.LFU, key => "v" + key, 2);
            cache.OnEvicted += (key, _) => evicted.Add(key);
            cache.Access(1);
            cache.Access(2);
            cache.Access(1);

            // Act
            cache.Evict(1);
            cache.Access(3);
            cache.Access(4);

            // Assert - 手动移除 1 后，2 是唯一的频率 1 条目，应被下一次淘汰
            Assert.That(evicted, Is.EqualTo(new[] { 1, 2 }));
            Assert.That(cache.GetCached(4, out _, true), Is.True);
        }

        [Test]
        public void AsyncCache_GetCachedAndEvict_WorkCorrectly()
        {
            // Arrange
            var evicted = new List<int>();
            var cache = Cache.MakeAsync<int, string>(
                Cache.Algorithm.LRU,
                (key, onReceive, _) => onReceive("a" + key),
                2);
            cache.OnEvicted += (key, _) => evicted.Add(key);
            cache.Access(1, _ => { }, _ => { });

            // Act & Assert
            Assert.That(
                cache.GetCached(1, out var value, true),
                Is.True);
            Assert.That(value, Is.EqualTo("a1"));

            cache.Evict(1);

            Assert.That(evicted, Is.EqualTo(new[] { 1 }));
            Assert.That(cache.GetCached(1, out _, true), Is.False);
        }

        #endregion

        #region EvictAll / Clear 测试

        [Test]
        public void EvictAll_AllAlgorithms_RemovesAllAndRaisesOnEvicted()
        {
            foreach (var algorithm in new[]
            {
                Cache.Algorithm.FIFO,
                Cache.Algorithm.LRU,
                Cache.Algorithm.LFU,
                Cache.Algorithm.NeverEvict
            })
            {
                // Arrange
                var evictedKeys = new List<int>();
                var evictedValues = new List<string>();
                var cache = Cache.Make<int, string>(
                    algorithm, key => "v" + key, 3);
                cache.OnEvicted += (key, value) =>
                {
                    evictedKeys.Add(key);
                    evictedValues.Add(value);
                };
                cache.Access(1);
                cache.Access(2);
                cache.Access(3);

                // Act
                cache.EvictAll();

                // Assert
                Assert.That(
                    evictedKeys,
                    Is.EquivalentTo(new[] { 1, 2, 3 }),
                    algorithm.ToString());
                Assert.That(
                    evictedValues,
                    Is.EquivalentTo(new[] { "v1", "v2", "v3" }),
                    algorithm.ToString());
                Assert.That(
                    cache.GetCached(1, out _, true),
                    Is.False,
                    algorithm.ToString());
                Assert.That(
                    cache.GetCached(2, out _, true),
                    Is.False,
                    algorithm.ToString());
                Assert.That(
                    cache.GetCached(3, out _, true),
                    Is.False,
                    algorithm.ToString());
            }
        }

        [Test]
        public void EvictAll_EmptyCache_DoesNotRaiseEvent()
        {
            // Arrange
            var evicted = 0;
            var cache = Cache.Make<int, string>(
                Cache.Algorithm.LRU, key => "v" + key, 2);
            cache.OnEvicted += (_, _) => evicted++;

            // Act & Assert
            Assert.DoesNotThrow(() => cache.EvictAll());
            Assert.That(evicted, Is.Zero);
        }

        [Test]
        public void EvictAll_AllAlgorithms_ResetsInternalState()
        {
            foreach (var algorithm in new[]
            {
                Cache.Algorithm.FIFO,
                Cache.Algorithm.LRU,
                Cache.Algorithm.LFU
            })
            {
                // Arrange
                var evicted = new List<int>();
                var cache = Cache.Make<int, string>(
                    algorithm, key => "v" + key, 2);
                cache.OnEvicted += (key, _) => evicted.Add(key);
                cache.Access(1);
                cache.Access(2);
                cache.EvictAll();
                evicted.Clear();

                // Act: 清空后容量重新可用，填满之前不应发生淘汰
                cache.Access(3);
                cache.Access(4);

                // Assert
                Assert.That(evicted, Is.Empty, algorithm.ToString());
                Assert.That(
                    cache.GetCached(3, out _, true),
                    Is.True,
                    algorithm.ToString());
                Assert.That(
                    cache.GetCached(4, out _, true),
                    Is.True,
                    algorithm.ToString());
            }
        }

        [Test]
        public void EvictAll_EventCanReenterCache()
        {
            // Arrange
            var evicted = new List<int>();
            var cache = Cache.Make<int, string>(
                Cache.Algorithm.LRU, key => "v" + key, 2);
            cache.OnEvicted += (key, _) =>
            {
                evicted.Add(key);
                cache.Access(100);
            };
            cache.Access(1);
            cache.Access(2);

            // Act & Assert
            Assert.DoesNotThrow(() => cache.EvictAll());
            Assert.That(evicted, Is.EquivalentTo(new[] { 1, 2 }));
            // 处理器中新写入的项不被本次清空波及
            Assert.That(cache.GetCached(100, out var value, true), Is.True);
            Assert.That(value, Is.EqualTo("v100"));
        }

        [Test]
        public void Clear_AllAlgorithms_RemovesAllWithoutOnEvicted()
        {
            foreach (var algorithm in new[]
            {
                Cache.Algorithm.FIFO,
                Cache.Algorithm.LRU,
                Cache.Algorithm.LFU,
                Cache.Algorithm.NeverEvict
            })
            {
                // Arrange
                var evicted = new List<int>();
                var cache = Cache.Make<int, string>(
                    algorithm, key => "v" + key, 3);
                cache.OnEvicted += (key, _) => evicted.Add(key);
                cache.Access(1);
                cache.Access(2);
                cache.Access(3);

                // Act
                cache.Clear();

                // Assert
                Assert.That(evicted, Is.Empty, algorithm.ToString());
                Assert.That(
                    cache.GetCached(1, out _, true),
                    Is.False,
                    algorithm.ToString());
                Assert.That(
                    cache.GetCached(2, out _, true),
                    Is.False,
                    algorithm.ToString());
                Assert.That(
                    cache.GetCached(3, out _, true),
                    Is.False,
                    algorithm.ToString());

                // 容量仍然可用：重新填充不会触发淘汰
                cache.Access(4);
                Assert.That(evicted, Is.Empty, algorithm.ToString());
            }
        }

        [Test]
        public void AsyncCache_EvictAll_RaisesOnEvictedAndClears()
        {
            // Arrange
            var evictedKeys = new List<int>();
            var evictedValues = new List<string>();
            var cache = Cache.MakeAsync<int, string>(
                Cache.Algorithm.LFU,
                (key, onReceive, _) => onReceive("a" + key),
                3);
            cache.OnEvicted += (key, value) =>
            {
                evictedKeys.Add(key);
                evictedValues.Add(value);
            };
            cache.Access(1, _ => { }, _ => { });
            cache.Access(2, _ => { }, _ => { });

            // Act
            cache.EvictAll();

            // Assert
            Assert.That(evictedKeys, Is.EquivalentTo(new[] { 1, 2 }));
            Assert.That(evictedValues, Is.EquivalentTo(new[] { "a1", "a2" }));
            Assert.That(cache.GetCached(1, out _, true), Is.False);
            Assert.That(cache.GetCached(2, out _, true), Is.False);
        }

        [Test]
        public void AsyncCache_Clear_DoesNotRaiseOnEvicted()
        {
            // Arrange
            var evicted = new List<int>();
            var cache = Cache.MakeAsync<int, string>(
                Cache.Algorithm.LRU,
                (key, onReceive, _) => onReceive("a" + key),
                2);
            cache.OnEvicted += (key, _) => evicted.Add(key);
            cache.Access(1, _ => { }, _ => { });
            cache.Access(2, _ => { }, _ => { });

            // Act
            cache.Clear();

            // Assert
            Assert.That(evicted, Is.Empty);
            Assert.That(cache.GetCached(1, out _, true), Is.False);
            Assert.That(cache.GetCached(2, out _, true), Is.False);
        }

        #endregion

        #region 综合测试

        [Test]
        public void AllAlgorithms_CanCacheReferenceTypes()
        {
            // Arrange
            foreach (var algo in new[]
            {
                Cache.Algorithm.FIFO,
                Cache.Algorithm.LRU,
                Cache.Algorithm.LFU,
                Cache.Algorithm.NeverEvict
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
                Cache.Algorithm.LRU, k => k.Length, 2);

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

        [Test]
        public void Count_TracksEntriesAfterAccessEvictAndClear()
        {
            foreach (var algo in new[]
            {
                Cache.Algorithm.FIFO,
                Cache.Algorithm.LRU,
                Cache.Algorithm.LFU,
                Cache.Algorithm.NeverEvict
            })
            {
                var cache = Cache.Make<string, int>(algo, _ => 1, 2);

                Assert.That(cache.Count, Is.EqualTo(0));

                cache.Access("a");
                cache.Access("b");
                Assert.That(cache.Count, Is.EqualTo(2));

                cache.Evict("a");
                Assert.That(cache.Count, Is.EqualTo(1));

                cache.Clear();
                Assert.That(cache.Count, Is.EqualTo(0));

                cache.Access("c");
                Assert.That(cache.Count, Is.EqualTo(1));

                cache.EvictAll();
                Assert.That(cache.Count, Is.EqualTo(0));
            }
        }

        #endregion
    }
}
