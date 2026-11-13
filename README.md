# JiksLib

C# / .NET 工具库，有意保持了与 Unity 2022 的兼容性，提供了在游戏开发实践中常用的工具。

JiksLib 把游戏开发中反复出现的一类底层工具收敛到一处：数据结构与集合、事件分发、有限状态机、服务定位、二进制读写、CSV 读写，以及字符串和集合扩展方法。库本体不依赖任何第三方包，同一份源码同时面向 .NET 10、.NET 11、.NET Framework 4.8 与 .NET Standard 2.1（Unity 2022 可直接引用），并以 MIT 许可证发布。

## 设计取向

- **零依赖**：`JiksLib` 项目不引用任何 NuGet 包，可作为源码直接放入 Unity 工程或随程序集分发。
- **少分配**：热路径尽量使用 `ArraySegment<byte>`、结构体枚举器和结构体事件发布器，减少装箱与临时对象；`ValueEventBus` 专为值类型事件设计，正是为了避免发布过程中的装箱。
- **显式契约**：可能失败的操作提供 `Try*` 形式，参数校验统一走 `ThrowIfNull`，异常类型与消息明确。
- **可验证**：`JiksLib.Test` 用 NUnit 覆盖各模块，`JiksLib.Benchmark` 用 BenchmarkDotNet 度量关键路径。
- **主线程假设**：事件总线、`SafeEvent` 与 `ServiceLocator` 仅支持在主线程使用；多数集合非线程安全，需要并发访问时请自行加锁。

## 目标框架

| 项目 | 目标框架 | 说明 |
| --- | --- | --- |
| `JiksLib` | `net10.0`、`net11.0`、`net48`、`netstandard2.1` | 库本体，使用 C# 9 语法，不依赖第三方包 |
| `JiksLib.Test` | `net10.0`、`net11.0`、`net48` | NUnit 单元测试 |
| `JiksLib.Benchmark` | `net10.0`、`net11.0`、`net48` | BenchmarkDotNet 基准测试 |

## 仓库结构

| 路径 | 内容 |
| --- | --- |
| `JiksLib/` | 库本体，按命名空间分目录 |
| `JiksLib.Test/` | 单元测试 |
| `JiksLib.Benchmark/` | 性能基准 |
| `ai-skill/` | 工作区 skill：给 AI 助手的任务规则，目前有 `performance-test/` |
| `artifacts/` | 构建输出目录（`bin`、`obj`、`benchmark-result`），由 `Directory.Build.props` 的 `ArtifactsPath` 指定 |

## 构建与测试

需要 .NET 10 / .NET 11 SDK。CI（`.github/workflows/ci.yml`）在 Windows、Linux 与 macOS 上构建并测试，其中 `net48` 目标的测试只在 Windows 上运行。

```bash
dotnet build JiksLib.slnx
dotnet test JiksLib.Test/JiksLib.Test.csproj --framework net10.0
```

## 性能测试

`JiksLib.Benchmark` 用 BenchmarkDotNet 度量关键路径。每次运行单独归档到 `artifacts/benchmark-result/`，和 `bin`、`obj` 一样属于不提交的构建产物：

```text
artifacts/benchmark-result/202611140357-1a2b3c4d/
├── raw/                # BenchmarkDotNet 原始输出（results/ 下的 md、csv、html 与运行日志）
├── probe/              # 需要时：内置探针输出的碎片化与保留内存数据（memory.jsonl）
├── run.json            # 运行元数据：提交、dirty、filter、job、机器
├── ai-evaluation.md    # AI 评价：按下方指标优先级分析本次结果，并给代码级建议
└── ai-comparison.md    # AI 对比：与上一次运行（按目录名中的时间判定）逐项比较
```

目录名取「运行开始的本地时间 `yyyyMMddHHmm` + 提交 ID 前 8 位」，例如 `202611140357-1a2b3c4d`。指标按重要性排序：内存分配次数与大小、分配碎片化、CPU 时间、总体内存占用，最后是 AI 从代码字面给出的分析与改进建议。碎片化与保留内存由内置探针测量：`dotnet run --project JiksLib.Benchmark -- probe <scenario>`（实现见 [FootprintProbe.cs](JiksLib.Benchmark/Probes/FootprintProbe.cs)），结果归档到 run 目录的 `probe/`。完整规则（命名、`run.json` 字段、指标口径与阈值、评价模板）见工作区 skill [ai-skill/performance-test/SKILL.md](ai-skill/performance-test/SKILL.md)。

## 类索引

按命名空间分组。类型列的斜杠表示配对使用的类型（通常是可变类型及其只读视图接口），文件列给出对应的源文件。

### `JiksLib`

| 类型 | 说明 | 文件 |
| --- | --- | --- |
| `EndianBitConverter` | 指定端序的基本类型转换器，提供 `LittleEndian`、`BigEndian`、`SystemEndian` 三个实例，读写通过 `ArraySegment<byte>` 进行以避免额外分配。 | [EndianBitConverter.cs](JiksLib/EndianBitConverter.cs) |
| `Progress` | `IProgress<T>` 的工厂：`Null<T>()` 空实现、`Create` 委托实现、`CreateSubProgress` 把子进度映射到父进度的一段区间、`Convert` 转换进度类型。 | [Progress.cs](JiksLib/Progress.cs) |
| `ShellUtils` | 平台与 Shell 工具：判断 Windows 家族平台、当前平台的可执行文件后缀、按 `PATH` 查找可执行文件。 | [ShellUtils.cs](JiksLib/ShellUtils.cs) |

### `JiksLib.Collections`

| 类型 | 说明 | 文件 |
| --- | --- | --- |
| `ArrayLinkedList<T>` | 基于数组的双向链表，用槽位索引代替节点引用；在已知槽位前后 O(1) 插入，按槽位 O(1) 删除并回收空槽。 | [ArrayLinkedList.cs](JiksLib/Collections/ArrayLinkedList.cs) |
| `BidirectionalDictionary<TKey, TValue>` / `IReadOnlyBidirectionalDictionary<TKey, TValue>` | 键与值都唯一的双向字典，可由值反查键（`TryGetKeyByValue`、`RemoveByValue`、`Reversed`）。 | [BidirectionalDictionary.cs](JiksLib/Collections/BidirectionalDictionary.cs)、[IReadOnlyBidirectionalDictionary.cs](JiksLib/Collections/IReadOnlyBidirectionalDictionary.cs) |
| `Cache` / `ICache<TKey, TValue>` / `IAsyncCache<TKey, TValue>` | 带淘汰策略的缓存工厂：容量限制下的 FIFO、LRU、LFU 与永不淘汰，同步与异步两种访问形态，淘汰时触发 `OnEvicted`。 | [Cache.cs](JiksLib/Collections/Cache.cs) |
| `Cell<T>` | 可多次赋值、赋值时触发 `OnSet` 事件的格子，可用于给值类型做带类型的装箱或提供轻量变更通知。 | [Cell.cs](JiksLib/Collections/Cell.cs) |
| `Deque<T>` | 基于环形缓冲区的双端队列，两端增删均为 O(1)，支持按下标随机访问和批量添加。 | [Deque.cs](JiksLib/Collections/Deque.cs) |
| `FloatRange` | 带开闭区间标记的浮点范围，提供 `Contains` 并重写 `ToString` 输出区间写法。 | [FloatRange.cs](JiksLib/Collections/FloatRange.cs) |
| `IntRange` | 带开闭区间标记的整数范围，可枚举、可计数（`IReadOnlyCollection<int>`），支持 `Contains` 与相等比较。 | [IntRange.cs](JiksLib/Collections/IntRange.cs) |
| `MultiDictionary<TKey, TValue>` / `IReadOnlyMultiDictionary<TKey, TValue>` | 一键多值的多重字典，记录同一键值对的重复次数并实现 `ILookup<TKey, TValue>`。 | [MultiDictionary.cs](JiksLib/Collections/MultiDictionary.cs)、[IReadOnlyMultiDictionary.cs](JiksLib/Collections/IReadOnlyMultiDictionary.cs) |
| `MultiHashSet<T>` / `IReadOnlyMultiSet<T>` | 允许同一元素重复出现的哈希集合，`Add` / `Remove` 返回该元素当前的数量。 | [MultiHashSet.cs](JiksLib/Collections/MultiHashSet.cs)、[IReadOnlyMultiSet.cs](JiksLib/Collections/IReadOnlyMultiSet.cs) |
| `OnceCell<T>` | 只能赋值一次的格子，读取未赋值内容或重复赋值都会抛异常，首次赋值时触发 `OnSet`。 | [OnceCell.cs](JiksLib/Collections/OnceCell.cs) |
| `PriorityQueue<TElement, TPriority>` | 基于二叉堆的优先级队列，支持出入队、`Try*`、`DequeueEnqueue`、批量入队、按值移除与无序枚举。 | [PriorityQueue.cs](JiksLib/Collections/PriorityQueue.cs) |
| `SparseSet<T>` | 稀疏集，元素紧凑存放在数组中并以稀疏下标索引；`Add` / `RemoveAt` 平均 O(1)，通过 `InnerBuffer` 与结构体枚举器支持缓存友好的高频遍历。 | [SparseSet.cs](JiksLib/Collections/SparseSet.cs) |

### `JiksLib.Control`

| 类型 | 说明 | 文件 |
| --- | --- | --- |
| `Disposable` | `IDisposable` 辅助：全局共享的 `Empty`，以及把委托包装为最多执行一次的 `IDisposable` 的 `FromAction`。 | [Disposable.cs](JiksLib/Control/Disposable.cs) |
| `DisposableStack` | 按后进先出顺序集中释放的栈；单个对象抛异常不会中断其余对象，`Dispose` 可安全重复调用。 | [DisposableStack.cs](JiksLib/Control/DisposableStack.cs) |
| `EventBus<TBaseEvent>` | 以类型为索引的事件总线，发布时沿运行时类型的继承链与接口链分发，监听器异常收集到输出列表中；`Publisher` 是发布端结构体。 | [EventBus.cs](JiksLib/Control/EventBus.cs) |
| `ValueEventBus<TConstraint>` | 面向值类型事件的事件总线，避免发布时的装箱；`SubPublisher<TEvent>` 可提前绑定单一事件类型以进一步降低开销。 | [ValueEventBus.cs](JiksLib/Control/ValueEventBus.cs) |
| `SafeEvent<TEvent>` / `ISafeEventPublisher<TEvent>` / `EventHandler<TEvent>` | 单个事件的安全包装器：隔离监听器异常、禁止重入发布、发布期间增删的监听器延后生效。 | [SafeEvent.cs](JiksLib/Control/SafeEvent.cs) |
| `EventFilter<TEventPublisher, TEvent>` | 在发布端过滤事件的装饰器，过滤函数可以修改事件，返回 `false` 则丢弃该事件。 | [EventFilter.cs](JiksLib/Control/EventFilter.cs) |
| `FSM<TState, TStateArg, TTransition>` | 完整的有限状态机，通过 `Builder` 注册状态与转移（含任意状态转移和默认兜底状态），提供 `OnBuild`、`OnEnter`、`OnExit`、`OnReset` 生命周期与切换事件。 | [FSM.cs](JiksLib/Control/FSM.cs) |
| `LightFSM<TState>` | 不带转移表的轻量状态机，只需实现 `IState` 的 `OnEnter` / `OnExit`，用 `Switch(nextState)` 切换。 | [LightFSM.cs](JiksLib/Control/LightFSM.cs) |
| `ServiceLocator` | 静态服务定位器；通过 `Scope` 批量注册服务，`EnterScope` 返回需按逆序退出的 `IDisposable`。 | [ServiceLocator.cs](JiksLib/Control/ServiceLocator.cs) |
| `UnitType` | 空结构体单元类型，用于填充泛型类或泛型方法中的“无参数”类型位。 | [UnitType.cs](JiksLib/Control/UnitType.cs) |

### `JiksLib.Extensions`

| 类型 | 说明 | 文件 |
| --- | --- | --- |
| `AnythingExtension` | 对所有类型生效的参数校验扩展：`ThrowIfNull` 在值为 `null` 时抛出异常并返回非空值。 | [AnythingExtension.cs](JiksLib/Extensions/AnythingExtension.cs) |
| `AsReadOnlyExtension` | 把 `Dictionary`、`List`、数组、`HashSet`、`LinkedList` 转为对应的只读接口，同时完成 null 检查。 | [AsReadOnlyExtension.cs](JiksLib/Extensions/AsReadOnlyExtension.cs) |
| `ListExtension` | 列表扩展：`RemoveO1`（用末元素填补空位的 O(1) 删除）、`PopBack`、`FindIndex`、`Shuffle`、`GetReversedEnumerable`、`RandomSelect` 与按权重抽取的 `RandomSelectIndex`。 | [ListExtension.cs](JiksLib/Extensions/ListExtension.cs) |
| `StringExtension` | 字符串扩展：`Split0`（空串返回空数组）、`BKDRHash`、`StripPrefix` / `StripSuffix` / `StripPrefixSuffix`。 | [StringExtension.cs](JiksLib/Extensions/StringExtension.cs) |

### `JiksLib.IO`

| 类型 | 说明 | 文件 |
| --- | --- | --- |
| `EndianBinaryReader` | 按指定端序读取二进制流，提供定长读取、`EndOfStreamException` 语义以及带长度前缀的字符串读取。 | [EndianBinaryReader.cs](JiksLib/IO/EndianBinaryReader.cs) |
| `EndianBinaryWriter` | 按指定端序写入二进制流，提供定长写入、`Seek`、`Flush` / `FlushAsync` 与异步写入变体。 | [EndianBinaryWriter.cs](JiksLib/IO/EndianBinaryWriter.cs) |

### `JiksLib.Reflection`

| 类型 | 说明 | 文件 |
| --- | --- | --- |
| `ScannableAttribute` | 可被扫描的标记特性基类；`ScanAssemblies` 扫描程序集并按特性类型归类，返回可通过 `GetTypesByAttribute` 查询类型列表的 `ScanResult`。 | [ScannableAttribute.cs](JiksLib/Reflection/ScannableAttribute.cs) |

### `JiksLib.Text`

| 类型 | 说明 | 文件 |
| --- | --- | --- |
| `CsvReader` | 按 RFC 4180 流式解析 CSV 的读取器，可修改分隔符以读取 TSV / DSV。 | [CsvReader.cs](JiksLib/Text/CsvReader.cs) |
| `CsvWriter` | 按 RFC 4180 生成 CSV 的写入器，可修改分隔符以输出 TSV / DSV，并支持强制给所有字段加引号。 | [CsvWriter.cs](JiksLib/Text/CsvWriter.cs) |
