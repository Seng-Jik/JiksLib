# 指标口径与判定

## 1. 报告怎么读

每次运行，BDN 为每个基准类型生成一份报告（CSV / HTML / Markdown）。一行 = 一个方法 + 一组 `[Params]` + 一个 job。

| 列 | 含义 | 用于 |
| --- | --- | --- |
| `Method` | 基准方法名 | 配对键 |
| `Job` | 运行配置（如 `Net10_0`） | 可比性 |
| `Params` | `[Params]` 参数值 | 配对键 |
| `Mean` | 每次操作的平均耗时 | 指标 3 |
| `Median` | 中位数；有离群值时优先于 Mean（默认不一定出现，`--allStats` 才固定有） | 指标 3 |
| `Error` | 99.9% 置信区间半宽 | 判断差异是否超出噪声 |
| `StdDev` | 标准差 | 同上 |
| `StdErr` | 均值的标准误（`--allStats` 才有） | 同上 |
| `Gen 0` / `Gen 1` / `Gen 2`（也可能写作 `Gen0`…） | **每 1000 次操作**触发的 GC 次数 | 指标 1 的分配压力 |
| `Allocated` | **每次操作**的托管分配字节数 | 指标 1 |

三个容易踩的事实：

- `[MemoryDiagnoser]` 会**另起一次运行**来测分配，所以报告里的时间和分配量来自不同的测量轮次，不要假设它们严格同一时刻、也不要用"时间涨了所以分配也涨了"这类因果推断。
- BDN 默认在每次迭代前强制 GC（`GcForce`），所以报告衡量的是**单次操作**的成本，不含长期堆增长。想看保留内存必须另做探针（见 §7）。
- Gen 列只在回收次数 > 0 时出现：`MemoryDiagnoser` 对 0 值隐藏整列，报告里没有 Gen 列不代表没测，而是本次没有触发回收。原始计数在 `console.log` 的 `// GC:` 行（`Gen0 Gen1 Gen2 分配字节数 操作数`），可直接核对每次调用的分配量。

## 2. 指标 1：分配次数与大小

**大小**：直接读 `Allocated`（B/op）。

**次数**：BDN 不提供。用三条证据合起来说：

1. `Allocated = 0` → 该路径零分配，次数为 0。这是硬结论，优先写。
2. `Gen0/1000 ops` 反映分配压力：同样的 `Allocated` 而 `Gen0` 上升，说明分配更碎或存活更久（晋升）；`Gen2` 上升通常意味着对象活过了 Gen0/Gen1 或进了 LOH。
3. 源码分配点清单：在 `ai-evaluation.md` 里逐个列 `[Benchmark]` 路径上的分配点（`new`、装箱、闭包、`params`、字符串、LINQ、迭代器、`ToArray`/`ToList`、异常），标注"每次调用执行几次"，用对象大小与 `Allocated` 对账（x64 上装箱一个 `int` 是 24 B；一个 `T[]` 是 24 + 元素数 × 元素大小，按 8 字节对齐）。对得上，次数才可信；对不上说明漏了分配点。

常见换算陷阱：

- `[Benchmark]` 方法内部循环 N 次时，`Allocated` 是 N 次操作的总量，要除以 N 才能和"每次调用"比较，并写明换算依据。长期做法是给方法加 `OperationsPerInvoke`（见 `references/runbook.md`）。
- `[GlobalSetup]` / `[IterationSetup]` 里的分配不计入 `Allocated`，但如果 setup 后对象被复用，热路径的分配可能被掩盖；评价时要检查 setup 是否替被测代码"预热"了容量。
- 分配字节数是确定性指标：同一代码路径的显示值应当一致。如果同一 commit 的两次运行也不一致，先怀疑基准不稳定（setup 泄漏、`invocationCount` 太小、BDN 精度约 ±0.5%），再谈代码差异。

## 3. 指标 2：碎片化

BDN 完全没有碎片化数据。可用口径：

| 量 | 取法 | 说明 |
| --- | --- | --- |
| `FragmentedBytes` | `GC.GetGCMemoryInfo()` | 上一次 GC 时的总碎片字节 |
| `HeapSizeBytes` | 同上 | 托管堆大小 |
| 碎片率 | `FragmentedBytes / HeapSizeBytes` | 跨进程比绝对值无意义，比值更稳定 |
| LOH 碎片 | 用 `compacting: false` 的强制 GC 后读 `FragmentedBytes` | 默认的 compacting GC 会压掉 SOH 碎片，剩下的大头是 LOH |
| `PinnedObjectsCount` | 同上 | 固定对象多会加剧碎片 |
| LOH 分配 | 由 `Allocated` 与单次分配大小推断（≥ 85 000 字节即 LOH） | 只能作为"碎片风险"证据，不能当碎片结论 |

规则：

- 只有跑了探针（§7）才能写碎片化数字；没跑就写 `N/A（本次未测碎片）` 并说明为什么这次不需要测。
- 探针输出必须放在 `<run>/probe/`，随 run 归档。
- **禁止**用 Gen2 次数或"分配量大"推断碎片化结论；它们只能作为风险信号，且必须标"推断"。
- net48 没有 `GCMemoryInfo`：job 改成 net48 时本项直接 `N/A`。

## 4. 指标 3：CPU 时间

- 单线程基准里 `Mean` ≈ CPU 时间；有 GC 或线程池参与时，`Mean` 是墙钟时间，包含 GC 暂停与其他线程占用。判断方法：看同一行的 `Gen0/1/2`，分配多的行时间更容易被 GC 污染。
- 有离群值时以 `Median` 为准；`Mean` 与 `Median` 方向不一致时不要下结论。
- `Error` 是 99.9% 置信区间的半宽：两次运行的 `Mean` 之差小于两边 `Error` 之和时，差异基本在噪声内。
- 要更接近"纯 CPU 时间"，可以在探针里用 `Process.GetCurrentProcess().TotalProcessorTime` 包住固定次数的调用再除以次数；它包含 GC 线程等所有线程的 CPU，与 BDN 的 `Mean` 不是同一口径，不能混着比较。
- `ThreadingDiagnoser`（`Lock Contentions`、`Completed Work Items`）只在需要判断锁竞争时加，且只在 .NET Core 3.0+ 可用。
- 跨 run 的时间比较用 §6 的阈值；跨机器不比较时间。
- BDN 的 `--statisticalTest` 只在**同一次运行内**比较多个 job，不能用来比较两次独立运行。

## 5. 指标 4：总体内存占用

两个不同的量，不要混：

| 量 | 口径 | 取法 |
| --- | --- | --- |
| 结构保留内存 | 构造并持有结构后，进程托管堆相对基线的增长 | `GC.GetTotalMemory(true)` 差值（§7） |
| 每元素开销 | 保留内存 ÷ 元素数 | 同上，用于和 `sizeof(T)` / 理论值对比 |
| 进程峰值工作集 | 进程整个生命周期的峰值 | `Process.GetCurrentProcess().PeakWorkingSet64`（Windows 有效） |

规则：

- BDN 的 `Allocated` 不能当"总体内存占用"：它不含长期保留的堆，也不含非托管内存。
- 保留内存受探测程序自身影响（JIT、字符串、字典），只和同样探针的另一次运行比，不和 BDN 报告比。
- 需要至少 3 次重复，取中位数；波动超过 10% 时写"测量不稳"，不要硬下结论。
- 没测就写 `N/A` 和原因。

## 6. 判定阈值（默认值）

| 指标 | 比较方式 | 判定 |
| --- | --- | --- |
| `Allocated` | 精确值 | 增加 = 退化；减少 = 改善；相等 = 无变化。若同一 commit 两次运行不同，先按基准不稳定处理 |
| `Gen0/1/2`（每 1000 ops） | 精确值 | 增加且 `Allocated` 同向 = 退化；单独 `Gen2` 上升 = 晋升/LOH 风险，需探针确认 |
| `Mean` / `Median` | 相对差 | `< 10%` = 噪声内；`10%–25%` = 需复跑确认；`≥ 25%` 且两者同向 = 退化/改善 |
| 碎片率 | 百分点差 | 上升 `≥ 1` 个百分点，或 `FragmentedBytes` 增加 `≥ 1 MiB` = 需关注 |
| 保留内存 | 相对差 | 增加 `≥ 5%` 且 3 次重复同向 = 退化 |

补充：

- 阈值是默认值。偏离时必须写明理由（例如"本次只跑了单个参数，样本不足"）。
- 分配量是确定性指标，任何增加都要解释；时间差异在阈值内就老实写"无显著差异"，不要为了有结论而拔高。
- 判定只在两边 job、TFM、参数、机器一致时才下；否则一律"不可比"并写清哪里不一致。

## 7. 探针（碎片化与总体占用）

BDN 不测这两项。探针已固化在 `JiksLib.Benchmark` 里：`JiksLib.Benchmark/Probes/FootprintProbe.cs`，通过 `probe` 子命令调用，**不要每次再写临时工程**。

| 场景 | 测什么 | 规模 |
| --- | --- | --- |
| `sparse-set-reserved` | `SparseSet<int>` 预扩容后填入：保留内存、零扩容分配、碎片 | 100,000 |
| `sparse-set-growth` | 默认容量逐个 `Add`：扩容的累计分配、保留内存、LOH 碎片 | 100,000 |
| `eventbus-listeners` | `EventBus<T>` 注册监听器：委托数组的保留内存与扩容分配 | 10,000 |

调用（每个进程只测一次，3 次 = 3 个进程）：

```powershell
dotnet build JiksLib.Benchmark/JiksLib.Benchmark.csproj -c Release -f net10.0
dotnet run --no-build --project JiksLib.Benchmark/JiksLib.Benchmark.csproj -c Release -f net10.0 -- probe sparse-set-growth 1
```

每个进程输出一行 JSON：`scenario / trial / n / retainedBytes / retainedPerItem / allocatedBytes / allocatedPerItem / heapSizeBytes / fragmentedBytes / fragmentedPercent / pinnedObjects / peakWorkingSetBytes / runtime / at`。`probe --list` 列出场景，`probe`（无参数）打印用法。

口径：

- 保留内存 = 构造并持有结构后，`GC.GetTotalMemory(true)` 相对基线的增长；累计分配 = `GC.GetTotalAllocatedBytes(precise: true)` 差值。
- 碎片 = 强制**非压缩**全量 GC 后 `GC.GetGCMemoryInfo().FragmentedBytes / HeapSizeBytes`。压缩 GC 会抹掉 SOH 碎片、只剩 LOH 碎片；非压缩才能看到完整碎片。`GetGCMemoryInfo()` 返回上一次 GC 的信息，所以读之前必须强制一次 GC。
- 同一场景跑 3 个独立进程，取中位数；同一进程里连测多个场景会互相污染（堆、LOH、JIT），不要那样做。
- **碎片率是进程级指标**：宿主程序集、JIT、堆布局都会影响它。探针实现或宿主一变，旧数据只能作历史参考，不能算百分比（实测：同一逻辑从临时工程搬进基准项目后，`sparse-set-growth` 从 43.93% 变成 32.64%，而保留/分配逐字节不变）。
- 输出写到 `<run>/probe/memory.jsonl`（一行一条），并在 `ai-evaluation.md` 里写明场景、N、重复次数、探针版本。

扩展场景：在 `FootprintProbe.ScenarioNames` 里加名字，在 `Measure` 的 `switch` 里加构造分支（保持"一次只构造一个场景"），必要时加规模常量。场景只应依赖 JiksLib，不要写业务代码。

## 8. 指标 5：代码字面分析

按优先级找问题，每条都要落到位置和预期影响：

1. **分配点**：`new`、装箱（值类型进 `object`/接口/非泛型集合/`string.Format`）、lambda 捕获、`params` 数组、字符串拼接与插值、LINQ、`yield` 迭代器、`ToArray`/`ToList`、`Enum.GetValues`、异常路径。
2. **扩容**：数组/列表/哈希表的增长策略、初始容量、`SparseSet` 稀疏数组的增长与复用；能否预留容量或复用缓冲区。
3. **复制**：`Array.Copy`、切片、防御性拷贝、`ArraySegment` 是否避免了真实复制。
4. **查找与分支**：哈希冲突、线性扫描、分支预测、虚调用/委托调用、接口调用是否必要。
5. **缓存局部性**：结构体 vs 类、数组 vs 链表、SoA vs AoS、枚举器是否结构体（`foreach` 走 `IEnumerable<T>` 会装箱）。
6. **与库的取向对齐**：README 的"少分配"是设计目标；热路径出现分配要能解释，否则就是缺陷。

每条建议写成：

```text
位置：JiksLib/Collections/Xxx.cs:123
现状：每次调用分配一个 24 B 的装箱对象
建议：改成泛型约束 + 结构体枚举器
预期：Allocated 24 B/op → 0；CPU 预计 -3% 左右（推断）
风险：公开 API 签名变化，需要 Major 版本
```

## 9. 已知限制

- BDN 的分配测量精度约 ±0.5%（官方说明 99.5% 准确），分配次数不可直接获取。
- `Gen 0/1/2` 是每 1000 次操作的次数，不是每次操作的次数；小分配量时会出现 0 或小数；全 0 时整列不会出现在报告里（见 §1）。
- 时间噪声随机器负载变化；默认 job 的 `invocationCount: 1` 使时间样本比默认配置更抖，跨 run 的时间结论要保守。
- `GCMemoryInfo` 仅 .NET Core 3.0+；net48 只能写 N/A。
- 碎片与保留内存是进程级数据，包含探针与运行时自身的分配；只做同探针的纵向比较。
