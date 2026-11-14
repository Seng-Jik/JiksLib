using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using JiksLib.Collections;

namespace JiksLib.Benchmark.Collections
{
    /// <summary>
    /// RingBuffer 写入、覆盖、随机访问、枚举、移除与复制路径的基准测试。
    /// </summary>
    /// <remarks>
    /// 每个 [Benchmark] 在一次调用内部循环 Capacity 次，因此 Allocated 与 Mean 是「整轮 Capacity 次操作」的值；
    /// 要比较单次操作，请除以当前的 Capacity。BDN 的 [Benchmark(OperationsPerInvoke = N)] 只接受编译期常量，
    /// 无法随 [Params] 变化，所以这里用注释说明而不是该特性。
    /// 同时提供 Deque 的对应操作作为参照：Deque.Add 是追加入队，RingBuffer.Add 是写入到下标 0；
    /// Deque.TryRemoveFront 与 RingBuffer.TryRemoveOldest 都移除最旧元素，可直接比较。
    /// </remarks>
    [SimpleJob(RuntimeMoniker.Net10_0, invocationCount: 1)]
    [MemoryDiagnoser]
    public class RingBufferBenchmarks
    {
        [Params(16, 256, 4096)]
        public int Capacity { get; set; }

        private RingBuffer<int> ring = null!;
        private Deque<int> deque = null!;
        private int[] data = null!;
        private int[] destination = null!;

        [GlobalSetup]
        public void GlobalSetup()
        {
            data = new int[Capacity];
            for (int i = 0; i < Capacity; i++)
                data[i] = i;

            destination = new int[Capacity];
        }

        // ========================================
        // Add
        // ========================================

        [IterationSetup(Target = nameof(Add_Fill))]
        public void Setup_Add_Fill() => ring = new RingBuffer<int>(Capacity);

        /// <summary>
        /// 从空缓存写入 Capacity 个元素，不触发覆盖
        /// </summary>
        /// <remarks>Allocated 是整轮 Capacity 次 Add 的合计值，不是单次 Add 的分配。</remarks>
        [Benchmark]
        public void Add_Fill()
        {
            for (int i = 0; i < Capacity; i++)
                ring.Add(data[i]);
        }

        [IterationSetup(Target = nameof(Add_Overwrite))]
        public void Setup_Add_Overwrite() => ring = CreateFilledRing();

        /// <summary>
        /// 已写满时继续写入 Capacity 个元素，每次写入都覆盖最旧元素
        /// </summary>
        [Benchmark]
        public void Add_Overwrite()
        {
            for (int i = 0; i < Capacity; i++)
                ring.Add(data[i]);
        }

        [IterationSetup(Target = nameof(Add_WithEvicted_Overwrite))]
        public void Setup_Add_WithEvicted_Overwrite() => ring = CreateFilledRing();

        /// <summary>
        /// 已写满时继续写入 Capacity 个元素，并取回被覆盖的元素
        /// </summary>
        [Benchmark]
        public void Add_WithEvicted_Overwrite()
        {
            for (int i = 0; i < Capacity; i++)
                ring.Add(data[i], out _);
        }

        // ========================================
        // AddRange
        // ========================================

        [IterationSetup(Target = nameof(AddRange_ArraySegment))]
        public void Setup_AddRange_ArraySegment() => ring = new RingBuffer<int>(Capacity);

        /// <summary>
        /// 用数组片段一次性写入 Capacity 个元素（一次调用内部逆序复制整个片段）
        /// </summary>
        [Benchmark]
        public void AddRange_ArraySegment()
        {
            ring.AddRange(new ArraySegment<int>(data));
        }

        // ========================================
        // 读取
        // ========================================

        [IterationSetup(Target = nameof(Indexer_Sequential))]
        public void Setup_Indexer_Sequential() => ring = CreateFilledRing();

        /// <summary>
        /// 按下标顺序读取全部元素
        /// </summary>
        [Benchmark]
        public int Indexer_Sequential()
        {
            int sum = 0;
            for (int i = 0; i < Capacity; i++)
                sum += ring[i];

            return sum;
        }

        [IterationSetup(Target = nameof(Enumerate))]
        public void Setup_Enumerate() => ring = CreateFilledRing();

        /// <summary>
        /// 用结构体枚举器遍历全部元素
        /// </summary>
        [Benchmark]
        public int Enumerate()
        {
            int sum = 0;
            foreach (var value in ring)
                sum += value;

            return sum;
        }

        [IterationSetup(Target = nameof(CopyTo))]
        public void Setup_CopyTo() => ring = CreateFilledRing();

        /// <summary>
        /// 将全部元素复制到已分配的数组中
        /// </summary>
        [Benchmark]
        public void CopyTo()
        {
            ring.CopyTo(destination, 0);
        }

        [IterationSetup(Target = nameof(ToArray))]
        public void Setup_ToArray() => ring = CreateFilledRing();

        /// <summary>
        /// 将全部元素转换为新数组（包含一次容量为 Capacity 的数组分配）
        /// </summary>
        [Benchmark]
        public int[] ToArray()
        {
            return ring.ToArray();
        }

        // ========================================
        // 移除
        // ========================================

        [IterationSetup(Target = nameof(TryRemoveNewest_All))]
        public void Setup_TryRemoveNewest_All() => ring = CreateFilledRing();

        /// <summary>
        /// 逐个移除最新元素直到缓存为空
        /// </summary>
        [Benchmark]
        public void TryRemoveNewest_All()
        {
            while (ring.TryRemoveNewest(out _)) { }
        }

        [IterationSetup(Target = nameof(TryRemoveOldest_All))]
        public void Setup_TryRemoveOldest_All() => ring = CreateFilledRing();

        /// <summary>
        /// 逐个移除最旧元素直到缓存为空
        /// </summary>
        [Benchmark]
        public void TryRemoveOldest_All()
        {
            while (ring.TryRemoveOldest(out _)) { }
        }

        [IterationSetup(Target = nameof(Clear))]
        public void Setup_Clear() => ring = CreateFilledRing();

        /// <summary>
        /// 清空已写满的缓存
        /// </summary>
        [Benchmark]
        public void Clear()
        {
            ring.Clear();
        }

        // ========================================
        // Deque 参照
        // ========================================

        [IterationSetup(Target = nameof(Deque_Add_Fill))]
        public void Setup_Deque_Add_Fill() => deque = new Deque<int>(Capacity);

        /// <summary>
        /// Deque 参照：从空队列追加入队 Capacity 个元素
        /// </summary>
        [Benchmark]
        public void Deque_Add_Fill()
        {
            for (int i = 0; i < Capacity; i++)
                deque.Add(data[i]);
        }

        [IterationSetup(Target = nameof(Deque_Indexer_Sequential))]
        public void Setup_Deque_Indexer_Sequential()
        {
            deque = new Deque<int>(Capacity);
            for (int i = 0; i < Capacity; i++)
                deque.Add(data[i]);
        }

        /// <summary>
        /// Deque 参照：按下标顺序读取全部元素
        /// </summary>
        [Benchmark]
        public int Deque_Indexer_Sequential()
        {
            int sum = 0;
            for (int i = 0; i < Capacity; i++)
                sum += deque[i];

            return sum;
        }

        [IterationSetup(Target = nameof(Deque_TryRemoveFront_All))]
        public void Setup_Deque_TryRemoveFront_All()
        {
            deque = new Deque<int>(Capacity);
            for (int i = 0; i < Capacity; i++)
                deque.Add(data[i]);
        }

        /// <summary>
        /// Deque 参照：逐个移除队首（最旧元素）直到为空
        /// </summary>
        [Benchmark]
        public void Deque_TryRemoveFront_All()
        {
            while (deque.TryRemoveFront(out _)) { }
        }

        private RingBuffer<int> CreateFilledRing()
        {
            var result = new RingBuffer<int>(Capacity);
            for (int i = 0; i < Capacity; i++)
                result.Add(data[i]);

            return result;
        }
    }
}
