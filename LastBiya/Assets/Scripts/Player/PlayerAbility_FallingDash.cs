/// <summary>
/// 下冲能力（门卫型）
/// 只管解锁判断，实际下冲行为由 PlayerState_FallingDash 执行
/// </summary>
public class PlayerAbility_FallingDash : BaseAbility<PlayerController>
{
    public PlayerAbility_FallingDash()
    {
        priority = 0;
    }

    public override bool CanActivate()
    {
        return isUnlocked;
    }

    public override void Activate()
    {
        // 瞬发，不需要持续 Tick
        isActive = false;
    }
}
