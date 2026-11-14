using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using JiksLib.Collections;

namespace JiksLib.Benchmark.Collections
{
    /// <summary>
    /// Cache 访问、普通 GetCached 与只读 GetCached 的对比。
    /// </summary>
    [SimpleJob(RuntimeMoniker.Net10_0, invocationCount: 1)]
    [MemoryDiagnoser]
    public class CacheBenchmarks
    {
        private const int Key = 1;

        private ICache<int, string> cache = null!;

        [Params(Cache.Algorithm.LRU, Cache.Algorithm.LFU)]
        public Cache.Algorithm Algorithm { get; set; }

        [GlobalSetup]
        public void GlobalSetup()
        {
            cache = Cache.Make<int, string>(
                Algorithm,
                key => "v" + key,
                8);

            cache.Access(Key);
        }

        /// <summary>
        /// 基准测试：已命中时走 Access。
        /// </summary>
        [Benchmark(Baseline = true)]
        public string AccessHit()
        {
            return cache.Access(Key);
        }

        /// <summary>
        /// 基准测试：GetCached，默认按正常访问更新 LRU/LFU 状态。
        /// </summary>
        [Benchmark]
        public bool GetCachedDefault()
        {
            return cache.GetCached(Key, out _, dontModifyCacheState: false);
        }

        /// <summary>
        /// 基准测试：GetCached，只读查询，不更新 LRU/LFU 状态。
        /// </summary>
        [Benchmark]
        public bool GetCachedPeek()
        {
            return cache.GetCached(Key, out _, dontModifyCacheState: true);
        }
    }
}
