# AI 评价 — v2.0.0（源 run 202611251855-791746c6）

> 归档说明：本文件是 `artifacts/benchmark-result/202611251855-791746c6/ai-evaluation.md` 的归档副本，**数值与结论未改动**；只把标题和“目录”一行改为归档位置，源 run 标识保留用于追溯。基线元数据见 `baseline.json`。

## 1. 运行概况

| 项 | 值 |
| --- | --- |
| 目录 | `JiksLib.Benchmark/ArchivedResult/v2.0.0/`（源 run `202611251855-791746c6`） |
| 提交 | `791746c6`（`main`；运行开始 clean，运行中 `README.md` 与 `JiksLib/JiksLib.csproj` 被外部修改，见 §6） |
| 命令 | `dotnet run --project JiksLib.Benchmark/JiksLib.Benchmark.csproj -c Release -f net10.0 -- --filter '*' --allStats --artifacts <run>/raw` |
| Job | `SimpleJob(RuntimeMoniker.Net10_0)` + `MemoryDiagnoser`；部分类固定 `invocationCount: 1` |
| 基准类型 | 16 个类 / **341 个基准用例**（全部执行完成，无 `Benchmarks with issues`） |
| 环境 | .NET SDK 11.0.100 / host runtime 10.0.15，Windows 11 26H2 (26300.9974)，i5-12600KF（10 物理 / 16 逻辑核），15.8 GB |
| 探针 | 内置 `probe`：3 场景 × 3 次，`probe/memory.jsonl` |
| 原始数据 | `raw/results/*-report-{md,csv,html}`、`raw/console.log` |

全局运行时间 22 分 07 秒（1327.31 s）。本次是**首次基线**，没有可对比的上一次成功运行；`202611251850-791746c6` 是构建失败的运行，已按失败归档，不参与对比。

## 2. 指标总览（按优先级）

只列需要说明的代表行；全部 341 行见 §8 附录，单位与报告一致。`Gen0` 为空表示该类本次没有触发 GC（BDN 隐藏全 0 列），不代表未测量。

| 基准（类.方法 [参数]） | Allocated | Gen0(/1000 ops) | Mean | Median | 碎片率 | 保留内存 |
| --- | --- | --- | --- | --- | --- | --- |
| `SafeEvent.PublishEvent_WithListeners [1/10/100]` | 0 B | — | 303.0 / 658.3 / 1,715.0 ns | 300 / 600 / 1,600 ns | N/A（未对该类跑探针） | N/A |
| `SafeEvent.PublishEvent_NoListeners [1/10/100]` | 0 B | — | 150.5 / 110.2 / 267.3 ns | 100 / 100 / 300 ns | — | — |
| `SafeEvent.AddAndRemoveListeners [1/10/100]`（预分配） | 0 B | — | 779.6 / 1,425.3 / 4,444.4 ns | 800 / 1,400 / 4,500 ns | — | — |
| `SafeEvent.AddAndRemoveListeners_DefaultCapacity [1/10/100]` | 56 / 296 / 2,160 B | — | 1,445.0 / 2,437.2 / 6,309.1 ns | 1,500 / 2,500 / 5,900 ns | — | — |
| `EventBus.PublishEvent_WithListeners [1/10/100/1000]` | 0 B | — | 1,698.9 / 1,647.8 / 2,935.4 / 17,645.7 ns | 1,600 / 1,600 / 2,900 / 17,500 ns | — | — |
| `EventBus.PublishEvent_NoListeners [1/10/100/1000]` | 0 B | — | 969.1 / 797.9 / 838.8 / 840.4 ns | 900 / 700 / 800 / 800 ns | — | — |
| `EventBus.AddListeners [1000]` / `_WithWarmup [1000]` | 16,840 B / 0 B | — | 115.5 / 116.0 μs | 114.0 / 114.2 μs | — | — |
| `ValueEventBus.PublishEvent_WithListeners [1000]` | 0 B | — | 17,141.2 ns | 17,100 ns | — | — |
| `ValueEventBus.PublishEvent_ViaSubPublisher [1000]` | 0 B | — | 15,666.3 ns | 15,500 ns | — | — |
| `ValueEventBusInterface.PublishViaInterface_NoListeners [1]` | 24 B | — | 1,138.0 ns | 1,100 ns | — | — |
| `ValueEventBusInterface.PublishDirect_NoListeners [1]` | 0 B | — | 217.2 ns | 200 ns | — | — |
| `SparseSet.Add [10,000]` / `Add_WithCapacity [10,000]` | 295,032 B / 0 B | — | 48,721.6 / 37,856.8 ns | 48,300 / 37,700 ns | — | — |
| `SparseSet.AddRange_ArraySegment [10,000]` | 0 B | — | 28,520.2 ns | 28,500 ns | — | — |
| `SparseSet.Constructor_WithCollection [10,000]` | 120,184 B | — | 44,620.0 ns | 49,300 ns | — | — |
| `Cache.AccessHit [LRU / LFU]` | 0 B | — | 2.175 / 20.923 ns | 2.174 / 20.859 ns | — | — |
| `RingBuffer.Add_Fill [4096]` | 0 B | — | 13,802.1 ns | 13,400 ns | — | — |
| `RingBuffer.ToArray [4096]` | 16,408 B | — | 4,377.1 ns | 5,900 ns | — | — |
| `ReadOnlyListSegment.SegmentToArray` | 2,072 B | 0.1981 | 99.1 ns | 99.0 ns | — | — |
| `ReadOnlyListSegment.SegmentOverArrayIndexer` | 0 B | 0.0000 | 576.1 ns | 575.4 ns | — | — |
| `StringSegment.Tokenize` | 0 B | 0.0000 | 360.6 ns | 359.6 ns | — | — |
| `StringSegment.StringSplit` | 3,376 B | 0.3229 | 466.9 ns | 466.1 ns | — | — |
| `StringSegment.SegmentIndexOfString` / `StringIndexOfString` | 0 B / 0 B | 0.0000 / 0.0000 | 216.4 / 15.2 ns | 216.3 / 15.1 ns | — | — |
| 探针 `sparse-set-reserved`（N=100,000） | 1,200,184 B 总分配 | — | — | — | **0.039%**（576 B） | **1,200,160 B**（12.00 B/元素） |
| 探针 `sparse-set-growth`（N=100,000） | 4,718,920 B 总分配 | — | — | — | **32.64%**（1,278,848 B） | **2,359,432 B**（23.59 B/元素） |
| 探针 `eventbus-listeners`（N=10,000） | 263,432 B 总分配 | — | — | — | **0.14%**（576 B） | **132,040 B**（13.20 B/监听器） |

## 3. 关键结论（最多 3 条）

1. **稳态热路径的零分配目标成立。** 事件发布（`EventBus` / `ValueEventBus` / `SafeEvent`，含 1000 监听器）、缓存命中、`RingBuffer` 增删读写、`ReadOnlyListSegment` 索引/枚举/复制、`StringSegment` 的切片/Trim/Tokenize/比较全部 `Allocated = 0 B`；唯一系统性分配点是**经 `ISafeEventPublisher<TConstraint>` 接口发布值类型事件**：24 B/次装箱，1,138.0 ns vs 直调 217.2 ns（无监听器，ListenerCount=1），与源码注释的“接口发布会装箱”一致。
2. **增长与扩容路径是分配和碎片的主要来源。** `SparseSet` 从默认容量逐个 `Add` 10,000 个元素分配 295,032 B（≈29.5 B/元素，含几何扩容），预扩容后 0 B 且耗时 48.7 μs → 37.9 μs（-22.3%）；探针显示 `sparse-set-growth` 的碎片率 32.64%，而 `sparse-set-reserved` 只有 0.039%，说明扩容留下的 LOH 空洞是碎片主因。
3. **本次提交（`791746c6`，`SafeEvent.Publish` 无空洞分支）的绝对表现良好但幅度不可量化。** 100 监听器发布 1,715.0 ns、0 B（较无监听器多约 1.45 μs，≈15 ns/监听器）；预分配后 Add+Remove 0 B，默认容量 100 个监听器分配 2,160 B。由于没有优化前的成功运行，本次只能作为基线，优化幅度需对父提交做 A/B（见 §7）。

## 4. 逐项分析

### `SafeEventBenchmarks`

- 分配：发布路径（有/无监听器、100 以内）全部 0 B。`AddAndRemoveListeners` 预分配 0 B；`_DefaultCapacity` 随监听器数量分配 56 / 296 / 2,160 B（`List` 扩容，≈21.6 B/监听器）。`PublishEvent_ListenerThrows` 固定 320 B，来自监听器每次 `new InvalidOperationException` 及异常记录。
- 时间：发布 1 / 10 / 100 个监听器为 303.0 / 658.3 / 1,715.0 ns；无监听器基线 150.5 / 110.2 / 267.3 ns；差值折算 ≈15 ns/监听器（100 档）与 ≈152 ns（1 档，含固定发布开销）。异常路径 7.2–8.2 μs，由异常构造/栈展开主导，与监听器数无关。
- 解读：发布循环在无空洞时不再做 2N 次写入，`listeners[i]!(@event)` + `try/catch` 的每监听器成本约 15 ns；`ListenerCount == listeners.Count` 的常态分支没有额外分配。Add/Remove 的成本主要是 `List` 增长与线性查找，预热容量后归零。
- 异常项：无 OOM/被跳过的行；小监听器档（1/10）的 Mean 与 Median 受计时噪声影响，结论以 100 档为主。

### `SafeEventRecursiveBenchmarks`

- 分配：3 个用例全部 0 B（递归队列与延迟增删队列在 setup 中预热）。
- 时间：`PublishEvent_RecursiveQueue` 770.4 ns、`PublishEvent_DeferredListenerChanges` 1,412.0 ns、`PublishEvent_RecursiveChain` 1,153.8 ns（深度 8）。
- 解读：队列排空复用 `Queue` 槽位，稳态无分配；深度 8 的链式发布 1.15 μs，说明每次排队/出队约 0.1 μs 量级。异常项：无。

### `EventBusBenchmarks`

- 分配：发布路径 4 档全部 0 B。`AddListeners` / `AddListenerOnly` / `RemoveListeners` 在 1/10/100/1000 监听器下分配 328 / 568 / 2,432 / 16,840 B；`AddListeners_WithWarmup` 与 `RemoveListenerOnly` 为 0 B。328 B 是首次创建该事件类型的 `SafeEvent` + 内部存储 + 字典条目的固定成本，其余是委托数组扩容。
- 时间：发布 1,698.9 / 1,647.8 / 2,935.4 / 17,645.7 ns；无监听器 798–969 ns；派生事件 2,017–17,372 ns；混合（基类+派生各一次）1,988–42,836 ns。加监听器 1/10/100/1000 为 3.7 / 5.3 / 18.5 / 115.5 μs；仅移除为 1.3 / 3.0 / 9.5 / 73.1 μs。
- 解读：`WarmupListenerSlot` 只消除分配（16,840 B → 0），**不降低 CPU**（115.5 vs 116.0 μs），因为它预留容量但不改变插入逻辑；首次 328 B 是类型存储的固有成本。发布成本随监听器线性增长，1000 档 ≈16.8 ns/监听器（推断，按与无监听器基线之差计算）。
- 异常项：无监听器基线 798–969 ns 明显高于 `ValueEventBus` 的 134–227 ns，且与 `EventBusComparison` 中同路径的 6.6 ns 矛盾（见下），建议复跑确认。

### `ValueEventBusBenchmarks`

- 分配：发布/子发布全部 0 B；Add/Remove 与 `EventBus` 相同（328 / 568 / 2,432 / 16,840 B），warmup 后 0 B。
- 时间：`PublishEvent_NoListeners` 208.5 / 134.1 / 217.3 / 227.1 ns；`PublishEvent_WithListeners` 845.9 / 1,061.1 / 2,181.6 / 17,141.2 ns；`ViaSubPublisher` 462.9 / 572.0 / 1,787.8 / 15,666.3 ns。
- 解读：值类型事件在泛型 `Publish<TEvent>` 路径上不装箱；`SubPublisher` 省掉每次发布的 `typeof(TEvent)` 字典查找，1000 档约 -1.5 μs（-8.6%），1 档约 -45%。发布 1000 监听器 ≈16.9 ns/监听器（推断）。

### `ValueEventBusInterfaceBenchmarks`

- 分配：24 行里 18 行非零。接口发布每次 24 B（值类型事件装箱为 `TConstraint`）；`InterfaceConversionOverhead` 480 B（10 次 `publisher` 装箱 + 10 次事件装箱），`DictionaryLookupWithTuple` 120 B（5 次事件装箱）。
- 时间：接口发布无监听器 1,138.0 / 1,236.3 / 1,217.2 ns，直调 217.2 / 153.8 / 261.6 ns；有监听器接口 1,973.4 / 2,187.4 / 3,908.2 ns，直调 1,138.3 / 1,401.0 / 2,247.4 ns。
- 解读：接口路径约慢 5–8 倍，主要来自装箱与接口/委托分发；这是 `ISafeEventPublisher<TConstraint>` 的设计取舍，源码注释已声明。需要高频发布值类型事件时应使用 `SubPublisher<TEvent>` 或直接调用 `Publisher`。

### `EventBusTypeChainBenchmarks`

- 分配：3 行均 24 B，来自基准内部 `new InterfaceEvent()` / `new TestEvent { ... }`（引用类型事件），发布路径本身 0 B。
- 时间：`PublishEvent_InterfaceBase` 2.353 μs、`PublishEvent_ObjectBase` 2.168 μs、`PublishEvent_SharedTypeChainCache` 2.727 μs（两次发布）。
- 解读：10 个监听器下，接口基类型与 class 基类型的发布成本接近（2.35 vs 2.17 μs）；共享 TypeChain 缓存两次发布 2.73 μs，未出现额外分配。异常项：无。

### `EventBusWarmupComparisonBenchmarks`

- 分配：独立缓存的首次发布 744 B、共享且已预热的缓存 480 B；Gen0 分别为 0.0710 / 0.0459 per 1000 ops。
- 时间：95.63 ns vs 63.10 ns（-34%）。
- 解读：差异包含 `new EventBus` + `AddListener` + `new DerivedTestEvent`，其中 TypeChain 构建约 264 B / 32.5 ns（推断）。该基准测的是“首次发布 + 建总线”，不是热路径发布；结论是**共享预热缓存能减少首次发布分配与时间**。

### `EventBusClearTypeChainCacheBenchmarks`

- 分配：两行均 32 B（`new DerivedTestEvent()`），清空缓存本身不分配。
- 时间：`PublishWithPrewarmedCache` 1.850 μs vs `PublishAfterClearTypeChainCache` 3.870 μs（+2.02 μs，+109%）。
- 解读：重建 TypeChain 约 2 μs CPU、0 B（该类型的 `TypesToPublish` 为空数组）；清缓存不会造成分配，但会让下一次发布慢一倍。热路径上不应清缓存。

### `EventBusComparisonBenchmarks`

- 分配：`AddListeners_Comparison` 656 / 1,136 / 4,864 / 33,680 B（= EventBus + ValueEventBus 两条总线之和）；`PublishEvent_*` 固定 24 B（基准内 `new TestEvent`）。
- 时间：`AddListeners_Comparison` 3.3 / 6.0 / 16.1 / 100.5 μs；`PublishEvent_Comparison` 2.67 / 3.37 / 9.65 / 20.22 μs；`PublishEvent_NoListeners_Comparison` 6.6–7.5 ns。
- 解读：**该类内部与跨类存在明显不一致**：无监听器发布 6.6 ns 远低于同类单总线基准的 798–969 ns / 134–227 ns；1000 监听器双总线发布 20.2 μs 也低于两个单总线之和（17.6 + 17.1 = 34.7 μs）。因此本类只用于看分配量（与单总线基准一致），**时间列不用于结论**，需单独复跑定位（见 §7）。

### `ListenerRemovalOrderBenchmarks`

- 分配：16 行全部 0 B。
- 时间：1000 监听器正序/逆序，`EventBus` 62.95 / 61.60 μs，`ValueEventBus` 61.22 / 60.37 μs；1/10/100 档差异在噪声内（<10%）。
- 解读：`SafeEvent.RemoveListener` 的双向扫描对正序/逆序删除都不敏感（1000 个约 60–63 ns/个）；无需按删除顺序优化调用方。

### `SparseSetBenchmarks`

- 分配：116 行里 96 行 0 B。非零集中在 `Add`（480 / 4,584 / 36,912 / 295,032 B，对应 10/100/1000/10000 个元素）、`AddRange_ArraySegment_WithGrowth`（360 / 2,520 / 24,120 / 240,120 B）、`Constructor_WithCollection`（304 / 1,384 / 12,184 / 120,184 B）与 `AddRange_IEnumerable_Lazy`（固定 48 B）。
- 时间：`Add` 1.77 / 3.44 / 20.19 / 48.72 μs，`Add_WithCapacity` 0.53 / 1.78 / 16.14 / 37.86 μs；`AddRange_ArraySegment` 0.78 / 2.35 / 14.11 / 28.52 μs；`AddRange_IEnumerable_Lazy` 2.32 / 6.76 / 44.26 / 72.11 μs。
- 解读：默认扩容的分配≈29.5 B/元素（10,000 档），时间比预扩容多 28.7%（10,000 档）；批量数组路径比逐个 Add 快约 41%（28.5 vs 48.7 μs）且零分配。`AddRange_IEnumerable_Lazy` 固定 48 B 与 N 无关，来源待定位（推断为惰性枚举器，不是集合本身）；其 10,000 档比数组路径慢 2.5 倍。
- 索引/枚举：`UnorderedIteration` 13.2 μs（1.32 ns/元素）vs `OrderedIteration` 30.1 μs（3.01 ns/元素）；`Contains_Last` 16.2 μs（线性扫描 1.6 ns/元素）；`RemoveAt_*`、`Indexer_*`、`Clear`、`CopyTo*` 均 0 B。异常项：`Constructor_WithCollection` 10,000 档 Mean 44.6 μs < Median 49.3 μs，存在左偏/离群，看 Median 更稳。

### `RingBufferBenchmarks`

- 分配：42 行里 39 行 0 B；`ToArray` 在 16/256/4096 容量分别为 88 / 1,048 / 16,408 B（= 4 × 容量 + 24 B 数组头），符合预期。
- 时间（4096）：`Add_Fill` 13.80 μs、`Add_Overwrite` 14.95 μs、`AddRange_ArraySegment` 8.21 μs、`Indexer_Sequential` 10.75 μs、`Enumerate` 9.28 μs、`CopyTo` 1.15 μs、`TryRemoveOldest_All` 9.54 μs；`Deque` 参照：`Add` 14.48 μs、索引 12.42 μs、`TryRemoveFront` 20.81 μs。
- 解读：覆盖写与追加写成本相当（13.8 vs 14.9 μs）；`AddRange` 因整段复制快 40%；`CopyTo` 是 `Array.Copy` 量级；移除最旧元素比 `Deque.TryRemoveFront` 快一倍（9.5 vs 20.8 μs），因为 `RingBuffer` 只移动计数、不搬元素。`ToArray` 4096 档 Mean 4.38 μs < Median 5.9 μs，分布左偏，分配量不受影响。

### `ReadOnlyListSegmentBenchmarks`

- 分配：12 行 0 B；`ManualSliceToArray` 与 `SegmentToArray` 固定 2,072 B/次（512 个 int + 24 B 头），Gen0=0.1981 per 1000 ops。
- 时间（每次遍历 1024 个元素）：数组 311.2 ns、`List` 445.8 ns、`ArraySegment` 454.9 ns、`SegmentOverArray` 576.1 ns、`SegmentOverList` 640.7 ns；`foreach`：`List` 406.0 ns、`Segment` 456.6 ns；复制 27.1 / 27.7 / 29.7 ns；切片 `ArraySegment` 0.78 ns vs `Segment` 1.71 ns。
- 解读：`ReadOnlyListSegment` 比直接数组慢约 85%（索引）和 12%（`foreach`），但仍是零分配、比 `List` 索引慢 44%；用于避免拷贝时是合理取舍。切片两条路径都在纳秒级且 0 B。

### `CacheBenchmarks`

- 分配：6 行全部 0 B。
- 时间：LRU `Access` 2.175 ns、`GetCached` 2.828 ns、`Peek` 1.833 ns；LFU 20.923 / 21.848 / 1.924 ns。
- 解读：LRU 命中只改链表的两个指针；LFU 需要维护频率桶，慢约 9.6 倍（20.9 vs 2.2 ns）；`Peek` 不改状态，两条算法都 ≈1.9 ns。无 GC 发生。

### `CacheEvictionBenchmarks`

- 分配：4 行均 64 B、Gen0=0.0061；来自基准工厂 `key => "v" + key` 每次重插新建字符串（缓存内部节点复用不分配）。
- 时间：FIFO 38.10 ns、LRU 38.64 ns、LFU 40.92 ns、`NeverEvict` 22.50 ns。
- 解读：淘汰顺序结构约 16 ns 开销；四种算法差异 <8%。分配不是缓存产生的，评价时不要把 64 B 记到淘汰实现上。

### `StringSegmentBenchmarks`

- 分配：14 行里 9 行 0 B。`StringSplit` / `StringSplit0` 3,376 B、`SegmentSplit0` 1,048 B、`StringSubstring` / `StringTrim` 88 B；Gen0 只出现在这些行（0.3229 / 0.1001 / 0.0084）。
- 时间：`Tokenize` 360.6 ns（0 B）vs `StringSplit` 466.9 ns（3,376 B）、`StringSplit0` 468.4 ns（3,376 B）、`SegmentSplit0` 1,208.8 ns（1,048 B）；`SegmentSlice` 3.05 ns（0 B）vs `StringSubstring` 5.75 ns（88 B）；`SegmentTrim` 5.84 ns（0 B）vs `StringTrim` 9.88 ns（88 B）；`SegmentIndexOfString` 216.4 ns（0 B）vs `StringIndexOfString` 15.2 ns（0 B）；`SegmentEquals` 1.648 ns vs `StringEquals` 0.425 ns；`SegmentStartsWith` 2.654 ns vs `StringStartsWith` 1.719 ns。
- 解读：零分配目标达成；`SegmentSplit0` 用 1 KB 换 2.6 倍时间（相对 `Split`），是否值得取决于场景。**唯一显著落后的是 `IndexOf`：216 ns vs 15 ns（14 倍）**，且已有 `Span.IndexOf(char)` 跳点 + `CompareOrdinal` 校验，仍未利用 `string.IndexOf(string, …)` 的向量化搜索。
- 异常项：无。

## 5. 代码字面分析与改进建议

| # | 位置 | 现状 | 建议 | 预期（指标与幅度） | 风险 |
| --- | --- | --- | --- | --- | --- |
| 1 | `JiksLib/StringSegment.cs:1105`（`IndexOfOrdinal`） | 先用 `IndexOfChar` 找首字符，再逐个候选 `CompareOrdinal`；实测 216.4 ns vs `string.IndexOf` 15.2 ns | 底层是连续 `string` 时直接调用 `x.IndexOf(y, searchStart, searchEnd - searchStart + 1, StringComparison.Ordinal)`，再减去 `Offset`；非连续/空片段走现有回退 | 时间 216 ns → 约 20–30 ns（推断，量级参考 `string.IndexOf` 的 15.2 ns）；分配保持 0 B | `startIndex`/空 needle/负 count 的边界语义、`net48` 重载存在性需确认；`Ordinal` 与逐字符校验等价，不涉及 `IgnoreCase` 版本 |
| 2 | `JiksLib/Control/ValueEventBus.cs:319`（`ISafeEventPublisher.Publish`） | 接口发布把值类型事件装箱：24 B/次；无监听器 1,138.0 ns vs 直调 217.2 ns（ListenerCount=1） | 提供 `SubPublisher<TEvent>` 的显式入口文档/示例，或在接口路径增加不装箱的 `TryGetSubPublisher` 适配；不建议改 `ISafeEventPublisher` 本身 | 24 B → 0 B；时间约 -0.9 μs（推断） | 新增公开 API；接口本身无法泛型化，泛型重载需另立类型 |
| 3 | `JiksLib/Control/EventBus.cs:239`（`WarmupListenerSlot`） | 1000 监听器未预热分配 16,840 B，预热后 0 B，但 CPU 不变（115.5 vs 116.0 μs） | 在文档与示例中把 `WarmupListenerSlot` 定位为“只消除分配、不加速插入”；如已知规模，可在批量注册 API 内部先预留容量 | 分配 16,840 B → 0 B；CPU 无显著变化（实测） | 需要调用方知道监听器上限；`WarmupListenerSlot` 与 `autoPurgeListenerSlot` 互斥 |
| 4 | `JiksLib/Collections/SparseSet.cs:156`（`Add`） | 默认容量 10,000 元素分配 295,032 B、48.7 μs；预扩容 0 B、37.9 μs | 保持现有 `EnsureCapacity`/构造函数 API；文档中给出“已知规模先 `new SparseSet<T>(n)`”的用法，批量输入用 `AddRange` 数组/片段重载（0 B、快 41%） | 分配 -100%；CPU -22.3%（实测） | 提前占用内存，容量估计过大增加峰值 |
| 5 | `JiksLib/Collections/SparseSet.cs:190`（`AddRange(IEnumerable)`） | 惰性序列路径固定 48 B/次（与 N 无关），10,000 档 72.1 μs vs 数组路径 28.5 μs | 先定位 48 B 来源（枚举器装箱 / `EnsureAdditionalCapacity`）；若为枚举器，增加结构体枚举器快路径；数组/`ICollection` 已知计数时走预分配分支 | 48 B → 0 B（推断）；时间可能小幅下降 | 需要确认分配点，不能仅凭推断改代码；泛型枚举器分发有复杂度 |
| 6 | `JiksLib/Control/SafeEvent.cs:349`（无空洞分支） | 本次优化后每监听器 ≈15 ns、发布 0 B；仍保留每监听器 `try/catch` | 不建议把 `try/catch` 移出循环：会改变“单监听器异常不中断其余监听器”的契约 | 无（维持现状） | 语义风险高，收益不明 |

补充观察（不属于上表）：`EventBus` 无监听器发布 798–969 ns 与 `ValueEventBus` 134–227 ns 的差距（约 3.7 倍）在同机同 job 下稳定出现，但 `EventBusComparison` 的同路径只有 6.6 ns，两者不可能同时成立；这更像基准/测量问题（调用点、JIT 内联、计时分辨率）而不是真实差异，建议按 §7 复跑定位后再决定是否优化 `TypeChain` 查找。

## 6. 测量可信度与缺口

| 指标 | 状态 | 说明 |
| --- | --- | --- |
| 分配大小 / 次数 | 已测（部分） | `Allocated` 来自 BDN，341 行全部有值；`Gen0` 只在发生 GC 的 5 个类中出现（`CacheEviction`、`ReadOnlyListSegment`、`EventBusComparison`、`EventBusWarmupComparison`、`StringSegment`），其余类没有 Gen 列 = 本次未触发 GC，不代表未测量。分配次数仍只能由源码分配点推断，BDN 不提供次数。 |
| 碎片化 | 已测（探针） | 3 场景 × 3 进程，`probe/memory.jsonl`；碎片率是进程级指标，只可与同探针后续运行比较，本 run 为首次基线。 |
| CPU 时间 | 已测 | 单线程 BDN 计时；各基准的 `InvocationCount` 由 BDN 自动选择（部分类显式 1），跨类时间仅作参考；`EventBusComparison` 的时间列有内部矛盾，已在 §4 标注。 |
| 总体内存占用 | 已测（探针） | 保留内存与峰值工作集来自 `probe`：`sparse-set-reserved` 1,200,160 B / 27.73 MB、`sparse-set-growth` 2,359,432 B / 29.75 MB、`eventbus-listeners` 132,040 B / 27.84 MB（3 次取中位数，保留/分配逐字节一致）。 |
| 环境一致性 | 部分 | BDN 报告 `Cannot setup power plan`，未能切换到高性能电源计划；所有行同一环境，绝对值可能偏保守，分配量不受影响。 |
| 工作区状态 | 不干净 | 运行开始时 `git status` 为空；运行中 `README.md`（19:03）与 `JiksLib/JiksLib.csproj`（19:11，版本 1.0.0→2.0.0）被外部修改。被测源码与基准工程未变，且 BDN 的生成工程构建发生在 18:55，早于两处修改，因此测量值仍对应 `791746c6` 的代码；`run.json` 记为 `dirty: true`。 |

## 7. 建议的下一步

1. **量化 `791746c6` 的优化幅度**：在父提交 `5bab883` 上以相同 `TMP`/`TEMP` 与 `--filter '*SafeEvent*'` 跑一次（新建 run 目录，不覆盖本次），与本次同 filter 的 18 行逐项比较 `Allocated` 与 Mean/Median；这是唯一能把“SafeEvent 优化”从绝对值变成前后对比的方式。
2. **复跑 `EventBusComparisonBenchmarks`**：与 `EventBusBenchmarks`、`ValueEventBusBenchmarks` 放在同一次运行里，确认无监听器发布 6.6 ns 与 798–969 ns 的矛盾；在此之前不要引用该类的任何时间结论。
3. **定位 `SparseSet.AddRange(IEnumerable)` 的固定 48 B**：用最小复现（`IEnumerable<int>` 惰性序列、`ICollection<int>`、数组三条路径）确认分配点，再决定是否加枚举器快路径。
4. **补一个 `StringSegment.IndexOf` 的候选实现**（建议 #1）后，用同一基准验证 216 ns 是否降到 20–30 ns 且分配仍为 0 B。
5. 若后续要跟踪碎片，保持 `probe` 实现与宿主不变，并与本 run 的 `probe/memory.jsonl` 做同场景对比；探针版本变化后旧碎片率只能作历史参考。

## 8. 附录：全部 341 行（来自 `raw/results/*.csv`）

> 生成方式：直接读取 BDN 的 CSV 报告，未改写任何数值；`Gen0` 列存在时原样保留。参数列按各类实际 `[Params]` 输出。

### `Collections.CacheBenchmarks`（6 行）

| Method | Algorithm | Mean | Median | Allocated |
| --- | --- | --- | --- | --- |
| AccessHit | LRU | 2.175 ns | 2.174 ns | 0 B |
| GetCachedDefault | LRU | 2.828 ns | 2.744 ns | 0 B |
| GetCachedPeek | LRU | 1.833 ns | 1.830 ns | 0 B |
| AccessHit | LFU | 20.923 ns | 20.859 ns | 0 B |
| GetCachedDefault | LFU | 21.848 ns | 21.855 ns | 0 B |
| GetCachedPeek | LFU | 1.924 ns | 1.925 ns | 0 B |

### `Collections.CacheEvictionBenchmarks`（4 行）

| Method | Algorithm | Mean | Median | Allocated | Gen0 |
| --- | --- | --- | --- | --- | --- |
| EvictAndReinsert | FIFO | 38.10 ns | 38.41 ns | 64 B | 0.0061 |
| EvictAndReinsert | LRU | 38.64 ns | 38.63 ns | 64 B | 0.0061 |
| EvictAndReinsert | LFU | 40.92 ns | 40.76 ns | 64 B | 0.0061 |
| EvictAndReinsert | NeverEvict | 22.50 ns | 22.47 ns | 64 B | 0.0061 |

### `Collections.ReadOnlyListSegmentBenchmarks`（14 行）

| Method | Mean | Median | Allocated | Gen0 |
| --- | --- | --- | --- | --- |
| ArrayIndexer | 311.1866 ns | 310.3095 ns | 0 B | 0.0000 |
| ListIndexer | 445.7505 ns | 445.0334 ns | 0 B | 0.0000 |
| ArraySegmentIndexer | 454.8964 ns | 453.7103 ns | 0 B | 0.0000 |
| SegmentOverArrayIndexer | 576.1294 ns | 575.3857 ns | 0 B | 0.0000 |
| SegmentOverListIndexer | 640.7229 ns | 639.3224 ns | 0 B | 0.0000 |
| ListForeach | 405.9514 ns | 405.9343 ns | 0 B | 0.0000 |
| SegmentForeach | 456.6004 ns | 457.2708 ns | 0 B | 0.0000 |
| ArrayCopy | 27.0651 ns | 26.7584 ns | 0 B | 0.0000 |
| ListCopyTo | 27.7427 ns | 27.6902 ns | 0 B | 0.0000 |
| SegmentCopyTo | 29.6964 ns | 29.7976 ns | 0 B | 0.0000 |
| ManualSliceToArray | 103.3933 ns | 101.8090 ns | 2072 B | 0.1981 |
| SegmentToArray | 99.1249 ns | 98.9889 ns | 2072 B | 0.1981 |
| ArraySegmentSlice | 0.7846 ns | 0.7839 ns | 0 B | 0.0000 |
| SegmentSlice | 1.7114 ns | 1.7144 ns | 0 B | 0.0000 |

### `Collections.RingBufferBenchmarks`（42 行）

| Method | Capacity | Mean | Median | Allocated |
| --- | --- | --- | --- | --- |
| Add_Fill | 16 | 439.1 ns | 400.0 ns | 0 B |
| Add_Overwrite | 16 | 385.2 ns | 350.0 ns | 0 B |
| Add_WithEvicted_Overwrite | 16 | 428.0 ns | 400.0 ns | 0 B |
| AddRange_ArraySegment | 16 | 308.2 ns | 300.0 ns | 0 B |
| Indexer_Sequential | 16 | 234.4 ns | 250.0 ns | 0 B |
| Enumerate | 16 | 260.0 ns | 300.0 ns | 0 B |
| CopyTo | 16 | 344.0 ns | 300.0 ns | 0 B |
| ToArray | 16 | 650.0 ns | 700.0 ns | 88 B |
| TryRemoveNewest_All | 16 | 172.0 ns | 200.0 ns | 0 B |
| TryRemoveOldest_All | 16 | 146.2 ns | 100.0 ns | 0 B |
| Clear | 16 | 236.6 ns | 200.0 ns | 0 B |
| Deque_Add_Fill | 16 | 467.7 ns | 500.0 ns | 0 B |
| Deque_Indexer_Sequential | 16 | 273.6 ns | 300.0 ns | 0 B |
| Deque_TryRemoveFront_All | 16 | 372.6 ns | 400.0 ns | 0 B |
| Add_Fill | 256 | 3,022.0 ns | 2,900.0 ns | 0 B |
| Add_Overwrite | 256 | 3,129.0 ns | 3,000.0 ns | 0 B |
| Add_WithEvicted_Overwrite | 256 | 3,185.6 ns | 3,200.0 ns | 0 B |
| AddRange_ArraySegment | 256 | 2,317.2 ns | 2,200.0 ns | 0 B |
| Indexer_Sequential | 256 | 2,631.3 ns | 2,600.0 ns | 0 B |
| Enumerate | 256 | 2,420.0 ns | 2,300.0 ns | 0 B |
| CopyTo | 256 | 514.7 ns | 500.0 ns | 0 B |
| ToArray | 256 | 771.9 ns | 800.0 ns | 1048 B |
| TryRemoveNewest_All | 256 | 2,490.9 ns | 2,300.0 ns | 0 B |
| TryRemoveOldest_All | 256 | 2,369.0 ns | 2,250.0 ns | 0 B |
| Clear | 256 | 213.2 ns | 200.0 ns | 0 B |
| Deque_Add_Fill | 256 | 2,971.7 ns | 2,900.0 ns | 0 B |
| Deque_Indexer_Sequential | 256 | 2,511.2 ns | 2,350.0 ns | 0 B |
| Deque_TryRemoveFront_All | 256 | 2,723.2 ns | 2,600.0 ns | 0 B |
| Add_Fill | 4096 | 13,802.1 ns | 13,400.0 ns | 0 B |
| Add_Overwrite | 4096 | 14,947.8 ns | 14,650.0 ns | 0 B |
| Add_WithEvicted_Overwrite | 4096 | 15,261.7 ns | 14,800.0 ns | 0 B |
| AddRange_ArraySegment | 4096 | 8,214.6 ns | 7,700.0 ns | 0 B |
| Indexer_Sequential | 4096 | 10,749.5 ns | 10,500.0 ns | 0 B |
| Enumerate | 4096 | 9,282.3 ns | 8,950.0 ns | 0 B |
| CopyTo | 4096 | 1,149.5 ns | 1,100.0 ns | 0 B |
| ToArray | 4096 | 4,377.1 ns | 5,900.0 ns | 16408 B |
| TryRemoveNewest_All | 4096 | 9,903.3 ns | 9,600.0 ns | 0 B |
| TryRemoveOldest_All | 4096 | 9,537.4 ns | 9,400.0 ns | 0 B |
| Clear | 4096 | 741.9 ns | 800.0 ns | 0 B |
| Deque_Add_Fill | 4096 | 14,482.7 ns | 14,500.0 ns | 0 B |
| Deque_Indexer_Sequential | 4096 | 12,424.7 ns | 12,100.0 ns | 0 B |
| Deque_TryRemoveFront_All | 4096 | 20,806.8 ns | 20,500.0 ns | 0 B |

### `Collections.SparseSetBenchmarks`（116 行）

| Method | ElementCount | Mean | Median | Allocated |
| --- | --- | --- | --- | --- |
| Add | 10 | 1,772.8 ns | 1,750.0 ns | 480 B |
| Add_WithCapacity | 10 | 532.2 ns | 500.0 ns | 0 B |
| AddRange_ArraySegment | 10 | 782.6 ns | 800.0 ns | 0 B |
| AddRange_ArraySegment_WithGrowth | 10 | 1,938.2 ns | 2,000.0 ns | 360 B |
| AddRange_Array | 10 | 822.5 ns | 800.0 ns | 0 B |
| AddRange_IEnumerable | 10 | 1,870.1 ns | 1,800.0 ns | 32 B |
| AddRange_IEnumerable_Lazy | 10 | 2,317.6 ns | 2,300.0 ns | 48 B |
| RemoveAt_First | 10 | 263.7 ns | 300.0 ns | 0 B |
| RemoveAt_Last | 10 | 291.7 ns | 300.0 ns | 0 B |
| RemoveAt_Middle | 10 | 325.8 ns | 300.0 ns | 0 B |
| Remove_ByValue | 10 | 718.8 ns | 700.0 ns | 0 B |
| Contains_First | 10 | 347.3 ns | 300.0 ns | 0 B |
| Contains_Last | 10 | 517.9 ns | 500.0 ns | 0 B |
| Contains_NonExisting | 10 | 418.0 ns | 400.0 ns | 0 B |
| IndexOf_First | 10 | 359.2 ns | 400.0 ns | 0 B |
| IndexOf_Last | 10 | 521.9 ns | 500.0 ns | 0 B |
| IndexOf_NonExisting | 10 | 429.0 ns | 400.0 ns | 0 B |
| TryIndex | 10 | 190.5 ns | 150.0 ns | 0 B |
| Indexer_Get | 10 | 292.9 ns | 300.0 ns | 0 B |
| Indexer_Set | 10 | 225.5 ns | 250.0 ns | 0 B |
| Clear | 10 | 257.4 ns | 300.0 ns | 0 B |
| UnorderedIteration | 10 | 348.9 ns | 350.0 ns | 0 B |
| OrderedIteration | 10 | 553.3 ns | 500.0 ns | 0 B |
| CopyToOrdered | 10 | 434.0 ns | 450.0 ns | 0 B |
| CopyToUnordered | 10 | 296.9 ns | 300.0 ns | 0 B |
| InnerBufferIndexToSparseSetIndex | 10 | 212.1 ns | 200.0 ns | 0 B |
| SparseSetIndexToInnerBufferIndex | 10 | 232.0 ns | 200.0 ns | 0 B |
| Constructor_WithCollection | 10 | 2,141.5 ns | 2,000.0 ns | 304 B |
| Stress_AddRemove | 10 | 1,279.0 ns | 1,300.0 ns | 0 B |
| Add | 100 | 3,435.0 ns | 3,450.0 ns | 4584 B |
| Add_WithCapacity | 100 | 1,780.0 ns | 1,700.0 ns | 0 B |
| AddRange_ArraySegment | 100 | 2,352.1 ns | 2,200.0 ns | 0 B |
| AddRange_ArraySegment_WithGrowth | 100 | 3,299.0 ns | 3,100.0 ns | 2520 B |
| AddRange_Array | 100 | 2,153.6 ns | 2,000.0 ns | 0 B |
| AddRange_IEnumerable | 100 | 4,491.4 ns | 4,500.0 ns | 32 B |
| AddRange_IEnumerable_Lazy | 100 | 6,763.3 ns | 6,750.0 ns | 48 B |
| RemoveAt_First | 100 | 287.5 ns | 300.0 ns | 0 B |
| RemoveAt_Last | 100 | 296.9 ns | 350.0 ns | 0 B |
| RemoveAt_Middle | 100 | 354.3 ns | 300.0 ns | 0 B |
| Remove_ByValue | 100 | 1,117.6 ns | 1,100.0 ns | 0 B |
| Contains_First | 100 | 236.3 ns | 200.0 ns | 0 B |
| Contains_Last | 100 | 1,374.5 ns | 1,300.0 ns | 0 B |
| Contains_NonExisting | 100 | 1,434.3 ns | 1,300.0 ns | 0 B |
| IndexOf_First | 100 | 331.8 ns | 300.0 ns | 0 B |
| IndexOf_Last | 100 | 1,481.0 ns | 1,350.0 ns | 0 B |
| IndexOf_NonExisting | 100 | 1,404.0 ns | 1,300.0 ns | 0 B |
| TryIndex | 100 | 216.7 ns | 200.0 ns | 0 B |
| Indexer_Get | 100 | 277.9 ns | 300.0 ns | 0 B |
| Indexer_Set | 100 | 176.3 ns | 200.0 ns | 0 B |
| Clear | 100 | 299.0 ns | 250.0 ns | 0 B |
| UnorderedIteration | 100 | 1,214.4 ns | 1,200.0 ns | 0 B |
| OrderedIteration | 100 | 2,104.8 ns | 2,100.0 ns | 0 B |
| CopyToOrdered | 100 | 1,447.0 ns | 1,400.0 ns | 0 B |
| CopyToUnordered | 100 | 424.7 ns | 400.0 ns | 0 B |
| InnerBufferIndexToSparseSetIndex | 100 | 230.0 ns | 200.0 ns | 0 B |
| SparseSetIndexToInnerBufferIndex | 100 | 226.9 ns | 250.0 ns | 0 B |
| Constructor_WithCollection | 100 | 2,983.3 ns | 2,900.0 ns | 1384 B |
| Stress_AddRemove | 100 | 4,097.0 ns | 3,800.0 ns | 0 B |
| Add | 1000 | 20,188.8 ns | 19,600.0 ns | 36912 B |
| Add_WithCapacity | 1000 | 16,144.1 ns | 16,100.0 ns | 0 B |
| AddRange_ArraySegment | 1000 | 14,110.8 ns | 13,600.0 ns | 0 B |
| AddRange_ArraySegment_WithGrowth | 1000 | 15,785.5 ns | 15,050.0 ns | 24120 B |
| AddRange_Array | 1000 | 14,642.4 ns | 14,250.0 ns | 0 B |
| AddRange_IEnumerable | 1000 | 29,965.6 ns | 29,950.0 ns | 32 B |
| AddRange_IEnumerable_Lazy | 1000 | 44,257.8 ns | 44,400.0 ns | 48 B |
| RemoveAt_First | 1000 | 335.7 ns | 300.0 ns | 0 B |
| RemoveAt_Last | 1000 | 193.0 ns | 150.0 ns | 0 B |
| RemoveAt_Middle | 1000 | 417.3 ns | 400.0 ns | 0 B |
| Remove_ByValue | 1000 | 6,395.8 ns | 6,300.0 ns | 0 B |
| Contains_First | 1000 | 261.2 ns | 300.0 ns | 0 B |
| Contains_Last | 1000 | 12,483.3 ns | 12,350.0 ns | 0 B |
| Contains_NonExisting | 1000 | 12,976.9 ns | 12,800.0 ns | 0 B |
| IndexOf_First | 1000 | 287.5 ns | 300.0 ns | 0 B |
| IndexOf_Last | 1000 | 12,634.4 ns | 12,350.0 ns | 0 B |
| IndexOf_NonExisting | 1000 | 12,598.9 ns | 12,200.0 ns | 0 B |
| TryIndex | 1000 | 148.9 ns | 100.0 ns | 0 B |
| Indexer_Get | 1000 | 258.1 ns | 250.0 ns | 0 B |
| Indexer_Set | 1000 | 281.4 ns | 300.0 ns | 0 B |
| Clear | 1000 | 474.0 ns | 500.0 ns | 0 B |
| UnorderedIteration | 1000 | 9,474.7 ns | 9,050.0 ns | 0 B |
| OrderedIteration | 1000 | 19,478.1 ns | 18,900.0 ns | 0 B |
| CopyToOrdered | 1000 | 9,865.1 ns | 9,450.0 ns | 0 B |
| CopyToUnordered | 1000 | 809.3 ns | 800.0 ns | 0 B |
| InnerBufferIndexToSparseSetIndex | 1000 | 176.6 ns | 200.0 ns | 0 B |
| SparseSetIndexToInnerBufferIndex | 1000 | 244.9 ns | 200.0 ns | 0 B |
| Constructor_WithCollection | 1000 | 13,146.4 ns | 12,500.0 ns | 12184 B |
| Stress_AddRemove | 1000 | 17,055.3 ns | 16,650.0 ns | 0 B |
| Add | 10000 | 48,721.6 ns | 48,300.0 ns | 295032 B |
| Add_WithCapacity | 10000 | 37,856.8 ns | 37,700.0 ns | 0 B |
| AddRange_ArraySegment | 10000 | 28,520.2 ns | 28,500.0 ns | 0 B |
| AddRange_ArraySegment_WithGrowth | 10000 | 35,046.4 ns | 35,000.0 ns | 240120 B |
| AddRange_Array | 10000 | 29,940.4 ns | 29,300.0 ns | 0 B |
| AddRange_IEnumerable | 10000 | 58,357.1 ns | 58,400.0 ns | 32 B |
| AddRange_IEnumerable_Lazy | 10000 | 72,110.7 ns | 71,850.0 ns | 48 B |
| RemoveAt_First | 10000 | 269.0 ns | 300.0 ns | 0 B |
| RemoveAt_Last | 10000 | 261.0 ns | 300.0 ns | 0 B |
| RemoveAt_Middle | 10000 | 438.7 ns | 400.0 ns | 0 B |
| Remove_ByValue | 10000 | 14,481.1 ns | 14,100.0 ns | 0 B |
| Contains_First | 10000 | 274.0 ns | 300.0 ns | 0 B |
| Contains_Last | 10000 | 16,234.7 ns | 16,100.0 ns | 0 B |
| Contains_NonExisting | 10000 | 15,313.3 ns | 15,050.0 ns | 0 B |
| IndexOf_First | 10000 | 339.1 ns | 300.0 ns | 0 B |
| IndexOf_Last | 10000 | 16,121.9 ns | 15,950.0 ns | 0 B |
| IndexOf_NonExisting | 10000 | 15,989.5 ns | 15,900.0 ns | 0 B |
| TryIndex | 10000 | 276.6 ns | 300.0 ns | 0 B |
| Indexer_Get | 10000 | 337.0 ns | 300.0 ns | 0 B |
| Indexer_Set | 10000 | 275.5 ns | 300.0 ns | 0 B |
| Clear | 10000 | 1,557.4 ns | 1,550.0 ns | 0 B |
| UnorderedIteration | 10000 | 13,210.0 ns | 13,200.0 ns | 0 B |
| OrderedIteration | 10000 | 30,112.6 ns | 29,700.0 ns | 0 B |
| CopyToOrdered | 10000 | 17,313.5 ns | 17,100.0 ns | 0 B |
| CopyToUnordered | 10000 | 1,796.9 ns | 1,850.0 ns | 0 B |
| InnerBufferIndexToSparseSetIndex | 10000 | 214.3 ns | 200.0 ns | 0 B |
| SparseSetIndexToInnerBufferIndex | 10000 | 194.8 ns | 200.0 ns | 0 B |
| Constructor_WithCollection | 10000 | 44,620.0 ns | 49,300.0 ns | 120184 B |
| Stress_AddRemove | 10000 | 58,734.8 ns | 58,600.0 ns | 0 B |

### `Control.EventBusBenchmarks`（36 行）

| Method | ListenerCount | Mean | Median | Allocated |
| --- | --- | --- | --- | --- |
| AddListeners | 1 | 3,729.0 ns | 3,600.0 ns | 328 B |
| AddListeners_WithWarmup | 1 | 1,599.0 ns | 1,600.0 ns | 0 B |
| AddListenerOnly | 1 | 2,555.6 ns | 2,500.0 ns | 328 B |
| RemoveListenerOnly | 1 | 1,334.7 ns | 1,300.0 ns | 0 B |
| RemoveListeners | 1 | 3,042.6 ns | 3,000.0 ns | 328 B |
| PublishEvent_NoListeners | 1 | 969.1 ns | 900.0 ns | 0 B |
| PublishEvent_WithListeners | 1 | 1,698.9 ns | 1,600.0 ns | 0 B |
| PublishDerivedEvent_WithBaseListeners | 1 | 2,017.2 ns | 2,000.0 ns | 0 B |
| PublishEvent_MixedListeners | 1 | 1,988.3 ns | 1,900.0 ns | 0 B |
| AddListeners | 10 | 5,259.6 ns | 5,000.0 ns | 568 B |
| AddListeners_WithWarmup | 10 | 2,919.1 ns | 2,900.0 ns | 0 B |
| AddListenerOnly | 10 | 3,255.3 ns | 3,200.0 ns | 568 B |
| RemoveListenerOnly | 10 | 2,992.7 ns | 2,900.0 ns | 0 B |
| RemoveListeners | 10 | 6,002.1 ns | 5,800.0 ns | 568 B |
| PublishEvent_NoListeners | 10 | 797.9 ns | 700.0 ns | 0 B |
| PublishEvent_WithListeners | 10 | 1,647.8 ns | 1,600.0 ns | 0 B |
| PublishDerivedEvent_WithBaseListeners | 10 | 2,086.3 ns | 1,900.0 ns | 0 B |
| PublishEvent_MixedListeners | 10 | 2,439.6 ns | 2,400.0 ns | 0 B |
| AddListeners | 100 | 18,544.0 ns | 18,800.0 ns | 2432 B |
| AddListeners_WithWarmup | 100 | 16,966.7 ns | 17,600.0 ns | 0 B |
| AddListenerOnly | 100 | 9,389.4 ns | 9,000.0 ns | 2432 B |
| RemoveListenerOnly | 100 | 9,460.1 ns | 9,350.0 ns | 0 B |
| RemoveListeners | 100 | 18,769.4 ns | 19,350.0 ns | 2432 B |
| PublishEvent_NoListeners | 100 | 838.8 ns | 800.0 ns | 0 B |
| PublishEvent_WithListeners | 100 | 2,935.4 ns | 2,900.0 ns | 0 B |
| PublishDerivedEvent_WithBaseListeners | 100 | 3,280.8 ns | 3,300.0 ns | 0 B |
| PublishEvent_MixedListeners | 100 | 6,831.2 ns | 6,900.0 ns | 0 B |
| AddListeners | 1000 | 115,492.0 ns | 114,000.0 ns | 16840 B |
| AddListeners_WithWarmup | 1000 | 116,020.2 ns | 114,150.0 ns | 0 B |
| AddListenerOnly | 1000 | 61,552.2 ns | 60,650.0 ns | 16840 B |
| RemoveListenerOnly | 1000 | 73,114.6 ns | 71,900.0 ns | 0 B |
| RemoveListeners | 1000 | 117,830.1 ns | 115,100.0 ns | 16840 B |
| PublishEvent_NoListeners | 1000 | 840.4 ns | 800.0 ns | 0 B |
| PublishEvent_WithListeners | 1000 | 17,645.7 ns | 17,500.0 ns | 0 B |
| PublishDerivedEvent_WithBaseListeners | 1000 | 17,372.2 ns | 17,000.0 ns | 0 B |
| PublishEvent_MixedListeners | 1000 | 42,835.7 ns | 42,900.0 ns | 0 B |

### `Control.EventBusClearTypeChainCacheBenchmarks`（2 行）

| Method | Mean | Median | Allocated |
| --- | --- | --- | --- |
| PublishWithPrewarmedCache | 1.850 μs | 1.800 μs | 32 B |
| PublishAfterClearTypeChainCache | 3.870 μs | 3.700 μs | 32 B |

### `Control.EventBusComparisonBenchmarks`（12 行）

| Method | ListenerCount | Mean | Median | Allocated | Gen0 |
| --- | --- | --- | --- | --- | --- |
| PublishEvent_NoListeners_Comparison | 1 | 6.590 ns | 6.519 ns | 24 B | 0.0023 |
| AddListeners_Comparison | 1 | 3,286.022 ns | 3,200.000 ns | 656 B | 0.0000 |
| PublishEvent_Comparison | 1 | 2,667.742 ns | 2,600.000 ns | 24 B | 0.0000 |
| PublishEvent_NoListeners_Comparison | 10 | 6.623 ns | 6.613 ns | 24 B | 0.0023 |
| AddListeners_Comparison | 10 | 5,973.684 ns | 5,900.000 ns | 1136 B | 0.0000 |
| PublishEvent_Comparison | 10 | 3,365.591 ns | 3,200.000 ns | 24 B | 0.0000 |
| PublishEvent_NoListeners_Comparison | 100 | 7.471 ns | 7.418 ns | 24 B | 0.0023 |
| AddListeners_Comparison | 100 | 16,063.265 ns | 15,050.000 ns | 4864 B | 0.0000 |
| PublishEvent_Comparison | 100 | 9,649.495 ns | 9,900.000 ns | 24 B | 0.0000 |
| PublishEvent_NoListeners_Comparison | 1000 | 6.653 ns | 6.636 ns | 24 B | 0.0023 |
| AddListeners_Comparison | 1000 | 100,545.055 ns | 98,400.000 ns | 33680 B | 0.0000 |
| PublishEvent_Comparison | 1000 | 20,221.649 ns | 19,300.000 ns | 24 B | 0.0000 |

### `Control.EventBusTypeChainBenchmarks`（3 行）

| Method | Mean | Median | Allocated |
| --- | --- | --- | --- |
| PublishEvent_InterfaceBase | 2.353 μs | 2.350 μs | 24 B |
| PublishEvent_ObjectBase | 2.168 μs | 2.100 μs | 24 B |
| PublishEvent_SharedTypeChainCache | 2.727 μs | 2.700 μs | 24 B |

### `Control.EventBusWarmupComparisonBenchmarks`（2 行）

| Method | Mean | Median | Allocated | Gen0 |
| --- | --- | --- | --- | --- |
| FirstPublish_IndependentCache | 95.63 ns | 94.85 ns | 744 B | 0.0710 |
| FirstPublish_SharedPrewarmedCache | 63.10 ns | 62.77 ns | 480 B | 0.0459 |

### `Control.ListenerRemovalOrderBenchmarks`（16 行）

| Method | ListenerCount | Mean | Median | Allocated |
| --- | --- | --- | --- | --- |
| RemoveForward_EventBus | 1 | 1.064 μs | 1.0000 μs | 0 B |
| RemoveReverse_EventBus | 1 | 1.133 μs | 1.1000 μs | 0 B |
| RemoveForward_ValueEventBus | 1 | 1.281 μs | 1.2500 μs | 0 B |
| RemoveReverse_ValueEventBus | 1 | 1.027 μs | 0.9500 μs | 0 B |
| RemoveForward_EventBus | 10 | 1.830 μs | 1.8000 μs | 0 B |
| RemoveReverse_EventBus | 10 | 1.737 μs | 1.8000 μs | 0 B |
| RemoveForward_ValueEventBus | 10 | 1.644 μs | 1.6000 μs | 0 B |
| RemoveReverse_ValueEventBus | 10 | 1.502 μs | 1.5000 μs | 0 B |
| RemoveForward_EventBus | 100 | 7.992 μs | 8.1500 μs | 0 B |
| RemoveReverse_EventBus | 100 | 8.212 μs | 8.2000 μs | 0 B |
| RemoveForward_ValueEventBus | 100 | 7.870 μs | 8.0000 μs | 0 B |
| RemoveReverse_ValueEventBus | 100 | 7.760 μs | 7.9500 μs | 0 B |
| RemoveForward_EventBus | 1000 | 62.949 μs | 61.3000 μs | 0 B |
| RemoveReverse_EventBus | 1000 | 61.600 μs | 61.0500 μs | 0 B |
| RemoveForward_ValueEventBus | 1000 | 61.216 μs | 60.5000 μs | 0 B |
| RemoveReverse_ValueEventBus | 1000 | 60.367 μs | 59.6500 μs | 0 B |

### `Control.SafeEventBenchmarks`（15 行）

| Method | ListenerCount | Mean | Median | Allocated |
| --- | --- | --- | --- | --- |
| PublishEvent_WithListeners | 1 | 303.0 ns | 300.0 ns | 0 B |
| PublishEvent_NoListeners | 1 | 150.5 ns | 100.0 ns | 0 B |
| AddAndRemoveListeners_DefaultCapacity | 1 | 1,445.0 ns | 1,500.0 ns | 56 B |
| AddAndRemoveListeners | 1 | 779.6 ns | 800.0 ns | 0 B |
| PublishEvent_ListenerThrows | 1 | 8,180.6 ns | 7,350.0 ns | 320 B |
| PublishEvent_WithListeners | 10 | 658.3 ns | 600.0 ns | 0 B |
| PublishEvent_NoListeners | 10 | 110.2 ns | 100.0 ns | 0 B |
| AddAndRemoveListeners_DefaultCapacity | 10 | 2,437.2 ns | 2,500.0 ns | 296 B |
| AddAndRemoveListeners | 10 | 1,425.3 ns | 1,400.0 ns | 0 B |
| PublishEvent_ListenerThrows | 10 | 7,226.3 ns | 6,700.0 ns | 320 B |
| PublishEvent_WithListeners | 100 | 1,715.0 ns | 1,600.0 ns | 0 B |
| PublishEvent_NoListeners | 100 | 267.3 ns | 300.0 ns | 0 B |
| AddAndRemoveListeners_DefaultCapacity | 100 | 6,309.1 ns | 5,900.0 ns | 2160 B |
| AddAndRemoveListeners | 100 | 4,444.4 ns | 4,500.0 ns | 0 B |
| PublishEvent_ListenerThrows | 100 | 7,862.6 ns | 7,900.0 ns | 320 B |

### `Control.SafeEventRecursiveBenchmarks`（3 行）

| Method | Mean | Median | Allocated |
| --- | --- | --- | --- |
| PublishEvent_RecursiveQueue | 770.4 ns | 700.0 ns | 0 B |
| PublishEvent_DeferredListenerChanges | 1,412.0 ns | 1,400.0 ns | 0 B |
| PublishEvent_RecursiveChain | 1,153.8 ns | 1,100.0 ns | 0 B |

### `Control.ValueEventBusBenchmarks`（32 行）

| Method | ListenerCount | Mean | Median | Allocated |
| --- | --- | --- | --- | --- |
| AddListeners | 1 | 2,546.9 ns | 2,400.0 ns | 328 B |
| AddListeners_WithWarmup | 1 | 1,362.5 ns | 1,400.0 ns | 0 B |
| AddListenerOnly | 1 | 2,358.2 ns | 2,400.0 ns | 328 B |
| RemoveListenerOnly | 1 | 940.2 ns | 1,000.0 ns | 0 B |
| RemoveListeners | 1 | 2,651.0 ns | 2,700.0 ns | 328 B |
| PublishEvent_NoListeners | 1 | 208.5 ns | 200.0 ns | 0 B |
| PublishEvent_WithListeners | 1 | 845.9 ns | 800.0 ns | 0 B |
| PublishEvent_ViaSubPublisher | 1 | 462.9 ns | 400.0 ns | 0 B |
| AddListeners | 10 | 4,741.1 ns | 4,600.0 ns | 568 B |
| AddListeners_WithWarmup | 10 | 2,725.0 ns | 2,700.0 ns | 0 B |
| AddListenerOnly | 10 | 4,104.2 ns | 3,900.0 ns | 568 B |
| RemoveListenerOnly | 10 | 1,852.7 ns | 1,800.0 ns | 0 B |
| RemoveListeners | 10 | 4,653.9 ns | 4,500.0 ns | 568 B |
| PublishEvent_NoListeners | 10 | 134.1 ns | 100.0 ns | 0 B |
| PublishEvent_WithListeners | 10 | 1,061.1 ns | 1,000.0 ns | 0 B |
| PublishEvent_ViaSubPublisher | 10 | 572.0 ns | 600.0 ns | 0 B |
| AddListeners | 100 | 17,425.3 ns | 18,050.0 ns | 2432 B |
| AddListeners_WithWarmup | 100 | 14,585.0 ns | 15,200.0 ns | 0 B |
| AddListenerOnly | 100 | 9,109.3 ns | 8,800.0 ns | 2432 B |
| RemoveListenerOnly | 100 | 8,470.0 ns | 8,500.0 ns | 0 B |
| RemoveListeners | 100 | 16,679.8 ns | 17,400.0 ns | 2432 B |
| PublishEvent_NoListeners | 100 | 217.3 ns | 200.0 ns | 0 B |
| PublishEvent_WithListeners | 100 | 2,181.6 ns | 2,050.0 ns | 0 B |
| PublishEvent_ViaSubPublisher | 100 | 1,787.8 ns | 1,700.0 ns | 0 B |
| AddListeners | 1000 | 105,543.2 ns | 104,200.0 ns | 16840 B |
| AddListeners_WithWarmup | 1000 | 103,063.9 ns | 101,900.0 ns | 0 B |
| AddListenerOnly | 1000 | 59,788.8 ns | 59,100.0 ns | 16840 B |
| RemoveListenerOnly | 1000 | 62,606.5 ns | 61,700.0 ns | 0 B |
| RemoveListeners | 1000 | 106,143.9 ns | 104,900.0 ns | 16840 B |
| PublishEvent_NoListeners | 1000 | 227.1 ns | 200.0 ns | 0 B |
| PublishEvent_WithListeners | 1000 | 17,141.2 ns | 17,100.0 ns | 0 B |
| PublishEvent_ViaSubPublisher | 1000 | 15,666.3 ns | 15,500.0 ns | 0 B |

### `Control.ValueEventBusInterfaceBenchmarks`（24 行）

| Method | ListenerCount | Mean | Median | Allocated |
| --- | --- | --- | --- | --- |
| PublishViaInterface_NoListeners | 1 | 1,138.0 ns | 1,100.0 ns | 24 B |
| PublishViaInterface_WithListeners | 1 | 1,973.4 ns | 1,950.0 ns | 24 B |
| PublishDirect_NoListeners | 1 | 217.2 ns | 200.0 ns | 0 B |
| PublishDirect_WithListeners | 1 | 1,138.3 ns | 1,150.0 ns | 0 B |
| PublishMultipleEventTypes_ViaInterface | 1 | 1,505.3 ns | 1,500.0 ns | 72 B |
| TupleAccessPerformance | 1 | 1,428.7 ns | 1,400.0 ns | 24 B |
| InterfaceConversionOverhead | 1 | 1,690.9 ns | 1,650.0 ns | 480 B |
| DictionaryLookupWithTuple | 1 | 1,215.3 ns | 1,200.0 ns | 120 B |
| PublishViaInterface_NoListeners | 10 | 1,236.3 ns | 1,200.0 ns | 24 B |
| PublishViaInterface_WithListeners | 10 | 2,187.4 ns | 2,100.0 ns | 24 B |
| PublishDirect_NoListeners | 10 | 153.8 ns | 100.0 ns | 0 B |
| PublishDirect_WithListeners | 10 | 1,401.0 ns | 1,300.0 ns | 0 B |
| PublishMultipleEventTypes_ViaInterface | 10 | 1,357.0 ns | 1,300.0 ns | 72 B |
| TupleAccessPerformance | 10 | 1,261.5 ns | 1,200.0 ns | 24 B |
| InterfaceConversionOverhead | 10 | 1,789.4 ns | 1,700.0 ns | 480 B |
| DictionaryLookupWithTuple | 10 | 1,629.5 ns | 1,600.0 ns | 120 B |
| PublishViaInterface_NoListeners | 100 | 1,217.2 ns | 1,200.0 ns | 24 B |
| PublishViaInterface_WithListeners | 100 | 3,908.2 ns | 3,850.0 ns | 24 B |
| PublishDirect_NoListeners | 100 | 261.6 ns | 250.0 ns | 0 B |
| PublishDirect_WithListeners | 100 | 2,247.4 ns | 2,000.0 ns | 0 B |
| PublishMultipleEventTypes_ViaInterface | 100 | 1,162.1 ns | 1,100.0 ns | 72 B |
| TupleAccessPerformance | 100 | 1,255.4 ns | 1,150.0 ns | 24 B |
| InterfaceConversionOverhead | 100 | 1,595.9 ns | 1,500.0 ns | 480 B |
| DictionaryLookupWithTuple | 100 | 1,366.0 ns | 1,300.0 ns | 120 B |

### `Text.StringSegmentBenchmarks`（14 行）

| Method | Mean | Median | Allocated | Gen0 |
| --- | --- | --- | --- | --- |
| Tokenize | 360.6203 ns | 359.5588 ns | 0 B | 0.0000 |
| StringSplit | 466.8658 ns | 466.1365 ns | 3376 B | 0.3229 |
| StringSplit0 | 468.4374 ns | 465.3339 ns | 3376 B | 0.3229 |
| SegmentSplit0 | 1,208.7963 ns | 1,246.2946 ns | 1048 B | 0.1001 |
| SegmentSlice | 3.0482 ns | 3.0792 ns | 0 B | 0.0000 |
| StringSubstring | 5.7451 ns | 5.6795 ns | 88 B | 0.0084 |
| SegmentTrim | 5.8426 ns | 5.8356 ns | 0 B | 0.0000 |
| StringTrim | 9.8768 ns | 9.7096 ns | 88 B | 0.0084 |
| SegmentIndexOfString | 216.3752 ns | 216.3375 ns | 0 B | 0.0000 |
| StringIndexOfString | 15.2178 ns | 15.1441 ns | 0 B | 0.0000 |
| SegmentEquals | 1.6480 ns | 1.6465 ns | 0 B | 0.0000 |
| StringEquals | 0.4246 ns | 0.4236 ns | 0 B | 0.0000 |
| SegmentStartsWith | 2.6541 ns | 2.6436 ns | 0 B | 0.0000 |
| StringStartsWith | 1.7194 ns | 1.7180 ns | 0 B | 0.0000 |


