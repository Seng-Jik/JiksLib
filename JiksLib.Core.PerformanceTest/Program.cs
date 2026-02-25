using BenchmarkDotNet.Running;
using JiksLib.PerformanceTest.Control;

namespace JiksLib.PerformanceTest
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
                typeof(EventBusComparisonBenchmarks)
            });

            switcher.Run(args);
        }
    }
}
