/// <summary>
/// 无敌能力
/// 手动激活/关闭，激活期间免疫敌人伤害
/// 由其他状态或 Ability 在需要时调用 TryActivate / ForceDeactivate
/// </summary>
public class PlayerAbility_Invincible : BaseAbility<PlayerController>
{
    public PlayerAbility_Invincible()
    {
        priority = 0;
    }

    public override bool CanActivate()
    {
        return isUnlocked && !isActive;
    }

    public override void Activate()
    {
        base.Activate();
        owner.IsInvincible = true;
    }

    public override void Deactivate()
    {
        owner.IsInvincible = false;
        base.Deactivate();
    }
}
