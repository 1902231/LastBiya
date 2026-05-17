using UnityEngine;
using System.Collections;

/// <summary>
/// Boss 战斗管理器（单例）
/// 从触发器读取配置，协调 Boss 战流程
/// </summary>
public class BossBattleManager : MonoBehaviour
{
    public static BossBattleManager Instance { get; private set; }
    
    [Header("调试")]
    [SerializeField] private bool enableDebugLog = true;
    
    private IBoss currentBoss;
    private BossBattleTrigger currentTrigger;
    private bool battleInProgress = false;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        // 监听 Boss 战触发事件（接收触发器引用）
        EventCenter.Instance.AddEventListener<BossBattleTrigger>("StartBossBattle", OnBossBattleTriggered);
        
        // 监听单位血量变化
        EventCenter.Instance.AddEventListener<IUnit>("HPChanged", OnUnitHPChanged);
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            EventCenter.Instance.RemoveEventListener<BossBattleTrigger>("StartBossBattle", OnBossBattleTriggered);
            EventCenter.Instance.RemoveEventListener<IUnit>("HPChanged", OnUnitHPChanged);
        }
    }

    /// <summary>
    /// Boss 战触发（接收触发器引用）
    /// </summary>
    private void OnBossBattleTriggered(BossBattleTrigger trigger)
    {
        if (battleInProgress)
        {
            DebugLog("Boss 战已在进行中！");
            return;
        }
        
        currentTrigger = trigger;
        currentBoss = trigger.Boss;
        
        if (currentBoss == null)
        {
            Debug.LogError("触发器中的 Boss 为空！");
            return;
        }
        
        StartCoroutine(StartBossBattle());
    }

    /// <summary>
    /// 开始 Boss 战序列
    /// </summary>
    private IEnumerator StartBossBattle()
    {
        battleInProgress = true;
        DebugLog($"[Boss 战] 开始：{currentBoss.BossID}");
        
        // ===== 阶段 1：准备阶段 =====
        PreparePhase();
        
        // ===== 阶段 2：开场动画 =====
        float introDuration = currentBoss.PlayIntroAnimation();
        yield return new WaitForSeconds(introDuration);
        
        // ===== 阶段 3：相机聚焦（三个子阶段）=====
        yield return StartCoroutine(CameraFocusPhase());
        
        // ===== 阶段 4：UI 显示（血条）=====
        ShowUIPhase();
        
        // ===== 阶段 5：启动战斗 =====
        StartCombatPhase();
    }

    /// <summary>
    /// 阶段 1：准备阶段
    /// </summary>
    private void PreparePhase()
    {
        DebugLog("[Boss 战] 准备阶段");
        
        // 触发禁用玩家输入事件
        EventCenter.Instance.EventTrigger("DisablePlayerInput");
        
        // 锁定房间门
        if (currentTrigger.RoomDoors != null)
        {
            foreach (var door in currentTrigger.RoomDoors)
            {
                if (door != null)
                    door.SetActive(true);
            }
        }
        
        // 播放开场音效
        if (currentTrigger.IntroSFX != null)
        {
            AudioManagerTest.Instance?.PlaySFX(currentTrigger.IntroSFX);
        }
    }

    /// <summary>
    /// 阶段 3：相机聚焦（三个子阶段）
    /// </summary>
    private IEnumerator CameraFocusPhase()
    {
        // 3.1 聚焦到目标
        DebugLog("[Boss 战] 相机聚焦到目标");
        
        if (CameraController.Instance != null)
        {
            CameraController.Instance.FocusAndZoom(
                currentTrigger.CameraFocusPoint,
                currentTrigger.CameraZoomSize,
                currentTrigger.CameraZoomDuration
            );
            
            // 等待聚焦完成
            yield return new WaitForSeconds(currentTrigger.CameraZoomDuration);
        }
        
        // 3.2 停留展示（显示 Boss 名称）
        DebugLog("[Boss 战] 显示 Boss 名称");
        
        // 显示 Boss 标题
        EventCenter.Instance.EventTrigger<string>("ShowBossTitle", currentBoss.UnitName);
        
        // 停留一段时间
        yield return new WaitForSeconds(currentTrigger.BossNameDisplayDuration);
        
        // 隐藏 Boss 标题
        EventCenter.Instance.EventTrigger("HideBossTitle");
        
        // 等待淡出动画完成（假设淡出时长为 0.5 秒）
        yield return new WaitForSeconds(0.5f);
        
        // 3.3 恢复相机
        DebugLog("[Boss 战] 相机恢复跟随玩家");
        
        if (CameraController.Instance != null)
        {
            CameraController.Instance.ResetCamera(1f);
            
            // 等待恢复完成
            yield return new WaitForSeconds(1f);
        }
    }

    /// <summary>
    /// 阶段 4：UI 显示（血条）
    /// </summary>
    private void ShowUIPhase()
    {
        DebugLog("[Boss 战] 显示 Boss 血条");
        
        // 触发开场完成事件，BossUIController 显示血条
        EventCenter.Instance.EventTrigger<IUnit>("BossIntroComplete", currentBoss as IUnit);
    }

    /// <summary>
    /// 阶段 5：启动战斗
    /// </summary>
    private void StartCombatPhase()
    {
        DebugLog("[Boss 战] 战斗开始");
        
        // 启动 Boss 战斗逻辑
        currentBoss.StartCombat();
        
        // 播放 Boss 战音乐
        if (currentTrigger.BattleMusic != null)
        {
            AudioManagerTest.Instance?.PlayMusic(currentTrigger.BattleMusic);
        }
        
        // 触发启用玩家输入事件
        EventCenter.Instance.EventTrigger("EnablePlayerInput");
    }

    /// <summary>
    /// 监听单位血量变化（检测 Boss/玩家死亡）
    /// </summary>
    private void OnUnitHPChanged(IUnit unit)
    {
        if (!battleInProgress) return;
        
        // 检测 Boss 死亡
        if (unit == currentBoss && currentBoss.IsDead)
        {
            OnBossDefeated();
        }
        
        // 检测玩家死亡
        if (unit.UnitName == "Player" && unit.CurrentHP <= 0)
        {
            OnPlayerDefeated();
        }
    }

    /// <summary>
    /// Boss 被击败
    /// </summary>
    private void OnBossDefeated()
    {
        DebugLog("[Boss 战] Boss 被击败");
        StartCoroutine(BossDefeatedSequence());
    }

    /// <summary>
    /// Boss 被击败序列
    /// </summary>
    private IEnumerator BossDefeatedSequence()
    {
        // 停止 Boss 战斗逻辑
        currentBoss.StopCombat();
        
        // 等待死亡动画
        yield return new WaitForSeconds(2f);
        
        // 隐藏 Boss UI
        EventCenter.Instance.EventTrigger<IUnit>("BossExited", currentBoss as IUnit);
        
        // 解锁房间门
        if (currentTrigger.RoomDoors != null)
        {
            foreach (var door in currentTrigger.RoomDoors)
            {
                if (door != null)
                    door.SetActive(false);
            }
        }
        
        // 停止 Boss 战音乐
        AudioManagerTest.Instance?.StopMusic();
        
        battleInProgress = false;
        DebugLog("[Boss 战] 结束");
    }

    /// <summary>
    /// 玩家被击败
    /// </summary>
    private void OnPlayerDefeated()
    {
        DebugLog("[Boss 战] 玩家被击败");
        StartCoroutine(PlayerDefeatedSequence());
    }

    /// <summary>
    /// 玩家被击败序列
    /// </summary>
    private IEnumerator PlayerDefeatedSequence()
    {
        // 停止 Boss 战斗逻辑
        currentBoss.StopCombat();
        
        // 等待玩家死亡动画
        yield return new WaitForSeconds(2f);
        
        // 隐藏 Boss UI
        EventCenter.Instance.EventTrigger<IUnit>("BossExited", currentBoss as IUnit);
        
        // 显示游戏结束界面
        // TODO: 集成 UIManager 后取消注释
        // UIManager.Instance?.ShowGameOverPanel();
        
        battleInProgress = false;
        DebugLog("[Boss 战] 玩家失败");
    }

    private void DebugLog(string message)
    {
        if (enableDebugLog)
            Debug.Log(message);
    }
}
