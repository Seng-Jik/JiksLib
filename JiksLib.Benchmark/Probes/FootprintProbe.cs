using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using JiksLib.Collections;
using JiksLib.Control;

namespace JiksLib.Benchmark.Probes
{
    /// <summary>
    /// 内存保留与碎片探针。
    /// BenchmarkDotNet 只报告每次操作的分配量与耗时，不报告 GC 后的碎片和结构的保留内存；
    /// 这两项由本探针测量（规则见 ai-skill/performance-test/references/metrics.md）。
    /// 用法：JiksLib.Benchmark probe &lt;scenario&gt; [trial]
    /// 每个进程只测量一次，重复测量请在外层循环调用，避免前一次测量污染堆。
    /// </summary>
    internal static class FootprintProbe
    {
        private const int ElementCount = 100_000;
        private const int ListenerCount = 10_000;

        private static readonly string[] ScenarioNames =
        {
            "sparse-set-reserved",
            "sparse-set-growth",
            "eventbus-listeners",
        };

        public static int Run(string[] args)
        {
            if (args.Length < 2 || IsHelp(args[1]))
            {
                PrintUsage();
                return args.Length < 2 ? 2 : 0;
            }

            if (args[1] == "--list")
            {
                foreach (string name in ScenarioNames)
                    Console.WriteLine(name);
                return 0;
            }

#if NET5_0_OR_GREATER
            string trialArgument = args.Length > 2 ? args[2] : "1";
            if (!int.TryParse(
                    trialArgument,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out int trial))
            {
                Console.Error.WriteLine($"trial 必须是整数：{trialArgument}");
                return 2;
            }

            try
            {
                return Measure(args[1], trial);
            }
            catch (ArgumentException ex)
            {
                Console.Error.WriteLine(ex.Message);
                PrintUsage();
                return 2;
            }
#else
            Console.Error.WriteLine(
                "probe 需要 .NET 5 及以上（GC.GetGCMemoryInfo / GC.GetTotalAllocatedBytes），当前目标框架不支持。");
            return 3;
#endif
        }

        private static bool IsHelp(string arg) => arg is "-h" or "--help" or "/?";

        private static void PrintUsage()
        {
            Console.WriteLine("用法：JiksLib.Benchmark probe <scenario> [trial]");
            Console.WriteLine("      JiksLib.Benchmark probe --list");
            Console.WriteLine();
            Console.WriteLine("场景（每个进程只测一次，重复测量请在外层循环调用）：");
            foreach (string name in ScenarioNames)
                Console.WriteLine($"  {name}");
        }

#if NET5_0_OR_GREATER
        private sealed class ProbeEvent
        {
            // 事件总线要求事件是 class；本探针只需要一个可注册监听的空类型。
        }

        private static readonly JiksLib.Control.EventHandler<ProbeEvent> SharedListener = static _ => { };

        private static int Measure(string scenario, int trial)
        {
            FullGc(compacting: true);
            long baseline = GC.GetTotalMemory(forceFullCollection: true);
            long allocatedBefore = GC.GetTotalAllocatedBytes(precise: true);

            int itemCount;
            object keep;

            switch (scenario)
            {
                case "sparse-set-reserved":
                    itemCount = ElementCount;
                    keep = BuildSparseSet(reserved: true);
                    break;
                case "sparse-set-growth":
                    itemCount = ElementCount;
                    keep = BuildSparseSet(reserved: false);
                    break;
                case "eventbus-listeners":
                    itemCount = ListenerCount;
                    keep = BuildEventBus();
                    break;
                default:
                    throw new ArgumentException($"未知场景：{scenario}");
            }

            long allocated = GC.GetTotalAllocatedBytes(precise: true) - allocatedBefore;
            long retained = GC.GetTotalMemory(forceFullCollection: true) - baseline;

            // 非压缩 GC 才能看到 SOH/LOH 的真实碎片；压缩 GC 会抹掉 SOH 碎片。
            FullGc(compacting: false);
            GCMemoryInfo info = GC.GetGCMemoryInfo();

            Console.WriteLine(BuildJson(
                scenario,
                trial,
                itemCount,
                retained,
                allocated,
                info.HeapSizeBytes,
                info.FragmentedBytes,
                info.PinnedObjectsCount,
                Process.GetCurrentProcess().PeakWorkingSet64));

            GC.KeepAlive(keep);
            return 0;
        }

        private static SparseSet<int> BuildSparseSet(bool reserved)
        {
            var set = reserved ? new SparseSet<int>(ElementCount) : new SparseSet<int>();
            for (int i = 0; i < ElementCount; i++)
                set.Add(i);
            return set;
        }

        private static EventBus<ProbeEvent> BuildEventBus()
        {
            var bus = new EventBus<ProbeEvent>(out _);
            for (int i = 0; i < ListenerCount; i++)
                bus.AddListener(SharedListener);
            return bus;
        }

        private static void FullGc(bool compacting)
        {
            GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: compacting);
            GC.WaitForPendingFinalizers();
            GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: compacting);
        }

        private static string BuildJson(
            string scenario,
            int trial,
            int itemCount,
            long retainedBytes,
            long allocatedBytes,
            long heapSizeBytes,
            long fragmentedBytes,
            long pinnedObjects,
            long peakWorkingSetBytes)
        {
            CultureInfo culture = CultureInfo.InvariantCulture;
            double retainedPerItem = (double)retainedBytes / itemCount;
            double allocatedPerItem = (double)allocatedBytes / itemCount;
            double fragmentedPercent = heapSizeBytes == 0
                ? 0.0
                : 100.0 * fragmentedBytes / heapSizeBytes;

            var properties = new List<string>(14)
            {
                $"\"scenario\":\"{Escape(scenario)}\"",
                $"\"trial\":{trial.ToString(culture)}",
                $"\"n\":{itemCount.ToString(culture)}",
                $"\"retainedBytes\":{retainedBytes.ToString(culture)}",
                $"\"retainedPerItem\":{retainedPerItem.ToString("R", culture)}",
                $"\"allocatedBytes\":{allocatedBytes.ToString(culture)}",
                $"\"allocatedPerItem\":{allocatedPerItem.ToString("R", culture)}",
                $"\"heapSizeBytes\":{heapSizeBytes.ToString(culture)}",
                $"\"fragmentedBytes\":{fragmentedBytes.ToString(culture)}",
                $"\"fragmentedPercent\":{fragmentedPercent.ToString("R", culture)}",
                $"\"pinnedObjects\":{pinnedObjects.ToString(culture)}",
                $"\"peakWorkingSetBytes\":{peakWorkingSetBytes.ToString(culture)}",
                $"\"runtime\":\"{Escape(Environment.Version.ToString())}\"",
                $"\"at\":\"{DateTimeOffset.Now.ToString("O", culture)}\"",
            };

            return "{" + string.Join(",", properties) + "}";
        }

        private static string Escape(string value)
        {
            var builder = new StringBuilder(value.Length + 8);
            foreach (char c in value)
            {
                switch (c)
                {
                    case '"': builder.Append("\\\""); break;
                    case '\\': builder.Append("\\\\"); break;
                    case '\n': builder.Append("\\n"); break;
                    case '\r': builder.Append("\\r"); break;
                    case '\t': builder.Append("\\t"); break;
                    default: builder.Append(c); break;
                }
            }
            return builder.ToString();
        }
#endif
    }
}
