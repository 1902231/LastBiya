# Boss 战斗系统 - Unity 设置清单

**快速设置指南** - 按照此清单在 Unity 中完成设置

---

## ✅ 第一步：BossUI 层级结构修改

### 在 Persistent 场景的 Canvas 下找到 BossUI

**当前结构：**
```
Canvas
└── BossUI
    ├── BossUIController (脚本)
    ├── HealthBar
    ├── PostureBar
    └── BossNameText
```

**需要添加：**
```
Canvas
└── BossUI
    ├── BossUIController (脚本)
    ├── HealthBar
    ├── PostureBar
    ├── BossNameText (小字，常驻显示)
    └── BossTitleGroup (新建！)  ← 添加这个
        ├── CanvasGroup (组件)
        └── BossTitleText (大字，开场显示)
```

### 详细步骤：

1. **创建 BossTitleGroup**
   - 右键 BossUI → Create Empty
   - 命名为 "BossTitleGroup"
   - 添加组件：`Canvas Group`

2. **创建 BossTitleText**
   - 右键 BossTitleGroup → UI → Text - TextMeshPro
   - 命名为 "BossTitleText"
   - 设置字体大小：**80-100**（大字）
   - 设置对齐方式：**居中**
   - 设置颜色：**白色或金色**
   - 设置文本：`"Boss Name"` (占位符)

3. **配置 BossTitleGroup**
   - 设置 RectTransform：
     - Anchor: Center
     - Position: (0, 100, 0) 或根据需要调整
     - Width: 800
     - Height: 200
   - 设置 Canvas Group：
     - Alpha: 1
     - Interactable: 不勾选
     - Block Raycasts: 不勾选

4. **初始隐藏 BossTitleGroup**
   - 取消勾选 BossTitleGroup 的 Active（或保持勾选，脚本会控制）

---

## ✅ 第二步：配置 BossUIView 组件

在 BossUI 物体上找到 `BossUIView` 组件，添加新字段的引用：

### Inspector 配置：

**Boss 标题（开场大字）：**
- `Boss Title Text`：拖拽 **BossTitleText** (TextMeshProUGUI)
- `Title Canvas Group`：拖拽 **BossTitleGroup** (CanvasGroup)
- `Title Fade In Duration`：**0.8** (淡入时长)
- `Title Fade Out Duration`：**0.5** (淡出时长)

**其他字段保持不变：**
- Boss Hp
- Boss Hp Buffer
- Boss Posture
- Boss Posture Buffer
- Boss Name Text (小字)
- Buffer Delay
- Buffer Chase Speed

---

## ✅ 第三步：配置 BossBattleTrigger

在 Boss 房间场景中找到 `BossTrigger` 物体，添加新配置：

### Inspector 配置：

**UI 配置（新增）：**
- `Boss Name Display Duration`：**2** (Boss 名称显示时长，秒)

**其他配置保持不变：**
- Boss Object
- Room Doors
- Camera Focus Point
- Camera Zoom Size
- Camera Zoom Duration
- Battle Music
- Intro SFX
- Trigger Once

---

## ✅ 第四步：测试流程

### 1. 进入 Play 模式
- 确保 Persistent 场景和 Boss 房间场景都已加载

### 2. 触发 Boss 战
- 控制玩家进入 BossTrigger

### 3. 观察序列（应该按顺序发生）：
1. ✅ 房间门关闭
2. ✅ 播放开场音效
3. ✅ Boss 播放开场动画
4. ✅ 相机聚焦到 Boss（平滑缩放）
5. ✅ **Boss 名称淡入显示**（大字）
6. ✅ 停留 2 秒
7. ✅ **Boss 名称淡出隐藏**
8. ✅ 相机恢复跟随玩家
9. ✅ **Boss 血条显示**（小字名称 + 血条 + 架势条）
10. ✅ Boss 开始战斗（行为树启动）
11. ✅ 播放 Boss 战音乐

### 4. 检查控制台日志
应该看到以下日志（如果启用了 Debug Log）：
```
[Boss 战] 开始：Boss01
[Boss 战] 准备阶段
[Boss 战] 相机聚焦到目标
[Boss 战] 显示 Boss 名称
[Boss 战] 相机恢复跟随玩家
[Boss 战] 显示 Boss 血条
[Boss 战] 战斗开始
```

---

## ✅ 第五步：调整效果（可选）

### 调整 Boss 标题样式
在 BossTitleText 上：
- 字体：选择更有冲击力的字体
- 大小：80-120
- 颜色：白色、金色、红色等
- 添加 Outline（描边）：黑色，Size: 5
- 添加 Shadow（阴影）：增强立体感

### 调整显示时长
在 BossBattleTrigger 上：
- `Boss Name Display Duration`：
  - 2 秒（默认）
  - 3 秒（更长展示）
  - 1.5 秒（快速展示）

### 调整淡入淡出速度
在 BossUIView 上：
- `Title Fade In Duration`：
  - 0.8 秒（默认）
  - 1.2 秒（更慢，更庄重）
  - 0.5 秒（更快）
- `Title Fade Out Duration`：
  - 0.5 秒（默认）
  - 0.3 秒（更快消失）

---

## 🐛 常见问题排查

### 问题 1：Boss 标题没有显示
**检查：**
- [ ] BossTitleGroup 是否在 BossUI 下
- [ ] BossUIView 的 `Boss Title Text` 和 `Title Canvas Group` 是否拖拽正确
- [ ] 控制台是否有 `"BossUIView: Boss 标题组件未设置！"` 警告

### 问题 2：Boss 标题显示但没有淡入效果
**检查：**
- [ ] DOTween 是否已导入项目
- [ ] 控制台是否有 DOTween 相关错误
- [ ] Canvas Group 的 Alpha 是否被其他脚本控制

### 问题 3：Boss 血条在开场动画前就显示了
**检查：**
- [ ] BossUIController 是否监听了 `"BossIntroComplete"` 事件（不是 `"BossEntered"`）
- [ ] BossUI 物体在 Start 时是否设置为 `SetActive(false)`

### 问题 4：相机聚焦后没有恢复
**检查：**
- [ ] CameraController 是否在 Persistent 场景中
- [ ] `ResetCamera()` 方法是否被调用
- [ ] 控制台是否有相关错误

---

## 📋 完成确认

完成以下所有项后，Boss 战斗系统即可正常工作：

- [ ] BossTitleGroup 和 BossTitleText 已创建
- [ ] BossUIView 的新字段已配置
- [ ] BossBattleTrigger 的 Boss Name Display Duration 已设置
- [ ] 测试流程全部通过
- [ ] Boss 标题显示效果满意
- [ ] 无控制台错误

---

**设置完成后，即可开始制作更多 Boss！**

每个新 Boss 只需：
1. 实现 `IBoss` 接口
2. 创建 BossBattleTrigger
3. 拖拽引用

UI 和相机效果会自动复用！
