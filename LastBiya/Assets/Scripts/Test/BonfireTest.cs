using UnityEngine;

/// <summary>
/// 篝火系统测试脚本
/// 功能：
/// 1. 当玩家死亡时，让玩家回到篝火位置
/// 2. 玩家血量回满
/// 3. Boss血量和架势条回满
/// 4. 恢复药水的使用次数
/// </summary>
public class BonfireTest : MonoBehaviour
{
    [Header("篝火配置")]
    [Tooltip("篝火位置（玩家复活点）")]
    public Transform bonfirePosition;

    [Header("引用")]
    [Tooltip("玩家控制器引用，留空则自动查找")]
    public PlayerController player;

    [Tooltip("Boss控制器引用，留空则自动查找")]
    public Boss01 boss;

    [Tooltip("药水测试脚本引用，留空则自动查找")]
    public PotionTest potionTest;

    [Header("调试")]
    [Tooltip("是否启用调试日志")]
    public bool enableDebugLog = true;

    private bool isPlayerDead = false;

    void Start()
    {
        // 自动查找玩家
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player")?.GetComponent<PlayerController>();
            if (player == null)
            {
                Debug.LogError("[BonfireTest] 未找到玩家对象！请确保玩家对象有 'Player' 标签。");
            }
        }

        // 自动查找Boss
        if (boss == null)
        {
            boss = FindObjectOfType<Boss01>();
            if (boss == null)
            {
                Debug.LogWarning("[BonfireTest] 未找到Boss对象！Boss血量恢复功能将不可用。");
            }
        }

        // 自动查找药水测试脚本
        if (potionTest == null)
        {
            potionTest = FindObjectOfType<PotionTest>();
            if (potionTest == null)
            {
                Debug.LogWarning("[BonfireTest] 未找到PotionTest脚本！药水恢复功能将不可用。");
            }
        }

        // 如果没有设置篝火位置，使用当前物体位置
        if (bonfirePosition == null)
        {
            bonfirePosition = transform;
            if (enableDebugLog)
                Debug.Log("[BonfireTest] 未设置篝火位置，使用当前物体位置作为复活点。");
        }

        // 监听玩家受伤事件
        EventCenter.Instance.AddEventListener<DamageInfo>("PlayerHurt", OnPlayerHurt);

        if (enableDebugLog)
            Debug.Log($"[BonfireTest] 篝火系统初始化完成，复活点位置：{bonfirePosition.position}");
    }

    void OnDestroy()
    {
        // 取消事件监听
        EventCenter.Instance.RemoveEventListener<DamageInfo>("PlayerHurt", OnPlayerHurt);
    }

    /// <summary>
    /// 玩家受伤事件回调
    /// </summary>
    private void OnPlayerHurt(DamageInfo damageInfo)
    {
        if (player == null) return;

        // 检查玩家是否死亡
        if (player.currentHP <= 0 && !isPlayerDead)
        {
            isPlayerDead = true;
            if (enableDebugLog)
                Debug.Log("[BonfireTest] 检测到玩家死亡，准备复活...");

            // 延迟一帧执行复活，避免在事件处理中修改状态
            Invoke(nameof(RespawnPlayer), 0.1f);
        }
    }

    /// <summary>
    /// 复活玩家
    /// </summary>
    private void RespawnPlayer()
    {
        if (player == null)
        {
            Debug.LogError("[BonfireTest] 玩家引用丢失，无法复活！");
            return;
        }

        if (enableDebugLog)
            Debug.Log("[BonfireTest] 开始复活玩家...");

        // 1. 将玩家传送到篝火位置
        player.transform.position = bonfirePosition.position;
        if (enableDebugLog)
            Debug.Log($"[BonfireTest] 玩家已传送到篝火位置：{bonfirePosition.position}");

        // 2. 玩家血量回满
        player.currentHP = player.maxHP;
        EventCenter.Instance.EventTrigger<IUnit>("HPChanged", player);
        if (enableDebugLog)
            Debug.Log($"[BonfireTest] 玩家血量已恢复至：{player.currentHP}/{player.maxHP}");

        // 3. 重置玩家速度
        if (player.Rb != null)
        {
            player.Rb.velocity = Vector2.zero;
        }

        // 4. Boss血量和架势条回满
        if (boss != null)
        {
            boss.currentHP = boss.maxHP;
            boss.currentPosture = boss.maxPosture;
            EventCenter.Instance.EventTrigger<IUnit>("HPChanged", boss);
            EventCenter.Instance.EventTrigger<IUnit>("PostureChanged", boss);
            if (enableDebugLog)
                Debug.Log($"[BonfireTest] Boss血量已恢复至：{boss.currentHP}/{boss.maxHP}，架势条已恢复至：{boss.currentPosture}/{boss.maxPosture}");
        }

        // 5. 恢复药水使用次数
        if (potionTest != null)
        {
            potionTest.RestorePotionCount();
            if (enableDebugLog)
                Debug.Log($"[BonfireTest] 药水使用次数已恢复");
        }

        // 重置死亡标记
        isPlayerDead = false;

        if (enableDebugLog)
            Debug.Log("[BonfireTest] 玩家复活完成！");
    }

    /// <summary>
    /// 手动触发复活（用于测试）
    /// </summary>
    [ContextMenu("手动触发复活")]
    public void ManualRespawn()
    {
        if (player != null)
        {
            player.currentHP = 0; // 模拟死亡
            isPlayerDead = true;
            RespawnPlayer();
        }
    }

    // 在Scene视图中绘制篝火位置
    void OnDrawGizmos()
    {
        if (bonfirePosition != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(bonfirePosition.position, 0.5f);
            Gizmos.DrawLine(bonfirePosition.position, bonfirePosition.position + Vector3.up * 2f);
        }
    }
}
