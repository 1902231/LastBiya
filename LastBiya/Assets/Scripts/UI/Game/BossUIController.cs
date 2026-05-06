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

        // 如果 Inspector 里拖了 Boss，自动绑定
        if (bossReference != null && bossReference is IUnit unit)
            BindBoss(unit);
    }

    /// <summary>
    /// 绑定一个 Boss，初始化 UI 显示
    /// </summary>
    public void BindBoss(IUnit boss)
    {
        currentBoss = boss;
        view.SetBossName(boss.UnitName);
        view.InitFull();
        gameObject.SetActive(true);
    }

    /// <summary>
    /// 解绑 Boss，隐藏 UI
    /// </summary>
    public void UnbindBoss()
    {
        currentBoss = null;
        gameObject.SetActive(false);
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
    }
}
