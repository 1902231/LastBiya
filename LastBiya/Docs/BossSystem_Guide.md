# Boss 系统使用指南

## 概述

Boss 系统采用 **HFSM（分层状态机）+ Behavior Designer（行为树）** 的协作架构：
- **HFSM**：负责具体动作执行（攻击、移动、动画等）
- **Behavior Designer**：负责高层决策（状态切换、技能选择、战术判断）

## 文件结构

```
Assets/Scripts/Boss/
├── BossController.cs           # Boss 主控制器
├── E_BossStateType.cs          # 状态枚举
├── Boss_AliveState.cs          # Alive 父状态
├── Boss_IdleState.cs           # 待机状态
├── Boss_MoveState.cs           # 移动状态
├── Boss_SlashAttackState.cs    # 前劈攻击状态
├── Boss_FlyUpState.cs          # 飞起状态
├── Boss_SlamDownState.cs       # 下砸状态
├── Boss_RangedAttackState.cs   # 远程攻击状态
├── Boss_StunnedState.cs        # 破防倒地状态
├── Boss_DeadState.cs           # 死亡状态
└── BossProjectile.cs           # 远程攻击弹射物
```

## 状态说明

### 1. Alive 状态（父状态）
- 所有非死亡状态的父状态
- 默认子状态：Idle
- 负责死亡检测

### 2. Idle 状态
- 待机，类似玩家 Idle
- 保持静止，朝向玩家

### 3. Move 状态
- 向玩家方向移动
- 自动朝向玩家

### 4. SlashAttack 状态（前劈攻击）
- **前摇阶段**：准备攻击（可配置时长）
- **判定阶段**：激活 Hitbox，造成伤害
- **后摇阶段**：攻击后的硬直时间

### 5. FlyUp 状态
- 飞到玩家头顶（可配置偏移量）
- 进入状态时锁定目标位置
- 关闭重力，退出时恢复

### 6. SlamDown 状态
- 向下冲刺并造成伤害
- 落地后有硬直时间
- 可添加震屏、特效等

### 7. RangedAttack 状态
- 发射远程弹射物
- **前摇阶段**：准备发射
- **后摇阶段**：发射后的硬直

### 8. Stunned 状态（破防倒地）
- Boss 无法行动
- 持续固定时间后结束

### 9. Dead 状态
- 死亡状态
- 禁用碰撞，停止移动

## BossController 公共接口

### 供 Behavior Designer 调用的方法

```csharp
// 状态切换
void SwitchToState(E_BossStateType stateType)

// 状态查询
E_BossStateType GetCurrentState()
float GetHPPercentage()              // 返回 0-1
float GetDistanceToPlayer()
bool IsPlayerInFront()

// 朝向控制
void FacePlayer()
void UpdateFacing(int dir)
```

## 在 Unity 中设置 Boss

### 1. 创建 Boss GameObject
1. 创建空物体，命名为 "Boss"
2. 添加组件：
   - `Rigidbody2D`（Dynamic，Freeze Rotation Z）
   - `Collider2D`（BoxCollider2D 或 CapsuleCollider2D）
   - `BossController` 脚本

### 2. 配置 BossController 参数

#### 生命值
- `Max HP`：最大生命值（默认 500）

#### 移动
- `Move Speed`：移动速度（默认 3）
- `Move Acceleration`：加速度（默认 30）

#### 前劈攻击
- `Slash Windup Duration`：前摇时间（默认 0.8s）
- `Slash Active Duration`：判定时间（默认 0.3s）
- `Slash Recovery Duration`：后摇时间（默认 0.5s）
- `Slash Damage`：伤害（默认 20）

#### 飞起
- `Fly Up Speed`：飞行速度（默认 8）
- `Fly Up Offset Y`：垂直偏移（默认 5）
- `Fly Up Offset X`：水平偏移（默认 0）
- `Fly Up Arrive Distance`：到达判定距离（默认 0.5）

#### 下砸
- `Slam Down Speed`：下冲速度（默认 20）
- `Slam Down Damage`：伤害（默认 30）
- `Slam Down Recovery Duration`：落地硬直（默认 0.8s）

#### 远程攻击
- `Ranged Windup Duration`：前摇时间（默认 0.6s）
- `Ranged Recovery Duration`：后摇时间（默认 0.4s）
- `Ranged Projectile Prefab`：弹射物预制体
- `Projectile Speed`：弹射物速度（默认 10）
- `Projectile Max Distance`：最大飞行距离（默认 15）
- `Projectile Damage`：伤害（默认 15）

#### 破防倒地
- `Stunned Duration`：持续时间（默认 3s）

#### 地面检测
- `Ground Check Offset`：检测点偏移（默认 (0, -1)）
- `Ground Check Radius`：检测半径（默认 0.3）
- `Ground Layer`：地面层级

### 3. 创建攻击判定框（可选）

为前劈攻击和下砸攻击创建 Hitbox：

1. 在 Boss 下创建子物体 "SlashHitbox"
2. 添加 `AttackHitbox` 组件
3. 添加 `Collider2D`（设置为 Trigger）
4. 默认禁用该物体（状态会自动激活/禁用）

### 4. 创建远程攻击弹射物预制体

1. 创建空物体 "BossProjectile"
2. 添加组件：
   - `Rigidbody2D`（Kinematic 或 Dynamic）
   - `Collider2D`（Trigger）
   - `AttackHitbox` 组件
   - `BossProjectile` 脚本
3. 保存为预制体
4. 将预制体拖到 BossController 的 `Ranged Projectile Prefab` 字段

## 使用 Behavior Designer 控制 Boss

### 创建自定义 Actions

#### 1. 切换状态 Action

```csharp
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;

[TaskCategory("Boss")]
public class SwitchBossState : Action
{
    public SharedBossController boss;
    public E_BossStateType targetState;
    
    public override TaskStatus OnUpdate()
    {
        if (boss.Value == null) return TaskStatus.Failure;
        boss.Value.SwitchToState(targetState);
        return TaskStatus.Success;
    }
}
```

#### 2. 检查血量 Conditional

```csharp
[TaskCategory("Boss")]
public class CheckBossHP : Conditional
{
    public SharedBossController boss;
    public float minHP = 0f;
    public float maxHP = 1f;
    
    public override TaskStatus OnUpdate()
    {
        if (boss.Value == null) return TaskStatus.Failure;
        float hpPercent = boss.Value.GetHPPercentage();
        return (hpPercent >= minHP && hpPercent <= maxHP) 
            ? TaskStatus.Success 
            : TaskStatus.Failure;
    }
}
```

#### 3. 检查距离 Conditional

```csharp
[TaskCategory("Boss")]
public class CheckDistanceToPlayer : Conditional
{
    public SharedBossController boss;
    public float minDistance = 0f;
    public float maxDistance = 10f;
    
    public override TaskStatus OnUpdate()
    {
        if (boss.Value == null) return TaskStatus.Failure;
        float distance = boss.Value.GetDistanceToPlayer();
        return (distance >= minDistance && distance <= maxDistance)
            ? TaskStatus.Success
            : TaskStatus.Failure;
    }
}
```

#### 4. 等待状态完成 Action

```csharp
[TaskCategory("Boss")]
public class WaitForStateComplete : Action
{
    public SharedBossController boss;
    public E_BossStateType stateToWait;
    
    public override TaskStatus OnUpdate()
    {
        if (boss.Value == null) return TaskStatus.Failure;
        
        // 检查当前状态是否还是目标状态
        if (boss.Value.GetCurrentState() != stateToWait)
            return TaskStatus.Success;
        
        return TaskStatus.Running;
    }
}
```

### 示例行为树结构

```
Root (Selector)
├─ Sequence [死亡检测]
│  ├─ CheckBossHP (HP <= 0)
│  └─ SwitchBossState (Dead)
│
├─ Sequence [阶段 1：100%-66% HP]
│  ├─ CheckBossHP (0.66 - 1.0)
│  └─ Selector [技能选择]
│     ├─ Sequence [近战攻击]
│     │  ├─ CheckDistanceToPlayer (0 - 3)
│     │  ├─ SwitchBossState (SlashAttack)
│     │  └─ WaitForStateComplete (SlashAttack)
│     │
│     ├─ Sequence [接近玩家]
│     │  ├─ CheckDistanceToPlayer (3 - 10)
│     │  └─ SwitchBossState (Move)
│     │
│     └─ SwitchBossState (Idle)
│
├─ Sequence [阶段 2：33%-66% HP]
│  ├─ CheckBossHP (0.33 - 0.66)
│  └─ Selector [新技能解锁]
│     ├─ Sequence [远程攻击]
│     │  ├─ CheckDistanceToPlayer (5 - 15)
│     │  ├─ SwitchBossState (RangedAttack)
│     │  └─ WaitForStateComplete (RangedAttack)
│     │
│     └─ ...
│
└─ Sequence [阶段 3：0%-33% HP]
   ├─ CheckBossHP (0 - 0.33)
   └─ Selector [狂暴模式]
      ├─ Sequence [飞起 + 下砸连招]
      │  ├─ SwitchBossState (FlyUp)
      │  ├─ Wait (1s)
      │  ├─ SwitchBossState (SlamDown)
      │  └─ WaitForStateComplete (SlamDown)
      │
      └─ ...
```

## 扩展建议

### 1. 添加新状态
1. 在 `E_BossStateType.cs` 添加枚举值
2. 创建新的状态类继承 `HFSM_BaseState<E_BossStateType, BossController>`
3. 在 `BossController.Start()` 中注册状态
4. 在行为树中使用新状态

### 2. 添加动画
在各状态的 `OnEnter()` 中播放动画：
```csharp
public override void OnEnter()
{
    var animator = owner.GetComponent<Animator>();
    if (animator != null)
        animator.Play("SlashAttack");
}
```

### 3. 添加音效和特效
在关键时机调用：
```csharp
// 攻击判定激活时
AudioManager.Instance.PlaySFX("BossSlash");
EffectManager.Instance.SpawnEffect("SlashEffect", position);
```

### 4. 添加受击反馈
在 `BossController.TakeDamage()` 中添加：
```csharp
public void TakeDamage(DamageInfo info)
{
    currentHP -= info.damage;
    LastDamageInfo = info;
    
    // 受击特效
    PlayHitEffect();
    
    // 可选：由行为树决定是否打断当前行为
    if (currentHP <= 0)
    {
        StateMachine.SwitchState(E_BossStateType.Dead);
        currentStateType = E_BossStateType.Dead;
    }
}
```

## 调试技巧

1. **查看当前状态**：在 Inspector 中查看 `currentStateType`
2. **Gizmos 可视化**：已实现地面检测和飞起目标位置的可视化
3. **日志输出**：在关键状态切换时添加 `Debug.Log()`
4. **行为树调试**：使用 Behavior Designer 的可视化调试功能

## 注意事项

1. **状态切换由行为树控制**：状态内部不主动切换到其他状态
2. **玩家引用**：确保场景中有 Tag 为 "Player" 的对象
3. **Layer 设置**：确保地面 Layer 正确设置
4. **Hitbox 管理**：攻击状态会自动激活/禁用 Hitbox
5. **弹射物回收**：弹射物返回 Boss 后会自动销毁

## 性能优化

1. 使用对象池管理弹射物
2. 限制同时存在的弹射物数量
3. 使用事件系统代替频繁的 GetComponent
4. 缓存常用组件引用
