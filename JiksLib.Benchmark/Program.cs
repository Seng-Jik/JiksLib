using System.Linq;
using BenchmarkDotNet.Running;
using JiksLib.Benchmark.Collections;
using JiksLib.Benchmark.Control;
using JiksLib.Benchmark.Probes;
using JiksLib.Benchmark.Text;

namespace JiksLib.Benchmark
{
    internal class Program
    {
        static int Main(string[] args)
        {
            // 内存/碎片探针：BDN 不提供这两项，用独立进程测量并输出一行 JSON。
            // 用法：JiksLib.Benchmark probe <scenario> [trial]
            if (args.Length > 0 && args[0] == "probe")
                return FootprintProbe.Run(args);

            // 使用 BenchmarkSwitcher 允许通过命令行参数选择要运行的基准测试
            var switcher = new BenchmarkSwitcher(new[]
            {
                typeof(EventBusBenchmarks),
                typeof(ListenerRemovalOrderBenchmarks),
                typeof(EventBusTypeChainBenchmarks),
                typeof(EventBusWarmupComparisonBenchmarks),
                typeof(EventBusClearTypeChainCacheBenchmarks),
                typeof(ValueEventBusBenchmarks),
                typeof(SafeEventBenchmarks),
                typeof(SafeEventRecursiveBenchmarks),
                typeof(EventBusComparisonBenchmarks),
                typeof(ValueEventBusInterfaceBenchmarks),
                typeof(SparseSetBenchmarks),
                typeof(CacheBenchmarks),
                typeof(CacheEvictionBenchmarks),
                typeof(RingBufferBenchmarks),
                typeof(ReadOnlyListSegmentBenchmarks),
                typeof(StringSegmentBenchmarks),
            });

            // 没有任何基准被选中（filter 拼错、类被改名等）时返回非零，避免 CI 静默通过。
            return switcher.Run(args).Any() ? 0 : 1;
        }
    }
}
