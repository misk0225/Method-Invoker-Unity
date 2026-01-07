# Unity CLI 測試執行指南

本文檔說明如何使用命令行介面 (CLI) 執行 Unity 單元測試。

## 前置需求

- Unity Editor 已安裝（透過 Unity Hub）
- PowerShell 5.0 或更高版本
- **專案路徑會自動偵測**：腳本會從所在位置向上尋找包含 `Assets` 資料夾的 Unity 專案根目錄

## 測試腳本說明

專案中包含兩個 PowerShell 腳本：

### 1. RunTests.ps1 - 執行單元測試

用於在 Unity 批次模式下執行所有單元測試並生成測試報告。

**名稱**：`RunTests.ps1`

**使用方法**：
```powershell
# 從 Method Invoker 工具所在目錄執行
cd "專案路徑\Assets\Method-Invoker-Unity"
.\RunTests.ps1

# 或從任何包含此腳本的目錄執行（會自動偵測專案路徑）
powershell -ExecutionPolicy Bypass -File .\RunTests.ps1
```

**功能**：
- **自動偵測 Unity 專案路徑**（向上尋找包含 Assets 資料夾的目錄）
- 自動尋找 Unity Editor 執行檔
- 以批次模式運行 EditMode 測試
- 生成 `TestResults.xml` 測試報告（位於專案根目錄）
- 生成 `TestRun.log` 詳細日誌

**輸出檔案**：
- `{專案根目錄}\TestResults.xml` - NUnit 格式的測試結果
- `{專案根目錄}\TestRun.log` - Unity 執行日誌

### 2. CompileCheck.ps1 - 編譯檢查

用於快速檢查專案是否有編譯錯誤。

**位置**：`d:\unity\Test\Assets\MethodInvoker\CompileCheck.ps1`

**使用方法**：
```powershell
# 從 Method Invoker 工具所在目錄執行
cd "專案路徑\Assets\Method-Invoker-Unity"
.\CompileCheck.ps1

# 或從任何包含此腳本的目錄執行（會自動偵測專案路徑）
powershell -ExecutionPolicy Bypass -File .\CompileCheck.ps1
```

**功能**：
- **自動偵測 Unity 專案路徑**（向上尋找包含 Assets 資料夾的目錄）
- 啟動 Unity 並編譯專案
- 檢測編譯錯誤並顯示
- 比完整測試更快

## 查看測試結果

### 方法 1: 使用 PowerShell 解析 XML

```powershell
# 查看測試總結
[xml]$xml = Get-Content "{專案根目錄}\TestResults.xml"
$testRun = $xml.'test-run'
Write-Host "總測試數: $($testRun.total)"
Write-Host "通過: $($testRun.passed)" -ForegroundColor Green
Write-Host "失敗: $($testRun.failed)" -ForegroundColor Red
Write-Host "跳過: $($testRun.skipped)" -ForegroundColor Yellow

# 查看失敗的測試
$xml.SelectNodes("//test-case[@result='Failed']") | ForEach-Object {
    Write-Host "`n測試: $($_.name)" -ForegroundColor Red
    Write-Host "錯誤: $($_.'failure'.'message')"
}
```

### 方法 2: 直接查看 XML 文件

使用任何文本編輯器或 XML 查看器打開 `{專案根目錄}\TestResults.xml`。

### 方法 3: 查看詳細日誌

```powershell
# 查看最後 50 行日誌
Get-Content "TestRun.log" | Select-Object -Last 50

# 搜尋錯誤
Get-Content "TestRun.log" | Select-String -Pattern "error|exception|failed" -Context 2
```

## 故障排除

### 問題 1: 找不到 Unity 執行檔

**錯誤訊息**: `找不到 Unity 編輯器，請手動指定路徑`

**解決方法**: 手動修改腳本中的 Unity 路徑
```powershell
# 在 RunTests.ps1 或 CompileCheck.ps1 中修改：
$unityExe = "C:\Program Files\Unity\Hub\Editor\2022.3.18f1\Editor\Unity.exe"
```

### 問題 2: 找不到 Unity 專案根目錄

**錯誤訊息**: `找不到 Unity 專案根目錄 (包含 Assets 資料夾的目錄)`

**原因**: 腳本無法從當前位置向上找到包含 `Assets` 資料夾的目錄

**解決方法**: 
1. 確認您的專案結構正確（應包含 `Assets` 資料夾）
2. 確認腳本位於專案的子目錄中
3. 如果專案結構特殊，可以手動指定專案路徑：
```powershell
# 在腳本中手動設定
$projectPath = "您的專案完整路徑"
```

### 問題 3: 測試失敗但無錯誤訊息

**症狀**: XML 中 `<failure><message></message>` 為空

**可能原因**:
- Unity 批次模式下異常沒有被正確捕獲
- 需要查看 `TestRun.log` 獲取詳細堆疊追蹤

**解決方法**:
```powershell
# 搜尋 "Exception" 或 "ArgumentException"
Get-Content "TestRun.log" | Select-String -Pattern "Exception" -Context 5
```

### 問題 4: Unity 授權問題

**錯誤訊息**: `License error` 或 `Activation required`

**解決方法**: 
- 先在 Unity Editor 中手動打開專案一次
- 確保 Unity 已正確啟動授權

### 問題 5: 套件依賴錯誤

**錯誤訊息**: `Package 'com.unity.xxx' not found`

**解決方法**: 
```powershell
# 檢查並修復 manifest.json
code "d:\unity\Test\Packages\manifest.json"
# 移除不存在的套件引用
```

## 在 Unity Editor 中運行測試（對比用）

如果 CLI 測試結果異常，可以在 Unity Editor 中對比：

1. 開啟 Unity Editor 並載入專案
2. 打開 **Window > General > Test Runner**
3. 選擇 **EditMode** 標籤
4. 點擊 **Run All** 按鈕
5. 查看詳細的錯誤訊息和堆疊追蹤

Editor 中的測試結果通常包含更詳細的錯誤資訊。

## 整合到 CI/CD

### 在 Jenkins/GitLab CI 中使用

```yaml
# .gitlab-ci.yml 範例
test:
  stage: test
  script:
    - cd "d:/unity/Test/Assets/MethodInvoker"
    - powershell -File RunTests.ps1
    - if ($LASTEXITCODE -ne 0) { exit 1 }
  artifacts:
    when: always
    reports:
      junit: d:/unity/Test/TestResults.xml
    paths:
      - TestRun.log
```

### 批次執行多個專案

```powershell
# TestAll.ps1
$projects = @(
    "d:\unity\Project1",
    "d:\unity\Project2"
)

foreach ($project in $projects) {
    Write-Host "測試專案: $project" -ForegroundColor Cyan
    cd "$project\Assets\MethodInvoker"
    .\RunTests.ps1
    if ($LASTEXITCODE -ne 0) {
        Write-Host "專案測試失敗: $project" -ForegroundColor Red
    }
}
```

## 效能最佳化

### 加快測試執行速度

1. **使用 `-nographics` 標誌** (已包含在 RunTests.ps1 中)
2. **僅運行特定測試**：
```powershell
# 修改 RunTests.ps1，添加 testFilter 參數
& $unityExe -runTests -batchmode -projectPath $projectPath `
    -testPlatform $testPlatform `
    -testFilter "MethodInvoker.Tests.MethodInvokerTests.Test_IntParameterMethod_CanInvoke" `
    -testResults $testResults -logFile TestRun.log
```

3. **並行執行** (如果有多個測試組件)：
```powershell
# 使用 PowerShell Jobs
Start-Job -ScriptBlock { cd "path1"; .\RunTests.ps1 }
Start-Job -ScriptBlock { cd "path2"; .\RunTests.ps1 }
Get-Job | Wait-Job | Receive-Job
```

## 常用命令快速參考

```powershell
# 執行測試
.\RunTests.ps1

# 僅檢查編譯
.\CompileCheck.ps1

# 查看測試統計
[xml]$xml = Get-Content "d:\unity\Test\TestResults.xml"
$xml.'test-run' | Select-Object total, passed, failed, skipped

# 列出失敗的測試名稱
([xml](Get-Content "d:\unity\Test\TestResults.xml")).SelectNodes("//test-case[@result='Failed']").name

# 清理測試輸出
Remove-Item TestResults.xml, TestRun.log, CompileCheck.log -ErrorAction SilentlyContinue
```

## 測試覆蓋率 (未來擴展)

Unity 2022+ 支援代碼覆蓋率，可透過以下方式啟用：

```powershell
# 在 RunTests.ps1 中添加覆蓋率參數
& $unityExe -runTests -batchmode -projectPath $projectPath `
    -testPlatform $testPlatform `
    -enableCodeCoverage `
    -coverageResultsPath "CodeCoverage" `
    -testResults $testResults -logFile TestRun.log
```

## 聯絡資訊

如有問題或建議，請聯絡開發團隊。

---

**最後更新**: 2026-01-07  
**適用版本**: Unity 2022.3.18f1  
**測試框架**: NUnit 3.5.0
