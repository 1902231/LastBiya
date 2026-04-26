/// <summary>
/// Boss 状态枚举
/// </summary>
public enum E_BossStateType_01
{
    // 父状态
    Alive,
    
    // 子状态
    Idle,           // 待机
    Move,           // 移动
    SlashAttack,    // 前劈攻击
    FlyUp,          // 飞起
    SlamDown,       // 下砸
    RangedAttack,   // 远程攻击
    Stunned,        // 破防倒地
    
    // 独立状态
    Dead,           // 死亡
}
