# 簡單編譯檢查腳本
$unityPath = "C:\Program Files\Unity\Hub\Editor\*\Editor\Unity.exe"
$unityExe = Get-ChildItem $unityPath -ErrorAction SilentlyContinue | Select-Object -First 1 -ExpandProperty FullName

if (-not $unityExe) {
    Write-Host "找不到 Unity" -ForegroundColor Red
    exit 1
}

# 自動偵測 Unity 專案路徑：從腳本所在目錄向上尋找包含 Assets 資料夾的目錄
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$currentPath = $scriptPath
$projectPath = $null

while ($currentPath) {
    if (Test-Path (Join-Path $currentPath "Assets")) {
        $projectPath = $currentPath
        break
    }
    $parentPath = Split-Path -Parent $currentPath
    if ($parentPath -eq $currentPath) { break }  # 已到達根目錄
    $currentPath = $parentPath
}

if (-not $projectPath) {
    Write-Host "找不到 Unity 專案根目錄 (包含 Assets 資料夾的目錄)" -ForegroundColor Red
    Write-Host "當前腳本位置: $scriptPath" -ForegroundColor Yellow
    exit 1
}

Write-Host "編譯檢查..." -ForegroundColor Green
Write-Host "Unity 路徑: $unityExe" -ForegroundColor Cyan
Write-Host "專案路徑: $projectPath" -ForegroundColor Cyan

# 設定日誌檔案的完整路徑（在腳本所在目錄）
$logFile = Join-Path $scriptPath "CompileCheck.log"

# 清理舊的日誌檔案
if (Test-Path $logFile) { 
    Remove-Item $logFile -Force
    Write-Host "已清理舊的日誌檔案" -ForegroundColor Gray
}

# 只編譯，不運行測試（使用 -quit 讓 Unity 編譯後自動退出）
Write-Host "`n執行編譯檢查..." -ForegroundColor Yellow
$process = Start-Process -FilePath $unityExe -ArgumentList @(
    "-quit",
    "-batchmode",
    "-nographics",
    "-projectPath", $projectPath,
    "-logFile", $logFile
) -NoNewWindow -PassThru -Wait

Write-Host "`n編譯完成，退出碼: $($process.ExitCode)" -ForegroundColor $(if ($process.ExitCode -eq 0) { "Green" } else { "Red" })

# 等待日誌檔案寫入完成
Start-Sleep -Seconds 2

if (Test-Path $logFile) {
    Write-Host "`n=== 分析編譯結果 ===" -ForegroundColor Cyan
    
    # 檢查真正的編譯錯誤（排除授權相關錯誤）
    $content = Get-Content $logFile -Raw
    $compileErrors = Get-Content $logFile | Select-String -Pattern "error CS\d+:|CompilerOutput:|Assembly-CSharp.*failed"
    
    # 檢查是否有腳本編譯錯誤
    $hasScriptErrors = $content -match "Scripts have compiler errors"
    
    if ($compileErrors -or $hasScriptErrors) {
        Write-Host "發現編譯錯誤:" -ForegroundColor Red
        if ($compileErrors) {
            $compileErrors | ForEach-Object { Write-Host $_.Line -ForegroundColor Yellow }
        }
        if ($hasScriptErrors) {
            Write-Host "腳本存在編譯錯誤，請檢查日誌檔案: $logFile" -ForegroundColor Yellow
        }
        exit 1
    } else {
        Write-Host "編譯成功，無錯誤!" -ForegroundColor Green
        exit 0
    }
} else {
    Write-Host "錯誤: 日誌檔案未生成" -ForegroundColor Red
    exit 1
}
