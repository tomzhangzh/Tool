# 文件行数闸门（棘轮基线）
#
# 目的：把「单文件承载多机制」挡在构建之前。阈值只约束框架自有代码，
# 第三方库（wwwroot/lib）与生成物（obj/bin）不参与。
#
# 规则：
#   - 未登记的文件：行数必须 <= -Threshold（默认 500）
#   - 已登记的文件：只许变小（棘轮），超过登记值即失败
#   - 重构到阈值以内的登记项：用 -Update 重新生成基线即可移除
#
# 用法：
#   powershell -File tools/check-file-size.ps1            # 检查（CI 用，违规退出码 1）
#   powershell -File tools/check-file-size.ps1 -Update    # 重算基线（仅在重构后下调时使用）

param(
    [string]$Root = (Split-Path $PSScriptRoot -Parent),
    [int]$Threshold = 500,
    [switch]$Update
)

$ErrorActionPreference = 'Stop'
$Root = (Resolve-Path $Root).Path
$baselinePath = Join-Path $Root 'tools/file-size-baseline.json'

# 扫描范围：框架自有 JS / C# / Razor；排除第三方库、生成目录、迁移脚本
$includeDirs = @('src', 'tools')
$skipPathPart = @(
    '\obj\', '\bin\',
    '\wwwroot\lib\',
    '\wwwroot\lib\'
)
$skipName = @('*.min.js', '*.g.cs', '*.Designer.cs', '*.AssemblyInfo.cs', 'AssemblyAttributes.cs')

function Get-TargetFiles {
    $files = @()
    foreach ($dir in $includeDirs) {
        $full = Join-Path $Root $dir
        if (-not (Test-Path $full)) { continue }
        $files += Get-ChildItem -LiteralPath $full -Recurse -File -Include *.js, *.cs, *.cshtml -ErrorAction SilentlyContinue
    }
    $files | Where-Object {
        $p = $_.FullName
        $skip = $false
        foreach ($part in $skipPathPart) { if ($p.Contains($part)) { $skip = $true; break } }
        foreach ($n in $skipName) { if ($_.Name -like $n) { $skip = $true; break } }
        # 本闸门自身与基线文件不计
        if ($_.Name -eq 'check-file-size.ps1' -or $_.FullName -eq $baselinePath) { $skip = $true }
        -not $skip
    } | Sort-Object FullName
}

# PS 5.1 没有 [IO.Path]::GetRelativePath，按目录前缀裁剪（仓库内路径大小写不敏感）
function Get-RelPath([string]$baseDir, [string]$full) {
    $base = $baseDir.TrimEnd('\') + '\'
    if ($full.StartsWith($base, [StringComparison]::OrdinalIgnoreCase)) {
        return $full.Substring($base.Length).Replace('\', '/')
    }
    return $full.Replace('\', '/')
}

function Get-LineCount([string]$path) {
    $n = (Get-Content -LiteralPath $path -ErrorAction SilentlyContinue | Measure-Object -Line).Lines
    if ($null -eq $n) { return 0 }
    return [int]$n
}

$files = Get-TargetFiles
$entries = @{}

foreach ($f in $files) {
    $rel = Get-RelPath $Root $f.FullName
    $lines = Get-LineCount $f.FullName
    if ($lines -gt $Threshold) { $entries[$rel] = $lines }
}

if ($Update) {
    $json = [ordered]@{
        _comment = '文件行数棘轮基线：登记项只许变小；重构到阈值以内后重跑 -Update 会自动移除。'
        threshold = $Threshold
        files = $entries
    }
    ($json | ConvertTo-Json -Depth 5) | Set-Content -LiteralPath $baselinePath -Encoding UTF8
    Write-Output "已更新基线：$($entries.Count) 个文件超过 $Threshold 行 -> $baselinePath"
    exit 0
}

$baseline = @{}
if (Test-Path $baselinePath) {
    $data = Get-Content -LiteralPath $baselinePath -Raw -Encoding UTF8 | ConvertFrom-Json
    if ($data.files) {
        foreach ($p in $data.files.PSObject.Properties) { $baseline[$p.Name] = [int]$p.Value }
    }
}

$violations = @()
foreach ($f in $files) {
    $rel = Get-RelPath $Root $f.FullName
    $lines = Get-LineCount $f.FullName
    if ($baseline.ContainsKey($rel)) {
        $limit = $baseline[$rel]
        if ($lines -gt $limit) { $violations += "$rel 共 $lines 行，超过登记上限 $limit（棘轮：只许变小）" }
    }
    elseif ($lines -gt $Threshold) {
        $violations += "$rel 共 $lines 行，超过阈值 $Threshold（请拆分，或确属合理后跑 -Update 登记）"
    }
}

$stale = @($baseline.Keys | Where-Object {
        $abs = Join-Path $Root ($_ -replace '/', '\')
        -not (Test-Path -LiteralPath $abs) -or (Get-LineCount $abs) -le $Threshold
    })

Write-Output "== 文件行数闸门（阈值 $Threshold 行，扫描 $($files.Count) 个文件，基线 $($baseline.Count) 项）=="
if ($stale.Count -gt 0) { Write-Output "提示：以下登记项已达标或不存在，可用 -Update 清理：$($stale -join ', ')" }

if ($violations.Count -eq 0) {
    Write-Output '结果：通过（0 违规）'
    exit 0
}

$violations | ForEach-Object { Write-Output "  ✗ $_" }
Write-Output "结果：失败（$($violations.Count) 违规）"
exit 1
