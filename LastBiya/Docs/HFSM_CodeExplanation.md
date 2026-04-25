# HFSM 泛型分层状态机 — 代码逐行解析文档

---

## 一、脚本总览

本项目的 HFSM（分层有限状态机）由四个脚本组成：

| 脚本 | 所属层 | 职责 |
|------|--------|------|
| HFSM_BaseState.cs | 框架层 | 状态基类，定义所有状态的通用结构 |
| HFSM.cs | 框架层 | 状态机本体，管理状态注册、切换、更新 |
| PlayerController.cs | 业务层 | 玩家控制器，创建并驱动状态机 |
| PlayerState_Idle.cs | 业务层 | 具体的"待机"状态实现 |

框架层使用泛型，不包含任何业务概念，可以直接复制到其他项目使用。
业务层填入具体的泛型参数，定义游戏中实际的状态和行为。

---

## 二、HFSM_BaseState.cs — 状态基类（框架层）

### 完整代码

```csharp
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public enum E_PlayerStateType
{
    None = -1,
    Idle,
}
public class HFSM_BaseState<TKey,TOwner>
{
    #region 非泛型逻辑
    /* ... 注释掉的旧版非泛型代码 ... */
    #endregion

    public TKey parentType;
    public TKey defaultChildType;

    protected HFSM<TKey, TOwner> hfsm;
    protected TOwner owner;

    public void Init(HFSM<TKey, TOwner> hfsm, TOwner owner)
    {
        this.hfsm = hfsm;
        this.owner = owner;
    }

    public virtual void OnEnter() { }
    public virtual void OnUpdate() { }
    public virtual void OnExit() { }
}
```

### 逐行解析

#### 枚举定义

```csharp
public enum E_PlayerStateType
{
    None = -1,
    Idle,
}
```

- `E_PlayerStateType`：定义玩家角色所有可能的状态标识。这是**业务层**的内容，理想情况下应该放在单独的文件中。
- `None = -1`：特殊标记值，表示"没有父状态"。赋值为 -1 是为了和正常状态（从 0 开始自增）区分开。`None` 不会被注册到状态机的字典中，不会创建对应的状态实例，它只是一个逻辑标记。
- `Idle`：自动赋值为 0，代表待机状态。后续会添加 Run、Jump 等更多状态。

#### 泛型类声明

```csharp
public class HFSM_BaseState<TKey, TOwner>
```

- `HFSM_BaseState`：所有状态的基类，任何具体状态（Idle、Run、Jump...）都要继承它。
- `<TKey, TOwner>`：两个泛型参数，是这个类的核心设计：
  - `TKey`（类型键）：状态标识的类型。在本项目中会被填入 `E_PlayerStateType`（枚举），但框架本身不知道这一点。它也可以是 `string`、`int` 或任何其他类型。
  - `TOwner`（拥有者）：持有这个状态机的对象的类型。在本项目中会被填入 `PlayerController`，这样状态内部可以直接访问 PlayerController 的属性和方法，不需要类型转换。

**泛型的意义**：如果不用泛型，这里就要写成 `public E_PlayerStateType parentType;` 和 `protected PlayerController owner;`，直接绑死了玩家的枚举和控制器。换一个项目（比如敌人 AI）就要改这个基类。用泛型后，基类完全不知道具体类型是什么，由使用者在继承时决定。

#### 字段定义

```csharp
public TKey parentType;
public TKey defaultChildType;
```

- `parentType`：当前状态的父状态标识。类型是 `TKey`（泛型），在本项目中实际是 `E_PlayerStateType`。例如 Idle 的 parentType 是 `Grounded`，Grounded 的 parentType 是 `Alive`，Alive 的 parentType 是 `None`（根节点）。
- `defaultChildType`：进入该父状态时默认激活的子状态标识。例如进入 Grounded 时默认进入 Idle。

```csharp
protected HFSM<TKey, TOwner> hfsm;
protected TOwner owner;
```

- `hfsm`：对状态机本体的引用。类型是 `HFSM<TKey, TOwner>`，注意泛型参数要和当前类保持一致。`protected` 意味着只有当前类和子类可以访问。状态内部通过 `hfsm.SwitchState(...)` 来触发状态切换。
- `owner`：对状态机拥有者的引用。类型是 `TOwner`，在本项目中实际是 `PlayerController`。状态内部通过 `owner` 直接访问拥有者的属性（如 `owner.transform.position`、`owner.MoveInput` 等），不需要任何类型转换。

#### Init 方法

```csharp
public void Init(HFSM<TKey, TOwner> hfsm, TOwner owner)
{
    this.hfsm = hfsm;
    this.owner = owner;
}
```

- `Init`：初始化方法，由 HFSM 的 `AddState` 内部自动调用，业务层不需要手动调用。
- 为什么不用构造函数？因为状态是在业务层 `new` 出来再传给 `AddState` 的。如果用构造函数注入，每个状态创建时都要手动传 `hfsm` 和 `owner`，写 10 个状态就重复 10 次。用 `Init` 让框架自动注入，业务层只需要 `new PlayerState_Idle()` 即可。

#### 虚方法

```csharp
public virtual void OnEnter() { }
public virtual void OnUpdate() { }
public virtual void OnExit() { }
```

- `virtual`：虚方法，子类可以通过 `override` 重写来实现具体逻辑。
- `OnEnter()`：状态被进入时调用。用于初始化该状态的行为，如播放动画、设置参数。
- `OnUpdate()`：状态激活期间每帧调用。用于持续性逻辑，如检测输入、更新速度、判断状态切换条件。
- `OnExit()`：状态被退出时调用。用于清理该状态的行为，如停止动画、重置参数。
- 默认实现为空方法体 `{ }`，子类不重写的话什么都不做。

---

## 三、HFSM.cs — 状态机本体（框架层）

### 完整代码

```csharp
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

//泛型分层状态机
public class HFSM<TKey,TOwner>
{
    public Dictionary<TKey,HFSM_BaseState<TKey,TOwner>> stateDic;
    private TKey noneKey;
    public TOwner owner;

    public HFSM_BaseState<TKey,TOwner> currentState;
    public HFSM(TOwner owner, TKey noneKey)
    { 
        this.owner = owner;
        this.noneKey = noneKey;
        stateDic = new();
    }

    public void AddState(TKey type, HFSM_BaseState<TKey, TOwner> state)
    {
        if (stateDic.ContainsKey(type)) return;
        state.Init(this, owner);
        stateDic.Add(type, state);
    }

    public void SwitchState(TKey type) { /* ... */ }
    public void OnUpdate() { /* ... */ }
    private HFSM_BaseState<TKey, TOwner> FindCommonParent(...) { /* ... */ }
    private HFSM_BaseState<TKey, TOwner> GetParent(...) { /* ... */ }
}
```

### 逐行解析

#### 泛型类声明

```csharp
public class HFSM<TKey, TOwner>
```

- 和 `HFSM_BaseState` 使用相同的泛型参数 `<TKey, TOwner>`，保证状态机和它管理的状态使用同一套类型系统。
- 当业务层写 `HFSM<E_PlayerStateType, PlayerController>` 时，这个类内部所有的 `TKey` 都变成 `E_PlayerStateType`，所有的 `TOwner` 都变成 `PlayerController`。

#### 字段定义

```csharp
public Dictionary<TKey, HFSM_BaseState<TKey, TOwner>> stateDic;
```

- 状态字典，key 是状态标识（`TKey`），value 是状态实例（`HFSM_BaseState<TKey, TOwner>`）。所有注册的状态都存在这里。
- 在本项目中，实际类型是 `Dictionary<E_PlayerStateType, HFSM_BaseState<E_PlayerStateType, PlayerController>>`，但你不需要写这么长，泛型帮你处理了。

```csharp
private TKey noneKey;
```

- 存储"无父状态"的标记值。在本项目中是 `E_PlayerStateType.None`。
- `private`：只有 HFSM 内部使用，外部不需要访问。
- 为什么要存这个值？因为 `TKey` 是泛型，不能直接写 `if (state.parentType == E_PlayerStateType.None)`。框架不知道 `TKey` 具体是什么类型，所以需要在构造时把"哪个值代表 None"告诉它。

```csharp
public TOwner owner;
```

- 状态机拥有者的引用。在本项目中是 `PlayerController` 实例。
- 在 `AddState` 时会通过 `Init` 传递给每个状态，让状态内部可以访问拥有者。

```csharp
public HFSM_BaseState<TKey, TOwner> currentState;
```

- 当前激活的叶子状态。状态机在任意时刻只有一个当前状态。

#### 构造函数

```csharp
public HFSM(TOwner owner, TKey noneKey)
{ 
    this.owner = owner;
    this.noneKey = noneKey;
    stateDic = new();
}
```

- `owner`：接收拥有者实例。在 PlayerController 中传入 `this`。
- `noneKey`：接收"无父状态"的标记值。在本项目中传入 `E_PlayerStateType.None`。
- `stateDic = new()`：初始化空字典。C# 9.0 的简写语法，等价于 `new Dictionary<TKey, HFSM_BaseState<TKey, TOwner>>()`。

#### AddState 方法

```csharp
public void AddState(TKey type, HFSM_BaseState<TKey, TOwner> state)
{
    if (stateDic.ContainsKey(type)) return;
    state.Init(this, owner);
    stateDic.Add(type, state);
}
```

- `if (stateDic.ContainsKey(type)) return;`：防止重复注册同一个状态标识。
- `state.Init(this, owner);`：**关键步骤**。在注册时自动调用状态的 Init 方法，把状态机自身（`this`）和拥有者（`owner`）注入到状态中。这就是为什么业务层不需要在 `new` 状态时传参数。
- `stateDic.Add(type, state);`：将状态标识和状态实例的映射关系存入字典。

#### SwitchState 方法

```csharp
public void SwitchState(TKey type)
{
    if (!stateDic.ContainsKey(type)) return;
```

- 安全检查：如果目标状态没有注册过，直接返回，避免字典查找异常。

```csharp
    // 首次进入，没有当前状态，直接走完整进入链
    if (currentState == null)
    {
        HFSM_BaseState<TKey, TOwner> target = stateDic[type];
```

- 首次调用 `SwitchState` 时（游戏刚启动），`currentState` 还是 null。这时没有"当前状态"可以退出，也没有公共祖先可以找，需要特殊处理。
- `target`：从字典中取出目标状态实例。

```csharp
        List<HFSM_BaseState<TKey, TOwner>> enterPath = new List<HFSM_BaseState<TKey, TOwner>>();
        HFSM_BaseState<TKey, TOwner> s = target;
        while (s != null)
        {
            enterPath.Add(s);
            s = GetParent(s);
        }
        enterPath.Reverse();
```

- 从目标状态往上收集整条祖先链。例如目标是 Idle，收集到的是 `[Idle, Grounded, Alive]`。
- `GetParent(s)`：获取 s 的父状态，到根节点时返回 null，循环终止。
- `Reverse()`：反转列表，变成从根到叶子的顺序 `[Alive, Grounded, Idle]`，因为进入时要从上往下依次调用 OnEnter。

```csharp
        foreach (HFSM_BaseState<TKey, TOwner> state in enterPath)
        {
            state.OnEnter();
        }
        currentState = target;
        return;
    }
```

- 依次调用每个状态的 OnEnter：`Alive.OnEnter() → Grounded.OnEnter() → Idle.OnEnter()`。
- 设置当前状态为目标状态。
- `return`：首次进入处理完毕，不执行后面的正常切换逻辑。

```csharp
    HFSM_BaseState<TKey, TOwner> CommonParent = FindCommonParent(currentState, stateDic[type]);
```

- **LCA（最近公共祖先）算法的入口**。找到当前状态和目标状态在树中的最近公共祖先。
- 例如从 Idle（Grounded 下）切到 Jump（Airborne 下），公共祖先是 Alive。
- 例如从 Idle 切到 Run（都在 Grounded 下），公共祖先是 Grounded。

```csharp
    HFSM_BaseState<TKey, TOwner> tempState = currentState;
    while (tempState != null && tempState != CommonParent)
    {
        tempState.OnExit();
        tempState = GetParent(tempState);
    }
```

- **退出阶段**：从当前状态往上逐层退出，直到公共祖先（不包含公共祖先本身）。
- 例如 Idle → Jump：退出 `Idle.OnExit() → Grounded.OnExit()`，到 Alive（公共祖先）停止。
- 例如 Idle → Run：退出 `Idle.OnExit()`，到 Grounded（公共祖先）停止。Grounded 不退出。

```csharp
    tempState = stateDic[type];
    List<HFSM_BaseState<TKey, TOwner>> enterPath2 = new List<HFSM_BaseState<TKey, TOwner>>();
    while (tempState != null && tempState != CommonParent)
    { 
        enterPath2.Add(tempState);
        tempState = GetParent(tempState);
    }
    enterPath2.Reverse();
    foreach (HFSM_BaseState<TKey, TOwner> state in enterPath2)
    { 
        state.OnEnter();
    }
```

- **进入阶段**：从公共祖先往下逐层进入目标状态。
- 先从目标状态往上收集到公共祖先的路径，然后反转（从上往下），依次调用 OnEnter。
- 例如 Idle → Jump：进入 `Airborne.OnEnter() → Jump.OnEnter()`。
- 例如 Idle → Run：进入 `Run.OnEnter()`。

```csharp
    currentState = stateDic[type];
```

- 更新当前状态为目标状态。

#### OnUpdate 方法

```csharp
public void OnUpdate()
{
    List<HFSM_BaseState<TKey, TOwner>> chain = new();
    HFSM_BaseState<TKey, TOwner> tempState = currentState;
    while (tempState != null)
    {
        chain.Add(tempState);
        tempState = GetParent(tempState);
    }
    chain.Reverse();

    foreach (HFSM_BaseState<TKey, TOwner> state in chain)
    {
        state.OnUpdate();
    }
}
```

- 每帧调用。不是只调用当前叶子状态的 OnUpdate，而是从根到叶子整条链都调用。
- 例如当前状态是 Idle，执行顺序是：`Alive.OnUpdate() → Grounded.OnUpdate() → Idle.OnUpdate()`。
- 这样父状态可以处理该层级的共性逻辑（如 Alive 检测受伤、Grounded 检测离地），子状态只处理自己独有的逻辑。

#### FindCommonParent 方法

```csharp
private HFSM_BaseState<TKey, TOwner> FindCommonParent(
    HFSM_BaseState<TKey, TOwner> currentState, 
    HFSM_BaseState<TKey, TOwner> targetState)
{
    List<HFSM_BaseState<TKey, TOwner>> ancestors = new List<HFSM_BaseState<TKey, TOwner>>();
    HFSM_BaseState<TKey, TOwner> tempState = currentState;
    while (tempState != null)
    {
        ancestors.Add(tempState);
        tempState = GetParent(tempState);
    }
```

- 第一步：收集当前状态的所有祖先（包括自身），存入列表。
- 例如当前是 Idle，收集到 `[Idle, Grounded, Alive]`。

```csharp
    tempState = targetState;
    while (tempState != null)
    {
        if (ancestors.Contains(tempState)) return tempState;
        tempState = GetParent(tempState);
    }
    return null;
}
```

- 第二步：从目标状态往上走，每走一步检查是否在当前状态的祖先列表中。第一个匹配的就是最近公共祖先。
- 例如目标是 Jump（祖先链：Jump → Airborne → Alive），走到 Alive 时发现它在 Idle 的祖先列表中，返回 Alive。
- `return null`：理论上不会执行到这里，因为所有状态都有共同的根节点。

#### GetParent 方法

```csharp
private HFSM_BaseState<TKey, TOwner> GetParent(HFSM_BaseState<TKey, TOwner> state)
{
    if (EqualityComparer<TKey>.Default.Equals(state.parentType, noneKey)) return null;
    stateDic.TryGetValue(state.parentType, out var parent);
    return parent;
}
```

- 根据状态的 `parentType` 从字典中查找父状态实例。
- `EqualityComparer<TKey>.Default.Equals(state.parentType, noneKey)`：泛型类型不能直接用 `==` 比较（因为编译器不知道 TKey 是否支持 `==` 运算符），所以用 `EqualityComparer` 来做相等性判断。如果 `parentType` 等于 `noneKey`（即 `None`），说明已经到了根节点，返回 null。
- `TryGetValue`：安全查找，如果字典中没有对应的 key，`parent` 会是 null，不会抛异常。

---

## 四、PlayerController.cs — 玩家控制器（业务层）

### 完整代码

```csharp
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private HFSM<E_PlayerStateType, PlayerController> playerFsm;

    // Start is called before the first frame update
    void Start()
    {
        playerFsm = new HFSM<E_PlayerStateType, PlayerController>(this, E_PlayerStateType.None);

        playerFsm.AddState(E_PlayerStateType.Idle, new PlayerState_Idle());

        playerFsm.SwitchState(E_PlayerStateType.Idle);
    }

    // Update is called once per frame
    void Update()
    {
        playerFsm.OnUpdate();
    }
}
```

### 逐行解析

```csharp
private HFSM<E_PlayerStateType, PlayerController> playerFsm;
```

- 声明状态机实例。**这里是泛型参数被"填入"的地方**：
  - `TKey` = `E_PlayerStateType`：用玩家状态枚举作为状态标识
  - `TOwner` = `PlayerController`：玩家控制器自身作为拥有者
- 从这一刻起，`playerFsm` 内部的字典类型确定为 `Dictionary<E_PlayerStateType, HFSM_BaseState<E_PlayerStateType, PlayerController>>`，所有状态的 `owner` 字段类型确定为 `PlayerController`。编译器自动完成这些类型替换。

```csharp
playerFsm = new HFSM<E_PlayerStateType, PlayerController>(this, E_PlayerStateType.None);
```

- 创建状态机实例，传入两个参数：
  - `this`：PlayerController 自身实例，作为 `owner`。之后所有状态内部的 `owner` 都指向这个 PlayerController。
  - `E_PlayerStateType.None`：告诉状态机"哪个枚举值代表没有父状态"。框架层的 `GetParent` 方法会用这个值来判断是否到达了根节点。

```csharp
playerFsm.AddState(E_PlayerStateType.Idle, new PlayerState_Idle());
```

- 注册 Idle 状态。`AddState` 内部会自动调用 `state.Init(this, owner)`，把状态机引用和 PlayerController 引用注入到 PlayerState_Idle 中。
- 业务层只需要 `new PlayerState_Idle()`，不需要传任何参数。

```csharp
playerFsm.SwitchState(E_PlayerStateType.Idle);
```

- 切换到 Idle 状态。因为是首次调用（currentState 为 null），会走首次进入的分支，从根到 Idle 依次调用 OnEnter。

```csharp
void Update()
{
    playerFsm.OnUpdate();
}
```

- 每帧驱动状态机更新。会从根到当前叶子状态依次调用 OnUpdate。

---

## 五、PlayerState_Idle.cs — 待机状态（业务层）

### 完整代码

```csharp
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerState_Idle : HFSM_BaseState<E_PlayerStateType, PlayerController>
{
    public PlayerState_Idle()
    {
        parentType = E_PlayerStateType.None;
    }

    public override void OnExit()
    {
        base.OnExit();
    }

    public override void OnUpdate()
    {
        base.OnUpdate();
    }

    public override void OnEnter()
    {
        base.OnEnter();
    }
}
```

### 逐行解析

```csharp
public class PlayerState_Idle : HFSM_BaseState<E_PlayerStateType, PlayerController>
```

- 继承泛型基类，**填入具体类型**：
  - `TKey` = `E_PlayerStateType`：和 PlayerController 中声明的状态机使用同一个枚举类型
  - `TOwner` = `PlayerController`：拥有者是 PlayerController
- 继承后，这个类内部的 `owner` 字段类型自动变成 `PlayerController`，`hfsm` 字段类型自动变成 `HFSM<E_PlayerStateType, PlayerController>`。可以直接写 `owner.transform.position` 而不需要类型转换。

```csharp
public PlayerState_Idle()
{
    parentType = E_PlayerStateType.None;
}
```

- 构造函数中声明自己的父状态标识。`None` 表示 Idle 当前是根节点（没有父状态）。
- 后续添加 Grounded 父状态后，这里会改成 `parentType = E_PlayerStateType.Grounded;`。

```csharp
public override void OnEnter() { base.OnEnter(); }
public override void OnUpdate() { base.OnUpdate(); }
public override void OnExit() { base.OnExit(); }
```

- 重写三个生命周期方法。当前调用 `base.xxx()` 执行基类的空实现，实际效果等于什么都不做。
- 后续在这里添加具体逻辑，例如：
  - `OnEnter`：播放待机动画
  - `OnUpdate`：检测移动输入，如果有输入则 `hfsm.SwitchState(E_PlayerStateType.Run)`
  - `OnExit`：停止待机动画

---

## 六、泛型机制总结

### 类型传递链路

```
PlayerController 声明时填入具体类型：
  HFSM<E_PlayerStateType, PlayerController>
    │
    ├── stateDic 的类型确定为：
    │   Dictionary<E_PlayerStateType, HFSM_BaseState<E_PlayerStateType, PlayerController>>
    │
    ├── currentState 的类型确定为：
    │   HFSM_BaseState<E_PlayerStateType, PlayerController>
    │
    └── 所有状态的 owner 类型确定为：
        PlayerController

PlayerState_Idle 继承时填入具体类型：
  HFSM_BaseState<E_PlayerStateType, PlayerController>
    │
    ├── parentType 的类型确定为：E_PlayerStateType
    ├── hfsm 的类型确定为：HFSM<E_PlayerStateType, PlayerController>
    └── owner 的类型确定为：PlayerController
```

### 如果要给敌人用同一套框架

```csharp
// 敌人定义自己的枚举
public enum E_SlimeState { None = -1, Patrol, Chase, Attack }

// 敌人的状态继承同一个泛型基类，填入不同的类型参数
public class SlimeState_Patrol : HFSM_BaseState<E_SlimeState, SlimeController>
{
    // owner 的类型是 SlimeController，不是 PlayerController
}

// 敌人的控制器创建自己的状态机
HFSM<E_SlimeState, SlimeController> slimeFsm 
    = new HFSM<E_SlimeState, SlimeController>(this, E_SlimeState.None);
```

框架层的 `HFSM.cs` 和 `HFSM_BaseState.cs` 一行代码都不需要改。
