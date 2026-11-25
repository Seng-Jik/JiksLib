# 更新日志

本文件记录 JiksLib 的版本变化。格式参考 [Keep a Changelog](https://keepachangelog.com/zh-CN/1.1.0/)，
版本号遵循[语义化版本](https://semver.org/lang/zh-CN/)。性能数据按版本基线归档在
[`JiksLib.Benchmark/ArchivedResult/`](JiksLib.Benchmark/ArchivedResult/README.md)。

## [2.0.0] - 2026-11-25

### 概述

2.0.0 是 JiksLib 的首个对外发布版本，此前没有公开的发布标签或 NuGet 包，
因此不存在需要兼容的 1.x 公开面；版本号从 2.0.0 起，对应下面这套已经定型的公开 API 与行为契约。

本版把公开面、测试、质量门禁与性能基线一起收口：7 个命名空间、44 个顶层公开类型，同一份源码面向
`netstandard2.1`、`net48`、`net10.0`、`net11.0`，运行时零依赖。所有示例与 API 说明见
[README.md](README.md)。

### 亮点

- **零依赖、四目标框架**：库本体不引用任何 NuGet 包；`net48` 仅在构建期由 SDK 引入参考程序集包，
  不随产物分发。Unity 2022 直接引用 `netstandard2.1` 产物即可。
- **稳态零分配**：事件发布、缓存命中、`RingBuffer` 读写、`ReadOnlyListSegment` 遍历、`StringSegment`
  切片/Trim/Tokenize 在基线实测中均为 `Allocated = 0 B`。
- **事件系统**：`SafeEvent` 隔离监听器异常并支持可配置的增删/递归策略；`EventBus` 沿继承链与接口链派发，
  带 TypeChain 缓存、预热与共享；`ValueEventBus` 面向值类型事件，泛型发布路径不装箱（经接口发布除外）。
- **状态机与生命周期**：`FSM` 支持转移表、任意状态转移、默认兜底与生命周期回调；
  `ServiceCollection` / `DisposableStack` 提供可读的释放顺序与异常聚合。
- **文本与 IO**：`StringSegment` 零拷贝切片；`CsvParser` 逐行零分配解析内存 CSV；`CsvReader` 单遍流式读取；
  端序读写器覆盖 `bool`、`char`、各宽度整型与 `float` / `double`。
- **可验证**：42 个测试文件、1727 个 `[Test]`，90% 行覆盖率门禁；16 个基准类共 341 个用例，
  `v2.0.0` 性能基线随仓库提交。

### 新增

- **集合（`JiksLib.Collections`）**：`ArrayLinkedList<T>`、`BidirectionalDictionary<,>`、
  `Cache` / `ICache<,>` / `IAsyncCache<,>`、`Cell<T>`、`Deque<T>`、`FloatRange`、`IntRange`、
  `MultiDictionary<,>`、`MultiHashSet<T>`、`OnceCell<T>`、`PriorityQueue<,>`、`ReadOnlyListSegment<T>`、
  `RingBuffer<T>`、`SparseSet<T>`，以及配套的只读视图接口。
- **事件（`JiksLib.Control`）**：`SafeEvent<TEvent>`、`EventBus<TBaseEvent>`、`ValueEventBus<TConstraint>`、
  `ISafeEventPublisher<TEvent>` 与 `SafeEventHandler<TEvent>`；两条总线都提供监听器预热与清理，
  `EventBus` 另有 TypeChain 发布路径预热与共享缓存。
- **状态机**：`FSM<TState, TStateArg, TTransition>`（含 `Builder`、`IState`、零分配转移枚举器）与
  `LightFSM<TState>`。
- **生命周期与服务**：`Disposable`、`DisposableStack`、`ServiceCollection`、`UnitType`。
- **文本（`JiksLib.Text`）**：`StringSegment`（含 `Tokenizer`、比较器与 BKDR 哈希）、`CsvParser`、
  `CsvReader`、`CsvWriter`。
- **二进制与反射**：`EndianBitConverter`、`EndianBinaryReader` / `EndianBinaryWriter`、`ScannableAttribute`。
- **扩展方法（`JiksLib.Extensions`）**：`ThrowIfNull`、`AsReadOnly` 系列、`RemoveO1` / `PopBack` / `Shuffle` /
  `RandomSelect` 等列表扩展，`Split0` / `Tokenize` / `BKDRHash` / `StripPrefix` 等字符串扩展。
- **工程与工具链**：四个目标框架同源构建、XML 文档与符号包（`.snupkg`）；性能测试 skill 支持版本基线归档，
  并提供 `compare-baseline.ps1` 做基线逐行对比。

### 变更

以下是与开发期 1.x 快照相比的公开面变化，从早期源码迁移时需要逐条处理：

1. **静态 `ServiceLocator` 移除**，由实例化的 `ServiceCollection` 取代：支持 fallback 链、按类型查找、
   按逆序释放自己持有的服务与附加动作。
2. **`EventFilter<TEventPublisher, TEvent>` 移除**。需要发布端过滤时，基于 `ISafeEventPublisher<TEvent>`
   自行实现装饰器，或在发布前处理。
3. **`CsvReader` 改为单遍流式 API**：使用 `ReadRow()` / `ReadField()` / `TryReadField(out string)`，
   不再提供 `Row` / `Enumerator` / `GetEnumerator`；内存中的 CSV 改用 `CsvParser`（`string` / `StringSegment`）。
4. **`StringSegment.Tokenizer` 的构造函数改为 `internal`**，统一通过 `Tokenize(char)` 获取；
   同时新增 `string.Tokenize` 与 `string.AsSegment`。
5. **`T[].AsSegment` 改为返回 `ArraySegment<T>`**；需要零拷贝只读视图时使用新增的
   `AsReadOnlyListSegment`（`T[]` / `ArraySegment<T>` 两个重载）。
6. **`Deque` / `RingBuffer` / `SparseSet.AddRange` 接受 `ReadOnlyListSegment<T>`**，
   数组、`ArraySegment<T>` 与 `IEnumerable<T>` 重载保留；底层为数组时走无拷贝路径。
7. **`ServiceCollection.Dispose()` 不再让集合失效**：不抛 `ObjectDisposedException`，释放后查找按未注册处理，
   仍可继续注册与追加释放动作；同一实例以多个键注册时不再去重，按注册次数各释放一次
   （需要只释放一次请传 `autoDispose: false`）。
8. **`DisposableStack.Dispose()` 后可继续复用**：重复 `Dispose` 安全，栈空时无操作；
   释放过程中 `Add` 会抛 `InvalidOperationException`。
9. **`FSM.CurrentTransitions` / `AnyTimeTransitions` 改为零分配结构体枚举器**：直接 `foreach` 不装箱；
   转成 `IEnumerable` 或走 LINQ 时仍会装箱，热路径请直接遍历属性。

### 修复

以下问题在开发期评审中发现并修复，随 2.0.0 一并发布：

1. **`IntRange` 单点区间**：`Min == Max` 且任一包含标记为真时按 1 个元素处理，两端都不包含时为空，
   `Count` / `Contains` / 枚举三者一致；计数超过 `int.MaxValue` 时抛 `OverflowException`。
2. **`Cache` 淘汰时序**：`OnEvicted` 在插入完成后触发，处理器内可安全重入缓存；
   `EvictAll` 先清空再逐项通知，`Clear` 不触发通知。
3. **端序字符串上限**：`EndianBinaryReader` / `EndianBinaryWriter` 校验字符串长度，
   负数或超过 `MaxStringLength`（默认 64 MiB）抛 `InvalidDataException`。
4. **`FSM` 切换闸门**：构建、切换或重置期间（含生命周期回调内）再次调用 `Switch` / `Reset`
   抛 `InvalidOperationException`，保证回调执行期间状态机不被重入切换。
5. **`SafeEvent` 发布路径**：仅在监听器列表存在空洞时压缩；常态发布不再做 2N 次冗余写入。
6. **`Progress.CreateSubProgress`**：区间映射在端点精确命中，并校验 NaN / ∞ 与 `[0, 1]` 范围。
7. **`RingBuffer.AddRange`**：按输入顺序逐个写入，未保留的元素同样按序触发 `OnOverwrite`，并去掉快照分配。
8. **覆盖率门禁**：找不到 cobertura 报告或解析失败时直接判失败，避免采集器失效时静默通过。

### 性能基线

`v2.0.0` 是本仓库第一个版本基线，归档在
[`JiksLib.Benchmark/ArchivedResult/v2.0.0/`](JiksLib.Benchmark/ArchivedResult/v2.0.0/)：
16 个基准类 / 122 个基准方法共 341 个用例 + 3 个探针场景，全局运行 22 分 07 秒。
环境为 Windows 11 26H2、i5-12600KF、15.8 GB 内存、.NET 10.0.15（SDK 11.0.100）。

| 代表场景 | 实测结果 |
| --- | --- |
| `SafeEvent` 发布，100 个监听器 | 1,715 ns，0 B |
| `EventBus` 发布，1,000 个监听器 | 17,646 ns，0 B |
| `ValueEventBus` 经 `SubPublisher` 发布，1,000 个监听器 | 15,666 ns，0 B |
| `Cache.Access` 命中 | LRU 2.2 ns / LFU 20.9 ns，0 B |
| `SparseSet.Add`，10,000 个元素 | 295,032 B / 48.7 μs；预扩容 0 B / 37.9 μs；`AddRange`（数组）0 B / 28.5 μs |
| `RingBuffer.Add_Fill`，容量 4,096 | 13.8 μs，0 B |
| `StringSegment.Tokenize` | 360.6 ns，0 B（`string.Split` 为 466.9 ns / 3,376 B） |

内存探针（每个场景 3 次，数值一致）：

| 场景 | 保留内存 | 碎片率 |
| --- | --- | --- |
| `sparse-set-reserved`（预扩容 10 万） | 12.00 B/元素 | 0.039% |
| `sparse-set-growth`（默认容量逐个添加 10 万） | 23.59 B/元素 | 32.64% |
| `eventbus-listeners`（1 万个监听器） | 13.20 B/监听器 | 0.14% |

说明：

- 这是首次基线，没有更早的成功运行可对比，因此本版只记录绝对值，不宣称优化幅度。
- 时间是机器相关的：只有同一 CPU / OS / .NET / job / filter 之间才能换算百分比；
  分配量是确定性指标，可跨机器精确比较；碎片率只在探针实现 `FootprintProbe.cs` 的 SHA-256 一致时比较。
- `EventBusComparisonBenchmarks` 的时间列存在跨类矛盾（无监听器发布 6.6 ns 与同类单总线基准的 798–969 ns
  不可能同时成立），本版不据此下结论，留待后续复跑定位。
- 经 `ISafeEventPublisher<TConstraint>` 接口发布值类型事件会装箱，实测 24 B/次；
  高频路径应使用 `SubPublisher<TEvent>` 或直接调用 `Publisher`。这是接口设计的已知取舍。

### 验证

- **测试**：`JiksLib.Test` 共 42 个测试文件、42 个 `TestFixture`、1727 个 `[Test]` 与 40 组 `[TestCase]`，
  使用 NUnit 4.3.2；覆盖全部 7 个命名空间。
- **CI**：Windows / Linux / macOS 三平台构建；`net10.0` 与 `net11.0` 全平台测试，`net48` 仅 Windows；
  三个平台都用同一套测试跑库的 `netstandard2.1` 产物（Unity 2022 目标）。
- **门禁**：`dotnet format whitespace` 全解决方案检查；Linux 上行覆盖率 ≥ 90%（低于阈值或缺报告即失败）；
  `dotnet pack` 校验；基准子集冒烟（`--filter "*CacheBenchmarks*" --job Dry`）。
- **代码质量**：`latest-all` 分析器 0 告警，编译器告警视为错误；`Deterministic` 构建，
  可移植 PDB 与符号包。

### 已知限制

- 事件总线与 `SafeEvent` 没有异步版本；`SafeEvent` 只隔离同步异常，`async void` 监听器在 `await` 之后的异常
  不在收集范围内。
- 事件监听器是强引用，没有弱引用或自动注销机制；注册到长生命周期总线时需在对象销毁前手动移除。
- `CsvReader` 没有异步 API；`CsvWriter` 只写入内存 `StringBuilder`，没有 `Stream` / `TextWriter` 目标。
- `PriorityQueue` 是最小堆；需要大者优先时传入反向比较器。
- `ArrayLinkedList.TrimExcess()` 会重排槽位，旧槽位句柄失效；`SparseSet.Remove` 用末元素填补空位，顺序不稳定；
  `RingBuffer` 容量构造后固定。
- 完整清单见 [README 的「已知边界」](README.md#已知边界)。

### 升级到 2.0.0

- **NuGet**：`dotnet add package JiksLib --version 2.0.0`（包内含四个目标的程序集与 XML 文档，另附符号包 `.snupkg`）。
- **Unity 2022**：把 `lib/netstandard2.1/JiksLib.dll` 与 `JiksLib.xml` 放入工程即可，库不含 Unity 依赖。
- **从 1.x 快照迁移**：按上文「变更」逐条检查，重点是被移除的 `ServiceLocator` / `EventFilter`，
  以及 `CsvReader` 单遍 API、`T[].AsSegment` 返回值与 `StringSegment.Tokenizer` 构造器可见性。
- **行为契约**：`ServiceCollection` / `DisposableStack` 的释放后复用、同一实例多键注册的释放次数、
  事件总线的重入与增删策略，详见 [README 的「本版固定的行为契约」](README.md#本版固定的行为契约)。

### 链接

- [README.md](README.md)
- [性能基线与归档规则](JiksLib.Benchmark/ArchivedResult/README.md)
- [问题反馈](https://github.com/Seng-Jik/JiksLib/issues)
- [MIT 许可证](LICENSE)

[2.0.0]: https://github.com/Seng-Jik/JiksLib/releases/tag/v2.0.0
