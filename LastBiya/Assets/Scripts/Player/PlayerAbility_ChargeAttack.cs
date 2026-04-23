/// <summary>
/// 蓄力攻击能力（门卫型）
/// 只管解锁判断，实际蓄力行为由 PlayerState_ChargeAttack 执行
/// priority = 2，蓄力期间除受伤(3)外不可被打断
/// </summary>
public class PlayerAbility_ChargeAttack : BaseAbility<PlayerController>
{
    public PlayerAbility_ChargeAttack()
    {
        priority = 2;
    }

    public override bool CanActivate()
    {
        return isUnlocked;
    }

    public override void Activate()
    {
        // 瞬发门卫
        isActive = false;
    }
}
