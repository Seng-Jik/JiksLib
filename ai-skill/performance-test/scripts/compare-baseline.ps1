<#
.SYNOPSIS
    把一次性能运行与版本基线（JiksLib.Benchmark/ArchivedResult/vX.Y.Z）逐行配对，输出 Markdown 对比表。

.DESCRIPTION
    默认基线 = ArchivedResult 中语义版本最新的 vX.Y.Z；默认本次 = artifacts/benchmark-result 中最近一次成功运行。
    只做机械计算：配对、Allocated 差值、Mean/Median 百分比、按 metrics.md 的阈值给初步判定。
    结论、原因与代码关联仍需人来写进 ai-comparison.md；脚本不臆测原因。

.PARAMETER Baseline
    基线目录，或 v2.0.0 这样的版本号。省略时自动选最新版本基线。

.PARAMETER Run
    本次运行目录。省略时自动选 artifacts/benchmark-result 下最近一次成功运行。

.PARAMETER Top
    最多输出多少行非“无明显变化”的结果（0 = 全部）。默认 0。

.PARAMETER IncludeUnchanged
    同时输出“无明显变化”的行。

.PARAMETER OutFile
    把 Markdown 写入文件（UTF-8 无 BOM）；省略时写标准输出。

.EXAMPLE
    pwsh ai-skill/performance-test/scripts/compare-baseline.ps1

.EXAMPLE
    pwsh ai-skill/performance-test/scripts/compare-baseline.ps1 -Baseline v2.0.0 -Top 40

.NOTES
    判定阈值见 ai-skill/performance-test/references/metrics.md §6；脚本只给初步判定，跨机器/跨探针要按那里的规则降级为“不可比”。
#>
[CmdletBinding()]
param(
    [string]$Baseline,
    [string]$Run,
    [int]$Top = 0,
    [switch]$IncludeUnchanged,
    [string]$OutFile
)

$ErrorActionPreference = 'Stop'

$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path
$ArchiveRoot = Join-Path $RepoRoot 'JiksLib.Benchmark/ArchivedResult'
$RunRoot = Join-Path $RepoRoot 'artifacts/benchmark-result'

function Get-LatestBaselinePath {
    if (-not (Test-Path $ArchiveRoot)) { throw "找不到基线目录：$ArchiveRoot" }

    $candidates = Get-ChildItem $ArchiveRoot -Directory |
        Where-Object { $_.Name -match '^v\d+\.\d+\.\d+$' -and (Test-Path (Join-Path $_.FullName 'baseline.json')) } |
        ForEach-Object {
            $meta = Get-Content (Join-Path $_.FullName 'baseline.json') -Raw | ConvertFrom-Json
            [pscustomobject]@{
                Name       = $_.Name
                Path       = $_.FullName
                Version    = [version]$meta.version
                ArchivedAt = [datetimeoffset]$meta.archivedAt
            }
        }

    $latest = $candidates | Sort-Object Version, ArchivedAt -Descending | Select-Object -First 1
    if (-not $latest) { throw "ArchivedResult 下没有可用的版本基线（需要 vX.Y.Z/baseline.json）" }
    return $latest.Path
}

function Resolve-BaselinePath([string]$value) {
    if ([string]::IsNullOrWhiteSpace($value)) { return (Get-LatestBaselinePath) }

    if (Test-Path $value) { return (Resolve-Path $value).Path }
    if ($value -match '^v?\d+\.\d+\.\d+$') {
        $name = if ($value.StartsWith('v')) { $value } else { "v$value" }
        $candidate = Join-Path $ArchiveRoot $name
        if (Test-Path $candidate) { return (Resolve-Path $candidate).Path }
    }
    throw "找不到基线：$value"
}

function Get-LatestRunPath {
    if (-not (Test-Path $RunRoot)) { throw "找不到运行目录：$RunRoot（先按 SKILL.md §3 跑一次基准）" }

    $latest = Get-ChildItem $RunRoot -Directory |
        Where-Object { $_.Name -match '^\d{12}-[0-9a-fA-F]{8}(-\d+)?$' -and (Test-Path (Join-Path $_.FullName 'raw/results')) } |
        Where-Object {
            $metaPath = Join-Path $_.FullName 'run.json'
            if (-not (Test-Path $metaPath)) { return $true }
            try { (Get-Content $metaPath -Raw | ConvertFrom-Json).status -ne 'failed' } catch { return $true }
        } |
        Sort-Object @{ Expression = { $_.Name.Substring(0, 12) } }, @{ Expression = { $_.Name } } -Descending |
        Select-Object -First 1

    if (-not $latest) { throw "artifacts/benchmark-result 下没有成功的运行目录" }
    return $latest.FullName
}

function Resolve-RunPath([string]$value) {
    if ([string]::IsNullOrWhiteSpace($value)) { return (Get-LatestRunPath) }
    if (-not (Test-Path $value)) { throw "找不到运行目录：$value" }
    return (Resolve-Path $value).Path
}

function ConvertTo-Nanoseconds([string]$text) {
    if ([string]::IsNullOrWhiteSpace($text)) { return $null }
    $t = $text.Trim()
    if ($t -eq 'NA' -or $t -eq '-') { return $null }
    if ($t -notmatch '^(?<num>[\d\.,]+)\s*(?<unit>ns|μs|us|ms|s)$') { return $null }

    $n = [double]::Parse($Matches.num.Replace(',', ''), [System.Globalization.CultureInfo]::InvariantCulture)
    switch ($Matches.unit) {
        'ns' { return $n }
        'μs' { return $n * 1000.0 }
        'us' { return $n * 1000.0 }
        'ms' { return $n * 1000000.0 }
        's' { return $n * 1000000000.0 }
    }
    return $null
}

function ConvertTo-Bytes([string]$text) {
    if ([string]::IsNullOrWhiteSpace($text)) { return $null }
    $t = $text.Trim()
    if ($t -eq 'NA' -or $t -eq '-') { return $null }
    if ($t -notmatch '^(?<num>[\d\.,]+)\s*B$') { return $null }
    return [double]::Parse($Matches.num.Replace(',', ''), [System.Globalization.CultureInfo]::InvariantCulture)
}

function Format-Nanoseconds([double]$ns) {
    if ($ns -ge 1000000.0) { return ('{0:N2} ms' -f ($ns / 1000000.0)) }
    if ($ns -ge 1000.0) { return ('{0:N2} μs' -f ($ns / 1000.0)) }
    return ('{0:N1} ns' -f $ns)
}

function Format-Bytes([double]$bytes) {
    $sign = if ($bytes -lt 0) { '-' } else { '' }
    $value = [math]::Abs($bytes)
    if ($value -ge 1048576.0) { return "$sign$('{0:N2} MiB' -f ($value / 1048576.0))" }
    if ($value -ge 1024.0) { return "$sign$('{0:N1} KiB' -f ($value / 1024.0))" }
    return "$sign$('{0:N0} B' -f $value)"
}

function Format-Percent([Nullable[double]]$pct) {
    if ($null -eq $pct) { return '—' }
    $sign = if ($pct -gt 0) { '+' } else { '' }
    return "$sign$('{0:N1}' -f $pct)%"
}

function Get-ReportFiles([string]$runPath) {
    $results = Join-Path $runPath 'raw/results'
    if (-not (Test-Path $results)) { throw "没有报告目录：$results" }
    return @(Get-ChildItem $results -Filter '*-report.csv' -File)
}

function Get-ReportClass($file) {
    return ($file.Name -replace '-report\.csv$', '')
}

function Read-Report([string]$path) {
    $rows = @(Import-Csv $path)
    if ($rows.Count -eq 0) { return [pscustomobject]@{ Rows = @(); ParamColumns = @(); HasAllocated = $false } }

    $columns = @($rows[0].PSObject.Properties.Name)
    $warmup = [array]::IndexOf($columns, 'WarmupCount')
    $mean = [array]::IndexOf($columns, 'Mean')

    $paramColumns = @()
    if ($warmup -ge 0 -and $mean -gt $warmup + 1) {
        $paramColumns = @($columns[($warmup + 1)..($mean - 1)])
    }

    return [pscustomobject]@{
        Rows         = $rows
        ParamColumns = $paramColumns
        HasAllocated = $columns -contains 'Allocated'
    }
}

function Get-RowKey($row, [string[]]$paramColumns) {
    $parts = foreach ($column in $paramColumns) { "$column=$($row.$column)" }
    return $row.Method + '|' + ($parts -join ';')
}

function Format-RowLabel($row, [string[]]$paramColumns) {
    $parts = foreach ($column in $paramColumns) { "$column=$($row.$column)" }
    if ($parts.Count -eq 0) { return $row.Method }
    return $row.Method + ' [' + ($parts -join ', ') + ']'
}

function Get-Verdict([Nullable[double]]$baseAlloc, [Nullable[double]]$runAlloc, [Nullable[double]]$baseMean, [Nullable[double]]$runMean, [Nullable[double]]$baseMedian, [Nullable[double]]$runMedian) {
    if ($null -ne $baseAlloc -and $null -ne $runAlloc -and $runAlloc -ne $baseAlloc) {
        if ($runAlloc -gt $baseAlloc) { return '退化' }
        return '改善'
    }

    if ($null -eq $baseMean -or $null -eq $runMean -or $baseMean -le 0) { return '不可比' }

    $meanPct = ($runMean - $baseMean) / $baseMean * 100.0
    $medianPct = $null
    if ($null -ne $baseMedian -and $null -ne $runMedian -and $baseMedian -gt 0) {
        $medianPct = ($runMedian - $baseMedian) / $baseMedian * 100.0
    }

    $absMean = [math]::Abs($meanPct)
    $sameDirection = $false
    if ($null -ne $medianPct -and [math]::Sign($meanPct) -eq [math]::Sign($medianPct)) { $sameDirection = $true }

    if ($absMean -lt 10.0) { return '无明显变化' }
    if ($absMean -ge 25.0 -and $sameDirection) {
        if ($meanPct -gt 0) { return '退化' }
        return '改善'
    }
    return '需复跑确认'
}

# ---------------------------------------------------------------- 参数解析与文件配对

$baselinePath = Resolve-BaselinePath $Baseline
$runPath = Resolve-RunPath $Run
$baselineMeta = $null
$baselineMetaPath = Join-Path $baselinePath 'baseline.json'
if (Test-Path $baselineMetaPath) {
    $baselineMeta = Get-Content $baselineMetaPath -Raw | ConvertFrom-Json
}
$runMeta = $null
$runMetaPath = Join-Path $runPath 'run.json'
if (Test-Path $runMetaPath) {
    $runMeta = Get-Content $runMetaPath -Raw | ConvertFrom-Json
}

$baselineReports = @{}
foreach ($file in Get-ReportFiles $baselinePath) { $baselineReports[(Get-ReportClass $file)] = $file.FullName }
$runReports = @{}
foreach ($file in Get-ReportFiles $runPath) { $runReports[(Get-ReportClass $file)] = $file.FullName }

$rows = New-Object System.Collections.Generic.List[object]
$onlyBaseline = New-Object System.Collections.Generic.List[string]
$onlyRun = New-Object System.Collections.Generic.List[string]
$columnWarnings = New-Object System.Collections.Generic.List[string]

foreach ($class in ($baselineReports.Keys | Sort-Object)) {
    if (-not $runReports.ContainsKey($class)) {
        $read = Read-Report $baselineReports[$class]
        foreach ($row in $read.Rows) { $onlyBaseline.Add("$class.$(Format-RowLabel $row $read.ParamColumns)") }
        continue
    }

    $base = Read-Report $baselineReports[$class]
    $curr = Read-Report $runReports[$class]

    if (($base.ParamColumns -join ',') -ne ($curr.ParamColumns -join ',')) {
        $columnWarnings.Add("$class 的参数列在两次运行间变化（基线: $($base.ParamColumns -join ',') / 本次: $($curr.ParamColumns -join ',')），本类按不可比处理")
        $rows.Add([pscustomobject]@{
                Class = $class; Label = '(整类)'; Verdict = '不可比'; Note = '参数列变化'
                BaseAlloc = $null; RunAlloc = $null; DeltaAlloc = $null
                BaseMean = $null; RunMean = $null; MeanPct = $null; MedianPct = $null
            })
        continue
    }

    $baseRows = @{}
    foreach ($row in $base.Rows) { $baseRows[(Get-RowKey $row $base.ParamColumns)] = $row }

    foreach ($row in $curr.Rows) {
        $key = Get-RowKey $row $curr.ParamColumns
        $label = Format-RowLabel $row $curr.ParamColumns
        if (-not $baseRows.ContainsKey($key)) {
            $onlyRun.Add("$class.$label")
            continue
        }

        $old = $baseRows[$key]
        $baseAlloc = if ($base.HasAllocated) { ConvertTo-Bytes $old.Allocated } else { $null }
        $runAlloc = if ($curr.HasAllocated) { ConvertTo-Bytes $row.Allocated } else { $null }
        $baseMean = ConvertTo-Nanoseconds $old.Mean
        $runMean = ConvertTo-Nanoseconds $row.Mean
        $baseMedian = ConvertTo-Nanoseconds $old.Median
        $runMedian = ConvertTo-Nanoseconds $row.Median

        $deltaAlloc = $null
        if ($null -ne $baseAlloc -and $null -ne $runAlloc) { $deltaAlloc = $runAlloc - $baseAlloc }

        $meanPct = $null
        if ($null -ne $baseMean -and $null -ne $runMean -and $baseMean -gt 0) { $meanPct = ($runMean - $baseMean) / $baseMean * 100.0 }

        $medianPct = $null
        if ($null -ne $baseMedian -and $null -ne $runMedian -and $baseMedian -gt 0) { $medianPct = ($runMedian - $baseMedian) / $baseMedian * 100.0 }

        $note = ''
        if (-not $base.HasAllocated -or -not $curr.HasAllocated) { $note = '缺少 Allocated 列' }

        $rows.Add([pscustomobject]@{
                Class = $class; Label = $label
                Verdict = Get-Verdict $baseAlloc $runAlloc $baseMean $runMean $baseMedian $runMedian
                Note = $note
                BaseAlloc = $baseAlloc; RunAlloc = $runAlloc; DeltaAlloc = $deltaAlloc
                BaseMean = $baseMean; RunMean = $runMean; MeanPct = $meanPct; MedianPct = $medianPct
            })
    }
}

foreach ($class in ($runReports.Keys | Sort-Object)) {
    if ($baselineReports.ContainsKey($class)) { continue }
    $read = Read-Report $runReports[$class]
    foreach ($row in $read.Rows) { $onlyRun.Add("$class.$(Format-RowLabel $row $read.ParamColumns)") }
}

# ---------------------------------------------------------------- 排序与输出

$order = @{ '退化' = 0; '改善' = 1; '需复跑确认' = 2; '不可比' = 3; '无明显变化' = 4 }
$sorted = $rows | Sort-Object `
    @{ Expression = { $order[$_.Verdict] } },
    @{ Expression = { if ($null -ne $_.DeltaAlloc) { [math]::Abs($_.DeltaAlloc) } else { 0 } }; Descending = $true },
    @{ Expression = { if ($null -ne $_.MeanPct) { [math]::Abs($_.MeanPct) } else { 0 } }; Descending = $true },
    @{ Expression = { $_.Class } },
    @{ Expression = { $_.Label } }

$display = $sorted
if (-not $IncludeUnchanged) { $display = @($display | Where-Object { $_.Verdict -ne '无明显变化' }) }
if ($Top -gt 0) { $display = @($display | Select-Object -First $Top) }

$baselineName = Split-Path $baselinePath -Leaf
$runName = Split-Path $runPath -Leaf
$baselineTitle = $baselineName

$builder = New-Object System.Text.StringBuilder
[void]$builder.AppendLine("# 性能对比（脚本计算） — $runName vs 基线 $baselineTitle")
[void]$builder.AppendLine('')
if ($baselineMeta) {
    $archivedAt = Get-Date $baselineMeta.archivedAt -Format 'yyyy-MM-dd HH:mm:ss zzz'
    [void]$builder.AppendLine("- 基线：``JiksLib.Benchmark/ArchivedResult/$baselineName/``，源 run ``$($baselineMeta.sourceRun)``，提交 ``$($baselineMeta.sourceCommit)``，归档于 $archivedAt")
} else {
    [void]$builder.AppendLine("- 基线：``$baselinePath``")
}
if ($runMeta) {
    [void]$builder.AppendLine("- 本次：``$runPath``，提交 ``$($runMeta.commit)``，dirty=$($runMeta.dirty)，filter=``$($runMeta.filter)``")
} else {
    [void]$builder.AppendLine("- 本次：``$runPath``")
}
[void]$builder.AppendLine('')

$warnings = New-Object System.Collections.Generic.List[string]
if ($baselineMeta -and $runMeta) {
    $envBaseline = $baselineMeta.environment
    $envRun = $runMeta.environment
    if ($envBaseline) {
        foreach ($key in @('cpu', 'os', 'dotnet')) {
            if ($envBaseline.$key -and $envRun.$key -and $envBaseline.$key -ne $envRun.$key) {
                $warnings.Add("环境 `$key` 不同（基线: $($envBaseline.$key) / 本次: $($envRun.$key)）→ 时间百分比不可比，只能看分配量")
            }
        }
    }
    if ($baselineMeta.scope -and $runMeta) {
        if ($baselineMeta.scope.filter -ne $runMeta.filter) { $warnings.Add("filter 不同（基线: $($baselineMeta.scope.filter) / 本次: $($runMeta.filter)）→ 只比两边都有的行") }
        if ($baselineMeta.scope.targetFramework -ne $runMeta.targetFramework) { $warnings.Add("TFM 不同（基线: $($baselineMeta.scope.targetFramework) / 本次: $($runMeta.targetFramework)）→ 不可比") }
    }
}
if ($baselineMeta -and $baselineMeta.probe -and $baselineMeta.probe.sourceSha256) {
    $probePath = Join-Path $RepoRoot 'JiksLib.Benchmark/Probes/FootprintProbe.cs'
    if (Test-Path $probePath) {
        $currentProbeHash = (Get-FileHash $probePath -Algorithm SHA256).Hash.ToLowerInvariant()
        if ($currentProbeHash -ne $baselineMeta.probe.sourceSha256) {
            $warnings.Add("探针源码已变（基线 SHA-256: $($baselineMeta.probe.sourceSha256.Substring(0, 12))… / 当前: $($currentProbeHash.Substring(0, 12))…）→ 碎片率/保留内存不可比")
        }
    }
}
foreach ($warning in $warnings) { [void]$builder.AppendLine("> ⚠ $warning") }
if ($warnings.Count -gt 0) { [void]$builder.AppendLine('') }

[void]$builder.AppendLine('| 基准（类.方法 [参数]） | Allocated 基线 → 本次 | Δ Allocated | Mean 基线 → 本次 | Δ Mean | Δ Median | 判定 |')
[void]$builder.AppendLine('| --- | --- | --- | --- | --- | --- | --- |')
foreach ($row in $display) {
    $baseAllocText = if ($null -ne $row.BaseAlloc) { Format-Bytes $row.BaseAlloc } else { '—' }
    $runAllocText = if ($null -ne $row.RunAlloc) { Format-Bytes $row.RunAlloc } else { '—' }
    $deltaAllocText = if ($null -ne $row.DeltaAlloc) { $sign = if ($row.DeltaAlloc -gt 0) { '+' } else { '' }; "$sign$(Format-Bytes $row.DeltaAlloc)" } else { '—' }
    $baseMeanText = if ($null -ne $row.BaseMean) { Format-Nanoseconds $row.BaseMean } else { '—' }
    $runMeanText = if ($null -ne $row.RunMean) { Format-Nanoseconds $row.RunMean } else { '—' }
    $verdictText = $row.Verdict
    if ($row.Note) { $verdictText += "（$($row.Note)）" }
    [void]$builder.AppendLine("| ``$($row.Class).$($row.Label)`` | $baseAllocText → $runAllocText | $deltaAllocText | $baseMeanText → $runMeanText | $(Format-Percent $row.MeanPct) | $(Format-Percent $row.MedianPct) | $verdictText |")
}
[void]$builder.AppendLine('')

$counts = $rows | Group-Object Verdict | ForEach-Object { "$($_.Name) $($_.Count)" }
[void]$builder.AppendLine("配对 $($rows.Count) 行（$($counts -join ' / ')）；仅本次 $($onlyRun.Count) 行；仅基线 $($onlyBaseline.Count) 行。")
if (-not $IncludeUnchanged) {
    $hidden = @($sorted | Where-Object { $_.Verdict -eq '无明显变化' }).Count
    if ($hidden -gt 0) { [void]$builder.AppendLine("已隐藏「无明显变化」$hidden 行（加 -IncludeUnchanged 查看）。") }
}
if ($Top -gt 0 -and @($sorted | Where-Object { $IncludeUnchanged -or $_.Verdict -ne '无明显变化' }).Count -gt $Top) {
    [void]$builder.AppendLine("仅显示前 $Top 行（-Top 0 查看全部）。")
}

if ($onlyRun.Count -gt 0) {
    [void]$builder.AppendLine('')
    [void]$builder.AppendLine('## 仅本次运行有的基准')
    [void]$builder.AppendLine('')
    foreach ($item in $onlyRun) { [void]$builder.AppendLine("- ``$item``") }
}
if ($onlyBaseline.Count -gt 0) {
    [void]$builder.AppendLine('')
    [void]$builder.AppendLine('## 仅基线有的基准')
    [void]$builder.AppendLine('')
    foreach ($item in $onlyBaseline) { [void]$builder.AppendLine("- ``$item``") }
}
if ($columnWarnings.Count -gt 0) {
    [void]$builder.AppendLine('')
    [void]$builder.AppendLine('## 参数列变化')
    [void]$builder.AppendLine('')
    foreach ($item in $columnWarnings) { [void]$builder.AppendLine("- $item") }
}

$text = $builder.ToString()
if ($OutFile) {
    if ([System.IO.Path]::IsPathRooted($OutFile)) {
        $fullOut = [System.IO.Path]::GetFullPath($OutFile)
    } else {
        $fullOut = [System.IO.Path]::GetFullPath((Join-Path (Get-Location).Path $OutFile))
    }
    [System.IO.File]::WriteAllText($fullOut, $text, (New-Object System.Text.UTF8Encoding($false)))
    Write-Host "已写入 $fullOut"
} else {
    Write-Output $text
}
