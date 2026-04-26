/// <summary>
/// Alive 状态：除死亡外所有状态的父状态
/// </summary>
public class Boss_AliveState : HFSM_BaseState<E_BossStateType_01, Boss01>
{
    public Boss_AliveState()
    {
        this.defaultChildType = E_BossStateType_01.Idle;
    }
}
