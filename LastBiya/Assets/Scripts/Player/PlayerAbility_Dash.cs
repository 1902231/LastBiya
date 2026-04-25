/// <summary>
/// 冲刺能力（门卫型）
/// 只管解锁判断和冷却，实际冲刺行为由 PlayerState_Dash 执行
/// </summary>
public class PlayerAbility_Dash : BaseAbility<PlayerController>
{
    public PlayerAbility_Dash()
    {
        priority = 0;
    }

    public override bool CanActivate()
    {
        return isUnlocked && owner.CanDash();
    }

    public override void Activate()
    {
        owner.StartDashCooldown();
        // 瞬发，不需要持续 Tick
        isActive = false;
    }
}
