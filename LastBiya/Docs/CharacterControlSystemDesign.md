# 角色控制系统技术设计文档

## 1. 概述

本文档描述 LastBiya 项目的角色控制系统架构设计。该系统采用 **New Input System + HFSM + Ability System** 三层架构，目标是实现类似空洞骑士的角色操控手感，同时保持良好的可扩展性。

```
┌─────────────────────────────────────────────┐
│              New Input System                │
│         （事件源，输入采集与分发）              │
└──────────────────┬──────────────────────────┘
                   │ 输入事件
                   ▼
┌─────────────────────────────────────────────┐
│                  HFSM                        │
│      （互斥运动状态流转，基础物理行为）         │
└──────────────────┬──────────────────────────┘
                   │ 状态查询 / 状态切换
                   ▼
┌─────────────────────────────────────────────┐
│            Ability System                    │
│    （可叠加能力，打断优先级，参数化行为）        │
└─────────────────────────────────────────────┘
```

**职责划分原则：**

- Input System：只负责采集原始输入，不包含任何游戏逻辑
- HFSM：只负责互斥的运动状态（不能同时跑和飞），不负责能力判断
- Ability System：只负责可叠加的动作能力，通过优先级管理打断关系

---

## 2. Input System 设计

### 2.1 Action Maps

系统定义三个 Action Map，任意时刻只有一个处于激活状态，保证输入上下文互斥。

#### Gameplay（游戏中）

| Action    | 类型           | 键盘绑定   | 手柄绑定       | 说明                                   |
| --------- | -------------- | ---------- | -------------- | -------------------------------------- |
| Move      | Value(Vector2) | WASD       | 左摇杆         | 水平移动 + 垂直方向（上看/下砸判定）    |
| Jump      | Button         | Space      | 南键(A/×)      | 支持长按/短按区分跳跃高度               |
| Attack    | Button         | J          | 西键(X/□)      | 配合 Move 垂直分量判定攻击方向          |
| Dash      | Button         | L          | 右肩键(RB/R1)  | 冲刺                                   |
| Skill     | Button         | K          | 北键(Y/△)      | 法术/技能释放                           |
| QuickCast | Button         | I          | 左肩键(LB/L1)  | 快速施法                               |
| Interact  | Button         | E          | 东键(B/○)      | NPC/物品交互                           |
| Heal      | Button         | A          | 左触发(LT/L2)  | 长按触发治疗                           |

#### UI（菜单/地图）

| Action        | 类型           | 键盘绑定 | 手柄绑定 | 说明       |
| ------------- | -------------- | -------- | -------- | ---------- |
| Navigate      | Value(Vector2) | 方向键   | 左摇杆   | 菜单导航   |
| Confirm       | Button         | Space    | 南键     | 确认       |
| Cancel        | Button         | Esc      | 东键     | 取消/返回  |
| OpenMap       | Button         | Tab      | Select   | 打开地图   |
| OpenInventory | Button         | I        | Start    | 打开道具栏 |
| TabLeft       | Button         | Q        | LB       | 标签页左切 |
| TabRight      | Button         | E        | RB       | 标签页右切 |

#### Dialogue（对话）

| Action  | 类型   | 键盘绑定 | 手柄绑定 | 说明     |
| ------- | ------ | -------- | -------- | -------- |
| Advance | Button | Space    | 南键     | 推进对话 |
| Skip    | Button | Esc      | 东键     | 跳过对话 |

### 2.2 Action Map 切换策略

```csharp
// 通过 Enable/Disable 保证互斥
public void SwitchToUI() {
    inputActions.Gameplay.Disable();
    inputActions.Dialogue.Disable();
    inputActions.UI.Enable();
}
```

### 2.3 输入缓冲（Input Buffering）

为提升手感，关键操作（跳跃、攻击、冲刺）需要输入缓冲。在动作尚未结束时提前按下的输入会被缓存，动作结束后立即执行。

```csharp
public class InputBuffer {
    private float bufferDuration = 0.15f; // 缓冲窗口 150ms
    private float bufferTimer;
    private InputAction bufferedAction;

    public void Buffer(InputAction action) {
        bufferedAction = action;
        bufferTimer = bufferDuration;
    }

    public void Tick(float deltaTime) {
        if (bufferTimer > 0) {
            bufferTimer -= deltaTime;
            if (bufferTimer <= 0)
                bufferedAction = null; // 过期清除
        }
    }

    public InputAction Consume() {
        var action = bufferedAction;
        bufferedAction = null;
        return action;
    }
}
```

---

## 3. HFSM 设计

### 3.1 核心数据结构

```csharp
public abstract class HState {
    public HState parent;
    protected HFSM stateMachine;

    public virtual void OnEnter() { }
    public virtual void OnExit() { }
    public virtual void OnUpdate() { }
    public virtual void OnFixedUpdate() { }
}
```

### 3.2 状态切换算法

HFSM 的核心是基于最近公共祖先（LCA）的状态切换。切换时只退出和进入必要的层级，父状态的 OnEnter/OnExit 仅在跨层切换时触发。

```csharp
public class HFSM {
    private HState currentState;

    public void ChangeState(HState target) {
        HState ancestor = FindCommonAncestor(currentState, target);

        // 退出阶段：从当前状态往上退到公共祖先（不包含祖先）
        HState s = currentState;
        while (s != ancestor) {
            s.OnExit();
            s = s.parent;
        }

        // 进入阶段：从公共祖先往下走到目标状态
        List<HState> enterPath = new List<HState>();
        s = target;
        while (s != ancestor) {
            enterPath.Add(s);
            s = s.parent;
        }
        enterPath.Reverse();

        foreach (HState state in enterPath) {
            state.OnEnter();
        }

        currentState = target;
    }

    private HState FindCommonAncestor(HState a, HState b) {
        HashSet<HState> ancestorsOfA = new HashSet<HState>();
        HState current = a;
        while (current != null) {
            ancestorsOfA.Add(current);
            current = current.parent;
        }

        current = b;
        while (current != null) {
            if (ancestorsOfA.Contains(current))
                return current;
            current = current.parent;
        }

        return null;
    }

    public void Update() {
        // 从根到当前叶子，依次执行每一层的 OnUpdate
        List<HState> chain = GetAncestorChain(currentState);
        foreach (HState state in chain) {
            state.OnUpdate();
        }
    }

    private List<HState> GetAncestorChain(HState state) {
        List<HState> chain = new List<HState>();
        HState s = state;
        while (s != null) {
            chain.Add(s);
            s = s.parent;
        }
        chain.Reverse(); // 根在前，叶子在后
        return chain;
    }
}
```

### 3.3 状态树结构

```
Root
├── Alive
│   ├── Grounded（地面）
│   │   ├── Idle        — 静止站立
│   │   ├── Run         — 水平移动
│   │   └── Land        — 落地硬直（可选，用于落地动画过渡）
│   └── Airborne（空中）
│       ├── Rise        — 上升阶段
│       ├── Fall        — 下落阶段
│       └── WallSlide   — 贴墙滑行
├── Hurt                — 受伤硬直
└── Dead                — 死亡
```

### 3.4 各层级职责

| 层级     | OnEnter 职责                   | OnExit 职责                  | OnUpdate 职责          |
| -------- | ------------------------------ | ---------------------------- | ---------------------- |
| Alive    | —                              | —                            | 通用计时器更新         |
| Grounded | 重置空中跳跃次数、重置冲刺     | 记录离地时间（土狼时间起点） | 地面检测               |
| Airborne | 启用重力                       | —                            | 土狼时间倒计时         |
| Idle     | 播放站立动画                   | —                            | 监听移动输入切换到 Run |
| Run      | 播放跑步动画                   | —                            | 应用水平速度           |
| Rise     | —                              | —                            | 检测速度转负切换 Fall  |
| Fall     | —                              | —                            | 限制最大下落速度       |
| WallSlide| 播放贴墙动画，降低下落速度     | —                            | 检测脱离墙壁           |
| Hurt     | 播放受伤动画，施加击退力       | 恢复控制权                   | 硬直计时               |
| Dead     | 播放死亡动画，禁用输入         | —                            | —                      |

### 3.5 土狼时间（Coyote Time）

角色离开平台边缘后仍有短暂窗口可以跳跃，提升手感。

```csharp
public class GroundedState : HState {
    public override void OnExit() {
        // 离地时开始计时
        stateMachine.Player.coyoteTimer = 0.1f; // 100ms 窗口
    }
}

public class AirborneState : HState {
    public override void OnUpdate() {
        if (stateMachine.Player.coyoteTimer > 0)
            stateMachine.Player.coyoteTimer -= Time.deltaTime;
    }
}

// 跳跃判定时
bool canJump = stateMachine.IsInState<GroundedState>()
            || stateMachine.Player.coyoteTimer > 0;
```

---

## 4. Ability System 设计

### 4.1 核心基类

```csharp
public abstract class Ability {
    public int priority;
    public bool isUnlocked = false;
    public bool isActive = false;

    protected PlayerController player;

    /// <summary>
    /// 当前条件下能否激活（子类重写添加具体条件）
    /// </summary>
    public virtual bool CanActivate() {
        return isUnlocked && !isActive;
    }

    /// <summary>
    /// 当前是否允许被更高优先级打断（子类重写实现条件性打断）
    /// </summary>
    public virtual bool CanBeInterrupted() {
        return true;
    }

    public virtual void Activate() { isActive = true; }
    public virtual void Deactivate() { isActive = false; }
    public virtual void Tick(float deltaTime) { }
}
```

### 4.2 优先级分层

```
优先级 3（最高）：HurtAbility, DeathAbility
    → 几乎不可被打断，可打断一切

优先级 2：DashAbility, SpellAbility
    → 可被优先级 3 打断
    → 可打断优先级 0-1

优先级 1：AttackAbility, HealAbility
    → 可被优先级 2-3 打断
    → 可打断优先级 0

优先级 0（最低）：DoubleJumpAbility, WallJumpAbility
    → 可被任何高层打断
```

### 4.3 AbilityManager

```csharp
public class AbilityManager {
    private List<Ability> activeAbilities = new List<Ability>();
    private Dictionary<System.Type, Ability> abilityMap = new Dictionary<System.Type, Ability>();

    public void Register(Ability ability) {
        abilityMap[ability.GetType()] = ability;
    }

    public T Get<T>() where T : Ability {
        return abilityMap[typeof(T)] as T;
    }

    public bool IsActive<T>() where T : Ability {
        return abilityMap.TryGetValue(typeof(T), out var a) && a.isActive;
    }

    /// <summary>
    /// 尝试激活一个 Ability，自动处理优先级打断逻辑
    /// </summary>
    public bool TryActivate(Ability ability) {
        if (!ability.CanActivate()) return false;

        // 检查是否被更高优先级压制
        foreach (var active in activeAbilities) {
            if (active.priority >= ability.priority && !active.CanBeInterrupted())
                return false;
        }

        // 打断所有优先级更低的
        for (int i = activeAbilities.Count - 1; i >= 0; i--) {
            if (activeAbilities[i].priority < ability.priority) {
                activeAbilities[i].Deactivate();
                activeAbilities.RemoveAt(i);
            }
        }

        ability.Activate();
        activeAbilities.Add(ability);
        return true;
    }

    public void Tick(float deltaTime) {
        // 倒序遍历，因为 Tick 中可能触发 Deactivate
        for (int i = activeAbilities.Count - 1; i >= 0; i--) {
            activeAbilities[i].Tick(deltaTime);
            if (!activeAbilities[i].isActive)
                activeAbilities.RemoveAt(i);
        }
    }
}
```

### 4.4 具体 Ability 示例

#### DashAbility（冲刺）

```csharp
public class DashAbility : Ability {
    public float distance = 7f;
    public float duration = 0.2f;
    public float cooldown = 0.6f;
    public float invincibleDuration = 0.1f;

    private float elapsed;
    private float cooldownTimer;
    private int direction;

    public DashAbility() { priority = 2; }

    public override bool CanActivate() {
        return base.CanActivate() && cooldownTimer <= 0;
    }

    public override bool CanBeInterrupted() {
        // 无敌帧期间不可打断
        return elapsed > invincibleDuration;
    }

    public override void Activate() {
        base.Activate();
        elapsed = 0f;
        direction = player.facingDirection;
        player.SetInvincible(true);
        player.velocity = new Vector2(direction * (distance / duration), 0);
        player.StateMachine.ChangeState(player.DashState); // 通知 HFSM
    }

    public override void Tick(float deltaTime) {
        elapsed += deltaTime;

        if (elapsed > invincibleDuration)
            player.SetInvincible(false);

        if (elapsed >= duration)
            Deactivate();
    }

    public override void Deactivate() {
        base.Deactivate();
        cooldownTimer = cooldown;
        player.SetInvincible(false);
    }
}
```

#### AttackAbility（攻击）

```csharp
public class AttackAbility : Ability {
    public float activeDuration = 0.15f;   // 攻击判定持续时间
    public float recoveryDuration = 0.2f;  // 后摇时间
    public float attackRange = 1.5f;

    private float elapsed;
    private bool isInRecovery;
    private AttackDirection attackDir;

    public AttackAbility() { priority = 1; }

    public override bool CanBeInterrupted() {
        // 只有后摇阶段可以被取消（用于冲刺取消、跳跃取消等）
        return isInRecovery;
    }

    public override void Activate() {
        base.Activate();
        elapsed = 0f;
        isInRecovery = false;

        // 根据输入方向决定攻击方向
        float vertical = player.MoveInput.y;
        if (vertical > 0.5f)
            attackDir = AttackDirection.Up;
        else if (vertical < -0.5f && !player.IsGrounded)
            attackDir = AttackDirection.Down; // 空中下砸
        else
            attackDir = AttackDirection.Forward;
    }

    public override void Tick(float deltaTime) {
        elapsed += deltaTime;

        if (!isInRecovery && elapsed >= activeDuration) {
            isInRecovery = true;
            // 关闭攻击判定碰撞体
        }

        if (elapsed >= activeDuration + recoveryDuration)
            Deactivate();
    }
}
```

### 4.5 打断关系矩阵

下表描述各 Ability 之间的打断关系（行打断列）：

|            | Hurt | Dash | Spell | Attack | Heal | DoubleJump |
| ---------- | ---- | ---- | ----- | ------ | ---- | ---------- |
| Hurt(3)    | —    | ✓*   | ✓     | ✓      | ✓    | ✓          |
| Dash(2)    | ✗    | —    | —     | ✓**    | ✓    | ✓          |
| Spell(2)   | ✗    | —    | —     | ✓**    | ✓    | ✓          |
| Attack(1)  | ✗    | ✗    | ✗     | —      | —    | ✓          |
| Heal(1)    | ✗    | ✗    | ✗     | —      | —    | ✓          |

- `✓*` Dash 无敌帧期间不可被 Hurt 打断（CanBeInterrupted 返回 false）
- `✓**` Attack 仅在后摇阶段可被打断

---

## 5. 三层通信机制

### 5.1 数据流方向

```
Input System ──(事件)──→ PlayerController ──→ HFSM.ChangeState()
                                          ──→ AbilityManager.TryActivate()

HFSM ←──(状态查询)── AbilityManager
     ──(强制切换)──→ HFSM.ChangeState()

AbilityManager ←──(能力查询)── HFSM
```

### 5.2 PlayerController 作为中枢

```csharp
public class PlayerController : MonoBehaviour {
    public HFSM StateMachine { get; private set; }
    public AbilityManager Abilities { get; private set; }
    public InputBuffer InputBuffer { get; private set; }

    // Input System 回调
    private void OnJumpInput(InputAction.CallbackContext ctx) {
        if (ctx.started) {
            // 先尝试 Ability（二段跳）
            if (Abilities.TryActivate(Abilities.Get<DoubleJumpAbility>()))
                return;

            // 再走 HFSM 常规跳跃
            if (CanNormalJump())
                StateMachine.ChangeState(riseState);
            else
                InputBuffer.Buffer(jumpAction); // 缓冲
        }

        if (ctx.canceled) {
            // 松开跳跃键，截断上升速度（可变跳跃高度）
            if (velocity.y > 0)
                velocity.y *= 0.5f;
        }
    }

    private void OnDashInput(InputAction.CallbackContext ctx) {
        if (ctx.started) {
            Abilities.TryActivate(Abilities.Get<DashAbility>());
        }
    }

    private void OnAttackInput(InputAction.CallbackContext ctx) {
        if (ctx.started) {
            if (!Abilities.TryActivate(Abilities.Get<AttackAbility>()))
                InputBuffer.Buffer(attackAction);
        }
    }
}
```

---

## 6. 能力解锁与参数修改

### 6.1 能力解锁

```csharp
// 获得二段跳能力
player.Abilities.Get<DoubleJumpAbility>().isUnlocked = true;

// 获得壁跳能力
player.Abilities.Get<WallJumpAbility>().isUnlocked = true;
```

解锁不需要修改 HFSM 的任何转换规则，因为能力判断在 Ability 的 `CanActivate()` 中。

### 6.2 护符/装备系统的参数修改

```csharp
public interface ICharm {
    void Apply(AbilityManager abilities);
    void Remove(AbilityManager abilities);
}

public class LongDashCharm : ICharm {
    public void Apply(AbilityManager abilities) {
        abilities.Get<DashAbility>().distance += 3f;
    }

    public void Remove(AbilityManager abilities) {
        abilities.Get<DashAbility>().distance -= 3f;
    }
}

public class QuickHealCharm : ICharm {
    public void Apply(AbilityManager abilities) {
        abilities.Get<HealAbility>().castDuration *= 0.6f;
    }

    public void Remove(AbilityManager abilities) {
        abilities.Get<HealAbility>().castDuration /= 0.6f;
    }
}
```

---

## 7. 推荐目录结构

```
Assets/
└── Scripts/
    └── Player/
        ├── PlayerController.cs          // 中枢，持有 HFSM 和 AbilityManager
        ├── Input/
        │   ├── PlayerInputActions.inputactions  // Input System 资产文件
        │   └── InputBuffer.cs
        ├── StateMachine/
        │   ├── HFSM.cs                  // 状态机核心（ChangeState、LCA算法）
        │   ├── HState.cs                // 状态基类
        │   ├── AliveState.cs
        │   ├── GroundedState.cs
        │   ├── AirborneState.cs
        │   ├── IdleState.cs
        │   ├── RunState.cs
        │   ├── RiseState.cs
        │   ├── FallState.cs
        │   ├── WallSlideState.cs
        │   ├── HurtState.cs
        │   └── DeadState.cs
        ├── Abilities/
        │   ├── Ability.cs               // 能力基类
        │   ├── AbilityManager.cs
        │   ├── DashAbility.cs
        │   ├── AttackAbility.cs
        │   ├── DoubleJumpAbility.cs
        │   ├── WallJumpAbility.cs
        │   ├── SpellAbility.cs
        │   └── HealAbility.cs
        └── Charms/
            ├── ICharm.cs
            ├── LongDashCharm.cs
            └── QuickHealCharm.cs
```

---

## 8. 设计决策总结

| 决策点               | 选择                     | 理由                                                     |
| -------------------- | ------------------------ | -------------------------------------------------------- |
| 输入系统             | New Input System         | Action Map 支持上下文切换，原生手柄支持，事件驱动         |
| 运动状态管理         | HFSM                     | 层级共享转换逻辑，减少重复代码，父状态管共性行为          |
| 能力/动作管理        | Ability System           | 解决 HFSM 并行行为和能力解锁膨胀问题，可叠加、可插拔     |
| 打断机制             | 优先级 + CanBeInterrupted | 优先级管结构性规则，CanBeInterrupted 管时序性细节         |
| 参数修改（护符）     | ICharm 接口直接修改 Ability 字段 | 简单直接，每个护符只关心自己影响的 Ability          |
| 输入缓冲             | 独立 InputBuffer 类      | 与状态机和能力系统解耦，统一管理缓冲窗口                  |
