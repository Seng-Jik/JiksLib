# 运行手册

## 1. 前置检查

- 仓库根目录执行；`dotnet --version` 应为 .NET 10 SDK（job 固定 `RuntimeMoniker.Net10_0`）。
- 插电、高性能电源计划；关掉浏览器、编译、杀毒扫描等重负载。时间对比只在同一台机器上做。
- 不要在共享 CI、虚拟机、云主机上产出要对比的时间数据；这些环境只适合看分配量。
- 首次运行包含 restore/build，耗时长但不影响测量值；不要在运行中途改代码。

## 2. 先看会跑什么

```powershell
dotnet run --project JiksLib.Benchmark/JiksLib.Benchmark.csproj -c Release -f net10.0 -- --list flat
```

打印的是 `BenchmarkSwitcher` 里注册过的全部基准（`JiksLib.Benchmark/Program.cs`）。新写的基准类**不加进 `Program.cs` 的这个数组就不会运行**，也不会出现在报告里。

## 3. 运行

完整流程见 `SKILL.md` §3，这里只列命令要点。

| 目的 | `--filter` 写法 |
| --- | --- |
| 全部 | `--filter '*'` |
| 单个类 | `--filter '*SparseSetBenchmarks*'` |
| 单个方法 | `--filter '*EventBusBenchmarks.PublishEvent_WithListeners*'` |
| 多个 | `--filter '*EventBus*' '*SparseSet*'`（多个模式是"或"） |

- `[Params]` 由代码里的 `[Params]` 决定。`--filter` 主要按类型/方法名匹配，不保证能精确选中某个参数值；需要只跑一组参数时直接改 `[Params]`，并在 `run.json` 的 `notes` 注明"参数集变更"。
- **不要加 `-m` / `--memory`**：类上已有 `[MemoryDiagnoser]`。
- **始终加 `--allStats`**：默认统计列是 Mean / Error / StdDev（Median 只在特定条件下才出现，不保证有）；`--allStats` 固定加入 Min / Q1 / Median / Q3 / Max，不改变测量。
- **不要加 `--join`**：每类一份报告才方便按类配对。
- **不要用 `--job` 改变迭代配置**：它会引入与 `[SimpleJob]` 不同的 job 行（或改写其设置），结果不能与默认 run 直接比较。仅开发调试时可用 `--job short`，此时 `run.json` 必须写 `"comparable": false`，且该 run 不能作为基线。
- `--artifacts` 必须指向当前 run 的 `raw` 目录的绝对路径。忘了传时 BDN 会在**当前目录**创建 `BenchmarkDotNet.Artifacts`，这是违规产物：删掉并重跑。
- 可选的 `--wakeLock System` 防止运行中睡眠。

## 4. 捕获输出与退出码

PowerShell（推荐，Windows 下实际使用）：

```powershell
$run = "artifacts/benchmark-result/202611140357-1a2b3c4d"
$raw = (Resolve-Path "$run/raw").Path

dotnet run --project JiksLib.Benchmark/JiksLib.Benchmark.csproj -c Release -f net10.0 -- `
  --filter '*' --allStats --artifacts $raw 2>&1 |
  Tee-Object -FilePath "$run/raw/console.log"

"exit=$LASTEXITCODE"
```

bash（WSL / Linux / macOS）：

```bash
run=artifacts/benchmark-result/202611140357-1a2b3c4d
mkdir -p "$run/raw"
dotnet run --project JiksLib.Benchmark/JiksLib.Benchmark.csproj -c Release -f net10.0 -- \
  --filter '*' --allStats --artifacts "$PWD/$run/raw" 2>&1 | tee "$run/raw/console.log"
echo "exit=${PIPESTATUS[0]}"
```

PowerShell 的 `$LASTEXITCODE` 反映 `dotnet` 的退出码；bash 必须用 `PIPESTATUS[0]`，`$?` 是 `tee` 的退出码。非零退出按失败处理（`references/result-layout.md` §3）。

## 5. 填 `run.json` 用的命令

```powershell
git rev-parse HEAD
git rev-parse --abbrev-ref HEAD
git log -1 --format=%s
git status --porcelain
dotnet --version
dotnet run --project JiksLib.Benchmark/JiksLib.Benchmark.csproj -c Release -f net10.0 -- --info
```

`--info` 只打印 BDN 的环境信息（OS、CPU、.NET 版本）然后退出，适合抄进 `environment`，不会跑基准。

## 6. 内存/碎片探针

碎片化与保留内存由 `JiksLib.Benchmark` 内置的 `probe` 子命令测量（实现：`JiksLib.Benchmark/Probes/FootprintProbe.cs`）。每个进程只测一个场景，所以每个场景跑 3 个进程取中位数：

```powershell
$probe = "artifacts/benchmark-result/<run>/probe"
New-Item -ItemType Directory -Force -Path $probe | Out-Null
$proj = "JiksLib.Benchmark/JiksLib.Benchmark.csproj"

dotnet build $proj -c Release -f net10.0
if ($LASTEXITCODE -ne 0) { throw "build failed" }

& { foreach ($s in "sparse-set-reserved","sparse-set-growth","eventbus-listeners") {
      foreach ($t in 1,2,3) {
        dotnet run --no-build --project $proj -c Release -f net10.0 -- probe $s $t
      }
    } } | Set-Content "$probe/memory.jsonl"
```

- `probe --list` 列出场景；`probe` 无参数打印用法。
- 必须先 `dotnet build` 再 `--no-build`，否则 MSBuild 的输出会混进 `memory.jsonl`。
- 不要在同一个进程里连测多个场景：堆、LOH 与 JIT 状态会互相污染，保留内存与碎片都不再可比。
- 探针需要 .NET 5+（`GC.GetGCMemoryInfo` / `GC.GetTotalAllocatedBytes`）；net48 目标会返回错误码 3。
- 新增场景：改 `FootprintProbe.ScenarioNames` 与 `Measure` 的 `switch`，场景只依赖 JiksLib。

## 7. 新增或修改基准

```csharp
[SimpleJob(RuntimeMoniker.Net10_0, invocationCount: 1)]
[MemoryDiagnoser]
public class XxxBenchmarks
{
    [Params(10, 100, 1000)]
    public int N { get; set; }

    [GlobalSetup]
    public void GlobalSetup() { /* 只做准备，不在 [Benchmark] 里做 warmup */ }

    [Benchmark]
    public void DoWork() { /* 被测路径 */ }
}
```

- 放在 `JiksLib.Benchmark/Collections/` 或 `JiksLib.Benchmark/Control/`，一个类一个文件。
- **必须保留 `[MemoryDiagnoser]`**，否则该类的分配指标全部缺失。
- **必须注册到 `Program.cs` 的 `BenchmarkSwitcher` 数组**。
- 方法内部循环 N 次时：N 是编译期常量才可加 `[Benchmark(OperationsPerInvoke = N)]` 让报告按 N 次归一；N 随 `[Params]` 变化时该特性无法使用（只接受常量），要在 XML 注释里说明并在评价里除以 N。
- `[Params]` 代表真实使用规模；改 `[Params]` 等于改测量口径，要在 `run.json` 的 `notes` 写明"参数集变更"。
- 不要在 `[Benchmark]` 里打印、写文件、分配日志字符串——这些都会进 `Allocated`。
- 需要保留状态时用 `[IterationSetup]` / `[IterationCleanup]`，不要靠字段累积（会触发 OOM 提示并污染测量）。

## 8. 故障处理

| 现象 | 处理 |
| --- | --- |
| `No benchmarks to run` | filter 拼错，或类没注册进 `Program.cs` |
| 编译错误 | 看 `raw/console.log`；修好后重跑，不要写评价 |
| `OutOfMemoryException!` 提示 | 基准有状态泄漏：把构造放进 `[IterationSetup]`，或减少 `[Params]` 规模 |
| 报告里出现 `-` | 该行数据不可用（诊断器不适用、被跳过、样本不足），照实写 N/A |
| 仓库根出现 `BenchmarkDotNet.Artifacts` | 忘了 `--artifacts`，删掉重跑 |
| 同一目录重复运行 | 报告被覆盖，前后结果混在一起：删掉该目录，重新建目录跑 |
| `probe` 返回 3 | 用 net48 目标运行了探针；改用 `-f net10.0` |

## 9. 运行后收尾

1. 确认 `raw/results/` 有报告、`raw/console.log` 完整。
2. 需要碎片/保留内存时，确认 `probe/memory.jsonl` 有 3 场景 × 3 次的记录。
3. 可以删除 `raw/` 下 BDN 生成的临时工程与构建输出（可重建），在 `run.json` 的 `pruned` 记录；`results/` 与日志保留。
4. 写 `run.json`（含是否跑探针）、`ai-evaluation.md`、`ai-comparison.md`。
5. 跑一遍 `SKILL.md` §7 的检查清单。
