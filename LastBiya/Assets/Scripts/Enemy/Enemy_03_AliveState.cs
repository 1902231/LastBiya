public class Enemy_03_AliveState : HFSM_BaseState<E_Enemy03StateType, Enemy_03>
{
    public Enemy_03_AliveState()
    {
        this.defaultChildType = E_Enemy03StateType.Patrol;
    }

    public override void OnUpdate()
    {
        if (owner.currentHP <= 0)
            hfsm.SwitchState(E_Enemy03StateType.Dead);
    }
}
