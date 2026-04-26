using UnityEngine;

public class PlayerState_Alive : HFSM_BaseState<E_PlayerStateType, PlayerController>
{
    public PlayerState_Alive()
    {

    }

    public override void OnEnter()
    {
        EventCenter.Instance.AddEventListener<DamageInfo>("PlayerHurt", OnPlayerHurt);
    }

    private void OnPlayerHurt(DamageInfo info)
    {
        hfsm.SwitchState(E_PlayerStateType.Heart);
    }

    public override void OnUpdate()
    {
    }

    public override void OnExit()
    {
        EventCenter.Instance.RemoveEventListener<DamageInfo>("PlayerHurt", OnPlayerHurt);
    }
}
