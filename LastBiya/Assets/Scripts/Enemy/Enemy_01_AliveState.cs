/// <summary>
/// 活着的父状态，管所有活着时的共性逻辑
/// </summary>
public class Enemy_01_AliveState : HFSM_BaseState<E_Enemy01StateType, Enemy_01>
{
    public Enemy_01_AliveState()
    {
        // 根状态，无 parent
    }

    public override void OnUpdate()
    {
        // 死亡检测（兜底，TakeDamage 里也会切）
        if (owner.currentHP <= 0)
            hfsm.SwitchState(E_Enemy01StateType.Dead);
    }
}
