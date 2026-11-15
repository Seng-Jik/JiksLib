using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using JiksLib.Collections;

namespace JiksLib.Benchmark.Collections
{
    /// <summary>
    /// ReadOnlyListSegment&lt;T&gt; 与 List&lt;T&gt;、数组、ArraySegment&lt;T&gt;
    /// 在遍历、复制和切片上的成本对比。
    /// 每个 [Benchmark] 内部循环 Iterations 次，因此报告里的 Allocated 与 Mean
    /// 是整轮的值；OperationsPerInvoke 已经把结果换算为单次操作。
    /// </summary>
    [SimpleJob(RuntimeMoniker.Net10_0)]
    [MemoryDiagnoser]
    public class ReadOnlyListSegmentBenchmarks
    {
        private const int Iterations = 100;
        private const int Size = 1024;
        private const int SliceOffset = 128;
        private const int SliceCount = 512;

        private int[] array = null!;
        private List<int> list = null!;
#if NET5_0_OR_GREATER
        private ArraySegment<int> arraySegment;
        private ArraySegment<int> arraySlice;
#endif
        private ReadOnlyListSegment<int> segmentOverArray;
        private ReadOnlyListSegment<int> segmentOverList;
        private ReadOnlyListSegment<int> sliceOverList;
        private int[] buffer = null!;

        [GlobalSetup]
        public void GlobalSetup()
        {
            array = new int[Size];

            for (int i = 0; i < array.Length; i++)
                array[i] = i;

            list = new List<int>(array);
#if NET5_0_OR_GREATER
            arraySegment = new ArraySegment<int>(array);
            arraySlice = new ArraySegment<int>(array, SliceOffset, SliceCount);
#endif
            segmentOverArray = new ReadOnlyListSegment<int>(array);
            segmentOverList = new ReadOnlyListSegment<int>(list);
            sliceOverList = new ReadOnlyListSegment<int>(list, SliceOffset, SliceCount);
            buffer = new int[Size];
        }

        [Benchmark(OperationsPerInvoke = Iterations)]
        public long ArrayIndexer()
        {
            long sum = 0;

            for (int i = 0; i < Iterations; i++)
            {
                for (int j = 0; j < array.Length; j++)
                    sum += array[j];
            }

            return sum;
        }

        [Benchmark(OperationsPerInvoke = Iterations)]
        public long ListIndexer()
        {
            long sum = 0;

            for (int i = 0; i < Iterations; i++)
            {
                for (int j = 0; j < list.Count; j++)
                    sum += list[j];
            }

            return sum;
        }

#if NET5_0_OR_GREATER
        [Benchmark(OperationsPerInvoke = Iterations)]
        public long ArraySegmentIndexer()
        {
            long sum = 0;
            int count = arraySegment.Count;

            for (int i = 0; i < Iterations; i++)
            {
                for (int j = 0; j < count; j++)
                    sum += arraySegment[j];
            }

            return sum;
        }
#endif

        [Benchmark(OperationsPerInvoke = Iterations)]
        public long SegmentOverArrayIndexer()
        {
            long sum = 0;
            int count = segmentOverArray.Count;

            for (int i = 0; i < Iterations; i++)
            {
                for (int j = 0; j < count; j++)
                    sum += segmentOverArray[j];
            }

            return sum;
        }

        [Benchmark(OperationsPerInvoke = Iterations)]
        public long SegmentOverListIndexer()
        {
            long sum = 0;
            int count = segmentOverList.Count;

            for (int i = 0; i < Iterations; i++)
            {
                for (int j = 0; j < count; j++)
                    sum += segmentOverList[j];
            }

            return sum;
        }

        [Benchmark(OperationsPerInvoke = Iterations)]
        public long ListForeach()
        {
            long sum = 0;

            for (int i = 0; i < Iterations; i++)
            {
                foreach (int value in list)
                    sum += value;
            }

            return sum;
        }

        [Benchmark(OperationsPerInvoke = Iterations)]
        public long SegmentForeach()
        {
            long sum = 0;

            for (int i = 0; i < Iterations; i++)
            {
                foreach (int value in segmentOverList)
                    sum += value;
            }

            return sum;
        }

        [Benchmark(OperationsPerInvoke = Iterations)]
        public long ArrayCopy()
        {
            long sum = 0;

            for (int i = 0; i < Iterations; i++)
            {
                Array.Copy(array, buffer, array.Length);
                sum += buffer[0];
            }

            return sum;
        }

        [Benchmark(OperationsPerInvoke = Iterations)]
        public long ListCopyTo()
        {
            long sum = 0;

            for (int i = 0; i < Iterations; i++)
            {
                list.CopyTo(buffer);
                sum += buffer[0];
            }

            return sum;
        }

        [Benchmark(OperationsPerInvoke = Iterations)]
        public long SegmentCopyTo()
        {
            long sum = 0;

            for (int i = 0; i < Iterations; i++)
            {
                segmentOverList.CopyTo(buffer);
                sum += buffer[0];
            }

            return sum;
        }

        [Benchmark(OperationsPerInvoke = Iterations)]
        public long ManualSliceToArray()
        {
            long sum = 0;

            for (int i = 0; i < Iterations; i++)
            {
                var copy = new int[SliceCount];
                list.CopyTo(SliceOffset, copy, 0, SliceCount);
                sum += copy[0];
            }

            return sum;
        }

        [Benchmark(OperationsPerInvoke = Iterations)]
        public long SegmentToArray()
        {
            long sum = 0;

            for (int i = 0; i < Iterations; i++)
            {
                var copy = sliceOverList.ToArray();
                sum += copy[0];
            }

            return sum;
        }

#if NET5_0_OR_GREATER
        [Benchmark(OperationsPerInvoke = Iterations)]
        public long ArraySegmentSlice()
        {
            long sum = 0;

            for (int i = 0; i < Iterations; i++)
                sum += arraySlice.Slice(64, 256).Count;

            return sum;
        }
#endif

        [Benchmark(OperationsPerInvoke = Iterations)]
        public long SegmentSlice()
        {
            long sum = 0;

            for (int i = 0; i < Iterations; i++)
                sum += sliceOverList.Slice(64, 256).Count;

            return sum;
        }
    }
}
