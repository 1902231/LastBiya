# 测试音频管理器 - 设置指南

## 概述

`AudioManagerTest` 是一个简单的测试用音频管理器，提供基础的音乐和音效播放功能。

## 功能特性

- ✅ 单例模式
- ✅ 音乐播放（带淡入淡出）
- ✅ 音效播放
- ✅ 音量控制
- ✅ 静音功能
- ✅ 自动创建 AudioSource

## 场景设置

### 方法1：自动创建（推荐）

1. 在 Persistent 场景中创建空物体 "AudioManagerTest"
2. 添加 `AudioManagerTest.cs` 脚本
3. 完成！AudioSource 会自动创建

### 方法2：手动配置

1. 在 Persistent 场景中创建空物体 "AudioManagerTest"
2. 添加 `AudioManagerTest.cs` 脚本
3. 创建两个子物体：
   - "MusicSource" - 添加 AudioSource 组件
   - "SFXSource" - 添加 AudioSource 组件
4. 将两个 AudioSource 拖拽到脚本的对应字段

### Inspector 配置

```
AudioManagerTest:
├── Music Source: (自动创建或手动拖拽)
├── SFX Source: (自动创建或手动拖拽)
├── Music Volume: 0.7 (音乐音量)
├── SFX Volume: 1.0 (音效音量)
└── Music Fade Duration: 1.0 (淡入淡出时长)
```

## 使用示例

### 播放音乐
```csharp
// 带淡入效果
AudioManagerTest.Instance.PlayMusic(bossBattleMusic);

// 不带淡入效果
AudioManagerTest.Instance.PlayMusic(bossBattleMusic, fadeIn: false);
```

### 停止音乐
```csharp
// 带淡出效果
AudioManagerTest.Instance.StopMusic();

// 不带淡出效果
AudioManagerTest.Instance.StopMusic(fadeOut: false);
```

### 播放音效
```csharp
// 默认音量
AudioManagerTest.Instance.PlaySFX(explosionSFX);

// 自定义音量（0-1）
AudioManagerTest.Instance.PlaySFX(explosionSFX, volumeScale: 0.5f);
```

### 暂停/恢复音乐
```csharp
AudioManagerTest.Instance.PauseMusic();
AudioManagerTest.Instance.ResumeMusic();
```

### 调整音量
```csharp
AudioManagerTest.Instance.SetMusicVolume(0.5f);
AudioManagerTest.Instance.SetSFXVolume(0.8f);
```

### 静音
```csharp
AudioManagerTest.Instance.SetMute(true);  // 静音
AudioManagerTest.Instance.SetMute(false); // 取消静音
```

## Boss 战中的使用

Boss 战系统已经集成了 `AudioManagerTest`：

1. **开场音效**：在准备阶段播放
2. **Boss 战音乐**：在战斗开始时播放（带淡入）
3. **停止音乐**：在 Boss 被击败时停止（带淡出）

### 在 BossBattleTrigger 中配置

```
BossBattleTrigger:
├── Battle Music: 拖拽 Boss 战音乐 AudioClip
└── Intro SFX: 拖拽开场音效 AudioClip
```

## 调试日志

AudioManagerTest 会在控制台输出日志：

```
AudioManagerTest: 播放音乐 'BossBattleTheme'
AudioManagerTest: 播放音效 'BossRoar'
AudioManagerTest: 停止音乐
```

## 注意事项

### ⚠️ 这是测试版本

- 仅用于开发测试
- 功能较为基础
- 正式版需要更完善的功能：
  - 音频池管理
  - 多音轨支持
  - 音频混合器集成
  - 3D 音效支持
  - 音频资源预加载

### ✅ 适用场景

- 快速原型开发
- 功能测试
- Demo 演示

## 迁移到正式版

当你有正式的 AudioManager 时，只需：

1. 将 `AudioManagerTest.Instance` 替换为 `AudioManager.Instance`
2. 确保接口一致（`PlayMusic`, `PlaySFX`, `StopMusic`）
3. 删除 `AudioManagerTest.cs`

## 常见问题

### Q1: 音乐没有播放？
**A:** 检查：
1. AudioManagerTest 是否在 Persistent 场景中
2. AudioClip 是否拖拽到 BossBattleTrigger
3. 控制台是否有 "播放音乐" 日志
4. AudioSource 的 Mute 是否勾选

### Q2: 音效听不到？
**A:** 检查：
1. SFX Volume 是否为 0
2. AudioClip 是否为空
3. 音效是否太短（被淹没在音乐中）

### Q3: 音乐切换不平滑？
**A:** 调整 `Music Fade Duration`（建议 1-2 秒）

### Q4: 音乐重复播放？
**A:** AudioManagerTest 会自动检测，如果正在播放相同的音乐，不会重复播放。

## 扩展功能（可选）

### 添加音乐循环点
```csharp
public void PlayMusicWithLoop(AudioClip clip, float loopStartTime)
{
    // 实现音乐循环点逻辑
}
```

### 添加音频淡入淡出交叉
```csharp
public void CrossfadeMusic(AudioClip newClip, float duration)
{
    // 实现交叉淡入淡出
}
```

### 添加音频优先级
```csharp
public void PlaySFXWithPriority(AudioClip clip, int priority)
{
    // 实现音效优先级管理
}
```
