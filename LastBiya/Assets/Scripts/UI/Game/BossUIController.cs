using UnityEngine;

/// <summary>
/// Boss UI Controller：订阅 HPChanged 和 PostureChanged 事件
/// 通过 IUnit 接口过滤，只响应绑定的 Boss
/// 可复用于不同 Boss，通过 BindBoss 绑定目标
/// </summary>
public class BossUIController : MonoBehaviour
{
    [SerializeField] BossUIView view;
    [SerializeField] [Tooltip("测试用：拖入 Boss 物体绑定")]
    MonoBehaviour bossReference;

    private IUnit currentBoss;

    void Start()
    {
        EventCenter.Instance.AddEventListener<IUnit>("HPChanged", OnHPChanged);
        EventCenter.Instance.AddEventListener<IUnit>("PostureChanged", OnPostureChanged);
        
        // 监听开场完成事件（显示血条）
        EventCenter.Instance.AddEventListener<IUnit>("BossIntroComplete", OnBossIntroComplete);
        
        // 监听标题显示/隐藏事件
        EventCenter.Instance.AddEventListener<string>("ShowBossTitle", OnShowBossTitle);
        EventCenter.Instance.AddEventListener("HideBossTitle", OnHideBossTitle);
        
        // 监听退出事件
        EventCenter.Instance.AddEventListener<IUnit>("BossExited", OnBossExited);

        // 如果 Inspector 里拖了 Boss，自动绑定（向后兼容，测试用）
        if (bossReference != null && bossReference is IUnit unit)
        {
            BindBoss(unit);
        }
        else
        {
            // 初始隐藏血条和架势条，但保持 GameObject 激活以便显示标题
            view.HideHealthBars();
        }
    }
    
    /// <summary>
    /// Boss 开场动画完成后显示血条
    /// </summary>
    private void OnBossIntroComplete(IUnit boss)
    {
        BindBoss(boss);
    }
    
    /// <summary>
    /// 显示 Boss 标题（大字，开场时显示）
    /// </summary>
    private void OnShowBossTitle(string bossName)
    {
        view.ShowTitle(bossName);
    }
    
    /// <summary>
    /// 隐藏 Boss 标题
    /// </summary>
    private void OnHideBossTitle()
    {
        view.HideTitle();
    }
    
    /// <summary>
    /// Boss 离开场景时自动解绑
    /// </summary>
    private void OnBossExited(IUnit boss)
    {
        if (boss == currentBoss)
            UnbindBoss();
    }

    /// <summary>
    /// 绑定一个 Boss，初始化 UI 显示
    /// </summary>
    public void BindBoss(IUnit boss)
    {
        currentBoss = boss;
        view.SetBossName(boss.UnitName);
        view.InitFull();
        view.ShowHealthBars();  // 显示血条和架势条
    }

    /// <summary>
    /// 解绑 Boss，隐藏 UI
    /// </summary>
    public void UnbindBoss()
    {
        currentBoss = null;
        view.HideHealthBars();  // 隐藏血条和架势条
    }

    private void OnHPChanged(IUnit unit)
    {
        if (unit != currentBoss) return;
        view.UpdateHP(unit.CurrentHP / unit.MaxHP);
    }

    private void OnPostureChanged(IUnit unit)
    {
        if (unit != currentBoss) return;
        if (unit.MaxPosture <= 0) return;
        view.UpdatePosture(unit.CurrentPosture / unit.MaxPosture);
    }

    void OnDestroy()
    {
        EventCenter.Instance.RemoveEventListener<IUnit>("HPChanged", OnHPChanged);
        EventCenter.Instance.RemoveEventListener<IUnit>("PostureChanged", OnPostureChanged);
        EventCenter.Instance.RemoveEventListener<IUnit>("BossIntroComplete", OnBossIntroComplete);
        EventCenter.Instance.RemoveEventListener<string>("ShowBossTitle", OnShowBossTitle);
        EventCenter.Instance.RemoveEventListener("HideBossTitle", OnHideBossTitle);
        EventCenter.Instance.RemoveEventListener<IUnit>("BossExited", OnBossExited);
    }
}
