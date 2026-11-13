using BenchmarkDotNet.Running;
using JiksLib.Benchmark.Collections;
using JiksLib.Benchmark.Control;
using JiksLib.Benchmark.Probes;

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
                typeof(ValueEventBusBenchmarks),
                typeof(EventBusComparisonBenchmarks),
                typeof(ValueEventBusInterfaceBenchmarks),
                typeof(SparseSetBenchmarks),
            });

            switcher.Run(args);
            return 0;
        }
    }
}
