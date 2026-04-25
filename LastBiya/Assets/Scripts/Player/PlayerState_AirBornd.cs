using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerState_AirBornd : HFSM_BaseState<E_PlayerStateType,PlayerController>
{
    public PlayerState_AirBornd()
    {
        this.parentType = E_PlayerStateType.Alive;
        this.defaultChildType = E_PlayerStateType.FreeFall;
    }

    public override void OnEnter()
    {
        //Debug.Log("����״̬��AirBornd");
        
    }

    public override void OnUpdate()
    {
        // 空中朝向更新，所有子状态（Jump、FreeFall 等）都继承
        owner.UpdateFacing(InputManager.Instance.MoveInput.x);
    }


    public override void OnExit()
    {
        //Debug.Log("�˳�״̬��AirBornd");
        

    }

}
