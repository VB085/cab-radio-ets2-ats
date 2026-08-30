# add-stream.ps1
# 向 ATS / ETS2 的 live_streams.sii 幂等插入「QQ Music Radio」条目
# （Companion 的 SiiWriter 兜底通道的原型脚本，M0 阶段代替手动编辑）
#
# 注意：请在游戏关闭时运行。
#
# 用法：
#   powershell -ExecutionPolicy Bypass -File scripts\add-stream.ps1                  # 两个游戏都处理
#   powershell -ExecutionPolicy Bypass -File scripts\add-stream.ps1 -Game ats        # 只处理 ATS
#   powershell -ExecutionPolicy Bypass -File scripts\add-stream.ps1 -Bitrate 192     # 改码率字段

param(
    [ValidateSet("ats", "ets2", "both")]
    [string]$Game = "both",
    [string]$Url = "http://127.0.0.1:17890/stream.mp3",
    [string]$Name = "Cab Radio",
    [string]$Genre = "Music",
    [string]$Lang = "CN",
    [string]$Bitrate = "128"
)

$ErrorActionPreference = "Stop"

$targets = @()
if ($Game -in @("ats", "both")) { $targets += [IO.Path]::Combine($env:USERPROFILE, "Documents", "American Truck Simulator", "live_streams.sii") }
if ($Game -in @("ets2", "both")) { $targets += [IO.Path]::Combine($env:USERPROFILE, "Documents", "Euro Truck Simulator 2", "live_streams.sii") }

$entry = "$Url|$Name|$Genre|$Lang|$Bitrate|1"

foreach ($path in $targets) {
    if (-not (Test-Path $path)) {
        Write-Warning "跳过（文件不存在）: $path"
        Write-Warning "    请先运行一次对应游戏，并在电台面板点 Streaming -> Update From Internet"
        continue
    }

    $content = [IO.File]::ReadAllText($path)

    # 情况 1：已有本项目条目（按 localhost 特征识别）→ 原地替换，保留原索引
    if ($content -match 'stream_data\[\d+\]:\s*"[^"]*127\.0\.0\.1:\d+[^"]*"') {
        $content = [regex]::Replace($content,
            'stream_data\[(\d+)\]:\s*"[^"]*127\.0\.0\.1:\d+[^"]*"',
            { param($m) 'stream_data[{0}]: "{1}"' -f $m.Groups[1].Value, $entry })
        Write-Host "[已更新] $path"
    }
    # 情况 2：新条目 → 追加到最后一条 stream_data 条目之后（保证落在 live_stream_def 块内），并同步顶部计数
    else {
        $indices = [regex]::Matches($content, 'stream_data\[(\d+)\]') | ForEach-Object { [int]$_.Groups[1].Value }
        $newIndex = if ($indices.Count -gt 0) { ($indices | Measure-Object -Maximum).Maximum + 1 } else { 0 }

        $countMatch = [regex]::Match($content, 'stream_data:\s*(\d+)')
        if ($countMatch.Success) {
            $newCount = [int]$countMatch.Groups[1].Value + 1
            $content = [regex]::Replace($content, 'stream_data:\s*(\d+)', "stream_data: $newCount", 1)
        }

        $insert = ' stream_data[{0}]: "{1}"' -f $newIndex, $entry
        $entryMatches = [regex]::Matches($content, 'stream_data\[\d+\]:\s*"[^"]*"\r?\n')
        if ($entryMatches.Count -gt 0) {
            $lastEntry = $entryMatches[$entryMatches.Count - 1]
            $content = $content.Insert($lastEntry.Index + $lastEntry.Length, $insert + "`r`n")
        }
        else {
            Write-Error "未找到可插入的条目位置: $path"; continue
        }
        Write-Host "[已新增] $path"
    }

    # 备份 + 写回（无 BOM 的 UTF-8，与游戏生成的文件保持一致）
    Copy-Item $path "$path.bak" -Force
    [IO.File]::WriteAllText($path, $content, (New-Object System.Text.UTF8Encoding($false)))
}

Write-Host "完成。启动游戏后如列表未刷新，在电台面板点 Streaming -> Update From Internet。"
