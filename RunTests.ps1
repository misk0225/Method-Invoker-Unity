# Unity 測試運行腳本 (支援授權處理)
# 使用方法: .\RunTests.ps1

$unityPath = "C:\Program Files\Unity\Hub\Editor\*\Editor\Unity.exe"
$unityExe = Get-ChildItem $unityPath -ErrorAction SilentlyContinue | Select-Object -First 1 -ExpandProperty FullName

if (-not $unityExe) {
    Write-Host "找不到 Unity 編輯器，請手動指定路徑" -ForegroundColor Red
    exit 1
}

$projectPath = "d:\unity\Test"
$testPlatform = "EditMode"
$testResults = "TestResults.xml"
$logFile = "TestRun.log"

Write-Host "運行測試..." -ForegroundColor Green
Write-Host "Unity 路徑: $unityExe" -ForegroundColor Cyan
Write-Host "專案路徑: $projectPath" -ForegroundColor Cyan

# 清理舊的結果文件
if (Test-Path $testResults) { Remove-Item $testResults }
if (Test-Path $logFile) { Remove-Item $logFile }

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
    "-testResults", $testResults,
    "-logFile", $logFile
)

Write-Host "執行命令: $unityExe $($arguments -join ' ')" -ForegroundColor Yellow

$process = Start-Process -FilePath $unityExe -ArgumentList $arguments -NoNewWindow -PassThru -Wait

Write-Host "`n測試完成，退出碼: $($process.ExitCode)" -ForegroundColor $(if ($process.ExitCode -eq 0) { "Green" } else { "Red" })

# 顯示測試結果
if (Test-Path $testResults) {
    Write-Host "`n=== 測試結果 ===" -ForegroundColor Cyan
    [xml]$xmlContent = Get-Content $testResults
    $testRun = $xmlContent.'test-run'
    
    Write-Host "總測試數: $($testRun.total)" -ForegroundColor White
    Write-Host "通過: $($testRun.passed)" -ForegroundColor Green
    Write-Host "失敗: $($testRun.failed)" -ForegroundColor $(if ($testRun.failed -eq 0) { "Green" } else { "Red" })
    Write-Host "跳過: $($testRun.skipped)" -ForegroundColor Yellow
    
    # 顯示失敗的測試
    if ([int]$testRun.failed -gt 0) {
        Write-Host "`n=== 失敗的測試 ===" -ForegroundColor Red
        $failures = $xmlContent.SelectNodes("//test-case[@result='Failed']")
        foreach ($failure in $failures) {
            Write-Host "`n測試: $($failure.fullname)" -ForegroundColor Yellow
            Write-Host "訊息: $($failure.failure.message)" -ForegroundColor Red
            if ($failure.failure.'stack-trace') {
                Write-Host "堆疊追蹤:" -ForegroundColor Gray
                Write-Host $failure.failure.'stack-trace' -ForegroundColor DarkGray
            }
        }
    }
} else {
    Write-Host "`n測試結果文件未生成" -ForegroundColor Red
}

# 顯示日誌摘要
if (Test-Path $logFile) {
    Write-Host "`n=== 日誌摘要 (最後 30 行) ===" -ForegroundColor Cyan
    Get-Content $logFile | Select-Object -Last 30
    
    # 檢查授權錯誤
    $licenseErrors = Get-Content $logFile | Select-String -Pattern "license|licensing|activation" -CaseSensitive:$false
    if ($licenseErrors) {
        Write-Host "`n=== 發現授權相關訊息 ===" -ForegroundColor Yellow
        $licenseErrors | ForEach-Object { Write-Host $_.Line -ForegroundColor Yellow }
        Write-Host "`n提示: 如果是授權問題，請確保:" -ForegroundColor Cyan
        Write-Host "  1. Unity Hub 已登入" -ForegroundColor White
        Write-Host "  2. 專案已在 Unity Hub 中啟用授權" -ForegroundColor White
        Write-Host "  3. 或使用 -username/-password 參數" -ForegroundColor White
    }
}

exit $process.ExitCode
