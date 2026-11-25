---
name: performance-test
description: Run and record JiksLib performance tests (BenchmarkDotNet), write the AI evaluation, and compare the run against the newest version baseline in JiksLib.Benchmark/ArchivedResult (falling back to the previous run). Also covers archiving a released version as a committed baseline. Use when asked to benchmark a change, measure allocations/CPU/memory of JiksLib hot paths, review benchmark results, compare two performance runs, or store a version baseline.
---

# JiksLib 性能测试规则

性能测试入口是 `JiksLib.Benchmark`：BenchmarkDotNet 0.15.8，每个基准类固定 `[SimpleJob(RuntimeMoniker.Net10_0, invocationCount: 1)]` + `[MemoryDiagnoser]`，目标框架 `net10.0;net11.0;net48`。这个 skill 规定**结果放哪、指标怎么读、AI 必须写什么、基线怎么建**。跨 run 的对比只有在目录、元数据、口径都一致时才有意义，所以先遵守格式，再谈结论。

## 一句话

**一次运行 = 一个目录 = 一个代码状态；一次定版 = 一个版本基线目录。**

- 日常运行：目录名 `<开始时间 yyyyMMddHHmm>-<commit 前 8 位>`，放在 `artifacts/benchmark-result/` 下；目录里必须有 `raw/`、`ai-evaluation.md`、`ai-comparison.md`。
- 版本基线：目录名 `v<X.Y.Z>`，放在 `JiksLib.Benchmark/ArchivedResult/` 下，**提交进仓库**，是后续所有运行的默认比较对象。

`artifacts/` 与 `bin`、`obj` 同类，`.gitignore` 已忽略 `/artifacts`，**不提交**；`ArchivedResult/` 是唯一入库存放性能数据的地方，**必须提交**。

## 1. 两类目录

### 1.1 日常运行（不提交）

```text
artifacts/benchmark-result/202611140357-1a2b3c4d/
├── raw/                  # 原始输出（报告与日志只读）
│   ├── console.log       # 本次运行的完整控制台输出
│   ├── results/          # BDN 报告：每个基准类型一份 md / csv / html
│   └── ...               # BDN 的运行日志、可重建的构建输出
├── probe/                # 内置探针输出（需要时）：memory.jsonl 与场景说明
├── run.json              # 运行元数据，可比性判断只看这里
├── ai-evaluation.md      # AI 评价：本次结果本身
└── ai-comparison.md      # AI 对比：与基线（见 §4）
```

- 时间取**运行开始**的本地时间（`Asia/Shanghai`），24 小时制、补零、**不含分隔符**。
- commit 取 `git rev-parse HEAD` 的**前 8 位**，不用 `--short`，避免位数漂移。
- 同一分钟重复运行同一 commit：目录名追加 `-2`、`-3`，复用旧目录会污染基线。
- 改代码就要新建目录；一个目录里不允许出现两个代码状态的结果。
- `raw/` 里的报告、`console.log` 与日志只读；BDN 生成的工程与构建输出是可重建产物，运行完可以删。
- 失败运行也要建目录、写 `run.json`（`"status": "failed"`）与 `console.log`；`ai-evaluation.md` 只写失败原因，不写指标。

完整命名算法、`run.json` 字段与失败处理见 `references/result-layout.md`。

### 1.2 版本基线（提交）

```text
JiksLib.Benchmark/ArchivedResult/
├── README.md                 # 归档规则与基线清单
└── v2.0.0/
    ├── baseline.json         # 基线元数据（版本来源、口径、环境、可比性要求）
    ├── run.json              # 源 run 的原始元数据
    ├── raw/                  # 源 run 的原始报告与日志
    ├── probe/memory.jsonl    # 源 run 的探针输出
    ├── ai-evaluation.md      # 源 run 的评价（只改标题与路径，数字不动）
    └── ai-comparison.md      # 归档时的对比（历史记录）
```

- 版本基线从日常运行**完整复制**，`raw/`、`probe/`、`run.json` 一个字节都不改；`ai-evaluation.md` / `ai-comparison.md` 只允许改标题、路径与归档说明。
- 一个版本一个目录，**已发布的不覆盖、不删除**；后续版本新建目录。
- 目录里必须有 `baseline.json`；基线选择只读它，不看目录修改时间。
- `*.log` 默认被 `.gitignore` 忽略，归档目录用 `!/JiksLib.Benchmark/ArchivedResult/**/*.log` 例外保留运行日志（测量证据的一部分）。
- 何时、怎么建基线见 §5；`baseline.json` 字段见 `references/result-layout.md` §5。

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
8. 按 §4 选基线，用 §6 的方法写 `ai-comparison.md`（模板 `templates/ai-comparison.md`）；全量逐行配对可以先用 `scripts/compare-baseline.ps1` 算表，再人工解释。
9. 对照 §8 检查清单逐条确认。

`--list flat` 可以先列出真正会跑的基准。新增基准类**必须**注册到 `JiksLib.Benchmark/Program.cs` 的 `BenchmarkSwitcher`，否则不在运行范围；探针场景在 `JiksLib.Benchmark/Probes/FootprintProbe.cs`（`probe` 子命令）里扩展。命令细节、过滤写法、环境要求见 `references/runbook.md`。

## 4. 基线怎么定

比较对象统一叫“基线”。按下面的优先级选，选完把**类型和名字**写进 `ai-comparison.md` 第一行。

1. **最新版本基线（默认）**：`JiksLib.Benchmark/ArchivedResult/` 下 `v<X.Y.Z>/baseline.json` 中语义版本最大的一个。**只要存在版本基线，就与它比较**，即使本地 `artifacts/` 里还有更晚的运行。
2. **上一次成功运行（回退）**：没有版本基线时（新仓库、或基线目录还没建），取 `artifacts/benchmark-result` 中按时间戳排序、排除本次、`raw/results` 非空的最近一次。
3. **同版本上一次运行（可选补充）**：主基线不是本次代码的直接前身、且本地存在更晚的同版本成功运行时，可以在对比里补一行“短期波动”，但判定仍以主基线为准。

```powershell
# 最新版本基线：按 baseline.json 的 version 排序，不看目录修改时间
$archive = "JiksLib.Benchmark/ArchivedResult"
Get-ChildItem $archive -Directory |
  Where-Object { $_.Name -match '^v\d+\.\d+\.\d+$' -and (Test-Path (Join-Path $_.FullName 'baseline.json')) } |
  ForEach-Object {
    $meta = Get-Content (Join-Path $_.FullName 'baseline.json') -Raw | ConvertFrom-Json
    [pscustomobject]@{ Name = $_.Name; Version = [version]$meta.version; ArchivedAt = [datetimeoffset]$meta.archivedAt }
  } |
  Sort-Object Version, ArchivedAt -Descending |
  Select-Object -First 1
```

回退到上一次成功运行（仅在没有任何版本基线时使用）：

```powershell
$root = "artifacts/benchmark-result"
$current = "202611140357-1a2b3c4d"
Get-ChildItem $root -Directory |
  Where-Object { $_.Name -match '^\d{12}-[0-9a-f]{8}(-\d+)?$' -and $_.Name -ne $current } |
  Where-Object { Test-Path (Join-Path $_.FullName 'raw/results') } |
  Sort-Object @{ Expression = { $_.Name.Substring(0, 12) } }, @{ Expression = { $_.Name } } -Descending |
  Select-Object -First 1
```

- 跳过没有 `raw/results` 的失败运行，并在对比里写明跳过了谁。
- 时间戳相同按目录名排序，`-2`、`-3` 视为更晚。
- **版本基线与本次 run 的版本关系要写明**：同版本 = 回归；本次版本更高 = 版本间比较（写 `git log <基线 commit>..HEAD`）；本次版本更低 = 逆版本比较，只作参考，不下退化结论。
- 基线可能来自另一台机器 / 另一个 dotnet：分配量照比；时间只在 `baseline.json.environment` 与 `run.json.environment` 的 CPU / OS / dotnet 一致时才换算百分比，否则写“不可比”。
- 基线的基准集合可能和本次不同（升级版本常新增基准）：只比交集，新增/缺失单列，不要当 0。
- `artifacts/` 不入库，本地历史可能被 `git clean -xdf` 清掉；版本基线在仓库里，不受影响。

## 5. 建版本基线

触发时机：`JiksLib/JiksLib.csproj` 的 `<Version>` 定版（发版 / 打 tag 前），或仓库第一次建立基线。一个版本只建一次。

1. **选 run**：必须同时满足 `status: success`、`comparable: true`、`filter: "*"`、`probe: builtin`、`raw/results` 非空，且是该版本代码状态下**最近的一次**完整运行。没有这样的 run 就补跑一次全量 + 探针，不要用局部 filter 的结果凑基线。
2. **复制**，不改内容：

```powershell
$src = "artifacts/benchmark-result/202611251855-791746c6"
$dst = "JiksLib.Benchmark/ArchivedResult/v2.0.0"
if (Test-Path $dst) { throw "基线目录已存在，不覆盖：$dst" }
Copy-Item $src $dst -Recurse
```

3. **写 `baseline.json`**（模板 `templates/baseline.json`）：`version`、`sourceRun`、`sourceCommit`、`scope`、`probe.sourceSha256`、`environment`、`comparison`、`notes`。探针哈希用
   `(Get-FileHash JiksLib.Benchmark/Probes/FootprintProbe.cs -Algorithm SHA256).Hash.ToLower()`。
4. **处理归档文档**：把 `ai-evaluation.md` / `ai-comparison.md` 的标题改成 `… — vX.Y.Z（源 run …）`，目录行改成归档路径，并加一段“归档说明：数值未改动”。**不要改任何数字、判定与结论。**
5. **校验完整性**（`raw/`、`probe/`、`run.json` 必须逐字节一致）：

```powershell
$src = "artifacts/benchmark-result/202611251855-791746c6"
$dst = "JiksLib.Benchmark/ArchivedResult/v2.0.0"
foreach ($rel in 'raw','probe','run.json') {
  $a = Get-ChildItem (Join-Path $src $rel) -Recurse -File |
    ForEach-Object { $_.FullName.Substring((Resolve-Path $src).Path.Length) + '|' + (Get-FileHash $_.FullName).Hash }
  $b = Get-ChildItem (Join-Path $dst $rel) -Recurse -File |
    ForEach-Object { $_.FullName.Substring((Resolve-Path $dst).Path.Length) + '|' + (Get-FileHash $_.FullName).Hash }
  Compare-Object $a $b
}
```

6. **更新清单**：在 `JiksLib.Benchmark/ArchivedResult/README.md` 的基线表里加一行。
7. **提交**：`git add JiksLib.Benchmark/ArchivedResult`（`.gitignore` 已为归档日志留例外）。提交前确认 `git status` 里没有 `artifacts/`。

禁止：把失败 run、`comparable: false`、局部 filter、没跑探针的 run 当版本基线；覆盖或删除旧版本目录；把 `dirty` 的 run 直接当基线而不在 `sourceDirtyNote` 里证明被测源码未变。

## 6. 评价与对比怎么写

- `ai-evaluation.md`：本次结果本身。按指标优先级组织，每个结论都要能在报告表格里指到具体行；代码建议必须指到 `文件:行`；实测与推断分开标注。
- `ai-comparison.md`：与基线逐项对比。第一行写基线**类型与名字**（`最新版本基线 v2.0.0` 或 `上一次运行 <目录名>`），先给判定（改善 / 退化 / 无明显变化 / 不可比），再给证据。
- 分配量按精确值比：任何增加都算退化，并在原因一节指出新增的分配点。
- 时间按 `references/metrics.md` 的阈值：`< 10%` 或 Mean/Median 不同向写“噪声内”，`≥ 25%` 且两者同向才判改善/退化，中间区间写“需复跑确认”；不同 job / 参数 / TFM / 机器之间不算百分比。
- 与版本基线比较时，用 `git log --oneline <基线 commit>..HEAD` 与 `git diff --stat` 关联提交，不要只贴两列数字。
- 碎片 / 保留内存：`baseline.json.probe.sourceSha256` 与当前 `FootprintProbe.cs` 的哈希一致才能算百分比；不一致写“不可比”，可并列绝对值作参考。
- `scripts/compare-baseline.ps1` 负责机械配对（默认就是最新版本基线）：

```powershell
# 全部差异行；输出 Markdown
pwsh ai-skill/performance-test/scripts/compare-baseline.ps1
# 只看前 40 行；指定基线与 run
pwsh ai-skill/performance-test/scripts/compare-baseline.ps1 -Baseline v2.0.0 -Top 40
# 比较指定 run，并直接写入文件
pwsh ai-skill/performance-test/scripts/compare-baseline.ps1 -Run artifacts/benchmark-result/<run> -OutFile artifacts/benchmark-result/<run>/comparison-raw.md
```

  脚本只给初步判定，不解释原因；`ai-comparison.md` 的结论、原因与代码关联仍要人写。
- 两个文件都从 `templates/` 复制，模板里的说明注释不要留在成品里。
- 没有数据的地方写 `N/A` 和原因。**不编造数字，不把推断写成测量结论。**
- 没有版本基线、也没有历史 run 时，明确写“首次基线，无基线可对比”，并列出下一次应该关注的行。

## 7. 禁止

- 修改 `raw/` 里的报告、`console.log` 或日志；修改基线里 `raw/`、`probe/`、`run.json` 的任何内容。
- 在同一个目录里跑两个代码状态；改代码后必须新建目录。
- 把两次不同 job / filter / TFM / 机器的时间差算成百分比并下结论。
- 用 `Job.Dry`、`--job short`、临时 `--iterationCount` 跑“正式结果”；确需快速试探时，在 `run.json` 写 `"comparable": false`，且不作为下一次的基线。
- 手写、改写或“修正”报告里的数字。
- 提交 `artifacts/`，或为它加 `.gitignore` 例外。
- 覆盖、删除已发布的版本基线目录；用一个不带探针或局部 filter 的 run 充当版本基线。
- 只贴表格不给结论，或只给“可以优化”这类没有位置、没有预期收益的建议。
- 为了凑齐五类指标而编造碎片化、内存占用结论。

## 8. 完成前检查

- [ ] 目录名 = 开始时间 + 8 位 commit，位置在 `artifacts/benchmark-result/`
- [ ] `raw/results/` 非空，`raw/console.log` 在，`raw/` 没有被改过
- [ ] `run.json` 含 commit、dirty、command、filter、job、dotnet 版本
- [ ] 需要碎片化/保留内存时，`probe/memory.jsonl` 存在且写明了场景、N、重复次数
- [ ] `ai-evaluation.md` 按 1→5 覆盖指标，缺的写 N/A 和原因
- [ ] `ai-comparison.md` 写明基线类型与名字、判定、可比性（环境/探针/参数集）
- [ ] 碎片/保留数字只与同一探针实现/宿主比较，没有跨探针版本算百分比
- [ ] 结论里的每个数字都能在 `raw/results/`、`run.json` 或 `baseline.json` 里找到
- [ ] 建基线时：`baseline.json` 的 `version` 与目录名一致，`raw/`、`probe/`、`run.json` 与源 run 逐字节一致，`ArchivedResult/README.md` 清单已更新
- [ ] `git status` 里没有 `artifacts/`；有 `ArchivedResult/` 时是有意提交

## 参考

- `references/result-layout.md` — 目录命名、`run.json` / `baseline.json` 字段、失败运行、基线归档与历史清理
- `references/metrics.md` — 指标口径、判定阈值、碎片化与内存探针
- `references/runbook.md` — 命令、过滤、环境要求、跨平台写法、新增基准、基线归档命令
- `scripts/compare-baseline.ps1` — 与最新版本基线逐行配对，输出 Markdown 对比表
- `templates/run.json`、`templates/baseline.json`、`templates/ai-evaluation.md`、`templates/ai-comparison.md`
