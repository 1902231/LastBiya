/// <summary>
/// 无敌能力
/// 支持两种模式：
/// 1. 设置 duration > 0 后激活，自动计时结束
/// 2. 手动调用 Deactivate 提前结束
/// </summary>
public class PlayerAbility_Invincible : BaseAbility<PlayerController>
{
    private float timer;
    private float duration;

    public PlayerAbility_Invincible()
    {
        priority = 0;
    }

    /// <summary>
    /// 设置本次无敌持续时间，0 表示不自动结束
    /// </summary>
    public void SetDuration(float dur)
    {
        duration = dur;
    }

    public override bool CanActivate()
    {
        // 允许重复激活以刷新计时
        return isUnlocked;
    }

    public override void Activate()
    {
        base.Activate();
        timer = 0f;
        owner.IsInvincible = true;
    }

    public override void Tick(float deltaTime)
    {
        if (duration <= 0f) return;

        timer += deltaTime;
        if (timer >= duration)
        {
            Deactivate();
        }
    }

    public override void Deactivate()
    {
        owner.IsInvincible = false;
        base.Deactivate();
    }
}
