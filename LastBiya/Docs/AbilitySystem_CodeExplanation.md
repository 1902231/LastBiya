# Ability 系统代码解析文档 — 与 HFSM 对比说明

---

## 一、两个系统的定位对比

| | HFSM（分层状态机） | Ability System（能力系统） |
|---|---|---|
| 管什么 | 互斥的运动状态（Idle、Run、Jump、Fall） | 可叠加的动作能力（Dash、Attack、Heal） |
| 同时激活 | 只能有一个当前状态 | 可以多个同时激活 |
| 核心算法 | LCA（最近公共祖先）决定退出链和进入链 | 优先级 + CanBeInterrupted 决定打断关系 |
| 数据结构 | 树形（父子层级） | 扁平列表（优先级排序） |
| 生命周期 | OnEnter → OnUpdate → OnExit | Activate → Tick → Deactivate |
| 适合的行为 | 角色不能同时处于两个状态的行为 | 可以叠加在任何运动状态上的行为 |

两者不是替代关系，而是分工关系：
- HFSM 回答"角色当前的运动姿态是什么"
- Ability 回答"角色当前正在执行哪些动作能力"

---

## 二、脚本总览

Ability 系统由两个脚本组成：

| 脚本 | 所属层 | 职责 |
|------|--------|------|
| BaseAbility.cs | 框架层 | 能力基类，定义所有能力的通用结构和生命周期 |
| AbilityManager.cs | 框架层 | 能力管理器，管理注册、激活、打断、每帧更新 |

---

## 三、BaseAbility.cs — 能力基类（框架层）

### 完整代码

```csharp
public abstract class BaseAbility<TOwner>
{
    public int priority;
    public bool isUnlocked;
    public bool isActive;

    protected TOwner owner;

    public void Init(TOwner owner)
    {
        this.owner = owner;
    }

    public virtual bool CanActivate()
    {
        return isUnlocked && !isActive;
    }

    public virtual bool CanBeInterrupted()
    {
        return true;
    }

    public virtual void Activate() { isActive = true; }
    public virtual void Tick(float deltaTime) { }
    public virtual void Deactivate() { isActive = false; }
}
```

### 逐行解析

#### 类声明

```csharp
public abstract class BaseAbility<TOwner>
```

- `abstract`：抽象类，不能直接 new，必须被继承。和 HFSM_BaseState 不同（HFSM_BaseState 不是 abstract），这里用 abstract 是因为一个"没有具体行为的能力"没有意义，强制要求子类实现具体逻辑。
- `<TOwner>`：只有一个泛型参数。

**与 HFSM_BaseState 的泛型对比：**

| | HFSM_BaseState | BaseAbility |
|---|---|---|
| 泛型参数 | `<TKey, TOwner>` | `<TOwner>` |
| 为什么 | 状态需要 TKey 来声明 parentType（父状态标识） | 能力不需要知道自己在字典里的 key 是什么 |

Ability 不存在父子层级关系，所以不需要 TKey 来标识"我的父能力是谁"。

#### 字段定义

```csharp
public int priority;
public bool isUnlocked;
public bool isActive;
protected TOwner owner;
```

- `priority`：优先级数值。数值越高，优先级越高。用于 AbilityManager 的打断判断。
- `isUnlocked`：是否已解锁。用于能力解锁系统（类似空洞骑士的能力获取）。未解锁的能力 CanActivate 返回 false。
- `isActive`：当前是否正在执行。一个 Ability 从 Activate 到 Deactivate 之间 isActive 为 true。
- `owner`：拥有者引用。类型是 TOwner，在本项目中实际是 PlayerController。protected 意味着只有子类可以访问。

**与 HFSM_BaseState 字段的对比：**

| HFSM_BaseState 字段 | BaseAbility 字段 | 说明 |
|---|---|---|
| `parentType` | 无 | Ability 没有父子层级 |
| `defaultChildType` | 无 | Ability 没有子能力概念 |
| `hfsm`（状态机引用） | 无 | Ability 不需要引用 AbilityManager（通过 owner 间接访问） |
| `owner` | `owner` | 相同，都是拥有者引用 |
| 无 | `priority` | HFSM 靠树结构管关系，Ability 靠优先级数值 |
| 无 | `isUnlocked` | HFSM 状态不存在"解锁"概念 |
| 无 | `isActive` | HFSM 用 currentState 判断，Ability 每个实例自己记录 |

#### Init 方法

```csharp
public void Init(TOwner owner)
{
    this.owner = owner;
}
```

- 由 AbilityManager 的 AddAbilities 内部自动调用，注入 owner 引用。
- 和 HFSM_BaseState 的 Init 对比：HFSM_BaseState.Init 接收两个参数（hfsm 和 owner），因为状态需要通过 hfsm 引用来调用 SwitchState。BaseAbility.Init 只接收 owner，因为能力不需要直接操作 AbilityManager。

#### CanActivate 方法

```csharp
public virtual bool CanActivate()
{
    return isUnlocked && !isActive;
}
```

- 判断当前条件下能否激活这个能力。
- 默认条件：已解锁 且 当前没有在执行中。
- 子类可以 override 添加更多条件，例如冲刺能力加冷却判断：

```csharp
public override bool CanActivate()
{
    return base.CanActivate() && cooldownTimer <= 0;
}
```

**HFSM 没有对应概念。** 状态切换由 SwitchState 直接执行，不存在"能不能切换"的前置判断（除了检查目标状态是否注册）。

#### CanBeInterrupted 方法

```csharp
public virtual bool CanBeInterrupted()
{
    return true;
}
```

- 判断当前时刻是否允许被更高优先级的能力打断。
- 默认返回 true（随时可以被打断）。
- 子类可以 override 实现条件性打断，例如冲刺的无敌帧期间不可打断：

```csharp
public override bool CanBeInterrupted()
{
    return elapsed > invincibleDuration;  // 无敌帧结束后才可打断
}
```

**HFSM 没有对应概念。** 状态切换是无条件的，调用 SwitchState 就一定会切换。

#### 生命周期方法

```csharp
public virtual void Activate() { isActive = true; }
public virtual void Tick(float deltaTime) { }
public virtual void Deactivate() { isActive = false; }
```

**与 HFSM_BaseState 生命周期的对比：**

| HFSM_BaseState | BaseAbility | 触发时机 |
|---|---|---|
| `OnEnter()` | `Activate()` | 进入/激活时 |
| `OnUpdate()` | `Tick(float deltaTime)` | 每帧执行 |
| `OnExit()` | `Deactivate()` | 退出/结束时 |

关键区别：
- HFSM 的 OnUpdate 不接收 deltaTime（通过 Time.deltaTime 全局访问），Ability 的 Tick 显式接收 deltaTime 参数，更灵活。
- HFSM 的 OnUpdate 是从根到叶子整条链都执行（父状态的 OnUpdate 也会执行），Ability 的 Tick 只执行自己的。
- Ability 的 Tick 中可以调用 Deactivate() 来结束自己（比如冲刺时间到了），HFSM 的状态不会在 OnUpdate 中"结束自己"，而是通过 SwitchState 切换到另一个状态。

---

## 四、AbilityManager.cs — 能力管理器（框架层）

### 完整代码

```csharp
public class AbilityManager<TAbilityType, TOwner>
{
    private Dictionary<TAbilityType, BaseAbility<TOwner>> abilityDic;
    private List<BaseAbility<TOwner>> activeAbilities = new();
    public TOwner Owner;

    public AbilityManager(TOwner owner)
    { 
        Owner = owner;
        abilityDic = new();
    }

    public void AddAbilities(TAbilityType type, BaseAbility<TOwner> ability)
    {
        if (abilityDic.ContainsKey(type)) return;
        ability.Init(Owner);
        abilityDic.Add(type, ability);
    }

    public BaseAbility<TOwner> Get(TAbilityType type) { /* ... */ }
    public T Get<T>(TAbilityType type) where T : BaseAbility<TOwner> { /* ... */ }
    public bool IsActive(TAbilityType type) { /* ... */ }
    public bool TryActivate(TAbilityType type) { /* ... */ }
    public void Tick(float deltaTime) { /* ... */ }
}
```

### 逐行解析

#### 类声明

```csharp
public class AbilityManager<TAbilityType, TOwner>
```

- `TAbilityType`：能力标识的类型（枚举），对应 HFSM 的 TKey。
- `TOwner`：拥有者类型，和 HFSM 的 TOwner 相同。

**与 HFSM 类声明的对比：**

| | HFSM | AbilityManager |
|---|---|---|
| 声明 | `HFSM<TKey, TOwner> where TKey : struct` | `AbilityManager<TAbilityType, TOwner>` |
| 泛型约束 | `where TKey : struct`（因为用了 TKey? nullable） | 无约束（不需要 nullable） |

AbilityManager 不需要 `where TKey : struct` 约束，因为它不使用 nullable 类型。能力没有父子层级，不需要用 null 表示"没有父能力"。

#### 两个容器

```csharp
private Dictionary<TAbilityType, BaseAbility<TOwner>> abilityDic;
private List<BaseAbility<TOwner>> activeAbilities = new();
```

- `abilityDic`：所有已注册的能力，不管是否激活。类似 HFSM 的 stateDic。
- `activeAbilities`：当前正在执行的能力列表。**HFSM 没有对应概念**，因为 HFSM 只有一个 currentState，而 AbilityManager 可以同时有多个激活的能力。

**这是两个系统最根本的结构差异：**

```
HFSM:           currentState = 一个状态（互斥）
AbilityManager: activeAbilities = 一个列表（可并行）
```

#### 构造函数

```csharp
public AbilityManager(TOwner owner)
{ 
    Owner = owner;
    abilityDic = new();
}
```

只接收 owner。和 HFSM 构造函数对比：

| | HFSM 构造函数 | AbilityManager 构造函数 |
|---|---|---|
| 参数 | `(TOwner owner)` | `(TOwner owner)` |
| 说明 | 之前需要 noneKey，改用 nullable 后不再需要 | 从来不需要额外参数 |

#### AddAbilities 方法

```csharp
public void AddAbilities(TAbilityType type, BaseAbility<TOwner> ability)
{
    if (abilityDic.ContainsKey(type)) return;
    ability.Init(Owner);
    abilityDic.Add(type, ability);
}
```

- 和 HFSM 的 AddState 逻辑完全对称：防重复 → 自动注入 → 存入字典。
- `ability.Init(Owner)`：自动注入 owner，业务层不需要手动传。

**与 HFSM.AddState 的对比：**

```csharp
// HFSM
public void AddState(TKey type, HFSM_BaseState<TKey, TOwner> state)
{
    if (stateDic.ContainsKey(type)) return;
    state.Init(this, owner);  // 注入 hfsm + owner
    stateDic.Add(type, state);
}

// AbilityManager
public void AddAbilities(TAbilityType type, BaseAbility<TOwner> ability)
{
    if (abilityDic.ContainsKey(type)) return;
    ability.Init(Owner);       // 只注入 owner
    abilityDic.Add(type, ability);
}
```

HFSM_BaseState 需要 hfsm 引用（用于在状态内部调用 SwitchState），BaseAbility 不需要 AbilityManager 引用。

#### Get 方法（两个重载）

```csharp
public BaseAbility<TOwner> Get(TAbilityType type)
{
    abilityDic.TryGetValue(type, out var ability);
    return ability;
}

public T Get<T>(TAbilityType type) where T : BaseAbility<TOwner>
{
    abilityDic.TryGetValue(type, out var ability);
    return ability as T;
}
```

- 第一个返回基类类型，日常使用。
- 第二个返回具体子类类型，用于需要访问子类特有字段的场景（如护符修改冲刺距离）。

**HFSM 没有 Get 方法**，因为状态切换通过 SwitchState(枚举) 完成，不需要拿到状态实例。

#### IsActive 方法

```csharp
public bool IsActive(TAbilityType type)
{
    return abilityDic.TryGetValue(type, out var a) && a.isActive;
}
```

- 查询某个能力是否正在执行。HFSM 的状态可以用这个来做判断。

**HFSM 的等价操作是直接比较 currentState**，因为只有一个当前状态。Ability 需要 IsActive 是因为可以有多个同时激活。

#### TryActivate 方法（核心）

```csharp
public bool TryActivate(TAbilityType type)
{
    if (!abilityDic.TryGetValue(type, out var ability)) return false;
    if (!ability.CanActivate()) return false;

    foreach (var active in activeAbilities)
    {
        if (active.priority >= ability.priority && !active.CanBeInterrupted())
            return false;
    }

    for (int i = activeAbilities.Count - 1; i >= 0; i--)
    {
        if (activeAbilities[i].priority < ability.priority)
        {
            activeAbilities[i].Deactivate();
            activeAbilities.RemoveAt(i);
        }
    }

    ability.Activate();
    activeAbilities.Add(ability);
    return true;
}
```

四步逻辑：

1. 从字典取出能力实例，取不到则失败
2. 问能力自己 CanActivate()，不满足则失败
3. 遍历当前所有激活的能力，如果有优先级更高或相同的且不可打断的，则失败
4. 打断所有优先级更低的，然后激活新能力

**与 HFSM.SwitchState 的对比：**

| | HFSM.SwitchState | AbilityManager.TryActivate |
|---|---|---|
| 前置检查 | 只检查目标状态是否注册 | 检查注册 + CanActivate + 优先级压制 |
| 可能失败 | 不会（只要注册了就一定切换） | 会（条件不满足返回 false） |
| 退出旧的 | LCA 算法决定退出哪些层级 | 优先级比较决定打断哪些能力 |
| 进入新的 | LCA 算法决定进入哪些层级 | 直接 Activate |
| 返回值 | void | bool（告诉调用者是否成功） |

#### Tick 方法

```csharp
public void Tick(float deltaTime)
{
    for (int i = activeAbilities.Count - 1; i >= 0; i--)
    {
        activeAbilities[i].Tick(deltaTime);
        if (!activeAbilities[i].isActive)
            activeAbilities.RemoveAt(i);
    }
}
```

- 倒序遍历所有激活中的能力，调用每个的 Tick。
- 如果某个能力在 Tick 中调用了 Deactivate()（isActive 变 false），立即从列表移除。
- 倒序遍历是因为 RemoveAt 会改变列表长度，正序会跳过元素。

**与 HFSM.OnUpdate 的对比：**

| | HFSM.OnUpdate | AbilityManager.Tick |
|---|---|---|
| 遍历对象 | 从根到叶子的祖先链（纵向） | activeAbilities 列表（横向） |
| 执行顺序 | 父状态先执行，子状态后执行 | 无固定顺序 |
| 自动清理 | 不会（状态不会自己消失） | 会（isActive 为 false 时自动移除） |
| 参数 | 无参数 | 接收 deltaTime |

---

## 五、两个系统的协作方式

在 PlayerController 中，两个系统并行运行：

```csharp
public class PlayerController : MonoBehaviour
{
    private HFSM<E_PlayerStateType, PlayerController> playerFsm;
    private AbilityManager<E_PlayerAbilityType, PlayerController> abilities;

    void Start()
    {
        // 初始化 HFSM
        playerFsm = new HFSM<E_PlayerStateType, PlayerController>(this);
        playerFsm.AddState(E_PlayerStateType.Idle, new PlayerState_Idle());
        playerFsm.SwitchState(E_PlayerStateType.Idle);

        // 初始化 AbilityManager
        abilities = new AbilityManager<E_PlayerAbilityType, PlayerController>(this);
        // abilities.AddAbilities(E_PlayerAbilityType.Dash, new DashAbility());
    }

    void Update()
    {
        playerFsm.OnUpdate();              // 驱动状态机
        abilities.Tick(Time.deltaTime);     // 驱动能力系统
    }
}
```

两个系统通过 owner（PlayerController）互相查询：

```csharp
// 在 HFSM 状态中查询 Ability
// owner.abilities.IsActive(E_PlayerAbilityType.Dash)

// 在 Ability 中查询 HFSM 状态
// owner.playerFsm.currentState

// 在 Ability 中切换 HFSM 状态
// owner.playerFsm.SwitchState(E_PlayerStateType.xxx)
```

---

## 六、优先级打断机制详解

这是 Ability 系统独有的机制，HFSM 没有对应概念。

### 优先级分层

```
优先级 3（最高）：Hurt, Death
优先级 2：        Dash, Spell
优先级 1：        Attack, Heal
优先级 0（最低）：DoubleJump, WallJump
```

### 打断规则

打断由两层控制：

1. `priority`（结构性规则）：高优先级有资格打断低优先级
2. `CanBeInterrupted()`（时序性规则）：即使有资格，也要看目标此刻是否允许被打断

### 打断场景示例

```
冲刺中(优先级2) → 受伤(优先级3)：
  受伤优先级更高 → 检查冲刺的 CanBeInterrupted()
  → 如果冲刺在无敌帧内：CanBeInterrupted() 返回 false → 受伤激活失败
  → 如果冲刺无敌帧已过：CanBeInterrupted() 返回 true → 打断冲刺 → 受伤激活

攻击中(优先级1) → 冲刺(优先级2)：
  冲刺优先级更高 → 检查攻击的 CanBeInterrupted()
  → 如果攻击在前摇/判定中：返回 false → 冲刺激活失败
  → 如果攻击在后摇中：返回 true → 打断攻击 → 冲刺激活

冲刺中(优先级2) → 攻击(优先级1)：
  攻击优先级更低 → 直接失败，不检查 CanBeInterrupted
```

---

## 七、完整架构对比总结

| 维度 | HFSM | Ability System |
|------|------|----------------|
| 核心数据结构 | 树（父子层级） | 字典 + 列表（扁平） |
| 同时激活数量 | 1 个 | 多个 |
| 关系管理 | 父子层级（parentType） | 优先级数值（priority） |
| 切换/激活机制 | LCA 算法（自动计算退出链和进入链） | 优先级比较 + CanBeInterrupted |
| 切换是否可能失败 | 不会 | 会 |
| 每帧更新范围 | 从根到叶子整条链 | 所有激活中的能力 |
| 自动清理 | 不会 | Tick 中自动移除已结束的能力 |
| 泛型参数 | `<TKey, TOwner>` + `where TKey : struct` | BaseAbility: `<TOwner>`, Manager: `<TAbilityType, TOwner>` |
| owner 注入 | Init(hfsm, owner) | Init(owner) |
| 适合管理 | 互斥运动状态 | 可叠加动作能力 |
