# Bug分析报告：冲刺穿墙 & 受伤后移动抖动

## 问题1：冲刺穿墙问题

### 根本原因

#### 1. **每帧强制设置速度导致物理引擎失效**
```csharp
// PlayerState_Dash.cs - OnFixedUpdate()
public override void OnFixedUpdate()
{
    if (!hasBounced)
        owner.Rb.velocity = dashDir * owner.DashSpeed;  // ❌ 问题所在
}
```

**问题分析：**
- 在 `OnFixedUpdate()` 中**每帧强制覆盖速度**
- Unity的物理引擎在碰撞时会自动调整速度，但下一帧立即被代码覆盖
- 导致碰撞检测形同虚设，玩家会"推着墙壁"移动，最终穿透

#### 2. **缺少墙壁检测**
```csharp
// PlayerState_Dash.cs - OnUpdate()
public override void OnUpdate()
{
    timer -= Time.deltaTime;
    if (timer <= 0)
    {
        // 只检查时间，不检查墙壁 ❌
        if (owner.isGrounded)
            hfsm.SwitchState(E_PlayerStateType.Grounded);
        else
            hfsm.SwitchState(E_PlayerStateType.AirBornd);
    }
}
```

**对比：下落冲刺有墙壁检测**
```csharp
// PlayerState_FallingDash.cs - OnUpdate()
// 墙壁检测优先于落地检测 ✓
if (owner.IsWallOnEitherSide())
{
    hfsm.SwitchState(E_PlayerStateType.FreeFall);
    return;
}
```

#### 3. **高速移动 + 离散碰撞检测**
- `DashSpeed = 20f`，每帧移动距离约 `20 * 0.02 = 0.4米`
- 如果墙壁碰撞体厚度 < 0.4米，可能在一帧内穿过
- 需要在Unity中设置 **Continuous** 碰撞检测模式

### Unity场景配置问题

#### 必须检查的配置：

1. **Rigidbody2D 设置（玩家物体）**
   - ❌ Collision Detection: **Discrete**（离散，默认）
   - ✓ Collision Detection: **Continuous**（连续，推荐）
   - 说明：Continuous模式会在高速移动时进行更精确的碰撞检测

2. **墙壁碰撞体厚度**
   - 墙壁的 BoxCollider2D 或其他碰撞体厚度应该 **≥ 0.5米**
   - 太薄的碰撞体容易被高速物体穿透

3. **Layer设置**
   - 确认 `wallLayer` 在 Inspector 中正确设置
   - 确认墙壁物体的 Layer 与 `wallLayer` 匹配
   - 检查 Physics2D 设置中的 Layer Collision Matrix

4. **Rigidbody2D 约束**
   - Constraints → Freeze Rotation Z：应该勾选（防止旋转）

### 代码结构问题

1. **速度设置方式错误**
   - ❌ 在 `OnFixedUpdate` 中强制设置 `velocity`
   - ✓ 应该只在 `OnEnter` 设置一次，或使用 `AddForce`

2. **缺少提前退出机制**
   - 冲刺状态应该在碰到墙壁时立即退出
   - 当前只有时间到期才退出

---

## 问题2：受伤后移动抖动

### 根本原因

#### 1. **Idle和Move状态频繁切换（核心问题）**

```csharp
// PlayerState_Grounded.cs - OnUpdate()
Vector2 input = InputManager.Instance.MoveInput;
if (input.x == 0)
{ 
    hfsm.SwitchState(E_PlayerStateType.Idle);  // ❌ 问题所在
}
```

**问题分析：**
- `Grounded` 状态的 `OnUpdate()` **每帧检查输入**
- 当 `input.x == 0` 时切换到 `Idle`
- 但 `Idle` 状态的 `OnUpdate()` 又检查 `input.x != 0` 切回 `Move`
- **形成死循环：Grounded → Idle → Move → Grounded → Idle → ...**

#### 2. **Idle状态强制清零速度**

```csharp
// PlayerState_Idle.cs - OnEnter()
public override void OnEnter()
{
    owner.Rb.velocity = Vector2.zero;  // ❌ 每次进入都清零
}
```

**抖动机制：**
1. 玩家在 `Move` 状态移动
2. 输入瞬间为0（手柄摇杆回中、键盘松开瞬间）
3. 切换到 `Idle`，速度被清零
4. 下一帧输入又不为0（摇杆抖动、按键抖动）
5. 切换回 `Move`，重新施加力
6. **循环往复 → 视觉上表现为剧烈抖动**

#### 3. **受伤后为什么更明显？**

```csharp
// PlayerState_Heart.cs - OnEnter()
Time.timeScale = owner.HurtTimeScale;  // 0.05
Time.fixedDeltaTime = 0.02f * Time.timeScale;  // 0.001

// OnUpdate() - 恢复阶段
Time.timeScale = 1f;
Time.fixedDeltaTime = 0.02f;
```

**关键问题：**
- 受伤时修改了 `Time.fixedDeltaTime`
- 虽然在 `OnExit()` 中恢复，但**可能存在时序问题**
- `FixedUpdate` 的调用频率可能在短时间内不稳定
- 导致物理计算和状态切换不同步，放大了抖动效果

#### 4. **状态切换逻辑缺陷**

**当前逻辑：**
```
Grounded.OnUpdate() 每帧执行
  ↓
检查 input.x == 0
  ↓
切换到 Idle（触发 OnEnter，清零速度）
  ↓
Idle.OnUpdate() 执行
  ↓
检查 input.x != 0
  ↓
切换到 Move
  ↓
Move.OnFixedUpdate() 施加力
  ↓
回到 Grounded.OnUpdate()
```

**问题：**
- `Grounded` 是父状态，它的 `OnUpdate()` **总是执行**
- 即使当前在 `Move` 子状态，`Grounded.OnUpdate()` 仍然会检查输入
- 导致 `Idle` 和 `Move` 之间疯狂切换

#### 5. **缺少输入死区（Dead Zone）**

```csharp
// 当前代码
if (input.x == 0)  // ❌ 精确比较
    hfsm.SwitchState(E_PlayerStateType.Idle);
```

**问题：**
- 手柄摇杆即使在中心位置，也可能输出微小的非零值（如 0.001）
- 导致永远不会真正进入 `Idle`，或频繁切换
- 应该使用死区阈值：`if (Mathf.Abs(input.x) < 0.1f)`

---

## 详细问题定位

### 问题1的触发条件
1. 玩家在地面或空中按下冲刺键
2. 冲刺方向有墙壁
3. 冲刺速度过快（20f）+ 墙壁碰撞体过薄
4. 物理引擎的碰撞响应被每帧的速度覆盖

### 问题2的触发条件
1. 玩家受到伤害，进入 `Heart` 状态
2. `Heart` 状态修改 `Time.timeScale` 和 `Time.fixedDeltaTime`
3. 恢复后切换到 `Grounded` 状态
4. 玩家尝试移动，触发 `Idle` ↔ `Move` 频繁切换
5. 每次切换 `Idle` 都清零速度，导致抖动

---

## 修复建议（不修改代码，仅配置）

### 问题1：冲刺穿墙

#### Unity场景配置修复：

1. **修改玩家 Rigidbody2D**
   - 选中玩家物体
   - Inspector → Rigidbody2D
   - Collision Detection: 改为 **Continuous**

2. **增加墙壁碰撞体厚度**
   - 选中墙壁物体
   - Inspector → BoxCollider2D（或其他碰撞体）
   - 调整 Size，确保厚度 ≥ 0.5

3. **检查Layer设置**
   - 选中玩家物体，查看 Inspector 中的 `wallLayer` 字段
   - 确认墙壁物体的 Layer 与此匹配
   - Edit → Project Settings → Physics 2D → Layer Collision Matrix
   - 确认玩家Layer和墙壁Layer之间的碰撞勾选

4. **调整冲刺参数**
   - 选中玩家物体
   - Inspector → Player Controller
   - 降低 `Dash Speed`：从 20 改为 **15** 或 **12**
   - 增加 `Wall Check Distance`：从 0.5 改为 **0.8** 或 **1.0**

### 问题2：受伤后移动抖动

#### Unity场景配置修复：

1. **调整输入死区（如果使用手柄）**
   - Edit → Project Settings → Input Manager
   - 找到 Horizontal 轴
   - Dead: 从 0.001 改为 **0.15** 或 **0.2**

2. **调整受伤参数**
   - 选中玩家物体
   - Inspector → Player Controller
   - `Hurt Time Scale`：从 0.05 改为 **0.1** 或 **0.2**（减少时间缩放幅度）
   - `Hit Stop Duration`：从 0.4 改为 **0.2** 或 **0.3**（缩短顿帧时间）

3. **调整移动参数**
   - `Move Acceleration`：从 50 改为 **30** 或 **40**（降低加速度，减少抖动）
   - `Move Speed`：保持不变或略微降低

4. **Rigidbody2D 插值设置**
   - 选中玩家物体
   - Inspector → Rigidbody2D
   - Interpolate: 改为 **Interpolate**（平滑移动）

---

## 代码层面的根本问题（仅分析，不修改）

### 问题1的代码缺陷

1. **速度覆盖问题**
   ```csharp
   // 每帧强制设置速度，覆盖物理引擎的碰撞响应
   owner.Rb.velocity = dashDir * owner.DashSpeed;
   ```
   - 应该只在 `OnEnter` 设置一次
   - 或者使用 `MovePosition` 代替 `velocity`

2. **缺少墙壁检测**
   ```csharp
   // 应该在 OnUpdate 或 OnFixedUpdate 中添加
   if (owner.IsWallAhead())
   {
       hfsm.SwitchState(E_PlayerStateType.Grounded);
       return;
   }
   ```

### 问题2的代码缺陷

1. **状态切换逻辑错误**
   ```csharp
   // Grounded.OnUpdate() 不应该管理 Idle/Move 切换
   // 应该由 Idle 和 Move 自己决定何时切换
   ```

2. **缺少输入死区**
   ```csharp
   // 应该使用阈值判断
   if (Mathf.Abs(input.x) < 0.1f)
       hfsm.SwitchState(E_PlayerStateType.Idle);
   ```

3. **Idle状态不应该清零速度**
   ```csharp
   // OnEnter() 中清零速度会导致急停
   // 应该让物理引擎自然减速，或使用阻尼
   ```

4. **Time.fixedDeltaTime 修改有风险**
   ```csharp
   // 修改全局时间会影响所有物理计算
   // 应该只修改 Time.timeScale，不修改 fixedDeltaTime
   // 或者使用局部的时间缩放方案
   ```

---

## 测试验证步骤

### 验证问题1修复

1. 将玩家 Rigidbody2D 改为 Continuous
2. 降低 Dash Speed 到 15
3. 增加墙壁碰撞体厚度到 0.5
4. 测试：在墙壁前冲刺，观察是否还会穿墙

### 验证问题2修复

1. 将 Rigidbody2D Interpolate 改为 Interpolate
2. 降低 Move Acceleration 到 30
3. 增加输入死区到 0.15
4. 降低 Hurt Time Scale 到 0.1
5. 测试：受伤后移动，观察是否还有抖动

---

## 总结

### 问题1：冲刺穿墙
- **主因**：每帧强制设置速度 + 缺少墙壁检测
- **配置修复**：Continuous碰撞检测 + 降低速度 + 增加墙壁厚度
- **代码缺陷**：速度覆盖物理引擎响应

### 问题2：受伤后移动抖动
- **主因**：Idle/Move频繁切换 + Idle清零速度 + 缺少输入死区
- **配置修复**：Interpolate插值 + 降低加速度 + 增加输入死区
- **代码缺陷**：状态切换逻辑错误 + Time.fixedDeltaTime修改有风险

两个问题都可以通过**Unity场景配置调整**来缓解，但要彻底解决需要修改代码逻辑。
