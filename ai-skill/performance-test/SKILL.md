---
name: performance-test
description: Run and record JiksLib performance tests (BenchmarkDotNet) and write the AI evaluation plus a comparison against the previous run. Use when asked to benchmark a change, measure allocations/CPU/memory of JiksLib hot paths, review benchmark results, or compare two performance runs.
---

# JiksLib 性能测试规则

性能测试入口是 `JiksLib.Benchmark`：BenchmarkDotNet 0.15.8，每个基准类固定 `[SimpleJob(RuntimeMoniker.Net10_0, invocationCount: 1)]` + `[MemoryDiagnoser]`，目标框架 `net10.0;net11.0;net48`。这个 skill 规定**结果放哪、指标怎么读、AI 必须写什么**。跨 run 的对比只有在目录、元数据、口径都一致时才有意义，所以先遵守格式，再谈结论。

## 一句话

**一次运行 = 一个目录 = 一个代码状态。**
目录名 `<开始时间 yyyyMMddHHmm>-<commit 前 8 位>`，放在 `artifacts/benchmark-result/` 下；目录里必须有 `raw/`、`ai-evaluation.md`、`ai-comparison.md`。

`artifacts/` 与 `bin`、`obj` 同类，`.gitignore` 已忽略 `/artifacts`。**不提交，也不为它加 `.gitignore` 例外。**

## 1. 结果目录

```text
artifacts/benchmark-result/202611140357-1a2b3c4d/
├── raw/                  # 原始输出（报告与日志只读）
│   ├── console.log       # 本次运行的完整控制台输出
│   ├── results/          # BDN 报告：每个基准类型一份 md / csv / html
│   └── ...               # BDN 的运行日志、可重建的构建输出
├── probe/                # 内置探针输出（需要时）：memory.jsonl 与场景说明
├── run.json              # 运行元数据，可比性判断只看这里
├── ai-evaluation.md      # AI 评价：本次结果本身
└── ai-comparison.md      # AI 对比：与上一次运行（可能写"首次基线"）
```

- 时间取**运行开始**的本地时间（`Asia/Shanghai`），24 小时制、补零、**不含分隔符**。
- commit 取 `git rev-parse HEAD` 的**前 8 位**，不用 `--short`，避免位数漂移。
- 同一分钟重复运行同一 commit：目录名追加 `-2`、`-3`，复用旧目录会污染基线。
- 改代码就要新建目录；一个目录里不允许出现两个代码状态的结果。
- `raw/` 里的报告、`console.log` 与日志只读；BDN 生成的工程与构建输出是可重建产物，运行完可以删。
- 失败运行也要建目录、写 `run.json`（`"status": "failed"`）与 `console.log`；`ai-evaluation.md` 只写失败原因，不写指标。

完整命名算法、`run.json` 字段与失败处理见 `references/result-layout.md`。

## 2. 指标优先级

评价与对比都按这个顺序组织，不要按报告表格的列顺序写。

| # | 指标 | 口径 | 来源 |
|---|------|------|------|
| 1 | 内存分配次数与大小 | 每次 `[Benchmark]` 调用的托管分配字节数（`Allocated`），配合 `Gen 0/1/2`（每 1000 次操作的 GC 次数）与源码分配点计数 | BDN 报告 + 源码 |
| 2 | 分配碎片化 | GC 后的 `FragmentedBytes / HeapSizeBytes`、LOH 分配、`PinnedObjectsCount` | 内置 `JiksLib.Benchmark probe`（net48 记 N/A） |
| 3 | CPU 时间 | `Mean` / `Median`（单线程即 CPU 时间），并用 Gen 次数与 GC 暂停判断是否被分配污染 | BDN 报告 |
| 4 | 总体内存占用 | 结构保留字节（`GC.GetTotalMemory(true)` 差值）与进程峰值工作集 | 内置 `probe` / `Process.PeakWorkingSet64` |
| 5 | 代码字面分析与建议 | 分配点、装箱、闭包、扩容与拷贝、缓存局部性；给 `文件:行`、预期收益与风险 | 源码 |

四条口径必须记住：

1. `Allocated` 是**每次 `[Benchmark]` 方法调用**的分配量，不是"每次元素操作"。方法内部循环 N 次时必须换算，并在评价里写明换算依据（更彻底的做法是改用 `OperationsPerInvoke`）。
2. BDN **不提供分配次数，也不提供碎片化**。零分配可由 `Allocated = 0` 直接判定；次数用 `Gen0` 与源码分配点共同说明；碎片化和总体占用用内置 `JiksLib.Benchmark probe` 测（`references/runbook.md` §6），没测就写 `N/A（本次未测）` 和理由。**禁止**用 Gen2 次数冒充碎片化，或用"元素大小 × N"冒充实测。
3. 时间只在**同一台机器、同一 job、同一 filter** 之间比较；跨机器只比分配与定性结论。
4. 碎片率是**进程级**指标（含宿主程序集、JIT 与堆布局）：只有探针实现与宿主完全一致时才能纵向比较；探针改动后旧数据只能作历史参考，不能算百分比。

完整口径、阈值和探针用法见 `references/metrics.md`。

## 3. 运行流程

1. 确认代码已定稿；`git status --porcelain` 记下脏文件，之后写进 `run.json`。
2. 计算目录名，创建 `<run>/raw/`：

```powershell
$root = "artifacts/benchmark-result"
$stamp = Get-Date -Format "yyyyMMddHHmm"
$sha = (git rev-parse HEAD).Substring(0, 8)
$run = "$root/$stamp-$sha"        # 已存在时追加 -2、-3
New-Item -ItemType Directory -Force -Path "$run/raw" | Out-Null
```

3. 运行并捕获控制台输出：

```powershell
$raw = (Resolve-Path "$run/raw").Path
dotnet run --project JiksLib.Benchmark/JiksLib.Benchmark.csproj -c Release -f net10.0 -- `
  --filter '*' --allStats --artifacts $raw 2>&1 |
  Tee-Object -FilePath "$run/raw/console.log"
```

4. 检查 `raw/results/` 有报告文件；没有就是失败，按失败处理，不要凭控制台文字编指标。
5. 需要碎片化/保留内存时跑内置探针（每个场景独立进程，3 次）：

```powershell
$probe = "$run/probe"
New-Item -ItemType Directory -Force -Path $probe | Out-Null
dotnet build JiksLib.Benchmark/JiksLib.Benchmark.csproj -c Release -f net10.0 | Out-Null
& { foreach ($s in 'sparse-set-reserved','sparse-set-growth','eventbus-listeners') {
      foreach ($t in 1,2,3) {
        dotnet run --no-build --project JiksLib.Benchmark/JiksLib.Benchmark.csproj -c Release -f net10.0 -- probe $s $t
      }
    } } | Set-Content "$probe/memory.jsonl"
```

6. 写 `run.json`：commit、dirty、命令、filter、job、机器、dotnet 版本、是否跑了探针。
7. 读 `raw/results/`、`probe/memory.jsonl` 与对应源码（`JiksLib/...`、`JiksLib.Benchmark/...`），写 `ai-evaluation.md`（模板 `templates/ai-evaluation.md`）。
8. 找上一次运行写 `ai-comparison.md`（模板 `templates/ai-comparison.md`）。
9. 对照 §7 检查清单逐条确认。

`--list flat` 可以先列出真正会跑的基准。新增基准类**必须**注册到 `JiksLib.Benchmark/Program.cs` 的 `BenchmarkSwitcher`，否则不在运行范围；探针场景在 `JiksLib.Benchmark/Probes/FootprintProbe.cs`（`probe` 子命令）里扩展。命令细节、过滤写法、环境要求见 `references/runbook.md`。

## 4. 上一次运行怎么定

只看目录名里的 12 位时间戳，不看 commit，也不看文件修改时间：

```powershell
$root = "artifacts/benchmark-result"
$current = "202611140357-1a2b3c4d"
$prev = Get-ChildItem $root -Directory |
  Where-Object { $_.Name -match '^\d{12}-[0-9a-f]{8}(-\d+)?$' -and $_.Name -ne $current } |
  Where-Object { Test-Path (Join-Path $_.FullName 'raw/results') } |
  Sort-Object @{ Expression = { [datetime]::ParseExact($_.Name.Substring(0, 12), 'yyyyMMddHHmm', $null) } },
              @{ Expression = { $_.Name } } -Descending |
  Select-Object -First 1
$prev.Name
```

- 跳过没有 `raw/results` 的失败运行，并在对比里写明跳过了谁。
- 时间戳相同按目录名排序，`-2`、`-3` 视为更晚。
- 上一次来自其他 commit、机器或 filter 时照比，但必须在"可比性检查"里标明，并且只比两边都有的基准；缺失项写"仅本次运行"或"仅上次运行"，不要当 0。
- `artifacts/` 不入库，所以历史只有本地这一份。删 `artifacts`、`git clean -xdf` 之前先确认基线要不要备份到仓库外。

## 5. 评价与对比怎么写

- `ai-evaluation.md`：本次结果本身。按指标优先级组织，每个结论都要能在报告表格里指到具体行；代码建议必须指到 `文件:行`；实测与推断分开标注。
- `ai-comparison.md`：与上一次运行逐项对比。先给判定（改善 / 退化 / 无明显变化 / 不可比），再给证据。分配量任何增加都算退化；时间按 `references/metrics.md` 的阈值：`< 10%` 或 Mean/Median 不同向写"噪声内"，`≥ 25%` 且两者同向才判改善/退化，中间区间写"需复跑确认"；不同 job / 参数 / TFM / 机器之间不算百分比。
- 两个文件都从 `templates/` 复制，模板里的说明注释不要留在成品里。
- 没有数据的地方写 `N/A` 和原因。**不编造数字，不把推断写成测量结论。**

## 6. 禁止

- 修改 `raw/` 里的报告、`console.log` 或日志。
- 在同一个目录里跑两个代码状态；改代码后必须新建目录。
- 把两次不同 job / filter / TFM / 机器的时间差算成百分比并下结论。
- 用 `Job.Dry`、`--job short`、临时 `--iterationCount` 跑"正式结果"；确需快速试探时，在 `run.json` 写 `"comparable": false`，且不作为下一次的基线。
- 手写、改写或"修正"报告里的数字。
- 提交 `artifacts/`，或为它加 `.gitignore` 例外。
- 只贴表格不给结论，或只给"可以优化"这类没有位置、没有预期收益的建议。
- 为了凑齐五类指标而编造碎片化、内存占用结论。

## 7. 完成前检查

- [ ] 目录名 = 开始时间 + 8 位 commit，位置在 `artifacts/benchmark-result/`
- [ ] `raw/results/` 非空，`raw/console.log` 在，`raw/` 没有被改过
- [ ] `run.json` 含 commit、dirty、command、filter、job、dotnet 版本
- [ ] 需要碎片化/保留内存时，`probe/memory.jsonl` 存在且写明了场景、N、重复次数
- [ ] `ai-evaluation.md` 按 1→5 覆盖指标，缺的写 N/A 和原因
- [ ] `ai-comparison.md` 写明基线目录名、判定与可比性
- [ ] 碎片/保留数字只与同一探针宿主比较，没有跨探针版本算百分比
- [ ] 结论里的每个数字都能在 `raw/results/` 或 `run.json` 里找到
- [ ] `git status` 里没有 `artifacts/`

## 参考

- `references/result-layout.md` — 目录命名、`run.json` 字段、失败运行与历史清理
- `references/metrics.md` — 指标口径、判定阈值、碎片化与内存探针
- `references/runbook.md` — 命令、过滤、环境要求、跨平台写法、新增基准
- `templates/run.json`、`templates/ai-evaluation.md`、`templates/ai-comparison.md`
