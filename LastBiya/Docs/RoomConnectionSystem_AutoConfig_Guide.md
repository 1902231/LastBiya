# 房间连接系统 - 自动配置指南

## 📋 概述

本指南介绍了房间连接系统的**自动配置功能**，包括：
1. **自动配置 GameSceneSO 的 doors 列表**（从 CSV 自动生成）
2. **基于门实际位置的玩家出生点计算**（动态计算，无需手动配置）

---

## 🎯 解决的问题

### 问题1：手动配置门列表繁琐易错
**之前**：需要在每个 GameSceneSO 的 Inspector 中手动添加门
**现在**：导入 CSV 时自动配置所有房间的门列表

### 问题2：玩家出生位置不准确
**之前**：使用固定偏移（如 `(8, 0, 0)`），不考虑门的实际位置
**现在**：根据目标门在场景中的实际位置 + 方向偏移计算

---

## 🚀 完整工作流程

### 步骤1：设计房间连接（CSV配置）

创建或编辑 `DoorConnections.csv`：

```csv
RoomID,DoorID,TargetRoomID,TargetDoorID
# === Room_01 的所有门 ===
Room_01,Right_01,Room_02,Left_01
# === Room_02 的所有门 ===
Room_02,Left_01,Room_01,Right_01
Room_02,Right_01,Room_03,Left_01
# === Room_03 的所有门 ===
Room_03,Left_01,Room_02,Right_01
```

**命名约定**（重要！）：
- 门ID必须包含方向关键词：`Left`、`Right`、`Top`、`Bottom`
- 格式：`方向_编号`，如 `Left_01`、`Right_02`
- 系统会根据关键词自动推断门的方向

### 步骤2：导入CSV（自动配置）

1. Unity菜单：`Tools → Import Door Connections`
2. 选择CSV文件路径
3. 拖入或选择 `DoorConnectionTable.asset`
4. 点击 **"导入CSV"** 按钮

**自动执行的操作**：
- ✅ 导入门连接数据到 `DoorConnectionTable.asset`
- ✅ 自动查找所有 `GameSceneSO` 资源
- ✅ 根据 CSV 自动配置每个房间的 `doors` 列表
- ✅ 自动推断门的方向（从门ID）
- ✅ 验证连接的双向性

**Console输出示例**：
```
✅ 自动配置房间 Room_01 的 1 个门
✅ 自动配置房间 Room_02 的 2 个门
✅ 自动配置房间 Room_03 的 1 个门
✅ 共自动配置了 3 个房间的门列表
✅ 导入成功！共导入 3 个门连接
✅ 连接表验证通过！共有 3 个连接
```

### 步骤3：在场景中放置Door组件

#### Room_01 场景

1. 创建 GameObject，命名为 `Door_Right`
2. 添加 `Door` 组件
3. 添加 `BoxCollider2D`，勾选 `Is Trigger`
4. 配置 Door 组件：
   - **Door ID**: `Right_01`（必须与CSV一致）
   - **Door Direction**: `Right`
   - **Is Active**: ✓
5. 调整 Door 的 `transform.position` 到合适位置（如房间右侧出口）

#### Room_02 场景

1. 创建 `Door_Left`：
   - Door ID: `Left_01`
   - Door Direction: `Left`
   - Position: 房间左侧入口

2. 创建 `Door_Right`：
   - Door ID: `Right_01`
   - Door Direction: `Right`
   - Position: 房间右侧出口

### 步骤4：配置 SceneLoader

在 Hierarchy 中选中 `SceneLoader` GameObject，配置：

1. **Connection Table**: 拖入 `DoorConnectionTable.asset`
2. **Door Spawn Offset**: `2`（玩家距离门的偏移距离，单位：Unity单位）
3. **All Rooms**: 添加所有房间的 GameSceneSO
   - Element 0: `Room_01.asset`
   - Element 1: `Room_02.asset`
   - Element 3: `Room_03.asset`

### 步骤5：测试

1. 运行游戏
2. 走到门附近触发传送
3. 观察 Console 输出：

```
玩家从门 Room_01.Right_01 进入
门传送：Room_01.Right_01 → Room_02.Left_01，临时位置：(8, 0, 0)
✅ 找到目标门位置，玩家出生在：(-5.5, 2.0, 0)
```

---

## 🔧 技术细节

### 自动配置原理

#### 1. 收集门信息
```csharp
Dictionary<string, HashSet<string>> roomDoors;
// 结果：
// {
//   "Room_01": ["Right_01"],
//   "Room_02": ["Left_01", "Right_01"],
//   "Room_03": ["Left_01"]
// }
```

#### 2. 查找所有 GameSceneSO
```csharp
string[] guids = AssetDatabase.FindAssets("t:GameSceneSO");
// 查找项目中所有 GameSceneSO 类型的资源
```

#### 3. 自动配置 doors 列表
```csharp
foreach (string doorID in roomDoors[room.roomID])
{
    DoorDirection direction = InferDoorDirection(doorID);
    doors.Add(new DoorData(doorID, direction));
}
room.doors = doors;
```

#### 4. 推断门方向
```csharp
private DoorDirection InferDoorDirection(string doorID)
{
    string lowerID = doorID.ToLower();
    
    if (lowerID.Contains("left"))   return DoorDirection.Left;
    if (lowerID.Contains("right"))  return DoorDirection.Right;
    if (lowerID.Contains("top"))    return DoorDirection.Top;
    if (lowerID.Contains("bottom")) return DoorDirection.Bottom;
    
    return DoorDirection.Right; // 默认值
}
```

### 动态位置计算原理

#### 1. 场景加载前（临时位置）
```csharp
// 使用默认偏移
positionToGo = CalculateSpawnPosition(targetDoor.direction);
// 结果：(8, 0, 0) 或 (-8, 0, 0) 等
```

#### 2. 场景加载后（精确位置）
```csharp
// 从场景中查找目标门
Door[] allDoors = FindObjectsOfType<Door>(true);
foreach (Door door in allDoors)
{
    if (door.doorID == targetDoorID)
    {
        Vector3 doorPosition = door.GetDoorPosition();
        Vector3 offset = GetOffsetByDirection(direction);
        return doorPosition + offset;
    }
}
```

#### 3. 计算示例
```
目标门位置：(-7.5, 2.0, 0)
门方向：Left
偏移量：(-2, 0, 0)  // doorSpawnOffset = 2
最终位置：(-7.5, 2.0, 0) + (-2, 0, 0) = (-9.5, 2.0, 0)
```

---

## 📝 配置清单

### CSV文件
- ✅ 路径：`Assets/Data/DoorConnections.csv`
- ✅ 格式：`RoomID,DoorID,TargetRoomID,TargetDoorID`
- ✅ 门ID包含方向关键词（Left/Right/Top/Bottom）
- ✅ 双向连接（A→B 和 B→A 都要配置）

### GameSceneSO 资源
- ✅ Room ID 与 CSV 中的 RoomID 一致
- ✅ Scene Reference 已配置
- ✅ **Doors 列表会自动配置**（无需手动）

### 场景中的 Door 组件
- ✅ Door ID 与 CSV 一致
- ✅ Door Direction 已配置
- ✅ 添加 Collider2D（Is Trigger = true）
- ✅ Transform.position 设置到合适位置

### SceneLoader 配置
- ✅ Connection Table 已拖入
- ✅ Door Spawn Offset 已设置（推荐 1.5 - 3）
- ✅ All Rooms 包含所有房间

---

## ⚠️ 常见问题

### Q1：导入CSV后，GameSceneSO的doors列表还是空的？

**原因**：GameSceneSO 的 `roomID` 与 CSV 中的 `RoomID` 不匹配

**解决**：
1. 检查 GameSceneSO 的 Room ID 字段
2. 确保与 CSV 中的 RoomID 完全一致（区分大小写）
3. 重新导入 CSV

### Q2：门方向推断错误？

**原因**：门ID不包含方向关键词

**解决**：
1. 修改门ID，包含方向关键词：
   - ✅ `Left_01`, `Right_Door`, `TopExit`
   - ❌ `Door01`, `Exit_A`
2. 或者在 GameSceneSO 中手动修改门的 direction

### Q3：玩家出生位置不对？

**可能原因**：
1. 场景中没有放置 Door 组件
2. Door 的 doorID 与配置不一致
3. Door 的 transform.position 不正确

**解决**：
1. 检查 Console 输出：
   - 如果显示 `未在场景中找到门 XXX`，说明场景中缺少Door组件
   - 如果显示 `使用默认偏移位置`，说明没找到门
2. 确保场景中的 Door.doorID 与 CSV 配置一致
3. 调整 Door 的 transform.position

### Q4：传送后玩家位置是 (0, 0, 0)？

**原因**：`doorSpawnOffset` 为 0 或未配置

**解决**：
1. 选中 SceneLoader GameObject
2. 设置 Door Spawn Offset = 2（或其他合适值）

---

## 🎨 最佳实践

### 1. 门ID命名规范
```
格式：方向_编号
示例：
- Left_01, Left_02
- Right_01, Right_02
- Top_01, Bottom_01
```

### 2. 场景布局建议
```
房间布局：
┌─────────────────────┐
│                     │
│      Room_01        │
│                     │
│                [Door_Right]
└─────────────────────┘

[Door_Left]
┌─────────────────────┐
│                     │
│      Room_02        │
│                     │
│                [Door_Right]
└─────────────────────┘
```

### 3. Door Spawn Offset 设置
- **2D横版游戏**：1.5 - 3（根据角色大小）
- **俯视角游戏**：2 - 4
- **大型场景**：3 - 5

### 4. 测试流程
1. 导入 CSV → 检查 Console 输出
2. 检查 GameSceneSO → 确认 doors 列表已配置
3. 场景中放置 Door → 确认 doorID 一致
4. 运行游戏 → 观察传送效果
5. 调整 Door 位置和 Spawn Offset

---

## 📊 完整示例

### CSV配置
```csv
RoomID,DoorID,TargetRoomID,TargetDoorID
Room_01,Right_01,Room_02,Left_01
Room_02,Left_01,Room_01,Right_01
Room_02,Right_01,Room_03,Left_01
Room_03,Left_01,Room_02,Right_01
```

### 导入后的 GameSceneSO（自动配置）

**Room_01.asset**:
- Room ID: `Room_01`
- Doors:
  - [0] Door ID: `Right_01`, Direction: `Right`

**Room_02.asset**:
- Room ID: `Room_02`
- Doors:
  - [0] Door ID: `Left_01`, Direction: `Left`
  - [1] Door ID: `Right_01`, Direction: `Right`

**Room_03.asset**:
- Room ID: `Room_03`
- Doors:
  - [0] Door ID: `Left_01`, Direction: `Left`

### 场景配置

**Room_01 场景**:
- Door GameObject:
  - Name: `Door_Right`
  - Position: `(10, 0, 0)`
  - Door ID: `Right_01`
  - Door Direction: `Right`

**Room_02 场景**:
- Door GameObject 1:
  - Name: `Door_Left`
  - Position: `(-10, 0, 0)`
  - Door ID: `Left_01`
  - Door Direction: `Left`
- Door GameObject 2:
  - Name: `Door_Right`
  - Position: `(10, 0, 0)`
  - Door ID: `Right_01`
  - Door Direction: `Right`

### 传送效果

玩家在 Room_01 触碰 `Door_Right`：
1. 系统查询：`Room_01.Right_01` → `Room_02.Left_01`
2. 加载 Room_02 场景
3. 查找 Room_02 中的 `Door_Left`（位置：`(-10, 0, 0)`）
4. 计算出生位置：`(-10, 0, 0) + (-2, 0, 0) = (-12, 0, 0)`
5. 玩家出现在 Room_02 的左侧门外

---

## 🎓 总结

### 优势
- ✅ **一键配置**：导入CSV自动配置所有房间
- ✅ **精确定位**：基于门的实际位置计算出生点
- ✅ **灵活布局**：门可以放在场景任意位置
- ✅ **易于维护**：只需修改CSV和场景中的Door位置

### 工作流程
```
CSV配置 → 导入工具 → 自动配置GameSceneSO → 场景放置Door → 运行测试
```

### 核心概念
1. **CSV是数据源**：定义房间连接关系
2. **自动配置**：减少手动工作，保证一致性
3. **动态计算**：基于场景实际布局，而非硬编码

---

## 📚 相关文档

- [房间连接系统用户指南](RoomConnectionSystem_UserGuide.md)
- [场景加载系统集成指南](SceneLoadSystem_Integration_Guide.md)
- [Unity编辑器扩展教程](UnityEditorExtension_DoorImporter.md)
