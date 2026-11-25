# 结果目录与元数据

## 1. 目录名

```text
artifacts/benchmark-result/<yyyyMMddHHmm>-<commit8>[-<n>]
```

| 片段 | 取值 | 说明 |
| --- | --- | --- |
| `yyyyMMddHHmm` | 运行**开始**的本地时间（`Asia/Shanghai`） | 12 位定宽、补零、无分隔符。定宽保证字典序 = 时间序 |
| `commit8` | `git rev-parse HEAD` 的前 8 位小写十六进制 | 不用 `--short`：位数可能随仓库规模变化 |
| `-<n>` | `-2`、`-3`… | 同一分钟、同一 commit 的重复运行时追加，从 2 开始 |

例子：`202611140357-1a2b3c4d`。

```powershell
$root  = "artifacts/benchmark-result"
$stamp = Get-Date -Format "yyyyMMddHHmm"
$sha   = (git rev-parse HEAD).Substring(0, 8)
$run   = Join-Path $root "$stamp-$sha"

for ($i = 2; Test-Path $run; $i++) {
    $run = Join-Path $root "$stamp-$sha-$i"
}
```

- 目录名在**运行前**确定；跨分钟结束也沿用开始时间。
- 不要用 `Get-Date` 的 `Sortable` 格式（`2026-11-14T03:57`），会引入分隔符。
- 不是 git 仓库、取不到提交时用 `nogit` 代替 `commit8`，并在 `run.json` 的 `notes` 说明；这样的目录不参与自动基线选择，只能手动对比。
- 目录已存在且属于别的内容时，**新建**带序号的新目录，绝不覆盖、删除旧目录。

## 2. 目录内容

```text
<run>/
├── raw/
│   ├── console.log          # 本次运行 stdout+stderr 的完整拷贝
│   ├── results/             # BDN 生成的报告，按基准类型分文件
│   ├── *.log                # BDN 自己的运行日志（若生成）
│   └── ...                  # BDN 生成的工程与构建输出（可重建，可删）
├── probe/              # 内置探针输出（需要时）：memory.jsonl
├── run.json
├── ai-evaluation.md
└── ai-comparison.md
```

### `raw/`

- 由 `--artifacts <run>/raw` 指定；不指定时 BDN 会在**当前工作目录**建 `BenchmarkDotNet.Artifacts`（通常是仓库根），这是违规的：把结果移入本 run 目录，或删掉后重跑。
- 报告默认格式是 CSV、HTML、Markdown（Markdown 为 GitHub 风格，实测文件名为
  `JiksLib.Benchmark.Collections.SparseSetBenchmarks-report-github.md`）。不同类型的基准各一份报告，正好用来按类配对；所以**不要加 `--join`**。
- 报告可能在 `results/` 下再按工程 / 配置 / TFM 分层，以实际为准；找文件用
  `Get-ChildItem raw -Recurse -Filter *-report*`。
- `console.log` 必须单独保存：BDN 的 Markdown 报告不含运行警告（如 OutOfMemory 提示、环境信息、被跳过的基准），这些只在控制台里。
- `results/`、`console.log`、`*.log` **只读**，任何情况下不改写。
- BDN 生成的临时工程和构建输出可以在运行结束后删除（它们是可重建的构建产物，和 `bin` 同类）；删除后在 `run.json` 的 `pruned` 里记录删了什么。`results/` 永远保留。

### `ai-evaluation.md`

本次结果本身的分析，模板见 `templates/ai-evaluation.md`。要求：

- 覆盖本次运行出现的**全部**基准类型与方法（`[Params]` 的每个参数组合都算一行）。
- 按指标优先级 1→5 写，不要按报告列顺序。
- 代码建议指到 `JiksLib/...cs:<行>`，写预期收益与风险。
- 缺的指标写 `N/A` 和原因，不猜。

### `ai-comparison.md`

与基线的对比（默认最新版本基线，没有版本基线才回退到上一次运行），模板见 `templates/ai-comparison.md`。要求：

- 第一行写明基线的**类型、名字与来源 run**（如 `vs 最新版本基线 v2.0.0（源 run 202611251855-791746c6）`）；没有基线就明确写"首次基线，无基线可对比"。
- 先给判定，再给证据；判定只用四种：改善 / 退化 / 无明显变化 / 不可比。
- 只比两边都有的基准；缺失项单列。
- 分配量按精确值比；时间按 `references/metrics.md` 的阈值比，且只在环境一致时算百分比。
- 碎片/保留内存只与同一探针实现/宿主比（核对 `baseline.json.probe.sourceSha256`）；探针版本不同就写"不可比"。
- 与版本基线比较时附 `git log <基线 commit>..HEAD` 与 `git diff --stat`，把差异关联到提交。

### `probe/`

只在需要碎片化/保留内存时才有：

- `memory.jsonl`：内置 `probe` 子命令的输出，一行一条（3 场景 × 3 次 = 9 行）。
- 必要时放 `README.md`，记录探针版本/宿主与对照数据。

探针实现随代码库演进（`JiksLib.Benchmark/Probes/FootprintProbe.cs`）。**探针实现或宿主改变后，旧的碎片率不能与新的直接比较**：碎片是进程级指标，保留/分配通常逐字节不变而碎片率会变。出现这种情况时在 `probe/README.md` 里同时记录两代数据并指明哪一代是规范值，下一次运行只用规范值。

### `run.json`

固定字段（缺了就无法判断可比性，视为不合格）：

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| `schemaVersion` | number | 目前为 `1` |
| `status` | string | `success` / `failed` |
| `startedAt` / `finishedAt` | string | ISO 8601 带时区，如 `2026-11-14T03:57:00+08:00` |
| `commit` | string | 8 位提交号，或 `nogit` |
| `commitSubject` | string | `git log -1 --format=%s` |
| `branch` | string | `git rev-parse --abbrev-ref HEAD` |
| `dirty` | boolean | `git status --porcelain` 是否有输出 |
| `dirtyFiles` | string[] | 脏文件列表（可截断到 20 条并注明） |
| `comparable` | boolean | 是否可与默认配置的历史 run 直接比较；用了 `--job short` 等临时参数时为 `false` |
| `probe` | string | `builtin`（跑了内置探针）/ `none`（未跑）；碎片/保留的对比要求两边一致 |
| `command` | string | 实际执行的完整命令（含 `--filter`、`--artifacts`） |
| `configuration` | string | `Release` |
| `targetFramework` | string | `net10.0`（由 `[SimpleJob]` 固定） |
| `job` | string | `SimpleJob(RuntimeMoniker.Net10_0, invocationCount: 1) + MemoryDiagnoser` |
| `filter` | string | 传给 `--filter` 的原文，如 `*` 或 `*SparseSetBenchmarks*` |
| `benchmarkTypes` | string[] | `raw/results/*-report.*` 的文件名前缀，去重 |
| `environment` | object | `dotnet`、`os`、`cpu`、`memoryGB` |
| `pruned` | string[] | 已删除的可重建内容，如 `["bin"]` |
| `notes` | string | 失败原因、临时参数、异常情况；没有就空字符串 |

模板：`templates/run.json`。

## 3. 失败运行

以下任一情况都算失败：

- `dotnet run` 非零退出；
- `raw/results/` 为空或没有报告文件；
- `console.log` 里出现编译错误、`No benchmarks to run`、未处理异常。

失败时：

1. 保留 `raw/console.log`（和已有的日志），不写指标；
2. `run.json` 的 `status` 写 `failed`，`notes` 写关键错误行；
3. `ai-evaluation.md` 只写失败原因与修复方向，不填表格；
4. `ai-comparison.md` 写"本次失败，无可比数据"，并说明下一次对比应以哪个目录为基线；
5. 不把失败目录当作后续基线（基线选择要求 `raw/results` 非空）。

## 4. 历史与清理

- `artifacts/` 不提交，GitHub 上看不到历史；本地对比只在本地有效。
- 版本基线在 `JiksLib.Benchmark/ArchivedResult/v<X.Y.Z>/` 里**入库**，不受 `git clean -xdf`、换机器、清 `artifacts` 影响；跨版本对比以它为准。
- 不要自动删除旧 run。确需清理时，至少保留最近 5 次成功运行与每次"发版/关键改动"对应的 run。
- 版本基线目录**不清理**：即使有了新版本基线，旧版本目录也保留（`ArchivedResult/README.md` 的清单按版本列出）。
- `dotnet clean` 只清 `bin`/`obj` 类输出，不会动 `benchmark-result`；不要自己写脚本清 `artifacts` 根目录。

## 5. 版本基线归档

版本基线把"某个已定版代码状态"的完整性能数据固定到仓库里，作为之后所有运行的默认比较对象。它与日常 run 的区别只有存放位置和是否入库：**基线入库，日常 run 不入库**。

### 5.1 目录与内容

```text
JiksLib.Benchmark/ArchivedResult/
├── README.md                 # 归档规则与基线清单（每个版本一行）
└── v2.0.0/
    ├── baseline.json         # 基线元数据；基线选择只读它
    ├── run.json              # 源 run 的原始元数据
    ├── raw/                  # 源 run 的 BDN 报告（md / csv / html）、console.log、运行日志
    ├── probe/memory.jsonl    # 源 run 的探针输出（3 场景 × 3 次）
    ├── ai-evaluation.md      # 源 run 的评价：只改标题与路径，数字不动
    └── ai-comparison.md      # 归档时的对比：作为历史记录保留
```

- 从源 run **完整复制**，不做裁剪：`console.log` 与运行日志是测量证据（警告、环境、被跳过的基准），即使体积大也保留。
- `raw/`、`probe/`、`run.json` 只读，任何情况下不改写。
- 目录名只允许 `v<major>.<minor>.<patch>` 形式，与 `baseline.json.version`、归档时 `JiksLib/JiksLib.csproj` 的 `<Version>` 三者一致；预发布版本（`-beta`）不进基线。
- 一个版本一个目录；已发布的不覆盖、不删除，后续版本新建目录，并在新基线的 `supersedes` 里写上一个版本。

### 5.2 `baseline.json`

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| `schemaVersion` | number | 目前为 `1` |
| `kind` | string | 固定 `version-baseline` |
| `version` | string | `X.Y.Z`，必须等于目录名与 `libraryVersion` |
| `libraryVersion` | string | 归档时的库版本（`<Version>`） |
| `archivedAt` | string | 归档时间，ISO 8601 带时区；版本相同时用它打破平局 |
| `sourceRun` | string | 源 run 目录名（`yyyyMMddHHmm-commit8`） |
| `sourceCommit` / `sourceCommitSubject` / `sourceBranch` | string | 源 run 的提交信息 |
| `sourceStartedAt` / `sourceFinishedAt` | string | 源 run 的起止时间 |
| `sourceDirty` / `sourceDirtyFiles` | boolean / string[] | 源 run 是否 dirty；dirty 时必须逐个列出脏文件 |
| `sourceDirtyNote` | string | dirty 时必填：证明被测源码未变（例如只改了 README / 版本号，且 BDN 构建早于修改） |
| `scope` | object | `command`、`filter`、`configuration`、`targetFramework`、`job`、`benchmarkTypes`（类数）、`benchmarks`（行数） |
| `probe` | object | `implementation`、`sourceSha256`（`FootprintProbe.cs` 的 SHA-256，小写）、`scenarios`、`trialsPerScenario` |
| `environment` | object | `dotnet`、`os`、`cpu`、`memoryGB` |
| `comparison` | object | `defaultFor`、`timingRequiresSameEnvironment`、`timingEnvironmentKeys`、`allocationComparableAcrossMachines`、`fragmentationRequiresSameProbeSha256` |
| `supersedes` | string / null | 被本基线取代的版本（通常为上一个），没有则 `null` |
| `notes` | string | 已知异常（如无法切换电源计划）、来源说明；没有就空字符串 |

模板：`templates/baseline.json`。

### 5.3 选择与创建

- 选择算法（最新语义版本优先、无基线才回退上一次运行）见 `SKILL.md` §4。
- 创建时机、源 run 的硬性条件、复制/写元数据/校验/更新清单的步骤见 `SKILL.md` §5。
- 源 run 必须是一次**全量**成功运行（`filter: "*"`、跑过内置探针、`comparable: true`）；局部 filter、失败运行、`Job.Dry` 结果都不能当版本基线。
- 校验：`raw/`、`probe/`、`run.json` 与源 run 逐字节一致；`baseline.json` 可被 `ConvertFrom-Json` 解析；目录名、`version`、`libraryVersion` 三者一致。
- 提交：`git add JiksLib.Benchmark/ArchivedResult`。`.gitignore` 为归档目录里的 `*.log` 留了例外（`!/JiksLib.Benchmark/ArchivedResult/**/*.log`），否则 `console.log` 会被忽略。

### 5.4 归档后怎么比

- **默认比较对象**：最新版本基线。日常 run 的 `ai-comparison.md` 第一行写 `vs 最新版本基线 vX.Y.Z`，并注明源 run 与 `sourceCommit`。
- **时间**：只有 `baseline.json.environment` 的 CPU / OS / dotnet 与本次 `run.json` 一致时才换算百分比；否则写"不可比"，不给百分比。
- **分配量**：确定性指标，可精确比较；跨版本差异要在 `git log <基线 commit>..HEAD` 里找对应改动。
- **碎片 / 保留内存**：`baseline.json.probe.sourceSha256` 与当前 `FootprintProbe.cs` 哈希一致才算百分比；不一致只能并列绝对值作参考。
- **基准集合**：只比交集；版本升级新增的基准在"仅本次"单列，删除的基准在"仅基线"单列，都不要当 0。
- `scripts/compare-baseline.ps1` 默认就按本节规则选最新版本基线、逐行配对并给出初步判定。

### 5.5 已归档的基线

| 版本 | 源 run | 提交 | 规模 | 环境 |
| --- | --- | --- | --- | --- |
| `v2.0.0` | `202611251855-791746c6` | `791746c6` | 16 类 / 341 用例 + 3 探针场景 | .NET 10.0.15，Windows 11 26H2，i5-12600KF |

清单同时维护在 `JiksLib.Benchmark/ArchivedResult/README.md`；两边不一致时以该 README 为准。
