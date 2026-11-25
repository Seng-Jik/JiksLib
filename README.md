# JiksLib

[![CI](https://github.com/Seng-Jik/JiksLib/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/Seng-Jik/JiksLib/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![Target Frameworks](https://img.shields.io/badge/.NET-netstandard2.1%20%7C%20net48%20%7C%20net10.0%20%7C%20net11.0-512bd4)](JiksLib/JiksLib.csproj)
![Version](https://img.shields.io/badge/version-2.0.0-brightgreen)

**JiksLib 是一个面向游戏与工具开发的 C# / .NET 基础库：集合、事件、状态机、缓存、二进制与 CSV。运行时零依赖，同一份源码同时面向 .NET 10、.NET 11、.NET Framework 4.8 与 .NET Standard 2.1，Unity 2022 可直接使用。**

游戏项目里最容易被重复实现、也最容易被写错的，往往是这些底层设施：一个既省内存又不会在回调里崩掉的事件系统，一个不靠 `try/catch` 撑起来的生命周期管理，一个零分配的 CSV 解析器，或者一个能把 Unity 2022 缺失的 `PriorityQueue` 补齐的集合。JiksLib 把它们收敛到一处，并以"热路径零分配、契约显式、可被测试与基准验证"的标准实现。

v2.0.0 是本库的一次整体收口：公开面、测试、CI 门禁与性能基准一起定版。库本体 7 个命名空间、40 余个公开类型，配套 42 个测试文件、1700 余个测试方法与 122 个基准方法；CI 在 Windows / Linux / macOS 上构建，行覆盖率以 90% 为门禁，分析器在 `latest-all` 下保持 0 告警。

## 特性

- **零依赖**：库本体不引用任何 NuGet 包。`net48` 目标仅在构建期由 SDK 自动引入参考程序集包，不随产物分发。
- **少分配**：热路径统一使用 `ArraySegment<byte>`、结构体枚举器、结构体事件发布器与零拷贝字符串切片；事件发布、CSV 解析、二进制数值读写与 `StringSegment` 切分在稳态下都可以做到零分配。
- **一份源码，四个目标**：Unity 2022 用 `netstandard2.1`，传统桌面程序用 `net48`，现代 .NET 用 `net10.0` / `net11.0`；只有两处成员因运行时能力差异而不同（见[兼容性](#兼容性)）。
- **显式契约**：异常类型与消息明确；可预期的失败优先用 `Try*` 或 `bool + out`，查询类操作找不到时返回 `null`；事件监听器的异常不会从 `Publish` 抛出，而是收集到调用方提供的集合。
- **可验证**：统一空白格式门禁、`latest-all` 分析器 0 告警、90% 行覆盖率门禁、BenchmarkDotNet 基准、结果归档与版本基线，全部在仓库内并随 CI 执行。
- **主线程假设**：事件总线、`SafeEvent`、`ServiceCollection` 与多数集合只按单线程使用设计，需要并发时由调用方加锁或把操作调度回主线程。

## 目录

- [特性](#特性)
- [兼容性](#兼容性)
- [安装](#安装)
- [快速开始](#快速开始)
- [模块与 API 索引](#模块与-api-索引)
- [性能](#性能)
- [构建与测试](#构建与测试)
- [仓库结构](#仓库结构)
- [开发约定](#开发约定)
- [版本 v2.0.0](#版本-v200)
- [反馈](#反馈)
- [许可证](#许可证)

## 兼容性

### 目标框架

| 目标框架 | 主要用途 | 说明 |
| --- | --- | --- |
| `netstandard2.1` | Unity 2022、.NET Standard 2.1 运行时 | 主目标；Unity 2022 直接引用该产物 |
| `net48` | .NET Framework 4.8 | 与 `netstandard2.1` 相同的 API 面 |
| `net10.0` / `net11.0` | 现代 .NET | 在相同 API 之上，额外提供两处依赖新运行时能力的成员 |

> **多目标 API 差异**：`net10.0` / `net11.0` 比 `net48` / `netstandard2.1` 多出
> `BidirectionalDictionary<TKey, TValue>` 的 `IEnumerable<KeyValuePair<TKey, TValue>>` 构造，
> 以及 `PriorityQueue<TElement, TPriority>.EnsureCapacity(int)`（旧运行时的 `List<T>` 没有 `EnsureCapacity`）。
> 除此之外 API 完全一致。

库本体使用 **C# 9**；测试与基准工程使用 C# 10。`Nullable` 在所有工程中启用。

### Unity 2022

Unity 2022 的脚本运行时支持 .NET Standard 2.1，因此：

- **推荐**：引用包内 `lib/netstandard2.1/JiksLib.dll`（连同同名 XML 文档），库不含任何 Unity 依赖。
- **备选**：把 `JiksLib/` 源码复制进 `Assets/` 参与编译。仓库不附带 `.asmdef`，需要独立程序集时请自行添加。

注意 `.editorconfig` 中的 Unity 约定：对可能为 `UnityEngine.Object` 的值，不要使用 `is null` / `is not null`、`??` 或 `?.`，因为模式匹配不会走 Unity 重载的 `==` / `!=`，无法识别"已销毁对象"。

### 线程与生命周期

- 事件总线、`SafeEvent`、`ServiceCollection`、`DisposableStack` 与大多数集合**都不是线程安全的**；需要并发访问时由调用方加锁。
- `EndianBinaryReader` / `EndianBinaryWriter`、`CsvReader` / `CsvWriter` 持有可变状态，同样不支持并发使用。
- 异步缓存（`IAsyncCache`）的工厂回调可能在任意线程完成，调用方必须自行序列化对缓存、回调状态与 `OnEvicted` 处理器的访问；`ICache` 与 `IAsyncCache` 本身都不是线程安全的。
- 事件总线与 `SafeEvent` **不实现 `IDisposable`**，也不持有弱引用：监听器是强引用，注册到长生命周期的总线或事件上时，必须在对象销毁前手动 `RemoveListener`，否则监听器及其捕获的对象会长期驻留。
- 集合的枚举器多数不检测版本号：不要依赖枚举期间修改集合的行为，结果未定义（基于 `Dictionary` 的集合通常会抛异常）。

## 安装

### NuGet

包 ID 为 `JiksLib`，MIT 许可证，同时提供符号包（`.snupkg`）。

```bash
dotnet add package JiksLib --version 2.0.0
```

包内含 `netstandard2.1`、`net48`、`net10.0`、`net11.0` 四套程序集与 XML 文档，README 也随包发布。

### 在 Unity 2022 中引用

1. 在 `Assets/` 下建立 `Plugins/JiksLib/`；
2. 把包内 `lib/netstandard2.1/JiksLib.dll` 与 `JiksLib.xml` 复制进去；
3. 在 Inspector 中确认该 DLL 的目标平台设置符合你的构建目标（默认 Any Platform 即可）。

### 源码构建

```bash
git clone https://github.com/Seng-Jik/JiksLib.git
cd JiksLib
dotnet build JiksLib.slnx
```

需要 .NET 10 / .NET 11 SDK（CI 同时安装 `10.0.x` 与 `11.0.x`）。

## 快速开始

### 事件总线

`EventBus<TBaseEvent>` 以静态类型为键注册监听器，发布时沿**运行时类型的继承链与接口链**派发。构造函数通过 `out` 参数返回发布器：

```csharp
public interface IGameEvent { }

public sealed class PlayerDied : IGameEvent
{
    public string Reason = string.Empty;
}
```

```csharp
var bus = new EventBus<IGameEvent>(out var publisher);

bus.AddListener<PlayerDied>(e => Console.WriteLine($"玩家死亡：{e.Reason}"));

// 第二个参数接收监听器抛出的异常；传 null 表示静默忽略，异常不会从 Publish 抛出
publisher.Publish(new PlayerDied { Reason = "hp = 0" }, null);
```

`ValueEventBus<TConstraint>` 面向**值类型事件**，只按确切结构体类型派发，发布过程不装箱；高频路径可以用发布器的 `GetSubPublisher` 提前绑定单一事件类型，省去每次字典查找：

```csharp
public interface IValueEvent { }

public readonly struct DamageEvent : IValueEvent
{
    public DamageEvent(int amount) => Amount = amount;
    public int Amount { get; }
}
```

```csharp
var bus = new ValueEventBus<IValueEvent>(out var publisher);
bus.AddListener<DamageEvent>(e => Console.WriteLine($"受到 {e.Amount} 点伤害"));

publisher.Publish(new DamageEvent(9), null);

var damagePublisher = publisher.GetSubPublisher<DamageEvent>();
damagePublisher.Publish(new DamageEvent(12), null);
```

> `GetSubPublisher` 定义在发布器上，而不是总线上；一旦调用，`PurgeListenerSlot` / `PurgeListenerSlots` 会永久禁用（即使副本已被丢弃）。

单个事件用 `SafeEvent<TEvent>` 即可，异常隔离与重入策略与两条总线一致：

```csharp
var safeEvent = new SafeEvent<int>(out var publisher, allowRecursiveInvoke: true);

safeEvent.AddListener(v => Console.WriteLine($"值变为 {v}"));
publisher.Publish(42, null);
```

### 有限状态机

`FSM<TState, TStateArg, TTransition>` 通过 `Builder` 注册状态与转移，支持任意状态转移（`AddAnyTimeTransition`）和默认兜底状态。`Builder` 只能构建一次，构建后只读：

```csharp
public enum BattleTransition { Start, Win, Lose }

public abstract class BattleState : FSM<BattleState, UnitType, BattleTransition>.IState
{
    public virtual void OnBuild(FSM<BattleState, UnitType, BattleTransition> fsm, UnitType arg) { }
    public virtual void OnEnter(BattleState? prevState, BattleTransition? transition) { }
    public virtual void OnExit(BattleTransition? transition, BattleState? nextState) { }
    public virtual void OnReset(BattleState? lastState, BattleState startState) { }
}

public sealed class Idle : BattleState { }
public sealed class Fighting : BattleState { }
public sealed class Victory : BattleState { }
```

```csharp
var idle = new Idle();
var fighting = new Fighting();
var victory = new Victory();

var builder = new FSM<BattleState, UnitType, BattleTransition>.Builder(idle);
builder.AddState(fighting);
builder.AddState(victory);
builder.AddTransition(idle, BattleTransition.Start, fighting);   // 指定状态的转移优先
builder.AddTransition(fighting, BattleTransition.Win, victory);
builder.AddAnyTimeTransition(BattleTransition.Lose, idle);      // 任意状态的兜底转移

var fsm = builder.Build(new UnitType());                        // Builder 只能构建一次
Console.WriteLine(fsm.Switch(BattleTransition.Start));           // True
Console.WriteLine(fsm.CurrentState.GetType().Name);              // Fighting
```

找不到转移时 `Switch(TTransition)` 返回 `false`；配置了 `defaultState` 则转移到默认状态并返回 `true`。在构建、切换或重置过程中（包括 `OnBuild` / `OnEnter` / `OnExit` / `OnReset` 回调与 `OnStateSwitch` 事件内）再次调用 `Switch` / `Reset` 会抛 `InvalidOperationException`。

不需要转移表时用 `LightFSM<TState>`：只需实现 `IState.OnEnter` / `OnExit`，用 `Switch(nextState)` 切换。

### 集合

```csharp
var recent = new RingBuffer<string>(capacity: 3);
recent.Add("a");
recent.Add("b");
recent.Add("c");
recent.Add("d");                  // 覆盖最旧的 "a"
Console.WriteLine(recent[0]);     // d：下标 0 是最新

// 与 BCL 的 System.Collections.Generic.PriorityQueue 同名：同时 using 两个命名空间时需要完全限定或起别名
var pending = new JiksLib.Collections.PriorityQueue<string, int>();   // 最小堆：数值越小越先出
pending.Enqueue("低优先级", 10);
pending.Enqueue("高优先级", 1);
Console.WriteLine(pending.Peek().Item1);          // 高优先级

var set = new SparseSet<int>();
int index = set.Add(42);
Console.WriteLine(set[index]);                    // 42

var deque = new Deque<int>();
deque.Add(2);
deque.AddFront(1);                                // 1, 2
```

批量输入优先用 `ReadOnlyListSegment<T>`，它在底层是数组时走无拷贝路径：

```csharp
ReadOnlyListSegment<int> segment = new[] { 1, 2, 3, 4 }.AsReadOnlyListSegment().Slice(1, 2);

var deque = new Deque<int>();
deque.AddRange(segment);      // 不复制、不装箱
```

> `PriorityQueue` 的类型名刻意与 BCL 同名同语义，用于补齐 Unity 2022 等尚未提供该类型的运行时。同一文件同时使用两个命名空间时，请完全限定或起别名。

### 缓存

`Cache` 是静态工厂，返回同步的 `ICache<,>` 或异步的 `IAsyncCache<,>`：

```csharp
ICache<int, string> cache = Cache.Make<int, string>(
    Cache.Algorithm.LRU,
    key => LoadName(key),        // 未命中时调用
    capacity: 128);

cache.OnEvicted += (key, value) => Console.WriteLine($"淘汰 {key}");

string name = cache.Access(7);

if (cache.GetCached(7, out string cached, dontModifyCacheState: true))
    Console.WriteLine(cached);

cache.Evict(7);
```

```csharp
IAsyncCache<int, string> cache = Cache.MakeAsync<int, string>(
    Cache.Algorithm.LFU,
    (key, onReceive, onException) => LoadNameAsync(key, onReceive, onException),
    capacity: 128);

cache.Access(
    7,
    value => Console.WriteLine(value),
    ex => Console.WriteLine(ex.Message));
```

四种算法：`FIFO`、`LRU`、`LFU`、`NeverEvict`（忽略容量）。同一个实例同时实现了同步与异步接口，用错一面会抛 `InvalidOperationException`——这是两种创建方式互斥的固有特性。`Evict` / `EvictAll` 会触发 `OnEvicted`，`Clear` 不会。

### 字符串与 CSV

`StringSegment` 是 `string` 的只读值类型视图，切片、Trim、查找与切分都不复制底层字符串：

```csharp
StringSegment line = "[INFO] loaded: 123 assets";

if (line.StartsWith("[INFO]", StringComparison.Ordinal))
    line = line.Slice("[INFO]".Length).Trim();

foreach (StringSegment token in line.Tokenize(':'))
    Console.WriteLine(token.ToString());   // "loaded"、" 123 assets"
```

内存中的 CSV 用 `CsvParser`，`Row` / `Field` 都是只读结构体切片，`foreach` 全程零分配：

```csharp
var parser = new CsvParser("id,name\n1,Alice\n2,\"Bob, Jr.\"");

foreach (var row in parser)
{
    foreach (var field in row)
        Console.Write(field.Value.ToString() + "\t");   // Field.Value 是 StringSegment
    Console.WriteLine();
}
```

流式读取用 `CsvReader`：`ReadRow()` 只定位一行，`ReadField()` / `TryReadField()` 逐字段物化，未读字段在下一行开始时直接跳过、不分配：

```csharp
using var reader = new CsvReader(stream);   // stream：任意可读流，默认 UTF-8、自动识别 BOM

while (reader.ReadRow())
{
    string id = reader.ReadField();

    while (reader.TryReadField(out string name))
        Console.WriteLine($"{id}: {name}");
}
```

写入用 `CsvWriter`，按 RFC 4180 的引号与 `""` 转义规则生成，记录分隔固定 CRLF：

```csharp
var writer = new CsvWriter();
writer.WriteField("id");
writer.WriteField("name");
writer.NextRecord();
writer.WriteField("1");
writer.WriteField("Bob, Jr.");    // 含分隔符，自动加引号
writer.NextRecord();

Console.WriteLine(writer.ToString());
// id,name
// 1,"Bob, Jr."
```

### 二进制读写

端序由传入的 `EndianBitConverter` 实例决定；字符串为 `Int32` 字节长度前缀 + 指定编码（默认 UTF-8），默认上限 64 MiB：

```csharp
using var stream = new MemoryStream();

using (var writer = new EndianBinaryWriter(EndianBitConverter.LittleEndian, stream, leaveOpen: true))
{
    writer.Write(1234);          // Int32
    writer.Write("JiksLib");     // 长度前缀 + UTF-8
}

stream.Position = 0;

using var reader = new EndianBinaryReader(EndianBitConverter.LittleEndian, stream);
Console.WriteLine(reader.ReadInt32());   // 1234
Console.WriteLine(reader.ReadString());  // JiksLib
```

定长读取不足时抛 `EndOfStreamException`。只需要基本类型与字节之间的转换时，直接用 `EndianBitConverter`，写入 `ArraySegment<byte>` 不产生分配：

```csharp
var buffer = new byte[4];
EndianBitConverter.BigEndian.GetBytes(0x12345678, new ArraySegment<byte>(buffer));
```

### 服务集合与释放

`ServiceCollection` 以类型为键注册服务，查找时先查自身、再沿 fallback 链查找；释放时按注册顺序的逆序释放自己持有的服务与附加动作，fallback 永不被释放：

```csharp
public interface IAudio { void Play(string clip); }

public sealed class AudioService : IAudio
{
    public void Play(string clip) { }
}

public sealed class GameServices : ServiceCollection
{
    public GameServices(ServiceCollection? fallback = null) : base(fallback)
    {
        Register<IAudio>(new AudioService());
        RunOnDispose(() => Console.WriteLine("服务已释放"));
    }
}
```

```csharp
using (var services = new GameServices())
{
    services.Get<IAudio>().Play("bgm");
}
```

`DisposableStack` 是同一套释放语义的独立版本：LIFO 执行，单个对象抛异常不会中断其余对象，全部完成后才重新抛出（多个异常聚合为 `AggregateException`）：

```csharp
using var stack = new DisposableStack();
stack.Add(someDisposable);                        // IDisposable
stack.Add(() => Console.WriteLine("cleanup"));    // Action
```

`Dispose` 可以安全重复调用，返回后还能当作全新的空栈/空集合继续使用；唯一限制是释放过程中禁止再 `Add`。

## 模块与 API 索引

按命名空间分组。类型列的斜杠表示配对使用的类型（通常是可变类型及其只读视图接口），文件列指向对应源文件。

### `JiksLib`

| 类型 | 说明 | 文件 |
| --- | --- | --- |
| `EndianBitConverter` | 指定端序的基本类型 ↔ 字节转换器，提供 `LittleEndian`、`BigEndian`、`SystemEndian` 三个实例；读写通过 `ArraySegment<byte>` 进行，原地写入不分配，长度不足抛 `ArgumentException`。 | [EndianBitConverter.cs](JiksLib/EndianBitConverter.cs) |
| `Progress` | `IProgress<T>` 工厂：`Null<T>()` 空实现、`Create` 委托实现；扩展方法 `CreateSubProgress` 把子进度的 `[0, 1]` 映射到父进度的 `[start, end]` 区间（允许反向），`Convert` 转换进度类型。 | [Progress.cs](JiksLib/Progress.cs) |
| `ShellUtils` | 平台与可执行文件工具：`IsWindowsFamilyOS` / `IsCurrentOSWindowsFamily`、`ExecutableSuffixes`，以及按 `PATH` 查找可执行文件的 `FindExecutable`（找不到返回 `null`，不查当前工作目录）。 | [ShellUtils.cs](JiksLib/ShellUtils.cs) |
| `StringSegment` | `string` 的只读值类型视图：切片、Trim、查找、比较、`Split` / `Tokenize` 不复制底层字符串；自带 `EqualityComparer` / `Comparer` 与 BKDR 哈希，`default` 即空片段。 | [StringSegment.cs](JiksLib/StringSegment.cs) |

### `JiksLib.Collections`

| 类型 | 说明 | 文件 |
| --- | --- | --- |
| `ArrayLinkedList<T>` | 基于数组的双向链表，用槽位索引代替节点引用；已知槽位时 O(1) 插入/删除并回收空槽。槽位即句柄，`TrimExcess()` 会重排槽位、使旧句柄失效。 | [ArrayLinkedList.cs](JiksLib/Collections/ArrayLinkedList.cs) |
| `BidirectionalDictionary<TKey, TValue>` / `IReadOnlyBidirectionalDictionary<TKey, TValue>` | 键与值都唯一的双向字典，可由值反查键（`TryGetKeyByValue`、`RemoveByValue`），支持 `Reversed` 视图与 `CopyAndReverse()`。 | [BidirectionalDictionary.cs](JiksLib/Collections/BidirectionalDictionary.cs)、[IReadOnlyBidirectionalDictionary.cs](JiksLib/Collections/IReadOnlyBidirectionalDictionary.cs) |
| `Cache` / `ICache<TKey, TValue>` / `IAsyncCache<TKey, TValue>` | 静态缓存工厂：容量限制下的 FIFO、LRU、LFU 与永不淘汰，同步与异步两种访问形态，淘汰时触发 `OnEvicted`；内部复用节点与频率桶，稳态插入不产生新分配。 | [Cache.cs](JiksLib/Collections/Cache.cs) |
| `Cell<T>` | 可多次赋值、每次赋值都触发 `OnSet` 的格子，可为值类型提供带类型的引用封装或轻量变更通知。 | [Cell.cs](JiksLib/Collections/Cell.cs) |
| `Deque<T>` | 基于环形缓冲区的双端队列，两端增删均为 O(1)，支持按下标随机访问、`AddRange` / `AddRangeFront` 批量添加与 `ReadOnlyListSegment` 无拷贝输入。 | [Deque.cs](JiksLib/Collections/Deque.cs) |
| `FloatRange` | 带开闭区间标记的浮点范围，拒绝 NaN 与 `min > max`，`ToString` 以 `InvariantCulture` 输出 `[a, b)` 形式。 | [FloatRange.cs](JiksLib/Collections/FloatRange.cs) |
| `IntRange` | 带开闭区间标记的整数范围，可枚举、可计数（`IReadOnlyCollection<int>`）；单点区间语义显式定义，`Count` 超过 `int.MaxValue` 时抛 `OverflowException`。 | [IntRange.cs](JiksLib/Collections/IntRange.cs) |
| `MultiDictionary<TKey, TValue>` / `IReadOnlyMultiDictionary<TKey, TValue>` | 一键多值的多重字典，记录同一键值对的重复次数并实现 `ILookup<TKey, TValue>`；索引器缺键时返回空集合，而不是抛异常。 | [MultiDictionary.cs](JiksLib/Collections/MultiDictionary.cs)、[IReadOnlyMultiDictionary.cs](JiksLib/Collections/IReadOnlyMultiDictionary.cs) |
| `MultiHashSet<T>` / `IReadOnlyMultiSet<T>` | 允许同一元素重复出现的哈希集合，`Add` / `Remove` 返回该元素当前的数量（`Count` 含重数）。 | [MultiHashSet.cs](JiksLib/Collections/MultiHashSet.cs)、[IReadOnlyMultiSet.cs](JiksLib/Collections/IReadOnlyMultiSet.cs) |
| `OnceCell<T>` | 只能赋值一次的格子，读取未赋值内容或重复赋值都会抛 `InvalidOperationException`，首次赋值时触发 `OnSet`。 | [OnceCell.cs](JiksLib/Collections/OnceCell.cs) |
| `PriorityQueue<TElement, TPriority>` | 基于二叉堆的最小优先队列：`Try*`、`DequeueEnqueue`、`EnqueueRange`、按值 `Remove`、`UnorderedItems` 视图与 `Clone`。类型名刻意与 BCL 同名同语义，用于为 Unity 2022 等旧运行时补齐该类型。 | [PriorityQueue.cs](JiksLib/Collections/PriorityQueue.cs) |
| `ReadOnlyListSegment<T>` | 对 `IReadOnlyList` 一段区间的零拷贝只读视图，值类型、默认值为空片段、可由 `T[]` 隐式转换；相等性为"同一底层列表 + 同一区间"，可作为 `Deque`、`RingBuffer`、`SparseSet` 的批量输入。 | [ReadOnlyListSegment.cs](JiksLib/Collections/ReadOnlyListSegment.cs) |
| `RingBuffer<T>` | 定长环形缓存，下标 0 为最新元素，写满覆盖最旧元素；支持 `AddRange`、`TryPeekNewest` / `TryRemoveOldest` 等、`CopyTo`、结构体枚举器，覆盖时触发 `OnOverwrite`。 | [RingBuffer.cs](JiksLib/Collections/RingBuffer.cs) |
| `SparseSet<T>` | 稀疏集，元素紧凑存放在数组中并以稀疏下标索引；`Add` / `RemoveAt` 摊销 O(1)，通过 `InnerBuffer` 与有序/无序结构体枚举器支持缓存友好的高频遍历。 | [SparseSet.cs](JiksLib/Collections/SparseSet.cs) |

### `JiksLib.Control`

| 类型 | 说明 | 文件 |
| --- | --- | --- |
| `Disposable` | `IDisposable` 辅助：全局共享的 `Empty` 空实现，以及把委托包装为最多执行一次的 `IDisposable` 的 `FromAction`。 | [Disposable.cs](JiksLib/Control/Disposable.cs) |
| `DisposableStack` | 按后进先出顺序集中释放的栈；单个对象抛异常不会中断其余对象，全部完成后统一抛出（多个异常为 `AggregateException`）；可重复 `Dispose` 并在之后继续复用。 | [DisposableStack.cs](JiksLib/Control/DisposableStack.cs) |
| `EventBus<TBaseEvent>` | class / interface 事件的类型索引总线：派发顺序为运行时类型 → 接口 → 基类 → `TBaseEvent`，监听器异常收集到调用方传入的集合；TypeChain 缓存可共享、预热（`WarmupPublish`）与清空（`ClearTypeChainCache`），监听器存储支持预热与清理。 | [EventBus.cs](JiksLib/Control/EventBus.cs) |
| `ValueEventBus<TConstraint>` | 值类型事件总线：只按确切结构体类型派发、发布过程不装箱；`SubPublisher<TEvent>` 可提前绑定单一事件类型以进一步降低开销。 | [ValueEventBus.cs](JiksLib/Control/ValueEventBus.cs) |
| `SafeEvent<TEvent>` / `ISafeEventPublisher<TEvent>` / `SafeEventHandler<TEvent>` | 单个事件的安全包装器：隔离监听器同步异常；默认拒绝发布期间增删监听器与递归发布，可配置为"最外层发布结束后应用增删"与 FIFO 队列化递归（队列长度无上限，终止性由调用方保证）。 | [SafeEvent.cs](JiksLib/Control/SafeEvent.cs) |
| `FSM<TState, TStateArg, TTransition>` | 完整的有限状态机：通过 `Builder` 注册状态、转移、任意状态转移与默认兜底状态，提供 `OnBuild`、`OnEnter`、`OnExit`、`OnReset` 生命周期与切换事件；`CurrentTransitions` / `AnyTimeTransitions` 零分配枚举转移；`Builder` 只能构建一次，可用复制构造函数派生可再次构建的副本。 | [FSM.cs](JiksLib/Control/FSM.cs) |
| `LightFSM<TState>` | 不带转移表的轻量状态机，只需实现 `IState` 的 `OnEnter` / `OnExit`，用 `Switch(nextState)` 切换。 | [LightFSM.cs](JiksLib/Control/LightFSM.cs) |
| `ServiceCollection` | 类型键服务集合：查找时先查自身再沿 fallback 链精确匹配，释放时按逆序释放自己持有的服务与附加动作，fallback 永不被释放；`Dispose` 不会让集合失效，释放后仍可作为空集合继续注册与查找。 | [ServiceCollection.cs](JiksLib/Control/ServiceCollection.cs) |
| `UnitType` | 空结构体单元类型，用于填充泛型类或泛型方法中的"无参数"类型位（例如 `FSM` 的 `TStateArg`）。 | [UnitType.cs](JiksLib/Control/UnitType.cs) |

### `JiksLib.Extensions`

| 类型 | 说明 | 文件 |
| --- | --- | --- |
| `AnythingExtension` | 对所有类型生效的参数校验扩展：`ThrowIfNull(value, paramName, message)` 在值为 `null` 时抛 `ArgumentNullException`，否则原样返回以支持链式调用。 | [AnythingExtension.cs](JiksLib/Extensions/AnythingExtension.cs) |
| `AsReadOnlyExtension` | 把 `Dictionary`、`List`、数组、`HashSet`、`LinkedList` 暴露为对应的只读接口；返回原实例而非包装器，只读性仅由返回接口保证（`List` 重载需静态调用，实例语法会绑定 BCL 的 `List<T>.AsReadOnly`）。 | [AsReadOnlyExtension.cs](JiksLib/Extensions/AsReadOnlyExtension.cs) |
| `ListExtension` | 列表扩展：`RemoveO1`（末元素填补空位的 O(1) 删除）、`PopBack`、`FindIndex`、`Shuffle`、`GetReversedEnumerable`、`AsSegment`（`T[]` → `ArraySegment`、`IReadOnlyList` → `ReadOnlyListSegment`）、`AsReadOnlyListSegment`、`RandomSelect` 与按权重抽取的 `RandomSelectIndex`（函数式与预计算权重两个重载）。 | [ListExtension.cs](JiksLib/Extensions/ListExtension.cs) |
| `StringExtension` | 字符串扩展：`Split0`（空串返回空数组）、`AsSegment`（零拷贝片段视图）、`Tokenize`（零分配切分器）、`BKDRHash`、`StripPrefix` / `StripSuffix` / `StripPrefixSuffix`（序数比较，失败时 `remain` 为空串）。 | [StringExtension.cs](JiksLib/Extensions/StringExtension.cs) |

### `JiksLib.IO`

| 类型 | 说明 | 文件 |
| --- | --- | --- |
| `EndianBinaryReader` | 按指定的 `EndianBitConverter` 读取二进制流：定长读取不足抛 `EndOfStreamException`，字符串读取为 `Int32` 字节长度前缀 + 指定编码（默认 UTF-8，上限 `MaxStringLength`）；无异步 API。 | [EndianBinaryReader.cs](JiksLib/IO/EndianBinaryReader.cs) |
| `EndianBinaryWriter` | 对称的写入端：定长写入、`Position` / `Seek`、`Flush` / `FlushAsync` 与异步写入变体；`Write(string)` 写入同一长度前缀格式，受 `MaxStringLength` 限制。 | [EndianBinaryWriter.cs](JiksLib/IO/EndianBinaryWriter.cs) |

### `JiksLib.Reflection`

| 类型 | 说明 | 文件 |
| --- | --- | --- |
| `ScannableAttribute` | 可被扫描的类型级标记特性基类（`Inherited = false`）；`ScanAssemblies` 反射给定程序集并按具体特性类型精确分组，返回可通过 `GetTypesByAttribute<TAttr>()` 查询类型列表的 `ScanResult`。不做跨调用缓存，加载失败的类型静默跳过。 | [ScannableAttribute.cs](JiksLib/Reflection/ScannableAttribute.cs) |

### `JiksLib.Text`

| 类型 | 说明 | 文件 |
| --- | --- | --- |
| `CsvParser` | 解析内存中 `string` / `StringSegment` 形式的 CSV：RFC 4180 引号与 `""` 转义，记录分隔兼容 CRLF / LF / CR；`Row` / `Field` 是只读结构体切片，`Field.Raw` 与 `HasEscapedQuotes` 保留原始信息。**`foreach` 枚举全程零分配**，只有读取含转义的字段值或调用 `ToString()` 才分配。 | [CsvParser.cs](JiksLib/Text/CsvParser.cs) |
| `CsvReader` | 从 `Stream` 单遍流式读取 CSV（默认 UTF-8 并自动识别 BOM）：`ReadRow()` 推进到下一行，`ReadField()` / `TryReadField()` 逐个读取字段；未读字段在下一行开始时扫描跳过，不物化、不分配。分隔符可配置以读取 TSV / DSV；无 `IEnumerable`、无异步。 | [CsvReader.cs](JiksLib/Text/CsvReader.cs) |
| `CsvWriter` | 在内存中按 RFC 4180 的引号/转义规则拼装 CSV，记录分隔固定 CRLF；分隔符可配置以输出 TSV / DSV（此时不再是严格 RFC 4180），`AlwaysWrap` 可强制给所有字段加引号。 | [CsvWriter.cs](JiksLib/Text/CsvWriter.cs) |

## 性能

### 设计手段

- **结构体发布器与枚举器**：`Publisher` / `SubPublisher` / 各类 `Enumerator` 都是结构体，`foreach` 与发布路径不产生装箱。
- **零拷贝切片**：`StringSegment`、`ReadOnlyListSegment`、`ArraySegment<byte>` 贯穿字符串、集合与 IO；`CsvParser.Field` 只保存切片。
- **缓存友好的存储**：`SparseSet` 把元素紧凑存放在连续数组，`RingBuffer` / `Deque` 使用环形缓冲，`ArrayLinkedList` 用槽位代替节点对象。
- **稳态零分配**：`Cache` 复用淘汰产生的节点与频率桶，事件总线缓存 `TypeChain`，`CsvReader` 复用固定缓冲与 `StringBuilder`。
- **可预热**：`EventBus.Publisher.WarmupPublish` / `EventBus.WarmupListenerSlot` 把首次发布与首次注册的构链、扩容成本移出热路径。
- **可度量**：关键路径都有对应的基准类与内存探针，回归以分配量优先、CPU 时间其次。

### 运行基准

基准工程使用 BenchmarkDotNet 0.15.8，共 16 个基准类、122 个基准方法，每个类固定 `[SimpleJob(RuntimeMoniker.Net10_0)]` + `[MemoryDiagnoser]`。`net11.0` 与 `net48` 目标只用于编译验证，基准实际在 .NET 10 上运行。

```bash
# 先看会跑哪些基准
dotnet run --project JiksLib.Benchmark -c Release -f net10.0 -- --list flat

# 跑全部基准，报告写入自定义目录
dotnet run --project JiksLib.Benchmark -c Release -f net10.0 -- \
  --filter '*' --allStats --artifacts artifacts/benchmark-result/<run>/raw
```

覆盖的基准类：`CacheBenchmarks`、`CacheEvictionBenchmarks`、`ReadOnlyListSegmentBenchmarks`、`RingBufferBenchmarks`、`SparseSetBenchmarks`、`EventBusBenchmarks`、`EventBusTypeChainBenchmarks`、`EventBusWarmupComparisonBenchmarks`、`EventBusClearTypeChainCacheBenchmarks`、`EventBusComparisonBenchmarks`、`ListenerRemovalOrderBenchmarks`、`ValueEventBusBenchmarks`、`ValueEventBusInterfaceBenchmarks`、`SafeEventBenchmarks`、`SafeEventRecursiveBenchmarks`、`StringSegmentBenchmarks`。

### 结果归档与 AI 评价

每次运行单独归档到 `artifacts/benchmark-result/<yyyyMMddHHmm>-<commit8>/`（与 `bin`、`obj` 一样不提交）：

```text
artifacts/benchmark-result/202611140357-1a2b3c4d/
├── raw/                # BenchmarkDotNet 原始报告（md / csv / html）与控制台日志
├── probe/              # 需要时：内存探针输出（memory.jsonl）
├── run.json            # 运行元数据：提交、dirty、命令、filter、job、机器
├── ai-evaluation.md    # 本次结果的 AI 评价：按指标优先级分析并给代码级建议
└── ai-comparison.md    # 与基线（默认最新版本基线）逐项比较
```

指标按重要性排序：**内存分配次数与大小 → 分配碎片化 → CPU 时间 → 总体内存占用 → 代码字面分析与建议**。完整规则（目录命名、`run.json` 字段、指标口径与阈值、评价模板、禁止事项）见工作区 skill [ai-skill/performance-test/SKILL.md](ai-skill/performance-test/SKILL.md)。

### 版本基线与回归比较

定版版本的完整结果另存一份到 `JiksLib.Benchmark/ArchivedResult/v<X.Y.Z>/`，**随仓库提交**，作为之后所有运行的默认比较对象：

```text
JiksLib.Benchmark/ArchivedResult/
├── README.md                 # 归档规则与基线清单
└── v2.0.0/
    ├── baseline.json         # 版本、源 run、提交、环境、探针 SHA-256、可比性要求
    ├── run.json              # 源 run 的原始元数据
    ├── raw/                  # BDN 报告与控制台日志
    ├── probe/memory.jsonl    # 探针原始输出
    ├── ai-evaluation.md      # 源 run 的评价（只改标题与路径）
    └── ai-comparison.md      # 归档时的对比（历史记录）
```

当前基线是 **v2.0.0**：源 run `202611251855-791746c6`、提交 `791746c6`，16 个基准类 / 122 个基准方法共 341 个用例 + 3 个探针场景，环境为 i5-12600KF / .NET 10.0.15。新运行默认与**最新版本基线**比较；时间只在同机同环境（CPU / OS / dotnet / job / filter）换算百分比，分配量可跨机器精确比较，碎片率要求 `FootprintProbe.cs` 的 SHA-256 与基线一致。日常运行与基线逐行配对可以用：

```bash
pwsh ai-skill/performance-test/scripts/compare-baseline.ps1 -Top 40
```

基线清单、归档规则与可比性说明见 [ArchivedResult/README.md](JiksLib.Benchmark/ArchivedResult/README.md)，建立新基线的流程见 skill 的 §5。

### 内存探针

BenchmarkDotNet 不提供分配次数与碎片化，碎片率、保留内存由内置探针测量（每个场景独立进程）：

```bash
dotnet run --project JiksLib.Benchmark -c Release -f net10.0 -- probe --list
dotnet run --project JiksLib.Benchmark -c Release -f net10.0 -- probe sparse-set-growth 1
```

支持三个场景：`sparse-set-reserved`（预扩容后填充 100,000 个元素）、`sparse-set-growth`（从默认容量逐个添加 100,000 个元素）、`eventbus-listeners`（注册 10,000 个监听器）。输出为单行 JSON，包含保留字节、碎片字节与碎片率、固定对象数、峰值工作集等字段；实现见 [FootprintProbe.cs](JiksLib.Benchmark/Probes/FootprintProbe.cs)。

## 构建与测试

需要 .NET 10 / .NET 11 SDK。仓库使用 `JiksLib.slnx` 解决方案文件（.NET SDK 的 slnx 格式）：

```bash
dotnet restore ./JiksLib.slnx
dotnet build ./JiksLib.slnx --configuration Release --no-restore

# 空白格式门禁（CI 对三个工程统一执行）
dotnet format whitespace ./JiksLib.slnx --verify-no-changes --no-restore

# 单元测试
dotnet test ./JiksLib.Test/JiksLib.Test.csproj --configuration Release --framework net10.0 --no-build
dotnet test ./JiksLib.Test/JiksLib.Test.csproj --configuration Release --framework net11.0 --no-build
dotnet test ./JiksLib.Test/JiksLib.Test.csproj --configuration Release --framework net48 --no-build   # 仅 Windows

# 用同一套测试验证 netstandard2.1 产物（Unity 2022 目标）；该步骤会按属性重建，不能加 --no-build
dotnet test ./JiksLib.Test/JiksLib.Test.csproj --configuration Release --framework net10.0 \
  -p:JiksLibTargetFramework=netstandard2.1

# 校验可打包
dotnet pack ./JiksLib/JiksLib.csproj --configuration Release --no-build --output ./pkg
```

### CI

`.github/workflows/ci.yml` 在 Windows、Linux、macOS 三平台执行：

| 步骤 | 平台 | 说明 |
| --- | --- | --- |
| restore + build | 三平台 | Release 构建三个工程 |
| format | 三平台 | `dotnet format whitespace ./JiksLib.slnx --verify-no-changes` |
| test `net10.0` / `net11.0` | 三平台 | 采集 XPlat Code Coverage |
| test `net48` | 仅 Windows | .NET Framework 4.8 目标 |
| coverage gate | 仅 Linux | `python3 .github/scripts/check_coverage.py ./TestResults/net10.0 90` |
| netstandard2.1 回归 | 三平台 | 同一套测试跑库的 netstandard2.1 产物 |
| pack | 仅 Linux | 确认包与 README 可正常打包 |
| benchmark smoke | Ubuntu | `--filter "*CacheBenchmarks*" --job Dry`，只验证可跑通，不比较数值 |

覆盖率门禁只读 cobertura 报告的 **`line-rate`**（不看分支覆盖率），阈值 90%；找不到报告或解析失败都直接判失败，避免采集器失效时静默通过。

### 测试布局

`JiksLib.Test` 共 42 个测试文件、42 个 `TestFixture`、1727 个 `[Test]` 与 40 组 `[TestCase]`，使用 NUnit 4.3.2：

| 目录 | 覆盖模块 |
| --- | --- |
| `Collections/` | 14 个集合类型（`ArrayLinkedList`、`BidirectionalDictionary`、`Cache`、`Cell`、`Deque`、`FloatRange`、`IntRange`、`MultiDictionary`、`MultiHashSet`、`OnceCell`、`PriorityQueue`、`ReadOnlyListSegment`、`RingBuffer`、`SparseSet`） |
| `Control/` | `Disposable`、`DisposableStack`、`EventBus`、`FSM`、`LightFSM`、`SafeEvent`、`ServiceCollection`、`UnitType`、`ValueEventBus`，以及监听器容量与发布预热专项 |
| `Extensions/` | `AnythingExtension`、`AsReadOnlyExtension`、`ListExtension`、`StringExtension` |
| `IO/` | `EndianBinaryReader`、`EndianBinaryWriter` |
| `Reflection/` | `ScannableAttribute` |
| `Text/` | `CsvParser`、`CsvReader`、`CsvWriter` |
| 根目录 | `EndianBitConverter`、`Progress`、`ShellUtils`、`StringSegment`（含比较器与 Tokenizer）、回归用例 |

## 仓库结构

| 路径 | 内容 |
| --- | --- |
| `JiksLib/` | 库本体，按命名空间分目录 |
| `JiksLib.Test/` | NUnit 单元测试，含 netstandard2.1 产物的兼容运行 |
| `JiksLib.Benchmark/` | BenchmarkDotNet 基准、内置内存探针，以及入库的版本基线 `ArchivedResult/` |
| `ai-skill/performance-test/` | 性能测试工作区 skill：结果布局、指标口径、运行手册、评价模板与基线对比脚本 |
| `.github/` | CI 工作流与覆盖率门禁脚本 |
| `artifacts/` | 构建输出（`bin`、`obj`、`benchmark-result`），由 `Directory.Build.props` 的 `ArtifactsPath` 指定，不提交 |
| `Directory.Build.props` | 三个工程共享的构建属性：分析器、确定性构建、符号与产物路径 |
| `.editorconfig` | 格式约定与 CA 规则取舍（每条关闭都注明原因） |

## 开发约定

### 代码风格

- 库本体 C# 9、测试与基准 C# 10；`Nullable` 全程启用，`TreatWarningsAsErrors` 对编译器告警生效。
- UTF-8（无 BOM）+ LF；4 空格缩进，`md` / `yaml` / `json` 为 2 空格；文件末尾保留换行。
- 分析器采用 `latest-all`，当前 0 告警。与公开 API 设计、零分配目标或旧运行时兼容性冲突的规则在 `.editorconfig` 中逐条关闭并写明原因；分析器告警不阻断构建，以免 SDK 升级引入新规则时构建意外中断。
- 提交前至少执行 `dotnet format whitespace ./JiksLib.slnx --verify-no-changes` 与三套测试。

### 错误约定

- 参数为空：抛 `ArgumentNullException`，统一走 `ThrowIfNull`。
- 索引越界：与 `string` / 数组保持一致，抛 `IndexOutOfRangeException`。
- 可预期的失败：优先 `Try*` 或 `bool + out`；查询类操作找不到时返回 `null`（例如 `ShellUtils.FindExecutable`）。
- 集合类操作保持"失败不破坏状态"：例如 `OnceCell` 在二次赋值时先抛异常、不写入。
- 监听器异常：由 `Publish` 收集到调用方传入的 `exceptionsOutput`，不从 `Publish` 抛出；`null` 表示静默丢弃。

### 新增能力

- 新的公开成员需要 XML 文档注释（`CS1591` 目前在库工程中关闭，但新增成员按此要求）。
- 宣称零分配或有复杂度保证的实现，需要对应的测试（断言分配量）或基准。
- 新增基准类必须注册到 `JiksLib.Benchmark/Program.cs` 的 `BenchmarkSwitcher`，否则不会进入运行范围。
- 新增探针场景在 `JiksLib.Benchmark/Probes/FootprintProbe.cs` 的 `probe` 子命令中扩展。
- 保持四个目标框架同源可编译；确实需要新运行时能力的成员用 `NETCOREAPP` 条件编译隔离，并在文档中注明。

## 版本 v2.0.0

当前版本：**2.0.0**（`PackageId` 为 `JiksLib`，MIT）。完整的版本记录与迁移说明见 [CHANGELOG.md](CHANGELOG.md)。

| 维度 | 2.0.0 的状态 |
| --- | --- |
| 公开面 | 7 个命名空间、40 余个公开类型 |
| 目标框架 | `netstandard2.1`、`net48`、`net10.0`、`net11.0` 同源构建 |
| 运行时依赖 | 0（`net48` 仅在构建期引入参考程序集包） |
| 测试 | 42 个测试文件、1700 余个测试方法 |
| 覆盖率门禁 | 行覆盖率 ≥ 90%（Linux CI） |
| 代码质量 | `latest-all` 分析器 0 告警、编译器警告视为错误 |
| 性能验证 | 16 个基准类、122 个基准方法、3 个内存探针场景；`v2.0.0` 版本基线（341 行）随仓库提交 |

### 本版固定的行为契约

以下契约在 2.0.0 中固化，从早期版本迁移时值得逐条确认：

1. **`ServiceCollection.Dispose()` 不会让集合失效**：不抛 `ObjectDisposedException`，释放后查找按未注册处理，仍可继续注册服务与追加释放动作，新项在下一次 `Dispose` 释放。
2. **同一服务实例以多个键注册时按注册次数各释放一次**（刻意的行为，不去重）；需要只释放一次时请传 `autoDispose: false` 自行管理。
3. **`DisposableStack` 与 `ServiceCollection` 都可在 `Dispose` 后继续复用**；释放期间 `Add` 会抛 `InvalidOperationException`。
4. **CSV 拆分为两套入口**：`CsvParser` 面向内存 `string` / `StringSegment`（零分配枚举），`CsvReader` 面向 `Stream`（单遍流式、逐字段物化）。
5. **事件总线通过构造函数 `out` 参数返回 `Publisher`**，`Publish` 必须传入 `exceptionsOutput`（可为 `null`）。
6. **事件总线的重入与增删策略按事件类型独立**：处理某类型时增删其他类型的监听器会立即生效；`allowRecursivePublish` 只拒绝同一类型的重入。

### 已知边界

- 事件总线与 `SafeEvent` 没有异步版本；`SafeEvent` 只隔离同步异常，`async void` 监听器在 `await` 之后的异常不在收集范围内。
- 事件监听器是强引用，没有弱引用或自动注销机制，事件源销毁前需要手动移除。
- `CsvReader` 没有异步 API；`CsvWriter` 只写入内存 `StringBuilder`，没有 `Stream` / `TextWriter` 目标。
- `PriorityQueue` 是最小堆；需要大者优先时请传入反向比较器。
- `ArrayLinkedList.TrimExcess()` 会重排槽位，之前拿到的槽位句柄全部失效。
- `SparseSet` 删除时用末元素填补空位，顺序不稳定；`InnerBuffer` 在 `Add` / `EnsureCapacity` 后失效，有效范围只有 `0..Count-1`。
- `RingBuffer` 容量在构造时固定，不可扩容。
- `ValueEventBus` 的发布器上调用 `GetSubPublisher` 后，`PurgeListenerSlot` / `PurgeListenerSlots` 永久禁用。

## 反馈

问题、建议与性能数据欢迎提交到 [GitHub Issues](https://github.com/Seng-Jik/JiksLib/issues)。报告性能问题时，请附上 `run.json` 与对应 run 目录中的 `ai-evaluation.md`，以便复现相同的 job、filter 与机器条件。

## 许可证

[MIT](LICENSE) © 2025 许兴逸 Seng Jik
