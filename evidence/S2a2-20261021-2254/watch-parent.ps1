# read-only parent-process watcher (S2a-2, item 3)
# Polls the process table, keeps a rolling PID -> process-info map (so a parent
# that already exited is still identifiable), and on the first sighting of the
# game writes: ancestor chain, full process snapshot, foreground window owner,
# and the tails of the Steam logs. Writes to the evidence dir. Never starts,
# kills or writes to any process. Exit: first launch recorded, or
# parent-watch.stop file, or deadline.
$ErrorActionPreference = 'SilentlyContinue'
$dir = 'C:\Users\memph\LongYinCoop\evidence\S2a2-20261021-2254'
$out = Join-Path $dir 'parent-watch.ndjson'
$hb  = Join-Path $dir 'parent-watch-heartbeat.txt'
$stop = Join-Path $dir 'parent-watch.stop'

Add-Type @"
using System;
using System.Runtime.InteropServices;
public class FgWin {
  [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
  [DllImport("user32.dll")] public static extern int GetWindowTextLength(IntPtr h);
  [DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern int GetWindowText(IntPtr h, System.Text.StringBuilder s, int n);
  [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
}
"@
function FgInfo {
  $h = [FgWin]::GetForegroundWindow()
  if ($h -eq [IntPtr]::Zero) { return @{ pid = 0; title = '' } }
  $sb = New-Object System.Text.StringBuilder 512
  [void][FgWin]::GetWindowText($h, $sb, 512)
  $p = 0
  [void][FgWin]::GetWindowThreadProcessId($h, [ref]$p)
  return @{ pid = [int]$p; title = $sb.ToString() }
}
function ProcessMap {
  $m = @{}
  foreach ($p in (Get-CimInstance Win32_Process)) { $m[[int]$p.ProcessId] = $p }
  return $m
}
function Chain([hashtable]$map, [int]$start, [int]$max) {
  $rows = @()
  $cur = $start
  for ($i = 0; $i -lt $max -and $cur -gt 0; $i++) {
    if ($map.ContainsKey($cur)) {
      $p = $map[$cur]
      $rows += [ordered]@{ depth = $i + 1; pid = $cur; ppid = [int]$p.ParentProcessId; name = $p.Name; created = ([string]$p.CreationDate); cmd = ([string]$p.CommandLine) }
      $cur = [int]$p.ParentProcessId
    } else {
      $rows += [ordered]@{ depth = $i + 1; pid = $cur; ppid = 0; name = '(not seen / gone)'; created = ''; cmd = '' }
      break
    }
  }
  return $rows
}
function LogTail([string]$path, [int]$n) {
  if (-not (Test-Path $path)) { return @() }
  return @(Get-Content $path -Tail $n)
}

$known = @{}
$deadline = (Get-Date).AddHours(8)
$recorded = $false
while (-not (Test-Path $stop) -and -not $recorded -and (Get-Date) -lt $deadline) {
  $map = ProcessMap
  foreach ($k in $map.Keys) { $known[$k] = $map[$k] }
  $game = @($map.Values | Where-Object { $_.Name -eq 'LongYinLiZhiZhuan.exe' })
  if ($game.Count -gt 0) {
    $g = $game | Sort-Object CreationDate | Select-Object -First 1
    $fg = FgInfo
    $fgp = $known[[int]$fg.pid]
    $fgName = ''
    $fgCmd = ''
    if ($fgp) { $fgName = [string]$fgp.Name; $fgCmd = [string]$fgp.CommandLine }
    $ts = (Get-Date).ToString('yyyy-MM-dd HH:mm:ss.fff')
    $snapFile = Join-Path $dir ("parent-watch-procs-{0}.txt" -f (Get-Date -Format 'yyyyMMdd-HHmmss'))
    $map.Values | Sort-Object ProcessId | ForEach-Object {
      "{0,7} {1,7} {2,-28} {3} :: {4}" -f $_.ProcessId, $_.ParentProcessId, $_.Name, ([string]$_.CreationDate), ([string]$_.CommandLine)
    } | Set-Content -Path $snapFile -Encoding UTF8
    $rec = [ordered]@{
      ts = $ts
      gamePid = [int]$g.ProcessId
      gamePpid = [int]$g.ParentProcessId
      gameCreated = [string]$g.CreationDate
      gameCmd = [string]$g.CommandLine
      ancestorChain = @(Chain $known ([int]$g.ProcessId) 6)
      foregroundWindow = @{ pid = [int]$fg.pid; title = $fg.title; procName = $fgName; procCmd = $fgCmd }
      steamConsoleTail = @(LogTail 'C:\Program Files (x86)\Steam\logs\console_log.txt' 25)
      steamGameProcessTail = @(LogTail 'C:\Program Files (x86)\Steam\logs\gameprocess_log.txt' 15)
      processSnapshot = $snapFile
    }
    ($rec | ConvertTo-Json -Depth 6 -Compress) | Add-Content -Path $out -Encoding UTF8
    $recorded = $true
    break
  }
  (Get-Date).ToString('yyyy-MM-dd HH:mm:ss.fff') + " waiting" | Set-Content -Path $hb -Encoding ASCII
  Start-Sleep -Milliseconds 500
}
if ($recorded) {
  "recorded $ts" | Set-Content -Path $hb -Encoding ASCII
} else {
  ("stopped without sighting at " + (Get-Date).ToString('yyyy-MM-dd HH:mm:ss')) | Set-Content -Path $hb -Encoding ASCII
}
