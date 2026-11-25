# AI 对比 — v2.0.0（源 run 202611251855-791746c6）vs 无（首次基线）

> 归档说明：本文件是归档时的原始对比，当时没有更早的可用基线，因此判定全部为“不可比”。**v2.0.0 之后的新 run 应把本目录作为基线**，按 `ai-skill/performance-test` 的新规则另写 `ai-comparison.md`，不要复用本文件的结论。

## 0. 判定摘要

| 基准（类.方法 [参数]） | 判定 | 依据（指标与差值） |
| --- | --- | --- |
| 全部 | 不可比 | 本地 `artifacts/benchmark-result/` 下没有更早的**成功**运行；本次是首次基线 |

总体：本次与 `791746c6` 的代码状态一致，`executed benchmarks: 341`，没有可对比的历史数据。后续运行应以本目录为基线。

## 1. 基线选择

- 候选目录按 12 位时间戳排序，排除本次：
  - `202611251850-791746c6`：**失败运行**，`raw/results/` 虽存在但 16 份报告全为 `NA`（`executed benchmarks: 0`，生成工程 restore/build 以 `-532462766` 退出），按 SKILL §3 跳过，不作为基线。
- 因此**没有可用基线**。本目录（源 run `202611251855-791746c6`，归档为 `v2.0.0`）是首次成功基线。

## 2. 指标对比（按优先级）

### 2.1 内存分配（大小与次数）

| 基准 | Allocated 本次 | 基线 | Δ | Gen0 本次/基线 | 判定 |
| --- | --- | --- | --- | --- | --- |
| 全部 | 见 `ai-evaluation.md` §8 | N/A | — | — | 首次基线，无对比 |

### 2.2 碎片化

| 场景 | 碎片率 本次 | 基线 | Δ | 说明 |
| --- | --- | --- | --- | --- |
| `sparse-set-reserved` | 0.039% | N/A | — | 首次探针基线，探针实现/宿主与后续运行一致时才可比较 |
| `sparse-set-growth` | 32.64% | N/A | — | 同上；绝对值只作历史参考 |
| `eventbus-listeners` | 0.14% | N/A | — | 同上 |

### 2.3 CPU 时间

| 基准 | Mean 本次 | 基线 | Δ% | Median Δ% | 置信区间是否重叠 | 判定 |
| --- | --- | --- | --- | --- | --- | --- |
| 全部 | 见 `ai-evaluation.md` §8 | N/A | — | — | — | 无基线 |

### 2.4 总体内存占用

| 场景 | 保留内存 本次 | 基线 | Δ% | 峰值工作集 Δ% | 判定 |
| --- | --- | --- | --- | --- | --- |
| `sparse-set-reserved` | 1,200,160 B（12.00 B/元素） | N/A | — | — | 无基线 |
| `sparse-set-growth` | 2,359,432 B（23.59 B/元素） | N/A | — | — | 无基线 |
| `eventbus-listeners` | 132,040 B（13.20 B/监听器） | N/A | — | — | 无基线 |

## 3. 逐项差异与原因

无。首次基线没有可解释的差异；`202611251850` 的失败原因（MSBuild 临时目录 `Access denied`）记录在本地 `artifacts/benchmark-result/202611251850-791746c6/ai-evaluation.md`（未入库），本次通过 `TMP`/`TEMP` 重定向规避。

## 4. 与提交差异的关联

```text
没有基线提交，git log 范围为 791746c6..791746c6（空）。
```

本次提交 `791746c6`（“执行性能优化”）只改了 `JiksLib/Control/SafeEvent.cs` 的 `Publish` 无空洞分支；本 run 中对应基准是 `SafeEventBenchmarks.PublishEvent_*` 与 `SafeEventRecursiveBenchmarks`。由于没有优化前的成功运行，**无法判断该提交的改善幅度**，只能记录优化后的绝对值：100 监听器发布 1,715.0 ns / 0 B，Add+Remove（预分配）4,444.4 ns / 0 B。

## 5. 不可比与缺失项

| 项 | 原因 |
| --- | --- |
| 时间 / 分配 / 碎片 / 保留内存的前后差异 | 没有更早的成功运行 |
| `202611251850-791746c6` | 失败运行（全 NA），已跳过 |
| 探针的纵向比较 | 本次是探针的首个基线；探针实现或宿主变化后旧数据不能算百分比 |
| 工作区一致性 | 本次运行中 `README.md` 与 `JiksLib/JiksLib.csproj` 被外部修改（dirty=true）；被测源码与基准工程未变，但后续对比应确认基线/新 run 的 dirty 情况 |

## 6. 建议

1. **下一次运行以本目录为基线**，保持同一台机器、同一 `--filter '*'`、同一 job、同一 `TMP`/`TEMP` 方案；按 SKILL §4/§6 的阈值比较。
2. **优先看的行**：`SafeEventBenchmarks.PublishEvent_WithListeners`（本次 303 / 658 / 1,715 ns，0 B）、`EventBusBenchmarks.PublishEvent_*`、`EventBus.AddListeners [1000]`（16,840 B vs warmup 0 B）、`SparseSet.Add [10000]`（295,032 B）、三个探针场景。
3. **若目标是量化 `791746c6`**：在父提交 `5bab883` 上用 `--filter '*SafeEvent*'` 单独跑一次（新建目录、同一机器），与本 run 的 18 行做同 filter 对比；不要把它当作全量基线。
4. `EventBusComparisonBenchmarks` 的时间列存在跨类矛盾（见评价 §4/§6），下一次对比前应先复跑定位，避免把噪声写进趋势。
