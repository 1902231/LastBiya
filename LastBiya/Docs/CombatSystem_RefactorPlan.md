# 战斗系统重构方案

## 一、当前存在的问题

### 问题 1：普通攻击和蓄力攻击没有互斥

普通攻击是持续型 Ability（在 activeAbilities 列表中），蓄力攻击是 HFSM 状态 + 瞬发门卫 Ability（Activate 后立刻 isActive=false，不在 activeAbilities 中）。两者在不同系统里运行，AbilityManager 的优先级机制无法管理它们之间的关系。

### 问题 2：蓄力攻击的实现方式与其他攻击不一致

蓄力攻击走 HFSM 状态（PlayerState_ChargeAttack），普通攻击走 Ability（PlayerAbility_Attack）。同样是"攻击"，实现路径完全不同，导致：
- 互斥需要手动跨系统判断
- 以后加新攻击类型时不知道该走哪条路
- 动画控制权在两个系统之间跳来跳去

### 问题 3：AbilityManager 缺少 using UnityEditor 的打包问题

AbilityManager.cs 顶部有 `using UnityEditor.Playables;`，打包时会报错。

### 问题 4：HFSM.cs 有无用的 using

`using static UnityEngine.GraphicsBuffer;` 没有被使用。

### 问题 5：输入被消费但 Ability 激活失败时输入丢失

Jump/FreeFall/DoubleJump 状态中，`Consume(FallingDashAction) && TryActivate(FallingDash)` 的写法会导致 Consume 成功但 TryActivate 失败时输入被吞掉。

### 问题 6：AbilityMgr.Tick 和 HFSM.OnUpdate 的执行顺序

当前 PlayerController.Update 中先 Tick 再 OnUpdate。这意味着 Ability 先执行，HFSM 后执行。如果攻击 Ability 在 Tick 中 Deactivate 了，同一帧 HFSM 的 OnUpdate 里状态还在用旧的 Ability 状态做判断。建议统一为先 HFSM 再 Ability。

---

## 二、重构方案：统一攻击到 Ability 层

### 核心思路

把蓄力攻击从 HFSM 状态改为持续型 Ability，和普通攻击一样。所有攻击类型统一在 Ability 层管理，HFSM 只管移动。

### 蓄力攻击改造

将 PlayerState_ChargeAttack 的逻辑迁移到 PlayerAbility_ChargeAttack 中，从瞬发门卫改为持续型 Ability：

```
PlayerAbility_ChargeAttack (priority=2, 持续型)
  Activate()  → isActive=true, 开始蓄力, 冻结移动速度
  Tick()      → 蓄力计时, 检测松手取消, 蓄满后启用 ChargeHitbox, 释放结束后 Deactivate
  Deactivate() → 关闭 Hitbox, 恢复移动
  CanBeInterrupted() → 始终返回 false（只有受伤能通过事件中心强制打断）
```

### 攻击互斥的实现

统一后，AbilityManager 的优先级机制自动处理互斥：
- 普通攻击 priority=1，蓄力攻击 priority=2
- 蓄力攻击可以打断普通攻击后摇（priority 更高）
- 普通攻击不能打断蓄力攻击（priority 更低 + CanBeInterrupted=false）
- 同 priority 的攻击之间通过 !isActive 天然互斥

### 需要删除的内容

- `E_PlayerStateType.ChargeAttack` 枚举值
- `PlayerState_ChargeAttack.cs` 文件
- PlayerController.Start 中 ChargeAttack 状态的注册

### 需要修改的文件

| 文件 | 改动 |
|------|------|
| PlayerAbility_ChargeAttack.cs | 从瞬发门卫改为持续型，迁入蓄力/释放逻辑 |
| PlayerController.cs | 删除 ChargeAttack 状态注册，蓄力参数保留 |
| PlayerState_Grounded.cs | 蓄力输入检测改为 TryActivate（不再 SwitchState） |
| PlayerState_Alive.cs | 可选：蓄力输入也放到 Alive 层（如果以后空中也能蓄力） |
| AbilityManager.cs | 删除 `using UnityEditor.Playables;` |
| HFSM.cs | 删除 `using static UnityEngine.GraphicsBuffer;` |

### 蓄力期间的移动限制

蓄力攻击需要冻结移动，但它不再是 HFSM 状态。解决方式：在 HFSM 的移动状态（Move、Jump 等）的 OnFixedUpdate 里检查是否有高优先级 Ability 在执行：

```csharp
// Move.OnFixedUpdate()
if (owner.AbilityMgr.IsActive(E_PlayerAbilityType.ChargeAttack))
    return; // 蓄力期间不施加移动力
owner.ApplyHorizontalMovement(input.x);
```

或者在 PlayerController 上加一个通用方法：

```csharp
public bool IsMovementLocked => AbilityMgr.IsActive(E_PlayerAbilityType.ChargeAttack);
```

---

## 三、为未来连招系统做的准备

统一到 Ability 层后，连招系统只需要在 PlayerController 上加两个字段：

```csharp
public E_PlayerAbilityType LastAttackType;  // 上一段攻击类型
public bool IsInComboWindow;                 // 是否在连招窗口内
```

每个攻击 Ability 的 CanActivate 里检查这两个字段，决定能不能接招。加新攻击段只需要新建一个 Ability 文件，不需要改框架。

---

## 四、预期成果

重构完成后：
- 所有攻击类型在同一个系统（Ability）中管理，互斥关系由优先级自动处理
- 蓄力攻击和普通攻击不会同时执行
- 攻击不占用 HFSM 状态，玩家可以边移动边攻击（除蓄力外）
- 动画控制权清晰：HFSM 管移动动画，Ability 管攻击动画，后者覆盖前者
- 框架层（HFSM.cs、BaseAbility.cs、AbilityManager.cs）不需要修改
- 连招系统可以在此基础上自然扩展
