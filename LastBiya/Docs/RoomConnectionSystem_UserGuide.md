# 房间连接系统 - 完整使用指南

## 📋 目录
1. [系统概述](#系统概述)
2. [快速开始](#快速开始)
3. [详细配置步骤](#详细配置步骤)
4. [使用示例](#使用示例)
5. [常见问题](#常见问题)
6. [进阶技巧](#进阶技巧)

---

## 系统概述

### 功能特性
✅ **统一的场景加载系统**
- 支持直接加载和门传送两种模式
- 一个事件、一个数据结构、易于扩展

✅ **CSV配表管理**
- Excel编辑门连接关系
- 一键导入Unity
- 自动验证双向连接

✅ **自动位置计算**
- 根据门方向自动计算玩家生成位置
- 可配置偏移量

✅ **完整的错误提示**
- 配置错误时有详细日志
- 便于调试

### 核心组件
```
GameSceneSO          - 房间配置（场景引用、门列表）
DoorConnectionTable  - 门连接配置表（CSV导入）
SceneLoader          - 场景加载管理器
Door                 - 门组件（挂载在场景物体上）
```

---

## 快速开始

### 5分钟快速体验

#### 1. 创建两个房间配置
```
Project窗口 → 右键 → Create → Game Scene → GameSceneSO
创建 Room01.asset 和 Room02.asset
```

#### 2. 配置房间信息
**Room01.asset**:
- roomID: `Room01`
- sceneReference: 拖入Room01场景
- doors: 添加一个门
  - doorID: `Right_01`
  - direction: `Right`

**Room02.asset**:
- roomID: `Room02`
- sceneReference: 拖入Room02场景
- doors: 添加一个门
  - doorID: `Left_01`
  - direction: `Left`

#### 3. 创建CSV配置文件
在 `Assets/Data/` 创建 `DoorConnections.csv`:
```csv
RoomID,DoorID,TargetRoomID,TargetDoorID
Room01,Right_01,Room02,Left_01
Room02,Left_01,Room01,Right_01
```

#### 4. 导入CSV
```
Unity菜单 → Tools → Import Door Connections
选择CSV文件 → 选择或创建DoorConnectionTable → 点击"导入CSV"
```

#### 5. 在场景中放置门
**Room01场景**:
- 创建空物体，命名 `Door_Right_01`
- 添加 `Door` 组件，设置 doorID = `Right_01`
- 添加 `BoxCollider2D`，勾选 `Is Trigger`
- 放置在场景右侧边缘

**Room02场景**:
- 创建空物体，命名 `Door_Left_01`
- 添加 `Door` 组件，设置 doorID = `Left_01`
- 添加 `BoxCollider2D`，勾选 `Is Trigger`
- 放置在场景左侧边缘

#### 6. 配置SceneLoader
找到SceneLoader物体，在Inspector中配置:
- Connection Table: 拖入 `DoorConnectionTable.asset`
- Door Spawn Offset: `8`
- All Rooms: 添加 `Room01.asset` 和 `Room02.asset`

#### 7. 测试
运行游戏，玩家接触门时会自动传送！

---

## 详细配置步骤

### 步骤1：创建房间配置（GameSceneSO）

#### 1.1 创建ScriptableObject
```
Project窗口 → 右键 → Create → Game Scene → GameSceneSO
```

#### 1.2 配置字段
| 字段 | 说明 | 示例 |
|------|------|------|
| roomID | 房间唯一标识 | `Room01` |
| sceneReference | Addressables场景引用 | 拖入场景资源 |
| sceneType | 场景类型 | `Location` |
| doors | 门列表 | 见下方 |

#### 1.3 配置门列表
点击 `doors` 的 `+` 号添加门：

**示例：Room01有两个门**
```
Door 0:
  doorID: Right_01
  direction: Right

Door 1:
  doorID: Top_01
  direction: Top
```

**门ID命名规则**：
- 格式：`方向_编号`
- 示例：`Left_01`, `Right_01`, `Top_01`, `Bottom_01`
- 同一方向多个门：`Left_01`, `Left_02`, `Left_03`

---

### 步骤2：编写CSV配置文件

#### 2.1 创建CSV文件
在 `Assets/Data/` 文件夹创建 `DoorConnections.csv`

#### 2.2 CSV格式
```csv
RoomID,DoorID,TargetRoomID,TargetDoorID
# === Room01 的所有门 ===
Room01,Right_01,Room02,Left_01
Room01,Top_01,Room03,Bottom_01
# === Room02 的所有门 ===
Room02,Left_01,Room01,Right_01
Room02,Right_01,Room03,Left_01
# === Room03 的所有门 ===
Room03,Bottom_01,Room01,Top_01
Room03,Left_01,Room02,Right_01
```

**格式说明**：
- 第一行是表头，必须保留
- 使用 `#` 开头的行作为注释（方便分组）
- 每行4列：来源房间ID、来源门ID、目标房间ID、目标门ID
- 双向连接需要写两行

**示例解读**：
```csv
Room01,Right_01,Room02,Left_01
```
表示：从Room01的Right_01门进入 → 从Room02的Left_01门出现

---

### 步骤3：导入CSV生成连接表

#### 3.1 打开导入工具
```
Unity菜单栏 → Tools → Import Door Connections
```

#### 3.2 选择CSV文件
- 点击"选择CSV文件"按钮
- 或直接在文本框中输入路径：`Assets/Data/DoorConnections.csv`

#### 3.3 选择或创建目标SO
- 如果已有 `DoorConnectionTable.asset`，拖入到 `目标DoorConnectionTableSO` 字段
- 如果没有，点击"创建新的DoorConnectionTable"按钮

#### 3.4 导入
点击"导入CSV"按钮，查看控制台输出：
```
✅ 导入成功！共导入 6 个门连接
✅ 连接表验证通过！共 6 个连接
```

#### 3.5 验证（可选）
点击"验证连接表"按钮，检查双向连接是否正确。

---

### 步骤4：在场景中放置门物体

#### 4.1 创建门物体
在场景中创建空物体：
```
Hierarchy → 右键 → Create Empty
命名：Door_Right_01（建议用门ID命名）
```

#### 4.2 添加Door组件
```
Inspector → Add Component → Door
```

#### 4.3 配置Door组件
| 字段 | 说明 | 示例 |
|------|------|------|
| doorID | 门ID（必须与GameSceneSO中一致） | `Right_01` |
| isActive | 是否启用 | ✅ |
| triggerOnce | 是否只能触发一次 | ❌ |

#### 4.4 添加碰撞器
```
Inspector → Add Component → Box Collider 2D
勾选 Is Trigger
调整 Size 覆盖门区域（建议：宽1，高2）
```

#### 4.5 放置位置
- **Right门**：放在场景右侧边缘
- **Left门**：放在场景左侧边缘
- **Top门**：放在场景顶部
- **Bottom门**：放在场景底部

**位置建议**：
```
场景大小：20x10
Right门：(9, 0, 0)
Left门：(-9, 0, 0)
Top门：(0, 4.5, 0)
Bottom门：(0, -4.5, 0)
```

---

### 步骤5：配置SceneLoader

#### 5.1 找到SceneLoader物体
通常在一个永不销毁的场景中（如 `PersistentScene`）

#### 5.2 配置门系统字段
| 字段 | 说明 | 配置 |
|------|------|------|
| Connection Table | 门连接配置表 | 拖入 `DoorConnectionTable.asset` |
| Door Spawn Offset | 生成位置偏移量 | `8`（根据场景大小调整） |
| All Rooms | 所有房间列表 | 添加所有 `GameSceneSO` |

**All Rooms配置示例**：
```
Size: 3
Element 0: Room01.asset
Element 1: Room02.asset
Element 2: Room03.asset
```

#### 5.3 其他必要配置
| 字段 | 说明 |
|------|------|
| Player Transform | 玩家Transform引用 |
| First Load Scene | 初始场景 |
| Fade Duration | 淡入淡出时长 |

---

## 使用示例

### 示例1：简单的两房间连接

#### 场景结构
```
Room01 (起始房间)
  └── Door_Right_01 (右侧门)

Room02 (目标房间)
  └── Door_Left_01 (左侧门)
```

#### GameSceneSO配置
**Room01.asset**:
```
roomID: Room01
doors:
  - doorID: Right_01
    direction: Right
```

**Room02.asset**:
```
roomID: Room02
doors:
  - doorID: Left_01
    direction: Left
```

#### CSV配置
```csv
RoomID,DoorID,TargetRoomID,TargetDoorID
Room01,Right_01,Room02,Left_01
Room02,Left_01,Room01,Right_01
```

#### 效果
- 玩家在Room01向右走，接触右侧门
- 传送到Room02，从左侧门出现（位置：x=-8）
- 可以从Room02的左门返回Room01

---

### 示例2：十字路口房间（4个门）

#### 场景结构
```
Room_Center (中心房间)
  ├── Door_Left_01   → Room_West
  ├── Door_Right_01  → Room_East
  ├── Door_Top_01    → Room_North
  └── Door_Bottom_01 → Room_South
```

#### GameSceneSO配置
**Room_Center.asset**:
```
roomID: Room_Center
doors:
  - doorID: Left_01, direction: Left
  - doorID: Right_01, direction: Right
  - doorID: Top_01, direction: Top
  - doorID: Bottom_01, direction: Bottom
```

#### CSV配置
```csv
RoomID,DoorID,TargetRoomID,TargetDoorID
Room_Center,Left_01,Room_West,Right_01
Room_Center,Right_01,Room_East,Left_01
Room_Center,Top_01,Room_North,Bottom_01
Room_Center,Bottom_01,Room_South,Top_01
Room_West,Right_01,Room_Center,Left_01
Room_East,Left_01,Room_Center,Right_01
Room_North,Bottom_01,Room_Center,Top_01
Room_South,Top_01,Room_Center,Bottom_01
```

---

### 示例3：同一方向多个门

#### 场景结构
```
Room_Hall (大厅，右侧有3个门)
  ├── Door_Right_01 → Room_A
  ├── Door_Right_02 → Room_B
  └── Door_Right_03 → Room_C
```

#### GameSceneSO配置
**Room_Hall.asset**:
```
roomID: Room_Hall
doors:
  - doorID: Right_01, direction: Right
  - doorID: Right_02, direction: Right
  - doorID: Right_03, direction: Right
```

#### 场景中放置
```
Door_Right_01: (9, 2, 0)   ← 上方
Door_Right_02: (9, 0, 0)   ← 中间
Door_Right_03: (9, -2, 0)  ← 下方
```

---

## 常见问题

### Q1：玩家接触门没有反应？
**检查清单**：
- [ ] 玩家是否有 `Player` 标签？
- [ ] Door组件的 `isActive` 是否勾选？
- [ ] Collider2D 是否勾选 `Is Trigger`？
- [ ] Door的 `doorID` 是否与GameSceneSO中一致？
- [ ] CSV中是否配置了这个门的连接？

**调试方法**：
```csharp
// 在Door.cs的OnTriggerEnter2D中添加日志
Debug.Log($"检测到碰撞：{collision.tag}");
```

---

### Q2：传送后玩家位置不对？
**原因**：`doorSpawnOffset` 设置不合适

**解决方法**：
1. 检查场景大小（Camera的Size）
2. 调整SceneLoader的 `Door Spawn Offset`
   - 场景宽20：offset = 8-9
   - 场景宽30：offset = 13-14
   - 场景宽40：offset = 18-19

**公式**：
```
offset = (场景宽度 / 2) - 1或2
```

---

### Q3：CSV导入失败？
**常见错误**：
1. **格式错误**
   - 检查是否有多余空格
   - 检查是否用英文逗号分隔
   - 检查是否有空行（空行会被跳过）

2. **编码问题**
   - 使用UTF-8编码保存CSV
   - 避免使用Excel的"CSV UTF-8"格式

3. **路径错误**
   - 确保CSV文件在Unity项目内
   - 路径必须以 `Assets/` 开头

**正确的CSV示例**：
```csv
RoomID,DoorID,TargetRoomID,TargetDoorID
Room01,Right_01,Room02,Left_01
```

---

### Q4：双向连接验证失败？
**错误示例**：
```
❌ 缺少反向连接：Room02.Left_01 → Room01.Right_01
```

**原因**：CSV中只写了单向连接

**解决方法**：
在CSV中添加反向连接：
```csv
Room01,Right_01,Room02,Left_01  ← 已有
Room02,Left_01,Room01,Right_01  ← 添加这行
```

---

### Q5：门ID不存在错误？
**错误日志**：
```
目标房间 Room02 中未找到门 Left_01
```

**原因**：GameSceneSO的doors列表中没有配置这个门

**解决方法**：
1. 打开 `Room02.asset`
2. 在 `doors` 列表中添加：
   - doorID: `Left_01`
   - direction: `Left`

---

### Q6：房间ID不存在错误？
**错误日志**：
```
未找到房间：Room02
```

**原因**：SceneLoader的 `All Rooms` 列表中没有这个房间

**解决方法**：
1. 找到SceneLoader物体
2. 在 `All Rooms` 列表中添加 `Room02.asset`

---

## 进阶技巧

### 技巧1：使用注释组织CSV
```csv
RoomID,DoorID,TargetRoomID,TargetDoorID
# ========== 第一层 ==========
# === Room01 ===
Room01,Right_01,Room02,Left_01
# === Room02 ===
Room02,Left_01,Room01,Right_01
Room02,Right_01,Room03,Left_01

# ========== 第二层 ==========
# === Room03 ===
Room03,Left_01,Room02,Right_01
```

---

### 技巧2：批量修改门偏移量
如果所有房间大小一致，可以统一设置 `doorSpawnOffset`。

如果房间大小不同，可以扩展系统：
```csharp
// 在GameSceneSO中添加
public float customSpawnOffset = 8f;

// 在SceneLoader中使用
float offset = targetRoom.customSpawnOffset;
```

---

### 技巧3：导出CSV备份
使用导入工具的"导出CSV"功能：
1. 选择DoorConnectionTable
2. 点击"导出CSV"
3. 选择保存位置

**用途**：
- 备份配置
- 分享给团队成员
- 版本控制

---

### 技巧4：快速测试门连接
在SceneLoader中添加调试方法：
```csharp
[ContextMenu("测试门连接")]
private void TestDoorConnection()
{
    if (connectionTable == null)
    {
        Debug.LogError("未配置连接表！");
        return;
    }

    Debug.Log("=== 门连接测试 ===");
    foreach (var conn in connectionTable.connections)
    {
        Debug.Log(conn.ToString());
    }
}
```

使用：
```
SceneLoader → Inspector → 右键 → 测试门连接
```

---

### 技巧5：可视化门的位置
在Door.cs中添加Gizmos：
```csharp
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
```

---

## 配置检查清单

### 房间配置检查
- [ ] GameSceneSO已创建
- [ ] roomID已设置（唯一）
- [ ] sceneReference已配置
- [ ] doors列表已添加所有门
- [ ] 每个门的doorID和direction已设置

### CSV配置检查
- [ ] CSV文件已创建
- [ ] 表头正确：`RoomID,DoorID,TargetRoomID,TargetDoorID`
- [ ] 所有连接都是双向的
- [ ] 没有多余空格
- [ ] 使用UTF-8编码

### 场景配置检查
- [ ] 门物体已创建
- [ ] Door组件已添加
- [ ] doorID与GameSceneSO一致
- [ ] Collider2D已添加并勾选Is Trigger
- [ ] 门位置合理（在场景边缘）

### SceneLoader配置检查
- [ ] Connection Table已配置
- [ ] Door Spawn Offset已设置
- [ ] All Rooms包含所有房间
- [ ] Player Transform已配置

---

## 总结

### 核心流程
```
1. 创建GameSceneSO（配置房间和门）
2. 编写CSV（配置门连接关系）
3. 导入CSV（生成DoorConnectionTable）
4. 放置Door物体（在场景中）
5. 配置SceneLoader（引用配置表和房间列表）
6. 测试运行
```

### 关键点
- **roomID和doorID必须唯一且一致**
- **CSV必须配置双向连接**
- **门物体必须有Collider2D (Is Trigger)**
- **玩家必须有Player标签**

### 扩展性
未来可以轻松添加：
- 传送门系统
- 快速旅行
- 剧情传送
- 只需添加新的 `SceneLoadMode` 枚举值

---

**祝你使用愉快！有问题随时查阅本文档。**
