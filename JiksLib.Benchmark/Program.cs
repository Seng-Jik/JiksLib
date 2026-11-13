using BenchmarkDotNet.Running;
using JiksLib.Benchmark.Collections;
using JiksLib.Benchmark.Control;

namespace JiksLib.Benchmark
{
    internal class Program
    {
        static void Main(string[] args)
        {
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
        }
    }
}
