using UnityEngine;

/// <summary>
/// 药水系统测试脚本
/// 功能：
/// 1. 游戏开始时，玩家药水使用次数为1
/// 2. 当玩家按下UseItem键时，如果药水使用次数大于0，让玩家血量回满，触发HPChanged事件，药水使用次数减一
/// </summary>
public class PotionTest : MonoBehaviour
{
    [Header("药水配置")]
    [Tooltip("药水初始使用次数")]
    public int initialPotionCount = 1;

    [Header("引用")]
    [Tooltip("玩家控制器引用，留空则自动查找")]
    public PlayerController player;

    // 当前药水使用次数
    private int currentPotionCount;

    void Start()
    {
        // 初始化药水使用次数
        currentPotionCount = initialPotionCount;

        // 自动查找玩家
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player")?.GetComponent<PlayerController>();
            if (player == null)
            {
                Debug.LogError("[PotionTest] 未找到玩家对象！请确保玩家对象有 'Player' 标签。");
                enabled = false;
                return;
            }
        }

        Debug.Log($"[PotionTest] 药水系统初始化完成，初始药水数量：{currentPotionCount}");
    }

    void Update()
    {
        // 检查InputManager是否存在
        if (InputManager.Instance == null)
        {
            Debug.LogWarning("[PotionTest] InputManager 未初始化！");
            return;
        }

        // 检查UseItemAction是否存在
        if (InputManager.Instance.UseItemAction == null)
        {
            Debug.LogWarning("[PotionTest] UseItemAction 未在 InputManager 中注册！");
            return;
        }

        // 检测UseItem按键按下
        if (InputManager.Instance.UseItemAction.WasPressedThisFrame())
        {
            TryUsePotion();
        }
    }

    /// <summary>
    /// 尝试使用药水
    /// </summary>
    private void TryUsePotion()
    {
        // 检查药水数量
        if (currentPotionCount <= 0)
        {
            Debug.Log("[PotionTest] 药水使用次数不足！");
            return;
        }

        // 检查玩家是否已满血
        if (player.currentHP >= player.maxHP)
        {
            Debug.Log("[PotionTest] 玩家血量已满，无需使用药水。");
            return;
        }

        // 使用药水：回满血量
        float healAmount = player.maxHP - player.currentHP;
        player.currentHP = player.maxHP;

        // 减少药水使用次数
        currentPotionCount--;

        // 触发HPChanged事件
        EventCenter.Instance.EventTrigger<IUnit>("HPChanged", player);

        Debug.Log($"[PotionTest] 使用药水成功！回复 {healAmount} 点生命值，剩余药水次数：{currentPotionCount}");
    }

    /// <summary>
    /// 恢复药水使用次数（供篝火系统调用）
    /// </summary>
    public void RestorePotionCount()
    {
        currentPotionCount = initialPotionCount;
        Debug.Log($"[PotionTest] 药水使用次数已恢复至：{currentPotionCount}");
    }

    /// <summary>
    /// 获取当前药水使用次数
    /// </summary>
    public int GetCurrentPotionCount()
    {
        return currentPotionCount;
    }

    /// <summary>
    /// 设置药水使用次数
    /// </summary>
    public void SetPotionCount(int count)
    {
        currentPotionCount = Mathf.Max(0, count);
        Debug.Log($"[PotionTest] 药水使用次数已设置为：{currentPotionCount}");
    }
}
