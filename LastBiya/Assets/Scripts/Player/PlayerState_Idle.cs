using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerState_Idle : HFSM_BaseState<E_PlayerStateType, PlayerController>
{
    public PlayerState_Idle()
    {
        this.parentType = E_PlayerStateType.Grounded;
    }

    public override void OnEnter()
    {
        //Debug.Log("进入状态：Idle");
        owner.Rb.velocity = Vector2.zero;
    }


    public override void OnUpdate()
    {
        if (InputManager.Instance.MoveInput.x != 0)
            hfsm.SwitchState(E_PlayerStateType.Move);
    }

    public override void OnExit()
    {
        //Debug.Log("退出状态：Idle");
        //base.OnExit();
    }
}
