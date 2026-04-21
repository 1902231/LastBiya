using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerState_Idle : HFSM_BaseState<E_PlayerStateType, PlayerController>
{
    public PlayerState_Idle()
    {
        
    }

    public override void OnEnter()
    {
        Debug.Log("进入状态：Idle");
        base.OnEnter();
    }


    public override void OnUpdate()
    {
        base.OnUpdate();
        //if (owner.Input.MoveAction.WasPressedThisFrame())
        //    hfsm.SwitchState(E_PlayerStateType.Move);
        if (InputManager.Instance.MoveAction.WasPressedThisFrame())
        {
            hfsm.SwitchState(E_PlayerStateType.Move);
        }
    }

    public override void OnExit()
    {
        Debug.Log("退出状态：Idle");
        base.OnExit();
    }
}
