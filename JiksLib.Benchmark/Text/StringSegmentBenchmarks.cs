using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using JiksLib.Extensions;

namespace JiksLib.Benchmark.Text
{
    /// <summary>
    /// StringSegment 与 string 在切片、切分、查找和比较上的成本对比。
    /// 每个 [Benchmark] 内部循环 Iterations 次，因此报告里的 Allocated 与 Mean
    /// 是整轮的值；OperationsPerInvoke 已经把结果换算为单次操作。
    /// </summary>
    [SimpleJob(RuntimeMoniker.Net10_0, invocationCount: 1)]
    [MemoryDiagnoser]
    public class StringSegmentBenchmarks
    {
        private const int Iterations = 100;

        private string csv = null!;
        private StringSegment segment;
        private string needle = null!;
        private StringSegment needleSegment;
        private string prefix = null!;
        private StringSegment prefixSegment;
        private string padded = null!;
        private StringSegment paddedSegment;
        private string leftString = null!;
        private string rightString = null!;
        private StringSegment left;
        private StringSegment right;

        [GlobalSetup]
        public void GlobalSetup()
        {
            var fields = new string[64];

            for (int i = 0; i < fields.Length; i++)
                fields[i] = "field" + i + new string('x', i % 7);

            csv = string.Join(",", fields);
            segment = new StringSegment(csv);

            needle = "field32";
            needleSegment = new StringSegment(needle);

            prefix = "field0";
            prefixSegment = new StringSegment(prefix);

            padded = "   " + csv.Substring(0, 32) + "   ";
            paddedSegment = new StringSegment(padded);

            leftString = "0123456789abcdef";
            rightString = "0123456789abcdef";
            left = new StringSegment(leftString);
            right = new StringSegment(rightString);
        }

        /// <summary>
        /// 无分配切分：Tokenize 只推进区间，不创建子字符串和数组。
        /// </summary>
        [Benchmark(OperationsPerInvoke = Iterations)]
        public int Tokenize()
        {
            int sum = 0;

            for (int i = 0; i < Iterations; i++)
            {
                var tokenizer = segment.Tokenize(',');

                while (tokenizer.MoveNext())
                    sum += tokenizer.Current.Length;
            }

            return sum;
        }

        /// <summary>
        /// 基线：string.Split 每次都分配数组和每个子字符串。
        /// </summary>
        [Benchmark(OperationsPerInvoke = Iterations)]
        public int StringSplit()
        {
            int sum = 0;

            for (int i = 0; i < Iterations; i++)
            {
                foreach (string part in csv.Split(','))
                    sum += part.Length;
            }

            return sum;
        }

        /// <summary>
        /// 基线：StringExtension.Split0 的 string 版本。
        /// </summary>
        [Benchmark(OperationsPerInvoke = Iterations)]
        public int StringSplit0()
        {
            int sum = 0;

            for (int i = 0; i < Iterations; i++)
            {
                foreach (string part in csv.Split0(','))
                    sum += part.Length;
            }

            return sum;
        }

        /// <summary>
        /// 片段版本 Split0：分配数组与片段结构，但不分配子字符串。
        /// </summary>
        [Benchmark(OperationsPerInvoke = Iterations)]
        public int SegmentSplit0()
        {
            int sum = 0;

            for (int i = 0; i < Iterations; i++)
            {
                foreach (var part in segment.Split0(','))
                    sum += part.Length;
            }

            return sum;
        }

        /// <summary>
        /// 无分配切片。
        /// </summary>
        [Benchmark(OperationsPerInvoke = Iterations)]
        public int SegmentSlice()
        {
            int sum = 0;

            for (int i = 0; i < Iterations; i++)
                sum += segment.Slice(10, 32).Length;

            return sum;
        }

        /// <summary>
        /// 基线：string.Substring 每次分配一个新字符串。
        /// </summary>
        [Benchmark(OperationsPerInvoke = Iterations)]
        public int StringSubstring()
        {
            int sum = 0;

            for (int i = 0; i < Iterations; i++)
                sum += csv.Substring(10, 32).Length;

            return sum;
        }

        /// <summary>
        /// 无分配 Trim：只移动区间端点。
        /// </summary>
        [Benchmark(OperationsPerInvoke = Iterations)]
        public int SegmentTrim()
        {
            int sum = 0;

            for (int i = 0; i < Iterations; i++)
                sum += paddedSegment.Trim().Length;

            return sum;
        }

        /// <summary>
        /// 基线：string.Trim 在需要裁剪时分配新字符串。
        /// </summary>
        [Benchmark(OperationsPerInvoke = Iterations)]
        public int StringTrim()
        {
            int sum = 0;

            for (int i = 0; i < Iterations; i++)
                sum += padded.Trim().Length;

            return sum;
        }

        /// <summary>
        /// 片段内查找，返回相对于片段的下标。
        /// </summary>
        [Benchmark(OperationsPerInvoke = Iterations)]
        public int SegmentIndexOfString()
        {
            int sum = 0;

            for (int i = 0; i < Iterations; i++)
                sum += segment.IndexOf(needleSegment);

            return sum;
        }

        /// <summary>
        /// 基线：string.IndexOf。
        /// </summary>
        [Benchmark(OperationsPerInvoke = Iterations)]
        public int StringIndexOfString()
        {
            int sum = 0;

            for (int i = 0; i < Iterations; i++)
                sum += csv.IndexOf(needle, StringComparison.Ordinal);

            return sum;
        }

        /// <summary>
        /// 片段相等比较。
        /// </summary>
        [Benchmark(OperationsPerInvoke = Iterations)]
        public bool SegmentEquals()
        {
            bool result = false;

            for (int i = 0; i < Iterations; i++)
                result ^= left.Equals(right);

            return result;
        }

        /// <summary>
        /// 基线：string 相等比较。
        /// </summary>
        [Benchmark(OperationsPerInvoke = Iterations)]
        public bool StringEquals()
        {
            bool result = false;

            for (int i = 0; i < Iterations; i++)
                result ^= leftString.Equals(rightString);

            return result;
        }

        /// <summary>
        /// 片段前缀判断。
        /// </summary>
        [Benchmark(OperationsPerInvoke = Iterations)]
        public bool SegmentStartsWith()
        {
            bool result = false;

            for (int i = 0; i < Iterations; i++)
                result ^= segment.StartsWith(prefixSegment);

            return result;
        }

        /// <summary>
        /// 基线：string.StartsWith。
        /// </summary>
        [Benchmark(OperationsPerInvoke = Iterations)]
        public bool StringStartsWith()
        {
            bool result = false;

            for (int i = 0; i < Iterations; i++)
                result ^= csv.StartsWith(prefix, StringComparison.Ordinal);

            return result;
        }
    }
}
