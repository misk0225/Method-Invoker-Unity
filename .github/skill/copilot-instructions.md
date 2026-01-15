## 介紹
這是一款 Unity 編輯器工具，解決了 Unity 內建檢視器無法直接呼叫帶有參數的方法的限制。該工具提供了一個直觀的可視化介面，使開發者能夠在編輯模式和執行時間下輕鬆呼叫任何公共方法。

## 主要功能

### 核心功能
- **方法呼叫介面**：提供直觀的編輯器視窗，可視化選擇並呼叫 GameObject 上的任何公共方法
- **參數編輯**：內建參數編輯器，支援在呼叫前設定方法參數
- **執行時與編輯模式**：可在 Play Mode 和 Edit Mode 下使用

### 支援的參數類型
- **基本類型**：int, float, double, bool, string, char, byte 等
- **Unity 類型**：Vector2, Vector3, Vector4, Color, Quaternion, Bounds 等
- **列舉類型**：支援所有自訂列舉
- **陣列**：支援各類型的一維陣列（int[], string[], 自訂類別[] 等）
- **自訂類別**：支援序列化的自訂類別和結構
- **巢狀類別**：支援多層巢狀的複雜物件結構
- **Unity Object 引用**：支援 Component、GameObject、ScriptableObject 等引用

### 序列化系統
- **自訂序列化**：實作完整的序列化/反序列化系統，支援複雜類型的持久化
- **委託序列化**：支援 Action 和 Func 委託的序列化與還原
- **Unity Object 引用保持**：正確處理 Unity Object 的引用關係
- **陣列與集合**：支援陣列、列表等集合類型的深度序列化

### 編輯器整合
- **Property Drawer**：為 MethodContainer 和 MethodEntry 提供自訂的 Inspector 顯示
- **編輯器視窗**：可透過 Tools 選單開啟獨立的 Method Invoker 視窗
- **即時更新**：支援在編輯器中即時更新方法列表和參數值

## 技術架構

### 主要類別
- **MethodContainer**：管理 GameObject 上所有可呼叫方法的容器
- **MethodEntry**：封裝單一方法的資訊，包括委託、參數和序列化資料
- **DelegateInfo**：儲存委託的目標和方法資訊
- **CustomSerializationUtility**：處理複雜類型的序列化與反序列化

### 測試覆蓋
專案包含 28 個單元測試，涵蓋：
- 基本類型參數的方法呼叫
- 複雜類型（陣列、自訂類別、巢狀結構）的處理
- 序列化與反序列化的正確性
- 邊界情況（null 值、空陣列等）

## 透過 CLI 進行開發迭代流程

`./Tests` 資料夾內包含了完整的測試套件。當你對程式碼進行修改或擴充功能後：

1. **編譯檢查**：使用 `./CompileCheck.ps1` 驗證代碼能否正常編譯
2. **執行測試**：使用 `./RunTests.ps1` 執行所有單元測試
3. **檢視結果**：測試結果會儲存在專案根目錄的 `TestResults.xml` 中

測試腳本會自動偵測 Unity 安裝路徑和專案路徑，並在測試完成後顯示詳細的結果報告。

## 開發注意事項

### 序列化限制
- `System.Reflection.MethodInfo` 無法直接序列化，需透過委託重建
- 處理 `object[]` 時需保留元素的實際類型資訊
- Unity Object 引用需透過專門的引用列表管理

### 效能考量
- 使用反射呼叫方法，效能略低於直接呼叫
- 大量方法的 GameObject 可能需要較長的掃描時間
- 建議在開發和測試階段使用，正式環境謹慎使用
