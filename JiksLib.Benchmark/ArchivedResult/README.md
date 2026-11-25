# 性能基线归档（ArchivedResult）

这里保存**已定版版本的性能基线**，与 `artifacts/benchmark-result/` 的日常运行归档不同：本目录**提交进仓库**，用于跨版本、跨时间回归比较；日常 run 仍然只留在本地 `artifacts/`。

## 目录

```text
JiksLib.Benchmark/ArchivedResult/
├── README.md                 # 本文件：归档规则与基线清单
└── v2.0.0/
    ├── baseline.json         # 基线元数据：版本、源 run、环境、口径、可比性要求
    ├── run.json              # 源 run 的原始元数据（原样保留，作 provenance）
    ├── raw/                  # BDN 原始报告（md / csv / html）、console.log、运行日志
    ├── probe/memory.jsonl    # 内置探针 3 场景 × 3 次的原始输出
    ├── ai-evaluation.md      # 源 run 的 AI 评价（归档时更新过标题与路径，数值未改）
    └── ai-comparison.md      # 归档时的 AI 对比（当时无更早基线）
```

## 规则

1. **一个版本一个目录**：目录名为 `v<major>.<minor>.<patch>`，与 `JiksLib/JiksLib.csproj` 的 `<Version>` 一致。已发布的基线目录**不覆盖、不删除**，后续版本新建目录。
2. **内容原样**：`raw/`、`probe/`、`run.json` 从源 run 完整复制，不改数字、不改报告。`ai-evaluation.md` / `ai-comparison.md` 只允许改标题、路径与归档说明。
3. **必须有 `baseline.json`**：基线选择只看它，不看目录修改时间。字段与写法见 `ai-skill/performance-test/references/result-layout.md` §5。
4. **基线来源**：取该版本代码状态下最近一次 `status: success`、`comparable: true`、`filter '*'` 且跑过内置探针的完整 run；`dirty` 允许，但必须在 `sourceDirtyNote` 里证明被测源码未变。
5. **比较规则**：时间只在环境指纹一致时算百分比；分配量是确定性指标，可精确比较；碎片率仅在探针源码 SHA-256 一致时比较。详见 `ai-skill/performance-test/references/metrics.md`。
6. **`*.log` 例外**：仓库 `.gitignore` 默认忽略 `*.log`，本目录通过 `!/JiksLib.Benchmark/ArchivedResult/**/*.log` 例外保留 `console.log` 与 BDN 运行日志，因为它们是测量证据的一部分。

## 基线清单

| 版本 | 归档日期 | 源 run | 提交 | 基准 | 环境 | 说明 |
| --- | --- | --- | --- | --- | --- | --- |
| `v2.0.0` | 2026-11-25 | `202611251855-791746c6` | `791746c6` | 16 类 / 341 用例 + 3 探针场景 | .NET 10.0.15，Windows 11 26H2，i5-12600KF | 首个版本基线；`SafeEvent.Publish` 无空洞分支优化后的结果 |

## 相关文档

- 工作区 skill：[`ai-skill/performance-test/SKILL.md`](../../ai-skill/performance-test/SKILL.md)
- 归档与命名：[`references/result-layout.md`](../../ai-skill/performance-test/references/result-layout.md)
- 指标口径与阈值：[`references/metrics.md`](../../ai-skill/performance-test/references/metrics.md)
- 运行手册：[`references/runbook.md`](../../ai-skill/performance-test/references/runbook.md)
