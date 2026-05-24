# Boss UI 显示逻辑修复

**问题发现时间**: 2026-05-12  
**修复状态**: ✅ 已修复

---

## 问题描述

### 原始问题

之前的实现中，`BossUI` 整个 GameObject 在游戏开始时被设置为 `SetActive(false)`，导致：

```
BossUI (SetActive = false)  ← 父物体隐藏
└── BossTitleGroup          ← 子物体也被隐藏
    └── BossTitleText
```

**问题**：
- 当触发 `"ShowBossTitle"` 事件时
- `BossTitleGroup.SetActive(true)` 被调用
- 但因为父物体 `BossUI` 是 `SetActive(false)`
- 所以 `BossTitleGroup` 仍然不可见
- **Boss 标题无法在开场动画时显示！**

---

## 解决方案

### 修改策略

**不再隐藏整个 BossUI GameObject**，而是：
1. 保持 `BossUI` GameObject 始终激活
2. 分别控制血条、架势条、名称的显示/隐藏
3. Boss 标题独立控制（通过 CanvasGroup）

---

## 修改内容

### 1. `BossUIView.cs` - 新增方法

```csharp
/// <summary>
/// 显示血条和架势条（不包括标题）
/// </summary>
public void ShowHealthBars()
{
    if (bossHp != null) bossHp.transform.parent.gameObject.SetActive(true);
    if (bossPosture != null) bossPosture.transform.parent.gameObject.SetActive(true);
    if (bossNameText != null) bossNameText.gameObject.SetActive(true);
}

/// <summary>
/// 隐藏血条和架势条（不包括标题）
/// </summary>
public void HideHealthBars()
{
    if (bossHp != null) bossHp.transform.parent.gameObject.SetActive(false);
    if (bossPosture != null) bossPosture.transform.parent.gameObject.SetActive(false);
    if (bossNameText != null) bossNameText.gameObject.SetActive(false);
}
```

### 2. `BossUIController.cs` - 修改显示/隐藏逻辑

**修改前：**
```csharp
void Start()
{
    // ...
    gameObject.SetActive(false);  // ❌ 隐藏整个 GameObject
}

public void BindBoss(IUnit boss)
{
    // ...
    gameObject.SetActive(true);  // ❌ 显示整个 GameObject
}

public void UnbindBoss()
{
    // ...
    gameObject.SetActive(false);  // ❌ 隐藏整个 GameObject
}
```

**修改后：**
```csharp
void Start()
{
    // ...
    view.HideHealthBars();  // ✅ 只隐藏血条和架势条
}

public void BindBoss(IUnit boss)
{
    // ...
    view.ShowHealthBars();  // ✅ 只显示血条和架势条
}

public void UnbindBoss()
{
    // ...
    view.HideHealthBars();  // ✅ 只隐藏血条和架势条
}
```

---

## 修复后的流程

### 完整时间线

```
游戏开始
    ↓
BossUI GameObject: 激活 ✓
├── HealthBar: 隐藏 ✗
├── PostureBar: 隐藏 ✗
├── BossNameText: 隐藏 ✗
└── BossTitleGroup: 隐藏 ✗
    ↓
玩家进入触发器
    ↓
开场动画播放
    ↓
相机聚焦到 Boss
    ↓
触发 "ShowBossTitle" 事件
    ↓
BossUI GameObject: 激活 ✓  ← 父物体是激活的！
├── HealthBar: 隐藏 ✗
├── PostureBar: 隐藏 ✗
├── BossNameText: 隐藏 ✗
└── BossTitleGroup: 显示 ✓  ← Boss 标题可以显示了！
    └── BossTitleText: "Boss01" (淡入)
    ↓
停留展示
    ↓
触发 "HideBossTitle" 事件
    ↓
BossTitleGroup: 隐藏 ✗ (淡出)
    ↓
相机恢复
    ↓
触发 "BossIntroComplete" 事件
    ↓
BossUI GameObject: 激活 ✓
├── HealthBar: 显示 ✓  ← 血条显示
├── PostureBar: 显示 ✓  ← 架势条显示
├── BossNameText: 显示 ✓  ← 名称显示
└── BossTitleGroup: 隐藏 ✗
    ↓
Boss 战进行中...
    ↓
Boss 被击败
    ↓
触发 "BossExited" 事件
    ↓
BossUI GameObject: 激活 ✓
├── HealthBar: 隐藏 ✗  ← 血条隐藏
├── PostureBar: 隐藏 ✗  ← 架势条隐藏
├── BossNameText: 隐藏 ✗  ← 名称隐藏
└── BossTitleGroup: 隐藏 ✗
```

---

## Unity 场景设置要求

### BossUI 层级结构

```
Canvas (Persistent 场景)
└── BossUI (GameObject, 始终激活)
    ├── BossUIController (脚本)
    ├── HealthBarGroup (GameObject, 初始隐藏)
    │   ├── BossHp (Image)
    │   └── BossHpBuffer (Image)
    ├── PostureBarGroup (GameObject, 初始隐藏)
    │   ├── BossPosture (Image)
    │   └── BossPostureBuffer (Image)
    ├── BossNameText (TextMeshProUGUI, 初始隐藏)
    └── BossTitleGroup (GameObject, 初始隐藏)
        ├── CanvasGroup (组件)
        └── BossTitleText (TextMeshProUGUI)
```

### 重要设置

1. **BossUI GameObject**：
   - ✅ 始终保持激活（勾选）
   - 不要在运行时设置为 `SetActive(false)`

2. **HealthBarGroup / PostureBarGroup**：
   - ✗ 初始设置为未激活（取消勾选）
   - 由脚本控制显示/隐藏

3. **BossNameText**：
   - ✗ 初始设置为未激活（取消勾选）
   - 由脚本控制显示/隐藏

4. **BossTitleGroup**：
   - ✗ 初始设置为未激活（取消勾选）
   - 由脚本通过 CanvasGroup 控制淡入淡出

---

## 关键改进

### 优点

1. ✅ **Boss 标题可以正常显示**
   - 父物体始终激活，子物体可以独立控制

2. ✅ **更精细的控制**
   - 血条、架势条、名称、标题分别控制
   - 可以实现更复杂的显示逻辑

3. ✅ **向后兼容**
   - 外部调用接口不变
   - 只是内部实现改变

### 注意事项

1. **层级结构要求**：
   - 血条和架势条需要有父物体（HealthBarGroup / PostureBarGroup）
   - 代码通过 `bossHp.transform.parent` 访问父物体

2. **初始状态**：
   - 在 Unity 中手动设置各个组件的初始激活状态
   - 或者让脚本在 Start 时统一设置

---

## 测试清单

### 功能测试

- [ ] 游戏开始时，血条、架势条、名称都不可见
- [ ] 进入 Boss 战时，Boss 标题可以正常淡入显示
- [ ] Boss 标题停留后可以正常淡出隐藏
- [ ] 开场动画完成后，血条、架势条、名称正常显示
- [ ] Boss 战结束时，血条、架势条、名称正常隐藏
- [ ] Boss 标题在战斗中不会显示

### 边界测试

- [ ] 快速进入/退出 Boss 战不会出现 UI 错乱
- [ ] 多次触发 Boss 战 UI 显示/隐藏正常
- [ ] 在 Boss 战中暂停游戏 UI 状态正常

---

## 相关文件

- `LastBiya/Assets/Scripts/UI/Game/BossUIView.cs` - 新增 ShowHealthBars/HideHealthBars 方法
- `LastBiya/Assets/Scripts/UI/Game/BossUIController.cs` - 修改显示/隐藏逻辑
- `LastBiya/Assets/Scripts/Boss/BossBattleManager.cs` - 无需修改

---

**修复完成时间**: 2026-05-12  
**修复者**: Kiro AI Assistant
