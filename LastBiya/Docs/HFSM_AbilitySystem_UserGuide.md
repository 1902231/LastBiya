# HFSM + Ability System 使用说明文档

---

## 一、框架概述

本框架包含两个独立的泛型系统，可以单独使用，也可以配合使用：

- HFSM（分层有限状态机）：管理互斥的状态（角色不能同时跑和跳）
- Ability System（能力系统）：管理可叠加的能力（角色可以边跑边攻击）

### 框架文件清单

| 文件 | 说明 |
|------|------|
| HFSM.cs | 状态机本体，管理状态注册、切换、更新 |
| HFSM_BaseState.cs | 状态基类，所有具体状态继承它 |
| BaseAbility.cs | 能力基类，所有具体能力继承它 |
| AbilityManager.cs | 能力管理器，管理能力注册、激活、打断、更新 |

这四个文件是纯框架代码，不包含任何业务逻辑，复制到任何 Unity 项目即可使用。

### 迁移注意事项

- 确保框架文件中没有多余的 using 引用（如 `UnityEditor.*`、`UnityEngine.InputSystem` 等）
- 建议将框架文件放在独立目录（如 `Scripts/Framework/`），与业务代码分离

---

## 二、HFSM 使用指南

### 第一步：定义状态枚举

为你的角色定义所有可能的状态标识。不需要 None 值，根节点的 parentType 默认为 null。

```csharp
public enum E_EnemyStateType
{
    // 父状态
    Alive,
    Combat,
    
    // 叶子状态
    Idle,
    Patrol,
    Chase,
    Attack,
    Hurt,
    Dead,
}
```

### 第二步：创建具体状态类

每个状态继承 `HFSM_BaseState<你的枚举, 你的控制器>`。

```csharp
// 父状态：Alive（管所有活着时的共性逻辑）
public class EnemyState_Alive : HFSM_BaseState<E_EnemyStateType, EnemyController>
{
    public EnemyState_Alive()
    {
        // 根节点，不设置 parentType（默认 null）
    }

    public override void OnUpdate()
    {
        // 所有活着的子状态都会执行这个检测
        if (owner.CurrentHP <= 0)
            hfsm.SwitchState(E_EnemyStateType.Dead);
    }
}

// 父状态：Combat（管战斗相关的共性逻辑）
public class EnemyState_Combat : HFSM_BaseState<E_EnemyStateType, EnemyController>
{
    public EnemyState_Combat()
    {
        parentType = E_EnemyStateType.Alive;
    }

    public override void OnEnter()
    {
        owner.AlertNearbyEnemies(); // 进入战斗时通知附近敌人
    }

    public override void OnExit()
    {
        owner.ResetAggro(); // 脱离战斗时重置仇恨
    }
}

// 叶子状态：Patrol（巡逻）
public class EnemyState_Patrol : HFSM_BaseState<E_EnemyStateType, EnemyController>
{
    public EnemyState_Patrol()
    {
        parentType = E_EnemyStateType.Alive;
    }

    public override void OnEnter()
    {
        owner.PlayAnimation("Patrol");
    }

    public override void OnUpdate()
    {
        owner.MoveAlongPath();

        if (owner.DetectPlayer())
            hfsm.SwitchState(E_EnemyStateType.Chase);
    }
}

// 叶子状态：Chase（追击，属于 Combat）
public class EnemyState_Chase : HFSM_BaseState<E_EnemyStateType, EnemyController>
{
    public EnemyState_Chase()
    {
        parentType = E_EnemyStateType.Combat;
    }

    public override void OnEnter()
    {
        owner.PlayAnimation("Run");
    }

    public override void OnUpdate()
    {
        owner.MoveTowardPlayer();

        if (owner.IsInAttackRange())
            hfsm.SwitchState(E_EnemyStateType.Attack);

        if (!owner.DetectPlayer())
            hfsm.SwitchState(E_EnemyStateType.Patrol);
    }
}

// 叶子状态：Attack（攻击，属于 Combat）
public class EnemyState_Attack : HFSM_BaseState<E_EnemyStateType, EnemyController>
{
    private float attackTimer;

    public EnemyState_Attack()
    {
        parentType = E_EnemyStateType.Combat;
    }

    public override void OnEnter()
    {
        attackTimer = 0f;
        owner.PlayAnimation("Attack");
    }

    public override void OnUpdate()
    {
        attackTimer += Time.deltaTime;
        if (attackTimer >= owner.AttackDuration)
            hfsm.SwitchState(E_EnemyStateType.Chase);
    }
}

// Hurt（受伤，直接挂在 Alive 下）
public class EnemyState_Hurt : HFSM_BaseState<E_EnemyStateType, EnemyController>
{
    private float hurtTimer;

    public EnemyState_Hurt()
    {
        parentType = E_EnemyStateType.Alive;
    }

    public override void OnEnter()
    {
        hurtTimer = 0f;
        owner.PlayAnimation("Hurt");
    }

    public override void OnUpdate()
    {
        hurtTimer += Time.deltaTime;
        if (hurtTimer >= 0.5f)
            hfsm.SwitchState(E_EnemyStateType.Patrol);
    }
}

// Dead（死亡，根级别）
public class EnemyState_Dead : HFSM_BaseState<E_EnemyStateType, EnemyController>
{
    public override void OnEnter()
    {
        owner.PlayAnimation("Dead");
        owner.DisableCollider();
    }
}
```

### 第三步：在控制器中创建和驱动状态机

```csharp
public class EnemyController : MonoBehaviour
{
    public HFSM<E_EnemyStateType, EnemyController> StateMachine { get; private set; }

    public float CurrentHP;
    public float AttackDuration = 1f;

    void Start()
    {
        // 创建状态机，传入自身作为 owner
        StateMachine = new HFSM<E_EnemyStateType, EnemyController>(this);

        // 注册所有状态（顺序无所谓，parentType 在状态构造函数里已声明）
        StateMachine.AddState(E_EnemyStateType.Alive,   new EnemyState_Alive());
        StateMachine.AddState(E_EnemyStateType.Combat,  new EnemyState_Combat());
        StateMachine.AddState(E_EnemyStateType.Idle,    new EnemyState_Idle());
        StateMachine.AddState(E_EnemyStateType.Patrol,  new EnemyState_Patrol());
        StateMachine.AddState(E_EnemyStateType.Chase,   new EnemyState_Chase());
        StateMachine.AddState(E_EnemyStateType.Attack,  new EnemyState_Attack());
        StateMachine.AddState(E_EnemyStateType.Hurt,    new EnemyState_Hurt());
        StateMachine.AddState(E_EnemyStateType.Dead,    new EnemyState_Dead());

        // 设置初始状态
        StateMachine.SwitchState(E_EnemyStateType.Patrol);
    }

    void Update()
    {
        StateMachine.OnUpdate();
    }

    // 以下是业务方法，供状态内部通过 owner 调用
    public void PlayAnimation(string name) { /* ... */ }
    public void MoveAlongPath() { /* ... */ }
    public void MoveTowardPlayer() { /* ... */ }
    public bool DetectPlayer() { return false; }
    public bool IsInAttackRange() { return false; }
    public void AlertNearbyEnemies() { /* ... */ }
    public void ResetAggro() { /* ... */ }
    public void DisableCollider() { /* ... */ }
}
```

### 状态树结构与切换行为

上面的代码构建了这样一棵树：

```
Alive
├── Combat
│   ├── Chase
│   └── Attack
├── Patrol
└── Hurt
Dead
```

切换行为示例：

```
Patrol → Chase：
  退出：Patrol.OnExit()
  进入：Combat.OnEnter() → Chase.OnEnter()
  （Alive 没动，因为 Patrol 和 Combat 都在 Alive 下）

Chase → Attack：
  退出：Chase.OnExit()
  进入：Attack.OnEnter()
  （Combat 没动，因为两者都在 Combat 下）

Attack → Patrol：
  退出：Attack.OnExit() → Combat.OnExit()
  进入：Patrol.OnEnter()
  （退出了 Combat 层，因为 Patrol 不在 Combat 下）

Chase → Dead：
  退出：Chase.OnExit() → Combat.OnExit() → Alive.OnExit()
  进入：Dead.OnEnter()
  （退出了整条 Alive 链，因为 Dead 和 Alive 是平级的）
```

### 关键规则

- 父状态的 OnUpdate 会在子状态之前执行（从根到叶子）
- 切换状态时，只有跨层级的父状态才会触发 OnEnter/OnExit
- 同一父状态下的子状态切换不会触发父状态的 OnEnter/OnExit
- 状态内部通过 `hfsm.SwitchState()` 触发切换
- 状态内部通过 `owner` 访问控制器的属性和方法

---

## 三、Ability System 使用指南

### 第一步：定义能力枚举

```csharp
public enum E_EnemyAbilityType
{
    Charge,      // 冲锋
    GroundSlam,  // 地面冲击
    Shield,      // 护盾
}
```

### 第二步：创建具体能力类

每个能力继承 `BaseAbility<你的控制器>`。

```csharp
// 冲锋能力
public class ChargeAbility : BaseAbility<EnemyController>
{
    public float chargeSpeed = 15f;
    public float chargeDuration = 0.8f;
    public float cooldown = 3f;

    private float elapsed;
    private float cooldownTimer;
    private Vector2 chargeDirection;

    public ChargeAbility()
    {
        priority = 2;
        isUnlocked = true;
    }

    public override bool CanActivate()
    {
        return base.CanActivate() && cooldownTimer <= 0;
    }

    public override bool CanBeInterrupted()
    {
        // 冲锋前 0.2 秒不可打断（蓄力阶段）
        return elapsed > 0.2f;
    }

    public override void Activate()
    {
        base.Activate();
        elapsed = 0f;
        chargeDirection = (owner.PlayerPosition - owner.transform.position).normalized;
        owner.PlayAnimation("Charge");
    }

    public override void Tick(float deltaTime)
    {
        elapsed += deltaTime;
        cooldownTimer -= deltaTime;

        // 持续施加冲锋速度
        owner.Move(chargeDirection * chargeSpeed * deltaTime);

        if (elapsed >= chargeDuration)
            Deactivate();
    }

    public override void Deactivate()
    {
        base.Deactivate();
        cooldownTimer = cooldown;
    }
}

// 护盾能力（可以和其他能力同时激活）
public class ShieldAbility : BaseAbility<EnemyController>
{
    public float shieldDuration = 5f;
    public float damageReduction = 0.5f;

    private float elapsed;

    public ShieldAbility()
    {
        priority = 1;
        isUnlocked = true;
    }

    public override void Activate()
    {
        base.Activate();
        elapsed = 0f;
        owner.DamageMultiplier *= damageReduction;
        owner.PlayEffect("ShieldOn");
    }

    public override void Tick(float deltaTime)
    {
        elapsed += deltaTime;
        if (elapsed >= shieldDuration)
            Deactivate();
    }

    public override void Deactivate()
    {
        base.Deactivate();
        owner.DamageMultiplier /= damageReduction;
        owner.PlayEffect("ShieldOff");
    }
}
```

### 第三步：在控制器中创建和驱动能力管理器

```csharp
public class EnemyController : MonoBehaviour
{
    public HFSM<E_EnemyStateType, EnemyController> StateMachine { get; private set; }
    public AbilityManager<E_EnemyAbilityType, EnemyController> Abilities { get; private set; }

    public float DamageMultiplier = 1f;
    public Vector2 PlayerPosition;

    void Start()
    {
        // 初始化 HFSM
        StateMachine = new HFSM<E_EnemyStateType, EnemyController>(this);
        // ... 注册状态（同上）
        StateMachine.SwitchState(E_EnemyStateType.Patrol);

        // 初始化 AbilityManager
        Abilities = new AbilityManager<E_EnemyAbilityType, EnemyController>(this);
        Abilities.AddAbilities(E_EnemyAbilityType.Charge, new ChargeAbility());
        Abilities.AddAbilities(E_EnemyAbilityType.GroundSlam, new GroundSlamAbility());
        Abilities.AddAbilities(E_EnemyAbilityType.Shield, new ShieldAbility());
    }

    void Update()
    {
        StateMachine.OnUpdate();
        Abilities.Tick(Time.deltaTime);
    }

    public void Move(Vector2 delta) { /* ... */ }
    public void PlayEffect(string name) { /* ... */ }
}
```

### 第四步：在状态中触发能力

```csharp
public class EnemyState_Chase : HFSM_BaseState<E_EnemyStateType, EnemyController>
{
    public EnemyState_Chase()
    {
        parentType = E_EnemyStateType.Combat;
    }

    public override void OnUpdate()
    {
        owner.MoveTowardPlayer();

        // 距离够近时尝试冲锋
        if (owner.DistanceToPlayer < 8f)
        {
            owner.Abilities.TryActivate(E_EnemyAbilityType.Charge);
            // TryActivate 返回 bool，失败了（冷却中/被压制）不影响后续逻辑
        }

        // 血量低时尝试开盾
        if (owner.CurrentHP < owner.MaxHP * 0.3f)
        {
            owner.Abilities.TryActivate(E_EnemyAbilityType.Shield);
        }

        if (owner.IsInAttackRange())
            hfsm.SwitchState(E_EnemyStateType.Attack);
    }
}
```

### 优先级打断规则

```
优先级高的可以打断优先级低的，但要看 CanBeInterrupted()：

Shield(优先级1) 正在执行 → 尝试激活 Charge(优先级2)：
  Charge 优先级更高 → 检查 Shield.CanBeInterrupted()
  → 返回 true → 打断 Shield → 激活 Charge ✓

Charge(优先级2) 正在执行 → 尝试激活 Shield(优先级1)：
  Shield 优先级更低 → 直接失败 ✗

Charge(优先级2) 蓄力阶段 → 尝试激活 GroundSlam(优先级3)：
  GroundSlam 优先级更高 → 检查 Charge.CanBeInterrupted()
  → 蓄力阶段返回 false → 激活失败 ✗

Charge(优先级2) 冲锋阶段 → 尝试激活 GroundSlam(优先级3)：
  GroundSlam 优先级更高 → 检查 Charge.CanBeInterrupted()
  → 冲锋阶段返回 true → 打断 Charge → 激活 GroundSlam ✓
```

---

## 四、两个系统的协作模式

### 通信方式

两个系统通过 owner（控制器）互相查询，不直接引用对方：

```csharp
// 在 HFSM 状态中 → 查询/触发 Ability
owner.Abilities.TryActivate(E_EnemyAbilityType.Charge);
owner.Abilities.IsActive(E_EnemyAbilityType.Shield);

// 在 Ability 中 → 查询/切换 HFSM 状态
owner.StateMachine.currentState  // 查询当前状态
owner.StateMachine.SwitchState(E_EnemyStateType.Hurt);  // 强制切换状态

// 在 Ability 中 → 查询其他 Ability
// 需要通过 owner 间接访问（Ability 不持有 AbilityManager 引用）
```

### 职责划分原则

```
问自己：这个行为发生时，角色还能同时做别的事吗？

不能（互斥）→ 放 HFSM
  例：Idle、Run、Jump、Fall、WallSlide、Patrol、Chase

能（可叠加）→ 放 Ability
  例：Dash、Attack、Shield、Heal、DoubleJump
```

### 每帧执行顺序

```csharp
void Update()
{
    StateMachine.OnUpdate();          // 先更新状态机（从根到叶子整条链）
    Abilities.Tick(Time.deltaTime);   // 再更新所有激活中的能力
}
```

---

## 五、快速接入清单

将框架接入新项目的最少步骤：

```
1. 复制框架文件（4个）到项目中
2. 定义你的状态枚举
3. 定义你的能力枚举（如果需要 Ability 系统）
4. 创建控制器类（MonoBehaviour）
5. 在控制器的 Start() 中：
   a. new HFSM<枚举, 控制器>(this)
   b. AddState 注册所有状态
   c. SwitchState 设置初始状态
   d. new AbilityManager<枚举, 控制器>(this)（可选）
   e. AddAbilities 注册所有能力（可选）
6. 在控制器的 Update() 中：
   a. StateMachine.OnUpdate()
   b. Abilities.Tick(Time.deltaTime)（可选）
7. 创建具体的状态类和能力类，实现业务逻辑
```
