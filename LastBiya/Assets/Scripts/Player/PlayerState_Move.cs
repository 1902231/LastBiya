using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Windows;

public class PlayerState_Move : HFSM_BaseState<E_PlayerStateType ,PlayerController>
{
    public PlayerState_Move()
    {
        this.parentType = E_PlayerStateType.Grounded;

    }

    public override void OnEnter()
    {
        //Debug.Log("进入状态：Move"); 
        
    }

    public override void OnUpdate()
    {
        // 更新朝向
        owner.UpdateFacing(InputManager.Instance.MoveInput.x);
    }

    public override void OnFixedUpdate()
    {
        Vector2 input = InputManager.Instance.MoveInput;
        owner.ApplyHorizontalMovement(input.x);
    }


    public override void OnExit()
    {
        //Debug.Log("退出状态：Move");
        base.OnExit();
    }

}
