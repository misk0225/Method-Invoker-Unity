# Unity 測試運行腳本 (支援授權處理)
# 使用方法: .\RunTests.ps1

$unityPath = "C:\Program Files\Unity\Hub\Editor\*\Editor\Unity.exe"
$unityExe = Get-ChildItem $unityPath -ErrorAction SilentlyContinue | Select-Object -First 1 -ExpandProperty FullName

if (-not $unityExe) {
    Write-Host "找不到 Unity 編輯器，請手動指定路徑" -ForegroundColor Red
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
$testPlatform = "EditMode"
# 測試結果檔案放在專案根目錄
$testResults = Join-Path $projectPath "TestResults.xml"
# 日誌檔案放在腳本所在目錄
$logFile = Join-Path $scriptPath "TestRun.log"

Write-Host "運行測試..." -ForegroundColor Green
Write-Host "Unity 路徑: $unityExe" -ForegroundColor Cyan
Write-Host "專案路徑: $projectPath" -ForegroundColor Cyan
Write-Host "測試結果: $testResults" -ForegroundColor Cyan
Write-Host "日誌檔案: $logFile" -ForegroundColor Cyan

# 清理舊的結果文件
if (Test-Path $testResults) { 
    Remove-Item $testResults -Force
    Write-Host "已清理舊的測試結果" -ForegroundColor Gray
}
if (Test-Path $logFile) { 
    Remove-Item $logFile -Force
    Write-Host "已清理舊的日誌檔案" -ForegroundColor Gray
}

# 運行測試，加入 -nographics 減少資源消耗，使用 -username 和 -password 或 -serial
# 如果有個人授權，使用 -username 和 -password
# 如果是 Plus/Pro，使用 -serial
$arguments = @(
    "-runTests",
    "-batchmode",
    "-nographics",
    "-silent-crashes",
    "-projectPath", $projectPath,
    "-testPlatform", $testPlatform,
    "-testResults", (Split-Path $testResults -Leaf),
    "-logFile", $logFile
)

Write-Host "執行命令: $unityExe $($arguments -join ' ')" -ForegroundColor Yellow

$process = Start-Process -FilePath $unityExe -ArgumentList $arguments -NoNewWindow -PassThru -Wait

Write-Host "`n測試完成，退出碼: $($process.ExitCode)" -ForegroundColor $(if ($process.ExitCode -eq 0) { "Green" } else { "Red" })

# 顯示測試結果
if (Test-Path $testResults) {
    Write-Host "`n=== 測試結果 ===" -ForegroundColor Cyan
    try {
        [xml]$xmlContent = Get-Content $testResults
        $testRun = $xmlContent.'test-run'
        
        Write-Host "總測試數: $($testRun.total)" -ForegroundColor White
        Write-Host "通過: $($testRun.passed)" -ForegroundColor Green
        Write-Host "失敗: $($testRun.failed)" -ForegroundColor $(if ([int]$testRun.failed -eq 0) { "Green" } else { "Red" })
        Write-Host "跳過: $($testRun.skipped)" -ForegroundColor Yellow
        Write-Host "執行時間: $($testRun.duration) 秒" -ForegroundColor Cyan
        
        # 顯示失敗的測試
        if ([int]$testRun.failed -gt 0) {
            Write-Host "`n=== 失敗的測試 ===" -ForegroundColor Red
            $failures = $xmlContent.SelectNodes("//test-case[@result='Failed']")
            foreach ($failure in $failures) {
                Write-Host "`n測試: $($failure.name)" -ForegroundColor Yellow
                Write-Host "完整名稱: $($failure.fullname)" -ForegroundColor Gray
                
                # 讀取 CDATA 內容
                $message = $failure.failure.message.'#cdata-section'
                if (-not $message) { $message = $failure.failure.message }
                Write-Host "錯誤訊息:" -ForegroundColor Red
                Write-Host $message -ForegroundColor Yellow
                
                if ($failure.failure.'stack-trace') {
                    $stackTrace = $failure.failure.'stack-trace'.'#cdata-section'
                    if (-not $stackTrace) { $stackTrace = $failure.failure.'stack-trace' }
                    Write-Host "`n堆疊追蹤:" -ForegroundColor Gray
                    Write-Host $stackTrace -ForegroundColor DarkGray
                }
            }
        } else {
            Write-Host "`n✓ 所有測試通過！" -ForegroundColor Green
        }
    } catch {
        Write-Host "`n解析測試結果時發生錯誤: $_" -ForegroundColor Red
        Write-Host "測試結果檔案位置: $testResults" -ForegroundColor Yellow
    }
} else {
    Write-Host "`n測試結果檔案未生成" -ForegroundColor Red
    Write-Host "預期位置: $testResults" -ForegroundColor Yellow
    if (Test-Path $logFile) {
        Write-Host "`n請檢查日誌檔案以了解詳情: $logFile" -ForegroundColor Yellow
    }
}

exit $process.ExitCode
