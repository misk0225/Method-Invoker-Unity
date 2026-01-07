# Method Invoker

A Unity Editor tool for invoking methods on GameObjects at runtime and in edit mode. Provides a visual interface to call public methods with parameters.

## Installation

### Via Git URL (Recommended)

1. Open Unity Package Manager (Window > Package Manager)
2. Click the `+` button in the top-left corner
3. Select "Add package from git URL..."
4. Enter: `https://github.com/yourusername/method-invoker.git`

### Via Manual Installation

1. Download or clone this repository
2. Copy the `MethodInvoker` folder to your Unity project's `Packages` folder

## Features

- ✅ No external dependencies (Odin Inspector removed)
- ✅ Invoke public void methods on any GameObject
- ✅ Support for method parameters (int, float, string, Vector3, Color, Enums, UnityEngine.Object, etc.)
- ✅ Visual parameter editor in Unity Editor
- ✅ Automatic method discovery
- ✅ Component grouping with visual dividers

## Usage

1. Open the Method Invoker window: `Tools > Method Invoker`
2. Drag a GameObject into the "Target GameObject" field
3. All public void methods from components will be displayed
4. Set parameter values if needed
5. Click "Invoke" to call the method

## 改動總結 (Odin Inspector 移除版本)

這個工具已經完全移除對 **Odin Inspector** 套件的依賴，使用 Unity 原生功能進行重寫。

## 主要變更

### 1. **新增檔案**
- `CustomSerializationUtility.cs` - 自訂序列化系統，替代 Odin 的序列化功能

### 2. **修改的檔案**

#### `MethodEntry.cs`
- ✅ 移除 `Sirenix.Serialization` 依賴
- ✅ 使用自訂的 `CustomSerializationUtility` 進行序列化
- ✅ 重命名結構體 `OdinSerializedData` → `SerializedData`

#### `MethodContainer.cs`
- ✅ 移除 `Sirenix.OdinInspector` 依賴
- ✅ 移除 `ListDrawerSettings` 特性

#### `MethodEntryDrawer.cs`
- ✅ 從 `OdinValueDrawer<MethodEntry>` 改為 `PropertyDrawer`
- ✅ 使用 Unity 原生的 `EditorGUI` 和 `EditorGUIUtility`
- ✅ 實作參數欄位顯示和 Invoke 按鈕功能
- ✅ 支援多種參數類型：
  - 基本型別：int, float, double, bool, string
  - Unity 型別：Vector2, Vector3, Vector4, Color
  - Enum 型別
  - Unity Object 型別

#### `MethodContainerDrawer.cs`
- ✅ 從 `OdinValueDrawer<MethodContainer>` 改為 `PropertyDrawer`
- ✅ 使用 Unity 原生 GUI 繪製
- ✅ 保留原有的分隔線和 MonoBehaviour 顯示功能
- ✅ 實作 GameObject 變更時自動刷新

#### `MethodInvokerWindow.cs`
- ✅ 從 `OdinEditorWindow` 改為 `EditorWindow`
- ✅ 手動管理 `SerializedObject` 和 `SerializedProperty`
- ✅ 加入捲軸視圖支援

### 3. **刪除的檔案**
- ❌ `MethodEntryProcessor.cs` - 功能已整合至 `MethodEntryDrawer`

## 自訂序列化系統功能

`CustomSerializationUtility` 支援以下類型的序列化：

- **基本型別**：bool, byte, sbyte, short, ushort, int, uint, long, ulong, float, double, char
- **字串型別**：string
- **Unity Object**：所有繼承自 `UnityEngine.Object` 的類型
- **陣列型別**：任意類型的一維陣列
- **委派型別**：Action, Func (最多5個參數)
- **列舉型別**：所有 enum 類型
- **複雜型別**：自訂類別和結構體（會序列化所有 public 欄位和帶有 `[SerializeField]` 的私有欄位）

## 使用方式

1. 開啟工具視窗：`Tools > Method Invoker`
2. 選擇目標 GameObject
3. 工具會自動列出該 GameObject 上所有 MonoBehaviour 的 public void 方法
4. 設定方法參數
5. 點擊 "Invoke" 按鈕執行方法

## 技術細節

### 序列化實作
- 使用二進位格式進行序列化
- Unity Object 引用單獨儲存在 `List<Object>` 中
- 支援類型反射和動態重建

### PropertyDrawer 實作
- 完全使用 Unity 原生 EditorGUI API
- 自動計算高度
- 支援巢狀屬性繪製

### 已知限制
- 目前支援最多 5 個參數的方法
- 複雜類型的序列化僅支援標記為可序列化的欄位
- 不支援泛型方法

## 相容性

- ✅ Unity 2019.4 或更新版本
- ✅ 無需任何第三方套件
- ✅ 完全使用 Unity 原生 API

## 效能考量

自訂序列化系統：
- 使用二進位格式，序列化效率高
- 使用反射機制，第一次可能較慢
- 支援大多數常用類型

## 未來改進方向

1. 支援更多參數類型（例如：Quaternion, Rect 等）
2. 加入參數預設值設定
3. 支援更多參數數量（目前最多5個）
4. 加入方法搜尋和過濾功能
5. 支援方法的返回值顯示
