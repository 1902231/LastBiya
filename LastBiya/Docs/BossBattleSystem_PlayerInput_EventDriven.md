# Boss 战斗系统 - 玩家输入控制（事件驱动版）

**实现时间**: 2026-05-12  
**状态**: ✅ 已实现（事件驱动，完全解耦）

---

## 设计理念

使用**事件驱动架构**实现玩家输入控制，确保各模块完全解耦：
- `BossBattleManager` 只负责触发事件，不知道 `InputManager` 的存在
- `InputManager` 只负责监听事件并响应，不知道 `BossBattleManager` 的存在
- 任何模块都可以触发输入控制事件，不需要依赖 `InputManager`

---

## 架构图

```
BossBattleManager
    ↓ (触发事件)
EventCenter.EventTrigger("DisablePlayerInput")
    ↓ (事件分发)
InputManager (监听事件)
    ↓ (执行操作)
playerNormalMap.Disable()
```

**关键点**：
- ✅ BossBattleManager 不依赖 InputManager
- ✅ InputManager 不依赖 BossBattleManager
- ✅ 通过 EventCenter 解耦

---

## 实现细节

### 1. InputManager.cs - 监听事件

```csharp
void Start()
{
    // 监听输入控制事件
    EventCenter.Instance.AddEventListener("DisablePlayerInput", OnDisablePlayerInput);
    EventCenter.Instance.AddEventListener("EnablePlayerInput", OnEnablePlayerInput);
}

/// <summary>
/// 响应禁用玩家输入事件
/// </summary>
private void OnDisablePlayerInput()
{
    playerNormalMap?.Disable();
    Debug.Log("[InputManager] 玩家输入已禁用");
}

/// <summary>
/// 响应启用玩家输入事件
/// </summary>
private void OnEnablePlayerInput()
{
    playerNormalMap?.Enable();
    Debug.Log("[InputManager] 玩家输入已启用");
}

void OnDestroy()
{
    // 移除事件监听
    if (EventCenter.Instance != null)
    {
        EventCenter.Instance.RemoveEventListener("DisablePlayerInput", OnDisablePlayerInput);
        EventCenter.Instance.RemoveEventListener("EnablePlayerInput", OnEnablePlayerInput);
    }
}
```

### 2. BossBattleManager.cs - 触发事件

```csharp
private void PreparePhase()
{
    DebugLog("[Boss 战] 准备阶段");
    
    // 触发禁用玩家输入事件
    EventCenter.Instance.EventTrigger("DisablePlayerInput");
    
    // 锁定房间门
    // ...
}

private void StartCombatPhase()
{
    DebugLog("[Boss 战] 战斗开始");
    
    // 启动 Boss 战斗逻辑
    currentBoss.StartCombat();
    
    // 播放 Boss 战音乐
    // ...
    
    // 触发启用玩家输入事件
    EventCenter.Instance.EventTrigger("EnablePlayerInput");
}
```

---

## 事件列表

| 事件名 | 参数 | 触发时机 | 监听者 |
|--------|------|---------|--------|
| `DisablePlayerInput` | 无 | Boss 战准备阶段 | `InputManager` |
| `EnablePlayerInput` | 无 | Boss 战斗开始 | `InputManager` |

---

## 优点

### 1. 完全解耦
- `BossBattleManager` 不需要知道 `InputManager` 的存在
- `InputManager` 不需要知道 `BossBattleManager` 的存在
- 两者通过事件系统通信

### 2. 可扩展性强
任何模块都可以触发输入控制事件：

```csharp
// 对话系统
EventCenter.Instance.EventTrigger("DisablePlayerInput");

// 过场动画
EventCenter.Instance.EventTrigger("DisablePlayerInput");

// UI 系统
EventCenter.Instance.EventTrigger("DisablePlayerInput");

// 玩家死亡
EventCenter.Instance.EventTrigger("DisablePlayerInput");
```

### 3. 易于测试
- 可以单独测试 `InputManager` 的事件响应
- 可以单独测试 `BossBattleManager` 的事件触发
- 不需要模拟依赖

### 4. 易于维护
- 修改 `InputManager` 不影响 `BossBattleManager`
- 修改 `BossBattleManager` 不影响 `InputManager`
- 添加新的输入控制场景不需要修改现有代码

---

## 完整流程

```
玩家进入触发器
    ↓
BossBattleManager.PreparePhase()
    ↓
EventCenter.EventTrigger("DisablePlayerInput")
    ↓
InputManager.OnDisablePlayerInput()
    ↓
playerNormalMap.Disable()
    ↓
[玩家输入: 禁用] ← 无法移动、跳跃、攻击
    ↓
Boss 开场动画（2-4 秒）
    ↓
相机聚焦展示（3-5 秒）
    ↓
Boss 血条显示
    ↓
BossBattleManager.StartCombatPhase()
    ↓
EventCenter.EventTrigger("EnablePlayerInput")
    ↓
InputManager.OnEnablePlayerInput()
    ↓
playerNormalMap.Enable()
    ↓
[玩家输入: 启用] ← 可以自由操作了！
    ↓
Boss 战进行中...
```

---

## 扩展示例

### 1. 对话系统

```csharp
public class DialogueManager : MonoBehaviour
{
    public void StartDialogue()
    {
        // 禁用玩家输入
        EventCenter.Instance.EventTrigger("DisablePlayerInput");
        
        // 显示对话框
        // ...
    }
    
    public void EndDialogue()
    {
        // 启用玩家输入
        EventCenter.Instance.EventTrigger("EnablePlayerInput");
        
        // 隐藏对话框
        // ...
    }
}
```

### 2. 过场动画

```csharp
public class CutsceneController : MonoBehaviour
{
    void Start()
    {
        // 禁用玩家输入
        EventCenter.Instance.EventTrigger("DisablePlayerInput");
        
        // 播放过场动画
        StartCoroutine(PlayCutscene());
    }
    
    IEnumerator PlayCutscene()
    {
        // 播放动画...
        yield return new WaitForSeconds(5f);
        
        // 启用玩家输入
        EventCenter.Instance.EventTrigger("EnablePlayerInput");
    }
}
```

### 3. UI 菜单

```csharp
public class PauseMenu : MonoBehaviour
{
    public void OpenMenu()
    {
        // 禁用玩家输入
        EventCenter.Instance.EventTrigger("DisablePlayerInput");
        
        // 显示菜单
        gameObject.SetActive(true);
    }
    
    public void CloseMenu()
    {
        // 启用玩家输入
        EventCenter.Instance.EventTrigger("EnablePlayerInput");
        
        // 隐藏菜单
        gameObject.SetActive(false);
    }
}
```

---

## 调试技巧

### 1. 查看事件触发日志

在 `EventCenter` 中添加日志（如果还没有）：

```csharp
public void EventTrigger(string name)
{
    Debug.Log($"[Event] {name} 触发");
    // ...
}
```

### 2. 查看 InputManager 响应日志

`InputManager` 已经内置了日志：

```
[InputManager] 玩家输入已禁用
[InputManager] 玩家输入已启用
```

### 3. 完整日志示例

```
[Boss 战] 准备阶段
[Event] DisablePlayerInput 触发
[InputManager] 玩家输入已禁用
[Boss 战] 相机聚焦到目标
[Boss 战] 显示 Boss 名称
[Boss 战] 相机恢复跟随玩家
[Boss 战] 显示 Boss 血条
[Boss 战] 战斗开始
[Event] EnablePlayerInput 触发
[InputManager] 玩家输入已启用
```

---

## 常见问题

### Q1: 事件没有触发？

**A:** 检查：
1. `EventCenter` 是否在场景中
2. `InputManager` 是否在场景中
3. `InputManager.Start()` 是否被调用（检查是否有其他脚本禁用了它）

### Q2: 输入没有被禁用？

**A:** 检查：
1. 控制台是否有 `[InputManager] 玩家输入已禁用` 日志
2. `playerNormalMap` 是否为 null
3. `InputActionAsset` 是否正确拖拽到 `InputManager`

### Q3: 输入无法恢复？

**A:** 检查：
1. 控制台是否有 `[InputManager] 玩家输入已启用` 日志
2. 是否有其他系统也触发了 `DisablePlayerInput` 事件
3. Boss 战流程是否正常完成

---

## 性能影响

**几乎为零**：
- 事件触发和监听的开销极小
- `InputActionMap.Disable()` 是 Unity 内置方法，性能开销极小
- 只在 Boss 战开始和结束时触发一次

---

## 与其他系统的兼容性

### 1. 多个系统同时禁用输入

如果需要支持多个系统同时禁用输入（例如 Boss 战 + 对话），可以使用**引用计数**：

```csharp
// InputManager.cs
private int disableInputCount = 0;

private void OnDisablePlayerInput()
{
    disableInputCount++;
    if (disableInputCount == 1)
    {
        playerNormalMap?.Disable();
        Debug.Log("[InputManager] 玩家输入已禁用");
    }
}

private void OnEnablePlayerInput()
{
    disableInputCount--;
    if (disableInputCount <= 0)
    {
        disableInputCount = 0;
        playerNormalMap?.Enable();
        Debug.Log("[InputManager] 玩家输入已启用");
    }
}
```

### 2. 优先级系统

如果需要支持不同优先级的输入控制（例如对话优先级高于 Boss 战），可以使用**优先级队列**。

---

## 总结

### 优点

- ✅ **完全解耦**：各模块通过事件通信，互不依赖
- ✅ **可扩展性强**：任何模块都可以触发输入控制事件
- ✅ **易于测试**：可以单独测试各模块
- ✅ **易于维护**：修改一个模块不影响其他模块
- ✅ **性能开销极小**：只在需要时触发事件

### 使用场景

- Boss 战开场动画
- 对话系统
- 过场动画
- UI 菜单
- 玩家死亡
- 任何需要暂停玩家操作的场景

---

**实现完成时间**: 2026-05-12  
**实现者**: Kiro AI Assistant
