# 房间连接系统 - 完整实现指南

## 📋 目录
1. [系统概述](#系统概述)
2. [架构设计](#架构设计)
3. [实现步骤](#实现步骤)
4. [代码模板](#代码模板)
5. [配置流程](#配置流程)
6. [测试验证](#测试验证)

---

## 系统概述

### 功能目标
实现类似《空洞骑士》的房间连接系统：
- 每个房间（Scene）可以有多个门（Door）
- 门之间双向连接：从房间A的右门进入 → 从房间B的左门出现
- 支持灵活配置：通过CSV表格管理所有房间的门连接关系

### 核心特性
- ✅ CSV配表管理门连接关系（策划友好）
- ✅ 自动生成双向连接（只需配置一次）
- ✅ 支持一个方向多个门（Left_01, Left_02...）
- ✅ 自动计算玩家生成位置（基于门方向+偏移量）
- ✅ 基于EventCenter的事件驱动架构

---

## 架构设计

### 数据流图
```
CSV表格 (策划编辑)
    ↓
[CSV导入工具] (Unity Editor)
    ↓
DoorConnectionTable.asset (运行时查询)
    ↓
SceneLoader (场景加载+玩家定位)
```

### 核心组件关系
```
GameSceneSO (房间配置)
├── roomID: string              // 房间唯一标识
├── sceneReference: AssetReference
└── doors: List<DoorData>       // 该房间的所有门

DoorData (门配置)
├── doorID: string              // 门标识 "Left_01"
└── direction: DoorDirection    // Left/Right/Top/Bottom

DoorConnectionTable (连接配置 - 全局唯一)
└── connections: List<DoorConnection>

DoorConnection (单个连接)
├── fromRoomID: string
├── fromDoorID: string
├── toRoomID: string
└── toDoorID: string

Door (场景中的门物体)
├── doorID: string              // 当前门ID
└── OnTriggerEnter2D()          // 检测玩家碰撞

SceneLoader (场景加载器)
├── doorSpawnOffset: float = 8  // 生成位置偏移
└── LoadSceneByDoor()           // 根据门ID加载场景
```

---

## 实现步骤

### 第一步：扩展 GameSceneSO
**文件路径**: `LastBiya/Assets/Scripts/ScriptableObject/GameSceneSO.cs`

**需要添加的内容**:
```csharp
[Header("房间标识")]
public string roomID = "Room01";  // 房间唯一ID

[Header("门配置")]
public List<DoorData> doors = new List<DoorData>();
```

**完整代码模板**（见下方代码模板章节）

---

### 第二步：创建 DoorConnectionTable
**文件路径**: `LastBiya/Assets/Scripts/ScriptableObject/DoorConnectionTable.cs`

**功能**:
- 存储所有房间的门连接关系
- 提供查询方法：根据来源门查找目标门

**完整代码模板**（见下方代码模板章节）

---

### 第三步：创建 CSV 导入工具
**文件路径**: `LastBiya/Assets/Scripts/Editor/DoorConnectionImporter.cs`

**功能**:
- Unity菜单：Tools → Import Door Connections
- 读取CSV文件并解析
- 自动生成双向连接
- 更新 DoorConnectionTable.asset

**完整代码模板**（见下方代码模板章节）

---

### 第四步：修改 TeleportPoint → Door
**文件路径**: `LastBiya/Assets/Scripts/Transition/Door.cs`

**改动**:
- 重命名文件：`TeleportPoint.cs` → `Door.cs`
- 移除手动配置的目标场景和位置
- 改为通过门ID查询连接表

**完整代码模板**（见下方代码模板章节）

---

### 第五步：扩展 SceneLoader
**文件路径**: `LastBiya/Assets/Scripts/Transition/SceneLoader.cs`

**需要添加的内容**:
```csharp
[Header("门系统配置")]
public DoorConnectionTable connectionTable;  // 连接表引用
public float doorSpawnOffset = 8f;           // 生成位置偏移

// 新增方法：根据门ID加载场景
public void LoadSceneByDoor(string fromRoomID, string fromDoorID)
```

**完整代码模板**（见下方代码模板章节）

---

### 第六步：创建 CSV 配置文件
**文件路径**: `LastBiya/Assets/Data/DoorConnections.csv`

**CSV格式**（按房间分组）:
```csv
RoomID,DoorID,TargetRoomID,TargetDoorID
# === Room01 的所有门 ===
Room01,Left_01,Room00,Right_01
Room01,Right_01,Room02,Left_01
# === Room02 的所有门 ===
Room02,Left_01,Room01,Right_01
Room02,Right_01,Room03,Left_01
Room02,Top_01,Room05,Bottom_01
```

**注意事项**:
- 第一行是表头，必须保留
- 使用 `#` 开头的行作为注释（导入工具会忽略）
- 每个连接需要手动写双向（Room01→Room02 和 Room02→Room01）

---

## 代码模板

### 1. GameSceneSO.cs（扩展版）
```csharp
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

[CreateAssetMenu(menuName = "Game Scene/GameSceneSO", order = 0)]
public class GameSceneSO : ScriptableObject
{
    [Header("房间标识")]
    [Tooltip("房间唯一ID，用于门连接系统")]
    public string roomID = "Room01";

    [Header("场景引用")]
    public AssetReference sceneReference;
    public ESceneType sceneType;

    [Header("门配置")]
    [Tooltip("该房间的所有门")]
    public List<DoorData> doors = new List<DoorData>();

    /// <summary>
    /// 根据门ID获取门数据
    /// </summary>
    public DoorData? GetDoor(string doorID)
    {
        foreach (var door in doors)
        {
            if (door.doorID == doorID)
                return door;
        }
        Debug.LogWarning($"房间 {roomID} 中未找到门 {doorID}");
        return null;
    }
}

/// <summary>
/// 门数据结构
/// </summary>
[System.Serializable]
public struct DoorData
{
    [Tooltip("门的唯一标识，如 Left_01, Right_01")]
    public string doorID;

    [Tooltip("门的方向")]
    public DoorDirection direction;
}

/// <summary>
/// 门的方向枚举
/// </summary>
public enum DoorDirection
{
    Left,
    Right,
    Top,
    Bottom
}

public enum ESceneType
{
    Menu,
    Location,
}
```

---

### 2. DoorConnectionTable.cs（新建）
```csharp
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 门连接配置表 - 存储所有房间的门连接关系
/// 全局唯一，通过CSV导入工具生成
/// </summary>
[CreateAssetMenu(menuName = "Game Scene/Door Connection Table", order = 1)]
public class DoorConnectionTable : ScriptableObject
{
    [Header("所有门连接")]
    public List<DoorConnection> connections = new List<DoorConnection>();

    /// <summary>
    /// 根据来源门查找目标门
    /// </summary>
    /// <param name="fromRoomID">来源房间ID</param>
    /// <param name="fromDoorID">来源门ID</param>
    /// <returns>目标门连接信息，未找到返回null</returns>
    public DoorConnection? GetConnection(string fromRoomID, string fromDoorID)
    {
        foreach (var connection in connections)
        {
            if (connection.fromRoomID == fromRoomID && connection.fromDoorID == fromDoorID)
            {
                return connection;
            }
        }
        Debug.LogWarning($"未找到门连接：{fromRoomID}.{fromDoorID}");
        return null;
    }

    /// <summary>
    /// 验证连接表的完整性（检查双向连接是否匹配）
    /// </summary>
    public void ValidateConnections()
    {
        int errorCount = 0;
        foreach (var connection in connections)
        {
            // 检查反向连接是否存在
            var reverse = GetConnection(connection.toRoomID, connection.toDoorID);
            if (reverse == null)
            {
                Debug.LogError($"缺少反向连接：{connection.toRoomID}.{connection.toDoorID} → {connection.fromRoomID}.{connection.fromDoorID}");
                errorCount++;
            }
            else if (reverse.Value.toRoomID != connection.fromRoomID || reverse.Value.toDoorID != connection.fromDoorID)
            {
                Debug.LogError($"反向连接不匹配：{connection.fromRoomID}.{connection.fromDoorID} ↔ {connection.toRoomID}.{connection.toDoorID}");
                errorCount++;
            }
        }

        if (errorCount == 0)
        {
            Debug.Log($"✅ 连接表验证通过！共 {connections.Count} 个连接");
        }
        else
        {
            Debug.LogError($"❌ 连接表验证失败！发现 {errorCount} 个错误");
        }
    }
}

/// <summary>
/// 单个门连接数据
/// </summary>
[System.Serializable]
public struct DoorConnection
{
    [Tooltip("来源房间ID")]
    public string fromRoomID;

    [Tooltip("来源门ID")]
    public string fromDoorID;

    [Tooltip("目标房间ID")]
    public string toRoomID;

    [Tooltip("目标门ID")]
    public string toDoorID;

    public DoorConnection(string fromRoomID, string fromDoorID, string toRoomID, string toDoorID)
    {
        this.fromRoomID = fromRoomID;
        this.fromDoorID = fromDoorID;
        this.toRoomID = toRoomID;
        this.toDoorID = toDoorID;
    }

    public override string ToString()
    {
        return $"{fromRoomID}.{fromDoorID} → {toRoomID}.{toDoorID}";
    }
}
```

---

### 3. DoorConnectionImporter.cs（新建 - Editor脚本）
```csharp
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// CSV门连接导入工具
/// 菜单：Tools → Import Door Connections
/// </summary>
public class DoorConnectionImporter : EditorWindow
{
    private const string CSV_PATH = "Assets/Data/DoorConnections.csv";
    private const string TABLE_PATH = "Assets/Data So/DoorConnectionTable.asset";

    private string csvPath = CSV_PATH;
    private string tablePath = TABLE_PATH;
    private DoorConnectionTable targetTable;

    [MenuItem("Tools/Import Door Connections")]
    public static void ShowWindow()
    {
        GetWindow<DoorConnectionImporter>("门连接导入工具");
    }

    private void OnGUI()
    {
        GUILayout.Label("CSV门连接导入工具", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // CSV文件路径
        EditorGUILayout.LabelField("CSV文件路径:");
        csvPath = EditorGUILayout.TextField(csvPath);
        if (GUILayout.Button("选择CSV文件"))
        {
            string path = EditorUtility.OpenFilePanel("选择CSV文件", "Assets/Data", "csv");
            if (!string.IsNullOrEmpty(path))
            {
                csvPath = "Assets" + path.Replace(Application.dataPath, "");
            }
        }

        EditorGUILayout.Space();

        // 目标SO路径
        EditorGUILayout.LabelField("目标DoorConnectionTable:");
        targetTable = EditorGUILayout.ObjectField(targetTable, typeof(DoorConnectionTable), false) as DoorConnectionTable;

        EditorGUILayout.Space();

        // 导入按钮
        if (GUILayout.Button("导入CSV", GUILayout.Height(30)))
        {
            ImportCSV();
        }

        EditorGUILayout.Space();

        // 验证按钮
        if (targetTable != null && GUILayout.Button("验证连接表"))
        {
            targetTable.ValidateConnections();
        }
    }

    private void ImportCSV()
    {
        // 检查CSV文件是否存在
        if (!File.Exists(csvPath))
        {
            EditorUtility.DisplayDialog("错误", $"CSV文件不存在：{csvPath}", "确定");
            return;
        }

        // 检查或创建目标SO
        if (targetTable == null)
        {
            targetTable = AssetDatabase.LoadAssetAtPath<DoorConnectionTable>(tablePath);
            if (targetTable == null)
            {
                targetTable = CreateInstance<DoorConnectionTable>();
                AssetDatabase.CreateAsset(targetTable, tablePath);
                Debug.Log($"创建新的DoorConnectionTable：{tablePath}");
            }
        }

        // 读取CSV
        string[] lines = File.ReadAllLines(csvPath);
        List<DoorConnection> connections = new List<DoorConnection>();

        for (int i = 1; i < lines.Length; i++) // 跳过表头
        {
            string line = lines[i].Trim();

            // 跳过空行和注释行
            if (string.IsNullOrEmpty(line) || line.StartsWith("#"))
                continue;

            // 解析CSV行
            string[] values = line.Split(',');
            if (values.Length != 4)
            {
                Debug.LogWarning($"第 {i + 1} 行格式错误，跳过：{line}");
                continue;
            }

            string fromRoomID = values[0].Trim();
            string fromDoorID = values[1].Trim();
            string toRoomID = values[2].Trim();
            string toDoorID = values[3].Trim();

            connections.Add(new DoorConnection(fromRoomID, fromDoorID, toRoomID, toDoorID));
        }

        // 更新SO
        targetTable.connections = connections;
        EditorUtility.SetDirty(targetTable);
        AssetDatabase.SaveAssets();

        Debug.Log($"✅ 导入成功！共导入 {connections.Count} 个门连接");
        EditorUtility.DisplayDialog("导入成功", $"成功导入 {connections.Count} 个门连接", "确定");

        // 自动验证
        targetTable.ValidateConnections();
    }
}
```

---

### 4. Door.cs（重构 TeleportPoint）
```csharp
using UnityEngine;

/// <summary>
/// 门组件 - 挂载在场景中的门物体上
/// 玩家接触后触发场景切换
/// 需要配合 Collider2D 组件使用，并勾选 Is Trigger
/// </summary>
public class Door : MonoBehaviour
{
    [Header("门标识")]
    [Tooltip("当前门的ID，如 Left_01, Right_01")]
    public string doorID = "Left_01";

    [Header("触发设置")]
    [Tooltip("是否启用门")]
    public bool isActive = true;

    [Tooltip("防止重复触发（勾选后只能触发一次）")]
    public bool triggerOnce = false;

    private bool hasTriggered = false;
    private GameSceneSO currentRoom;

    private void Start()
    {
        // 获取当前场景的房间配置
        // 注意：需要在SceneLoader中提供获取当前房间的方法
        currentRoom = FindObjectOfType<SceneLoader>()?.GetCurrentRoom();
        
        if (currentRoom == null)
        {
            Debug.LogError($"门 {doorID} 无法获取当前房间信息！");
        }
    }

    /// <summary>
    /// 玩家进入触发器时自动触发传送
    /// </summary>
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 检查是否是玩家
        if (!collision.CompareTag("Player"))
            return;

        // 检查门是否激活
        if (!isActive)
        {
            Debug.Log($"门 {doorID} 未激活");
            return;
        }

        // 检查是否已经触发过
        if (triggerOnce && hasTriggered)
        {
            Debug.Log($"门 {doorID} 已使用过");
            return;
        }

        // 触发传送
        TriggerTransition();

        // 标记已触发
        if (triggerOnce)
            hasTriggered = true;
    }

    /// <summary>
    /// 触发场景切换
    /// </summary>
    private void TriggerTransition()
    {
        if (currentRoom == null)
        {
            Debug.LogError("当前房间信息为空，无法触发传送！");
            return;
        }

        Debug.Log($"玩家从门 {currentRoom.roomID}.{doorID} 进入");

        // 创建门传送数据
        var doorData = new DoorTransitionData(currentRoom.roomID, doorID);

        // 通过 EventCenter 触发门传送请求
        EventCenter.Instance.EventTrigger<DoorTransitionData>("DoorTransitionRequest", doorData);
    }

    /// <summary>
    /// 在编辑器中绘制门位置（方便调试）
    /// </summary>
    private void OnDrawGizmos()
    {
        Gizmos.color = isActive ? Color.green : Color.gray;
        Gizmos.DrawWireCube(transform.position, new Vector3(1f, 2f, 0f));
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position, new Vector3(1.5f, 3f, 0f));

#if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 2f,
            $"门: {doorID}\n状态: {(isActive ? "激活" : "未激活")}");
#endif
    }
}

/// <summary>
/// 门传送数据结构
/// </summary>
public struct DoorTransitionData
{
    public string fromRoomID;
    public string fromDoorID;

    public DoorTransitionData(string fromRoomID, string fromDoorID)
    {
        this.fromRoomID = fromRoomID;
        this.fromDoorID = fromDoorID;
    }
}
```

---

### 5. SceneLoader.cs（扩展版 - 部分代码）
在现有的 `SceneLoader.cs` 中添加以下内容：

```csharp
// ========== 在类的字段区域添加 ==========
[Header("门系统配置")]
[Tooltip("门连接配置表")]
public DoorConnectionTable connectionTable;

[Tooltip("门生成位置偏移量（根据门方向自动计算）")]
public float doorSpawnOffset = 8f;

[Header("房间配置")]
[Tooltip("所有房间的配置列表（用于根据roomID查找GameSceneSO）")]
public List<GameSceneSO> allRooms = new List<GameSceneSO>();

// ========== 在 OnEnable 中添加订阅 ==========
private void OnEnable()
{
    // 订阅场景加载请求事件
    EventCenter.Instance.AddEventListener<SceneLoadData>("SceneLoadRequest", OnLoadRequestEvent);
    
    // 订阅门传送请求事件（新增）
    EventCenter.Instance.AddEventListener<DoorTransitionData>("DoorTransitionRequest", OnDoorTransitionRequest);
}

// ========== 在 OnDisable 中添加取消订阅 ==========
private void OnDisable()
{
    // 取消订阅场景加载请求事件
    EventCenter.Instance.RemoveEventListener<SceneLoadData>("SceneLoadRequest", OnLoadRequestEvent);
    
    // 取消订阅门传送请求事件（新增）
    EventCenter.Instance.RemoveEventListener<DoorTransitionData>("DoorTransitionRequest", OnDoorTransitionRequest);
}

// ========== 新增方法 ==========

/// <summary>
/// 获取当前加载的房间（供Door组件调用）
/// </summary>
public GameSceneSO GetCurrentRoom()
{
    return currentLoadedScene;
}

/// <summary>
/// 根据房间ID查找GameSceneSO
/// </summary>
private GameSceneSO GetRoomByID(string roomID)
{
    foreach (var room in allRooms)
    {
        if (room.roomID == roomID)
            return room;
    }
    Debug.LogError($"未找到房间：{roomID}");
    return null;
}

/// <summary>
/// 处理门传送请求
/// </summary>
private void OnDoorTransitionRequest(DoorTransitionData doorData)
{
    if (connectionTable == null)
    {
        Debug.LogError("DoorConnectionTable 未配置！");
        return;
    }

    // 1. 查询连接表，获取目标门信息
    var connection = connectionTable.GetConnection(doorData.fromRoomID, doorData.fromDoorID);
    if (connection == null)
    {
        Debug.LogError($"未找到门连接：{doorData.fromRoomID}.{doorData.fromDoorID}");
        return;
    }

    // 2. 获取目标房间的GameSceneSO
    GameSceneSO targetRoom = GetRoomByID(connection.Value.toRoomID);
    if (targetRoom == null)
    {
        Debug.LogError($"未找到目标房间：{connection.Value.toRoomID}");
        return;
    }

    // 3. 获取目标门的配置
    var targetDoor = targetRoom.GetDoor(connection.Value.toDoorID);
    if (targetDoor == null)
    {
        Debug.LogError($"目标房间 {targetRoom.roomID} 中未找到门 {connection.Value.toDoorID}");
        return;
    }

    // 4. 根据门方向计算玩家生成位置
    Vector3 spawnPosition = CalculateSpawnPosition(targetDoor.Value.direction);

    Debug.Log($"门传送：{doorData.fromRoomID}.{doorData.fromDoorID} → {targetRoom.roomID}.{connection.Value.toDoorID}，生成位置：{spawnPosition}");

    // 5. 触发场景加载
    var loadData = new SceneLoadData(targetRoom, spawnPosition, true);
    OnLoadRequestEvent(loadData);
}

/// <summary>
/// 根据门方向计算玩家生成位置
/// </summary>
private Vector3 CalculateSpawnPosition(DoorDirection direction)
{
    switch (direction)
    {
        case DoorDirection.Left:
            return new Vector3(-doorSpawnOffset, 0, 0);
        case DoorDirection.Right:
            return new Vector3(doorSpawnOffset, 0, 0);
        case DoorDirection.Top:
            return new Vector3(0, doorSpawnOffset, 0);
        case DoorDirection.Bottom:
            return new Vector3(0, -doorSpawnOffset, 0);
        default:
            return Vector3.zero;
    }
}
```

---

## 配置流程

### 步骤1：创建房间配置（GameSceneSO）
1. 在Unity中右键 → Create → Game Scene → GameSceneSO
2. 命名为 `Room01.asset`
3. 配置字段：
   - **roomID**: `Room01`
   - **sceneReference**: 拖入对应的场景
   - **doors**: 添加门列表
     - doorID: `Left_01`, direction: `Left`
     - doorID: `Right_01`, direction: `Right`

### 步骤2：编写CSV配置文件
1. 在 `Assets/Data/` 文件夹下创建 `DoorConnections.csv`
2. 使用Excel或文本编辑器编辑：
```csv
RoomID,DoorID,TargetRoomID,TargetDoorID
# === Room01 ===
Room01,Right_01,Room02,Left_01
# === Room02 ===
Room02,Left_01,Room01,Right_01
Room02,Right_01,Room03,Left_01
```

### 步骤3：导入CSV生成连接表
1. Unity菜单 → Tools → Import Door Connections
2. 选择CSV文件路径
3. 点击"导入CSV"按钮
4. 查看控制台输出，确认导入成功
5. 点击"验证连接表"检查双向连接是否正确

### 步骤4：在场景中放置门物体
1. 在Room01场景中创建空物体，命名为 `Door_Right_01`
2. 添加组件：
   - **Door** 组件，设置 doorID = `Right_01`
   - **BoxCollider2D**，勾选 `Is Trigger`，调整大小覆盖门区域
3. 将物体放置在场景右侧边缘

### 步骤5：配置SceneLoader
1. 找到场景中的SceneLoader物体
2. 在Inspector中配置：
   - **Connection Table**: 拖入 `DoorConnectionTable.asset`
   - **Door Spawn Offset**: 设置为 `8`（根据场景大小调整）
   - **All Rooms**: 添加所有房间的GameSceneSO

---

## 测试验证

### 测试清单
- [ ] CSV导入成功，无错误提示
- [ ] 连接表验证通过，双向连接匹配
- [ ] 玩家接触门物体时触发场景切换
- [ ] 玩家在目标场景的正确位置生成
- [ ] 可以从目标场景返回原场景
- [ ] 多个门的场景切换正常

### 调试技巧
1. **查看门连接**：在DoorConnectionTable.asset的Inspector中查看所有连接
2. **Gizmos可视化**：在Scene视图中可以看到门的位置（绿色方框）
3. **控制台日志**：观察"门传送"日志，确认连接查询正确
4. **验证工具**：使用"验证连接表"按钮检查配置错误

### 常见问题
**Q: 玩家接触门没有反应？**
- 检查Door组件的doorID是否正确
- 检查Collider2D是否勾选Is Trigger
- 检查玩家是否有"Player"标签

**Q: 场景切换后玩家位置不对？**
- 检查doorSpawnOffset是否合适
- 检查目标门的direction是否正确

**Q: CSV导入失败？**
- 检查CSV格式是否正确（逗号分隔，无多余空格）
- 检查是否有空行或格式错误的行

---

## 扩展功能（未来）
- [ ] 支持门的动画效果
- [ ] 支持门的解锁条件（需要钥匙）
- [ ] 支持单向门（只能从一侧进入）
- [ ] 自动生成小地图（基于连接关系）
- [ ] 可视化编辑器（节点连线方式配置）

---

## 总结
这套系统通过CSV配表实现了灵活的房间连接管理，策划可以方便地在Excel中编辑门连接关系，开发者通过Unity工具一键导入。系统基于EventCenter实现了解耦的事件驱动架构，易于扩展和维护。

**下一步**：按照本文档的代码模板，逐步实现各个组件，然后进行测试验证。
