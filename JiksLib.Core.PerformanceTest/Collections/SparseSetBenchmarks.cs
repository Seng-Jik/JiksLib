using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using JiksLib.Collections;

namespace JiksLib.PerformanceTest.Collections
{
    /// <summary>
    /// SparseSet 性能基准测试
    /// </summary>
    [SimpleJob(RuntimeMoniker.Net10_0, invocationCount: 1)]
    [MemoryDiagnoser]
    public class SparseSetBenchmarks
    {
        [Params(10, 100, 1000, 10000)]
        public int ElementCount { get; set; }

        private SparseSet<int> set = null!;
        private int[] testData = null!;
        private int[] targetArray = null!;

        [GlobalSetup]
        public void GlobalSetup()
        {
            testData = new int[ElementCount];
            for (int i = 0; i < ElementCount; i++)
                testData[i] = i;
            targetArray = new int[ElementCount];
        }

        /// <summary>
        /// 默认：预填充 ElementCount 个元素
        /// </summary>
        [IterationSetup]
        public void IterationSetup()
        {
            set = new SparseSet<int>(ElementCount);
            for (int i = 0; i < ElementCount; i++)
                set.Add(testData[i]);
        }

        // ========================================
        // Add
        // ========================================

        [IterationSetup(Target = nameof(Add))]
        public void Setup_Add() => set = new SparseSet<int>();

        /// <summary>
        /// 逐个添加元素（含扩容）
        /// </summary>
        [Benchmark]
        public void Add()
        {
            for (int i = 0; i < ElementCount; i++)
                set.Add(testData[i]);
        }

        [IterationSetup(Target = nameof(Add_WithCapacity))]
        public void Setup_AddWithCapacity() => set = new SparseSet<int>(ElementCount);

        /// <summary>
        /// 逐个添加元素（提前预留容量，无扩容）
        /// </summary>
        [Benchmark]
        public void Add_WithCapacity()
        {
            for (int i = 0; i < ElementCount; i++)
                set.Add(testData[i]);
        }

        // ========================================
        // RemoveAt
        // ========================================

        /// <summary>
        /// 移除第一个元素（触发 swap-down）
        /// </summary>
        [Benchmark]
        public void RemoveAt_First()
        {
            set.RemoveAt(0);
        }

        /// <summary>
        /// 移除最后一个元素（仅缩减计数）
        /// </summary>
        [Benchmark]
        public void RemoveAt_Last()
        {
            set.RemoveAt(ElementCount - 1);
        }

        /// <summary>
        /// 移除中间位置的元素（触发 swap-down）
        /// </summary>
        [Benchmark]
        public void RemoveAt_Middle()
        {
            set.RemoveAt(ElementCount / 2);
        }

        // ========================================
        // Remove (by value)
        // ========================================

        /// <summary>
        /// 按值移除（需要先查找，O(n)）
        /// </summary>
        [Benchmark]
        public void Remove_ByValue()
        {
            set.Remove(ElementCount / 2);
        }

        // ========================================
        // Contains
        // ========================================

        /// <summary>
        /// 查找第一个元素（O(1) 命中）
        /// </summary>
        [Benchmark]
        public void Contains_First()
        {
            _ = set.Contains(0);
        }

        /// <summary>
        /// 查找最后一个元素（O(n) 最坏）
        /// </summary>
        [Benchmark]
        public void Contains_Last()
        {
            _ = set.Contains(ElementCount - 1);
        }

        /// <summary>
        /// 查找不存在的元素（O(n) 最坏）
        /// </summary>
        [Benchmark]
        public void Contains_NonExisting()
        {
            _ = set.Contains(-1);
        }

        // ========================================
        // IndexOf
        // ========================================

        /// <summary>
        /// IndexOf 首元素（O(1) 命中）
        /// </summary>
        [Benchmark]
        public int IndexOf_First() => set.IndexOf(0);

        /// <summary>
        /// IndexOf 末元素（O(n) 最坏）
        /// </summary>
        [Benchmark]
        public int IndexOf_Last() => set.IndexOf(ElementCount - 1);

        /// <summary>
        /// IndexOf 不存在的元素（O(n) 最坏）
        /// </summary>
        [Benchmark]
        public int IndexOf_NonExisting() => set.IndexOf(-1);

        // ========================================
        // TryIndex / Indexer
        // ========================================

        /// <summary>
        /// TryIndex 查找存在的下标（O(1)）
        /// </summary>
        [Benchmark]
        public bool TryIndex() => set.TryIndex(ElementCount / 2, out _);

        /// <summary>
        /// 索引器读取（O(1)）
        /// </summary>
        [Benchmark]
        public int Indexer_Get() => set[ElementCount / 2];

        /// <summary>
        /// 索引器写入（O(1)）
        /// </summary>
        [Benchmark]
        public void Indexer_Set() => set[ElementCount / 2] = 999;

        // ========================================
        // Clear
        // ========================================

        /// <summary>
        /// 清空所有元素（O(n)）
        /// </summary>
        [Benchmark]
        public void Clear() => set.Clear();

        // ========================================
        // Iteration
        // ========================================

        /// <summary>
        /// 无序迭代（按内部缓冲区顺序，缓存友好）
        /// </summary>
        [Benchmark]
        public int UnorderedIteration()
        {
            int sum = 0;
            var enumerator = set.GetEnumerator();
            while (enumerator.MoveNext())
                sum += enumerator.Current;
            return sum;
        }

        /// <summary>
        /// 有序迭代（按稀疏下标顺序，需额外查找）
        /// </summary>
        [Benchmark]
        public int OrderedIteration()
        {
            int sum = 0;
            var enumerator = set.GetOrderedEnumerator;
            while (enumerator.MoveNext())
                sum += enumerator.Current;
            return sum;
        }

        // ========================================
        // CopyTo
        // ========================================

        /// <summary>
        /// 有序复制到数组（按稀疏下标顺序）
        /// </summary>
        [Benchmark]
        public void CopyToOrdered()
        {
            set.CopyToOrdered(new ArraySegment<int>(targetArray));
        }

        /// <summary>
        /// 无序复制到数组（按内部缓冲区顺序，直接 Array.Copy）
        /// </summary>
        [Benchmark]
        public void CopyToUnordered()
        {
            set.CopyToUnordered(new ArraySegment<int>(targetArray));
        }

        // ========================================
        // 下标转换
        // ========================================

        /// <summary>
        /// 内部缓冲区下标 → 稀疏下标
        /// </summary>
        [Benchmark]
        public int InnerBufferIndexToSparseSetIndex()
        {
            return set.InnerBufferIndexToSparseSetIndex(ElementCount / 2);
        }

        /// <summary>
        /// 稀疏下标 → 内部缓冲区下标
        /// </summary>
        [Benchmark]
        public bool SparseSetIndexToInnerBufferIndex()
        {
            return set.SparseSetIndexToInnerBufferIndex(ElementCount / 2, out _);
        }

        // ========================================
        // 构造
        // ========================================

        [IterationSetup(Target = nameof(Constructor_WithCollection))]
        public void Setup_Constructor() { /* 不需要默认的预填充 */ }

        /// <summary>
        /// 从集合构造
        /// </summary>
        [Benchmark]
        public SparseSet<int> Constructor_WithCollection()
        {
            return new SparseSet<int>(testData);
        }

        // ========================================
        // 混合增删
        // ========================================

        [IterationSetup(Target = nameof(Stress_AddRemove))]
        public void Setup_Stress()
        {
            set = new SparseSet<int>(ElementCount);
        }

        /// <summary>
        /// 混合增删压力测试：添加全部后删除全部
        /// </summary>
        [Benchmark]
        public void Stress_AddRemove()
        {
            for (int i = 0; i < ElementCount; i++)
                set.Add(testData[i]);
            for (int i = 0; i < ElementCount; i++)
                set.RemoveAt(i);
        }
    }
}
