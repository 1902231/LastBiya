# 门传送系统 - 快速开始

## 🚀 5分钟快速配置

### 1️⃣ 创建CSV文件（30秒）

创建 `Assets/Data/DoorConnections.csv`：

```csv
RoomID,DoorID,TargetRoomID,TargetDoorID
Room_01,Right_01,Room_02,Left_01
Room_02,Left_01,Room_01,Right_01
```

**规则**：门ID必须包含方向（Left/Right/Top/Bottom）

---

### 2️⃣ 导入CSV（10秒）

1. Unity菜单：`Tools → Import Door Connections`
2. 选择CSV文件
3. 点击 **"导入CSV"**

✅ 系统自动配置所有 GameSceneSO 的 doors 列表！

---

### 3️⃣ 场景中放置Door（2分钟）

#### Room_01 场景：
```
GameObject: Door_Right
├─ Door (Script)
│  ├─ Door ID: Right_01
│  └─ Door Direction: Right
└─ BoxCollider2D (Is Trigger: ✓)
```

#### Room_02 场景：
```
GameObject: Door_Left
├─ Door (Script)
│  ├─ Door ID: Left_01
│  └─ Door Direction: Left
└─ BoxCollider2D (Is Trigger: ✓)
```

**重要**：调整 Door 的 `transform.position` 到合适位置！

---

### 4️⃣ 配置SceneLoader（1分钟）

选中 `SceneLoader` GameObject：
- **Connection Table**: 拖入 `DoorConnectionTable.asset`
- **Door Spawn Offset**: `2`
- **All Rooms**: 添加所有 GameSceneSO

---

### 5️⃣ 测试（1分钟）

运行游戏，走到门附近，触发传送！

---

## 📋 配置检查清单

- [ ] CSV文件已创建，门ID包含方向关键词
- [ ] 已通过 `Tools → Import Door Connections` 导入
- [ ] GameSceneSO 的 doors 列表已自动配置
- [ ] 场景中已放置 Door 组件，doorID 与 CSV 一致
- [ ] Door 添加了 Collider2D（Is Trigger = true）
- [ ] SceneLoader 已配置 Connection Table 和 All Rooms
- [ ] 玩家有 "Player" Tag

---

## ⚠️ 常见错误

### 错误1：未找到门连接
```
未找到门连接：Room_01.Rgiht_01
```
**原因**：门ID拼写错误
**解决**：检查场景中 Door 组件的 doorID 字段

### 错误2：doors列表为空
```
房间 Room_02 的doors列表为空！
```
**原因**：GameSceneSO 的 roomID 与 CSV 不匹配
**解决**：检查 GameSceneSO 的 Room ID 字段

### 错误3：未找到目标门
```
未在场景中找到门 Left_01
```
**原因**：场景中没有放置 Door 组件
**解决**：在目标场景中添加 Door GameObject

---

## 🎯 核心概念

### 自动配置流程
```
CSV → 导入工具 → 自动配置 GameSceneSO.doors
```

### 传送流程
```
玩家触碰Door → 查询CSV连接表 → 加载目标场景 
→ 查找目标门位置 → 计算出生点 → 传送玩家
```

### 位置计算
```
玩家出生位置 = 目标门位置 + 方向偏移
```

---

## 📚 详细文档

- [自动配置完整指南](RoomConnectionSystem_AutoConfig_Guide.md)
- [系统用户手册](RoomConnectionSystem_UserGuide.md)
- [集成指南](SceneLoadSystem_Integration_Guide.md)
