using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using JiksLib.Collections;

namespace JiksLib.Benchmark.Collections
{
    /// <summary>
    /// Cache 手动 Evict 在不同算法下的成本对比。
    /// FIFO 需要维护顺序队列，LRU/LFU 为节点移除，NeverEvict 仅字典操作。
    /// </summary>
    [SimpleJob(RuntimeMoniker.Net10_0, invocationCount: 1)]
    [MemoryDiagnoser]
    public class CacheEvictionBenchmarks
    {
        private const int Capacity = 1000;
        private const int Key = 500;

        private ICache<int, string> cache = null!;

        [Params(
            Cache.Algorithm.FIFO,
            Cache.Algorithm.LRU,
            Cache.Algorithm.LFU,
            Cache.Algorithm.NeverEvict)]
        public Cache.Algorithm Algorithm { get; set; }

        [GlobalSetup]
        public void GlobalSetup()
        {
            cache = Cache.Make<int, string>(
                Algorithm,
                key => "v" + key,
                Capacity);

            for (int i = 0; i < Capacity; ++i)
                cache.Access(i);
        }

        /// <summary>
        /// 基准测试：移除一个已存在的键，再重新创建以维持容量。
        /// </summary>
        [Benchmark]
        public void EvictAndReinsert()
        {
            cache.Evict(Key);
            cache.Access(Key);
        }
    }
}
