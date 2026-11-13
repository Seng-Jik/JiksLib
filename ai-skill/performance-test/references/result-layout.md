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
├── run.json
├── ai-evaluation.md
└── ai-comparison.md
```

### `raw/`

- 由 `--artifacts <run>/raw` 指定；不指定时 BDN 会在**当前工作目录**建 `BenchmarkDotNet.Artifacts`（通常是仓库根），这是违规的：把结果移入本 run 目录，或删掉后重跑。
- 报告默认格式是 CSV、HTML、Markdown，文件名形如
  `JiksLib.Benchmark.Collections.SparseSetBenchmarks-report.md`。不同类型的基准各一份报告，正好用来按类配对；所以**不要加 `--join`**。
- 报告可能在 `results/` 下再按工程 / 配置 / TFM 分层，以实际为准；找文件用
  `Get-ChildItem raw -Recurse -Filter *-report.md`。
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

与"上一次运行"的对比，模板见 `templates/ai-comparison.md`。要求：

- 第一行写明基线的**目录名**与选择依据；没有基线就明确写"首次基线，无上一次可对比"。
- 先给判定，再给证据；判定只用四种：改善 / 退化 / 无明显变化 / 不可比。
- 只比两边都有的基准；缺失项单列。
- 分配量按精确值比；时间按 `references/metrics.md` 的阈值比。

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

- `artifacts/` 不提交，GitHub 上看不到历史；对比只在本地有效。
- 不要自动删除旧 run。确需清理时，至少保留最近 5 次成功运行与每次"发版/关键改动"对应的 run。
- `git clean -xdf` 会连基线一起删掉；执行前把要保留的 run 复制到仓库外。
- `dotnet clean` 只清 `bin`/`obj` 类输出，不会动 `benchmark-result`；不要自己写脚本清 `artifacts` 根目录。
