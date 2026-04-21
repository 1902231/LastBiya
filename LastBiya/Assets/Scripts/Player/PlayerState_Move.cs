using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerState_Move : HFSM_BaseState<E_PlayerStateType ,PlayerController>
{
    public PlayerState_Move()
    {

    }

    public override void OnEnter()
    {
        Debug.Log("½øÈë×´Ì¬£ºWalk");
        base.OnEnter();
    }

    public override void OnUpdate()
    {
        base.OnUpdate();
    }


    public override void OnExit()
    {
        Debug.Log("ÍË³ö×´Ì¬£ºWalk");
        base.OnExit();
    }

}
