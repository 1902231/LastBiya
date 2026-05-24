# 场景加载系统集成方案

## 📋 目录
1. [集成目标](#集成目标)
2. [当前架构分析](#当前架构分析)
3. [集成后架构设计](#集成后架构设计)
4. [详细实现步骤](#详细实现步骤)
5. [代码示例](#代码示例)
6. [测试验证](#测试验证)
7. [常见问题](#常见问题)

---

## 集成目标

### 要解决的问题
- ❌ 当前有两套独立的场景加载系统
- ❌ 两个事件名称、两个数据结构、两个处理方法
- ❌ 代码冗余，维护成本高
- ❌ 扩展性差，添加新传送方式需要新增整套系统

### 集成后的效果
- ✅ 统一为一套场景加载系统
- ✅ 一个事件、一个数据结构、一个处理方法
- ✅ 代码简洁，易于维护
- ✅ 扩展性强，添加新方式只需增加枚举值

---

## 当前架构分析

### 系统1：直接加载（原有）
```
触发：手动调用或TeleportPoint
数据：SceneLoadData { sceneToLoad, targetPosition, fadeScreen }
事件："SceneLoadRequest"
处理：OnLoadRequestEvent()
```

### 系统2：门传送（新增）
```
触发：Door组件
数据：DoorTransitionData { fromRoomID, fromDoorID }
事件："DoorTransitionRequest"
处理：OnDoorTransitionRequest()
```

### 问题分析
```csharp
// OnDoorTransitionRequest() 的最后
var loadData = new SceneLoadData(targetRoom, spawnPosition, true);
OnLoadRequestEvent(loadData);  // ← 最终还是调用直接加载
```
**结论**：门传送本质就是场景加载，只是多了"查询连接表"这一步。

---

## 集成后架构设计

### 统一数据流
```
触发源（Door / 其他）
    ↓
创建 SceneLoadData（包含 loadMode）
    ↓
触发事件 "SceneLoadRequest"
    ↓
SceneLoader.OnLoadRequestEvent()
    ↓
根据 loadMode 分发处理
    ├── Direct → HandleDirectLoad()
    └── DoorTransition → HandleDoorTransition()
    ↓
统一的场景加载流程
```

### 核心组件

#### 1. SceneLoadData（统一数据结构）
```csharp
public struct SceneLoadData
{
    // 方式1：直接加载
    public GameSceneSO sceneToLoad;
    public Vector3 targetPosition;
    
    // 方式2：门传送
    public string fromRoomID;
    public string fromDoorID;
    
    // 共用
    public bool fadeScreen;
    public SceneLoadMode loadMode;  // ← 关键：标识加载模式
}
```

#### 2. SceneLoadMode（加载模式枚举）
```csharp
public enum SceneLoadMode
{
    Direct,          // 直接加载
    DoorTransition   // 门传送
}
```

#### 3. SceneLoader（统一处理）
```csharp
private void OnLoadRequestEvent(SceneLoadData data)
{
    // 根据模式分发
    switch (data.loadMode)
    {
        case SceneLoadMode.Direct:
            HandleDirectLoad(data);
            break;
        case SceneLoadMode.DoorTransition:
            HandleDoorTransition(data);
            break;
    }
    
    // 后续统一加载流程
    // ...
}
```

---

## 详细实现步骤

### 步骤1：扩展 SceneLoadData.cs

**文件**：`LastBiya/Assets/Scripts/Transition/SceneLoadData.cs`

#### 1.1 添加门传送字段
在结构体中添加：
```csharp
// 方式2：门传送
/// <summary>
/// 来源房间ID（门传送用）
/// </summary>
public string fromRoomID;

/// <summary>
/// 来源门ID（门传送用）
/// </summary>
public string fromDoorID;
```

#### 1.2 添加加载模式字段
```csharp
/// <summary>
/// 场景加载模式
/// </summary>
public SceneLoadMode loadMode;
```

#### 1.3 修改原有构造函数
在第一个构造函数中，初始化所有字段：
```csharp
public SceneLoadData(GameSceneSO sceneToLoad, Vector3 targetPosition, bool fadeScreen)
{
    // 直接加载字段
    this.sceneToLoad = sceneToLoad;
    this.targetPosition = targetPosition;
    this.fadeScreen = fadeScreen;
    
    // 门传送字段设为默认值
    this.fromRoomID = null;
    this.fromDoorID = null;
    
    // 标记为直接加载模式
    this.loadMode = SceneLoadMode.Direct;
}
```

#### 1.4 添加门传送构造函数
```csharp
/// <summary>
/// 门传送模式构造函数
/// </summary>
public SceneLoadData(string fromRoomID, string fromDoorID, bool fadeScreen = true)
{
    // 门传送字段
    this.fromRoomID = fromRoomID;
    this.fromDoorID = fromDoorID;
    this.fadeScreen = fadeScreen;
    
    // 直接加载字段设为默认值
    this.sceneToLoad = null;
    this.targetPosition = Vector3.zero;
    
    // 标记为门传送模式
    this.loadMode = SceneLoadMode.DoorTransition;
}
```

#### 1.5 添加枚举定义
在文件末尾添加：
```csharp
/// <summary>
/// 场景加载模式枚举
/// </summary>
public enum SceneLoadMode
{
    /// <summary>
    /// 直接加载：指定场景和位置
    /// </summary>
    Direct,
    
    /// <summary>
    /// 门传送：通过门ID查询连接表
    /// </summary>
    DoorTransition
}
```

---

### 步骤2：重构 SceneLoader.cs

**文件**：`LastBiya/Assets/Scripts/Transition/SceneLoader.cs`

#### 2.1 修改 OnLoadRequestEvent() 方法

**原有代码**：
```csharp
private void OnLoadRequestEvent(SceneLoadData data)
{
    if (isLoading) return;
    isLoading = true;
    
    sceneToLoad = data.sceneToLoad;
    positionToGo = data.targetPosition;
    this.fadeScreen = data.fadeScreen;

    if (currentLoadedScene != null)
        StartCoroutine(UnLoadPrevioussScene());
    else
        LoadNewScene();
}
```

**修改为**：
```csharp
private void OnLoadRequestEvent(SceneLoadData data)
{
    if (isLoading) return;
    isLoading = true;
    this.fadeScreen = data.fadeScreen;

    // 根据加载模式分发处理
    switch (data.loadMode)
    {
        case SceneLoadMode.Direct:
            HandleDirectLoad(data);
            break;
            
        case SceneLoadMode.DoorTransition:
            HandleDoorTransition(data);
            break;
            
        default:
            Debug.LogError($"未知的加载模式：{data.loadMode}");
            isLoading = false;
            return;
    }

    // 后续统一加载流程
    if (currentLoadedScene != null)
        StartCoroutine(UnLoadPrevioussScene());
    else
        LoadNewScene();
}
```

#### 2.2 创建 HandleDirectLoad() 方法

将原有的直接加载逻辑提取为独立方法：
```csharp
/// <summary>
/// 处理直接加载模式
/// </summary>
private void HandleDirectLoad(SceneLoadData data)
{
    sceneToLoad = data.sceneToLoad;
    positionToGo = data.targetPosition;
    
    Debug.Log($"直接加载场景：{sceneToLoad.name}，位置：{positionToGo}");
}
```

#### 2.3 创建 HandleDoorTransition() 方法

将 `OnDoorTransitionRequest()` 的逻辑移到这里：
```csharp
/// <summary>
/// 处理门传送模式
/// </summary>
private void HandleDoorTransition(SceneLoadData data)
{
    if (connectionTable == null)
    {
        Debug.LogError("DoorConnectionTable 未配置！");
        isLoading = false;
        return;
    }

    // 1. 查询连接表
    var connection = connectionTable.GetConnection(data.fromRoomID, data.fromDoorID);
    if (connection == null)
    {
        Debug.LogError($"未找到门连接：{data.fromRoomID}.{data.fromDoorID}");
        isLoading = false;
        return;
    }

    // 2. 获取目标房间
    GameSceneSO targetRoom = GetRoomByID(connection.Value.toRoomID);
    if (targetRoom == null)
    {
        Debug.LogError($"未找到目标房间：{connection.Value.toRoomID}");
        isLoading = false;
        return;
    }

    // 3. 获取目标门
    DoorData targetDoor = targetRoom.GetDoor(connection.Value.toDoorID);
    if (targetDoor == null)
    {
        Debug.LogError($"目标房间 {targetRoom.roomID} 中未找到门 {connection.Value.toDoorID}");
        isLoading = false;
        return;
    }

    // 4. 计算生成位置
    sceneToLoad = targetRoom;
    positionToGo = CalculateSpawnPosition(targetDoor.direction);
    
    Debug.Log($"门传送：{data.fromRoomID}.{data.fromDoorID} → {targetRoom.roomID}.{connection.Value.toDoorID}，位置：{positionToGo}");
}
```

#### 2.4 删除 OnDoorTransitionRequest() 方法

删除整个方法：
```csharp
// 删除这个方法
private void OnDoorTransitionRequest(DoorTransitionData doorData)
{
    // ...
}
```

#### 2.5 修改事件订阅

**OnEnable() 方法**：
```csharp
private void OnEnable()
{
    // 只订阅一个事件
    EventCenter.Instance.AddEventListener<SceneLoadData>("SceneLoadRequest", OnLoadRequestEvent);
    
    // 删除这行
    // EventCenter.Instance.AddEventListener<DoorTransitionData>("DoorTransitionRequest", OnDoorTransitionRequest);
}
```

**OnDisable() 方法**：
```csharp
private void OnDisable()
{
    // 只取消订阅一个事件
    EventCenter.Instance.RemoveEventListener<SceneLoadData>("SceneLoadRequest", OnLoadRequestEvent);
    
    // 删除这行
    // EventCenter.Instance.RemoveEventListener<DoorTransitionData>("DoorTransitionRequest", OnDoorTransitionRequest);
}
```

---

### 步骤3：修改 Door.cs

**文件**：`LastBiya/Assets/Scripts/Transition/Door.cs`

#### 3.1 修改 TriggerTransition() 方法

**原有代码**：
```csharp
public void TriggerTransition()
{
    if (currentRoom == null)
    {
        Debug.LogError("当前房间信息为空，无法触发传送！");
        return;
    }

    Debug.Log($"玩家从门 {currentRoom.roomID}.{doorID} 进入");

    var doorData = new DoorTransitionData(currentRoom.roomID, doorID);
    EventCenter.Instance.EventTrigger<DoorTransitionData>("DoorTransitionRequest", doorData);
}
```

**修改为**：
```csharp
public void TriggerTransition()
{
    if (currentRoom == null)
    {
        Debug.LogError("当前房间信息为空，无法触发传送！");
        return;
    }

    Debug.Log($"玩家从门 {currentRoom.roomID}.{doorID} 进入");

    // 使用新的构造函数（门传送模式）
    var loadData = new SceneLoadData(currentRoom.roomID, doorID, true);
    
    // 触发统一的场景加载事件
    EventCenter.Instance.EventTrigger<SceneLoadData>("SceneLoadRequest", loadData);
}
```

#### 3.2 删除 DoorTransitionData 结构体

删除文件末尾的结构体定义：
```csharp
// 删除这个结构体
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

## 代码示例

### 完整的 SceneLoadData.cs
```csharp
using UnityEngine;

/// <summary>
/// 场景加载数据结构（统一版本）
/// 支持多种加载模式：直接加载、门传送等
/// </summary>
public struct SceneLoadData
{
    // ========== 方式1：直接加载 ==========
    public GameSceneSO sceneToLoad;
    public Vector3 targetPosition;
    
    // ========== 方式2：门传送 ==========
    public string fromRoomID;
    public string fromDoorID;
    
    // ========== 共用字段 ==========
    public bool fadeScreen;
    public SceneLoadMode loadMode;

    // ========== 构造函数1：直接加载 ==========
    public SceneLoadData(GameSceneSO sceneToLoad, Vector3 targetPosition, bool fadeScreen)
    {
        this.sceneToLoad = sceneToLoad;
        this.targetPosition = targetPosition;
        this.fadeScreen = fadeScreen;
        this.fromRoomID = null;
        this.fromDoorID = null;
        this.loadMode = SceneLoadMode.Direct;
    }

    // ========== 构造函数2：门传送 ==========
    public SceneLoadData(string fromRoomID, string fromDoorID, bool fadeScreen = true)
    {
        this.fromRoomID = fromRoomID;
        this.fromDoorID = fromDoorID;
        this.fadeScreen = fadeScreen;
        this.sceneToLoad = null;
        this.targetPosition = Vector3.zero;
        this.loadMode = SceneLoadMode.DoorTransition;
    }
}

/// <summary>
/// 场景加载模式枚举
/// </summary>
public enum SceneLoadMode
{
    Direct,          // 直接加载
    DoorTransition   // 门传送
}
```

### SceneLoader.cs 关键部分
```csharp
private void OnEnable()
{
    EventCenter.Instance.AddEventListener<SceneLoadData>("SceneLoadRequest", OnLoadRequestEvent);
}

private void OnDisable()
{
    EventCenter.Instance.RemoveEventListener<SceneLoadData>("SceneLoadRequest", OnLoadRequestEvent);
}

private void OnLoadRequestEvent(SceneLoadData data)
{
    if (isLoading) return;
    isLoading = true;
    this.fadeScreen = data.fadeScreen;

    switch (data.loadMode)
    {
        case SceneLoadMode.Direct:
            HandleDirectLoad(data);
            break;
        case SceneLoadMode.DoorTransition:
            HandleDoorTransition(data);
            break;
    }

    if (currentLoadedScene != null)
        StartCoroutine(UnLoadPrevioussScene());
    else
        LoadNewScene();
}

private void HandleDirectLoad(SceneLoadData data)
{
    sceneToLoad = data.sceneToLoad;
    positionToGo = data.targetPosition;
}

private void HandleDoorTransition(SceneLoadData data)
{
    // 查询连接表 → 获取目标房间 → 计算位置
    // （完整代码见步骤2.3）
}
```

### Door.cs 关键部分
```csharp
public void TriggerTransition()
{
    if (currentRoom == null)
    {
        Debug.LogError("当前房间信息为空，无法触发传送！");
        return;
    }

    // 使用门传送模式
    var loadData = new SceneLoadData(currentRoom.roomID, doorID, true);
    EventCenter.Instance.EventTrigger<SceneLoadData>("SceneLoadRequest", loadData);
}
```

---

## 测试验证

### 测试清单

#### 1. 编译测试
- [ ] 代码无编译错误
- [ ] 无警告信息

#### 2. 直接加载测试（如果有）
- [ ] 原有的直接加载方式仍然正常
- [ ] 玩家传送到正确位置
- [ ] 淡入淡出效果正常

#### 3. 门传送测试
- [ ] 玩家接触门触发传送
- [ ] 从门A进入，从门B出现
- [ ] 双向传送正常（A→B 和 B→A）
- [ ] 玩家生成位置正确
- [ ] 淡入淡出效果正常

#### 4. 错误处理测试
- [ ] 门连接配置错误时有正确提示
- [ ] 房间ID不存在时有正确提示
- [ ] 门ID不存在时有正确提示

#### 5. 控制台日志
- [ ] 无错误日志
- [ ] 传送日志信息正确

---

## 常见问题

### Q1：修改后原有功能会受影响吗？
**A**：不会。原有的直接加载方式完全兼容，只是内部实现改为通过 `HandleDirectLoad()` 处理。

### Q2：为什么要用 switch 判断模式？
**A**：这是策略模式的实现方式，便于扩展。未来添加新的传送方式只需：
1. 在枚举中添加新值
2. 在 switch 中添加新 case
3. 实现新的 Handle 方法

### Q3：数据结构变大了会影响性能吗？
**A**：影响极小。`SceneLoadData` 是结构体，多几个字段只增加几个字节，对性能影响可以忽略。

### Q4：如果未来要添加传送门功能怎么办？
**A**：非常简单：
```csharp
// 1. 添加枚举值
public enum SceneLoadMode
{
    Direct,
    DoorTransition,
    Teleport  // ← 新增
}

// 2. 添加构造函数
public SceneLoadData(string teleportID, bool fadeScreen = true)
{
    // ...
    this.loadMode = SceneLoadMode.Teleport;
}

// 3. 添加处理方法
case SceneLoadMode.Teleport:
    HandleTeleport(data);
    break;
```

### Q5：修改顺序重要吗？
**A**：是的！建议按文档顺序：
1. 先扩展 `SceneLoadData`（添加新功能）
2. 再重构 `SceneLoader`（兼容新旧）
3. 最后修改 `Door`（使用新系统）
4. 删除旧代码（清理）

这样可以避免编译错误。

---

## 修改检查清单

### 文件修改清单
- [ ] `SceneLoadData.cs` - 添加字段、构造函数、枚举
- [ ] `SceneLoader.cs` - 重构 `OnLoadRequestEvent()`，添加 Handle 方法
- [ ] `SceneLoader.cs` - 修改事件订阅
- [ ] `SceneLoader.cs` - 删除 `OnDoorTransitionRequest()`
- [ ] `Door.cs` - 修改 `TriggerTransition()`
- [ ] `Door.cs` - 删除 `DoorTransitionData` 结构体

### 代码检查清单
- [ ] 所有构造函数初始化了所有字段
- [ ] 事件订阅只有一个 `SceneLoadRequest`
- [ ] 没有 `DoorTransitionRequest` 事件
- [ ] 没有 `DoorTransitionData` 结构体
- [ ] `switch` 语句包含所有枚举值

---

## 总结

### 集成前
```
两套系统 → 两个事件 → 两个数据结构 → 代码冗余
```

### 集成后
```
一套系统 → 一个事件 → 一个数据结构 → 代码简洁
```

### 核心改变
1. **统一数据结构**：`SceneLoadData` 支持多种模式
2. **统一事件**：只有 `"SceneLoadRequest"`
3. **统一处理**：`OnLoadRequestEvent()` 根据模式分发

### 扩展性
未来添加新传送方式只需：
- 枚举 +1
- 构造函数 +1
- Handle方法 +1

**简单、清晰、易维护！**
