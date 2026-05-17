# Boss 战斗系统 - 设置指南

## 系统概述

Boss 战斗系统通过事件驱动实现解耦，支持：
- 开场动画序列
- 相机聚焦缩放（DOTween）
- UI 自动绑定
- 房间门锁定/解锁
- Boss/玩家死亡的差异化处理

## 文件结构

```
Assets/Scripts/
├── Boss/
│   ├── IBoss.cs                    # Boss 接口
│   ├── BossBattleTrigger.cs        # 触发器（持有配置）
│   ├── BossBattleManager.cs        # 管理器（协调流程）
│   └── Boss01/
│       └── Boss01.cs               # 实现 IBoss 接口
├── CameraController.cs             # 相机控制器
└── UI/Game/
    └── BossUIController.cs         # UI 控制器（已修改）
```

## 场景设置步骤

### 1. 在 Persistent 场景中设置管理器

```
Persistent 场景:
├── Managers (空物体)
│   ├── InputManager (已有)
│   ├── BossBattleManager (新建)
│   └── CameraController (新建)
│       └── 拖拽 Main Virtual Camera
└── Canvas
    └── BossUI
        └── BossUIController (已有，无需修改)
```

**CameraController 设置：**
1. 创建空物体 "CameraController"
2. 添加 `CameraController.cs` 脚本
3. 拖拽场景中的 Cinemachine Virtual Camera 到 `Main Virtual Camera` 字段

**BossBattleManager 设置：**
1. 创建空物体 "BossBattleManager"
2. 添加 `BossBattleManager.cs` 脚本
3. 勾选 `Enable Debug Log`（可选，用于调试）

---

### 2. 在 Boss 房间场景中设置

```
Room_Boss_01 场景:
├── Boss01 (GameObject)
│   ├── Boss01.cs (已实现 IBoss)
│   ├── Animator (可选)
│   └── Behavior Tree (必须，默认禁用)
│
├── CameraFocus (空物体，可选)
│   └── 放在你希望相机聚焦的位置
│
├── BossTrigger (GameObject)
│   ├── BoxCollider2D (Is Trigger = true)
│   └── BossBattleTrigger.cs
│
├── Door_Left (GameObject)
└── Door_Right (GameObject)
```

---

### 3. 配置 Boss01

**必须设置：**
1. 添加 `Behavior Tree` 组件
2. **取消勾选** `Start When Enabled`（重要！）
3. 配置行为树逻辑

**可选设置：**
- 添加 `Animator` 组件
- 在 `Boss01.PlayIntroAnimation()` 中播放开场动画

---

### 4. 配置 BossBattleTrigger

创建触发器物体：
1. 创建空物体 "BossTrigger"
2. 添加 `BoxCollider2D`，勾选 `Is Trigger`
3. 调整碰撞体大小和位置（玩家进入时触发）
4. 添加 `BossBattleTrigger.cs` 脚本

**Inspector 配置：**

#### Boss 配置
- `Boss Object`：拖拽 Boss01 物体

#### 房间门配置
- `Room Doors`：拖拽所有房间门（数组）

#### 相机配置
- `Camera Focus Point`：拖拽 CameraFocus 物体（可选，留空则聚焦 Boss）
- `Camera Zoom Size`：8（相机缩放大小）
- `Camera Zoom Duration`：1.5（缩放过渡时间）

#### 音乐配置
- `Battle Music`：拖拽 Boss 战音乐（可选）
- `Intro SFX`：拖拽开场音效（可选）

#### 触发设置
- `Trigger Once`：勾选（只触发一次）

---

## 工作流程

### 完整流程图

```
玩家进入触发器
    ↓
BossBattleTrigger 触发事件
    ↓
BossBattleManager 接收事件
    ↓
┌─────────────────────────────────┐
│ 阶段 1：准备阶段                 │
│ - 锁定房间门                     │
│ - 播放开场音效                   │
└─────────────────────────────────┘
    ↓
┌─────────────────────────────────┐
│ 阶段 2：开场动画                 │
│ - Boss.PlayIntroAnimation()     │
│ - 等待动画播放完成               │
└─────────────────────────────────┘
    ↓
┌─────────────────────────────────┐
│ 阶段 3：相机聚焦                 │
│ - 相机聚焦到 Boss/聚焦点         │
│ - DOTween 平滑缩放               │
└─────────────────────────────────┘
    ↓
┌─────────────────────────────────┐
│ 阶段 4：UI 显示                  │
│ - 触发 "BossEntered" 事件        │
│ - BossUIController 自动绑定      │
└─────────────────────────────────┘
    ↓
┌─────────────────────────────────┐
│ 阶段 5：启动战斗                 │
│ - Boss.StartCombat()             │
│ - 启动行为树                     │
│ - 播放 Boss 战音乐               │
│ - 相机恢复跟随玩家               │
└─────────────────────────────────┘
    ↓
Boss 战进行中...
    ↓
Boss 或玩家死亡
    ↓
结束序列（解锁门、隐藏 UI）
```

---

## 代码示例

### 添加新 Boss

```csharp
using UnityEngine;
using BehaviorDesigner.Runtime;

public class Boss02 : MonoBehaviour, IDamageable, IUnit, IBoss
{
    // IUnit 实现
    public string UnitName => "Boss02";
    public float CurrentHP => currentHP;
    public float MaxHP => maxHP;
    // ...
    
    // IBoss 实现
    public string BossID => "Boss02";
    public bool IsDead => currentHP <= 0;
    public Transform Transform => transform;
    
    private BehaviorTree behaviorTree;
    private Animator animator;
    
    void Awake()
    {
        behaviorTree = GetComponent<BehaviorTree>();
        animator = GetComponent<Animator>();
        
        if (behaviorTree != null)
            behaviorTree.DisableBehavior();
    }
    
    public void StartCombat()
    {
        if (behaviorTree != null)
            behaviorTree.EnableBehavior();
    }
    
    public void StopCombat()
    {
        if (behaviorTree != null)
            behaviorTree.DisableBehavior();
    }
    
    public float PlayIntroAnimation()
    {
        if (animator != null)
        {
            animator.Play("Boss02_Intro");
            return 4f; // 动画时长
        }
        return 0f;
    }
    
    // ... 其他 Boss 逻辑 ...
}
```

---

## 调试技巧

### 1. 查看日志
在 `BossBattleManager` 中勾选 `Enable Debug Log`，查看控制台输出：
```
[Boss 战] 开始：Boss01
[Boss 战] 准备阶段
[Boss 战] 相机聚焦
[Boss 战] UI 显示
[Boss 战] 战斗开始
```

### 2. 检查事件触发
在 `EventCenter` 中添加日志：
```csharp
public void EventTrigger<T>(string name, T info)
{
    Debug.Log($"[Event] {name} 触发，参数：{info}");
    // ...
}
```

### 3. 验证 Boss 引用
在 `BossBattleTrigger.Start()` 中会自动验证 Boss 是否实现 `IBoss` 接口。

---

## 常见问题

### Q1: Boss 行为树没有启动？
**A:** 检查：
1. Behavior Tree 组件是否添加
2. `Start When Enabled` 是否取消勾选
3. `Boss01.Awake()` 是否调用了 `DisableBehavior()`

### Q2: 相机没有聚焦？
**A:** 检查：
1. `CameraController` 是否在 Persistent 场景中
2. `Main Virtual Camera` 是否拖拽正确
3. DOTween 是否导入项目

### Q3: UI 没有显示？
**A:** 检查：
1. `BossUIController` 是否监听了 `"BossEntered"` 事件
2. `BossUI` 物体是否在 Persistent 场景的 Canvas 下
3. 控制台是否有 `[Boss 战] UI 显示` 日志

### Q4: 玩家死亡后 Boss 还在攻击？
**A:** 检查：
1. `PlayerController` 是否实现了 `IUnit` 接口
2. `UnitName` 是否返回 "Player"
3. 玩家受伤时是否触发了 `"HPChanged"` 事件

---

## 扩展功能

### 1. 添加相机震动
在 Boss 攻击时调用：
```csharp
CameraController.Instance?.Shake(0.3f, 0.5f);
```

### 2. 添加多阶段 Boss
在 `Boss01.cs` 中监听血量变化：
```csharp
void Update()
{
    if (currentHP <= maxHP * 0.5f && !phase2Triggered)
    {
        phase2Triggered = true;
        TriggerPhase2();
    }
}

private void TriggerPhase2()
{
    // 播放阶段 2 过场动画
    // 切换行为树
}
```

### 3. 集成 AudioManager
取消注释 `BossBattleManager` 中的音乐相关代码：
```csharp
// AudioManager.Instance?.PlaySFX(currentTrigger.IntroSFX);
// AudioManager.Instance?.PlayMusic(currentTrigger.BattleMusic);
```

---

## 总结

### 优点
- ✅ 完全解耦（触发器、管理器、Boss、UI 互不依赖）
- ✅ 通用于所有 Boss（通过 IBoss 接口）
- ✅ 配置简单（直接拖拽引用）
- ✅ 易于扩展（添加新 Boss 只需实现接口）
- ✅ 支持多场景（Persistent + 房间场景）

### 需要的依赖
- Cinemachine（相机控制）
- DOTween（相机缩放动画）
- Behavior Designer（Boss 行为树）

### 下一步
1. 测试 Boss 战流程
2. 添加开场动画
3. 集成音乐系统
4. 添加更多 Boss
