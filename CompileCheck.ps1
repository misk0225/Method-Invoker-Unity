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

# 只編譯，不運行測試
& $unityExe -quit -batchmode -projectPath $projectPath -executeMethod UnityEditor.EditorApplication.Exit -logFile CompileCheck.log

Start-Sleep -Seconds 5

if (Test-Path CompileCheck.log) {
    $errors = Get-Content CompileCheck.log | Select-String -Pattern "error CS|Error:|Exception:"
    if ($errors) {
        Write-Host "發現編譯錯誤:" -ForegroundColor Red
        $errors | ForEach-Object { Write-Host $_.Line -ForegroundColor Yellow }
    } else {
        Write-Host "編譯成功，無錯誤!" -ForegroundColor Green
    }
}
