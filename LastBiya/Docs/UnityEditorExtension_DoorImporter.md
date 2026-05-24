# Unity编辑器扩展教程 - DoorConnectionImporter 详解

## 📚 目录
1. [Unity编辑器扩展基础](#unity编辑器扩展基础)
2. [EditorWindow 核心概念](#editorwindow-核心概念)
3. [DoorConnectionImporter 逐行解析](#doorconnectionimporter-逐行解析)
4. [常用API详解](#常用api详解)
5. [实战练习](#实战练习)

---

## Unity编辑器扩展基础

### 什么是编辑器扩展？

Unity编辑器扩展允许你创建自定义工具来提高开发效率。就像Unity自带的Inspector、Hierarchy、Project窗口一样，你也可以创建自己的编辑器窗口和工具。

### 编辑器扩展的类型

1. **自定义编辑器窗口**（EditorWindow）- 本教程重点
   - 创建独立的工具窗口
   - 例如：CSV导入工具、关卡编辑器、资源批处理工具

2. **自定义Inspector**（Editor）
   - 修改组件在Inspector中的显示方式
   - 例如：为自定义组件添加按钮、美化布局

3. **菜单项**（MenuItem）
   - 在Unity菜单栏添加自定义菜单
   - 例如：Tools → My Custom Tool

4. **场景视图扩展**（Handles）
   - 在Scene视图中绘制自定义图形
   - 例如：可视化路径、范围指示器

### 必须知道的规则

#### 规则1：必须放在 Editor 文件夹
```
Assets/
  ├── Scripts/
  │   ├── Editor/              ← 编辑器脚本必须放这里
  │   │   └── DoorConnectionImporter.cs
  │   └── GameSceneSO.cs       ← 运行时脚本
  └── ...
```

**为什么？**
- Unity会将Editor文件夹中的脚本标记为"仅编辑器"
- 这些脚本不会被打包到游戏中
- 可以使用 `UnityEditor` 命名空间（运行时不可用）

#### 规则2：必须引用 UnityEditor 命名空间
```csharp
using UnityEditor;  // ← 编辑器扩展必需
```

#### 规则3：编辑器脚本不能挂载到GameObject上
- 编辑器脚本只在Unity编辑器中运行
- 不能像 `MonoBehaviour` 那样挂载到场景对象上

---

## EditorWindow 核心概念

### EditorWindow 是什么？

`EditorWindow` 是Unity提供的基类，用于创建自定义编辑器窗口。就像你打开的Inspector、Console、Project窗口一样，你可以创建自己的窗口。

### EditorWindow 的生命周期

```
用户点击菜单
    ↓
ShowWindow() 被调用（创建窗口）
    ↓
OnEnable() 被调用（窗口初始化）
    ↓
OnGUI() 被反复调用（每帧多次，绘制界面）
    ↓
用户关闭窗口
    ↓
OnDisable() 被调用
    ↓
OnDestroy() 被调用（清理资源）
```

### 核心方法对比

| 方法 | 调用时机 | 作用 | 类比 |
|------|---------|------|------|
| `ShowWindow()` | 用户点击菜单时 | 创建/打开窗口 | 类似 `Instantiate()` |
| `OnEnable()` | 窗口创建/激活时 | 初始化数据 | 类似 `Awake()` |
| `OnGUI()` | 每帧多次 | 绘制界面 | 类似 `Update()` |
| `OnDisable()` | 窗口关闭/失焦时 | 清理临时数据 | 类似 `OnDisable()` |

---

## DoorConnectionImporter 逐行解析

### 第一部分：类定义和字段

```csharp
using UnityEngine;
using UnityEditor;  // ← 编辑器扩展必需
using System.IO;    // ← 文件操作需要
using System.Collections.Generic;

public class DoorConnectionImporter : EditorWindow
{
    // 字段1：CSV文件路径
    private string csvPath = "Assets/Data/DoorConnections.csv";
    
    // 字段2：目标ScriptableObject
    private DoorConnectionTable targetTable;
```

**知识点**：
- **继承 `EditorWindow`**：这是创建自定义窗口的关键
- **字段的作用**：存储窗口的状态（用户输入的路径、选择的资源等）
- **字段的生命周期**：窗口打开期间一直存在，关闭后销毁

---

### 第二部分：创建窗口 - ShowWindow()

```csharp
[MenuItem("Tools/Import Door Connections")]
public static void ShowWindow()
{
    GetWindow<DoorConnectionImporter>("门连接导入工具");
}
```

#### 详细解析

**1. `[MenuItem]` 特性**
```csharp
[MenuItem("Tools/Import Door Connections")]
```
- **作用**：在Unity菜单栏添加菜单项
- **参数格式**：`"顶级菜单/子菜单/子子菜单"`
- **效果**：Unity顶部会出现 `Tools → Import Door Connections`

**示例**：
```csharp
[MenuItem("Tools/My Tools/Tool A")]        // Tools → My Tools → Tool A
[MenuItem("Window/Custom/My Window")]      // Window → Custom → My Window
[MenuItem("Assets/Batch Process")]         // 右键菜单中出现
```

**2. `GetWindow<T>()` 方法**
```csharp
GetWindow<DoorConnectionImporter>("门连接导入工具");
```
- **作用**：获取或创建编辑器窗口
- **泛型参数**：指定窗口类型
- **第二个参数**：窗口标题（显示在标签上）
- **返回值**：窗口实例（本例未使用）

**行为**：
- 如果窗口已打开 → 聚焦到该窗口
- 如果窗口未打开 → 创建新窗口

**为什么是 static？**
- `[MenuItem]` 要求方法必须是静态的
- Unity需要在没有实例的情况下调用这个方法

---

### 第三部分：绘制界面 - OnGUI()

```csharp
private void OnGUI()
{
    // 绘制UI元素
}
```

#### OnGUI() 的工作原理

**调用频率**：
- 每帧调用多次（不是一次！）
- 鼠标移动、键盘输入、窗口重绘都会触发

**即时模式GUI（IMGUI）**：
- Unity编辑器使用的是即时模式GUI
- 每次 `OnGUI()` 调用时，重新绘制所有UI元素
- 不像UGUI那样有持久的GameObject

**类比理解**：
```csharp
// IMGUI（编辑器）- 每帧重新绘制
void OnGUI()
{
    if (GUILayout.Button("点击"))  // 每帧都创建按钮
    {
        Debug.Log("按钮被点击");
    }
}

// UGUI（游戏）- 持久对象
void Start()
{
    Button btn = GetComponent<Button>();  // 按钮一直存在
    btn.onClick.AddListener(() => Debug.Log("按钮被点击"));
}
```

---

### 第四部分：UI控件详解

#### 1. 标题和间距

```csharp
GUILayout.Label("CSV门连接导入工具", EditorStyles.boldLabel);
EditorGUILayout.Space();
```

**GUILayout.Label()**：
- 绘制文本标签
- 参数1：文本内容
- 参数2：样式（可选）

**EditorStyles**：Unity内置样式
- `EditorStyles.boldLabel`：粗体标签
- `EditorStyles.helpBox`：帮助框样式
- `EditorStyles.miniButton`：小按钮样式

**EditorGUILayout.Space()**：
- 添加垂直间距
- 让界面不那么拥挤

---

#### 2. 文本输入框

```csharp
EditorGUILayout.LabelField("CSV文件路径:");
csvPath = EditorGUILayout.TextField(csvPath);
```

**工作原理**：
```csharp
// 第一次调用（假设 csvPath = "Assets/Data/file.csv"）
csvPath = EditorGUILayout.TextField(csvPath);
// ↓
// 1. Unity绘制一个文本框，显示 "Assets/Data/file.csv"
// 2. 用户修改为 "Assets/Data/new.csv"
// 3. 方法返回 "Assets/Data/new.csv"
// 4. csvPath 被更新为 "Assets/Data/new.csv"

// 下一帧调用
csvPath = EditorGUILayout.TextField(csvPath);
// ↓
// 1. Unity绘制文本框，显示 "Assets/Data/new.csv"（新值）
```

**关键点**：
- 必须将返回值赋值回变量
- 否则用户的输入会丢失

---

#### 3. 按钮

```csharp
if (GUILayout.Button("选择CSV文件"))
{
    // 按钮被点击时执行
}
```

**返回值**：
- `true`：用户点击了按钮
- `false`：用户没有点击

**每帧都调用？**
- 是的，`GUILayout.Button()` 每帧都调用
- 但只有在用户点击的那一帧返回 `true`

**示例**：
```csharp
// 帧1：用户没点击 → 返回 false → if块不执行
// 帧2：用户没点击 → 返回 false → if块不执行
// 帧3：用户点击了 → 返回 true  → if块执行
// 帧4：用户没点击 → 返回 false → if块不执行
```

---

#### 4. 文件选择对话框

```csharp
string path = EditorUtility.OpenFilePanel("选择CSV文件", "Assets/Data", "csv");
```

**参数**：
- 参数1：对话框标题
- 参数2：默认打开的文件夹
- 参数3：文件扩展名过滤（只显示.csv文件）

**返回值**：
- 用户选择的文件的**绝对路径**
- 如果用户取消，返回空字符串

**路径转换**：
```csharp
// OpenFilePanel 返回绝对路径
string absolutePath = "C:/Projects/MyGame/Assets/Data/file.csv";

// Unity需要相对路径
string relativePath = "Assets/Data/file.csv";

// 转换方法
Application.dataPath;  // "C:/Projects/MyGame/Assets"
string relative = "Assets" + absolutePath.Replace(Application.dataPath, "");
// 结果：relative = "Assets/Data/file.csv"
```

**为什么需要转换？**
- Unity的资源系统使用相对路径
- `AssetDatabase.LoadAssetAtPath()` 等方法需要相对路径

---

#### 5. 对象引用字段（最重要！）

```csharp
targetTable = EditorGUILayout.ObjectField(
    targetTable,                      // 当前对象
    typeof(DoorConnectionTable),      // 对象类型
    false                             // 是否允许场景对象
) as DoorConnectionTable;
```

**参数详解**：

| 参数 | 说明 | 示例 |
|------|------|------|
| 参数1 | 当前对象引用 | `targetTable` |
| 参数2 | 允许的对象类型 | `typeof(DoorConnectionTable)` |
| 参数3 | 是否允许场景对象 | `false` = 只能选择项目资源 |

**使用方式**：
1. **拖拽**：从Project窗口拖拽 `.asset` 文件到这个框
2. **选择**：点击右侧的小圆点，从弹出窗口选择

**类型限制**：
```csharp
// 只能拖入 DoorConnectionTable 类型的资源
typeof(DoorConnectionTable)

// 如果拖入其他类型（如 GameSceneSO），会被拒绝
```

**场景对象 vs 项目资源**：
```csharp
// false：只能选择项目资源（.asset, .prefab等）
EditorGUILayout.ObjectField(obj, typeof(MyType), false);

// true：可以选择场景中的GameObject
EditorGUILayout.ObjectField(obj, typeof(Transform), true);
```

---

### 第五部分：CSV导入逻辑 - ImportCSV()

#### 步骤1：验证文件存在

```csharp
if (!File.Exists(csvPath))
{
    EditorUtility.DisplayDialog("错误", $"CSV文件不存在：{csvPath}", "确定");
    return;
}
```

**File.Exists()**：
- C#标准库方法
- 检查文件是否存在
- 参数：文件路径（绝对或相对）

**EditorUtility.DisplayDialog()**：
- 显示模态对话框（阻塞执行）
- 参数1：标题
- 参数2：消息内容
- 参数3：按钮文本
- 返回值：用户点击的按钮（本例只有一个按钮）

---

#### 步骤2：读取CSV文件

```csharp
string[] lines = File.ReadAllLines(csvPath);
```

**File.ReadAllLines()**：
- 读取文件的所有行
- 返回字符串数组

**示例**：
```csv
RoomID,DoorID,TargetRoomID,TargetDoorID
Room01,Right_01,Room02,Left_01
Room02,Left_01,Room01,Right_01
```

**读取结果**：
```csharp
lines[0] = "RoomID,DoorID,TargetRoomID,TargetDoorID"
lines[1] = "Room01,Right_01,Room02,Left_01"
lines[2] = "Room02,Left_01,Room01,Right_01"
```

---

#### 步骤3：解析CSV数据

```csharp
for (int i = 1; i < lines.Length; i++)  // 从1开始，跳过表头
{
    string line = lines[i].Trim();
    
    // 跳过空行和注释
    if (string.IsNullOrEmpty(line) || line.StartsWith("#"))
        continue;
    
    // 分割CSV行
    string[] values = line.Split(',');
    
    // 验证列数
    if (values.Length != 4)
    {
        Debug.LogWarning($"第 {i + 1} 行格式错误，跳过：{line}");
        continue;
    }
    
    // 提取数据
    string fromRoomID = values[0].Trim();
    string fromDoorID = values[1].Trim();
    string toRoomID = values[2].Trim();
    string toDoorID = values[3].Trim();
    
    // 创建连接对象
    connections.Add(new DoorConnection(fromRoomID, fromDoorID, toRoomID, toDoorID));
}
```

**逐步解析**：

**1. 为什么从索引1开始？**
```csharp
for (int i = 1; i < lines.Length; i++)
```
- `lines[0]` 是表头：`"RoomID,DoorID,TargetRoomID,TargetDoorID"`
- 我们只需要数据行，所以从 `lines[1]` 开始

**2. Trim() 的作用**
```csharp
string line = lines[i].Trim();
```
- 去除首尾空格
- 例如：`"  Room01,Right_01  "` → `"Room01,Right_01"`

**3. 跳过空行和注释**
```csharp
if (string.IsNullOrEmpty(line) || line.StartsWith("#"))
    continue;
```
- 空行：`""`
- 注释行：`"# === Room01 ==="`

**4. Split() 分割字符串**
```csharp
string[] values = line.Split(',');
```
- 按逗号分割
- 例如：`"Room01,Right_01,Room02,Left_01"` → `["Room01", "Right_01", "Room02", "Left_01"]`

**5. 验证列数**
```csharp
if (values.Length != 4)
```
- CSV应该有4列
- 如果不是4列，说明格式错误

**6. 提取数据**
```csharp
string fromRoomID = values[0].Trim();  // 第1列
string fromDoorID = values[1].Trim();  // 第2列
string toRoomID = values[2].Trim();    // 第3列
string toDoorID = values[3].Trim();    // 第4列
```

---

#### 步骤4：更新ScriptableObject

```csharp
targetTable.connections = connections;
EditorUtility.SetDirty(targetTable);
AssetDatabase.SaveAssets();
```

**关键API**：

**1. EditorUtility.SetDirty()**
```csharp
EditorUtility.SetDirty(targetTable);
```
- **作用**：标记资源为"已修改"
- **为什么需要？**：Unity不会自动检测ScriptableObject的修改
- **如果不调用**：修改不会被保存到磁盘

**类比**：
```
你修改了Word文档 → 标题栏出现 * 号 → 提示你保存
SetDirty() 就是告诉Unity："这个资源被修改了，记得保存！"
```

**2. AssetDatabase.SaveAssets()**
```csharp
AssetDatabase.SaveAssets();
```
- **作用**：保存所有被标记为"已修改"的资源
- **效果**：将内存中的修改写入磁盘（.asset文件）

**完整流程**：
```
1. targetTable.connections = connections;  // 修改内存中的数据
2. EditorUtility.SetDirty(targetTable);    // 标记为"已修改"
3. AssetDatabase.SaveAssets();             // 保存到磁盘
```

---

## 常用API详解

### 1. EditorUtility 工具类

| 方法 | 作用 | 示例 |
|------|------|------|
| `DisplayDialog()` | 显示对话框 | `EditorUtility.DisplayDialog("标题", "内容", "确定")` |
| `DisplayDialogComplex()` | 显示多按钮对话框 | 返回用户点击的按钮索引 |
| `OpenFilePanel()` | 打开文件选择对话框 | 返回文件路径 |
| `OpenFolderPanel()` | 打开文件夹选择对话框 | 返回文件夹路径 |
| `SaveFilePanel()` | 打开保存文件对话框 | 返回保存路径 |
| `SetDirty()` | 标记资源为已修改 | 必须调用才能保存修改 |

### 2. AssetDatabase 资源管理类

| 方法 | 作用 | 示例 |
|------|------|------|
| `SaveAssets()` | 保存所有已修改的资源 | `AssetDatabase.SaveAssets()` |
| `LoadAssetAtPath<T>()` | 加载资源 | `AssetDatabase.LoadAssetAtPath<GameSceneSO>("Assets/Data/Room01.asset")` |
| `CreateAsset()` | 创建资源 | `AssetDatabase.CreateAsset(obj, "Assets/Data/New.asset")` |
| `Refresh()` | 刷新资源数据库 | 导入外部文件后调用 |

### 3. GUILayout vs EditorGUILayout

| GUILayout | EditorGUILayout | 区别 |
|-----------|-----------------|------|
| `Label()` | `LabelField()` | EditorGUILayout样式更统一 |
| `TextField()` | `TextField()` | EditorGUILayout有更多选项 |
| `Button()` | - | 按钮通常用GUILayout |
| - | `ObjectField()` | 编辑器专用，拖拽资源 |

**建议**：
- 编辑器窗口优先使用 `EditorGUILayout`
- 按钮可以用 `GUILayout.Button()`

---

## 实战练习

### 练习1：添加"清空连接表"按钮

**目标**：在窗口中添加一个按钮，点击后清空所有连接

**提示**：
```csharp
if (targetTable != null && GUILayout.Button("清空连接表"))
{
    // 1. 清空 targetTable.connections
    // 2. 调用 SetDirty()
    // 3. 调用 SaveAssets()
    // 4. 显示提示对话框
}
```

---

### 练习2：添加连接数量显示

**目标**：在窗口中显示当前连接表的连接数量

**提示**：
```csharp
if (targetTable != null)
{
    EditorGUILayout.LabelField("连接数量:", targetTable.connections.Count.ToString());
}
```

---

### 练习3：添加导出CSV功能

**目标**：将DoorConnectionTable导出为CSV文件

**提示**：
```csharp
if (targetTable != null && GUILayout.Button("导出CSV"))
{
    string path = EditorUtility.SaveFilePanel("导出CSV", "Assets/Data", "DoorConnections", "csv");
    if (!string.IsNullOrEmpty(path))
    {
        // 1. 创建CSV内容（表头 + 数据行）
        // 2. 使用 File.WriteAllText() 写入文件
        // 3. 显示成功提示
    }
}
```

---

### 练习4：添加自动创建DoorConnectionTable功能

**目标**：如果targetTable为空，点击按钮自动创建

**提示**：
```csharp
if (targetTable == null && GUILayout.Button("创建新的DoorConnectionTable"))
{
    targetTable = CreateInstance<DoorConnectionTable>();
    AssetDatabase.CreateAsset(targetTable, "Assets/Data So/DoorConnectionTable.asset");
    AssetDatabase.SaveAssets();
}
```

---

## 总结

### 核心知识点

1. **EditorWindow**：创建自定义编辑器窗口的基类
2. **[MenuItem]**：在Unity菜单栏添加菜单项
3. **OnGUI()**：每帧调用，绘制界面
4. **IMGUI**：即时模式GUI，每帧重新绘制
5. **EditorUtility.SetDirty()**：标记资源为已修改
6. **AssetDatabase.SaveAssets()**：保存资源到磁盘

### 开发流程

1. 创建类，继承 `EditorWindow`
2. 添加 `[MenuItem]` 和 `ShowWindow()` 方法
3. 在 `OnGUI()` 中绘制界面
4. 实现业务逻辑（导入、导出、处理数据等）
5. 放在 `Editor` 文件夹中

### 调试技巧

1. 使用 `Debug.Log()` 输出信息
2. 使用 `EditorUtility.DisplayDialog()` 显示提示
3. 在 `OnGUI()` 中显示变量值（用于调试）
4. 使用 `try-catch` 捕获异常

### 进阶学习

- **自定义Inspector**：修改组件在Inspector中的显示
- **SceneView扩展**：在Scene视图中绘制自定义图形
- **PropertyDrawer**：自定义字段的绘制方式
- **EditorPrefs**：保存编辑器设置

---

## 参考资源

- [Unity官方文档 - EditorWindow](https://docs.unity3d.com/ScriptReference/EditorWindow.html)
- [Unity官方文档 - MenuItem](https://docs.unity3d.com/ScriptReference/MenuItem.html)
- [Unity官方教程 - Editor Scripting](https://learn.unity.com/tutorial/editor-scripting)

---

**恭喜你完成了Unity编辑器扩展的学习！现在你可以创建自己的编辑器工具了。**
