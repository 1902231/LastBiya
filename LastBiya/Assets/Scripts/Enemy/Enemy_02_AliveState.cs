public class Enemy_02_AliveState : HFSM_BaseState<E_Enemy02StateType, Enemy_02>
{
    public override void OnUpdate()
    {
        if (owner.currentHP <= 0)
            hfsm.SwitchState(E_Enemy02StateType.Dead);
    }
}
