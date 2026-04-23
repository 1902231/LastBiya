/// <summary>
/// 二段跳能力
/// 管理空中额外跳跃次数，激活时消耗一次，落地时重置
/// </summary>
public class PlayerAbility_DoubleJump : BaseAbility<PlayerController>
{
    private int maxAirJumps;
    private int airJumpsRemaining;

    public PlayerAbility_DoubleJump(int maxAirJumps = 1)
    {
        this.maxAirJumps = maxAirJumps;
        this.airJumpsRemaining = maxAirJumps;
    }

    public override bool CanActivate()
    {
        return isUnlocked && airJumpsRemaining > 0;
    }

    public override void Activate()
    {
        airJumpsRemaining--;
        // 瞬发，不需要持续 Tick
        isActive = false;
    }

    public void ResetJumps()
    {
        airJumpsRemaining = maxAirJumps;
    }
}
