# Boss 战斗系统 - 玩家输入控制

**实现时间**: 2026-05-12  
**状态**: ✅ 已实现

---

## 功能说明

在 Boss 战开场动画期间，玩家无法移动、跳跃、攻击等，确保玩家专注观看开场演出。

---

## 实现方案

### 方案选择：InputManager 输入禁用

**优点**：
- ✅ 不修改玩家脚本
- ✅ 完全阻止输入，玩家无法进行任何操作
- ✅ 可以扩展到其他场景（对话、UI、过场动画等）
- ✅ 符合 Unity Input System 的设计理念
- ✅ 代码简洁，易于维护

**原理**：
- 利用 Unity Input System 的 `InputActionMap.Disable()` 方法
- 禁用整个 `PlayerNormal` ActionMap
- 所有玩家输入（移动、跳跃、攻击、冲刺等）都会被阻止

---

## 修改内容

### 1. `InputManager.cs` - 新增方法

```csharp
/// <summary>
/// 禁用玩家输入（用于过场动画、Boss 开场等）
/// </summary>
public void DisablePlayerInput()
{
    playerNormalMap?.Disable();
}

/// <summary>
/// 启用玩家输入
/// </summary>
public void EnablePlayerInput()
{
    playerNormalMap?.Enable();
}
```

### 2. `BossBattleManager.cs` - 准备阶段禁用输入

```csharp
private void PreparePhase()
{
    DebugLog("[Boss 战] 准备阶段");
    
    // 禁用玩家输入
    InputManager.Instance?.DisablePlayerInput();
    DebugLog("[Boss 战] 玩家输入已禁用");
    
    // 锁定房间门
    // ...
}
```

### 3. `BossBattleManager.cs` - 战斗开始恢复输入

```csharp
private void StartCombatPhase()
{
    DebugLog("[Boss 战] 战斗开始");
    
    // 启动 Boss 战斗逻辑
    currentBoss.StartCombat();
    
    // 播放 Boss 战音乐
    // ...
    
    // 恢复玩家输入
    InputManager.Instance?.EnablePlayerInput();
    DebugLog("[Boss 战] 玩家输入已恢复");
}
```

---

## 完整流程

```
玩家进入触发器
    ↓
┌─────────────────────────────────┐
│ 阶段 1：准备阶段                 │
│ - 禁用玩家输入 ← 玩家无法移动    │
│ - 锁定房间门                     │
│ - 播放开场音效                   │
└─────────────────────────────────┘
    ↓
┌─────────────────────────────────┐
│ 阶段 2：开场动画                 │
│ - Boss 播放开场动画              │
│ - 玩家无法操作 ← 输入被禁用      │
└─────────────────────────────────┘
    ↓
┌─────────────────────────────────┐
│ 阶段 3：相机聚焦（三个子阶段）   │
│ - 相机聚焦到 Boss                │
│ - Boss 名称显示                  │
│ - 相机恢复                       │
│ - 玩家无法操作 ← 输入被禁用      │
└─────────────────────────────────┘
    ↓
┌─────────────────────────────────┐
│ 阶段 4：UI 显示                  │
│ - Boss 血条显示                  │
│ - 玩家无法操作 ← 输入被禁用      │
└─────────────────────────────────┘
    ↓
┌─────────────────────────────────┐
│ 阶段 5：启动战斗                 │
│ - Boss 启动战斗逻辑              │
│ - 播放 Boss 战音乐               │
│ - 恢复玩家输入 ← 玩家可以移动了！│
└─────────────────────────────────┘
    ↓
Boss 战进行中...（玩家可以自由操作）
```

---

## 时间线

### 玩家输入状态

```
游戏开始
    ↓
[玩家输入: 启用] ← 可以自由移动
    ↓
玩家探索地图...
    ↓
进入 Boss 触发器
    ↓
[玩家输入: 禁用] ← 无法移动、跳跃、攻击
    ↓
Boss 开场动画播放（约 2-4 秒）
    ↓
相机聚焦展示（约 3-5 秒）
    ↓
Boss 血条显示
    ↓
[玩家输入: 启用] ← 可以自由移动了！
    ↓
Boss 战进行中...
```

### 总禁用时长

**约 5-9 秒**（取决于配置）：
- Boss 开场动画时长：`Boss.PlayIntroAnimation()` 返回值
- 相机聚焦时长：`CameraZoomDuration` + `BossNameDisplayDuration` + 恢复时长（约 2.5 秒）

---

## 扩展用途

这个输入控制机制可以用于：

### 1. 对话系统
```csharp
// 对话开始时
InputManager.Instance?.DisablePlayerInput();

// 对话结束时
InputManager.Instance?.EnablePlayerInput();
```

### 2. 过场动画
```csharp
// 过场动画开始
InputManager.Instance?.DisablePlayerInput();

// 过场动画结束
InputManager.Instance?.EnablePlayerInput();
```

### 3. UI 界面
```csharp
// 打开背包/菜单
InputManager.Instance?.DisablePlayerInput();

// 关闭背包/菜单
InputManager.Instance?.EnablePlayerInput();
```

### 4. 玩家死亡
```csharp
// 玩家死亡时
InputManager.Instance?.DisablePlayerInput();

// 复活后
InputManager.Instance?.EnablePlayerInput();
```

---

## 调试技巧

### 1. 查看日志

在 `BossBattleManager` 中勾选 `Enable Debug Log`，查看控制台：

```
[Boss 战] 准备阶段
[Boss 战] 玩家输入已禁用
[Boss 战] 相机聚焦到目标
[Boss 战] 显示 Boss 名称
[Boss 战] 相机恢复跟随玩家
[Boss 战] 显示 Boss 血条
[Boss 战] 战斗开始
[Boss 战] 玩家输入已恢复
```

### 2. 测试输入禁用

在开场动画期间：
- 按移动键 → 玩家不移动
- 按跳跃键 → 玩家不跳跃
- 按攻击键 → 玩家不攻击
- 按冲刺键 → 玩家不冲刺

战斗开始后：
- 所有输入恢复正常

### 3. 检查 InputManager

在 Hierarchy 中找到 `InputManager`，确保：
- 它在 Persistent 场景中
- 它是 DontDestroyOnLoad 的
- `Input Actions` 字段已拖拽 `PlayerInputActions` 资产

---

## 常见问题

### Q1: 开场动画期间玩家还能移动？

**A:** 检查：
1. `InputManager` 是否在场景中
2. 控制台是否有 `[Boss 战] 玩家输入已禁用` 日志
3. `InputManager.Instance` 是否为 null

### Q2: 战斗开始后玩家无法移动？

**A:** 检查：
1. 控制台是否有 `[Boss 战] 玩家输入已恢复` 日志
2. 是否有其他系统也禁用了输入
3. 玩家是否处于受伤硬直状态

### Q3: 输入禁用后无法恢复？

**A:** 可能原因：
1. Boss 战流程中断（Boss 或玩家在开场时死亡）
2. 协程被打断
3. `StartCombatPhase()` 没有被调用

**解决方案**：
在 `BossBattleManager.OnDestroy()` 中添加保险：
```csharp
void OnDestroy()
{
    // 确保输入恢复
    InputManager.Instance?.EnablePlayerInput();
    
    // 其他清理...
}
```

---

## 性能影响

**几乎为零**：
- `InputActionMap.Disable()` 是 Unity 内置方法，性能开销极小
- 只在 Boss 战开始和结束时调用一次
- 不涉及每帧检查或计算

---

## 总结

### 优点

- ✅ 不修改玩家脚本，完全解耦
- ✅ 实现简单，只需两行代码
- ✅ 可以扩展到其他场景
- ✅ 符合 Unity Input System 最佳实践
- ✅ 性能开销极小

### 使用场景

- Boss 战开场动画
- 对话系统
- 过场动画
- UI 界面
- 玩家死亡
- 任何需要暂停玩家操作的场景

---

**实现完成时间**: 2026-05-12  
**实现者**: Kiro AI Assistant
